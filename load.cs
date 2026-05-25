using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System
{
    /// <summary>
    /// Classic-professional splash screen.
    /// Layout: dark top band | gradient content | dark bottom band.
    /// Sequence: fade-in → progress animation (4 s) → fade-out → frmlogin.
    /// </summary>
    public partial class load : Form
    {
        // ── Palette (classic academic) ─────────────────────────────────────────
        private static readonly Color BandDark    = Color.FromArgb(6,   11,  46);   // header/footer band
        private static readonly Color BgDeep      = Color.FromArgb(11,  20,  70);   // gradient top
        private static readonly Color BgMid       = Color.FromArgb(19,  32,  90);   // gradient bottom
        private static readonly Color AntiqueGold = Color.FromArgb(212, 175,  55);  // main gold
        private static readonly Color GoldDim     = Color.FromArgb(148, 118,  30);  // muted gold (rules, track)
        private static readonly Color GoldFill    = Color.FromArgb(225, 190,  65);  // progress fill
        private static readonly Color Cream       = Color.FromArgb(255, 252, 235);  // warm white
        private static readonly Color CreamMuted  = Color.FromArgb(175, 170, 148);  // muted cream
        private static readonly Color BandText    = Color.FromArgb(110, 118, 168);  // text inside bands
        private static readonly Color SepLine     = Color.FromArgb( 42,  50, 108);  // thin separator

        // ── Layout constants ──────────────────────────────────────────────────
        private const int FormW     = 720;
        private const int FormH     = 450;
        private const int BandH     = 52;          // top and bottom band height

        // ── Animation ─────────────────────────────────────────────────────────
        private const int    FadeInterval     = 20;
        private const double FadeStep         = 0.07;
        private const int    ProgressInterval = 50;
        private const int    TotalTicks       = 80;   // 4 s

        private static readonly string[] StatusMessages =
        {
            "Initializing system...",
            "Loading modules...",
            "Preparing workspace...",
            "Almost ready...",
            "Welcome to Kingdom Preparatory!"
        };

        // ── Live UI refs ──────────────────────────────────────────────────────
        private Panel _progressFill;
        private Label _statusLabel;
        private Timer _progressTimer;

        // ─────────────────────────────────────────────────────────────────────
        public load()
        {
            InitializeComponent();
            BuildSplashScreen();
            this.Load += OnSplashLoad;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Screen builder
        // ─────────────────────────────────────────────────────────────────────
        private void BuildSplashScreen()
        {
            SuspendLayout();

            BackColor       = BandDark;
            FormBorderStyle = FormBorderStyle.None;
            ClientSize      = new Size(FormW, FormH);
            StartPosition   = FormStartPosition.CenterScreen;
            Opacity         = 0;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);

            Controls.Clear();

            // ── Heraldic crest (88 × 90) ─────────────────────────────────────
            const int logoW = 96, logoH = 96;
            pictureBoxLogo.Bounds    = new Rectangle((FormW - logoW) / 2, BandH + 10, logoW, logoH);
            pictureBoxLogo.SizeMode  = PictureBoxSizeMode.Zoom;
            pictureBoxLogo.BackColor = Color.Transparent;
            pictureBoxLogo.Paint    += PaintCrestFallback;

            // Try loading real school logo; fallback shield is drawn when Image is null
            try
            {
                string logoPath = System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(
                        System.Reflection.Assembly.GetExecutingAssembly().Location),
                    "Resources", "school_logo.png");
                if (System.IO.File.Exists(logoPath))
                    pictureBoxLogo.Image = Image.FromFile(logoPath);
            }
            catch { /* PaintCrestFallback will draw the KPS shield instead */ }

            Controls.Add(pictureBoxLogo);

            // ── School name  (Georgia — classic serif) ────────────────────────
            Controls.Add(Lbl(
                "KINGDOM PREPARATORY SCHOOL",
                new Font("Georgia", 21F, FontStyle.Bold),
                Cream,
                new Rectangle(30, BandH + 114, FormW - 60, 46),
                ContentAlignment.MiddleCenter));

            // ── Gold double rule (title / subtitle divider) ───────────────────
            int ruleX = (FormW - 300) / 2;
            Controls.Add(new Panel { Bounds = new Rectangle(ruleX, BandH + 163, 300, 1), BackColor = AntiqueGold });
            Controls.Add(new Panel { Bounds = new Rectangle(ruleX, BandH + 167, 300, 1), BackColor = AntiqueGold });

            // ── Subtitle ──────────────────────────────────────────────────────
            Controls.Add(Lbl(
                "School Management System",
                new Font("Segoe UI", 11F, FontStyle.Regular),
                AntiqueGold,
                new Rectangle(30, BandH + 174, FormW - 60, 28),
                ContentAlignment.MiddleCenter));

            // ── Tagline (italic, muted) ───────────────────────────────────────
            Controls.Add(Lbl(
                "“ KNOWLEDGE  IS  POWER ”",
                new Font("Segoe UI", 9F, FontStyle.Italic),
                CreamMuted,
                new Rectangle(30, BandH + 208, FormW - 60, 22),
                ContentAlignment.MiddleCenter));

            // ── Thin separator ────────────────────────────────────────────────
            Controls.Add(new Panel
            {
                Bounds    = new Rectangle((FormW - 420) / 2, BandH + 242, 420, 1),
                BackColor = SepLine
            });

            // ── Status text ───────────────────────────────────────────────────
            _statusLabel = Lbl(
                StatusMessages[0],
                new Font("Segoe UI", 8.5F, FontStyle.Regular),
                CreamMuted,
                new Rectangle(30, BandH + 252, FormW - 60, 20),
                ContentAlignment.MiddleCenter);
            Controls.Add(_statusLabel);

            // ── Progress bar (8 px tall, antique gold track) ─────────────────
            const int trackW = 560;
            int trackX = (FormW - trackW) / 2;
            var track = new Panel
            {
                Bounds    = new Rectangle(trackX, BandH + 278, trackW, 8),
                BackColor = GoldDim
            };
            _progressFill = new Panel
            {
                Bounds    = new Rectangle(0, 0, 0, 8),
                BackColor = GoldFill
            };
            track.Controls.Add(_progressFill);
            Controls.Add(track);

            // ── Decorative bracket tips on the progress bar ───────────────────
            // Left cap
            Controls.Add(new Panel { Bounds = new Rectangle(trackX - 2, BandH + 276, 2, 12), BackColor = AntiqueGold });
            // Right cap
            Controls.Add(new Panel { Bounds = new Rectangle(trackX + trackW, BandH + 276, 2, 12), BackColor = AntiqueGold });

            ResumeLayout(false);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Background: gradient + dark bands + in-band text
        // ─────────────────────────────────────────────────────────────────────
        protected override void OnPaintBackground(PaintEventArgs e)
        {
            var g  = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            // Top dark band
            using (var b = new SolidBrush(BandDark))
                g.FillRectangle(b, 0, 0, FormW, BandH);

            // Content gradient
            using (var b = new LinearGradientBrush(
                new Rectangle(0, BandH, FormW, FormH - BandH * 2),
                BgDeep, BgMid, LinearGradientMode.Vertical))
                g.FillRectangle(b, 0, BandH, FormW, FormH - BandH * 2);

            // Bottom dark band
            using (var b = new SolidBrush(BandDark))
                g.FillRectangle(b, 0, FormH - BandH, FormW, BandH);

            // Gold separator lines (band edges)
            using (var p = new Pen(GoldDim, 1f))
            {
                g.DrawLine(p, 50, BandH,          FormW - 50, BandH);
                g.DrawLine(p, 50, FormH - BandH,  FormW - 50, FormH - BandH);
            }

            // ── Top band text: form title ─────────────────────────────────────
            using (var f  = new Font("Segoe UI", 7.5F, FontStyle.Regular))
            using (var br = new SolidBrush(BandText))
            {
                var sf = new StringFormat
                {
                    Alignment     = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(
                    "SCHOOL MANAGEMENT SYSTEM  ·  v1.0.0",
                    f, br, new RectangleF(0, 0, FormW, BandH), sf);
            }

            // ── Bottom band: two footer lines ─────────────────────────────────
            using (var sf = new StringFormat
            {
                Alignment     = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                int bandTop = FormH - BandH;

                using (var f  = new Font("Segoe UI", 8F, FontStyle.Regular))
                using (var br = new SolidBrush(Color.FromArgb(125, 132, 180)))
                    g.DrawString(
                        "© 2024 Kingdom Preparatory School  ·  All Rights Reserved",
                        f, br, new RectangleF(0, bandTop, FormW, BandH * 0.52f), sf);

                using (var f  = new Font("Segoe UI", 8F, FontStyle.Regular))
                using (var br = new SolidBrush(Color.FromArgb(95, 102, 150)))
                    g.DrawString(
                        "DEVELOPED BY: DARKTEK IMPLECTION",
                        f, br, new RectangleF(0, bandTop + BandH * 0.50f, FormW, BandH * 0.50f), sf);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Heraldic shield crest (fallback when no logo image is assigned)
        // ─────────────────────────────────────────────────────────────────────
        private void PaintCrestFallback(object sender, PaintEventArgs e)
        {
            if (pictureBoxLogo.Image != null) return;

            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;

            int w = pictureBoxLogo.Width;
            int h = pictureBoxLogo.Height;
            const int pad = 5;
            var outer = new Rectangle(pad, pad, w - pad * 2, h - pad * 2);
            var inner = new Rectangle(pad + 6, pad + 6, w - (pad + 6) * 2, h - (pad + 6) * 2);

            // ── Shield fill ───────────────────────────────────────────────────
            using (var path = ShieldPath(outer))
            using (var fill = new LinearGradientBrush(outer,
                Color.FromArgb(22, 36, 100), Color.FromArgb(12, 20, 72),
                LinearGradientMode.Vertical))
                g.FillPath(fill, path);

            // ── Outer gold border ─────────────────────────────────────────────
            using (var path = ShieldPath(outer))
            using (var pen  = new Pen(AntiqueGold, 2.2f))
                g.DrawPath(pen, path);

            // ── Inner thin gold border ────────────────────────────────────────
            using (var path = ShieldPath(inner))
            using (var pen  = new Pen(Color.FromArgb(140, 212, 175, 55), 1f))
                g.DrawPath(pen, path);

            // ── "KPS" in classic serif ────────────────────────────────────────
            using (var font  = new Font("Georgia", 15F, FontStyle.Bold))
            using (var brush = new SolidBrush(AntiqueGold))
            {
                var sf = new StringFormat
                {
                    Alignment     = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                // Centre text in the upper 68 % of the shield (avoids the pointed tip)
                g.DrawString("KPS", font, brush,
                    new RectangleF(pad, pad, w - pad * 2, (h - pad * 2) * 0.68f), sf);
            }
        }

        /// <summary>Classic 5-point heraldic shield from a bounding rectangle.</summary>
        private static GraphicsPath ShieldPath(Rectangle r)
        {
            float cx = r.X + r.Width / 2f;
            var pts = new PointF[]
            {
                new PointF(r.X,     r.Y),                            // top-left
                new PointF(r.Right, r.Y),                            // top-right
                new PointF(r.Right, r.Y + r.Height * 0.62f),        // right shoulder
                new PointF(cx,      r.Bottom),                       // bottom point
                new PointF(r.X,     r.Y + r.Height * 0.62f),        // left shoulder
            };
            var p = new GraphicsPath();
            p.AddPolygon(pts);
            return p;
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Helper
        // ─────────────────────────────────────────────────────────────────────
        private static Label Lbl(string text, Font font, Color fore,
                                 Rectangle bounds, ContentAlignment align) =>
            new Label
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

        // ─────────────────────────────────────────────────────────────────────
        //  Life-cycle
        // ─────────────────────────────────────────────────────────────────────
        private void OnSplashLoad(object sender, EventArgs e) =>
            FadeIn(StartProgressAnimation);

        private void FadeIn(Action onComplete)
        {
            double opacity = 0;
            var t = new Timer { Interval = FadeInterval };
            t.Tick += (s, _) =>
            {
                opacity  = Math.Min(opacity + FadeStep, 1.0);
                Opacity  = opacity;
                if (opacity >= 1.0) { t.Stop(); t.Dispose(); onComplete?.Invoke(); }
            };
            t.Start();
        }

        private void StartProgressAnimation()
        {
            int tick = 0;
            _progressTimer = new Timer { Interval = ProgressInterval };
            _progressTimer.Tick += (s, _) =>
            {
                tick++;
                double pct = Math.Min((double)tick / TotalTicks, 1.0);

                _progressFill.Width = (int)(_progressFill.Parent.Width * pct);

                int msgIdx = Math.Min((int)(pct * StatusMessages.Length), StatusMessages.Length - 1);
                _statusLabel.Text  = StatusMessages[msgIdx];

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
            var t = new Timer { Interval = FadeInterval };
            t.Tick += (s, _) =>
            {
                opacity  = Math.Max(opacity - FadeStep, 0.0);
                Opacity  = opacity;
                if (opacity <= 0) { t.Stop(); t.Dispose(); onComplete?.Invoke(); }
            };
            t.Start();
        }

        private void LaunchLogin() { new frmlogin().Show(); Close(); }
    }
}
