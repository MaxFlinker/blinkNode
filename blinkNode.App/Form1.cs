using System.Net.NetworkInformation;

namespace blinkNode.App;

public partial class Form1 : Form
{
    private readonly System.Windows.Forms.Timer sampleTimer = new() { Interval = 1000 };
    private readonly System.Windows.Forms.Timer pulseTimer = new() { Interval = 180 };
    private readonly NotifyIcon trayIcon = new();
    private readonly Icon applicationIcon;
    private readonly Dictionary<(bool Receive, bool Send), Icon> activityIcons = [];
    private readonly Label receiveLamp = MakeLamp(Color.FromArgb(65, 210, 145));
    private readonly Label sendLamp = MakeLamp(Color.FromArgb(82, 170, 255));
    private readonly Label receiveValue = MakeLabel("Waiting for network activity…", 11);
    private readonly Label sendValue = MakeLabel("Waiting for network activity…", 11);
    private readonly Label modeStatus = MakeLabel("MEASURED · Windows network counters", 9, Color.FromArgb(150, 167, 188));
    private readonly CheckBox alwaysOnTop = new() { Text = "Keep in front", AutoSize = true, ForeColor = Color.Gainsboro };
    private readonly CheckBox simulate = new() { Text = "Preview virtual blink", AutoSize = true, ForeColor = Color.Gainsboro };
    private readonly CheckBox minimizeToTray = new() { Text = "Minimize to tray", AutoSize = true, ForeColor = Color.Gainsboro, Checked = true };
    private readonly CheckBox blinkTray = new() { Text = "Blink tray icon", AutoSize = true, ForeColor = Color.Gainsboro, Checked = true };
    private readonly CheckBox blinkTaskbar = new() { Text = "Blink taskbar icon", AutoSize = true, ForeColor = Color.Gainsboro, Checked = true };
    private readonly Label title = MakeLabel("blinkNode Light", 18, Color.White, bold: true);
    private readonly Dictionary<string, (long Received, long Sent)> previous = new(StringComparer.Ordinal);
    private bool paused;
    private bool simulationOn;
    private bool automaticSimulation;
    private bool currentReceive;
    private bool currentSend;

    public Form1()
    {
        InitializeComponent();
        BuildWindow();
        applicationIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath) ?? SystemIcons.Application;
        Icon = applicationIcon;
        LoadActivityIcons();
        trayIcon.Text = "blinkNode Light";
        trayIcon.Icon = applicationIcon;
        trayIcon.Visible = true;
        var trayMenu = new ContextMenuStrip();
        trayMenu.Items.Add("Open blinkNode Light", null, (_, _) => RestoreWindow());
        trayMenu.Items.Add("Pause / resume", null, (_, _) => TogglePause());
        trayMenu.Items.Add("Exit", null, (_, _) => Close());
        trayIcon.ContextMenuStrip = trayMenu;
        trayIcon.DoubleClick += (_, _) => RestoreWindow();

        alwaysOnTop.CheckedChanged += (_, _) => TopMost = alwaysOnTop.Checked;
        simulate.CheckedChanged += (_, _) => SetSimulation(simulate.Checked);
        blinkTray.CheckedChanged += (_, _) => ApplyActivityIcon(currentReceive, currentSend);
        blinkTaskbar.CheckedChanged += (_, _) => ApplyActivityIcon(currentReceive, currentSend);
        sampleTimer.Tick += (_, _) => SampleNetwork();
        pulseTimer.Tick += (_, _) => ClearLamps();
        Resize += (_, _) => { if (WindowState == FormWindowState.Minimized && minimizeToTray.Checked) Hide(); };
        FormClosed += (_, _) =>
        {
            sampleTimer.Dispose(); pulseTimer.Dispose(); trayIcon.Visible = false; trayIcon.Dispose();
            foreach (var icon in activityIcons.Values) icon.Dispose();
            if (!ReferenceEquals(applicationIcon, SystemIcons.Application)) applicationIcon.Dispose();
        };

