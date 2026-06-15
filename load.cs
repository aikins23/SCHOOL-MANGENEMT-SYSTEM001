using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System
{
    /// <summary>
    /// Modern, classic, and professional splash screen.
    /// Sequence: fade in, animate loading state, fade out, then open login.
    /// </summary>
    public partial class load : Form
    {
        private static readonly Color Navy = Color.FromArgb(8, 25, 61);
        private static readonly Color NavyDark = Color.FromArgb(3, 12, 34);
        private static readonly Color NavySoft = Color.FromArgb(20, 48, 100);
        private static readonly Color Gold = Color.FromArgb(200, 159, 54);
        private static readonly Color GoldSoft = Color.FromArgb(238, 221, 166);
        private static readonly Color Ivory = Color.White;
        private static readonly Color Paper = Color.White;
        private static readonly Color Ink = Color.FromArgb(22, 31, 48);
        private static readonly Color MutedInk = Color.FromArgb(104, 113, 132);
        private static readonly Color Border = Color.FromArgb(218, 210, 190);

        private const int FormW = 820;
        private const int FormH = 500;
        private const int BrandW = 292;
        private const int CornerRadius = 14;

        private const int FadeInterval = 10;
        private const double FadeStep = 0.15;
        private const int ProgressInterval = 45;
        private const int TotalTicks = 90;

        private static readonly string[] StatusMessages =
        {
            "Starting application services...",
            "Initializing database schema...",
            "Syncing tables...",
            "Preparing secure workspace...",
            "Opening sign in..."
        };

        private Panel _progressTrack;
        private Panel _progressFill;
        private Label _statusLabel;
        private Label _percentLabel;
        private Timer _progressTimer;
        private bool _isDbInitialized = false;

        public load()
        {
            InitializeComponent();
            BuildSplashScreen();
            Load += OnSplashLoad;
        }

        private void BuildSplashScreen()
        {
            SuspendLayout();

            FormBorderStyle = FormBorderStyle.None;
            ClientSize = new Size(FormW, FormH);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Ivory;
            Opacity = 0;
            DoubleBuffered = true;
            Region = new Region(RoundedRect(new Rectangle(0, 0, FormW, FormH), CornerRadius));
            this.Icon = kingdom_Preparatory_School_Management_System.Common.Branding.AppIcon;

            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer,
                true);

            Controls.Clear();

            BuildBrandPanel();
            BuildContentPanel();

            ResumeLayout(false);
        }

        private async System.Threading.Tasks.Task InitializeDatabaseAsync()
        {
            try
            {
                await Data.DatabaseInitializer.InitializeAsync();
                await Data.SyncSchema.EnsureSyncColumnsAsync();
                await Data.SyncSchema.EnsurePerformanceIndexesAsync();
            }
            catch (Exception ex)
            {
                Services.LoggerHelper.LogWarning("Splash DB Init: " + ex.Message);
            }
            finally
            {
                _isDbInitialized = true;
            }
        }

        private void BuildBrandPanel()
        {
            // Logo made significantly bigger (256x256) and centered in the brand panel
            int logoSize = 256;
            pictureBoxLogo.Bounds = new Rectangle((BrandW - logoSize) / 2, 40, logoSize, logoSize);
            pictureBoxLogo.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxLogo.BackColor = Color.Transparent;
            pictureBoxLogo.Image = Common.Branding.GetLogo(onBlue: true);
            pictureBoxLogo.Paint += PaintLogoFallback;
            Controls.Add(pictureBoxLogo);

            Controls.Add(CreateLabel(
                "NYANSAPO ERP",
                new Font("Georgia", 13.5F, FontStyle.Bold),
                Color.White,
                new Rectangle(34, 310, BrandW - 68, 28),
                ContentAlignment.MiddleCenter));

            Controls.Add(CreateLabel(
                "SMART SCHOOL SOLUTIONS",
                new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                GoldSoft,
                new Rectangle(34, 340, BrandW - 68, 22),
                ContentAlignment.MiddleCenter));

            Controls.Add(CreateLabel(
                "Untangling Complexity",
                new Font("Georgia", 9.75F, FontStyle.Italic),
                Color.FromArgb(225, 231, 245),
                new Rectangle(34, 380, BrandW - 68, 28),
                ContentAlignment.MiddleCenter));

            Controls.Add(CreateLabel(
                "Version 1.0.0",
                new Font("Segoe UI", 8F, FontStyle.Regular),
                Color.FromArgb(155, 169, 205),
                new Rectangle(34, FormH - 62, BrandW - 68, 20),
                ContentAlignment.MiddleCenter));
        }

        private void BuildContentPanel()
        {
            int contentX = BrandW + 54;
            int contentW = FormW - contentX - 58;

            Controls.Add(CreateLabel(
                "SCHOOL MANAGEMENT SYSTEM",
                new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Gold,
                new Rectangle(contentX, 82, contentW, 20),
                ContentAlignment.MiddleLeft));

            Controls.Add(CreateLabel(
                Common.AppConfig.ProductName,
                new Font("Georgia", 25.5F, FontStyle.Bold),
                Ink,
                new Rectangle(contentX, 108, contentW, 48),
                ContentAlignment.MiddleLeft));

            Controls.Add(CreateLabel(
                "A clean workspace for admissions, academics, finance, attendance, staff, and reports.",
                new Font("Segoe UI", 10.25F, FontStyle.Regular),
                MutedInk,
                new Rectangle(contentX + 1, 164, contentW - 20, 54),
                ContentAlignment.TopLeft));

            Controls.Add(new Panel
            {
                Bounds = new Rectangle(contentX, 226, contentW, 1),
                BackColor = Border
            });

            Controls.Add(CreateLabel(
                "LOCAL-FIRST DESKTOP  |  CLOUD-READY OPERATIONS",
                new Font("Segoe UI Semibold", 7.75F, FontStyle.Bold),
                Color.FromArgb(130, 119, 88),
                new Rectangle(contentX, 232, contentW, 18),
                ContentAlignment.MiddleLeft));

            Controls.Add(CreateLabel(
                "PREPARING YOUR SESSION",
                new Font("Segoe UI Semibold", 8F, FontStyle.Bold),
                Color.FromArgb(81, 89, 108),
                new Rectangle(contentX, 260, contentW, 20),
                ContentAlignment.MiddleLeft));

            _statusLabel = CreateLabel(
                StatusMessages[0],
                new Font("Segoe UI", 9.5F, FontStyle.Regular),
                MutedInk,
                new Rectangle(contentX, 286, contentW - 62, 24),
                ContentAlignment.MiddleLeft);
            Controls.Add(_statusLabel);

            _percentLabel = CreateLabel(
                "0%",
                new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Ink,
                new Rectangle(contentX + contentW - 58, 286, 58, 24),
                ContentAlignment.MiddleRight);
            Controls.Add(_percentLabel);

            _progressTrack = new Panel
            {
                Bounds = new Rectangle(contentX, 322, contentW, 8),
                BackColor = Color.FromArgb(225, 220, 207)
            };
            _progressFill = new Panel
            {
                Bounds = new Rectangle(0, 0, 0, 8),
                BackColor = Gold
            };
            _progressTrack.Controls.Add(_progressFill);
            Controls.Add(_progressTrack);

            Controls.Add(CreateLabel(
                "Developed for reliable daily school administration",
                new Font("Segoe UI", 8.5F, FontStyle.Regular),
                Color.FromArgb(128, 135, 151),
                new Rectangle(contentX, FormH - 78, contentW, 22),
                ContentAlignment.MiddleLeft));
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            using (var paper = new SolidBrush(Paper))
                g.FillRectangle(paper, 0, 0, FormW, FormH);

            using (var brand = new LinearGradientBrush(
                new Rectangle(0, 0, BrandW, FormH),
                Navy,
                NavyDark,
                LinearGradientMode.Vertical))
            {
                g.FillRectangle(brand, 0, 0, BrandW, FormH);
            }

            using (var accent = new SolidBrush(NavySoft))
                g.FillRectangle(accent, BrandW - 6, 0, 6, FormH);

            using (var gold = new SolidBrush(Gold))
                g.FillRectangle(gold, BrandW - 2, 0, 2, FormH);

            using (var softPanel = new SolidBrush(Color.White))
                g.FillRectangle(softPanel, BrandW, 0, FormW - BrandW, FormH);

            DrawBrandOrnaments(g);

            using (var pen = new Pen(Color.FromArgb(175, 166, 145), 1f))
                g.DrawPath(pen, RoundedRect(new Rectangle(0, 0, FormW - 1, FormH - 1), CornerRadius));
        }

        private void DrawBrandOrnaments(Graphics g)
        {
            using (var pen = new Pen(Color.FromArgb(58, 84, 139), 1f))
            {
                g.DrawLine(pen, 46, 30, BrandW - 46, 30);
                g.DrawLine(pen, 46, FormH - 92, BrandW - 46, FormH - 92);
            }

            using (var centerPen = new Pen(Color.FromArgb(92, 111, 154), 1f))
            {
                g.DrawLine(centerPen, 98, 300, BrandW - 98, 300);
            }
        }

        private void PaintLogoFallback(object sender, PaintEventArgs e)
        {
            if (pictureBoxLogo.Image != null)
                return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle bounds = new Rectangle(5, 5, pictureBoxLogo.Width - 10, pictureBoxLogo.Height - 10);
            using (var fill = new SolidBrush(Ivory))
                g.FillEllipse(fill, bounds);
            using (var pen = new Pen(Gold, 3f))
                g.DrawEllipse(pen, bounds);

            using (var font = new Font("Georgia", 22F, FontStyle.Bold))
            using (var brush = new SolidBrush(Navy))
            using (var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                g.DrawString("NS", font, brush, bounds, format);
            }
        }

        private static Label CreateLabel(string text, Font font, Color fore, Rectangle bounds, ContentAlignment align)
        {
            return new Label
            {
                Text = text,
                Font = font,
                ForeColor = fore,
                Bounds = bounds,
                TextAlign = align,
                BackColor = Color.Transparent,
                AutoSize = false,
                UseMnemonic = false
            };
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            int diameter = radius * 2;
            var path = new GraphicsPath();
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void OnSplashLoad(object sender, EventArgs e)
        {
            _ = InitializeDatabaseAsync();
            FadeIn(StartProgressAnimation);
        }

        private void FadeIn(Action onComplete)
        {
            double opacity = 0;
            var timer = new Timer { Interval = FadeInterval };
            timer.Tick += (s, args) =>
            {
                opacity = Math.Min(opacity + FadeStep, 1.0);
                Opacity = opacity;

                if (opacity >= 1.0)
                {
                    timer.Stop();
                    timer.Dispose();
                    onComplete?.Invoke();
                }
            };
            timer.Start();
        }

        private void StartProgressAnimation()
        {
            int tick = 0;
            _progressTimer = new Timer { Interval = ProgressInterval };
            _progressTimer.Tick += (s, args) =>
            {
                tick++;

                // Slow down progress at 90% if DB isn't ready yet
                if (tick >= TotalTicks * 0.9 && !_isDbInitialized)
                {
                    tick = (int)(TotalTicks * 0.9);
                }

                double percent = Math.Min((double)tick / TotalTicks, 1.0);
                int percentValue = (int)Math.Round(percent * 100);

                _progressFill.Width = (int)(_progressTrack.Width * percent);
                _percentLabel.Text = percentValue + "%";

                int messageIndex = Math.Min((int)(percent * StatusMessages.Length), StatusMessages.Length - 1);
                _statusLabel.Text = StatusMessages[messageIndex];

                if (tick >= TotalTicks && _isDbInitialized)
                {
                    _progressTimer.Stop();
                    _progressTimer.Dispose();
                    FadeOut(LaunchLogin);
                }
            };
            _progressTimer.Start();
        }

        private void FadeOut(Action onComplete)
        {
            double opacity = 1.0;
            var timer = new Timer { Interval = FadeInterval };
            timer.Tick += (s, args) =>
            {
                opacity = Math.Max(opacity - FadeStep, 0.0);
                Opacity = opacity;

                if (opacity <= 0)
                {
                    timer.Stop();
                    timer.Dispose();
                    onComplete?.Invoke();
                }
            };
            timer.Start();
        }

        private void LaunchLogin()
        {
            new frmlogin().Show();
            Close();
        }
    }
}
