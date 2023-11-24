@echo off

setx DOTNET_ROLL_FORWARD_TO_PRERELEASE 1
set TARGET=%AppData%\Nuget\godot

if not exist %TARGET% mkdir %TARGET%
dotnet nuget add source godot --name Godot

for %%i in (C:\godot\GodotSharp\Tools\nupkgs\*.nupkg) do xcopy %%i %TARGET% /Y /Q
for %%i in (C:\godot\GodotSharp\Tools\nupkgs\*.snupkg) do xcopy %%i %TARGET% /Y /Q