using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TurboCPP_Rebuilt
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(args));
        }
    }

    public class CompilerMessage
    {
        public string Type { get; set; }
        public string File { get; set; }
        public int Line { get; set; }
        public string Message { get; set; }
    }

    public class BorlandMenuRenderer : ToolStripRenderer
    {
        private readonly Color colGray = Color.FromArgb(168, 168, 168);
        private readonly Color colTeal = Color.FromArgb(0, 168, 168);
        private readonly Color colGreen = Color.FromArgb(0, 168, 0);
        private readonly Color colRed = Color.FromArgb(168, 0, 0);
        private readonly Color colBlack = Color.FromArgb(0, 0, 0);
        private readonly Color colWhite = Color.FromArgb(255, 255, 255);
        private readonly Color colYellow = Color.FromArgb(255, 255, 85);
        private readonly Color colBorder = Color.FromArgb(0, 0, 0);

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip is StatusStrip)
            {
                using (SolidBrush b = new SolidBrush(colTeal))
                {
                    e.Graphics.FillRectangle(b, e.AffectedBounds);
                }
            }
            else
            {
                using (SolidBrush b = new SolidBrush(colGray))
                {
                    e.Graphics.FillRectangle(b, e.AffectedBounds);
                }
            }
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            Rectangle rc = new Rectangle(Point.Empty, e.Item.Size);
            if (e.Item.Selected || e.Item.Pressed)
            {
                using (SolidBrush b = new SolidBrush(colGreen))
                {
                    e.Graphics.FillRectangle(b, rc);
                }
            }
            else
            {
                using (SolidBrush b = new SolidBrush(colGray))
                {
                    e.Graphics.FillRectangle(b, rc);
                }
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            string text = e.Text;
            if (string.IsNullOrEmpty(text)) return;

            bool isStatus = e.ToolStrip is StatusStrip;
            bool isSel = e.Item.Selected || e.Item.Pressed;

            Color normalColor = isSel ? colWhite : colBlack;
            Color hotkeyColor = isSel ? colYellow : (isStatus ? colWhite : colRed);

            int ampIdx = text.IndexOf('&');
            if (ampIdx >= 0 && ampIdx < text.Length - 1)
            {
                string before = text.Substring(0, ampIdx);
                string hotkey = text.Substring(ampIdx + 1, 1);
                string after = text.Substring(ampIdx + 2);

                Font f = e.Item.Font;
                int x = e.TextRectangle.Left;
                int y = e.TextRectangle.Top + (e.TextRectangle.Height - f.Height) / 2;

                if (before.Length > 0)
                {
                    TextRenderer.DrawText(e.Graphics, before, f, new Point(x, y), normalColor, TextFormatFlags.NoPadding);
                    x += TextRenderer.MeasureText(e.Graphics, before, f, Size.Empty, TextFormatFlags.NoPadding).Width;
                }
                TextRenderer.DrawText(e.Graphics, hotkey, f, new Point(x, y), hotkeyColor, TextFormatFlags.NoPadding);
                x += TextRenderer.MeasureText(e.Graphics, hotkey, f, Size.Empty, TextFormatFlags.NoPadding).Width;

                if (after.Length > 0)
                {
                    TextRenderer.DrawText(e.Graphics, after, f, new Point(x, y), normalColor, TextFormatFlags.NoPadding);
                }
            }
            else
            {
                TextRenderer.DrawText(e.Graphics, text, e.Item.Font, e.TextRectangle, normalColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            if (e.ToolStrip is ToolStripDropDown)
            {
                using (Pen p = new Pen(colBorder, 1))
                {
                    e.Graphics.DrawRectangle(p, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
                }
            }
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            using (Pen p = new Pen(Color.FromArgb(100, 100, 100), 1))
            {
                e.Graphics.DrawLine(p, 4, y, e.Item.Width - 8, y);
            }
        }
    }

    public class MainForm : Form
    {
        [DllImport("user32.dll")]
        public static extern IntPtr SendMessage(IntPtr hWnd, int wMsg, int wParam, int lParam);

        [DllImport("user32.dll", EntryPoint = "SendMessageA")]
        public static extern int SendMessage(IntPtr hWnd, int wMsg, int wParam, ref Point lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern uint GetShortPathName(string lpszLongPath, StringBuilder lpszShortPath, uint cchBuffer);

        private const int WM_SETREDRAW = 0x000B;
        private const int EM_GETSCROLLPOS = 0x04DD;
        private const int EM_SETSCROLLPOS = 0x04DE;

        // Classic EGA Palette (Authentic Turbo C++ 3.0 colors)
        private readonly Color ColBorlandBlue = Color.FromArgb(0, 0, 168);          // #0000A8 Navy Blue (Editor Background)
        private readonly Color ColBorlandCyan = Color.FromArgb(0, 168, 168);        // #00A8A8 Cyan (Borders, coordinates)
        private readonly Color ColBorlandBrightCyan = Color.FromArgb(85, 255, 255);  // #55FFFF Bright Cyan (Comments, Library functions)
        private readonly Color ColBorlandYellow = Color.FromArgb(255, 255, 85);     // #FFFF55 Bright Yellow (Default text, variables)
        private readonly Color ColBorlandGreen = Color.FromArgb(0, 168, 0);         // #00A800 Green
        private readonly Color ColBorlandBrightGreen = Color.FromArgb(85, 255, 85); // #55FF55 Light Green (Preprocessor directives)
        private readonly Color ColBorlandWhite = Color.FromArgb(255, 255, 255);     // #FFFFFF Bright White (Keywords)
        private readonly Color ColBorlandRed = Color.FromArgb(255, 85, 85);         // #FF5555 Light Red (Strings & character literals)
        private readonly Color ColBorlandMagenta = Color.FromArgb(255, 85, 255);    // #FF55FF Light Magenta (Numbers & numeric literals)
        private readonly Color ColBorlandGray = Color.FromArgb(168, 168, 168);      // #A8A8A8 Light Gray
        private readonly Color ColBorlandDarkGray = Color.FromArgb(84, 84, 84);     // #545454
        private readonly Color ColBorlandShadow = Color.FromArgb(0, 0, 0);           // Black Shadow

        // Main Controls
        private MenuStrip menuStrip;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel lblZoomStatus;
        private Panel pnlDesktop;
        private Panel pnlWindowFrame;
        private Panel pnlWindowHeader;
        private Panel pnlWindowLeft;
        private Panel pnlWindowRight;
        private Panel pnlWindowBottom;
        private Panel pnlEditorContainer;
        private RichTextBox rtbEditor;
        private ContextMenuStrip contextMenuEditor;

        // Font Engine
        private PrivateFontCollection pfc = new PrivateFontCollection();
        private FontFamily customFontFamily = null;
        private string activeFontName = "Nouveau IBM";
        private const float DEFAULT_FONT_SIZE = 13.5f;
        private float currentFontSize = DEFAULT_FONT_SIZE;

        // Compiling Modal Dialog
        private Panel pnlCompilingDialog;
        private Label lblCompilingTitle;
        private Label lblCompilingMainFile;
        private Label lblCompilingTarget;
        private Label lblCompilingLines;
        private Label lblCompilingWarnings;
        private Label lblCompilingErrors;
        private Label lblCompilingStatus;

        // Message Window (Compiler Errors)
        private Panel pnlMessageWindow;
        private ListBox lbMessages;

        // State & Engine
        private string currentFilePath = "";
        private bool isModified = false;
        private int currentWindowIndex = 1;
        private string turboRootDir;
        private string fastCompilerExe;
        private string dosboxExe;
        private string lastRunExe = "";
        private bool useClassicDosCompiler = false;
        private bool isWindowZoomed = false;
        private bool isHighlighting = false;
        private Timer highlightDebounceTimer;

        public MainForm(string[] args)
        {
            InitializePaths();
            InitializeFonts();
            InitializeComponent();
            SetupShortcuts();

            string fileToOpen = null;
            if (args != null && args.Length > 0 && File.Exists(args[0]))
            {
                fileToOpen = args[0];
            }
            else
            {
                string prjDir = Path.Combine(turboRootDir, "Projects");
                string lastFile = Path.Combine(prjDir, "last_session.txt");
                if (File.Exists(lastFile))
                {
                    try
                    {
                        string savedPath = File.ReadAllText(lastFile).Trim();
                        if (File.Exists(savedPath)) fileToOpen = savedPath;
                    }
                    catch { }
                }

                if (string.IsNullOrEmpty(fileToOpen))
                {
                    string jbn = Path.Combine(prjDir, "jbn.c");
                    string noname = Path.Combine(prjDir, "NONAME00.C");
                    if (File.Exists(jbn)) fileToOpen = jbn;
                    else if (File.Exists(noname)) fileToOpen = noname;
                }
            }

            if (!string.IsNullOrEmpty(fileToOpen) && File.Exists(fileToOpen))
            {
                OpenFile(fileToOpen);
            }
            else
            {
                NewFile();
            }
        }

        private void InitializePaths()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
            if (Directory.Exists(Path.Combine(baseDir, "Compiler")) || Directory.Exists(Path.Combine(baseDir, "BIN")))
            {
                turboRootDir = baseDir;
            }
            else if (Directory.Exists(@"C:\TURBOC3\BIN")) turboRootDir = @"C:\TURBOC3";
            else if (Directory.Exists(@"F:\TURBOC3\BIN")) turboRootDir = @"F:\TURBOC3";
            else turboRootDir = baseDir;

            // Fast Modern C Compiler (TCC for Windows)
            fastCompilerExe = Path.Combine(turboRootDir, @"Compiler\tcc\tcc.exe");
            if (!File.Exists(fastCompilerExe)) fastCompilerExe = Path.Combine(baseDir, @"Compiler\tcc\tcc.exe");
            if (!File.Exists(fastCompilerExe)) fastCompilerExe = Path.Combine(@"C:\TURBOC3", @"Compiler\tcc\tcc.exe");
            if (!File.Exists(fastCompilerExe)) fastCompilerExe = Path.Combine(@"F:\TURBOC3", @"Compiler\tcc\tcc.exe");

            // DOSBox-X for classic 16-bit DOS fallback
            dosboxExe = Path.Combine(turboRootDir, @"DOSBox-X\dosbox-x.exe");
            if (!File.Exists(dosboxExe)) dosboxExe = Path.Combine(baseDir, @"DOSBox-X\dosbox-x.exe");
            if (!File.Exists(dosboxExe)) dosboxExe = Path.Combine(@"C:\TURBOC3", @"DOSBox-X\dosbox-x.exe");
            if (!File.Exists(dosboxExe)) dosboxExe = Path.Combine(@"F:\TURBOC3", @"DOSBox-X\dosbox-x.exe");
        }

        private void InitializeFonts()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
            string[] fontCandidates = new string[]
            {
                Path.Combine(baseDir, "Nouveau_IBM.ttf"),
                Path.Combine(baseDir, @"Fonts\Nouveau_IBM.ttf"),
                Path.Combine(baseDir, @"DOSBox-X\Nouveau_IBM.ttf"),
                Path.Combine(turboRootDir, "Nouveau_IBM.ttf"),
                Path.Combine(turboRootDir, @"Fonts\Nouveau_IBM.ttf"),
                Path.Combine(turboRootDir, @"DOSBox-X\Nouveau_IBM.ttf"),
                @"C:\TURBOC3\Nouveau_IBM.ttf",
                @"F:\TURBOC3\Nouveau_IBM.ttf"
            };

            foreach (string path in fontCandidates)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        pfc.AddFontFile(path);
                        if (pfc.Families.Length > 0)
                        {
                            customFontFamily = pfc.Families[0];
                            activeFontName = customFontFamily.Name;
                            break;
                        }
                    }
                    catch { }
                }
            }
        }

        public Font GetIdeFont(float size, FontStyle style = FontStyle.Regular)
        {
            if (customFontFamily != null && activeFontName == customFontFamily.Name)
            {
                try { return new Font(customFontFamily, size, style); } catch { }
            }
            try { return new Font(activeFontName, size, style); } catch { }
            return new Font("Consolas", size, style);
        }

        public void SetFont(string fontName)
        {
            activeFontName = fontName;
            ApplyAllFonts();
        }

        public void ApplyAllFonts()
        {
            if (rtbEditor != null)
            {
                rtbEditor.Font = GetIdeFont(currentFontSize, FontStyle.Regular);
            }
            if (menuStrip != null)
            {
                menuStrip.Font = GetIdeFont(10f, FontStyle.Bold);
            }
            if (statusStrip != null)
            {
                statusStrip.Font = GetIdeFont(9.5f, FontStyle.Regular);
            }
            if (contextMenuEditor != null)
            {
                contextMenuEditor.Font = GetIdeFont(10f, FontStyle.Regular);
            }
            if (lblCompilingTitle != null)
            {
                lblCompilingTitle.Font = GetIdeFont(11f, FontStyle.Bold);
            }
            if (lbMessages != null)
            {
                lbMessages.Font = GetIdeFont(10f, FontStyle.Regular);
            }
            if (pnlWindowHeader != null) pnlWindowHeader.Invalidate();
            if (pnlWindowBottom != null) pnlWindowBottom.Invalidate();
            UpdateZoomStatus();
            if (rtbEditor != null) HighlightSyntax(rtbEditor);
        }

        private void InitializeComponent()
        {
            this.Text = "Turbo C++ 3.0";
            this.Size = new Size(1140, 760);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = ColBorlandBlue;
            this.DoubleBuffered = true;
            this.KeyPreview = true;

            string iconPath = Path.Combine(turboRootDir, "tc_icon.ico");
            if (!File.Exists(iconPath)) iconPath = Path.Combine(turboRootDir, @"Turbo C++\tc_icon.ico");
            if (File.Exists(iconPath))
            {
                try { this.Icon = new Icon(iconPath); } catch { }
            }

            // Menu Bar
            menuStrip = new MenuStrip();
            menuStrip.Renderer = new BorlandMenuRenderer();
            menuStrip.Font = GetIdeFont(10f, FontStyle.Bold);
            menuStrip.Dock = DockStyle.Top;
            CreateBorlandMenu();
            this.Controls.Add(menuStrip);

            // Status Bar
            statusStrip = new StatusStrip();
            statusStrip.Renderer = new BorlandMenuRenderer();
            statusStrip.Font = GetIdeFont(9.5f, FontStyle.Regular);
            statusStrip.Dock = DockStyle.Bottom;
            CreateBorlandStatusBar();
            this.Controls.Add(statusStrip);

            // Desktop Canvas (Area between Menu and Status)
            pnlDesktop = new Panel();
            pnlDesktop.Dock = DockStyle.Fill;
            pnlDesktop.BackColor = ColBorlandBlue;
            pnlDesktop.Padding = new Padding(8, 6, 14, 10);
            pnlDesktop.Paint += (s, e) => DrawDesktopShadow(e.Graphics);

            // Window Frame Panel (Surrounds the Editor with double lines & shadow)
            pnlWindowFrame = new Panel();
            pnlWindowFrame.Dock = DockStyle.Fill;
            pnlWindowFrame.BackColor = ColBorlandBlue;
            pnlDesktop.Controls.Add(pnlWindowFrame);

            // Frame Header (Top border of the editor window)
            pnlWindowHeader = new Panel();
            pnlWindowHeader.Dock = DockStyle.Top;
            pnlWindowHeader.Height = 24;
            pnlWindowHeader.BackColor = ColBorlandBlue;
            pnlWindowHeader.Paint += PnlWindowHeader_Paint;
            pnlWindowHeader.MouseClick += PnlWindowHeader_MouseClick;
            pnlWindowHeader.DoubleClick += (s, e) => ToggleWindowZoom();

            // Frame Left Border (Double cyan vertical lines)
            pnlWindowLeft = new Panel();
            pnlWindowLeft.Dock = DockStyle.Left;
            pnlWindowLeft.Width = 8;
            pnlWindowLeft.BackColor = ColBorlandBlue;
            pnlWindowLeft.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(ColBorlandCyan, 1))
                {
                    e.Graphics.DrawLine(pen, 3, 0, 3, pnlWindowLeft.Height);
                    e.Graphics.DrawLine(pen, 5, 0, 5, pnlWindowLeft.Height);
                }
            };

            // Frame Right Border (Double cyan vertical lines)
            pnlWindowRight = new Panel();
            pnlWindowRight.Dock = DockStyle.Right;
            pnlWindowRight.Width = 8;
            pnlWindowRight.BackColor = ColBorlandBlue;
            pnlWindowRight.Paint += (s, e) =>
            {
                using (Pen pen = new Pen(ColBorlandCyan, 1))
                {
                    e.Graphics.DrawLine(pen, 2, 0, 2, pnlWindowRight.Height);
                    e.Graphics.DrawLine(pen, 4, 0, 4, pnlWindowRight.Height);
                }
            };

            // Frame Bottom Border (Double cyan lines & coordinates)
            pnlWindowBottom = new Panel();
            pnlWindowBottom.Dock = DockStyle.Bottom;
            pnlWindowBottom.Height = 22;
            pnlWindowBottom.BackColor = ColBorlandBlue;
            pnlWindowBottom.Paint += PnlWindowBottom_Paint;

            // Editor Container Panel (Fixes margins and guarantees code NEVER goes under top bar)
            pnlEditorContainer = new Panel();
            pnlEditorContainer.Dock = DockStyle.Fill;
            pnlEditorContainer.BackColor = ColBorlandBlue;
            pnlEditorContainer.Padding = new Padding(8, 6, 8, 4);

            // Editor Control (RichTextBox with full Top-to-Bottom and Right-to-Left ScrollBars)
            rtbEditor = new RichTextBox();
            rtbEditor.Dock = DockStyle.Fill;
            rtbEditor.BackColor = ColBorlandBlue;
            rtbEditor.ForeColor = ColBorlandYellow;
            rtbEditor.Font = GetIdeFont(currentFontSize, FontStyle.Regular);
            rtbEditor.BorderStyle = BorderStyle.None;
            rtbEditor.AcceptsTab = true;
            rtbEditor.WordWrap = false;
            rtbEditor.DetectUrls = false;
            rtbEditor.ScrollBars = RichTextBoxScrollBars.Both; // Scroll top-to-bottom and right-to-left!

            // Standard 4-character tab width
            rtbEditor.SelectionTabs = new int[] { 32, 64, 96, 128, 160, 192, 224, 256, 288, 320, 352, 384, 416, 448, 480 };

            CreateContextMenu();
            rtbEditor.ContextMenuStrip = contextMenuEditor;

            // Debounce timer for real-time smooth syntax highlighting
            highlightDebounceTimer = new Timer();
            highlightDebounceTimer.Interval = 200;
            highlightDebounceTimer.Tick += (s, e) =>
            {
                highlightDebounceTimer.Stop();
                HighlightSyntax(rtbEditor);
            };

            rtbEditor.SelectionChanged += (s, e) => pnlWindowBottom.Invalidate();
            rtbEditor.TextChanged += (s, e) =>
            {
                isModified = true;
                pnlWindowHeader.Invalidate();
                if (!isHighlighting)
                {
                    highlightDebounceTimer.Stop();
                    highlightDebounceTimer.Start();
                }
            };

            // Mouse wheel font zoom (Ctrl + MouseWheel)
            rtbEditor.MouseWheel += (s, e) =>
            {
                if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
                {
                    if (e.Delta > 0) ZoomIn();
                    else if (e.Delta < 0) ZoomOut();
                    ((HandledMouseEventArgs)e).Handled = true;
                }
            };

            // Setup Custom Key Handlers
            SetupEditorKeyHandlers();

            // Assemble editor container
            pnlEditorContainer.Controls.Add(rtbEditor);

            // Assemble Window Frame in strict docking order
            pnlWindowFrame.Controls.Add(pnlEditorContainer);
            pnlWindowFrame.Controls.Add(pnlWindowHeader);
            pnlWindowFrame.Controls.Add(pnlWindowBottom);
            pnlWindowFrame.Controls.Add(pnlWindowLeft);
            pnlWindowFrame.Controls.Add(pnlWindowRight);

            // Docking order enforcement: DockStyle.Fill control must be sent to back
            pnlEditorContainer.SendToBack();
            pnlWindowHeader.BringToFront();
            pnlWindowBottom.BringToFront();
            pnlWindowLeft.BringToFront();
            pnlWindowRight.BringToFront();

            // Message Window (Compiler Errors) - Hidden by default
            CreateMessageWindow();
            pnlDesktop.Controls.Add(pnlMessageWindow);

            // Compiling Popup Dialog - Hidden by default
            CreateCompilingDialog();
            pnlDesktop.Controls.Add(pnlCompilingDialog);

            this.Controls.Add(pnlDesktop);

            // Z-Order
            menuStrip.BringToFront();
            statusStrip.BringToFront();
            pnlDesktop.BringToFront();

            UpdateZoomStatus();
        }

        private void DrawDesktopShadow(Graphics g)
        {
            if (isWindowZoomed) return; // In zoomed mode, no desktop shadow
            Rectangle frameRect = pnlWindowFrame.Bounds;
            using (SolidBrush shadowBrush = new SolidBrush(Color.FromArgb(160, 0, 0, 0)))
            {
                g.FillRectangle(shadowBrush, frameRect.Right, frameRect.Top + 6, 8, frameRect.Height);
                g.FillRectangle(shadowBrush, frameRect.Left + 8, frameRect.Bottom, frameRect.Width, 8);
            }
        }

        private void PnlWindowHeader_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            int w = pnlWindowHeader.Width;
            int h = pnlWindowHeader.Height;

            // Double cyan lines across the top
            using (Pen pen = new Pen(ColBorlandCyan, 1))
            {
                g.DrawLine(pen, 4, 8, w - 4, 8);
                g.DrawLine(pen, 4, 10, w - 4, 10);
            }

            using (Font f = GetIdeFont(10.5f, FontStyle.Bold))
            {
                // Left close box [■]
                TextRenderer.DrawText(g, "[", f, new Point(8, 2), ColBorlandCyan);
                TextRenderer.DrawText(g, "■", f, new Point(16, 2), ColBorlandBrightGreen);
                TextRenderer.DrawText(g, "]", f, new Point(28, 2), ColBorlandCyan);

                // Title
                string mod = isModified ? "*" : "";
                string title = string.Format(" {0}{1} ", Path.GetFileName(currentFilePath).ToUpper(), mod);
                Size titleSize = TextRenderer.MeasureText(g, title, f);
                int titleX = (w - titleSize.Width) / 2;
                if (titleX < 45) titleX = 45;

                // Clear background behind title
                using (SolidBrush bg = new SolidBrush(ColBorlandBlue))
                {
                    g.FillRectangle(bg, titleX - 2, 0, titleSize.Width + 4, h);
                }
                TextRenderer.DrawText(g, title, f, new Point(titleX, 2), ColBorlandYellow);

                // Right icons: [↑]/[↕] [1]
                int rightX = w - 65;
                using (SolidBrush bg = new SolidBrush(ColBorlandBlue))
                {
                    g.FillRectangle(bg, rightX - 4, 0, 60, h);
                }
                string zoomIcon = isWindowZoomed ? "↕" : "↑";
                TextRenderer.DrawText(g, "[", f, new Point(rightX, 2), ColBorlandCyan);
                TextRenderer.DrawText(g, zoomIcon, f, new Point(rightX + 8, 2), ColBorlandBrightGreen);
                TextRenderer.DrawText(g, "]", f, new Point(rightX + 18, 2), ColBorlandCyan);

                TextRenderer.DrawText(g, "[", f, new Point(rightX + 28, 2), ColBorlandCyan);
                TextRenderer.DrawText(g, currentWindowIndex.ToString(), f, new Point(rightX + 36, 2), ColBorlandYellow);
                TextRenderer.DrawText(g, "]", f, new Point(rightX + 46, 2), ColBorlandCyan);
            }
        }

        private void PnlWindowHeader_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.X >= 6 && e.X <= 38)
            {
                CloseFile();
            }
            else if (e.X >= pnlWindowHeader.Width - 68 && e.X <= pnlWindowHeader.Width - 32)
            {
                ToggleWindowZoom();
            }
        }

        private void PnlWindowBottom_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            int w = pnlWindowBottom.Width;
            int h = pnlWindowBottom.Height;

            // Double cyan lines across the bottom
            using (Pen pen = new Pen(ColBorlandCyan, 1))
            {
                g.DrawLine(pen, 4, 10, w - 4, 10);
                g.DrawLine(pen, 4, 12, w - 4, 12);
            }

            using (Font f = GetIdeFont(9.5f, FontStyle.Bold))
            {
                // Coordinates: Line:Col
                int line = 1;
                int col = 1;
                if (rtbEditor != null)
                {
                    line = rtbEditor.GetLineFromCharIndex(rtbEditor.SelectionStart) + 1;
                    int firstChar = rtbEditor.GetFirstCharIndexOfCurrentLine();
                    col = rtbEditor.SelectionStart - firstChar + 1;
                }

                string coordText = string.Format(" {0}:{1} ", line, col);
                Size sz = TextRenderer.MeasureText(g, coordText, f);
                int coordX = w - sz.Width - 25;

                using (SolidBrush bg = new SolidBrush(ColBorlandBlue))
                {
                    g.FillRectangle(bg, coordX - 2, 0, sz.Width + 4, h);
                }
                TextRenderer.DrawText(g, coordText, f, new Point(coordX, 2), ColBorlandCyan);
            }
        }

        private ToolStripMenuItem AddItem(ToolStripDropDownItem parent, string text, EventHandler onClick, Keys shortcut = Keys.None)
        {
            var item = new ToolStripMenuItem(text, null, onClick);
            if (shortcut != Keys.None) item.ShortcutKeys = shortcut;
            parent.DropDownItems.Add(item);
            return item;
        }

        private void CreateBorlandMenu()
        {
            // File
            var mnuFile = new ToolStripMenuItem("&File");
            AddItem(mnuFile, "&New", (s, e) => NewFile(), Keys.Control | Keys.N);
            AddItem(mnuFile, "&Open...             F3", (s, e) => OpenFileDialogAction());
            AddItem(mnuFile, "&Save               F2", (s, e) => SaveCurrentFile());
            AddItem(mnuFile, "Save &as...", (s, e) => SaveAsFileDialogAction());
            AddItem(mnuFile, "Save a&ll", (s, e) => SaveCurrentFile());
            mnuFile.DropDownItems.Add(new ToolStripSeparator());
            AddItem(mnuFile, "Open &Projects dir", (s, e) => OpenProjectsFolder());
            AddItem(mnuFile, "&Close", (s, e) => CloseFile(), Keys.Control | Keys.W);
            mnuFile.DropDownItems.Add(new ToolStripSeparator());
            AddItem(mnuFile, "E&xit             Alt+X", (s, e) => this.Close());
            menuStrip.Items.Add(mnuFile);

            // Edit
            var mnuEdit = new ToolStripMenuItem("&Edit");
            AddItem(mnuEdit, "&Undo          Ctrl+Z", (s, e) => Undo());
            AddItem(mnuEdit, "&Redo          Ctrl+Y", (s, e) => Redo());
            mnuEdit.DropDownItems.Add(new ToolStripSeparator());
            AddItem(mnuEdit, "Cu&t         Shift+Del", (s, e) => CutSelection(), Keys.Control | Keys.X);
            AddItem(mnuEdit, "&Copy         Ctrl+Ins", (s, e) => CopySelection(), Keys.Control | Keys.C);
            AddItem(mnuEdit, "&Paste       Shift+Ins", (s, e) => PasteClipboard(), Keys.Control | Keys.V);
            AddItem(mnuEdit, "&Clear             Del", (s, e) => ClearSelection());
            mnuEdit.DropDownItems.Add(new ToolStripSeparator());
            AddItem(mnuEdit, "Select &all     Ctrl+A", (s, e) => SelectAll());
            menuStrip.Items.Add(mnuEdit);

            // Search
            var mnuSearch = new ToolStripMenuItem("&Search");
            AddItem(mnuSearch, "&Find...        Ctrl+F", (s, e) => ShowFindDialog());
            AddItem(mnuSearch, "&Replace...     Ctrl+H", (s, e) => ShowReplaceDialog());
            AddItem(mnuSearch, "&Search again", (s, e) => ShowFindDialog());
            AddItem(mnuSearch, "&Go to line...  Ctrl+G", (s, e) => ShowGoToLineDialog());
            menuStrip.Items.Add(mnuSearch);

            // Run
            var mnuRun = new ToolStripMenuItem("&Run");
            AddItem(mnuRun, "&Run           Ctrl+F9", (s, e) => RunProgram());
            AddItem(mnuRun, "&Program reset Ctrl+F2", (s, e) => { });
            AddItem(mnuRun, "Go to &cursor       F4", (s, e) => { });
            AddItem(mnuRun, "&Trace into         F7", (s, e) => { });
            AddItem(mnuRun, "&Step over          F8", (s, e) => { });
            AddItem(mnuRun, "&User screen    Alt+F5", (s, e) => ShowUserScreen());
            mnuRun.DropDownItems.Add(new ToolStripSeparator());
            AddItem(mnuRun, "Launch &Classic TC.EXE", (s, e) => LaunchClassicDosbox());
            menuStrip.Items.Add(mnuRun);

            // Compile
            var mnuCompile = new ToolStripMenuItem("&Compile");
            AddItem(mnuCompile, "&Compile        Alt+F9", (s, e) => CompileProgram());
            AddItem(mnuCompile, "&Make               F9", (s, e) => CompileProgram());
            AddItem(mnuCompile, "&Link", (s, e) => CompileProgram());
            AddItem(mnuCompile, "&Build all", (s, e) => CompileProgram());
            mnuCompile.DropDownItems.Add(new ToolStripSeparator());
            AddItem(mnuCompile, "&Toggle Message window", (s, e) => ToggleMessageWindow());
            menuStrip.Items.Add(mnuCompile);

            // Debug
            var mnuDebug = new ToolStripMenuItem("&Debug");
            AddItem(mnuDebug, "&Inspect...", (s, e) => { });
            AddItem(mnuDebug, "&Evaluate/modify...", (s, e) => { });
            AddItem(mnuDebug, "Call &stack...  Ctrl+F3", (s, e) => { });
            AddItem(mnuDebug, "&Watches", (s, e) => { });
            AddItem(mnuDebug, "&Toggle breakpoint Ctrl+F8", (s, e) => { });
            menuStrip.Items.Add(mnuDebug);

            // Project
            var mnuProject = new ToolStripMenuItem("&Project");
            AddItem(mnuProject, "&Open project...", (s, e) => OpenFileDialogAction());
            AddItem(mnuProject, "&Close project", (s, e) => { });
            AddItem(mnuProject, "&Add item...", (s, e) => { });
            AddItem(mnuProject, "&Delete item", (s, e) => { });
            menuStrip.Items.Add(mnuProject);

            // Options
            var mnuOptions = new ToolStripMenuItem("&Options");
            var itemTccMode = new ToolStripMenuItem("Compiler: &Fast Modern C (TCC) [Active]");
            var itemDosMode = new ToolStripMenuItem("Compiler: &Borland 16-bit (DOSBox)");

            itemTccMode.Click += (s, e) =>
            {
                useClassicDosCompiler = false;
                itemTccMode.Text = "Compiler: &Fast Modern C (TCC) [Active]";
                itemDosMode.Text = "Compiler: &Borland 16-bit (DOSBox)";
                MessageBox.Show("Switched to Modern Fast C Compiler (TCC).\nSupports C99/C11, conio.h, clrscr(), getch(), and instant compilation.", "Compiler Option", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            itemDosMode.Click += (s, e) =>
            {
                useClassicDosCompiler = true;
                itemTccMode.Text = "Compiler: &Fast Modern C (TCC)";
                itemDosMode.Text = "Compiler: &Borland 16-bit (DOSBox) [Active]";
                MessageBox.Show("Switched to Classic Borland 16-bit Compiler (TCC via DOSBox-X).", "Compiler Option", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };

            mnuOptions.DropDownItems.Add(itemTccMode);
            mnuOptions.DropDownItems.Add(itemDosMode);
            mnuOptions.DropDownItems.Add(new ToolStripSeparator());

            // Environment & Font
            var mnuEnv = new ToolStripMenuItem("&Environment");
            var mnuFont = new ToolStripMenuItem("&Font Style");
            AddItem(mnuFont, "Authentic IBM VGA DOS (Nouveau IBM)", (s, e) => SetFont("Nouveau IBM"));
            AddItem(mnuFont, "Consolas", (s, e) => SetFont("Consolas"));
            AddItem(mnuFont, "Lucida Console", (s, e) => SetFont("Lucida Console"));
            AddItem(mnuFont, "Courier New", (s, e) => SetFont("Courier New"));
            mnuEnv.DropDownItems.Add(mnuFont);

            mnuEnv.DropDownItems.Add(new ToolStripSeparator());
            AddItem(mnuEnv, "Zoom &In (Text)        Ctrl++", (s, e) => ZoomIn());
            AddItem(mnuEnv, "Zoom &Out (Text)       Ctrl+-", (s, e) => ZoomOut());
            AddItem(mnuEnv, "&Reset Zoom (100%)     Ctrl+0", (s, e) => ResetZoom());
            AddItem(mnuEnv, "&Zoom Window               F5", (s, e) => ToggleWindowZoom());
            mnuOptions.DropDownItems.Add(mnuEnv);

            mnuOptions.DropDownItems.Add(new ToolStripSeparator());
            AddItem(mnuOptions, "&Directories...", (s, e) => ShowDirectoriesDialog());
            menuStrip.Items.Add(mnuOptions);

            // Window
            var mnuWindow = new ToolStripMenuItem("&Window");
            AddItem(mnuWindow, "&Size/Move     Ctrl+F5", (s, e) => { });
            AddItem(mnuWindow, "&Zoom Window               F5", (s, e) => ToggleWindowZoom());
            AddItem(mnuWindow, "Zoom &In (Text)        Ctrl++", (s, e) => ZoomIn());
            AddItem(mnuWindow, "Zoom &Out (Text)       Ctrl+-", (s, e) => ZoomOut());
            AddItem(mnuWindow, "&Reset Zoom (100%)     Ctrl+0", (s, e) => ResetZoom());
            AddItem(mnuWindow, "&Fullscreen (App)   Alt+Enter", (s, e) => ToggleFormFullscreen());
            mnuWindow.DropDownItems.Add(new ToolStripSeparator());
            AddItem(mnuWindow, "&Next               F6", (s, e) => { });
            AddItem(mnuWindow, "&Close          Ctrl+W", (s, e) => CloseFile());
            AddItem(mnuWindow, "Close &all", (s, e) => { });
            AddItem(mnuWindow, "&Cascade", (s, e) => { });
            AddItem(mnuWindow, "&Tile", (s, e) => { });
            menuStrip.Items.Add(mnuWindow);

            // Help
            var mnuHelp = new ToolStripMenuItem("&Help");
            AddItem(mnuHelp, "&Contents", (s, e) => ShowHelpDialog());
            AddItem(mnuHelp, "&Index         Shift+F1", (s, e) => ShowHelpDialog());
            AddItem(mnuHelp, "&Topic search  Ctrl+F1", (s, e) => ShowHelpDialog());
            mnuHelp.DropDownItems.Add(new ToolStripSeparator());
            AddItem(mnuHelp, "&About...", (s, e) => ShowAboutDialog());
            menuStrip.Items.Add(mnuHelp);
        }

        private void CreateBorlandStatusBar()
        {
            var btnF1 = new ToolStripStatusLabel(" F1 Help ");
            btnF1.Click += (s, e) => ShowHelpDialog();

            var btnF2 = new ToolStripStatusLabel(" F2 Save ");
            btnF2.Click += (s, e) => SaveCurrentFile();

            var btnF3 = new ToolStripStatusLabel(" F3 Open ");
            btnF3.Click += (s, e) => OpenFileDialogAction();

            var btnAltF9 = new ToolStripStatusLabel(" Alt+F9 Compile ");
            btnAltF9.Click += (s, e) => CompileProgram();

            var btnF9 = new ToolStripStatusLabel(" F9 Make ");
            btnF9.Click += (s, e) => CompileProgram();

            var btnCtrlF9 = new ToolStripStatusLabel(" Ctrl+F9 Run ");
            btnCtrlF9.Click += (s, e) => RunProgram();

            var btnAltF5 = new ToolStripStatusLabel(" Alt+F5 User screen ");
            btnAltF5.Click += (s, e) => ShowUserScreen();

            var spacer = new ToolStripStatusLabel { Spring = true };

            lblZoomStatus = new ToolStripStatusLabel(" Zoom: 100% ");
            lblZoomStatus.Click += (s, e) => ResetZoom();
            lblZoomStatus.ToolTipText = "Click to reset zoom to 100%. Use Ctrl+Plus/Minus or Ctrl+MouseWheel to zoom text.";

            statusStrip.Items.AddRange(new ToolStripItem[] {
                btnF1, btnF2, btnF3, btnAltF9, btnF9, btnCtrlF9, btnAltF5, spacer, lblZoomStatus
            });
        }

        private void CreateContextMenu()
        {
            contextMenuEditor = new ContextMenuStrip();
            contextMenuEditor.Renderer = new BorlandMenuRenderer();
            contextMenuEditor.Font = GetIdeFont(10f, FontStyle.Regular);

            var miCut = new ToolStripMenuItem("Cu&t         (Ctrl+X)", null, (s, e) => CutSelection());
            var miCopy = new ToolStripMenuItem("&Copy        (Ctrl+C)", null, (s, e) => CopySelection());
            var miPaste = new ToolStripMenuItem("&Paste       (Ctrl+V)", null, (s, e) => PasteClipboard());
            var miSep = new ToolStripSeparator();
            var miZoomIn = new ToolStripMenuItem("Zoom &In     (Ctrl++)", null, (s, e) => ZoomIn());
            var miZoomOut = new ToolStripMenuItem("Zoom &Out    (Ctrl+-)", null, (s, e) => ZoomOut());
            var miResetZoom = new ToolStripMenuItem("&Reset Zoom  (Ctrl+0)", null, (s, e) => ResetZoom());
            var miSep2 = new ToolStripSeparator();
            var miSelAll = new ToolStripMenuItem("Select &All  (Ctrl+A)", null, (s, e) => SelectAll());

            contextMenuEditor.Items.AddRange(new ToolStripItem[] {
                miCut, miCopy, miPaste, miSep, miZoomIn, miZoomOut, miResetZoom, miSep2, miSelAll
            });
        }

        private void SetupShortcuts()
        {
            this.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.F2 && !e.Alt && !e.Control) { SaveCurrentFile(); e.Handled = true; }
                else if (e.KeyCode == Keys.F3 && !e.Alt && !e.Control) { OpenFileDialogAction(); e.Handled = true; }
                else if (e.KeyCode == Keys.F9 && e.Alt) { CompileProgram(); e.Handled = true; }
                else if (e.KeyCode == Keys.F9 && e.Control) { RunProgram(); e.Handled = true; }
                else if (e.KeyCode == Keys.F5 && e.Alt) { ShowUserScreen(); e.Handled = true; }
                else if (e.KeyCode == Keys.F5 && !e.Alt && !e.Control) { ToggleWindowZoom(); e.Handled = true; }
                else if (e.KeyCode == Keys.Enter && e.Alt) { ToggleFormFullscreen(); e.Handled = true; }
                else if (e.KeyCode == Keys.X && e.Alt) { this.Close(); e.Handled = true; }
                else if (e.KeyCode == Keys.Insert && e.Control) { CopySelection(); e.Handled = true; }
                else if (e.KeyCode == Keys.Insert && e.Shift) { PasteClipboard(); e.Handled = true; }
                else if (e.KeyCode == Keys.Delete && e.Shift) { CutSelection(); e.Handled = true; }
                else if (e.Control && (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add)) { ZoomIn(); e.Handled = true; }
                else if (e.Control && (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract)) { ZoomOut(); e.Handled = true; }
                else if (e.Control && (e.KeyCode == Keys.D0 || e.KeyCode == Keys.NumPad0)) { ResetZoom(); e.Handled = true; }
            };
        }

        private void SetupEditorKeyHandlers()
        {
            rtbEditor.KeyDown += (s, e) =>
            {
                // Native Copy
                if (e.Control && e.KeyCode == Keys.C)
                {
                    CopySelection();
                    e.Handled = true;
                }
                // Native Paste (Zero-error)
                else if (e.Control && e.KeyCode == Keys.V)
                {
                    PasteClipboard();
                    e.Handled = true;
                }
                // Native Cut
                else if (e.Control && e.KeyCode == Keys.X)
                {
                    CutSelection();
                    e.Handled = true;
                }
                // Zoom Shortcuts
                else if (e.Control && (e.KeyCode == Keys.Oemplus || e.KeyCode == Keys.Add))
                {
                    ZoomIn();
                    e.Handled = true;
                }
                else if (e.Control && (e.KeyCode == Keys.OemMinus || e.KeyCode == Keys.Subtract))
                {
                    ZoomOut();
                    e.Handled = true;
                }
                else if (e.Control && (e.KeyCode == Keys.D0 || e.KeyCode == Keys.NumPad0))
                {
                    ResetZoom();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.F5 && !e.Alt && !e.Control)
                {
                    ToggleWindowZoom();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Enter && e.Alt)
                {
                    ToggleFormFullscreen();
                    e.Handled = true;
                }
                // Smart Enter: keep line indentation
                else if (e.KeyCode == Keys.Enter && !e.Control && !e.Alt)
                {
                    int lineIndex = rtbEditor.GetLineFromCharIndex(rtbEditor.SelectionStart);
                    if (lineIndex >= 0 && lineIndex < rtbEditor.Lines.Length)
                    {
                        string currentLine = rtbEditor.Lines[lineIndex];
                        int indentCount = 0;
                        while (indentCount < currentLine.Length && (currentLine[indentCount] == ' ' || currentLine[indentCount] == '\t'))
                            indentCount++;
                        string indent = currentLine.Substring(0, indentCount);
                        rtbEditor.SelectedText = "\n" + indent;
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                    }
                }
                // Tab -> 4 spaces
                else if (e.KeyCode == Keys.Tab && !e.Control && !e.Alt)
                {
                    rtbEditor.SelectedText = "    ";
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
            };
        }

        #region Authentic Borland Compiling Dialog

        private void CreateCompilingDialog()
        {
            pnlCompilingDialog = new Panel();
            pnlCompilingDialog.Size = new Size(460, 250);
            pnlCompilingDialog.BackColor = ColBorlandBlue;
            pnlCompilingDialog.Visible = false;

            pnlDesktop.SizeChanged += (s, e) =>
            {
                pnlCompilingDialog.Location = new Point(
                    (pnlDesktop.Width - pnlCompilingDialog.Width) / 2,
                    (pnlDesktop.Height - pnlCompilingDialog.Height) / 2
                );
            };

            pnlCompilingDialog.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                int w = pnlCompilingDialog.Width;
                int h = pnlCompilingDialog.Height;

                // Cyan border
                using (Pen pen = new Pen(ColBorlandCyan, 2))
                {
                    g.DrawRectangle(pen, 2, 2, w - 4, h - 4);
                }

                // Inner frame
                using (Pen pen = new Pen(ColBorlandCyan, 1))
                {
                    g.DrawRectangle(pen, 8, 26, w - 16, h - 34);
                }
            };

            lblCompilingTitle = new Label();
            lblCompilingTitle.Text = " Compiling ";
            lblCompilingTitle.Font = GetIdeFont(11f, FontStyle.Bold);
            lblCompilingTitle.ForeColor = ColBorlandYellow;
            lblCompilingTitle.BackColor = ColBorlandBlue;
            lblCompilingTitle.AutoSize = true;
            lblCompilingTitle.Location = new Point(175, 4);
            pnlCompilingDialog.Controls.Add(lblCompilingTitle);

            lblCompilingMainFile = new Label { Location = new Point(30, 35), AutoSize = true, Font = GetIdeFont(10.5f), ForeColor = ColBorlandWhite };
            lblCompilingTarget = new Label { Location = new Point(30, 65), AutoSize = true, Font = GetIdeFont(10.5f), ForeColor = ColBorlandWhite };
            lblCompilingLines = new Label { Location = new Point(30, 105), AutoSize = true, Font = GetIdeFont(10.5f), ForeColor = ColBorlandWhite };
            lblCompilingWarnings = new Label { Location = new Point(30, 135), AutoSize = true, Font = GetIdeFont(10.5f), ForeColor = ColBorlandWhite };
            lblCompilingErrors = new Label { Location = new Point(30, 165), AutoSize = true, Font = GetIdeFont(10.5f), ForeColor = ColBorlandWhite };
            lblCompilingStatus = new Label { Location = new Point(90, 205), AutoSize = true, Font = GetIdeFont(11f, FontStyle.Bold), ForeColor = ColBorlandBrightGreen };

            pnlCompilingDialog.Controls.AddRange(new Control[] {
                lblCompilingMainFile, lblCompilingTarget, lblCompilingLines,
                lblCompilingWarnings, lblCompilingErrors, lblCompilingStatus
            });

            // Key press or click to dismiss
            pnlCompilingDialog.Click += (s, e) => pnlCompilingDialog.Visible = false;
            foreach (Control c in pnlCompilingDialog.Controls)
            {
                c.Click += (s, e) => pnlCompilingDialog.Visible = false;
            }
        }

        private void ShowCompilingPopup(string file, int totalLines, int warnings, int errors, bool success)
        {
            lblCompilingMainFile.Text = string.Format("Main file: {0}", file);
            lblCompilingTarget.Text = string.Format("Target:    {0}.EXE", Path.GetFileNameWithoutExtension(file));
            lblCompilingLines.Text = string.Format("Total lines: {0,-10} Compiling: Editor", totalLines);
            lblCompilingWarnings.Text = string.Format("Warnings:    {0}", warnings);
            lblCompilingErrors.Text = string.Format("Errors:      {0}", errors);

            if (success)
            {
                lblCompilingStatus.ForeColor = ColBorlandBrightGreen;
                lblCompilingStatus.Text = "Success : Press any key or click";
            }
            else
            {
                lblCompilingStatus.ForeColor = ColBorlandRed;
                lblCompilingStatus.Text = "Errors  : Press any key or click";
            }

            pnlCompilingDialog.Location = new Point(
                (pnlDesktop.Width - pnlCompilingDialog.Width) / 2,
                (pnlDesktop.Height - pnlCompilingDialog.Height) / 2
            );
            pnlCompilingDialog.Visible = true;
            pnlCompilingDialog.BringToFront();
            pnlCompilingDialog.Focus();
        }

        #endregion

        #region Authentic Borland Message Window

        private void CreateMessageWindow()
        {
            pnlMessageWindow = new Panel();
            pnlMessageWindow.Height = 150;
            pnlMessageWindow.Dock = DockStyle.Bottom;
            pnlMessageWindow.BackColor = ColBorlandBlue;
            pnlMessageWindow.Visible = false;

            pnlMessageWindow.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                int w = pnlMessageWindow.Width;
                int h = pnlMessageWindow.Height;

                using (Pen pen = new Pen(ColBorlandCyan, 2))
                {
                    g.DrawRectangle(pen, 2, 2, w - 4, h - 4);
                }

                using (Font f = GetIdeFont(10f, FontStyle.Bold))
                {
                    string title = " Message ";
                    Size sz = TextRenderer.MeasureText(g, title, f);
                    int titleX = (w - sz.Width) / 2;
                    using (SolidBrush bg = new SolidBrush(ColBorlandBlue))
                    {
                        g.FillRectangle(bg, titleX - 2, 0, sz.Width + 4, 18);
                    }
                    TextRenderer.DrawText(g, title, f, new Point(titleX, 2), ColBorlandYellow);
                }
            };

            lbMessages = new ListBox();
            lbMessages.Dock = DockStyle.Fill;
            lbMessages.BackColor = ColBorlandBlue;
            lbMessages.ForeColor = ColBorlandWhite;
            lbMessages.Font = GetIdeFont(10f);
            lbMessages.BorderStyle = BorderStyle.None;
            lbMessages.DoubleClick += LbMessages_DoubleClick;

            pnlMessageWindow.Controls.Add(lbMessages);
        }

        private void ToggleMessageWindow()
        {
            pnlMessageWindow.Visible = !pnlMessageWindow.Visible;
            if (pnlMessageWindow.Visible) pnlMessageWindow.BringToFront();
        }

        private void LbMessages_DoubleClick(object sender, EventArgs e)
        {
            if (lbMessages.SelectedItem == null) return;
            string selected = lbMessages.SelectedItem.ToString();

            // Match line number
            var match = Regex.Match(selected, @"\b(\d+):");
            if (match.Success)
            {
                int line;
                if (int.TryParse(match.Groups[1].Value, out line) && line > 0 && line <= rtbEditor.Lines.Length)
                {
                    int charIndex = rtbEditor.GetFirstCharIndexFromLine(line - 1);
                    if (charIndex >= 0)
                    {
                        rtbEditor.SelectionStart = charIndex;
                        rtbEditor.ScrollToCaret();
                        rtbEditor.Focus();
                    }
                }
            }
        }

        #endregion

        #region File Operations

        public void NewFile()
        {
            string prjDir = Path.Combine(turboRootDir, "Projects");
            if (!Directory.Exists(prjDir)) Directory.CreateDirectory(prjDir);

            // Find next available NONAMExx.C
            string fullPath = Path.Combine(prjDir, "NONAME00.C");
            int counter = 0;
            while (File.Exists(fullPath) && new FileInfo(fullPath).Length > 0 && counter < 99)
            {
                counter++;
                fullPath = Path.Combine(prjDir, string.Format("NONAME{0:D2}.C", counter));
            }

            string starter = "#include <stdio.h>\n#include <conio.h>\n\nint main() {\n    \n    return 0;\n}\n";
            if (!File.Exists(fullPath))
            {
                try { File.WriteAllText(fullPath, starter); } catch { }
            }

            OpenFile(fullPath);
        }

        public void OpenFile(string path)
        {
            currentFilePath = path;
            rtbEditor.Text = File.Exists(path) ? File.ReadAllText(path) : "";
            isModified = false;

            try
            {
                string prjDir = Path.Combine(turboRootDir, "Projects");
                File.WriteAllText(Path.Combine(prjDir, "last_session.txt"), path);
            }
            catch { }

            HighlightSyntax(rtbEditor);
            rtbEditor.SelectionStart = 0;
            rtbEditor.SelectionLength = 0;

            pnlWindowHeader.Invalidate();
            pnlWindowBottom.Invalidate();
        }

        public void CloseFile()
        {
            if (isModified)
            {
                var res = MessageBox.Show(string.Format("Save changes to {0} before closing?", Path.GetFileName(currentFilePath)), "Turbo C++", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (res == DialogResult.Cancel) return;
                if (res == DialogResult.Yes) SaveCurrentFile();
            }
            NewFile();
        }

        public void SaveCurrentFile()
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                string prjDir = Path.Combine(turboRootDir, "Projects");
                if (!Directory.Exists(prjDir)) Directory.CreateDirectory(prjDir);
                currentFilePath = Path.Combine(prjDir, "NONAME00.C");
            }

            try
            {
                File.WriteAllText(currentFilePath, rtbEditor.Text);
                isModified = false;
                pnlWindowHeader.Invalidate();
                HighlightSyntax(rtbEditor);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void SaveAsFileDialogAction()
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.InitialDirectory = Path.Combine(turboRootDir, "Projects");
                sfd.Filter = "C Files (*.c)|*.c|C++ Files (*.cpp)|*.cpp|All Files (*.*)|*.*";
                sfd.DefaultExt = "c";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    currentFilePath = sfd.FileName;
                    SaveCurrentFile();
                }
            }
        }

        public void OpenFileDialogAction()
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.InitialDirectory = Path.Combine(turboRootDir, "Projects");
                ofd.Filter = "C/C++ Files (*.c;*.cpp;*.h;*.hpp)|*.c;*.cpp;*.h;*.hpp|All Files (*.*)|*.*";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    OpenFile(ofd.FileName);
                }
            }
        }

        private void OpenProjectsFolder()
        {
            string prjDir = Path.Combine(turboRootDir, "Projects");
            if (!Directory.Exists(prjDir)) Directory.CreateDirectory(prjDir);
            Process.Start("explorer.exe", prjDir);
        }

        #endregion

        #region Clipboard Operations (Native & Zero Error)

        private void CopySelection()
        {
            if (rtbEditor.SelectionLength > 0)
            {
                Clipboard.SetText(rtbEditor.SelectedText);
            }
        }

        private void CutSelection()
        {
            if (rtbEditor.SelectionLength > 0)
            {
                Clipboard.SetText(rtbEditor.SelectedText);
                rtbEditor.SelectedText = "";
            }
        }

        private void PasteClipboard()
        {
            if (!Clipboard.ContainsText()) return;
            string text = Clipboard.GetText();
            if (string.IsNullOrEmpty(text)) return;

            // Zero-error normalization:
            // Strip nulls, normalize \r\n, preserve every single symbol and character exactly
            text = text.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\0", "");

            rtbEditor.SelectedText = text;
            isModified = true;
            pnlWindowHeader.Invalidate();
            HighlightSyntax(rtbEditor);
        }

        private void Undo()
        {
            if (rtbEditor.CanUndo) rtbEditor.Undo();
        }

        private void Redo()
        {
            if (rtbEditor.CanRedo) rtbEditor.Redo();
        }

        private void ClearSelection()
        {
            if (rtbEditor.SelectionLength > 0) rtbEditor.SelectedText = "";
        }

        private void SelectAll()
        {
            rtbEditor.SelectAll();
        }

        #endregion

        #region Syntax Highlighting Engine (Authentic Turbo C++ Palette)

        private static readonly Regex RegexKeywords = new Regex(
            @"\b(auto|break|case|char|const|continue|default|do|double|else|enum|extern|float|for|goto|if|int|long|register|return|short|signed|sizeof|static|struct|switch|typedef|union|unsigned|void|volatile|while|class|public|private|protected|virtual|friend|inline|operator|new|delete|this|template|try|catch|throw|namespace|using|bool|true|false|near|far|huge|pascal|cdecl|interrupt|asm)\b",
            RegexOptions.Compiled);

        private static readonly Regex RegexSpecialFunctions = new Regex(
            @"\b(clrscr|getch|getche|gotoxy|delay|wherex|wherey|textcolor|textbackground|cprintf|cscanf|kbhit|sound|nosound|initgraph|closegraph|circle|line|rectangle|outtextxy|setcolor|setfillstyle|floodfill|main|printf|scanf|cin|cout|endl)\b",
            RegexOptions.Compiled);

        private static readonly Regex RegexPreprocessor = new Regex(
            @"^\s*#(include|define|undef|ifdef|ifndef|if|else|elif|endif|error|pragma)\b[^\n]*",
            RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex RegexStrings = new Regex(
            @"""(\\.|[^""\\\n])*""|'(\\.|[^'\\\n])*'",
            RegexOptions.Compiled);

        private static readonly Regex RegexComments = new Regex(
            @"//[^\n]*|/\*[\s\S]*?\*/",
            RegexOptions.Compiled);

        private static readonly Regex RegexNumbers = new Regex(
            @"\b(0x[0-9a-fA-F]+|0b[01]+|\d+(\.\d+)?(f|u|l|ul|ll)?)\b",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private void HighlightSyntax(RichTextBox rtb)
        {
            if (rtb == null || isHighlighting || rtb.TextLength == 0) return;
            isHighlighting = true;

            int originalSelectionStart = rtb.SelectionStart;
            int originalSelectionLength = rtb.SelectionLength;

            Point scrollPos = Point.Empty;
            SendMessage(rtb.Handle, EM_GETSCROLLPOS, 0, ref scrollPos);
            SendMessage(rtb.Handle, WM_SETREDRAW, 0, 0);

            try
            {
                string text = rtb.Text;

                // 1. Reset all to iconic Borland Yellow (Default code, variables, operators)
                rtb.SelectAll();
                rtb.SelectionColor = ColBorlandYellow;

                // 2. Numbers & Numeric Literals -> Light Magenta (#FF55FF)
                foreach (Match m in RegexNumbers.Matches(text))
                {
                    rtb.Select(m.Index, m.Length);
                    rtb.SelectionColor = ColBorlandMagenta;
                }

                // 3. Keywords -> Bright White (#FFFFFF)
                foreach (Match m in RegexKeywords.Matches(text))
                {
                    rtb.Select(m.Index, m.Length);
                    rtb.SelectionColor = ColBorlandWhite;
                }

                // 4. Special conio/DOS/STDIO functions -> Bright Cyan (#55FFFF)
                foreach (Match m in RegexSpecialFunctions.Matches(text))
                {
                    rtb.Select(m.Index, m.Length);
                    rtb.SelectionColor = ColBorlandBrightCyan;
                }

                // 5. Preprocessor Directives & Headers -> Light Green (#55FF55)
                foreach (Match m in RegexPreprocessor.Matches(text))
                {
                    rtb.Select(m.Index, m.Length);
                    rtb.SelectionColor = ColBorlandBrightGreen;
                }

                // 6. Strings and Character Literals -> Light Red (#FF5555)
                foreach (Match m in RegexStrings.Matches(text))
                {
                    rtb.Select(m.Index, m.Length);
                    rtb.SelectionColor = ColBorlandRed;
                }

                // 7. Comments -> Light Cyan (#55FFFF)
                foreach (Match m in RegexComments.Matches(text))
                {
                    rtb.Select(m.Index, m.Length);
                    rtb.SelectionColor = ColBorlandBrightCyan;
                }
            }
            catch { }
            finally
            {
                rtb.Select(originalSelectionStart, originalSelectionLength);
                SendMessage(rtb.Handle, EM_SETSCROLLPOS, 0, ref scrollPos);
                SendMessage(rtb.Handle, WM_SETREDRAW, 1, 0);
                rtb.Invalidate();
                isHighlighting = false;
            }
        }

        #endregion

        #region Compiler & Runner Engine

        public bool CompileProgram()
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                string defPrjDir = Path.Combine(turboRootDir, "Projects");
                if (!Directory.Exists(defPrjDir)) Directory.CreateDirectory(defPrjDir);
                currentFilePath = Path.Combine(defPrjDir, "NONAME00.C");
            }

            // Unconditionally save the exact current editor text to disk
            try
            {
                File.WriteAllText(currentFilePath, rtbEditor.Text);
                isModified = false;
                pnlWindowHeader.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not save source file before compiling: " + ex.Message, "Compile Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            string srcPath = currentFilePath;
            string fileName = Path.GetFileName(srcPath);
            string baseName = Path.GetFileNameWithoutExtension(srcPath);
            string prjDir = Path.GetDirectoryName(srcPath);
            if (string.IsNullOrEmpty(prjDir)) prjDir = Path.Combine(turboRootDir, "Projects");
            if (!Directory.Exists(prjDir)) Directory.CreateDirectory(prjDir);

            string targetExe = Path.Combine(prjDir, baseName + ".EXE");
            if (File.Exists(targetExe))
            {
                try { File.Delete(targetExe); } catch { }
            }

            lbMessages.Items.Clear();
            int warningCount = 0;
            int errorCount = 0;
            var messages = new List<CompilerMessage>();

            Cursor.Current = Cursors.WaitCursor;

            // Fast Modern C Compiler (TCC)
            if (!useClassicDosCompiler && File.Exists(fastCompilerExe))
            {
                string tccDir = Path.GetDirectoryName(fastCompilerExe);
                string incDir = Path.Combine(tccDir, "include");
                string libDir = Path.Combine(tccDir, "lib");

                var psi = new ProcessStartInfo
                {
                    FileName = fastCompilerExe,
                    Arguments = string.Format("-I\"{0}\" -L\"{1}\" \"{2}\" -o \"{3}\"", incDir, libDir, srcPath, targetExe),
                    WorkingDirectory = prjDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                string compilerOutput = "";
                try
                {
                    using (var proc = Process.Start(psi))
                    {
                        compilerOutput = proc.StandardError.ReadToEnd() + "\n" + proc.StandardOutput.ReadToEnd();
                        proc.WaitForExit(10000);
                    }
                }
                catch (Exception ex)
                {
                    compilerOutput = "error: " + ex.Message;
                }

                messages = ParseTccCompilerOutput(compilerOutput, fileName);
            }
            else
            {
                // Fallback: Borland 16-bit TCC in DOSBox-X with -P flag
                messages = CompileWithBorlandDos(srcPath, prjDir, baseName, fileName, targetExe);
            }

            Cursor.Current = Cursors.Default;

            foreach (var m in messages)
            {
                string entry = string.Format("{0} {1} {2}: {3}", m.Type, m.File, m.Line, m.Message);
                lbMessages.Items.Add(entry);
                if (m.Type.Equals("Error", StringComparison.OrdinalIgnoreCase)) errorCount++;
                else warningCount++;
            }

            bool success = File.Exists(targetExe) && errorCount == 0;

            if (success)
            {
                lastRunExe = targetExe;
                pnlMessageWindow.Visible = false;
            }
            else
            {
                if (messages.Count == 0 && !File.Exists(targetExe))
                {
                    lbMessages.Items.Add("Error: Compilation failed. No output executable produced.");
                    errorCount = 1;
                }
                pnlMessageWindow.Visible = true;
                pnlMessageWindow.BringToFront();
            }

            ShowCompilingPopup(fileName, rtbEditor.Lines.Length, warningCount, errorCount, success);
            return success;
        }

        private List<CompilerMessage> ParseTccCompilerOutput(string output, string defaultFileName)
        {
            var list = new List<CompilerMessage>();
            if (string.IsNullOrEmpty(output)) return list;

            // Pattern: path/file.c:line: error/warning: message
            var regex = new Regex(@"^([^:\r\n]+):(\d+):\s*(error|warning):\s*(.+)$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            foreach (Match m in regex.Matches(output))
            {
                int line = 1;
                int.TryParse(m.Groups[2].Value, out line);
                string type = m.Groups[3].Value.ToLower().Contains("error") ? "Error" : "Warning";
                list.Add(new CompilerMessage
                {
                    Type = type,
                    File = Path.GetFileName(m.Groups[1].Value),
                    Line = line,
                    Message = m.Groups[4].Value.Trim()
                });
            }

            if (list.Count == 0)
            {
                var regexGen = new Regex(@"^(error|warning):\s*(.+)$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                foreach (Match m in regexGen.Matches(output))
                {
                    string type = m.Groups[1].Value.ToLower().Contains("error") ? "Error" : "Warning";
                    list.Add(new CompilerMessage
                    {
                        Type = type,
                        File = defaultFileName,
                        Line = 1,
                        Message = m.Groups[2].Value.Trim()
                    });
                }
            }

            return list;
        }

        private List<CompilerMessage> CompileWithBorlandDos(string srcPath, string prjDir, string baseName, string fileName, string targetExe)
        {
            string buildFileName = fileName;
            bool usedTempBuild = false;
            if (baseName.Length > 8 || fileName.Contains(" "))
            {
                buildFileName = "TC_BLD" + Path.GetExtension(srcPath);
                string tempSrc = Path.Combine(prjDir, buildFileName);
                try { File.Copy(srcPath, tempSrc, true); } catch { }
                usedTempBuild = true;
            }

            string buildBase = Path.GetFileNameWithoutExtension(buildFileName);
            string buildExe = Path.Combine(prjDir, buildBase + ".EXE");
            string logFile = Path.Combine(prjDir, "COMPILE.LOG");
            if (File.Exists(logFile)) try { File.Delete(logFile); } catch { }

            string compileConf = Path.Combine(prjDir, "COMPILE_RUN.CONF");
            string confContent = string.Format(
                "[cpu]\r\n" +
                "cputype = 386\r\n" +
                "cycles = max\r\n" +
                "\r\n" +
                "[dos]\r\n" +
                "hard drive data rate limit = 0\r\n" +
                "floppy drive data rate limit = 0\r\n" +
                "\r\n" +
                "[dosbox]\r\n" +
                "memsize = 16\r\n" +
                "\r\n" +
                "[autoexec]\r\n" +
                "@echo off\r\n" +
                "mount C \"{0}\"\r\n" +
                "c:\r\n" +
                "path C:\\BIN;Z:\\\r\n" +
                "cd \\BIN\r\n" +
                "tcc.exe -P -IC:\\INCLUDE -LC:\\LIB -eC:\\Projects\\{1}.EXE C:\\Projects\\{2} > C:\\Projects\\COMPILE.LOG\r\n" +
                "exit\r\n",
                turboRootDir,
                buildBase,
                buildFileName
            );

            File.WriteAllText(compileConf, confContent);

            var psi = new ProcessStartInfo
            {
                FileName = dosboxExe,
                Arguments = "-conf \"" + compileConf + "\" -noconsole -exit",
                WorkingDirectory = Path.GetDirectoryName(dosboxExe),
                UseShellExecute = false,
                CreateNoWindow = true
            };

            try
            {
                using (var proc = Process.Start(psi))
                {
                    proc.WaitForExit(15000);
                }
            }
            catch { }

            try { if (File.Exists(compileConf)) File.Delete(compileConf); } catch { }

            if (usedTempBuild && File.Exists(buildExe))
            {
                try { File.Copy(buildExe, targetExe, true); } catch { }
            }

            string logText = File.Exists(logFile) ? File.ReadAllText(logFile) : "";
            return ParseBorlandCompilerLog(logText);
        }

        private List<CompilerMessage> ParseBorlandCompilerLog(string log)
        {
            var list = new List<CompilerMessage>();
            if (string.IsNullOrEmpty(log)) return list;

            var regexWithLine = new Regex(@"^(Error|Warning)\s+([^\s]+)\s+(\d+):\s+(.+)$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            foreach (Match m in regexWithLine.Matches(log))
            {
                int line = 1;
                int.TryParse(m.Groups[3].Value, out line);
                list.Add(new CompilerMessage
                {
                    Type = m.Groups[1].Value,
                    File = Path.GetFileName(m.Groups[2].Value),
                    Line = line,
                    Message = m.Groups[4].Value
                });
            }

            if (list.Count == 0)
            {
                var regexGeneral = new Regex(@"^(Error|Warning):\s+(.+)$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
                foreach (Match m in regexGeneral.Matches(log))
                {
                    list.Add(new CompilerMessage
                    {
                        Type = m.Groups[1].Value,
                        File = Path.GetFileName(currentFilePath),
                        Line = 1,
                        Message = m.Groups[2].Value
                    });
                }
            }

            return list;
        }

        public void RunProgram()
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                string defPrjDir = Path.Combine(turboRootDir, "Projects");
                if (!Directory.Exists(defPrjDir)) Directory.CreateDirectory(defPrjDir);
                currentFilePath = Path.Combine(defPrjDir, "NONAME00.C");
            }

            string srcPath = currentFilePath;
            string baseName = Path.GetFileNameWithoutExtension(srcPath);
            string prjDir = Path.GetDirectoryName(srcPath);
            if (string.IsNullOrEmpty(prjDir)) prjDir = Path.Combine(turboRootDir, "Projects");
            string exeFile = Path.Combine(prjDir, baseName + ".EXE");

            // Always compile the latest code from the editor
            bool ok = CompileProgram();
            if (!ok) return;

            if (!File.Exists(exeFile))
            {
                MessageBox.Show("Executable was not created: " + Path.GetFileName(exeFile), "Run Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            lastRunExe = exeFile;
            string exeName = Path.GetFileName(exeFile);

            // Launch User Screen (Native DOS Console)
            string userScreenBat = Path.Combine(prjDir, "USER_SCREEN.BAT");
            string batContent = string.Format(
                "@echo off\r\n" +
                "title Turbo C++ User Screen - {0}\r\n" +
                "color 07\r\n" +
                "cls\r\n" +
                "\"{1}\"\r\n" +
                "echo.\r\n" +
                "echo ----------------------------------------------------------------------\r\n" +
                "echo [ Program finished. Press any key to return to Turbo C++... ]\r\n" +
                "pause > nul\r\n" +
                "exit\r\n",
                exeName,
                exeFile
            );

            try
            {
                File.WriteAllText(userScreenBat, batContent);

                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c \"" + userScreenBat + "\"",
                    WorkingDirectory = prjDir,
                    UseShellExecute = true
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Run error: " + ex.Message, "Run Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public void ShowUserScreen()
        {
            if (string.IsNullOrEmpty(lastRunExe) || !File.Exists(lastRunExe))
            {
                MessageBox.Show("No program has been run yet. Press Ctrl+F9 to Run.", "User Screen", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            RunProgram();
        }

        public void LaunchClassicDosbox()
        {
            try
            {
                Process.Start(dosboxExe);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error launching DOSBox-X: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Zooming Engine (Window Zoom & Font Zoom)

        public void ToggleWindowZoom()
        {
            isWindowZoomed = !isWindowZoomed;
            if (isWindowZoomed)
            {
                pnlDesktop.Padding = new Padding(0, 0, 0, 0);
            }
            else
            {
                pnlDesktop.Padding = new Padding(8, 6, 14, 10);
            }
            pnlWindowHeader.Invalidate();
            pnlWindowBottom.Invalidate();
            pnlDesktop.Invalidate();
        }

        public void ToggleFormFullscreen()
        {
            if (this.WindowState == FormWindowState.Maximized)
            {
                this.WindowState = FormWindowState.Normal;
            }
            else
            {
                this.WindowState = FormWindowState.Maximized;
            }
        }

        public void ZoomIn()
        {
            if (currentFontSize < 36f)
            {
                currentFontSize += 1.5f;
                ApplyFontSize();
            }
        }

        public void ZoomOut()
        {
            if (currentFontSize > 8f)
            {
                currentFontSize -= 1.5f;
                ApplyFontSize();
            }
        }

        public void ResetZoom()
        {
            currentFontSize = DEFAULT_FONT_SIZE;
            ApplyFontSize();
        }

        private void ApplyFontSize()
        {
            if (rtbEditor != null)
            {
                rtbEditor.Font = GetIdeFont(currentFontSize, FontStyle.Regular);
            }
            UpdateZoomStatus();
            if (pnlWindowBottom != null) pnlWindowBottom.Invalidate();
            if (rtbEditor != null) HighlightSyntax(rtbEditor);
        }

        private void UpdateZoomStatus()
        {
            int pct = (int)Math.Round((currentFontSize / DEFAULT_FONT_SIZE) * 100);
            if (lblZoomStatus != null)
            {
                lblZoomStatus.Text = string.Format(" Zoom: {0}% ", pct);
            }
        }

        #endregion

        #region Dialogs & Helpers

        private void ShowFindDialog()
        {
            using (var form = new Form())
            {
                form.Text = "Find";
                form.Size = new Size(340, 140);
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                var lbl = new Label { Text = "Find what:", Location = new Point(15, 18), AutoSize = true };
                var txt = new TextBox { Location = new Point(90, 15), Width = 210, Text = rtbEditor.SelectedText };
                var btnFind = new Button { Text = "Find Next", Location = new Point(130, 55), DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "Cancel", Location = new Point(220, 55), DialogResult = DialogResult.Cancel };

                form.Controls.AddRange(new Control[] { lbl, txt, btnFind, btnCancel });
                form.AcceptButton = btnFind;
                form.CancelButton = btnCancel;

                if (form.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(txt.Text))
                {
                    int start = rtbEditor.SelectionStart + rtbEditor.SelectionLength;
                    int idx = rtbEditor.Find(txt.Text, start, RichTextBoxFinds.None);
                    if (idx < 0) idx = rtbEditor.Find(txt.Text, 0, RichTextBoxFinds.None);
                    if (idx >= 0)
                    {
                        rtbEditor.SelectionStart = idx;
                        rtbEditor.SelectionLength = txt.Text.Length;
                        rtbEditor.Focus();
                    }
                    else
                    {
                        MessageBox.Show("Text not found.", "Find", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void ShowReplaceDialog()
        {
            using (var form = new Form())
            {
                form.Text = "Replace";
                form.Size = new Size(350, 180);
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                var lbl1 = new Label { Text = "Find what:", Location = new Point(15, 18), AutoSize = true };
                var txtFind = new TextBox { Location = new Point(100, 15), Width = 210, Text = rtbEditor.SelectedText };

                var lbl2 = new Label { Text = "Replace with:", Location = new Point(15, 52), AutoSize = true };
                var txtReplace = new TextBox { Location = new Point(100, 50), Width = 210 };

                var btnReplace = new Button { Text = "Replace", Location = new Point(130, 95), DialogResult = DialogResult.OK };
                var btnReplaceAll = new Button { Text = "Replace All", Location = new Point(220, 95) };

                btnReplaceAll.Click += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(txtFind.Text))
                    {
                        rtbEditor.Text = rtbEditor.Text.Replace(txtFind.Text, txtReplace.Text);
                        HighlightSyntax(rtbEditor);
                        form.Close();
                    }
                };

                form.Controls.AddRange(new Control[] { lbl1, txtFind, lbl2, txtReplace, btnReplace, btnReplaceAll });
                form.AcceptButton = btnReplace;

                if (form.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(txtFind.Text))
                {
                    if (rtbEditor.SelectedText == txtFind.Text)
                    {
                        rtbEditor.SelectedText = txtReplace.Text;
                    }
                    int idx = rtbEditor.Find(txtFind.Text, rtbEditor.SelectionStart, RichTextBoxFinds.None);
                    if (idx >= 0)
                    {
                        rtbEditor.SelectionStart = idx;
                        rtbEditor.SelectionLength = txtFind.Text.Length;
                        rtbEditor.Focus();
                    }
                }
            }
        }

        private void ShowGoToLineDialog()
        {
            using (var form = new Form())
            {
                form.Text = "Go to Line";
                form.Size = new Size(300, 140);
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                var lbl = new Label { Text = "Line number:", Location = new Point(15, 20), AutoSize = true };
                var txt = new TextBox { Location = new Point(100, 18), Width = 160, Text = "1" };
                var btnOk = new Button { Text = "OK", Location = new Point(100, 55), DialogResult = DialogResult.OK };
                var btnCancel = new Button { Text = "Cancel", Location = new Point(185, 55), DialogResult = DialogResult.Cancel };

                form.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
                form.AcceptButton = btnOk;
                form.CancelButton = btnCancel;

                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    int line;
                    if (int.TryParse(txt.Text, out line) && line > 0 && line <= rtbEditor.Lines.Length)
                    {
                        int charIdx = rtbEditor.GetFirstCharIndexFromLine(line - 1);
                        if (charIdx >= 0)
                        {
                            rtbEditor.SelectionStart = charIdx;
                            rtbEditor.ScrollToCaret();
                            rtbEditor.Focus();
                        }
                    }
                }
            }
        }

        private void ShowDirectoriesDialog()
        {
            MessageBox.Show(
                string.Format(
                    "Include Directories: {0}\\INCLUDE\n" +
                    "Library Directories: {0}\\LIB\n" +
                    "Output Directory:    {0}\\Projects\n" +
                    "Compiler Directory:  {0}\\Compiler\\tcc",
                    turboRootDir
                ),
                "Directories",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void ShowHelpDialog()
        {
            MessageBox.Show(
                "Turbo C++ 3.0 Rebuilt Environment\n\n" +
                "Shortcuts:\n" +
                "  Alt+F9       : Compile Current Program\n" +
                "  Ctrl+F9      : Run Program (DOS User Screen)\n" +
                "  Alt+F5       : View User Screen\n" +
                "  F5           : Zoom Window (Maximize / Restore IDE window)\n" +
                "  Ctrl++       : Zoom In Font\n" +
                "  Ctrl+-       : Zoom Out Font\n" +
                "  Ctrl+0       : Reset Zoom (100%)\n" +
                "  Ctrl+Wheel   : Dynamic Font Zoom\n" +
                "  Alt+Enter    : Fullscreen App\n" +
                "  F2           : Save File\n" +
                "  F3           : Open File\n" +
                "  Ctrl+C / Ins : Copy\n" +
                "  Ctrl+V / Shft: Paste\n" +
                "  Alt+X        : Exit\n\n" +
                "Features:\n" +
                "  * Full C99/C11 modern syntax support with conio.h\n" +
                "  * Authentic Turbo C++ blue UI, fonts & highlighting\n" +
                "  * Top-to-bottom and left-to-right scrollbars\n" +
                "  * Completely anti-minimize on Alt+Tab",
                "Turbo C++ Help",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        private void ShowAboutDialog()
        {
            MessageBox.Show(
                "Turbo C++ Version 3.0 Rebuilt\n" +
                "Authentic Borland C++ Interface\n" +
                "Engineered for modern Windows with zero-error copy/paste,\n" +
                "anti-minimize, full zooming & high-speed C compiler.",
                "About Turbo C++",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
        }

        #endregion
    }
}
