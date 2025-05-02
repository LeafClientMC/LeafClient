/*
 * 
 *       🌿 LeafClientMC™️ 2024
 *       All the code on this project was written by ZiAD on GitHub.
 *       Some code snippets were taken from stackoverflow.com (don't come at me, all developers do that).
 * 
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinBlur;
using static WinBlur.UI;

namespace Leaf_Client
{
    public partial class IntroForm : Form
    {

        public IntroForm()
        {
            InitializeComponent();
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

        private void IntroForm_Load(object sender, EventArgs e)
        {

            Random rand = new Random();

            var timer = new System.Windows.Forms.Timer
            {
                Interval = rand.Next(3000, 7000)
            };

            timer.Tick += (sender, e) =>
                {
                    timer.Stop();

                    this.Hide();
                    ToastForm toast = new ToastForm();
                    toast.Show();

                };

            timer.Start();
        }
    }
}
