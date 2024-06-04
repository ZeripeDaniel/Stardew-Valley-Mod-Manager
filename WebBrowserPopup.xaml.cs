using System;
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
            webView.Source = new Uri(url);
        }
    }
}
