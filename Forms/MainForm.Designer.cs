namespace EasyRCP
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        
        private Label labelEmail;
        private TextBox textEmail;
        private Label labelPassword;
        private TextBox textPassword;
        private Button buttonSave;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.labelEmail = new System.Windows.Forms.Label();
            this.textEmail = new System.Windows.Forms.TextBox();
            this.labelPassword = new System.Windows.Forms.Label();
            this.textPassword = new System.Windows.Forms.TextBox();
            this.buttonSave = new System.Windows.Forms.Button();
            this.SuspendLayout();

            // labelEmail
            this.labelEmail.AutoSize = true;
            this.labelEmail.Location = new System.Drawing.Point(12, 15);
            this.labelEmail.Text = "Adres e-mail:";

            // textEmail
            this.textEmail.Location = new System.Drawing.Point(100, 12);
            this.textEmail.Width = 200;

            // labelPassword
            this.labelPassword.AutoSize = true;
            this.labelPassword.Location = new System.Drawing.Point(12, 45);
            this.labelPassword.Text = "Hasło:";

            // textPassword
            this.textPassword.Location = new System.Drawing.Point(100, 42);
            this.textPassword.Width = 200;
            this.textPassword.UseSystemPasswordChar = true;

            // buttonSave
            this.buttonSave.Location = new System.Drawing.Point(100, 80);
            this.buttonSave.Text = "Zapisz";
            this.buttonSave.Click += new System.EventHandler(this.ButtonSave_Click);

            // Form1
            this.ClientSize = new System.Drawing.Size(320, 130);
            this.Controls.Add(this.labelEmail);
            this.Controls.Add(this.textEmail);
            this.Controls.Add(this.labelPassword);
            this.Controls.Add(this.textPassword);
            this.Controls.Add(this.buttonSave);
            this.Text = "Ustawienia";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}
