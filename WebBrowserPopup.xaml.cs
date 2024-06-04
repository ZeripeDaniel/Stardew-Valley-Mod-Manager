using System.Windows;

namespace StardewValley_Mod_Manager
{
    public partial class WebBrowserPopup : Window
    {
        public WebBrowserPopup()
        {
            InitializeComponent();
        }

        public void Navigate(string url)
        {
            webBrowser.Address = url;
        }
    }
}
