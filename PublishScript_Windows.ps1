$datetime = Get-Date -Format "yyyyMMdd-HHmm"
$DATETIME_FOLDER = $datetime

# Clean previous build
Remove-Item "WooriOptical\bin\Release" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "backend" -Recurse -Force -ErrorAction SilentlyContinue

# Publish backend and copy database file
dotnet publish WooriOptical\WooriOptical.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./backend
Copy-Item "WooriOptical\WooriOptical.db" "backend\" -Force

# Build Electron app
npm run package --prefix WooriOptical.Electron

# Copy output with datetime folder
New-Item -ItemType Directory -Path "Release\$DATETIME_FOLDER" -Force

Copy-Item "WooriOptical.Electron\out\*" "Release\$DATETIME_FOLDER\" -Recurse -Force

Remove-Item "WooriOptical\bin\Release" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "backend" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item "WooriOptical.Electron\out" -Recurse -Force -ErrorAction SilentlyContinue

Write-Output "Release completed: Release\$DATETIME_FOLDER"