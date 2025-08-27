# Woori Optical Customer Management

Lightweight customer-management backend (ASP.NET Core) plus multiple desktop hosts (Electron, macOS SwiftUI, WinForms). This README documents macOS and cross‑platform deployment and the included publish scripts.

## Supported hosts
- Backend: ASP.NET Core (WooriOptical project)
- Desktop hosts:
  - Electron (cross‑platform) — WooriOptical.Electron
  - macOS SwiftUI host — WooriOpticalOSX (Xcode target)
  - Windows WinForms host — WooriOptical.Desktop

## Quick prerequisites
- .NET 9+ SDK
- macOS (Intel or Apple Silicon) for macOS packaging
- Xcode for macOS SwiftUI host
- Node.js + npm for Electron
- (Optional) electron-builder for packaging
- dotnet accessible in PATH if you run .dll variant

## Build & run backend (development)
From repo root:
```bash
dotnet restore
dotnet build
dotnet run --project WooriOptical/WooriOptical.csproj --urls "http://127.0.0.1:5000"
```
Use 127.0.0.1 for local-only bindings (avoids some sandbox/dns issues).

## Electron host (development)
1. Ensure backend is running (see above) or place published backend in WooriOptical.Electron/backend for dev.
2. Start Electron:
```bash
cd WooriOptical.Electron
npm install
npm start
```

### Packaging Electron (recommended)
- Publish backend first and copy into extraResources referenced by package.json. Example build snippet in package.json:
```json
"build": {
  "appId": "com.myunglyu.woorioptical",
  "mac": { "target": ["dmg","zip"] },
  "extraResources": [
    { "from": "publish/backend/", "to": "backend", "filter": ["**/*"] }
  ],
  "asarUnpack": ["backend/**"]
}
```
- Important: put the backend in extraResources and use asarUnpack so the binary remains executable at runtime.
- Ensure the backend executable has execute permission before packaging: `chmod +x publish/backend/WooriOptical`

## macOS SwiftUI host (development)
- Option A — external backend (recommended during development)
  1. Run backend: `dotnet run --project WooriOptical/WooriOptical.csproj --urls "http://127.0.0.1:5000"`
  2. Open `WooriOpticalOSX/WooriOpticalOSX.xcodeproj` and Run.

- Option B — bundle backend into the .app (single-bundle distribution)
  1. Publish backend for macOS (see publish section).
  2. Add an Xcode Run Script build phase (before "Copy Bundle Resources") to copy published files into the app bundle Resources (e.g., Contents/Resources/Server). Example:
     ```bash
     SERVER_PROJECT="${SRCROOT}/../../WooriOptical"
     OUT_DIR="${SRCROOT}/../publish/Server"
     dotnet publish "$SERVER_PROJECT" -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -o "$OUT_DIR"
     DEST_DIR="${BUILT_PRODUCTS_DIR}/${PRODUCT_NAME}.app/Contents/Resources/Server"
     mkdir -p "$DEST_DIR"
     rsync -a --delete "$OUT_DIR/" "$DEST_DIR/"
     chmod -R 755 "$DEST_DIR"
     ```
  3. Implement AppDelegate to start the bundled server (Contents/Resources/Server/...) on app launch and stop it on termination. The SwiftUI app already polls localhost and will load the WebView once the server responds.

## Publish backend (macOS / Windows)
- macOS Arm (Apple Silicon):
```bash
dotnet publish WooriOptical/WooriOptical.csproj -c Release -r osx-arm64 --self-contained true /p:PublishSingleFile=true -o publish/macos-arm64
```
- macOS Intel:
```bash
dotnet publish WooriOptical/WooriOptical.csproj -c Release -r osx-x64 --self-contained true /p:PublishSingleFile=true -o publish/macos-x64
```
- Windows x64:
```powershell
dotnet publish WooriOptical/WooriOptical.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true -o publish/win-x64
```
After publishing, copy the published executable into your desktop host (Electron extraResources or Xcode app bundle). Ensure `chmod +x` on macOS.

## Included publish scripts
- PublishScript_Windows.ps1 — publishes backend (win-x64), builds Electron package, and creates a timestamped Release folder.
- PublishScript_macOS.sh — publishes backend for macOS, copies to Electron/backend, installs Node modules and runs electron packager/maker (adjust as needed).

Run them from repo root:
```bash
# macOS
chmod +x PublishScript_macOS.sh
./PublishScript_macOS.sh

# Windows (PowerShell)
.\PublishScript_Windows.ps1
```
Adjust paths/runtimes inside the scripts to match target architecture.

## Troubleshooting & tips
- If Electron can't spawn the backend:
  - Verify the backend binary exists in the packaged Resources/backend or app.asar.unpacked/backend.
  - Ensure execute bit is set: `chmod +x`.
  - If the backend is a .dll, Electron must invoke `dotnet path/to.dll` (or publish a self-contained binary).
  - For packaged apps, use `extraResources` + `asarUnpack` so the binary is accessible/executable.
  - Use 127.0.0.1 instead of localhost to avoid resolution failures in sandboxed contexts.

- If macOS SwiftUI host shows sandbox errors:
  - App Sandbox restricts launching bundled binaries. For development disable sandbox; for production use proper helper tools (SMJobBless) or sign and configure privileged helpers per Apple guidelines.
  - Add network entitlement if sandboxing (outgoing network).

- Use the app’s logs: Electron main process prints backend stdout/stderr; capture those to diagnose startup issues.
=======
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
- admin.json (WooriOptical/admin.json) is used for initial admin seeding; keep it out of source control if sensitive.
- Backend defaults to local-only access.

---

© 2025 Woori Optical.
