@echo off
if not exist "%~dp0Builds\Windows\Skeleton Defender.exe" (
  echo Build Windows from the Skeleton Defender menu in Unity first.
  pause
  exit /b 1
)
start "" "%~dp0Builds\Windows\Skeleton Defender.exe"
