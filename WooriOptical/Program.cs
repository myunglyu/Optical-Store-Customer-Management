using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using WooriOptical.Models;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
if (builder.Environment.IsProduction() && OperatingSystem.IsWindows())
{
    builder.Logging.AddEventLog();
}


// Add services to the container.
builder.Services.AddControllersWithViews();

// Add response caching
builder.Services.AddResponseCaching();
builder.Services.AddMemoryCache();

// Add health checks
builder.Services.AddHealthChecks();

// Add DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Identity services
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Configure Identity options to disable registration
builder.Services.Configure<IdentityOptions>(options =>
{
    // Disable user registration
    options.SignIn.RequireConfirmedAccount = false;
});

// Configure application cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Register services
builder.Services.AddScoped<WooriOptical.Services.ICustomerService, WooriOptical.Services.CustomerService>();
builder.Services.AddScoped<WooriOptical.Services.IBackupService, WooriOptical.Services.BackupService>();
builder.Services.AddScoped<WooriOptical.Services.IOrderService, WooriOptical.Services.OrderService>();
builder.Services.AddScoped<WooriOptical.Services.IPrescriptionService, WooriOptical.Services.PrescriptionService>();
// Register IHttpClientFactory for outbound HTTP calls (USPS lookup)
builder.Services.AddHttpClient();


var app = builder.Build();

// Open browser to app URL on startup
// var url = "http://localhost:5000";
// try
// {
//     using var process = new System.Diagnostics.Process();
//     process.StartInfo.FileName = url;
//     process.StartInfo.UseShellExecute = true;
//     process.Start();
// }
// catch { /* Ignore errors if browser can't be opened */ }

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}


// Restrict to local access only
app.Use(async (context, next) =>
{
    var remoteIp = context.Connection.RemoteIpAddress;
    if (remoteIp == null || !System.Net.IPAddress.IsLoopback(remoteIp))
    {
        context.Response.StatusCode = 403;
        await context.Response.WriteAsync("Local access only.");
        return;
    }
    await next();
});

app.UseHttpsRedirection();
app.UseResponseCaching();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();


app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// Add health check endpoint
app.MapHealthChecks("/health");


// Seed admin account from admin.json in all environments
using (var scope = app.Services.CreateScope())
{
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var logger = scope.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger("SeedAdmin");
    
    // Try multiple possible locations for admin.json
    var possiblePaths = new[]
    {
        Path.Combine(Directory.GetCurrentDirectory(), "admin.json"),
        Path.Combine(AppContext.BaseDirectory, "admin.json"),
        Path.Combine(builder.Environment.ContentRootPath, "admin.json")
    };
    
    string? adminJsonPath = null;
    foreach (var path in possiblePaths)
    {
        if (File.Exists(path))
        {
            adminJsonPath = path;
            logger?.LogInformation("Found admin.json at: {Path}", path);
            break;
        }
    }
    
    if (adminJsonPath != null)
    {
        try
        {
            var json = await File.ReadAllTextAsync(adminJsonPath);
            if (!string.IsNullOrWhiteSpace(json))
            {
                using var doc = System.Text.Json.JsonDocument.Parse(json);
                var root = doc.RootElement;
                var userName = root.GetProperty("UserName").GetString();
                var email = root.TryGetProperty("Email", out var emailProp) ? emailProp.GetString() : null;
                var password = root.GetProperty("Password").GetString();
                
                if (!string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(password))
                {
                    logger?.LogInformation("Creating admin user: {UserName}", userName);
                    
                    // Ensure Admin role exists
                    var adminRoleExists = await roleManager.RoleExistsAsync("Admin");
                    if (!adminRoleExists)
                    {
                        var roleResult = await roleManager.CreateAsync(new IdentityRole("Admin"));
                        if (!roleResult.Succeeded)
                        {
                            logger?.LogError("Failed to create Admin role: {Errors}", string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                            throw new Exception($"Failed to create Admin role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                        }
                        logger?.LogInformation("Created Admin role");
                    }

                    var managerRoleExists = await roleManager.RoleExistsAsync("Manager");
                    if (!managerRoleExists)
                    {
                        var roleResult = await roleManager.CreateAsync(new IdentityRole("Manager"));
                        if (!roleResult.Succeeded)
                        {
                            logger?.LogError("Failed to create Manager role: {Errors}", string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                            throw new Exception($"Failed to create Manager role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                        }
                        logger?.LogInformation("Created Manager role");
                    }

                    var UserRoleExists = await roleManager.RoleExistsAsync("User");
                    if (!UserRoleExists)
                    {
                        var roleResult = await roleManager.CreateAsync(new IdentityRole("User"));
                        if (!roleResult.Succeeded)
                        {
                            logger?.LogError("Failed to create User role: {Errors}", string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                            throw new Exception($"Failed to create User role: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                        }
                        logger?.LogInformation("Created User role");
                    }

                    var adminUser = await userManager.FindByNameAsync(userName);
                    if (adminUser == null)
                    {
                        var user = new IdentityUser { UserName = userName, Email = email };
                        var result = await userManager.CreateAsync(user, password);
                        if (!result.Succeeded)
                        {
                            logger?.LogError("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                            throw new Exception($"Failed to create admin user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                        }
                        await userManager.AddToRoleAsync(user, "Admin");
                        logger?.LogInformation("Successfully created admin user: {UserName}", userName);
                    }
                    else
                    {
                        // Ensure user is in Admin role
                        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
                        {
                            await userManager.AddToRoleAsync(adminUser, "Admin");
                            logger?.LogInformation("Added Admin role to existing user: {UserName}", userName);
                        }
                    }
                }
                else
                {
                    logger?.LogWarning("admin.json missing required UserName or Password fields");
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to read or parse admin.json from: {Path}", adminJsonPath);
        }
    }
    else
    {
        logger?.LogWarning("admin.json not found in any of the expected locations: {Paths}", string.Join(", ", possiblePaths));
    }
}


app.Run();
