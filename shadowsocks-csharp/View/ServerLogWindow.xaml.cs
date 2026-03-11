using Shadowsocks.Controller;
using Shadowsocks.Controller.HttpRequest;
using Shadowsocks.Model;
using Shadowsocks.Util;
using Shadowsocks.ViewModel;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace Shadowsocks.View
{
    public partial class ServerLogWindow
    {
        private const string IndexColumnPath = nameof(Server.Index);
        private const string GroupColumnPath = nameof(Server.GroupName);
        private const string ServerColumnPath = nameof(Server.FriendlyName);
        private const string ConnectingColumnPath = "SpeedLog.Connecting";
        private const string MaxDownSpeedColumnPath = "SpeedLog.MaxDownSpeed";
        private const string MaxUpSpeedColumnPath = "SpeedLog.MaxUpSpeed";
        private const string TotalDownloadBytesColumnPath = "SpeedLog.TotalDownloadBytes";
        private const string TotalUploadBytesColumnPath = "SpeedLog.TotalUploadBytes";
        private const string TotalDownloadRawBytesColumnPath = "SpeedLog.TotalDownloadRawBytes";
        private const string ConnectErrorColumnPath = "SpeedLog.ConnectError";
        private const string ErrorTimeoutTimesColumnPath = "SpeedLog.ErrorTimeoutTimes";
        private const string ErrorEmptyTimesColumnPath = "SpeedLog.ErrorEmptyTimes";
        private const string ErrorContinuousTimesColumnPath = "SpeedLog.ErrorContinuousTimes";
        private const string ErrorPercentColumnPath = "SpeedLog.ErrorPercent";

        public ServerLogWindow(MainController controller, WindowStatus status)
        {
            InitializeComponent();
            I18NUtil.SetLanguage(Resources, @"ServerLogWindow");
            LoadLanguage();

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

        private void LoadLanguage()
        {
            IndexColumn.Header = this.GetWindowStringValue(@"Index");
            GroupColumn.Header = this.GetWindowStringValue(@"Group");
            ServerColumn.Header = this.GetWindowStringValue(@"Server");
            ConnectingColumn.Header = this.GetWindowStringValue(@"Connecting");
            AvgConnectTimeColumn.Header = this.GetWindowStringValue(@"Latency");
            AvgDownloadBytesColumn.Header = this.GetWindowStringValue(@"AvgDSpeed");
            MaxDownSpeedColumn.Header = this.GetWindowStringValue(@"MaxDSpeed");
            AvgUploadBytesColumn.Header = this.GetWindowStringValue(@"AvgUpSpeed");
            MaxUpSpeedColumn.Header = this.GetWindowStringValue(@"MaxUpSpeed");
            TotalDownloadBytesColumn.Header = this.GetWindowStringValue(@"Dload");
            TotalUploadBytesColumn.Header = this.GetWindowStringValue(@"Upload");
            TotalDownloadRawBytesColumn.Header = this.GetWindowStringValue(@"DloadRaw");
            ConnectErrorColumn.Header = this.GetWindowStringValue(@"Error");
            ErrorTimeoutTimesColumn.Header = this.GetWindowStringValue(@"Timeout");
            ErrorEmptyTimesColumn.Header = this.GetWindowStringValue(@"EmptyResponse");
            ErrorContinuousTimesColumn.Header = this.GetWindowStringValue(@"Continuous");
            ErrorPercentColumn.Header = this.GetWindowStringValue(@"ErrorPercent");
        }

        private void LoadConfig(bool isFirstLoad)
        {
            UpdateTitle();
            ServerLogViewModel.ReadConfig();

            Dispatcher.CurrentDispatcher.InvokeAsync(() =>
            {
                if (isFirstLoad && ServerLogViewModel.SelectedServer != null)
                {
                    SelectServerCell(ServerLogViewModel.SelectedServer, ServerColumn);
                }
            }, DispatcherPriority.Input);
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

        private void AlwaysTopMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Topmost = !Topmost;
        }

        private void AutoSizeMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            foreach (var column in ServerDataGrid.Columns)
            {
                column.Width = DataGridLength.Auto;
            }

            ServerDataGrid.UpdateLayout();
            SizeToContent = SizeToContent.Width;
        }

        private void DisconnectDirectMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Server.ForwardServer.Connections.CloseAll();
        }

        private void DisconnectAllMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            _controller.DisconnectAllConnections();
            Server.ForwardServer.Connections.CloseAll();
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
            var config = Global.GuiConfig;
            if (config.Index >= 0 && config.Index < config.Configs.Count)
            {
                try
                {
                    _controller.ClearTransferTotal(config.Configs[config.Index].Id);
                }
                catch
                {
                    // ignored
                }
            }
        }

        private void ClearTotalMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var config = Global.GuiConfig;
            foreach (var server in config.Configs)
            {
                _controller.ClearTransferTotal(server.Id);
            }
        }

        private void CopyCurrentLinkMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var config = Global.GuiConfig;
            if (config.Index >= 0 && config.Index < config.Configs.Count)
            {
                var link = config.Configs[config.Index].SsrLink;
                Clipboard.SetDataObject(link);
            }
        }

        private void CopyCurrentGroupLinksMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var config = Global.GuiConfig;
            if (config.Index >= 0 && config.Index < config.Configs.Count)
            {
                var group = config.Configs[config.Index].Group;
                var link = config.Configs.Where(t => t.Group == group).Aggregate(string.Empty, (current, t) => current + $@"{t.SsrLink}{Environment.NewLine}");
                Clipboard.SetDataObject(link);
            }
        }

        private void CopyAllEnableLinksMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var config = Global.GuiConfig;
            var link = config.Configs.Where(t => t.Enable).Aggregate(string.Empty, (current, t) => current + $@"{t.SsrLink}{Environment.NewLine}");
            Clipboard.SetDataObject(link);
        }

        private void CopyAllLinksMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var config = Global.GuiConfig;
            var link = config.Configs.Aggregate(string.Empty, (current, t) => current + $@"{t.SsrLink}{Environment.NewLine}");
            Clipboard.SetDataObject(link);
        }

        private void ServerDataGrid_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
            {
                return;
            }

            var source = e.OriginalSource as DependencyObject;
            if (TryGetRowHeaderServer(source, out var headerServer))
            {
                headerServer.Enable = !headerServer.Enable;
                Global.SaveConfig();
                return;
            }

            if (!TryGetCellServer(source, out var cell, out var server))
            {
                return;
            }

            switch (cell.Column.SortMemberPath)
            {
                case ServerColumnPath:
                {
                    _controller.DisconnectAllConnections(true);
                    _controller.SelectServerIndex(server.Index - 1);
                    SelectServerCell(server, IndexColumn);
                    break;
                }
                case GroupColumnPath:
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

                    SelectServerCell(server, IndexColumn);
                    break;
                }
            }
        }

        private void ServerDataGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
            {
                return;
            }

            if (!TryGetCellServer(e.OriginalSource as DependencyObject, out var cell, out var server))
            {
                return;
            }

            switch (cell.Column.SortMemberPath)
            {
                case IndexColumnPath:
                    _controller.ShowConfigForm(server.Index - 1);
                    break;
                case ConnectingColumnPath:
                    server.Connections.CloseAll();
                    break;
                case MaxDownSpeedColumnPath:
                case MaxUpSpeedColumnPath:
                    server.SpeedLog.ClearMaxSpeed();
                    break;
                case TotalDownloadBytesColumnPath:
                case TotalUploadBytesColumnPath:
                    server.SpeedLog.ClearTrans();
                    break;
                case TotalDownloadRawBytesColumnPath:
                    server.SpeedLog.Clear();
                    server.Enable = true;
                    break;
                case ConnectErrorColumnPath:
                case ErrorTimeoutTimesColumnPath:
                case ErrorEmptyTimesColumnPath:
                case ErrorContinuousTimesColumnPath:
                case ErrorPercentColumnPath:
                    server.SpeedLog.ClearError();
                    server.Enable = true;
                    break;
                default:
                    SelectServerCell(server, IndexColumn);
                    break;
            }
        }

        private void SelectServerCell(Server server, DataGridColumn column)
        {
            if (server == null || column == null)
            {
                return;
            }

            ServerDataGrid.SelectedCells.Clear();
            ServerDataGrid.SelectedItem = server;
            ServerDataGrid.CurrentCell = new DataGridCellInfo(server, column);
            ServerDataGrid.ScrollIntoView(server, column);
        }

        private static bool TryGetCellServer(DependencyObject source, out DataGridCell cell, out Server server)
        {
            cell = FindParent<DataGridCell>(source);
            server = cell?.DataContext as Server;
            return cell != null && server != null;
        }

        private static bool TryGetRowHeaderServer(DependencyObject source, out Server server)
        {
            var rowHeader = FindParent<DataGridRowHeader>(source);
            server = rowHeader?.DataContext as Server;
            return server != null;
        }

        private static T FindParent<T>(DependencyObject source) where T : DependencyObject
        {
            while (source != null)
            {
                if (source is T match)
                {
                    return match;
                }

                source = source switch
                {
                    Visual visual => VisualTreeHelper.GetParent(visual),
                    Visual3D visual3D => VisualTreeHelper.GetParent(visual3D),
                    _ => null
                };
            }

            return null;
        }
    }
}
