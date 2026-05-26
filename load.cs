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
        private static readonly Color Navy = Color.FromArgb(11, 31, 73);
        private static readonly Color NavyDark = Color.FromArgb(5, 18, 48);
        private static readonly Color NavySoft = Color.FromArgb(25, 52, 103);
        private static readonly Color Gold = Color.FromArgb(197, 158, 57);
        private static readonly Color GoldSoft = Color.FromArgb(235, 219, 167);
        private static readonly Color Ivory = Color.FromArgb(248, 246, 239);
        private static readonly Color Paper = Color.FromArgb(255, 253, 247);
        private static readonly Color Ink = Color.FromArgb(28, 36, 52);
        private static readonly Color MutedInk = Color.FromArgb(105, 113, 130);
        private static readonly Color Border = Color.FromArgb(215, 207, 185);

        private const int FormW = 760;
        private const int FormH = 460;
        private const int BrandW = 270;
        private const int CornerRadius = 12;

        private const int FadeInterval = 18;
        private const double FadeStep = 0.08;
        private const int ProgressInterval = 45;
        private const int TotalTicks = 86;

        private static readonly string[] StatusMessages =
        {
            "Starting application services",
            "Checking school records",
            "Loading academic modules",
            "Preparing secure workspace",
            "Opening sign in"
        };

        private Panel _progressTrack;
        private Panel _progressFill;
        private Label _statusLabel;
        private Label _percentLabel;
        private Timer _progressTimer;

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

        private void BuildBrandPanel()
        {
            pictureBoxLogo.Bounds = new Rectangle(71, 80, 128, 128);
            pictureBoxLogo.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxLogo.BackColor = Color.Transparent;
            pictureBoxLogo.Image = LoadLogoImage();
            pictureBoxLogo.Paint += PaintLogoFallback;
            Controls.Add(pictureBoxLogo);

            Controls.Add(CreateLabel(
                "KINGDOM PREP.",
                new Font("Georgia", 14F, FontStyle.Bold),
                Color.White,
                new Rectangle(32, 222, BrandW - 64, 28),
                ContentAlignment.MiddleCenter));

            Controls.Add(CreateLabel(
                "AKIM ODA - ABENASE",
                new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                GoldSoft,
                new Rectangle(32, 252, BrandW - 64, 22),
                ContentAlignment.MiddleCenter));

            Controls.Add(CreateLabel(
                "Knowledge is Power",
                new Font("Georgia", 10F, FontStyle.Italic),
                Color.FromArgb(225, 231, 245),
                new Rectangle(32, 298, BrandW - 64, 28),
                ContentAlignment.MiddleCenter));

            Controls.Add(CreateLabel(
                "Version 1.0.0",
                new Font("Segoe UI", 8F, FontStyle.Regular),
                Color.FromArgb(155, 169, 205),
                new Rectangle(32, FormH - 58, BrandW - 64, 20),
                ContentAlignment.MiddleCenter));
        }

        private void BuildContentPanel()
        {
            int contentX = BrandW + 48;
            int contentW = FormW - contentX - 54;

            Controls.Add(CreateLabel(
                "SCHOOL MANAGEMENT SYSTEM",
                new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold),
                Gold,
                new Rectangle(contentX, 74, contentW, 20),
                ContentAlignment.MiddleLeft));

            Controls.Add(CreateLabel(
                "Kingdom Preparatory School",
                new Font("Georgia", 25F, FontStyle.Bold),
                Ink,
                new Rectangle(contentX, 100, contentW, 48),
                ContentAlignment.MiddleLeft));

            Controls.Add(CreateLabel(
                "A clean workspace for admissions, academics, finance, attendance, staff, and reports.",
                new Font("Segoe UI", 10.5F, FontStyle.Regular),
                MutedInk,
                new Rectangle(contentX + 1, 153, contentW - 18, 54),
                ContentAlignment.TopLeft));

            Controls.Add(new Panel
            {
                Bounds = new Rectangle(contentX, 220, contentW, 1),
                BackColor = Border
            });

            Controls.Add(CreateLabel(
                "PREPARING YOUR SESSION",
                new Font("Segoe UI Semibold", 8F, FontStyle.Bold),
                Color.FromArgb(81, 89, 108),
                new Rectangle(contentX, 244, contentW, 20),
                ContentAlignment.MiddleLeft));

            _statusLabel = CreateLabel(
                StatusMessages[0],
                new Font("Segoe UI", 9.5F, FontStyle.Regular),
                MutedInk,
                new Rectangle(contentX, 270, contentW - 62, 24),
                ContentAlignment.MiddleLeft);
            Controls.Add(_statusLabel);

            _percentLabel = CreateLabel(
                "0%",
                new Font("Segoe UI Semibold", 9F, FontStyle.Bold),
                Ink,
                new Rectangle(contentX + contentW - 58, 270, 58, 24),
                ContentAlignment.MiddleRight);
            Controls.Add(_percentLabel);

            _progressTrack = new Panel
            {
                Bounds = new Rectangle(contentX, 306, contentW, 7),
                BackColor = Color.FromArgb(225, 220, 207)
            };
            _progressFill = new Panel
            {
                Bounds = new Rectangle(0, 0, 0, 7),
                BackColor = Gold
            };
            _progressTrack.Controls.Add(_progressFill);
            Controls.Add(_progressTrack);

            Controls.Add(CreateLabel(
                "Developed for reliable daily school administration",
                new Font("Segoe UI", 8.5F, FontStyle.Regular),
                Color.FromArgb(128, 135, 151),
                new Rectangle(contentX, FormH - 72, contentW, 22),
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
                g.FillRectangle(accent, BrandW - 8, 0, 8, FormH);

            using (var gold = new SolidBrush(Gold))
                g.FillRectangle(gold, BrandW - 2, 0, 2, FormH);

            DrawBrandOrnaments(g);

            using (var pen = new Pen(Color.FromArgb(175, 166, 145), 1f))
                g.DrawPath(pen, RoundedRect(new Rectangle(0, 0, FormW - 1, FormH - 1), CornerRadius));
        }

        private void DrawBrandOrnaments(Graphics g)
        {
            using (var pen = new Pen(Color.FromArgb(58, 84, 139), 1f))
            {
                g.DrawLine(pen, 42, 54, BrandW - 42, 54);
                g.DrawLine(pen, 42, FormH - 92, BrandW - 42, FormH - 92);
            }

            using (var goldPen = new Pen(Color.FromArgb(150, Gold), 1f))
            {
                g.DrawEllipse(goldPen, 52, 61, 166, 166);
                g.DrawEllipse(goldPen, 63, 72, 144, 144);
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
                g.DrawString("KPS", font, brush, bounds, format);
            }
        }

        private Image LoadLogoImage()
        {
            string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string[] candidates =
            {
                Path.Combine(baseDir ?? "", "Resources", "school_logo.png"),
                Path.Combine(baseDir ?? "", "..", "..", "Resources", "school_logo.png"),
                Path.Combine(Directory.GetCurrentDirectory(), "Resources", "school_logo.png")
            };

            foreach (string candidate in candidates)
            {
                try
                {
                    if (!File.Exists(candidate))
                        continue;

                    using (var source = Image.FromFile(candidate))
                    {
                        return new Bitmap(source);
                    }
                }
                catch
                {
                    // Fallback drawing will render the logo mark.
                }
            }

            return null;
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
                double percent = Math.Min((double)tick / TotalTicks, 1.0);
                int percentValue = (int)Math.Round(percent * 100);

                _progressFill.Width = (int)(_progressTrack.Width * percent);
                _percentLabel.Text = percentValue + "%";

                int messageIndex = Math.Min((int)(percent * StatusMessages.Length), StatusMessages.Length - 1);
                _statusLabel.Text = StatusMessages[messageIndex];

                if (tick >= TotalTicks)
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
