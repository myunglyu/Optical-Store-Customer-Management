const { app, BrowserWindow } = require('electron');
const { spawn } = require('child_process');
const path = require('path');
let backend;

function createWindow() {
  const win = new BrowserWindow({
    width: 1200,
    height: 800,
    webPreferences: { 
      nodeIntegration: false,
      contextIsolation: true
    }
  });
  win.loadURL('http://localhost:5000');
}

function getBackendPath() {
  if (app.isPackaged) {
    // In packaged app, extraResources are in a different location
    return path.join(process.resourcesPath, 'backend', 'WooriOptical.exe');
  } else {
    // In development
    return path.join(__dirname, 'backend', 'WooriOptical.exe');
  }
}

app.whenReady().then(() => {
  const backendPath = getBackendPath();

  backend = spawn(backendPath, [], {
    cwd: path.dirname(backendPath),
    env: { 
      ...process.env, 
      ASPNETCORE_URLS: 'http://localhost:5000' 
    }
  });

  // Wait for backend to start
  setTimeout(createWindow, 3000);
});

app.on('window-all-closed', () => {
  if (backend) backend.kill();
  if (process.platform !== 'darwin') app.quit();
});