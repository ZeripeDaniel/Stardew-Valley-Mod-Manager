using System;
using System.Windows;
using System.Windows.Media.Imaging;

namespace StardewValley_Mod_Manager
{
    public partial class ProgressPopup : Window
    {
        public bool IsCancelled { get; private set; } = false;

        public ProgressPopup()
        {
            InitializeComponent();
            this.Topmost = true;
            LoadGif();
        }

        private void LoadGif()
        {
            string gifPath = "/Resources/chick.gif";
            var gifUri = new Uri(gifPath, UriKind.RelativeOrAbsolute);
            var image = new BitmapImage(gifUri);
            WpfAnimatedGif.ImageBehavior.SetAnimatedSource(LoadingGif, image);
            WpfAnimatedGif.ImageBehavior.SetRepeatBehavior(LoadingGif, System.Windows.Media.Animation.RepeatBehavior.Forever);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsCancelled = true;
            this.Close();
        }

        public void UpdateProgress(int value, int maximum)
        {
            ProgressBar.Maximum = maximum;
            ProgressBar.Value = value;
        }
    }
}
