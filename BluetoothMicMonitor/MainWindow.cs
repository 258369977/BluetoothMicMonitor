using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BluetoothMicMonitor
{
public class MainWindow : Window
{
    private ListBox _listProcs;
    private TextBox _txtDevice;
    private CheckBox _chkAutoStart;
    private Ellipse _statusIndicator;
    private TextBlock _statusText;
    private TextBlock _statusDetail;
    private Button _btnStartStop;
    private Button _btnInstall;
    private Button _btnSave;
        private Button _btnUninstall;
    private Button _btnToggleDevice;
    private Ellipse _deviceStatusDot;
    private TextBlock _deviceStatusLabel;
    private bool _isRunning;

    public MainWindow()
    {
        Title = "蓝牙麦克风监控";
        Width = 560; Height = 480;
        MinWidth = 460; MinHeight = 400;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5));
        ResizeMode = ResizeMode.CanResizeWithGrip;
        BuildUI();
        LoadConfig();
        BindEvents();
        UpdateUI();
    }

    private void BuildUI()
    {
        var rootGrid = new Grid();
        rootGrid.Margin = new Thickness(20, 14, 20, 18);
        for (int i = 0; i < 5; i++)
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = i == 2 ? new GridLength(1, GridUnitType.Star) : GridLength.Auto });

        var statusBorder = MakeBorder(MakeStatusPanel(), 0);
        Grid.SetRow(statusBorder, 0);
        rootGrid.Children.Add(statusBorder);

        var deviceBorder = MakeBorder(MakeDevicePanel(), 1);
        Grid.SetRow(deviceBorder, 1);
        rootGrid.Children.Add(deviceBorder);

        var procBorder = MakeBorder(MakeProcessPanel(), 2);
        Grid.SetRow(procBorder, 2);
        rootGrid.Children.Add(procBorder);

        _chkAutoStart = new CheckBox
        {
            Content = "开机自动启动监控",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)),
            VerticalAlignment = VerticalAlignment.Center
        };
        var autoBorder = MakeBorder(_chkAutoStart, 3);
        Grid.SetRow(autoBorder, 3);
        rootGrid.Children.Add(autoBorder);

        var bottomPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        _btnSave = new Button
        {
            Content = "保存配置", Height = 30, MinWidth = 80,
            Padding = new Thickness(14, 0, 14, 0), FontSize = 12,
            Cursor = System.Windows.Input.Cursors.Hand,
            Background = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 8, 0)
        };
        _btnUninstall = new Button
        {
            Content = "卸载服务", Height = 30, MinWidth = 80,
            Padding = new Thickness(14, 0, 14, 0), FontSize = 12,
            Cursor = System.Windows.Input.Cursors.Hand,
            Background = Brushes.White,
            Foreground = new SolidColorBrush(Color.FromRgb(0xCC, 0x33, 0x33)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xE0, 0xA0, 0xA0)),
            BorderThickness = new Thickness(1)
        };
        bottomPanel.Children.Add(_btnSave);
        bottomPanel.Children.Add(_btnUninstall);
        Grid.SetRow(bottomPanel, 4);
        rootGrid.Children.Add(bottomPanel);

        Content = rootGrid;
    }

    private Border MakeBorder(UIElement content, int row)
    {
        return new Border
        {
            Background = Brushes.White,
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(14, (row == 3 ? 8 : 10), 14, (row == 3 ? 8 : 10)),
            Margin = new Thickness(0, 0, 0, 8),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xE2, 0xE2, 0xE2)),
            BorderThickness = new Thickness(1),
            Child = content
        };
    }

    private Grid MakeStatusPanel()
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var leftPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        _statusIndicator = new Ellipse
        {
            Width = 12, Height = 12,
            Margin = new Thickness(0, 0, 8, 0),
            Fill = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)),
            VerticalAlignment = VerticalAlignment.Center
        };
        var textPanel = new StackPanel();
        _statusText = new TextBlock
        {
            Text = "监控已停止", FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33))
        };
        _statusDetail = new TextBlock
        {
            Text = "点击启动开始监控", FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
            Margin = new Thickness(0, 2, 0, 0)
        };
        textPanel.Children.Add(_statusText);
        textPanel.Children.Add(_statusDetail);
        leftPanel.Children.Add(_statusIndicator);
        leftPanel.Children.Add(textPanel);
        grid.Children.Add(leftPanel);

        var rightPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        _btnStartStop = new Button
        {
            Content = "启动", Height = 30, MinWidth = 80,
            Padding = new Thickness(14, 0, 14, 0), FontSize = 12,
            Cursor = System.Windows.Input.Cursors.Hand,
            Background = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 8, 0)
        };
        _btnInstall = new Button
        {
            Content = "开机自启", Height = 30, MinWidth = 80,
            Padding = new Thickness(14, 0, 14, 0), FontSize = 12,
            Cursor = System.Windows.Input.Cursors.Hand,
            Background = Brushes.White,
            Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0)),
            BorderThickness = new Thickness(1)
        };
        rightPanel.Children.Add(_btnStartStop);
        rightPanel.Children.Add(_btnInstall);
        Grid.SetColumn(rightPanel, 1);
        grid.Children.Add(rightPanel);

        return grid;
    }

    private Grid MakeDevicePanel()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        // Row 0: device name input row
        var row0 = new Grid { Margin = new Thickness(0) };
        row0.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row0.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        row0.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        row0.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        row0.Children.Add(new TextBlock
        {
            Text = "设备名称:", VerticalAlignment = VerticalAlignment.Center,
            FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)),
            Margin = new Thickness(0, 0, 10, 0)
        });

        _txtDevice = new TextBox
        {
            Height = 28, VerticalContentAlignment = VerticalAlignment.Center,
            FontSize = 12, BorderBrush = new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0)),
            Padding = new Thickness(6, 0, 6, 0)
        };
        Grid.SetColumn(_txtDevice, 1);
        row0.Children.Add(_txtDevice);

        var btnScan = new Button
        {
            Content = "扫描", Width = 56, Height = 28, FontSize = 11,
            Margin = new Thickness(8, 0, 0, 0), Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0)),
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        btnScan.Click += OnScan;
        Grid.SetColumn(btnScan, 2);
        row0.Children.Add(btnScan);

        _btnToggleDevice = new Button
        {
            Content = "切换", Width = 56, Height = 28, FontSize = 11,
            Margin = new Thickness(4, 0, 0, 0), Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0)),
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        _btnToggleDevice.Click += OnToggleDevice;
        Grid.SetColumn(_btnToggleDevice, 3);
        row0.Children.Add(_btnToggleDevice);

        Grid.SetRow(row0, 0);
        grid.Children.Add(row0);

        // Row 1: device status indicator
        var statusRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 6, 0, 0)
        };
        _deviceStatusDot = new Ellipse
        {
            Width = 8, Height = 8,
            Margin = new Thickness(0, 0, 6, 0),
            Fill = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)),
            VerticalAlignment = VerticalAlignment.Center
        };
        statusRow.Children.Add(_deviceStatusDot);
        _deviceStatusLabel = new TextBlock
        {
            Text = "未知", FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
            VerticalAlignment = VerticalAlignment.Center
        };
        statusRow.Children.Add(_deviceStatusLabel);
        Grid.SetRow(statusRow, 1);
        grid.Children.Add(statusRow);

        return grid;
    }

    private Grid MakeProcessPanel()
    {
        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        grid.Children.Add(new TextBlock
        {
            Text = "目标进程", FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
            Margin = new Thickness(0, 0, 0, 6)
        });

        var inner = new Grid();
        inner.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        inner.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetRow(inner, 1);

        _listProcs = new ListBox
        {
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xD5, 0xD5, 0xD5)),
            FontSize = 12, Padding = new Thickness(4),
            SelectionMode = SelectionMode.Single
        };
        inner.Children.Add(_listProcs);

        var btnPanel = new StackPanel
        {
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Top
        };
        var btnAdd = new Button
        {
            Content = "+ 添加", Width = 72, Height = 28, FontSize = 12,
            Margin = new Thickness(0, 0, 0, 5), Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0)),
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        btnAdd.Click += OnAdd;
        var btnRemove = new Button
        {
            Content = "- 移除", Width = 72, Height = 28, FontSize = 12,
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0)),
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        btnRemove.Click += OnRemove;
        btnPanel.Children.Add(btnAdd);
        btnPanel.Children.Add(btnRemove);
        Grid.SetColumn(btnPanel, 1);
        inner.Children.Add(btnPanel);

        grid.Children.Add(inner);
        return grid;
    }

    private void LoadConfig()
    {
        var cfg = Program.GetConfig();
        _txtDevice.Text = cfg.DeviceName;
        _listProcs.Items.Clear();
        foreach (var p in cfg.TargetProcesses)
            _listProcs.Items.Add(p);
        _chkAutoStart.IsChecked = cfg.AutoStart || ConfigManager.IsAutoStartEnabled();
        _isRunning = Program.IsMonitorRunning();
        RefreshDeviceStatus();
    }

    private void BindEvents()
    {
        _btnStartStop.Click += OnStartStop;
        _btnInstall.Click += OnInstall;
        _btnSave.Click += OnSave;
        _btnUninstall.Click += OnUninstall;
    }

    private void OnStartStop(object sender, RoutedEventArgs e)
    {
        if (_isRunning) { Program.StopDaemon(); _isRunning = false; UpdateUI(); }
        else
        {
            var procs = _listProcs.Items.Cast<string>().ToList();
            if (procs.Count == 0) { MessageBox.Show("请至少添加一个目标进程。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (string.IsNullOrWhiteSpace(_txtDevice.Text.Trim())) { MessageBox.Show("请输入蓝牙设备名称。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            var cfg = CollectConfig(); ConfigManager.Save(cfg); Program.StartDaemon(cfg);
            _isRunning = true; UpdateUI();
        }
    }

    private void OnInstall(object sender, RoutedEventArgs e)
    {
        var procs = _listProcs.Items.Cast<string>().ToList();
        if (procs.Count == 0) { MessageBox.Show("请至少添加一个目标进程。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        var cfg = CollectConfig(); cfg.AutoStart = true; ConfigManager.Save(cfg); ConfigManager.SetAutoStart(true);
        _chkAutoStart.IsChecked = true;
        if (!_isRunning) { Program.StartDaemon(cfg); _isRunning = true; }
        UpdateUI();
    }

    private void OnToggleDevice(object sender, RoutedEventArgs e)
    {
        var btn = (Button)sender;
        btn.IsEnabled = false;
        try
        {
            var devName = _txtDevice.Text.Trim();
            if (string.IsNullOrWhiteSpace(devName))
            {
                MessageBox.Show("请先输入设备名称。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            bool isEnabled = SetupApi.IsDeviceEnabled(devName);
            string err;
            if (SetupApi.SetDeviceState(devName, !isEnabled, out err))
            {
                RefreshDeviceStatus();
            }
            else
            {
                MessageBox.Show("操作失败: " + err, "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("操作失败: " + ex.Message, "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { btn.IsEnabled = true; }
    }

    private void RefreshDeviceStatus()
    {
        var devName = _txtDevice.Text.Trim();
        if (string.IsNullOrWhiteSpace(devName))
        {
            _deviceStatusDot.Fill = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
            _deviceStatusLabel.Text = "未设置";
            _btnToggleDevice.IsEnabled = false;
            return;
        }
        bool enabled = SetupApi.IsDeviceEnabled(devName);
        _btnToggleDevice.IsEnabled = true;
        if (enabled)
        {
            _deviceStatusDot.Fill = new SolidColorBrush(Color.FromRgb(0x22, 0xBB, 0x55));
            _deviceStatusLabel.Text = "已启用";
            _btnToggleDevice.Content = "禁用";
        }
        else
        {
            _deviceStatusDot.Fill = new SolidColorBrush(Color.FromRgb(0xE8, 0x11, 0x23));
            _deviceStatusLabel.Text = "已禁用";
            _btnToggleDevice.Content = "启用";
        }
    }

    private void OnScan(object sender, RoutedEventArgs e)
    {
        var btn = (Button)sender; btn.IsEnabled = false; btn.Content = "...";
        try
        {
            var devices = SetupApi.EnumerateMediaDevices();
            if (devices.Count == 0) { MessageBox.Show("未找到 MEDIA 类设备。", "扫描结果", MessageBoxButton.OK, MessageBoxImage.Information); return; }
            var dlg = new ScanDialog(devices); dlg.Owner = this;
            if (dlg.ShowDialog() == true && dlg.SelectedDevice != null) { _txtDevice.Text = dlg.SelectedDevice; RefreshDeviceStatus(); }
        }
        catch (Exception ex) { MessageBox.Show("扫描失败: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error); }
        finally { btn.IsEnabled = true; btn.Content = "扫描"; }
    }

    private void OnAdd(object sender, RoutedEventArgs e)
    {
        var dlg = new AddProcessDialog(); dlg.Owner = this;
        if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.ProcessName))
        {
            var name = dlg.ProcessName.Trim();
            if (!name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) name += ".exe";
            if (!_listProcs.Items.Cast<string>().Any(x => x.Equals(name, StringComparison.OrdinalIgnoreCase)))
                _listProcs.Items.Add(name);
            UpdateUI();
        }
    }

    private void OnRemove(object sender, RoutedEventArgs e)
    {
        if (_listProcs.SelectedIndex >= 0) _listProcs.Items.RemoveAt(_listProcs.SelectedIndex);
        else MessageBox.Show("请先选中一个进程。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
        UpdateUI();
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_txtDevice.Text.Trim())) { MessageBox.Show("请输入蓝牙设备名称。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        var cfg = CollectConfig(); ConfigManager.Save(cfg);
        if (cfg.AutoStart) ConfigManager.SetAutoStart(true); else ConfigManager.SetAutoStart(false);
        UpdateUI();
    }

    private void OnUninstall(object sender, RoutedEventArgs e)
    {
        Program.StopDaemon(); ConfigManager.SetAutoStart(false);
        _chkAutoStart.IsChecked = false;
        Program.SetConfig(new AppConfig { TargetProcesses = new List<string>(_listProcs.Items.Cast<string>()), DeviceName = _txtDevice.Text.Trim(), AutoStart = false });
        _isRunning = false; UpdateUI();
    }

    private AppConfig CollectConfig()
    {
        return new AppConfig { TargetProcesses = _listProcs.Items.Cast<string>().ToList(), DeviceName = _txtDevice.Text.Trim(), AutoStart = _chkAutoStart.IsChecked ?? false };
    }

    private void UpdateUI()
    {
        _isRunning = Program.IsMonitorRunning();
        RefreshDeviceStatus();
        if (_isRunning)
        {
            _statusIndicator.Fill = new SolidColorBrush(Color.FromRgb(0x22, 0xBB, 0x55));
            _statusText.Text = "监控运行中"; _statusDetail.Text = "后台运行中 - 关闭窗口最小化到托盘";
            _btnStartStop.Content = "停止";
            _btnStartStop.Background = new SolidColorBrush(Color.FromRgb(0xE8, 0x11, 0x23));
            _btnStartStop.Foreground = Brushes.White;
            _btnStartStop.BorderBrush = new SolidColorBrush(Color.FromRgb(0xE8, 0x11, 0x23));
        }
        else
        {
            _statusIndicator.Fill = new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC));
            _statusText.Text = "监控已停止"; _statusDetail.Text = "点击启动开始监控";
            _btnStartStop.Content = "启动";
            _btnStartStop.Background = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4));
            _btnStartStop.Foreground = Brushes.White;
            _btnStartStop.BorderBrush = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4));
        }
        _btnInstall.Content = _chkAutoStart.IsChecked == true ? "已安装" : "开机自启";
        _btnInstall.IsEnabled = _chkAutoStart.IsChecked != true;
        _btnUninstall.IsEnabled = _chkAutoStart.IsChecked == true || _isRunning;
    }
}

public class AddProcessDialog : Window
{
    public string ProcessName { get; private set; }
    private TextBox _txt;

    public AddProcessDialog()
    {
        Title = "添加进程"; Width = 360; Height = 150;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        Background = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5));
        _txt = new TextBox { Height = 28, VerticalContentAlignment = VerticalAlignment.Center, FontSize = 12, BorderBrush = new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0)), Padding = new Thickness(6, 0, 6, 0) };
        var grid = new Grid { Margin = new Thickness(14) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.Children.Add(new TextBlock { Text = "进程名称（含 .exe）:", FontSize = 12, Foreground = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55)), Margin = new Thickness(0, 0, 0, 6) });
        Grid.SetRow(_txt, 1); grid.Children.Add(_txt);
        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
        var btnCancel = new Button { Content = "取消", Width = 64, Height = 28, FontSize = 12, Margin = new Thickness(0, 0, 8, 0), Background = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0)), BorderThickness = new Thickness(1), Cursor = System.Windows.Input.Cursors.Hand, IsCancel = true };
        var btnOK = new Button { Content = "确定", Width = 64, Height = 28, FontSize = 12, Background = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)), Foreground = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)), BorderThickness = new Thickness(1), Cursor = System.Windows.Input.Cursors.Hand, IsDefault = true };
        btnOK.Click += (s, ev) => { var input = _txt.Text.Trim(); if (string.IsNullOrWhiteSpace(input)) { MessageBox.Show("请输入进程名称。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning); return; } ProcessName = input; DialogResult = true; Close(); };
        btnPanel.Children.Add(btnCancel); btnPanel.Children.Add(btnOK);
        Grid.SetRow(btnPanel, 2); grid.Children.Add(btnPanel);
        Content = grid; _txt.Focus();
    }
}

public class ScanDialog : Window
{
    public string SelectedDevice { get; private set; }

    public ScanDialog(List<string> devices)
    {
        Title = "扫描 MEDIA 设备"; Width = 420; Height = 340;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        Background = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5));
        var grid = new Grid { Margin = new Thickness(14, 12, 14, 14) };
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var listBox = new ListBox { BorderBrush = new SolidColorBrush(Color.FromRgb(0xD5, 0xD5, 0xD5)), FontSize = 12, Padding = new Thickness(4), SelectionMode = SelectionMode.Single };
        foreach (var d in devices) listBox.Items.Add(d);
        grid.Children.Add(listBox);
        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
        var btnCancel = new Button { Content = "取消", Width = 70, Height = 28, FontSize = 12, Margin = new Thickness(0, 0, 8, 0), Background = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(0xC0, 0xC0, 0xC0)), BorderThickness = new Thickness(1), Cursor = System.Windows.Input.Cursors.Hand };
        btnCancel.Click += (s, ev) => Close();
        var btnSelect = new Button { Content = "选择", Width = 70, Height = 28, FontSize = 12, Background = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)), Foreground = Brushes.White, BorderBrush = new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)), BorderThickness = new Thickness(1), Cursor = System.Windows.Input.Cursors.Hand };
        btnSelect.Click += (s, ev) => { string sel = listBox.SelectedItem as string; if (sel != null) { SelectedDevice = sel; DialogResult = true; Close(); } else MessageBox.Show("请先选择一个设备。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning); };
        btnPanel.Children.Add(btnCancel); btnPanel.Children.Add(btnSelect);
        Grid.SetRow(btnPanel, 1); grid.Children.Add(btnPanel);
        Content = grid;
    }
}
}