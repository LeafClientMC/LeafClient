using DiscordRPC;
using DiscordRPC.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Leaf_Client
{
    public partial class ToastForm : Form
    {

        int toastX, toastY;

        public ToastForm()
        {
            InitializeComponent();

            Position();
        }

        private static string checkPlayer()
        {

            if (Properties.Settings.Default.SessionInfo != null)
            {

                return $"Playing as {Properties.Settings.Default.SessionInfo.Username}";

            }
            else if (Properties.Settings.Default.OfflineSession != null)
            {

                return $"Playing as {Properties.Settings.Default.OfflineSession.Username}";

            }

            return "User is not logged into any account";

        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams handleParams = base.CreateParams;
                handleParams.ExStyle |= 0x02000000;
                return handleParams;
            }
        }

        private void guna2PictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void checkIfLoaded_Tick(object sender, EventArgs e)
        {
            if (CrossAccessible.doneLoading == true)
            {

                checkIfLoaded.Stop();

                this.Close();

            }
        }

        private void ToastForm_Load(object sender, EventArgs e)
        {

        }

        private void ToastForm_Shown(object sender, EventArgs e)
        {

        }

        private void btnDiscord_Click(object sender, EventArgs e)
        {
            Process.Start("https://direct-link.net/420207/leafclient-discord-server");
        }

        private void btnInstagram_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.instagram.com/leafclient");
        }

        private void Position()
        {

            int ScreenWidth = Screen.PrimaryScreen.WorkingArea.Width;
            int ScreenHeight = Screen.PrimaryScreen.WorkingArea.Height;

            toastX = ScreenWidth - this.Width - 5;
            toastY = ScreenHeight - this.Height - 10;

            this.Location = new Point(toastX, toastY);

        }

    }
}
