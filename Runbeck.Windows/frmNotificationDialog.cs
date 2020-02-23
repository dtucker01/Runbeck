using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Runbeck.Windows
{
    public partial class frmNotificationDialog : Form
    {
        /// <summary>
        /// Dialog to present to user.
        /// </summary>
        /// <param name="message">Message to show to user.</param>
        /// <param name="bgColor">Color of the Dialog box.</param>
        public frmNotificationDialog(string message, Color bgColor)
        {
            InitializeComponent();

            this.BackColor = bgColor;
            lblMessage.Text = message;
        }

        //Position the dialog box in the upper left of screen and start timer.
        private void frmNotificationDialog_Load(object sender, EventArgs e)
        {
            Top = 20;
            Left = Screen.PrimaryScreen.Bounds.Width - Width - 20;
            timerClose.Start();
        }

        private void timerClose_Tick(object sender, EventArgs e)
        {
            Close();
        }
    }
}
