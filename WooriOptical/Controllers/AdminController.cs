
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using WooriOptical.Models;
using WooriOptical.Services;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Runtime.CompilerServices;

namespace WooriOptical.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IBackupService _backupService;

    public AdminController(UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, IBackupService backupService)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _backupService = backupService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Get all user accounts
        var users = _userManager.Users.ToList();
        var accounts = new List<Account>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            accounts.Add(new Account
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Password = string.Empty,
                Role = roles.FirstOrDefault()
            });
        }

        return View(accounts);
    }

    [HttpGet]
    public IActionResult CreateAccount()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateAccount(Account newAccount)
    {
        if (!ModelState.IsValid)
            return View(newAccount);

        var user = new Account { UserName = newAccount.UserName, Email = newAccount.Email, Password = newAccount.Password };
        var result = await _userManager.CreateAsync(user, user.Password);
        if (result.Succeeded)
        {
            // Assign role if specified
            if (!string.IsNullOrEmpty(newAccount.Role))
            {
                await _userManager.AddToRoleAsync(user, newAccount.Role);
            }
            TempData["Message"] = "Account created successfully.";
            return RedirectToAction("Index");
        }
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return View(newAccount);
    }

    [HttpGet]
    public async Task<IActionResult> EditAccount(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            TempData["Message"] = "Invalid user ID.";
            return RedirectToAction("Index");
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            TempData["Message"] = "User not found.";
            return RedirectToAction("Index");
        }

        var account = new AccountViewModel
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            Password = string.Empty, // Password should not be pre-filled
            Role = (await _userManager.GetRolesAsync(user)).FirstOrDefault()
        };

        return View(account);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditAccount(AccountViewModel updatedAccount)
    {
        if (!ModelState.IsValid)
            return View(updatedAccount);

        var user = await _userManager.FindByIdAsync(updatedAccount.Id);
        if (user == null)
        {
            TempData["Message"] = "User not found.";
            return RedirectToAction("Index");
        }

        user.UserName = updatedAccount.UserName;
        user.Email = updatedAccount.Email;

        var updateResult = await _userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(updatedAccount);
        }

        // Handle password change if provided
        if (!string.IsNullOrEmpty(updatedAccount.Password))
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var passwordResult = await _userManager.ResetPasswordAsync(user, token, updatedAccount.Password);
            if (!passwordResult.Succeeded)
            {
                foreach (var error in passwordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(updatedAccount);
            }
        }

        // Handle role changes
        var currentRoles = await _userManager.GetRolesAsync(user);
        if (currentRoles.Any())
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
        }

        if (!string.IsNullOrEmpty(updatedAccount.Role))
        {
            await _userManager.AddToRoleAsync(user, updatedAccount.Role);
        }

        TempData["Message"] = "Account updated successfully.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            TempData["Message"] = "Invalid user ID.";
            return RedirectToAction("Index");
        }

        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            TempData["Message"] = "User not found.";
            return RedirectToAction("Index");
        }

        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded)
        {
            TempData["Message"] = "User deleted successfully.";
        }
        else
        {
            TempData["Message"] = "Failed to delete user: " + string.Join(", ", result.Errors.Select(e => e.Description));
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BackupDatabase()
    {
        var result = await _backupService.CreateBackupAsync();
        if (result)
        {
            TempData["Message"] = "Database backup created successfully.";
        }
        else
        {
            TempData["Message"] = "Failed to create database backup.";
        }

        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult ManageBackups()
    {
        if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "Backups")))
        {
            TempData["Message"] = "No backups available.";
            return RedirectToAction("Index");
        }

        var backups = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Backups"), "*.db")
            .Select(Path.GetFileName)
            .ToList();

        if (backups.Count == 0)
        {
            TempData["Message"] = "No backups available.";
            return RedirectToAction("Index");
        }

        return View(backups);
    }

    public IActionResult DownloadBackup(string fileName)
    {
        var backupPath = Path.Combine(AppContext.BaseDirectory, "Backups", fileName);
        if (!System.IO.File.Exists(backupPath))
            return NotFound();

        var contentType = "application/octet-stream";
        return File(System.IO.File.ReadAllBytes(backupPath), contentType, fileName);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteBackup(string fileName)
    {
        var backupPath = Path.Combine(AppContext.BaseDirectory, "Backups", fileName);
        if (!System.IO.File.Exists(backupPath))
            return NotFound();

        System.IO.File.Delete(backupPath);
        TempData["Message"] = "Backup deleted successfully.";
        return RedirectToAction("ManageBackups");
    }

    [HttpGet]
    public IActionResult RestoreDatabase()
    {
        if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "Backups")))
        {
            TempData["Message"] = "No backups available.";
            return RedirectToAction("Index");
        }

        var backups = Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, "Backups"), "*.db")
            .Select(Path.GetFileName)
            .ToList();

        if (backups.Count == 0)
        {
            TempData["Message"] = "No backups available.";
            return RedirectToAction("Index");
        }
        
        return View(backups);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RestoreDatabase(string filename)
    {
        if (string.IsNullOrEmpty(filename))
        {
            TempData["Message"] = "Invalid backup filename.";
            return RedirectToAction("Index");
        }

        var backupPath = Path.Combine(AppContext.BaseDirectory, "Backups", filename);

        var result = await _backupService.RestoreBackupAsync(backupPath);
        if (result)
        {
            TempData["Message"] = "Database restored successfully.";
        }
        else
        {
            TempData["Message"] = "Failed to restore database.";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> UploadBackup(IFormFile backupFile)
    {
        if (backupFile != null && backupFile.Length > 0)
        {
            var backupDir = Path.Combine(AppContext.BaseDirectory, "Backups");
            Directory.CreateDirectory(backupDir);
            var filePath = Path.Combine(backupDir, Path.GetFileName(backupFile.FileName));
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await backupFile.CopyToAsync(stream);
            }
            TempData["Message"] = "Backup uploaded successfully.";
        }
        else
        {
            TempData["Message"] = "No file selected.";
        }
        return RedirectToAction("ManageBackups");
    }
}