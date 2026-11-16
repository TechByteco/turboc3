@echo off
setlocal
title Turbo C++ Installer
color 1F

echo ========================================================
echo          TURBO C++ 3.0 REBUILT - AUTOMATED INSTALLER
echo   - 100%% Authentic Turbo C++ Blue IDE UI ^& Output Screen
echo   - High-Speed C Compiler (Full C99/C11 + conio.h support)
echo   - Native Copy/Paste (Ctrl+C, Ctrl+V, Right-Click)
echo   - Anti-Minimize on Alt+Tab
echo   - Full Horizontal ^& Vertical Scrollbars
echo ========================================================
echo.

set "CURRENT_DIR=%~dp0"
if "%CURRENT_DIR:~-1%"=="\" set "CURRENT_DIR=%CURRENT_DIR:~0,-1%"

echo [1/4] Detecting installation path...
echo       Directory: %CURRENT_DIR%
echo.

echo [2/4] Verifying essential components...
if not exist "%CURRENT_DIR%\Turbo C++.exe" (
    echo [ERROR] Turbo C++.exe not found in %CURRENT_DIR%!
    pause
    exit /b 1
)

if not exist "%CURRENT_DIR%\Compiler\tcc\tcc.exe" (
    echo [ERROR] Fast C Compiler not found in %CURRENT_DIR%\Compiler\tcc!
    pause
    exit /b 1
)
echo       Verification OK!
echo.

echo [3/4] Installing Desktop and Start Menu Shortcuts...
cscript //nologo "%CURRENT_DIR%\CreateShortcuts.vbs" "%CURRENT_DIR%"
if errorlevel 1 (
    echo [WARNING] Shortcut creation encountered a notice, continuing...
) else (
    echo       - Desktop shortcut created: "Turbo C++"
    echo       - Start Menu shortcut created: "Turbo C++"
)
echo.

echo [4/4] Installation Complete!
echo.
echo ========================================================
echo  HOW TO RUN ^& USE YOUR TURBO C++:
echo.
echo  * LAUNCH:
echo    - Double click "Turbo C++" on your Desktop, OR
echo    - Double click "%CURRENT_DIR%\Turbo C++.exe", OR
echo    - Run "%CURRENT_DIR%\START_TURBO_CPP.bat"
echo.
echo  * COMPILE ^& RUN:
echo    - Alt+F9 : Compile (Instant C99/C11 compilation with Turbo C conio.h)
echo    - Ctrl+F9: Run (Opens authentic DOS User Screen window)
echo    - Alt+F5 : View User Screen anytime
echo.
echo  * COPY ^& PASTE (NO ERRORS!):
echo    - Copy in Windows (Ctrl+C) and Paste in Turbo C++ (Ctrl+V or Right-Click)
echo.
echo  * ANTI-MINIMIZE:
echo    - Alt+Tab switch to Chrome/PDF freely without Turbo C++ minimizing!
echo ========================================================
echo.

set /p LAUNCH="Do you want to start Turbo C++ right now? (Y/N): "
if /i "%LAUNCH%"=="Y" (
    start "" "%CURRENT_DIR%\Turbo C++.exe"
)
