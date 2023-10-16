namespace TestForm
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            LoginButton = new Button();
            SuspendLayout();
            // 
            // LoginButton
            // 
            LoginButton.BackColor = SystemColors.GradientInactiveCaption;
            LoginButton.ForeColor = SystemColors.ActiveCaptionText;
            LoginButton.Location = new Point(210, 121);
            LoginButton.Name = "LoginButton";
            LoginButton.Size = new Size(107, 34);
            LoginButton.TabIndex = 0;
            LoginButton.Text = "Log in";
            LoginButton.UseVisualStyleBackColor = false;
            LoginButton.Click += LoginButton_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(329, 167);
            Controls.Add(LoginButton);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
        }

        #endregion

        private Button LoginButton;
    }
}