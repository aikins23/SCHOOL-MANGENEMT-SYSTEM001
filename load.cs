using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System
{
    /// <summary>
    /// Modern splash screen for Kingdom Preparatory School Management System.
    /// All layout is built in code so it is easy to change without touching
    /// the designer.  Sequence:
    ///   1. Fade in  (300 ms)
    ///   2. Progress bar fills + status messages cycle  (4 s)
    ///   3. Fade out (300 ms) → frmlogin opens
    /// </summary>
    public partial class load : Form
    {
        // ── Palette ───────────────────────────────────────────────────────────
        private static readonly Color BgTop     = Color.FromArgb(10,  10,  65);
        private static readonly Color BgBottom  = Color.FromArgb(28,  28, 108);
        private static readonly Color GoldBold  = Color.FromArgb(255, 215,   0);
        private static readonly Color GoldSoft  = Color.FromArgb(200, 170,   0);
        private static readonly Color TextWhite = Color.White;
        private static readonly Color TextMuted = Color.FromArgb(155, 165, 210);
        private static readonly Color TrackBg   = Color.FromArgb( 48,  52, 128);
        private static readonly Color FooterFg  = Color.FromArgb( 85,  90, 145);

        // ── Animation constants ───────────────────────────────────────────────
        private const int FadeInterval     = 20;   // ms per fade step
        private const double FadeStep      = 0.08; // opacity change per step
        private const int ProgressInterval = 50;   // ms per progress tick
        private const int TotalTicks       = 80;   // 4 s total loading time

        // ── Status messages (evenly spaced across the progress bar) ──────────
        private static readonly string[] StatusMessages =
        {
            "Initializing system...",
            "Loading modules...",
            "Preparing workspace...",
            "Almost ready...",
            "Welcome to Kingdom Preparatory!"
        };

        // ── Live UI references ────────────────────────────────────────────────
        private Panel _progressFill;
        private Label _statusLabel;
        private Timer _progressTimer;

        // ─────────────────────────────────────────────────────────────────────
        public load()
        {
            InitializeComponent();   // creates pictureBoxLogo field
            BuildSplashScreen();
            this.Load += OnSplashLoad;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Screen builder
        // ─────────────────────────────────────────────────────────────────────
        private void BuildSplashScreen()
        {
            SuspendLayout();

            // Form
            BackColor       = BgTop;           // shown before gradient paints
            FormBorderStyle = FormBorderStyle.None;
            ClientSize      = new Size(680, 420);
            StartPosition   = FormStartPosition.CenterScreen;
            Opacity         = 0;              // start transparent; fade in on Load

            // Double-buffer so gradient + progress animation are flicker-free
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);

            Controls.Clear();

            // ── Logo (88 × 88, horizontally centred) ─────────────────────────
            const int logoSize = 88;
            pictureBoxLogo.Bounds    = new Rectangle((680 - logoSize) / 2, 32, logoSize, logoSize);
            pictureBoxLogo.SizeMode  = PictureBoxSizeMode.Zoom;
            pictureBoxLogo.BackColor = Color.Transparent;
            pictureBoxLogo.Paint    += PaintLogoFallback;
            Controls.Add(pictureBoxLogo);

            // ── School name ───────────────────────────────────────────────────
            Controls.Add(Lbl(
                "KINGDOM PREPARATORY SCHOOL",
                new Font("Segoe UI", 20F, FontStyle.Bold),
                TextWhite,
                new Rectangle(40, 134, 600, 44),
                ContentAlignment.MiddleCenter));

            // ── "School Management System" subtitle ───────────────────────────
            Controls.Add(Lbl(
                "School Management System",
                new Font("Segoe UI", 11F, FontStyle.Regular),
                GoldBold,
                new Rectangle(40, 182, 600, 28),
                ContentAlignment.MiddleCenter));

            // ── Divider (narrow gold rule) ────────────────────────────────────
            Controls.Add(new Panel
            {
                Bounds    = new Rectangle((680 - 220) / 2, 218, 220, 2),
                BackColor = GoldSoft
            });

            // ── Tagline ───────────────────────────────────────────────────────
            Controls.Add(Lbl(
                "“KNOWLEDGE IS POWER”",
                new Font("Segoe UI", 9.5F, FontStyle.Italic),
                TextMuted,
                new Rectangle(40, 226, 600, 22),
                ContentAlignment.MiddleCenter));

            // ── Status text ───────────────────────────────────────────────────
            _statusLabel = Lbl(
                StatusMessages[0],
                new Font("Segoe UI", 9F, FontStyle.Regular),
                TextMuted,
                new Rectangle(40, 300, 600, 20),
                ContentAlignment.MiddleCenter);
            Controls.Add(_statusLabel);

            // ── Progress bar ──────────────────────────────────────────────────
            const int trackW = 560;
            const int trackX = (680 - trackW) / 2;
            var track = new Panel
            {
                Bounds    = new Rectangle(trackX, 326, trackW, 6),
                BackColor = TrackBg
            };
            _progressFill = new Panel
            {
                Bounds    = new Rectangle(0, 0, 0, 6),
                BackColor = GoldBold
            };
            track.Controls.Add(_progressFill);
            Controls.Add(track);

            // ── "Powered by" dots decoration ─────────────────────────────────
            Controls.Add(Lbl(
                "· · ·",
                new Font("Segoe UI", 10F, FontStyle.Regular),
                TrackBg,
                new Rectangle(40, 340, 600, 16),
                ContentAlignment.MiddleCenter));

            // ── Footer ────────────────────────────────────────────────────────
            Controls.Add(Lbl(
                "v1.0.0",
                new Font("Segoe UI", 8F, FontStyle.Regular),
                FooterFg,
                new Rectangle(24, 393, 120, 18),
                ContentAlignment.MiddleLeft));

            Controls.Add(Lbl(
                "DEVELOPED BY DARKTECH HUB",
                new Font("Segoe UI", 8F, FontStyle.Regular),
                FooterFg,
                new Rectangle(536, 393, 120, 18),
                ContentAlignment.MiddleRight));

            ResumeLayout(false);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Gradient background
        // ─────────────────────────────────────────────────────────────────────
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (var brush = new LinearGradientBrush(
                ClientRectangle, BgTop, BgBottom, LinearGradientMode.Vertical))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Logo fallback — draws a gold-ring shield with "KPS" text when no
        //  image is assigned to pictureBoxLogo.
        // ─────────────────────────────────────────────────────────────────────
        private void PaintLogoFallback(object sender, PaintEventArgs e)
        {
            if (pictureBoxLogo.Image != null) return;

            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            var b = new Rectangle(4, 4, pictureBoxLogo.Width - 8, pictureBoxLogo.Height - 8);

            // Circle fill (slightly lighter than form background)
            using (var fill = new SolidBrush(Color.FromArgb(35, 35, 120)))
                g.FillEllipse(fill, b);

            // Gold outer ring
            using (var pen = new Pen(GoldBold, 2.5f))
                g.DrawEllipse(pen, b);

            // Inner thin ring (adds depth)
            var inner = new Rectangle(b.X + 6, b.Y + 6, b.Width - 12, b.Height - 12);
            using (var pen = new Pen(Color.FromArgb(100, 255, 215, 0), 1f))
                g.DrawEllipse(pen, inner);

            // "KPS" abbreviation centred in the circle
            using (var font  = new Font("Segoe UI", 17F, FontStyle.Bold))
            using (var brush = new SolidBrush(GoldBold))
            {
                var sf = new StringFormat
                {
                    Alignment     = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString("KPS", font, brush,
                    new RectangleF(b.X, b.Y, b.Width, b.Height), sf);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Life-cycle
        // ─────────────────────────────────────────────────────────────────────
        private void OnSplashLoad(object sender, EventArgs e)
        {
            FadeIn(onComplete: StartProgressAnimation);
        }

        private void FadeIn(Action onComplete)
        {
            double opacity = 0;
            var timer = new Timer { Interval = FadeInterval };
            timer.Tick += (s, _) =>
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
            _progressTimer.Tick += (s, e) =>
            {
                tick++;
                double pct = Math.Min((double)tick / TotalTicks, 1.0);

                // Advance progress fill
                _progressFill.Width = (int)(_progressFill.Parent.Width * pct);

                // Update status message
                int msgIdx = Math.Min(
                    (int)(pct * StatusMessages.Length),
                    StatusMessages.Length - 1);
                _statusLabel.Text = StatusMessages[msgIdx];

                if (tick >= TotalTicks)
                {
                    _progressTimer.Stop();
                    _progressTimer.Dispose();
                    FadeOut(onComplete: LaunchLogin);
                }
            };
            _progressTimer.Start();
        }

        private void FadeOut(Action onComplete)
        {
            double opacity = 1.0;
            var timer = new Timer { Interval = FadeInterval };
            timer.Tick += (s, _) =>
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

        // ─────────────────────────────────────────────────────────────────────
        //  Helper
        // ─────────────────────────────────────────────────────────────────────
        private static Label Lbl(string text, Font font, Color fore,
                                 Rectangle bounds, ContentAlignment align)
        {
            return new Label
            {
                Text        = text,
                Font        = font,
                ForeColor   = fore,
                Bounds      = bounds,
                TextAlign   = align,
                BackColor   = Color.Transparent,
                AutoSize    = false,
                UseMnemonic = false
            };
        }
    }
}
