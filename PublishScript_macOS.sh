#!/bin/bash

dotnet publish WooriOptical/WooriOptical.csproj -c Release --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./backend
echo "Backend published to ./backend"
cp ./WooriOptical/WooriOptical.db ./backend
cp -R ./backend ../WooriOptical.Electron/backend
echo "Backend copied to ../WooriOptical.Electron/backend"

cd WooriOptical.Electron

npm install
echo "Node modules installed"
npm run make
echo "app packaged at /WooriOptical.Electron/out"
