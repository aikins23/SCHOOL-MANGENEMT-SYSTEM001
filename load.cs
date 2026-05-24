using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace kingdom_Preparatory_School_Management_System
{
    public partial class load : Form
    {
        private Timer animationTimer;
        private int dotAnimationTicks = 0;
        private int dotOpacityPhase = 0;
        private int totalLoadingTicks = 0;
        private int maxLoadingTicks = 100; // ~5 seconds at 50ms

        public load()
        {
            InitializeComponent();
            UiTheme.Apply(this);
            // Animation initialized in load_Load
        }

        private void load_Load(object sender, EventArgs e)
        {
            try
            {
                // Load school background image - consider applying blur effect for better text readability
                this.BackgroundImage = Properties.Resources.school_background;
            }
            catch
            {
                this.BackColor = System.Drawing.Color.White;
            }
            InitializeSplashAnimation();
        }

        private void InitializeSplashAnimation()
        {
            animationTimer = new Timer();
            animationTimer.Interval = 50;
            animationTimer.Tick += AnimationTimer_Tick;
            animationTimer.Start();
            dotAnimationTicks = 0;
            totalLoadingTicks = 0;
        }

        private void AnimationTimer_Tick(object sender, EventArgs e)
        {
            dotAnimationTicks++;
            totalLoadingTicks++;

            int phasePosition = dotAnimationTicks % 24;
            UpdateDotAnimation(phasePosition);

            if (totalLoadingTicks >= maxLoadingTicks)
            {
                animationTimer.Stop();
                FadeOutAndShowLogin();
            }
        }

        private void UpdateDotAnimation(int phasePosition)
        {
            if (phasePosition >= 0 && phasePosition <= 23)
            {
                if (Controls.ContainsKey("labelLoadingDots"))
                {
                    Control labelLoadingDots = Controls["labelLoadingDots"];
                    labelLoadingDots.Text = "● ● ● ●";
                    labelLoadingDots.Visible = true;
                }
            }
        }

        private void FadeOutAndShowLogin()
        {
            Timer fadeTimer = new Timer();
            fadeTimer.Interval = 30;
            int opacityLevel = 10;

            fadeTimer.Tick += (s, e) =>
            {
                opacityLevel -= 1;
                if (opacityLevel <= 0)
                {
                    fadeTimer.Stop();
                    new frmlogin().Show();
                    this.Hide();
                }
                else
                {
                    this.Opacity = opacityLevel / 10.0;
                }
            };

            fadeTimer.Start();
        }

    }
}
