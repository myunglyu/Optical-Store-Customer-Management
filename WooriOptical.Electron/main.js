const { app, BrowserWindow, dialog } = require('electron');
const { spawn } = require('child_process');
const fs = require('fs');
const path = require('path');
let backend;

function getBackendPath() {
  console.log('app.isPackaged=', app.isPackaged, 'platform=', process.platform, 'NODE_ENV=', process.env.NODE_ENV);

  // candidates to try (ordered)
  const candidates = [];

  if (app.isPackaged) {
    // packaged app: Resources folder
    candidates.push(path.join(process.resourcesPath, 'backend', 'WooriOptical'));
    // electron-builder may put extras in app.asar.unpacked
    candidates.push(path.join(process.resourcesPath, 'app.asar.unpacked', 'backend', 'WooriOptical'));
    // sometimes people put extras directly in Resources
    candidates.push(path.join(process.resourcesPath, 'WooriOptical'));
  } else {
    // development — be permissive: try several dev locations
    candidates.push(path.join(__dirname, 'backend', 'WooriOptical'));
    candidates.push(path.join(__dirname, '..', 'backend', 'WooriOptical'));
    candidates.push(path.join(process.cwd(), 'backend', 'WooriOptical'));
  }

  // Also accept .exe or .dll variants (Windows dev builds or dotnet DLL)
  const variants = [];
  for (const c of candidates) {
    variants.push(c);
    variants.push(c + '.exe');
    variants.push(c + '.dll');
  }

  for (const p of variants) {
    try {
      if (fs.existsSync(p)) {
        console.log('Found backend candidate:', p);
        return p;
      }
    } catch (err) {
      console.warn('existsSync failed for', p, err);
    }
  }

  // nothing found — return null to allow caller to handle
  console.error('Backend executable not found. Tried:', variants);
  return null;
}

function createWindow() {
  const win = new BrowserWindow({
    width: 1200,
    height: 800,
    webPreferences: { 
      nodeIntegration: false,
      contextIsolation: true
    }
  });
  win.setMenuBarVisibility(false);
  win.loadURL('http://127.0.0.1:5000');
}

app.whenReady().then(() => {
  const backendPath = getBackendPath();

  if (!backendPath) {
    const msg = 'Backend binary not found. Make sure you published/copy the backend into Resources/backend or app.asar.unpacked/backend.';
    console.error(msg);
    dialog.showErrorBox('Backend not found', msg);
    createWindow(); // still open UI so user can see error
    return;
  }

  // If we found a .dll, run with 'dotnet'
  const isDll = backendPath.endsWith('.dll');
  const isExe = fs.existsSync(backendPath) && fs.statSync(backendPath).mode & 0o111; // check exec bit

  // If file exists but isn't executable on macOS, try to set +x (best-effort)
  if (process.platform === 'darwin' && !isDll) {
    try {
      fs.chmodSync(backendPath, 0o755);
    } catch (err) {
      console.warn('Could not chmod backend (permission?), continuing:', err.message);
    }
  }

  let spawnCmd, spawnArgs;
  if (isDll) {
    spawnCmd = 'dotnet';
    spawnArgs = [backendPath];
  } else {
    spawnCmd = backendPath;
    spawnArgs = [];
  }

  try {
    backend = spawn(spawnCmd, spawnArgs, {
      cwd: path.dirname(backendPath),
      env: {
        ...process.env,
        ASPNETCORE_URLS: 'http://127.0.0.1:5000'
      },
      stdio: ['ignore', 'pipe', 'pipe']
    });

    backend.stdout.on('data', d => console.log('[backend]', d.toString()));
    backend.stderr.on('data', d => console.error('[backend]', d.toString()));
    backend.on('exit', (code, sig) => console.log('Backend exited', code, sig));
  } catch (err) {
    console.error('Failed to spawn backend:', err);
    dialog.showErrorBox('Failed to start backend', String(err));
  }

  // wait for backend (simple delay); prefer polling HTTP for production
  setTimeout(createWindow, 3000);
});

app.on('window-all-closed', () => {
  if (backend) backend.kill();
  if (process.platform !== 'darwin') app.quit();
});