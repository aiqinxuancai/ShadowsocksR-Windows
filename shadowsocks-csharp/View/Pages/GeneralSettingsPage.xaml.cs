using Shadowsocks.Controller;
using Shadowsocks.Controller.HttpRequest;
using Shadowsocks.Model;
using Shadowsocks.Util;
using Shadowsocks.ViewModel;
using System;
using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Appearance;

namespace Shadowsocks.View.Pages
{
    public partial class GeneralSettingsPage : Page
    {
        private readonly MainController _controller;

        public SettingViewModel SettingViewModel { get; set; } = new();

        public GeneralSettingsPage()
        {
            InitializeComponent();
        }

        public GeneralSettingsPage(MainController controller)
        {
            try
            {
                _controller = controller;
                InitializeComponent();
                ApplicationThemeManager.Apply(this);
                // I18N resources are already loaded via XAML MergedDictionaries
                // I18NUtil.SetLanguage(Resources, @"SettingsWindow");

                _controller.ConfigChanged += controller_ConfigChanged;
                Unloaded += (o, e) => { _controller.ConfigChanged -= controller_ConfigChanged; };

                Loaded += (o, e) => LoadCurrentConfiguration();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"GeneralSettingsPage初始化错误: {ex.Message}\n\n{ex.StackTrace}", "错误", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                throw;
            }
        }

        private void LoadCurrentConfiguration()
        {
            SettingViewModel.ReadConfig();
            SettingViewModel.ModifiedConfiguration.PropertyChanged += (o, args) =>
            {
                // Configuration has been modified
            };
            if (AutoStartupCheckBox != null)
            {
                AutoStartupCheckBox.IsChecked = AutoStartup.Check();
            }
        }

        private void controller_ConfigChanged(object sender, EventArgs e)
        {
            LoadCurrentConfiguration();
        }

        private void SaveConfig()
        {
            if (SettingViewModel.ModifiedConfiguration.LangName != Global.GuiConfig.LangName)
            {
                MessageBox.Show(Resources[@"RestartRequired"] as string, Controller.HttpRequest.UpdateChecker.Name, MessageBoxButton.OK);
            }
            _controller.SaveServersConfig(SettingViewModel.ModifiedConfiguration, true);
            var isAutoStartup = AutoStartupCheckBox.IsChecked.GetValueOrDefault();
            if (isAutoStartup != AutoStartup.Check()
            && !AutoStartup.Set(isAutoStartup))
            {
                MessageBox.Show(Resources[@"FailAutoStartUp"] as string, Controller.HttpRequest.UpdateChecker.Name, MessageBoxButton.OK);
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            SaveConfig();
        }

        private void DefaultButton_Click(object sender, RoutedEventArgs e)
        {
            SettingViewModel.ModifiedConfiguration.ReconnectTimes = 4;
            SettingViewModel.ModifiedConfiguration.ConnectTimeout = SettingViewModel.ModifiedConfiguration.ProxyEnable ? 10 : 5;
            SettingViewModel.ModifiedConfiguration.Ttl = 60;
        }

        private void AutoStartupCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            // Auto startup checkbox changed
        }
    }
}
