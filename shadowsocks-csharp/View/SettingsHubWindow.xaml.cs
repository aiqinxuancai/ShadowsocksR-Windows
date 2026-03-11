using RunAtStartup;
using Shadowsocks.Controller;
using Shadowsocks.Enums;
using Shadowsocks.Model;
using Shadowsocks.Util;
using Shadowsocks.View.Controls;
using Shadowsocks.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Shadowsocks.View
{
    public enum SettingsHubTab { General, Dns, Port, Subscribe }

    public partial class SettingsHubWindow
    {
        private readonly MainController _controller;
        private Configuration _modifiedConfiguration;
        private Configuration _observedConfiguration;
        private Configuration _modifiedPortConfiguration;
        private SettingsHubTab _initialTab;
        private event EventHandler PortValueChanged;
        private int _oldSelectedPortIndex = -1;
        private bool _isDeleteServer;
        private bool _isLoading;
        private bool _isSaving;
        private bool _portEventsInitialized;
        private bool _settingsDirty;
        private bool _dnsDirty;
        private bool _portDirty;
        private bool _subscribeDirty;

        public SettingsHubWindow(MainController controller, SettingsHubTab initialTab = SettingsHubTab.General)
        {
            InitializeComponent();
            _controller = controller;
            _initialTab = initialTab;
            SetLanguageResources();
            _controller.ConfigChanged += Controller_ConfigChanged;
            DnsSettingViewModel.DnsClientsChanged += DnsSettingViewModel_DnsClientsChanged;
            SubscribeWindowViewModel.SubscribesChanged += SubscribeWindowViewModel_SubscribesChanged;
            Closed += SettingsHubWindow_Closed;
            LoadAllConfigurations();
        }

        public SettingViewModel SettingViewModel { get; } = new();
        public DnsSettingViewModel DnsSettingViewModel { get; } = new();
        public SubscribeWindowViewModel SubscribeWindowViewModel { get; } = new();

        public void SelectTab(SettingsHubTab tab)
        {
            _initialTab = tab;
            if (SettingsTabControl != null)
            {
                SettingsTabControl.SelectedIndex = (int)tab;
            }
        }

        private void SettingsHubWindow_Closed(object sender, EventArgs e)
        {
            _controller.ConfigChanged -= Controller_ConfigChanged;
            DnsSettingViewModel.DnsClientsChanged -= DnsSettingViewModel_DnsClientsChanged;
            SubscribeWindowViewModel.SubscribesChanged -= SubscribeWindowViewModel_SubscribesChanged;
            if (_observedConfiguration != null)
            {
                _observedConfiguration.PropertyChanged -= ModifiedConfiguration_PropertyChanged;
            }
        }

        private void SetLanguageResources()
        {
            I18NUtil.SetLanguage(Resources, @"SettingsHubWindow");
            I18NUtil.SetLanguage(GeneralTabRoot.Resources, @"SettingsWindow");
            I18NUtil.SetLanguage(DnsTabRoot.Resources, @"DnsSettingWindow");
            I18NUtil.SetLanguage(PortTabRoot.Resources, @"PortSettingsWindow");
            I18NUtil.SetLanguage(SubscribeTabRoot.Resources, @"SubscribeWindow");
        }

        private static string GetResourceString(ResourceDictionary resources, string key)
        {
            if (resources.Contains(key) && resources[key] is string value)
            {
                return value;
            }

            foreach (var dictionary in resources.MergedDictionaries)
            {
                if (dictionary.Contains(key) && dictionary[key] is string mergedValue)
                {
                    return mergedValue;
                }
            }

            return key;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            InitializePortTab();
            UpdateSubscribeInfoVisibility();
            SelectTab(_initialTab);
        }

        private void InitializePortTab()
        {
            if (_portEventsInitialized) return;
            LoadPortItems();
            PortValueChanged += PortValueChanged_Handler;

            foreach (var textBox in ViewUtils.FindVisualChildren<TextBox>(PortTabRoot))
            {
                if (textBox.Name.EndsWith(@"TextBox"))
                {
                    textBox.TextChanged += (_, _) => PortValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            foreach (var checkBox in ViewUtils.FindVisualChildren<CheckBox>(PortTabRoot))
            {
                checkBox.Checked += (_, _) => PortValueChanged?.Invoke(this, EventArgs.Empty);
                checkBox.Unchecked += (_, _) => PortValueChanged?.Invoke(this, EventArgs.Empty);
            }
            foreach (var comboBox in ViewUtils.FindVisualChildren<ComboBox>(PortTabRoot))
            {
                comboBox.SelectionChanged += (_, _) => PortValueChanged?.Invoke(this, EventArgs.Empty);
            }
            foreach (var numberUpDown in ViewUtils.FindVisualChildren<NumberUpDown>(PortTabRoot))
            {
                numberUpDown.ValueChanged += (_, _) => PortValueChanged?.Invoke(this, EventArgs.Empty);
            }
            _portEventsInitialized = true;
        }

        private void LoadAllConfigurations()
        {
            _isLoading = true;
            try
            {
                LoadMainConfiguration();
                DnsSettingViewModel.ReadConfig();
                LoadPortConfiguration();
                ResetDirtyFlags();
                UpdateWindowTitle();
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void LoadMainConfiguration()
        {
            if (_observedConfiguration != null)
            {
                _observedConfiguration.PropertyChanged -= ModifiedConfiguration_PropertyChanged;
            }
            _modifiedConfiguration = Global.Load();
            SettingViewModel.ModifiedConfiguration = _modifiedConfiguration;
            _observedConfiguration = SettingViewModel.ModifiedConfiguration;
            _observedConfiguration.PropertyChanged += ModifiedConfiguration_PropertyChanged;
            SubscribeWindowViewModel.ReadConfig(_modifiedConfiguration);
            AutoStartupCheckBox.IsChecked = AutoStartup.Check();
            _isDeleteServer = false;
        }

        private void LoadPortConfiguration()
        {
            _modifiedPortConfiguration = Global.Load();
            LoadPortConfiguration(_modifiedPortConfiguration);
            LoadSelectedPort();
            if (PortTypeComboBox.Items.Count > 0 && PortTypeComboBox.SelectedIndex == -1)
            {
                PortTypeComboBox.SelectedIndex = 0;
            }
        }

        private void Controller_ConfigChanged(object sender, EventArgs e)
        {
            if (!_isSaving) LoadAllConfigurations();
        }

        private void ModifiedConfiguration_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) => MarkSettingsDirty();
        private void DnsSettingViewModel_DnsClientsChanged(object sender, EventArgs e) => MarkDnsDirty();
        private void SubscribeWindowViewModel_SubscribesChanged(object sender, EventArgs e) => MarkSubscribeDirty();

        private void MarkSettingsDirty() { if (_isLoading) return; _settingsDirty = true; RefreshApplyButton(); }
        private void MarkDnsDirty() { if (_isLoading) return; _dnsDirty = true; RefreshApplyButton(); }
        private void MarkPortDirty() { if (_isLoading) return; _portDirty = true; RefreshApplyButton(); }
        private void MarkSubscribeDirty() { if (_isLoading) return; _subscribeDirty = true; RefreshApplyButton(); }

        private void RefreshApplyButton() => ApplyButton.IsEnabled = _settingsDirty || _dnsDirty || _portDirty || _subscribeDirty;

        private void ResetDirtyFlags()
        {
            _settingsDirty = _dnsDirty = _portDirty = _subscribeDirty = false;
            ApplyButton.IsEnabled = false;
        }

        private void UpdateWindowTitle()
        {
            Title = $@"{this.GetWindowStringValue(@"Title")}({(Global.GuiConfig.ShareOverLan ? GetResourceString(GeneralTabRoot.Resources, @"Any") : GetResourceString(GeneralTabRoot.Resources, @"Local"))}:{Global.GuiConfig.LocalPort} {GetResourceString(GeneralTabRoot.Resources, @"Version")}:{Controller.HttpRequest.UpdateChecker.FullVersion})";
        }

        private bool SaveAll()
        {
            if (!ApplyButton.IsEnabled) return true;
            if ((_settingsDirty || _subscribeDirty) && !PrepareMainConfigurationForSave())
            {
                ShowSubscribeSaveError();
                return false;
            }

            _isSaving = true;
            try
            {
                if (_settingsDirty || _subscribeDirty) SaveMainConfiguration();
                if (_dnsDirty) DnsSettingViewModel.SaveConfig();
                if (_portDirty) SavePortConfiguration();
                ResetDirtyFlags();
                UpdateWindowTitle();
                return true;
            }
            finally
            {
                _isSaving = false;
            }
        }

        private bool PrepareMainConfigurationForSave()
        {
            var tags = new HashSet<string>();
            foreach (var serverSubscribe in SubscribeWindowViewModel.SubscribeCollection)
            {
                if (!tags.Add(serverSubscribe.Tag)) return false;
            }

            _modifiedConfiguration.ServerSubscribes.Clear();
            _modifiedConfiguration.ServerSubscribes.AddRange(SubscribeWindowViewModel.SubscribeCollection);

            if (_modifiedConfiguration.Configs.Any(server => !string.IsNullOrEmpty(server.SubTag) && _modifiedConfiguration.ServerSubscribes.All(subscribe => subscribe.Tag != server.SubTag)))
            {
                if (MessageBox.Show(GetResourceString(SubscribeTabRoot.Resources, @"SaveQuestion"), Controller.HttpRequest.UpdateChecker.Name, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes) == MessageBoxResult.Yes)
                {
                    _modifiedConfiguration.Configs.RemoveAll(server => !string.IsNullOrEmpty(server.SubTag) && _modifiedConfiguration.ServerSubscribes.All(subscribe => subscribe.Tag != server.SubTag));
                    _isDeleteServer = true;
                }
            }
            return true;
        }

        private void SaveMainConfiguration()
        {
            if (SettingViewModel.ModifiedConfiguration.LangName != Global.GuiConfig.LangName)
            {
                MessageBox.Show(GetResourceString(GeneralTabRoot.Resources, @"RestartRequired"), Controller.HttpRequest.UpdateChecker.Name, MessageBoxButton.OK);
            }
            _controller.SaveServersConfig(_modifiedConfiguration, true);

            var isAutoStartup = AutoStartupCheckBox.IsChecked.GetValueOrDefault();
            if (isAutoStartup != AutoStartup.Check() && !AutoStartup.Set(isAutoStartup))
            {
                MessageBox.Show(GetResourceString(GeneralTabRoot.Resources, @"FailAutoStartUp"), Controller.HttpRequest.UpdateChecker.Name, MessageBoxButton.OK);
            }
            _isDeleteServer = false;
        }

        private void ShowSubscribeSaveError()
        {
            MessageBox.Show(GetResourceString(SubscribeTabRoot.Resources, @"SaveError"), Controller.HttpRequest.UpdateChecker.Name, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e) { if (SaveAll()) Close(); }
        private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
        private void ApplyButton_Click(object sender, RoutedEventArgs e) => SaveAll();
        private void DefaultButton_Click(object sender, RoutedEventArgs e)
        {
            SettingViewModel.ModifiedConfiguration.ReconnectTimes = 4;
            SettingViewModel.ModifiedConfiguration.ConnectTimeout = SettingViewModel.ModifiedConfiguration.ProxyEnable ? 10 : 5;
            SettingViewModel.ModifiedConfiguration.Ttl = 60;
        }
        private void AutoStartupCheckBox_CheckedChanged(object sender, RoutedEventArgs e) => MarkSettingsDirty();
        private void DnsAddButton_OnClick(object sender, RoutedEventArgs e) => DnsSettingViewModel.AddNewDns();
        private void DnsDeleteButton_OnClick(object sender, RoutedEventArgs e) => DnsSettingViewModel.Delete();

        private void DnsTestButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && DnsSettingViewModel.CurrentClient != null)
            {
                var client = DnsSettingViewModel.CurrentClient;
                var domain = DnsDomainTextBox.Text;
                button.IsEnabled = false;
                DnsAnswerTextBox.Text = string.Empty;
                Task.Run(async () =>
                {
                    var result = await client.QueryIpAddressAsync(domain, default);
                    Dispatcher?.InvokeAsync(() => { DnsAnswerTextBox.Text = $@"{result}"; });
                }).ContinueWith(_ => { Dispatcher?.InvokeAsync(() => { button.IsEnabled = true; }); });
            }
        }

        private void PortValueChanged_Handler(object sender, EventArgs e) => MarkPortDirty();

        private void LoadPortItems()
        {
            PortTypeComboBox.ItemsSource = new[]
            {
                new { Text = this.GetWindowStringValue(@"PortForward"), Value = PortMapType.Forward },
                new { Text = this.GetWindowStringValue(@"ForceProxy"), Value = PortMapType.ForceProxy },
                new { Text = this.GetWindowStringValue(@"ProxyWithRule"), Value = PortMapType.RuleProxy }
            };
        }

        private void LoadPortConfiguration(Configuration configuration)
        {
            PortServersComboBox.Items.Clear();
            PortServersComboBox.Items.Add(string.Empty);
            var serverGroup = new Dictionary<string, int>();
            foreach (var server in configuration.Configs)
            {
                if (!string.IsNullOrEmpty(server.Group) && !serverGroup.ContainsKey(server.Group))
                {
                    PortServersComboBox.Items.Add(@"#" + server.Group);
                    serverGroup[server.Group] = 1;
                }
            }
            foreach (var server in configuration.Configs)
            {
                PortServersComboBox.Items.Add(GetDisplayText(server));
            }

            PortPortsListBox.Items.Clear();
            var list = new int[configuration.PortMap.Count];
            var listIndex = 0;
            foreach (var item in configuration.PortMap)
            {
                int.TryParse(item.Key, out list[listIndex]);
                listIndex += 1;
            }
            Array.Sort(list);
            foreach (var port in list)
            {
                var remarks = configuration.PortMap[port.ToString()].Remarks ?? string.Empty;
                PortPortsListBox.Items.Add(port + @"    " + remarks);
            }

            _oldSelectedPortIndex = -1;
            if (PortPortsListBox.Items.Count > 0)
            {
                PortPortsListBox.SelectedIndex = 0;
            }
        }

        private void SaveSelectedPort()
        {
            if (_oldSelectedPortIndex == -1) return;
            var refreshList = false;
            var key = _oldSelectedPortIndex.ToString();
            if (key != PortLocalPortNumber.NumValue.ToString())
            {
                if (_modifiedPortConfiguration.PortMap.ContainsKey(key))
                {
                    _modifiedPortConfiguration.PortMap.Remove(key);
                }
                refreshList = true;
                key = PortLocalPortNumber.NumValue.ToString();
                if (!int.TryParse(key, out _oldSelectedPortIndex))
                {
                    _oldSelectedPortIndex = 0;
                }
            }
            if (!_modifiedPortConfiguration.PortMap.ContainsKey(key))
            {
                _modifiedPortConfiguration.PortMap[key] = new PortMapConfig();
            }

            var config = _modifiedPortConfiguration.PortMap[key];
            config.Enable = PortEnableCheckBox.IsChecked.GetValueOrDefault();
            config.Type = (PortMapType)PortTypeComboBox.SelectedValue;
            config.Id = GetId(PortServersComboBox.Text);
            config.Server_addr = PortTargetAddressTextBox.Text;
            config.Server_port = PortTargetPortNumber.NumValue;
            if (config.Remarks != PortRemarksTextBox.Text) refreshList = true;
            config.Remarks = PortRemarksTextBox.Text;

            if (refreshList) LoadPortConfiguration(_modifiedPortConfiguration);
        }

        private void LoadSelectedPort()
        {
            var key = ServerListText2Key((string)PortPortsListBox.SelectedItem);
            var serverGroup = new Dictionary<string, int>();
            foreach (var server in _modifiedPortConfiguration.Configs)
            {
                if (!string.IsNullOrEmpty(server.Group) && !serverGroup.ContainsKey(server.Group))
                {
                    serverGroup[server.Group] = 1;
                }
            }

            if (key != null && _modifiedPortConfiguration.PortMap.ContainsKey(key))
            {
                var config = _modifiedPortConfiguration.PortMap[key];
                PortEnableCheckBox.IsChecked = config.Enable;
                PortTypeComboBox.SelectedValue = config.Type;
                var text = GetIdText(config.Id);
                if (text.Length == 0 && serverGroup.ContainsKey(config.Id)) text = $@"#{config.Id}";
                PortServersComboBox.Text = text;
                PortLocalPortNumber.NumValue = int.Parse(key);
                PortTargetAddressTextBox.Text = config.Server_addr;
                PortTargetPortNumber.NumValue = config.Server_port;
                PortRemarksTextBox.Text = config.Remarks ?? string.Empty;
                if (!int.TryParse(key, out _oldSelectedPortIndex)) _oldSelectedPortIndex = 0;
            }
            else
            {
                PortEnableCheckBox.IsChecked = false;
                PortTypeComboBox.SelectedIndex = 0;
                PortServersComboBox.SelectedIndex = 0;
                PortLocalPortNumber.NumValue = 0;
                PortTargetAddressTextBox.Text = string.Empty;
                PortTargetPortNumber.NumValue = 0;
                PortRemarksTextBox.Text = string.Empty;
            }
        }

        private string GetIdText(string id)
        {
            foreach (var server in _modifiedPortConfiguration.Configs)
            {
                if (id == server.Id) return GetDisplayText(server);
            }
            return string.Empty;
        }

        private void SavePortConfiguration()
        {
            SaveSelectedPort();
            _controller.SaveServersPortMap(_modifiedPortConfiguration);
        }

        private void PortAddButton_Click(object sender, RoutedEventArgs e)
        {
            var hasBind = PortValueChanged != null;
            if (hasBind) PortValueChanged -= PortValueChanged_Handler;

            SaveSelectedPort();
            const string key = @"0";
            if (!_modifiedPortConfiguration.PortMap.ContainsKey(key))
            {
                _modifiedPortConfiguration.PortMap[key] = new PortMapConfig();
                PortValueChanged_Handler(this, EventArgs.Empty);
            }

            var config = _modifiedPortConfiguration.PortMap[key];
            config.Enable = PortEnableCheckBox.IsChecked.GetValueOrDefault();
            config.Type = (PortMapType)PortTypeComboBox.SelectedValue;
            config.Id = GetId(PortServersComboBox.Text);
            config.Server_addr = PortTargetAddressTextBox.Text;
            config.Remarks = PortRemarksTextBox.Text;
            config.Server_port = PortTargetPortNumber.NumValue;

            _oldSelectedPortIndex = -1;
            LoadPortConfiguration(_modifiedPortConfiguration);
            LoadSelectedPort();

            if (hasBind) PortValueChanged += PortValueChanged_Handler;
        }

        private void PortDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var key = _oldSelectedPortIndex.ToString();
            if (_modifiedPortConfiguration.PortMap.ContainsKey(key))
            {
                _modifiedPortConfiguration.PortMap.Remove(key);
            }
            _oldSelectedPortIndex = -1;
            LoadPortConfiguration(_modifiedPortConfiguration);
            LoadSelectedPort();
            PortValueChanged?.Invoke(this, EventArgs.Empty);
        }

        private void PortTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PortTypeComboBox.SelectedIndex == 0)
            {
                PortTargetAddressTextBox.IsReadOnly = false;
                PortTargetPortNumber.IsEnabled = true;
            }
            else
            {
                PortTargetAddressTextBox.IsReadOnly = true;
                PortTargetPortNumber.IsEnabled = false;
            }
        }

        private void PortPortsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;
            var hasBind = PortValueChanged != null;
            if (hasBind) PortValueChanged -= PortValueChanged_Handler;
            if (PortPortsListBox.SelectedIndex != -1)
            {
                SaveSelectedPort();
                LoadSelectedPort();
            }
            if (hasBind) PortValueChanged += PortValueChanged_Handler;
        }

        private static string GetDisplayText(Server server) => (!string.IsNullOrEmpty(server.Group) ? server.Group + @" - " : @"    - ") + server.FriendlyName + @"        #" + server.Id;

        private static string GetId(string text)
        {
            return text.IndexOf('#') >= 0 ? text.Substring(text.IndexOf('#') + 1) : text;
        }

        private static string ServerListText2Key(string text)
        {
            if (text != null)
            {
                var position = text.IndexOf(' ');
                if (position > 0) return text.Substring(0, position);
            }
            return text;
        }

        private void SubscribeListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateSubscribeInfoVisibility();

        private void UpdateSubscribeInfoVisibility()
        {
            SubscribeInfoGrid.Visibility = SubscribeListBox.SelectedIndex == -1 ? Visibility.Hidden : Visibility.Visible;
        }

        private void SubscribeAddButton_Click(object sender, RoutedEventArgs e)
        {
            SubscribeWindowViewModel.SubscribeCollection.Add(new ServerSubscribe());
            SetSubscribeListSelectedIndex(SubscribeWindowViewModel.SubscribeCollection.Count - 1);
        }

        private void SubscribeDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var index = SubscribeListBox.SelectedIndex;
            if (SubscribeListBox.SelectedItem is ServerSubscribe serverSubscribe)
            {
                var tag = serverSubscribe.Tag;
                _modifiedConfiguration.Configs.RemoveAll(server => server.SubTag == tag);
                _isDeleteServer = true;
                SubscribeWindowViewModel.SubscribeCollection.Remove(serverSubscribe);
            }
            SetSubscribeListSelectedIndex(index);
            MarkSubscribeDirty();
        }

        private void SetSubscribeListSelectedIndex(int index)
        {
            if (index < 0) return;
            if (index < SubscribeListBox.Items.Count)
            {
                SubscribeListBox.SelectedIndex = index;
                SubscribeListBox.ScrollIntoView(SubscribeListBox.Items[index]);
            }
            else
            {
                SubscribeListBox.SelectedIndex = SubscribeListBox.Items.Count - 1;
                if (SubscribeListBox.SelectedIndex > 0)
                {
                    SubscribeListBox.ScrollIntoView(SubscribeListBox.Items[SubscribeListBox.Items.Count - 1]);
                }
            }
        }

        private void SubscribeUpdateButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (SubscribeListBox.SelectedItem is ServerSubscribe serverSubscribe && SaveAll())
            {
                Global.UpdateSubscribeManager.CreateTask(Global.GuiConfig, Global.UpdateNodeChecker, true, new List<ServerSubscribe> { serverSubscribe });
            }
        }
    }
}
