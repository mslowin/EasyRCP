using EasyRCP.Forms;
using EasyRCP.Services;

namespace EasyRCP;

public partial class MainForm : Form
{
    private NotifyIcon trayIcon;
    private ContextMenuStrip trayMenu;

    /// <summary>
    /// Main form is just a settings form that is hidden by default.
    /// </summary>
    public MainForm(bool isHidden)
    {
        InitializeComponent();
        this.StartPosition = FormStartPosition.CenterScreen;
        if (isHidden)
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
        }

        trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Rozpocznij pracê", null, (s, e) => StartWork());
        trayMenu.Items.Add("Opcje", null, (s, e) => ShowSettings());
        trayMenu.Items.Add("WyjdŸ", null, (s, e) => Environment.Exit(0));

        trayIcon = new NotifyIcon();
        trayIcon.Text = "EasyRCP";
        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.Visible = true;
        byte[] iconBytes = Properties.Resources.RcpOnlineIcon;
        using (var ms = new MemoryStream(iconBytes))
        {
            trayIcon.Icon = new Icon(ms);
        }

        trayIcon.DoubleClick += (s, e) => this.Show();
    }

    private static void StartWork()
    {
        Task.Run(() =>
        {
            RcpAutomationService.StartWork();
        });
    }

    private void ShowSettings()
    {
        var credentials = UserCredentialsService.LoadCredentials();
        this.textEmail.Text = credentials?.Email ?? string.Empty;
        this.textPassword.Text = credentials?.Password ?? string.Empty;

        this.Invoke((MethodInvoker)(() =>
        {
            this.Visible = true;
            this.ShowInTaskbar = true;
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
        }));
    }

    private void ButtonSave_Click(object sender, EventArgs e)
    {
        if(string.IsNullOrEmpty(textEmail.Text) || string.IsNullOrEmpty(textPassword.Text))
        {
            MessageBox.Show("Dane logowania nie mog¹ byæ puste.", "B³¹d", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        UserCredentialsService.SaveCredentials(textEmail.Text, textPassword.Text);
        this.WindowState = FormWindowState.Minimized;
        this.ShowInTaskbar = false;
        this.Visible = false;

        using var prompt = new StartWorkPromptForm();
        if (prompt.ShowDialog() == DialogResult.Yes)
        {
            RcpAutomationService.StartWork();
        }
        ////MessageBox.Show("Dane zapisane.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        e.Cancel = true;
        this.Hide();
        base.OnFormClosing(e);
    }
}
