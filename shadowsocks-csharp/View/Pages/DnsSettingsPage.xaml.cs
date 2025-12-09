using System.Windows.Controls;
using Wpf.Ui.Appearance;

namespace Shadowsocks.View.Pages
{
    public partial class DnsSettingsPage : Page
    {
        public DnsSettingsPage()
        {
            InitializeComponent();
            ApplicationThemeManager.Apply(this);
        }
    }
}
