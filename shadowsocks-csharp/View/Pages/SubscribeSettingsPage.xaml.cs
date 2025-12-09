using Shadowsocks.Controller;
using Shadowsocks.Model;
using Shadowsocks.Util;
using Shadowsocks.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Appearance;

namespace Shadowsocks.View.Pages
{
    public partial class SubscribeSettingsPage : Page
    {
        private readonly MainController _controller;
        private Configuration _modifiedConfiguration;
        private bool _isDeleteServer;

        public SubscribeWindowViewModel SubscribeWindowViewModel { get; set; } = new();

        public SubscribeSettingsPage()
        {
            InitializeComponent();
            ApplicationThemeManager.Apply(this);
        }

        public SubscribeSettingsPage(MainController controller)
        {
            _controller = controller;
            InitializeComponent();
            // I18N resources are already loaded via XAML MergedDictionaries
            // I18NUtil.SetLanguage(Resources, @"SubscribeWindow");

            _controller.ConfigChanged += controller_ConfigChanged;
            SubscribeWindowViewModel.SubscribesChanged += SubscribeWindowViewModel_SubscribesChanged;

            Unloaded += (o, e) =>
            {
                _controller.ConfigChanged -= controller_ConfigChanged;
                SubscribeWindowViewModel.SubscribesChanged -= SubscribeWindowViewModel_SubscribesChanged;
            };

            Loaded += (o, e) => LoadCurrentConfiguration();
        }

        private void SubscribeWindowViewModel_SubscribesChanged(object sender, EventArgs e)
        {
            // Subscribes changed
        }

        private void Page_Loaded(object sender, RoutedEventArgs _)
        {
            InfoGrid.Visibility = ServerSubscribeListBox.SelectedIndex == -1 ? Visibility.Hidden : Visibility.Visible;
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
        }

        private void LoadCurrentConfiguration()
        {
            _modifiedConfiguration = Global.Load();
            SubscribeWindowViewModel.ReadConfig(_modifiedConfiguration);
        }

        private void ServerSubscribeListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            InfoGrid.Visibility = ServerSubscribeListBox.SelectedIndex == -1 ? Visibility.Hidden : Visibility.Visible;
        }

        private void DeleteUnusedServer()
        {
            _modifiedConfiguration.Configs.RemoveAll(server =>
                    !string.IsNullOrEmpty(server.SubTag)
                    && _modifiedConfiguration.ServerSubscribes.All(subscribe => subscribe.Tag != server.SubTag));
            _isDeleteServer = true;
        }

        private bool SaveConfig()
        {
            var remarks = new HashSet<string>();
            foreach (var serverSubscribe in SubscribeWindowViewModel.SubscribeCollection)
            {
                if (remarks.Contains(serverSubscribe.Tag))
                {
                    return false;
                }
                remarks.Add(serverSubscribe.Tag);
            }
            _modifiedConfiguration.ServerSubscribes.Clear();
            _modifiedConfiguration.ServerSubscribes.AddRange(SubscribeWindowViewModel.SubscribeCollection);

            if (_modifiedConfiguration.Configs.Any(server =>
            !string.IsNullOrEmpty(server.SubTag)
            && _modifiedConfiguration.ServerSubscribes.All(subscribe => subscribe.Tag != server.SubTag)))
            {
                if (MessageBox.Show(Resources[@"SaveQuestion"] as string,
                            Controller.HttpRequest.UpdateChecker.Name, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes)
                        == MessageBoxResult.Yes)
                {
                    DeleteUnusedServer();
                }
            }
            _controller.SaveServersConfig(_modifiedConfiguration, _isDeleteServer);
            return true;
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!SaveConfig())
            {
                SaveError();
            }
            else
            {
                Global.UpdateSubscribeManager.CreateTask(Global.GuiConfig, Global.UpdateNodeChecker, true);
            }
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            SubscribeWindowViewModel.SubscribeCollection.Add(new ServerSubscribe());
            SetServerListSelectedIndex(SubscribeWindowViewModel.SubscribeCollection.Count - 1);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var index = ServerSubscribeListBox.SelectedIndex;
            if (ServerSubscribeListBox.SelectedItem is ServerSubscribe serverSubscribe)
            {
                var tag = serverSubscribe.Tag;
                _modifiedConfiguration.Configs.RemoveAll(server => server.SubTag == tag);
                _isDeleteServer = true;
                SubscribeWindowViewModel.SubscribeCollection.Remove(serverSubscribe);
            }

            SetServerListSelectedIndex(index);
        }

        private void SetServerListSelectedIndex(int index)
        {
            if (index < 0)
            {
                return;
            }

            if (index < ServerSubscribeListBox.Items.Count)
            {
                ServerSubscribeListBox.SelectedIndex = index;
                ServerSubscribeListBox.ScrollIntoView(ServerSubscribeListBox.Items[index]);
            }
            else
            {
                ServerSubscribeListBox.SelectedIndex = ServerSubscribeListBox.Items.Count - 1;
                if (ServerSubscribeListBox.SelectedIndex > 0)
                {
                    ServerSubscribeListBox.ScrollIntoView(ServerSubscribeListBox.Items[ServerSubscribeListBox.Items.Count - 1]);
                }
            }
        }

        private void SaveError()
        {
            MessageBox.Show(Resources[@"SaveError"] as string, Controller.HttpRequest.UpdateChecker.Name, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void UpdateButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (ServerSubscribeListBox.SelectedItem is ServerSubscribe serverSubscribe)
            {
                if (SaveConfig())
                {
                    Global.UpdateSubscribeManager.CreateTask(Global.GuiConfig, Global.UpdateNodeChecker, true, new List<ServerSubscribe> { serverSubscribe });
                }
                else
                {
                    SaveError();
                }
            }
        }
    }
}
