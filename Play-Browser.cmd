@echo off
setlocal
set "SKELETON_NODE=%USERPROFILE%\.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe"
where node >nul 2>nul
if %ERRORLEVEL% EQU 0 set "SKELETON_NODE=node"
if not exist "%~dp0Builds\Web\index.html" (
  echo Build Browser from the Skeleton Defender menu in Unity first.
  pause
  exit /b 1
)
echo Open http://127.0.0.1:8765 in your browser after the server starts.
"%SKELETON_NODE%" "%~dp0tools\serve-web.cjs"
pause