        CaptureCounters();
        ApplyActivityIcon(false, false);
        sampleTimer.Start();
    }

    private void BuildWindow()
    {
        Text = "blinkNode Light";
        ClientSize = new Size(410, 335);
        MinimumSize = new Size(390, 320);
        BackColor = Color.FromArgb(22, 27, 36);
        ForeColor = Color.Gainsboro;
        Font = new Font("Segoe UI", 9F);
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        ShowInTaskbar = true;

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(20, 16, 20, 14), ColumnCount = 1, RowCount = 5 };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
        var header = new Panel { Dock = DockStyle.Fill };
        title.Dock = DockStyle.Left;
        title.Width = 270;
        var minimizeButton = new Button
        {
            Text = "—",
            AccessibleName = "Minimize",
            AccessibleDescription = "Minimize blinkNode Light to the tray or taskbar",
            Width = 34,
            Dock = DockStyle.Right,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(31, 38, 49),
            ForeColor = Color.Gainsboro,
            Font = new Font("Segoe UI", 12F),
            TabStop = true
        };
        minimizeButton.FlatAppearance.BorderSize = 0;
        minimizeButton.Click += (_, _) => WindowState = FormWindowState.Minimized;
        header.Controls.Add(title);
        header.Controls.Add(minimizeButton);
        root.Controls.Add(header, 0, 0);
        root.Controls.Add(BuildLane("RECEIVE", receiveLamp, receiveValue), 0, 1);
        root.Controls.Add(BuildLane("SEND", sendLamp, sendValue), 0, 2);
        root.Controls.Add(modeStatus, 0, 3);

        var footer = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, FlowDirection = FlowDirection.LeftToRight };
        footer.Controls.Add(alwaysOnTop);
        footer.Controls.Add(simulate);
        footer.Controls.Add(minimizeToTray);
        footer.Controls.Add(blinkTray);
        footer.Controls.Add(blinkTaskbar);
        root.Controls.Add(footer, 0, 4);
        Controls.Add(root);
    }

    private static Control BuildLane(string name, Label lamp, Label value)
    {
        var lane = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = new Padding(0, 3, 0, 3), BackColor = Color.FromArgb(31, 38, 49), Padding = new Padding(12, 4, 10, 4) };
        lane.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 42));
        lane.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86));
        lane.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        lane.Controls.Add(lamp, 0, 0);
        lane.Controls.Add(MakeLabel(name, 9, Color.FromArgb(166, 181, 201), bold: true), 1, 0);
        lane.Controls.Add(value, 2, 0);
        return lane;
    }

    private void LoadActivityIcons()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "Icons");
        activityIcons[(false, false)] = LoadIcon("idle.ico");
        activityIcons[(true, false)] = LoadIcon("receive.ico");
        activityIcons[(false, true)] = LoadIcon("send.ico");
        activityIcons[(true, true)] = LoadIcon("both.ico");

        Icon LoadIcon(string name)
        {
            var path = Path.Combine(directory, name);
            return File.Exists(path) ? new Icon(path) : (Icon)applicationIcon.Clone();
        }
    }

    private static Label MakeLamp(Color color) => new()
    {
        BackColor = Color.FromArgb(42, 49, 60),
        ForeColor = color,
        Text = "●",
        Font = new Font("Segoe UI Symbol", 18F, FontStyle.Bold),
        TextAlign = ContentAlignment.MiddleCenter,
        Dock = DockStyle.Fill,
        AutoSize = false
    };

    private static Label MakeLabel(string text, float size, Color? color = null, bool bold = false) => new()
    {
        Text = text,
        ForeColor = color ?? Color.Gainsboro,
        Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
        TextAlign = ContentAlignment.MiddleLeft,
        Dock = DockStyle.Fill,
        AutoEllipsis = true
    };

    private void CaptureCounters()
    {
        previous.Clear();
        foreach (var adapter in GetAdapters())
        {
            try
            {
                var stats = adapter.GetIPv4Statistics();
                previous[adapter.Id] = (stats.BytesReceived, stats.BytesSent);
            }
            catch (NetworkInformationException) { }
        }
    }

    private void SampleNetwork()
    {
        if (paused || simulate.Checked) return;

        long received = 0, sent = 0;
        var available = false;
        foreach (var adapter in GetAdapters())
        {
            try
            {
                var stats = adapter.GetIPv4Statistics();
                available = true;
                if (previous.TryGetValue(adapter.Id, out var old))
                {
                    received += Math.Max(0, stats.BytesReceived - old.Received);
                    sent += Math.Max(0, stats.BytesSent - old.Sent);
                }
                previous[adapter.Id] = (stats.BytesReceived, stats.BytesSent);
            }
            catch (NetworkInformationException) { }
        }

        modeStatus.Text = available ? "MEASURED · Windows network counters" : "SIMULATED · no network counter available";
        if (!available)
        {
            receiveValue.Text = sendValue.Text = "Virtual blink · no counter source";
            if (!automaticSimulation)
            {
                automaticSimulation = true;
                sampleTimer.Stop();
                pulseTimer.Interval = 450;
                pulseTimer.Tick -= ClearLamps;
                pulseTimer.Tick += SimulatedPulse;
                pulseTimer.Start();
            }
            return;
        }

        receiveValue.Text = FormatRate(received);
        sendValue.Text = FormatRate(sent);
        SetLamp(receiveLamp, received > 0);
        SetLamp(sendLamp, sent > 0);
        ApplyActivityIcon(received > 0, sent > 0);
        pulseTimer.Stop();
        pulseTimer.Start();
    }

    private static NetworkInterface[] GetAdapters() => NetworkInterface.GetAllNetworkInterfaces()
        .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback && n.OperationalStatus == OperationalStatus.Up).ToArray();

    private static string FormatRate(long bytes) => bytes < 1024 ? $"{bytes} B/s" : $"{bytes / 1024d:0.#} KB/s";

    private void SetSimulation(bool enabled)
    {
        CaptureCounters();
        if (enabled)
        {
            automaticSimulation = false;
            modeStatus.Text = "SIMULATED · sample blink, no activity data read";
            receiveValue.Text = sendValue.Text = "Preview only";
            sampleTimer.Stop();
            pulseTimer.Interval = 360;
            pulseTimer.Tick -= ClearLamps;
            pulseTimer.Tick += SimulatedPulse;
            if (!paused) pulseTimer.Start();
        }
        else
        {
            pulseTimer.Stop();
            pulseTimer.Tick -= SimulatedPulse;
            pulseTimer.Tick += ClearLamps;
            pulseTimer.Interval = 180;
            if (!paused) sampleTimer.Start();
            ClearLamps();
        }
    }

    private void SimulatedPulse(object? sender, EventArgs e)
    {
        simulationOn = !simulationOn;
        var receive = simulationOn;
        var send = !simulationOn;
        SetLamp(receiveLamp, receive);
        SetLamp(sendLamp, send);
        ApplyActivityIcon(receive, send);
    }

    private static void SetLamp(Label lamp, bool active) => lamp.BackColor = active ? lamp.ForeColor : Color.FromArgb(42, 49, 60);
    private void ClearLamps(object? sender = null, EventArgs? e = null)
    {
        SetLamp(receiveLamp, false);
        SetLamp(sendLamp, false);
        ApplyActivityIcon(false, false);
    }

    private void ApplyActivityIcon(bool receive, bool send)
    {
        currentReceive = receive;
        currentSend = send;
        var frame = activityIcons.TryGetValue((receive, send), out var icon) ? icon : applicationIcon;
        if (blinkTaskbar.Checked) Icon = frame;
        else Icon = applicationIcon;
        trayIcon.Icon = blinkTray.Checked ? frame : applicationIcon;
    }

    private void TogglePause()
    {
        paused = !paused;
        if (paused)
        {
            sampleTimer.Stop();
            pulseTimer.Stop();
            modeStatus.Text = "PAUSED · local counters are not being sampled";
            ClearLamps();
        }
        else
        {
            CaptureCounters();
            if (simulate.Checked) pulseTimer.Start();
            else sampleTimer.Start();
            modeStatus.Text = simulate.Checked ? "SIMULATED · sample blink, no activity data read" : "MEASURED · Windows network counters";
        }
    }

    private void RestoreWindow() { Show(); WindowState = FormWindowState.Normal; Activate(); }
}
