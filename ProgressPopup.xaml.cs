using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace StardewValley_Mod_Manager
{
    public partial class ProgressPopup : Window
    {
        public bool IsCancelled { get; private set; } = false;

        public ProgressPopup()
        {
            InitializeComponent();
            this.Topmost = true; // ProgressPopup을 항상 위에 오도록 설정
            LoadGif();
        }
        private void LoadGif()
        {
            string gifPath = "/Resources/chick.gif"; // your gif file path
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
