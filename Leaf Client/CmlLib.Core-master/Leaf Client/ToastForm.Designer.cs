namespace Leaf_Client
{
    partial class ToastForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.guna2Shapes1 = new Guna.UI2.WinForms.Guna2Shapes();
            this.label3 = new System.Windows.Forms.Label();
            this.btnWebsite = new Guna.UI2.WinForms.Guna2PictureBox();
            this.btnInstagram = new Guna.UI2.WinForms.Guna2PictureBox();
            this.btnDiscord = new Guna.UI2.WinForms.Guna2PictureBox();
            this.guna2PictureBox2 = new Guna.UI2.WinForms.Guna2PictureBox();
            this.guna2ShadowForm1 = new Guna.UI2.WinForms.Guna2ShadowForm(this.components);
            this.guna2Elipse1 = new Guna.UI2.WinForms.Guna2Elipse(this.components);
            this.checkIfLoaded = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.btnWebsite)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnInstagram)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnDiscord)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.guna2PictureBox2)).BeginInit();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.Font = new System.Drawing.Font("Bahnschrift", 10F);
            this.label2.ForeColor = System.Drawing.Color.DarkGray;
            this.label2.Location = new System.Drawing.Point(8, 39);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(315, 34);
            this.label2.TabIndex = 10;
            this.label2.Text = "LEAF CLIENT IS LOADING IN THE BACKGROUND,\r\nPLEASE WAIT.";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Bahnschrift Condensed", 20F, System.Drawing.FontStyle.Bold);
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(4, 6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(232, 33);
            this.label1.TabIndex = 11;
            this.label1.Text = "WELCOME TO LEAF CLIENT";
            // 
            // guna2Shapes1
            // 
            this.guna2Shapes1.BackColor = System.Drawing.Color.Transparent;
            this.guna2Shapes1.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(80)))), ((int)(((byte)(80)))));
            this.guna2Shapes1.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(60)))), ((int)(((byte)(60)))));
            this.guna2Shapes1.Location = new System.Drawing.Point(257, -190);
            this.guna2Shapes1.Name = "guna2Shapes1";
            this.guna2Shapes1.PolygonSkip = 1;
            this.guna2Shapes1.Rotate = 0F;
            this.guna2Shapes1.Size = new System.Drawing.Size(426, 491);
            this.guna2Shapes1.TabIndex = 12;
            this.guna2Shapes1.Text = "guna2Shapes1";
            this.guna2Shapes1.UseTransparentBackground = true;
            this.guna2Shapes1.Zoom = 80;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.Color.Transparent;
            this.label3.Font = new System.Drawing.Font("Bahnschrift", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.ForeColor = System.Drawing.Color.DarkGray;
            this.label3.Location = new System.Drawing.Point(8, 83);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(299, 16);
            this.label3.TabIndex = 13;
            this.label3.Text = "WHILE YOU\'RE WAITING CHECK OUT OUR SOCIALS:";
            // 
            // btnWebsite
            // 
            this.btnWebsite.BackColor = System.Drawing.Color.Transparent;
            this.btnWebsite.Image = global::Leaf_Client.Properties.Resources.Website;
            this.btnWebsite.ImageRotate = 0F;
            this.btnWebsite.Location = new System.Drawing.Point(98, 107);
            this.btnWebsite.Name = "btnWebsite";
            this.btnWebsite.Size = new System.Drawing.Size(37, 35);
            this.btnWebsite.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.btnWebsite.TabIndex = 16;
            this.btnWebsite.TabStop = false;
            this.btnWebsite.UseTransparentBackground = true;
            // 
            // btnInstagram
            // 
            this.btnInstagram.BackColor = System.Drawing.Color.Transparent;
            this.btnInstagram.Image = global::Leaf_Client.Properties.Resources.Instagram;
            this.btnInstagram.ImageRotate = 0F;
            this.btnInstagram.Location = new System.Drawing.Point(55, 107);
            this.btnInstagram.Name = "btnInstagram";
            this.btnInstagram.Size = new System.Drawing.Size(37, 35);
            this.btnInstagram.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.btnInstagram.TabIndex = 15;
            this.btnInstagram.TabStop = false;
            this.btnInstagram.UseTransparentBackground = true;
            this.btnInstagram.Click += new System.EventHandler(this.btnInstagram_Click);
            // 
            // btnDiscord
            // 
            this.btnDiscord.BackColor = System.Drawing.Color.Transparent;
            this.btnDiscord.Image = global::Leaf_Client.Properties.Resources.Discord_Bubble;
            this.btnDiscord.ImageRotate = 0F;
            this.btnDiscord.Location = new System.Drawing.Point(12, 107);
            this.btnDiscord.Name = "btnDiscord";
            this.btnDiscord.Size = new System.Drawing.Size(37, 35);
            this.btnDiscord.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.btnDiscord.TabIndex = 14;
            this.btnDiscord.TabStop = false;
            this.btnDiscord.UseTransparentBackground = true;
            this.btnDiscord.Click += new System.EventHandler(this.btnDiscord_Click);
            // 
            // guna2PictureBox2
            // 
            this.guna2PictureBox2.BackColor = System.Drawing.Color.Transparent;
            this.guna2PictureBox2.Image = global::Leaf_Client.Properties.Resources.leaf_client_fade;
            this.guna2PictureBox2.ImageRotate = 0F;
            this.guna2PictureBox2.Location = new System.Drawing.Point(327, 20);
            this.guna2PictureBox2.Name = "guna2PictureBox2";
            this.guna2PictureBox2.Size = new System.Drawing.Size(89, 94);
            this.guna2PictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.guna2PictureBox2.TabIndex = 2;
            this.guna2PictureBox2.TabStop = false;
            this.guna2PictureBox2.UseTransparentBackground = true;
            this.guna2PictureBox2.Click += new System.EventHandler(this.guna2PictureBox2_Click);
            // 
            // guna2ShadowForm1
            // 
            this.guna2ShadowForm1.BorderRadius = 20;
            this.guna2ShadowForm1.TargetForm = this;
            // 
            // guna2Elipse1
            // 
            this.guna2Elipse1.BorderRadius = 20;
            this.guna2Elipse1.TargetControl = this;
            // 
            // checkIfLoaded
            // 
            this.checkIfLoaded.Enabled = true;
            this.checkIfLoaded.Interval = 1000;
            this.checkIfLoaded.Tick += new System.EventHandler(this.checkIfLoaded_Tick);
            // 
            // ToastForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(50)))), ((int)(((byte)(50)))), ((int)(((byte)(50)))));
            this.BackgroundImage = global::Leaf_Client.Properties.Resources.minecraft_shaders_night_lights_hd_wallpaper_preview__1_;
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.ClientSize = new System.Drawing.Size(410, 151);
            this.Controls.Add(this.btnWebsite);
            this.Controls.Add(this.btnInstagram);
            this.Controls.Add(this.btnDiscord);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.guna2PictureBox2);
            this.Controls.Add(this.guna2Shapes1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.label2);
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "ToastForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.TopMost = true;
            this.Load += new System.EventHandler(this.ToastForm_Load);
            this.Shown += new System.EventHandler(this.ToastForm_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.btnWebsite)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnInstagram)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.btnDiscord)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.guna2PictureBox2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Guna.UI2.WinForms.Guna2PictureBox guna2PictureBox2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private Guna.UI2.WinForms.Guna2Shapes guna2Shapes1;
        private System.Windows.Forms.Label label3;
        private Guna.UI2.WinForms.Guna2PictureBox btnDiscord;
        private Guna.UI2.WinForms.Guna2PictureBox btnInstagram;
        private Guna.UI2.WinForms.Guna2PictureBox btnWebsite;
        private Guna.UI2.WinForms.Guna2ShadowForm guna2ShadowForm1;
        private Guna.UI2.WinForms.Guna2Elipse guna2Elipse1;
        private System.Windows.Forms.Timer checkIfLoaded;
    }
}