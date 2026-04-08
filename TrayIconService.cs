using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

public class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly string _url;
    private readonly Icon? _appIcon;
    private Form? _outputForm;
    private TextBox? _outputTextBox;
    private readonly StringBuilder _outputLog = new();

    public TrayIconService(IHostApplicationLifetime lifetime, IConfiguration configuration)
    {
        Console.WriteLine("🔧 Initializing TrayIconService...");
        _lifetime = lifetime;

        // Get the URL from configuration
        var urls = configuration["ASPNETCORE_URLS"] ?? "http://localhost:5000";
        _url = urls.Split(';')[0]; // Take the first URL if multiple are configured
        Console.WriteLine($"🌐 Web URL: {_url}");

        _appIcon = LoadIcon();

        _notifyIcon = new NotifyIcon
        {
            Icon = _appIcon,
            Visible = true,
            Text = "DVR Lab - Running"
        };
        Console.WriteLine("✅ NotifyIcon created and made visible");

        // Create context menu with icons
        var contextMenu = new ContextMenuStrip();

        // "View your DVR Lab" menu item with globe icon
        var viewDvrItem = new ToolStripMenuItem("🌐 View your DVR Lab", null, (s, e) => OpenBrowser());
        contextMenu.Items.Add(viewDvrItem);

        // "View app output" menu item with document icon
        var viewOutputItem = new ToolStripMenuItem("📄 View app output", null, (s, e) => ShowOutputWindow());
        contextMenu.Items.Add(viewOutputItem);

        contextMenu.Items.Add("-"); // Separator

        // "Exit" menu item with X icon
        var exitItem = new ToolStripMenuItem("❌ Exit", null, (s, e) => Exit());
        contextMenu.Items.Add(exitItem);

        _notifyIcon.ContextMenuStrip = contextMenu;
        _notifyIcon.DoubleClick += (s, e) => OpenBrowser();
        Console.WriteLine("✅ Context menu configured");

        // Capture console output
        CaptureConsoleOutput();

        // Show a notification when the app starts
        _notifyIcon.ShowBalloonTip(
            3000,
            "DVR Lab Started",
            $"DVR Lab is running in the system tray.\nDouble-click to open the web interface.",
            ToolTipIcon.Info
        );
        Console.WriteLine("✅ TrayIconService initialization complete!");
    }

    private Icon LoadIcon()
    {
        // Try to load the application icon
        try
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "dvr lab.ico");
            Console.WriteLine($"Looking for icon at: {iconPath}");
            Console.WriteLine($"Icon file exists: {File.Exists(iconPath)}");

            if (File.Exists(iconPath))
            {
                var icon = new Icon(iconPath);
                Console.WriteLine("✅ Icon loaded successfully!");
                return icon;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error loading icon: {ex.Message}");
        }

        // Fallback to default icon
        Console.WriteLine("⚠️ Using default system icon");
        return SystemIcons.Application;
    }

    private void CaptureConsoleOutput()
    {
        // Redirect console output to our log
        var originalOut = Console.Out;
        var writer = new StringWriter(_outputLog);
        var multiWriter = new MultiTextWriter(originalOut, writer);
        Console.SetOut(multiWriter);
    }

    private void ShowOutputWindow()
    {
        if (_outputForm == null || _outputForm.IsDisposed)
        {
            _outputForm = new Form
            {
                Text = "DVR Lab - Application Output",
                Width = 800,
                Height = 600,
                StartPosition = FormStartPosition.CenterScreen,
                Icon = _appIcon
            };

            _outputTextBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 9),
                BackColor = Color.Black,
                ForeColor = Color.LimeGreen,
                Text = _outputLog.ToString()
            };

            _outputForm.Controls.Add(_outputTextBox);

            // Add a timer to refresh the output every second
            var refreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            refreshTimer.Tick += (s, e) =>
            {
                if (_outputTextBox != null && !_outputTextBox.IsDisposed)
                {
                    var currentText = _outputLog.ToString();
                    if (_outputTextBox.Text != currentText)
                    {
                        _outputTextBox.Text = currentText;
                        _outputTextBox.SelectionStart = _outputTextBox.Text.Length;
                        _outputTextBox.ScrollToCaret();
                    }
                }
            };
            refreshTimer.Start();

            _outputForm.FormClosing += (s, e) =>
            {
                refreshTimer.Stop();
                refreshTimer.Dispose();
            };
        }

        _outputForm.Show();
        _outputForm.BringToFront();
    }

    private void OpenBrowser()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = _url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open browser: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void Exit()
    {
        _outputForm?.Close();
        _notifyIcon.Visible = false;
        _lifetime.StopApplication();
        Application.Exit();
    }

    public void Dispose()
    {
        _outputForm?.Dispose();
        _notifyIcon?.Dispose();
        _appIcon?.Dispose();
    }
}

// Helper class to write to multiple TextWriters
public class MultiTextWriter : TextWriter
{
    private readonly TextWriter[] _writers;

    public MultiTextWriter(params TextWriter[] writers)
    {
        _writers = writers;
    }

    public override Encoding Encoding => Encoding.UTF8;

    public override void Write(char value)
    {
        foreach (var writer in _writers)
            writer.Write(value);
    }

    public override void WriteLine(string? value)
    {
        foreach (var writer in _writers)
            writer.WriteLine(value);
    }

    public override void Flush()
    {
        foreach (var writer in _writers)
            writer.Flush();
    }
}
