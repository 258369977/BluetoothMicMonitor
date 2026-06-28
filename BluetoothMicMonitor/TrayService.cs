using System;
using System.Drawing;
using System.Windows.Forms;

namespace BluetoothMicMonitor
{
    public class TrayService : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _menu;
        private readonly ToolStripMenuItem _miOpen;
        private readonly ToolStripMenuItem _miLogs;
        private readonly ToolStripMenuItem _miAutoStart;
        private readonly ToolStripMenuItem _miExit;

        public event Action OpenRequested;
        public event Action LogsRequested;
        public event Action<bool> AutoStartToggled;
        public event Action ExitRequested;

        public TrayService()
        {
            _miOpen = new ToolStripMenuItem("打开面板", null, (sender, args) => { if (OpenRequested != null) OpenRequested(); });
            _miLogs = new ToolStripMenuItem("查看日志", null, (sender, args) => { if (LogsRequested != null) LogsRequested(); });
            _miAutoStart = new ToolStripMenuItem("开机自启");
            _miAutoStart.Click += (sender, args) => { _miAutoStart.Checked = !_miAutoStart.Checked; if (AutoStartToggled != null) AutoStartToggled(_miAutoStart.Checked); };
            _miExit = new ToolStripMenuItem("退出", null, (sender, args) => { if (ExitRequested != null) ExitRequested(); });

            _menu = new ContextMenuStrip();
            _menu.Items.Add(_miOpen);
            _menu.Items.Add(_miLogs);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(_miAutoStart);
            _menu.Items.Add(new ToolStripSeparator());
            _menu.Items.Add(_miExit);

            _notifyIcon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Text = "蓝牙麦克风监控",
                ContextMenuStrip = _menu,
                Visible = false
            };
            _notifyIcon.DoubleClick += (sender, args) => { if (OpenRequested != null) OpenRequested(); };
        }

        public bool AutoStartChecked
        {
            get { return _miAutoStart.Checked; }
            set { _miAutoStart.Checked = value; }
        }

        public bool Running
        {
            set
            {
                _notifyIcon.Icon = value ? SystemIcons.Shield : SystemIcons.Application;
                _notifyIcon.Text = value ? "蓝牙麦克风监控 - 运行中" : "蓝牙麦克风监控 - 已停止";
            }
        }

        public void Show() { _notifyIcon.Visible = true; }
        public void Hide() { _notifyIcon.Visible = false; }

        public void Dispose()
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _menu.Dispose();
        }
    }
}
