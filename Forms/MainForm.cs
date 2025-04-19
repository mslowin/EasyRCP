using EasyRCP.Services;

namespace EasyRCP;

public partial class MainForm : Form
{
    private NotifyIcon trayIcon;
    private ContextMenuStrip trayMenu;

    /// <summary>
    /// Main form is just a settings form that is hidden by default.
    /// </summary>
    public MainForm()
    {
        InitializeComponent();
        this.StartPosition = FormStartPosition.CenterScreen;
        this.WindowState = FormWindowState.Minimized;
        this.ShowInTaskbar = false;
        this.Visible = false;

        trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Rozpocznij pracê", null, (s, e) => StartWork());
        trayMenu.Items.Add("Opcje", null, (s, e) => ShowSettings());
        trayMenu.Items.Add("WyjdŸ", null, (s, e) => Application.Exit());

        trayIcon = new NotifyIcon();
        trayIcon.Text = "EasyRCP";
        trayIcon.Icon = new Icon("Resources/RcpOnlineIcon.ico");
        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.Visible = true;

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
        UserCredentialsService.SaveCredentials(textEmail.Text, textPassword.Text);
        this.WindowState = FormWindowState.Minimized;
        this.ShowInTaskbar = false;
        this.Visible = false;
        ////MessageBox.Show("Dane zapisane.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        e.Cancel = true;
        this.Hide();
        base.OnFormClosing(e);
    }
}
