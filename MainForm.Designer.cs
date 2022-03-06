namespace ZumaBot2 {
    partial class MainForm {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            this.tmrDetect = new System.Windows.Forms.Timer(this.components);
            this.pbxIn = new System.Windows.Forms.PictureBox();
            this.pbxMid = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pbxIn)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbxMid)).BeginInit();
            this.SuspendLayout();
            // 
            // tmrDetect
            // 
            this.tmrDetect.Interval = 2000;
            // 
            // pbxIn
            // 
            this.pbxIn.Location = new System.Drawing.Point(12, 12);
            this.pbxIn.Name = "pbxIn";
            this.pbxIn.Size = new System.Drawing.Size(320, 240);
            this.pbxIn.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbxIn.TabIndex = 0;
            this.pbxIn.TabStop = false;
            // 
            // pbxMid
            // 
            this.pbxMid.Location = new System.Drawing.Point(338, 12);
            this.pbxMid.Name = "pbxMid";
            this.pbxMid.Size = new System.Drawing.Size(320, 240);
            this.pbxMid.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbxMid.TabIndex = 1;
            this.pbxMid.TabStop = false;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.pbxMid);
            this.Controls.Add(this.pbxIn);
            this.Name = "MainForm";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.MainForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.pbxIn)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbxMid)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Timer tmrDetect;
        private PictureBox pbxIn;
        private PictureBox pbxMid;
    }
}