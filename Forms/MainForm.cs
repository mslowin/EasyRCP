using EasyRCP.Forms;
using EasyRCP.Services;

namespace EasyRCP;

public partial class MainForm : Form
{
    private NotifyIcon _trayIcon;

    private ContextMenuStrip _trayMenu;

    private RcpApiClient? _apiClient;

    private bool _isHidden;

    /// <summary>
    /// Main form is just a settings form that is hidden by default.
    /// </summary>
    /// <param name="isHidden">Indicates if the form should be run as hidden or not.</param>
    /// <param name="rcpApi">The api to connect to RCP.</param>
    public MainForm(bool isHidden, RcpApiClient? rcpApi = null)
    {
        InitializeComponent();
        this.StartPosition = FormStartPosition.CenterScreen;
        if (isHidden)
        {
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Visible = false;
        }

        _trayMenu = new ContextMenuStrip();
        _trayMenu.Items.Add("Rozpocznij pracê", null, (s, e) => StartWork());
        _trayMenu.Items.Add("Opcje", null, (s, e) => ShowSettings());
        _trayMenu.Items.Add("WyjdŸ", null, (s, e) => Environment.Exit(0));

        _trayIcon = new NotifyIcon();
        _trayIcon.Text = "EasyRCP";
        _trayIcon.ContextMenuStrip = _trayMenu;
        _trayIcon.Visible = true;
        byte[] iconBytes = Properties.Resources.RcpOnlineIcon;
        using (var ms = new MemoryStream(iconBytes))
        {
            _trayIcon.Icon = new Icon(ms);
        }

        _trayIcon.DoubleClick += (s, e) => this.Show();

        _isHidden = isHidden;
        _apiClient = rcpApi;
    }

    private void StartWork()
    {
        Task.Run(async () =>
        {
            if (_apiClient != null)
            {
                await RcpAutomationService.StartWorkAsync(_apiClient);
            }
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

    private async void ButtonSave_Click(object sender, EventArgs e)
    {
        if(string.IsNullOrEmpty(textEmail.Text) || string.IsNullOrEmpty(textPassword.Text))
        {
            MessageBox.Show(
                "Dane logowania nie mog¹ byæ puste.",
                "EasyRCP - B³¹d",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        UserCredentialsService.SaveCredentials(textEmail.Text, textPassword.Text);
        this.WindowState = FormWindowState.Minimized;
        this.ShowInTaskbar = false;
        this.Visible = false;

        if (_apiClient != null)
        {
            // TODO: tutaj chyba po prostu powinno wychodziæ a nie pytaæ, czy rozpocz¹æ pracê, ewentualnie sprawdzaæ,
            // czy osoba jest na stanowisku i jeœli nie, to dopiero pytaæ! <-------------------------------------------------------------
            using var prompt = new StartWorkPromptForm();
            if (prompt.ShowDialog() == DialogResult.Yes)
            {
                await RcpAutomationService.StartWorkAsync(_apiClient);
            }
        }
        else
        {
            // If the _apiClient is null it just means there were no credentials and this needs to run only once and quit - returning to Program.cs
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_isHidden)
        {
            // Application should cancel the form closing only in the final (hidden) mode.
            // The credentials dialogs (that have _isHidden false) should just close the app on the X button
            e.Cancel = true;
            this.Hide();
        }
        else
        {
            // Dispose the NotifyIcon to remove it from the system tray
            _trayIcon.Dispose();
        }
        base.OnFormClosing(e);
    }
}
