# Woori Optical Customer Management

This is a production-ready, secure customer management system for optical stores, built with ASP.NET Core MVC, Entity Framework Core (SQLite), and Identity. It includes a WinForms + WebView2 desktop host, Electron for cross-platform support, and PWA support.

## Download Pre-Built Application
[https://github.com/myunglyu/CustomerManagement/releases]

## Features
- Customer, order, and prescription management
- Secure authentication with admin account
- Admin account seeded from `admin.json` at build
- Local-only access (no remote connections allowed)
- WinForms + WebView2 desktop host
- Electron for multi-platform application build
- Single-file, self-contained deployment
- Print support


## Getting Started

### Prerequisites
- .NET 9.0 SDK or newer
- Windows 10/11 (x64)

### Setup
1. **Clone the repository**
2. **Configure the admin account**
   - Edit `WooriOptical/admin.json` with your desired admin credentials:
     ```json
     {
       "UserName": "admin",
       "Password": "YourStrongPassword!",
       "Email": "admin@example.com"
     }
     ```
   - Password can be changed later in the app.
3. **Restore and build the solution**
   ```powershell
   dotnet restore
   dotnet build
   ```
4. **Recreate the database (optional, for a fresh start)**
   ```powershell
   dotnet ef database drop -f --project WooriOptical/WooriOptical.csproj
   dotnet ef database update --project WooriOptical/WooriOptical.csproj
   ```
   
5. **Run the backend app**
   ```powershell
   dotnet run --project WooriOptical/WooriOptical.csproj
   ```
   - The app will be available at `https://localhost:500` (or as configured).
   - Only local access is allowed.
   - Copy woorioptical.db to the debug folder

6. **Run the desktop app**
   ```powershell
   dotnet run --project WooriOptical.Desktop/WooriOptical.Desktop.csproj
   ```
   **Or Access from the browser**
   

### Deployment
- Use the following command to publish as a single-file, self-contained executable:
  ```powershell
  dotnet publish WooriOptical/WooriOptical.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
  dotnet publish WooriOptical.Desktop/WooriOptical.Desktop.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
  ```
- Copy the contents of the `publish` folders to your deployment directory (e.g., `Customer Management`).

## Notes
- The admin account is seeded only if it does not already exist.
- The backend will refuse all non-local requests.

---

© 2025 Woori Optical. All rights reserved.
