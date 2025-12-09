using System;
using System.Collections.ObjectModel;
using System.Windows;
using Shadowsocks.Controller;
using Shadowsocks.Util;
using Shadowsocks.View.Pages;
using Wpf.Ui.Controls;
using Wpf.Ui.Appearance; // Added this

namespace Shadowsocks.View
{
    /// <summary>
    /// 统一设置窗口
    /// </summary>
    public partial class UnifiedSettingsWindow
    {
        private readonly MainController _controller;

        public ObservableCollection<object> NavigationItems { get; set; } = new ObservableCollection<object>
            {
                new NavigationViewItem
                {
                    Content = "常规设置",
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                    Tag = "general",
                    TargetPageType = typeof(GeneralSettingsPage)
                },
                new NavigationViewItem
                {
                    Content = "订阅管理",
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Cloud24 },
                    Tag = "subscribe",
                    TargetPageType = typeof(SubscribeSettingsPage)
                },
                new NavigationViewItem
                {
                    Content = "端口设置",
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Connector24 },
                    Tag = "port",
                    TargetPageType = typeof(PortSettingsPage)
                },
                new NavigationViewItem
                {
                    Content = "DNS设置",
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Server24 },
                    Tag = "dns",
                    TargetPageType = typeof(DnsSettingsPage)
                }
        };


        public ObservableCollection<object> FooterNavigationItems { get; set; } = new ObservableCollection<object>
            {
                new NavigationViewItem
                {
                    Content = "关于",
                    Icon = new SymbolIcon { Symbol = SymbolRegular.Info24 },
                    Tag = "about"
                } 
        };

        public UnifiedSettingsWindow(MainController controller)
        {
            try
            {
                DataContext = this;
                _controller = controller;
                InitializeComponent();
                ApplicationThemeManager.Apply(this); 
                DataContext = this;
                //// 默认导航到第一页
                Loaded += (sender, args) =>
                {
                    try
                    {
                        if (NavigationItems.Count > 0)
                        {
                            // 先测试AboutPage，它不需要MainController
                            NavigateToPage("about");
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                };
            }
            catch (Exception ex)
            {
                throw;
            }

           
        }

        private void NavigationView_OnSelectionChanged(NavigationView sender, RoutedEventArgs args)
        {
            if (sender.SelectedItem is not NavigationViewItem item)
                return;

            var tag = item.Tag as string;
            NavigateToPage(tag);
        }

        private void NavigateToPage(string tag)
        {
            try
            {
                object page = tag switch
                {
                    "general" => new GeneralSettingsPage(_controller),
                    "subscribe" => new SubscribeSettingsPage(_controller),
                    "port" => new PortSettingsPage(_controller),
                    "dns" => new DnsSettingsPage(),
                    "about" => new AboutPage(),
                    _ => null
                };

                if (page != null)
                {
                    ContentFrame.Navigate(page);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"导航到页面 {tag} 时出错: {ex.Message}\n\n{ex.StackTrace}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
