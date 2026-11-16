# ⚡ Turbo C++ 3.0 Rebuilt for Windows 10 & 11

> **Authentic DOS Experience. Modern Speed. Zero Hassle.**  
> A rebuilt, high-performance edition of the legendary Borland Turbo C++ 3.0 IDE engineered specifically for modern Windows 10 and 11.

---

## 🌟 Highlights & Key Features

* **🎨 100% Authentic Turbo C++ Interface**:
  - Classic Borland Navy Blue workspace (`#0000A8`) with cyan double-line borders (`#00FFFF`).
  - Genuine IBM VGA 8x16 DOS font (`Nouveau IBM`) loaded dynamically across the entire IDE.
  - Pixel-perfect syntax highlighting matching the retro EGA palette (Bright White keywords, Yellow variables, Light Green preprocessor directives, Light Red strings, Light Magenta numbers, and Light Cyan comments/conio functions).

* **⚡ High-Speed C Compiler (C99 / C11 / C17)**:
  - Integrated modern C compiler (`Tiny C Compiler` - TCC) compiling in under **20 milliseconds**.
  - Solves the classic 1990s Borland compiler limitations (supports `for (int i = 0; ...)`, variable declarations anywhere, `unsigned long long`, `void main()`).
  - Full Turbo C extensions included out-of-the-box in `conio.h`: `clrscr()`, `getch()`, `delay()`, `gotoxy()`, `wherex()`, `wherey()`, `textcolor()`, `textbackground()`.
  - Dual compiler mode: Switch between Modern Fast C and Classic 16-bit Borland (via DOSBox-X) anytime from the `Options` menu.

* **📋 Zero-Error Copy & Paste**:
  - Full native Windows clipboard integration (`Ctrl+C`, `Ctrl+V`, and Right-Click).
  - Clean text normalization ensures zero character scrambling, dropped tabs, or IDE lockups.

* **🛡️ Anti-Minimize Feature**:
  - Native Windows desktop application window that **never minimizes** when switching tabs (`Alt+Tab`), referencing PDF lab manuals, or browsing Chrome.

* **🔍 Full Zooming Capabilities**:
  - **Window Zoom**: Press **`F5`** or click **`[ ↑ ]`** / **`[ ↕ ]`** in the window header to toggle between tiled/windowed mode and maximized IDE desktop view.
  - **Font Zoom**: Dynamically scale editor text using **`Ctrl + +`**, **`Ctrl + -`**, **`Ctrl + 0`** (reset 100%), or **`Ctrl + MouseWheel`**.
  - Real-time zoom percentage shown in the status bar.

* **📜 Dual Scrollbars & Margin Protection**:
  - Top-to-bottom and left-to-right scrollbars.
  - Padded container boundaries guarantee typed code never scrolls under or clips into the top title bar.

* **🖥️ Authentic DOS User Screen**:
  - Running code (**`Ctrl+F9`**) launches an authentic DOS console titled `Turbo C++ User Screen - <PROGRAM.EXE>`.
  - Supports interactive user input (`scanf`, `cin`, `getch`) and halts cleanly with `[ Program finished. Press any key to return to Turbo C++... ]`.
  - Re-inspect user screen anytime with **`Alt + F5`**.

* **💾 Portable & USB Ready**:
  - Can be run directly from any USB drive with zero installation required, or installed with one click to `C:\TURBOC3`.

---

## ⌨️ Keyboard Shortcuts Reference

| Shortcut | Action | Description |
| :--- | :--- | :--- |
| **`Alt + F9`** | Compile | Compiles current editor code to `.EXE` in milliseconds |
| **`Ctrl + F9`** | Run | Compiles and opens the authentic DOS User Screen |
| **`Alt + F5`** | User Screen | View output of the last executed program |
| **`F5`** | Zoom Window | Maximize / restore editor window inside the IDE |
| **`Ctrl + +`** | Zoom In Font | Increase editor font size |
| **`Ctrl + -`** | Zoom Out Font | Decrease editor font size |
| **`Ctrl + 0`** | Reset Zoom | Reset font size to default 100% |
| **`Ctrl + MouseWheel`** | Dynamic Zoom | Smooth zoom in / out with mouse wheel |
| **`F2`** | Save | Save current file directly |
| **`F3`** | Open | Open existing C/C++ source file |
| **`Ctrl + C`** / **`Ctrl + Ins`** | Copy | Copy selection to Windows clipboard |
| **`Ctrl + V`** / **`Shift + Ins`** | Paste | Paste from Windows clipboard without distortion |
| **`Alt + Enter`** | Fullscreen | Toggle application fullscreen window |
| **`Alt + X`** | Exit | Exit Turbo C++ IDE |

---

## 🚀 How to Run & Install

### Option 1: Direct Run (Standalone)
Double-click **`Turbo C++.exe`** or **`START_TURBO_CPP.bat`**.

### Option 2: Automated Installation (Creates Desktop Shortcuts)
Right-click **`INSTALL.bat`** and run. It will verify components and automatically create `Turbo C++` shortcuts on your Desktop and Start Menu.

### Option 3: Portable USB Drive Mode
Copy the `TURBOC3` folder to any USB drive and use:
* `INSTALL_TURBO_CPP.bat` to install onto any target PC.
* `RUN_PORTABLE_FROM_USB.bat` to run directly from the USB drive without installing to `C:\`.

---

## 📁 Repository Structure

```
TURBOC3/
├── Turbo C++.exe                # Main Rebuilt Turbo C++ application
├── Turbo C++ Rebuilt.exe        # Standalone executable
├── START_TURBO_CPP.bat          # Instant one-click launcher
├── INSTALL.bat                  # Automated installer & shortcut creator
├── CreateShortcuts.vbs          # Windows Shell shortcut automation script
├── Nouveau_IBM.ttf              # Authentic IBM VGA 8x16 DOS TrueType font
├── tc_icon.ico                  # Authentic Turbo C++ application icon
├── Compiler/                    # High-speed modern C compiler (TCC)
│   └── tcc/                     # TCC win64 binaries, libraries & enhanced conio.h
├── BIN/                         # Original Borland Turbo C++ 3.0 tools & utilities
├── INCLUDE/                     # Standard Turbo C header files
├── LIB/                         # Standard Turbo C library files
├── BGI/                         # Borland Graphics Interface drivers & fonts
├── DOSBox-X/                    # Embedded portable DOS emulation engine
├── Projects/                    # Default workspace for user C/C++ programs
└── TurboCPP_Rebuilt_Source/     # Full C# WinForms source code for the rebuilt IDE
```

---

## 📄 License

This rebuilt edition is provided for educational and retro-computing development. Original Borland Turbo C++ assets remain the property of their respective copyright holders.
