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
        private Timer timer1;

        public load()
        {
            InitializeComponent();
            UiTheme.Apply(this);
            InitializeTimer();
        }

        private void InitializeTimer()
        {
            timer1 = new Timer();
            timer1.Interval = 500; // Set the interval (in milliseconds) as needed
            timer1.Tick += new EventHandler(timer1_Tick);
            timer1.Start();
        }

        private void load_Load(object sender, EventArgs e)
        {
            // Any initialization code for the form load event
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            loader.Width += 25;
            if (loader.Width >= 599)
            {
                timer1.Stop();
                frmlogin frm = new frmlogin();
                frm.Show();
                this.Hide();
            }
        }
    }
}
