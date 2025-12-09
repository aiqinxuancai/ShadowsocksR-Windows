using Shadowsocks.Controller;
using Shadowsocks.Controller.HttpRequest;
using Shadowsocks.Model;
using Shadowsocks.Util;
using Shadowsocks.ViewModel;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Shadowsocks.View
{
    public partial class ServerLogWindow
    {
        public ServerLogWindow(MainController controller, WindowStatus status)
        {
            InitializeComponent();
            I18NUtil.SetLanguage(Resources, @"ServerLogWindow");

            _controller = controller;
            Closed += (o, e) => { _controller.ConfigChanged -= controller_ConfigChanged; };
            _controller.ConfigChanged += controller_ConfigChanged;
            LoadConfig(true);

            if (status == null)
            {
                SizeToContent = SizeToContent.Width;
                Height = 600;
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
            }
            else
            {
                SizeToContent = SizeToContent.Manual;
                status.SetStatus(this);
            }
        }

        private void LoadConfig(bool isFirstLoad)
        {
            UpdateTitle();
            ServerLogViewModel.ReadConfig();

            if (isFirstLoad && ServerLogViewModel.SelectedServer != null)
            {
                // Scroll to selected server
                ServerDataGrid.ScrollIntoView(ServerLogViewModel.SelectedServer);
            }
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadConfig(false);
        }

        private readonly MainController _controller;
        public ServerLogViewModel ServerLogViewModel { get; set; } = new();

        private void UpdateTitle()
        {
            Title = $@"{this.GetWindowStringValue(@"Title")}({(Global.GuiConfig.ShareOverLan ? this.GetWindowStringValue(@"Any") : this.GetWindowStringValue(@"Local"))}:{Global.GuiConfig.LocalPort} {this.GetWindowStringValue(@"Version")}{Controller.HttpRequest.UpdateChecker.FullVersion})";
        }

        #region Menu

        private void DisconnectDirectMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            _controller.DisconnectAllConnections(true);
        }

        private void DisconnectAllMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            _controller.DisconnectAllConnections();
        }

        private void ClearMaxMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var config = Global.GuiConfig;
            foreach (var server in config.Configs)
            {
                server.SpeedLog.ClearMaxSpeed();
            }
        }

        private void ClearAllMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var config = Global.GuiConfig;
            foreach (var server in config.Configs)
            {
                server.SpeedLog.Clear();
            }
        }

        private void ClearSelectedTotalMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (ServerDataGrid.SelectedItem is Server server)
            {
                server.SpeedLog.ClearTrans();
            }
        }

        private void ClearTotalMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var config = Global.GuiConfig;
            foreach (var server in config.Configs)
            {
                server.SpeedLog.ClearTrans();
            }
        }

        private void CopyCurrentLinkMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (ServerDataGrid.SelectedItem is Server server)
            {
                Clipboard.SetDataObject(server.SsrLink);
            }
        }

        private void CopyCurrentGroupLinksMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (ServerDataGrid.SelectedItem is Server selectedServer)
            {
                var config = Global.GuiConfig;
                var link = config.Configs
                    .Where(t => t.Group == selectedServer.Group)
                    .Aggregate(string.Empty, (current, t) => current + $@"{t.SsrLink}{Environment.NewLine}");
                Clipboard.SetDataObject(link);
            }
        }

        private void CopyAllEnableLinksMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var config = Global.GuiConfig;
            var link = config.Configs
                .Where(t => t.Enable)
                .Aggregate(string.Empty, (current, t) => current + $@"{t.SsrLink}{Environment.NewLine}");
            Clipboard.SetDataObject(link);
        }

        private void CopyAllLinksMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var config = Global.GuiConfig;
            var link = config.Configs.Aggregate(string.Empty, (current, t) => current + $@"{t.SsrLink}{Environment.NewLine}");
            Clipboard.SetDataObject(link);
        }

        private void AlwaysTopMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Topmost = !Topmost;
        }

        private void AutoSizeMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            SizeToContent = SizeToContent.Width;
        }

        #endregion

        #region DataGrid Events

        private Server _lastClickedServer;
        private DateTime _lastClickTime;
        private const int DoubleClickInterval = 500; // milliseconds

        private void ServerDataGrid_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Get clicked element
            var dep = (DependencyObject)e.OriginalSource;
            while (dep != null && !(dep is DataGridCell))
            {
                dep = System.Windows.Media.VisualTreeHelper.GetParent(dep);
            }

            if (dep is DataGridCell cell)
            {
                var row = DataGridRow.GetRowContainingElement(cell);
                if (row?.Item is Server server)
                {
                    _currentServer = server;
                    _currentColumn = cell.Column;

                    // Check for double-click
                    var now = DateTime.Now;
                    if (_lastClickedServer == server &&
                        (now - _lastClickTime).TotalMilliseconds < DoubleClickInterval)
                    {
                        HandleDoubleTap(server, cell.Column);
                        _lastClickedServer = null;
                    }
                    else
                    {
                        _lastClickedServer = server;
                        _lastClickTime = now;
                    }
                }
            }
        }

        private Server _currentServer;
        private DataGridColumn _currentColumn;

        private void ServerDataGrid_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_currentServer != null && _currentColumn != null)
            {
                HandleSingleTap(_currentServer, _currentColumn);
            }
        }

        private void HandleSingleTap(Server server, DataGridColumn column)
        {
            var header = column.Header?.ToString();
            if (header == this.GetWindowStringValue(@"Server"))
            {
                _controller.DisconnectAllConnections(true);
                _controller.SelectServerIndex(server.Index - 1);
                ServerDataGrid.SelectedItem = server;
            }
            else if (header == this.GetWindowStringValue(@"Group"))
            {
                var group = server.Group;
                if (!string.IsNullOrEmpty(group))
                {
                    var enable = !server.Enable;
                    foreach (var sameGroupServer in ServerLogViewModel.ServersCollection)
                    {
                        if (sameGroupServer.Group == group)
                        {
                            sameGroupServer.Enable = enable;
                        }
                    }
                    Global.SaveConfig();
                }
            }
        }

        private void HandleDoubleTap(Server server, DataGridColumn column)
        {
            var header = column.Header?.ToString();

            if (header == this.GetWindowStringValue(@"Index"))
            {
                _controller.ShowConfigForm(server.Index - 1);
            }
            else if (header == this.GetWindowStringValue(@"Connecting"))
            {
                server.Connections.CloseAll();
            }
            else if (header == this.GetWindowStringValue(@"MaxDownSpeed") ||
                     header == this.GetWindowStringValue(@"MaxUpSpeed"))
            {
                server.SpeedLog.ClearMaxSpeed();
            }
            else if (header == this.GetWindowStringValue(@"TotalDownload") ||
                     header == this.GetWindowStringValue(@"TotalUpload"))
            {
                server.SpeedLog.ClearTrans();
            }
            else if (header == this.GetWindowStringValue(@"TotalDownloadRaw"))
            {
                server.SpeedLog.Clear();
                server.Enable = true;
            }
            else if (header == this.GetWindowStringValue(@"ConnectError") ||
                     header == this.GetWindowStringValue(@"ErrorTimeout") ||
                     header == this.GetWindowStringValue(@"ErrorEmpty") ||
                     header == this.GetWindowStringValue(@"ErrorContinuous") ||
                     header == this.GetWindowStringValue(@"ErrorPercent"))
            {
                server.SpeedLog.ClearError();
                server.Enable = true;
            }
        }

        #endregion
    }
}
