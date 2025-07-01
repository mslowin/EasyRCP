using EasyRCP.Forms;
using EasyRCP.Services;
using Timer = System.Windows.Forms.Timer;

namespace EasyRCP;

public partial class MainForm : Form
{
    private RcpApiClient? _apiClient;
    private bool _isHidden;

    private NotifyIcon _trayIcon;
    private ContextMenuStrip _trayMenu;

    /// <summary>
    /// The label in the tray menu that displays the current work status.
    /// </summary>
    private ToolStripLabel _statusLabel;

    /// <summary>
    /// The menu item in the tray menu that allows the user to start work.
    /// </summary>
    private ToolStripMenuItem _startWorkMenuItem;

    /// <summary>
    /// The menu item in the tray menu that allows the user to end already started work.
    /// </summary>
    private ToolStripMenuItem _endWorkMenuItem;

    /// <summary>
    /// Timer used to update the elapsed work time in the tray menu.
    /// </summary>
    private Timer _workTimer;

    /// <summary>
    /// The timestamp representing when the work started.
    /// </summary>
    private DateTime? _workStartTime;

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

        // tekst na górze czy praca rejestrowana, czy nie
        _statusLabel = new ToolStripLabel();
        _statusLabel.ForeColor = Color.Green;
        _statusLabel.Font = new Font(_statusLabel.Font, FontStyle.Bold);
        _trayMenu.Items.Add(_statusLabel);
        _trayMenu.Items.Add(new ToolStripSeparator());

        // przyciski w trayu
        _startWorkMenuItem = new ToolStripMenuItem("Rozpocznij pracê", null, async (s, e) => await StartWork());
        _startWorkMenuItem.ForeColor = Color.Green;
        _trayMenu.Items.Add(_startWorkMenuItem);
        _endWorkMenuItem = new ToolStripMenuItem("Zakoñcz pracê", null, async (s, e) => await EndWork());
        _endWorkMenuItem.ForeColor = Color.Red;
        _trayMenu.Items.Add(_endWorkMenuItem);
        _trayMenu.Items.Add("Opcje", null, (s, e) => ShowSettings());
        _trayMenu.Items.Add("WyjdŸ", null, (s, e) => Environment.Exit(0));

        // Ikona/przycisk w trayu
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

        // timer do zliczania czasu pracy
        _workTimer = new Timer();
        _workTimer.Interval = 1000;
        _workTimer.Tick += WorkTimer_Tick;

        _isHidden = isHidden;
        _apiClient = rcpApi;
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        await UpdateWorkStatusAsync();
    }

    private async Task StartWork()
    {
        if (_apiClient != null)
        {
            await RcpAutomationService.StartWorkAsync(_apiClient);
            await UpdateWorkStatusAsync(true);
        }
    }

    private async Task EndWork()
    {
        if (_apiClient != null)
        {
            await RcpAutomationService.EndWork(_apiClient);
            await UpdateWorkStatusAsync(false);
        }
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

    /// <summary>
    /// Checks if the work has already started and updates tray items accordingly.
    /// </summary>
    /// <param name="isWorking">Optional bool, if it is provided, the method will not check for work status in RCP system.</param>
    private async Task UpdateWorkStatusAsync(bool? isWorking = null)
    {
        if (_apiClient == null)
            return;

        if (isWorking == null)
        {
            isWorking = await RcpAutomationService.CheckIfWorkAlreadyStartedAsync(_apiClient);
        }

        _startWorkMenuItem.Visible = !isWorking.GetValueOrDefault();
        _endWorkMenuItem.Visible = isWorking.GetValueOrDefault();

        if (isWorking.GetValueOrDefault())
        {
            _statusLabel.ForeColor = Color.Green;
            var latestActivity = await RcpAutomationService.GetLatestActivityAsync(_apiClient);
            if (latestActivity.HasValue)
            {
                _workStartTime = latestActivity.Value;
                _workTimer.Start();
            }
            else
            {
                _statusLabel.Text = "Czas pracy jest rejestrowany";
                _workTimer.Stop();
            }
        }
        else
        {
            _statusLabel.ForeColor = Color.Red;
            _statusLabel.Text = "Czas pracy nie jest rejestrowany";
            _workTimer.Stop();
        }
    }

    private void WorkTimer_Tick(object? sender, EventArgs e)
    {
        if (_workStartTime.HasValue)
        {
            var elapsed = DateTime.UtcNow - _workStartTime.Value.ToUniversalTime();
            _statusLabel.Text = $"Czas pracy jest rejestrowany {elapsed:hh\\:mm\\:ss}";
        }
    }

    private async void ButtonSave_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(textEmail.Text) || string.IsNullOrEmpty(textPassword.Text))
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
            bool isWorking = await RcpAutomationService.CheckIfWorkAlreadyStartedAsync(_apiClient);
            if (!isWorking)
            {
                using var prompt = new StartWorkPromptForm();
                if (prompt.ShowDialog() == DialogResult.Yes)
                {
                    await RcpAutomationService.StartWorkAsync(_apiClient);
                }
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
