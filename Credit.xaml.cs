using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// <summary>
    /// Credit.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class Credit : Window
    {
        public Credit()
        {
            InitializeComponent();
        }
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void ZZText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            string url = "https://www.youtube.com/@Maker_ZZ"; // 유튜브 채널 URL로 변경
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"링크를 열 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CrickText_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            string url = "https://www.youtube.com/@J_Crick"; // 유튜브 채널 URL로 변경
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"링크를 열 수 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
