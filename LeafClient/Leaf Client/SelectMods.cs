using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Leaf_Client
{
    public partial class SelectMods : Form
    {
        public SelectMods()
        {
            InitializeComponent();
        }

        bool designMode = (LicenseManager.UsageMode == LicenseUsageMode.Designtime);

        protected override CreateParams CreateParams
        {
            get
            {
                if (designMode == false)
                {

                    CreateParams handleParams = base.CreateParams;
                    handleParams.ExStyle |= 0x02000000;
                    return handleParams;

                }
                return null;
            }
        }

        private void btnNonePM_Click(object sender, EventArgs e)
        {
            if (CrossAccessible.Mods.Contains("Sodium"))
                CrossAccessible.Mods.Remove("Sodium");
            else if (CrossAccessible.Mods.Contains("Noxesium"))
                CrossAccessible.Mods.Remove("Noxesium");
            else if (CrossAccessible.Mods.Contains("Lithium"))
                CrossAccessible.Mods.Remove("Lithium");

            lbSelected.Text = lbSelected.Text.Replace("Sodium, \n", "");
            lbSelected.Text = lbSelected.Text.Replace("Noxesium, \n", "");
            lbSelected.Text = lbSelected.Text.Replace("Lithium, \n", "");
        }

        private void label4_Click(object sender, EventArgs e)
        {
            if (CrossAccessible.Mods.Contains("Sodium"))
                CrossAccessible.Mods.Remove("Sodium");
            else if (CrossAccessible.Mods.Contains("Noxesium"))
                CrossAccessible.Mods.Remove("Noxesium");
            else if (CrossAccessible.Mods.Contains("Lithium"))
                CrossAccessible.Mods.Remove("Lithium");

            lbSelected.Text = lbSelected.Text.Replace("Sodium, \n", "");
            lbSelected.Text = lbSelected.Text.Replace("Noxesium, \n", "");
            lbSelected.Text = lbSelected.Text.Replace("Lithium, \n", "");
        }

        private void btnNoneLS_Click(object sender, EventArgs e)
        {
            if (CrossAccessible.Mods.Contains("Iris"))
                CrossAccessible.Mods.Remove("Iris");
            else if (CrossAccessible.Mods.Contains("Phosphor"))
                CrossAccessible.Mods.Remove("Phosphor");
            else if (CrossAccessible.Mods.Contains("Starlight"))
                CrossAccessible.Mods.Remove("Starlight");

            lbSelected.Text = lbSelected.Text.Replace("Iris, \n", "");
            lbSelected.Text = lbSelected.Text.Replace("Phosphor, \n", "");
            lbSelected.Text = lbSelected.Text.Replace("Starlight, \n", "");
        }

        private void label11_Click(object sender, EventArgs e)
        {
            if (CrossAccessible.Mods.Contains("Iris"))
                CrossAccessible.Mods.Remove("Iris");
            else if (CrossAccessible.Mods.Contains("Phosphor"))
                CrossAccessible.Mods.Remove("Phosphor");
            else if (CrossAccessible.Mods.Contains("Starlight"))
                CrossAccessible.Mods.Remove("Starlight");

            lbSelected.Text = lbSelected.Text.Replace("Iris, \n", "");
            lbSelected.Text = lbSelected.Text.Replace("Phosphor, \n", "");
            lbSelected.Text = lbSelected.Text.Replace("Starlight, \n", "");
        }

        private void label5_Click(object sender, EventArgs e)
        {

            if (CrossAccessible.Mods.Contains("Sodium"))
            {

                lbSelected.Text = lbSelected.Text.Replace("Sodium, \n", "");
                CrossAccessible.Mods.Remove("Sodium");

            }
            else
            {

                lbSelected.Text += "Sodium, \n";
                CrossAccessible.Mods.Add("Sodium");

            }
        }

        private void label6_Click(object sender, EventArgs e)
        {
            if (CrossAccessible.Mods.Contains("Noxesium"))
            {

                lbSelected.Text = lbSelected.Text.Replace("Noxesium, \n", "");
                CrossAccessible.Mods.Remove("Noxesium");

            }
            else
            {

                lbSelected.Text += "Noxesium, \n";
                CrossAccessible.Mods.Add("Noxesium");

            }
        }

        private void label7_Click(object sender, EventArgs e)
        {
            if (CrossAccessible.Mods.Contains("Lithium"))
            {

                lbSelected.Text = lbSelected.Text.Replace("Lithium, \n", "");
                CrossAccessible.Mods.Remove("Lithium");

            }
            else
            {

                lbSelected.Text += "Lithium, \n";
                CrossAccessible.Mods.Add("Lithium");

            }
        }

        private void label10_Click(object sender, EventArgs e)
        {
            if (CrossAccessible.Mods.Contains("Starlight"))
                CrossAccessible.Mods.Remove("Starlight");
            else if (CrossAccessible.Mods.Contains("Phosphor"))
                CrossAccessible.Mods.Remove("Phosphor");

            if (CrossAccessible.Mods.Contains("Iris"))
            {

                lbSelected.Text = lbSelected.Text.Replace("Iris, \n", "");
                CrossAccessible.Mods.Remove("Iris");

            }
            else
            {

                lbSelected.Text += "Iris, \n";
                lbSelected.Text = lbSelected.Text.Replace("Phosphor, \n", "");
                lbSelected.Text = lbSelected.Text.Replace("Starlight, \n", "");
                CrossAccessible.Mods.Add("Iris");

            }
        }

        private void label9_Click(object sender, EventArgs e)
        {
            if (CrossAccessible.Mods.Contains("Starlight"))
                CrossAccessible.Mods.Remove("Starlight");
            else if (CrossAccessible.Mods.Contains("Iris"))
                CrossAccessible.Mods.Remove("Iris");


            if (CrossAccessible.Mods.Contains("Phosphor"))
            {

                lbSelected.Text = lbSelected.Text.Replace("Phosphor, \n", "");
                CrossAccessible.Mods.Remove("Phosphor");

            }
            else
            {

                lbSelected.Text += "Phosphor, \n";
                lbSelected.Text = lbSelected.Text.Replace("Starlight, \n", "");
                lbSelected.Text = lbSelected.Text.Replace("Iris, \n", "");
                CrossAccessible.Mods.Add("Phosphor");

            }

        }

        private void label13_Click(object sender, EventArgs e)
        {
            if (CrossAccessible.Mods.Contains("Phosphor"))
                CrossAccessible.Mods.Remove("Phosphor");
            else if (CrossAccessible.Mods.Contains("Iris"))
                CrossAccessible.Mods.Remove("Iris");

            if (CrossAccessible.Mods.Contains("Starlight"))
            {

                lbSelected.Text = lbSelected.Text.Replace("Starlight, \n", "");
                CrossAccessible.Mods.Remove("Starlight");

            }
            else
            {

                lbSelected.Text += "Starlight, \n";
                lbSelected.Text = lbSelected.Text.Replace("Phosphor, \n", "");
                lbSelected.Text = lbSelected.Text.Replace("Iris, \n", "");
                CrossAccessible.Mods.Add("Starlight");

            }
        }

        private void btnLaunch_Click(object sender, EventArgs e)
        {
            this.Close();
        }

    }
}
