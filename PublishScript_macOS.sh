#!/bin/bash

rm -rf ./backend
rm -rf ./WooriOptical.Electron/out
rm -rf ./WooriOptical.Electron/backend

dotnet publish WooriOptical/WooriOptical.csproj -c Release --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./backend
echo "Backend published to ./backend"
cp ./WooriOptical/WooriOptical.db ./backend
cp -R ./backend ./WooriOptical.Electron/backend
echo "Backend copied to ./WooriOptical.Electron/backend"

cd WooriOptical.Electron

npm install
echo "Node modules installed"
npm run make
cp -R ./out ../WooriOptical_Build_MacOS
echo "Electron app built at WooriOptical_Build_MacOS"

cd ..
rm -rf ./backend
rm -rf ./WooriOptical.Electron/out
rm -rf ./WooriOptical.Electron/backend
echo "Cleanup complete"
