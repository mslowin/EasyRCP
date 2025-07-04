namespace EasyRCP.Forms
{
    /// <summary>
    /// Represents a form that prompts the user to start work, providing Yes and No options.
    /// </summary>
    public partial class StartWorkPromptForm : Form
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StartWorkPromptForm"/> class.
        /// Sets the form's start position to the center of the screen.
        /// </summary>
        public StartWorkPromptForm()
        {
            InitializeComponent();
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        /// <summary>
        /// Handles the Click event of the Yes button.
        /// Sets the dialog result to Yes and closes the form.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
        private void buttonYes_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Yes;
            this.Close();
        }

        /// <summary>
        /// Handles the Click event of the No button.
        /// Sets the dialog result to No and closes the form.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
        private void buttonNo_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.No;
            this.Close();
        }
    }
}
