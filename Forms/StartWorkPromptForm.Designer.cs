namespace EasyRCP.Forms
{
    partial class StartWorkPromptForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label labelQuestion;
        private System.Windows.Forms.Button buttonYes;
        private System.Windows.Forms.Button buttonNo;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;

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
            this.labelQuestion = new System.Windows.Forms.Label();
            this.buttonYes = new System.Windows.Forms.Button();
            this.buttonNo = new System.Windows.Forms.Button();
            this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // labelQuestion
            // 
            this.labelQuestion.Dock = System.Windows.Forms.DockStyle.Top;
            this.labelQuestion.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.labelQuestion.Location = new System.Drawing.Point(0, 0);
            this.labelQuestion.Name = "labelQuestion";
            this.labelQuestion.Padding = new System.Windows.Forms.Padding(10);
            this.labelQuestion.Size = new System.Drawing.Size(384, 60);
            this.labelQuestion.TabIndex = 0;
            this.labelQuestion.Text = "Czy zarejestrować rozpoczęcie pracy?";
            this.labelQuestion.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // buttonYes
            // 
            ////this.buttonYes.Image = Properties.Resources.PlayIcon; // green triangle icon
            ////this.buttonYes.Image = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "play.png"));
            this.buttonYes.TextImageRelation = TextImageRelation.ImageBeforeText;
            this.buttonYes.BackColor = System.Drawing.Color.LightGreen;
            this.buttonYes.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonYes.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.buttonYes.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.buttonYes.Name = "buttonYes";
            this.buttonYes.Size = new System.Drawing.Size(100, 50);
            this.buttonYes.TabIndex = 1;
            this.buttonYes.Text = "TAK";
            this.buttonYes.UseVisualStyleBackColor = false;
            this.buttonYes.Anchor = AnchorStyles.None;
            this.buttonYes.Click += new System.EventHandler(this.buttonYes_Click);
            // 
            // buttonNo
            // 
            ////this.buttonNo.Image = Properties.Resources.CrossIcon; // red cross icon
            ////this.buttonYes.Image = Image.FromFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "cross.png"));
            this.buttonNo.BackColor = System.Drawing.Color.LightCoral;
            this.buttonNo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonNo.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.buttonNo.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.buttonNo.Name = "buttonNo";
            this.buttonNo.Size = new System.Drawing.Size(100, 50);
            this.buttonNo.TabIndex = 2;
            this.buttonNo.Text = "NIE";
            this.buttonNo.UseVisualStyleBackColor = false;
            this.buttonNo.Anchor = AnchorStyles.None;
            this.buttonNo.Click += new System.EventHandler(this.buttonNo_Click);
            // 
            // tableLayoutPanel
            // 
            this.tableLayoutPanel.Anchor = AnchorStyles.Top;
            this.tableLayoutPanel.ColumnCount = 2;
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel.Controls.Add(this.buttonYes, 0, 0);
            this.tableLayoutPanel.Controls.Add(this.buttonNo, 1, 0);
            this.tableLayoutPanel.Location = new System.Drawing.Point(0, 70);
            this.tableLayoutPanel.Name = "tableLayoutPanel";
            this.tableLayoutPanel.RowCount = 1;
            this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60F));
            this.tableLayoutPanel.Size = new System.Drawing.Size(384, 60);
            this.tableLayoutPanel.TabIndex = 3;
            // 
            // StartWorkPromptForm
            // 
            this.AcceptButton = this.buttonYes;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(384, 161);
            this.ControlBox = false;
            this.Controls.Add(this.tableLayoutPanel);
            this.Controls.Add(this.labelQuestion);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "StartWorkPromptForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Start pracy";
            this.TopMost = true;
            this.tableLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion
    }
}