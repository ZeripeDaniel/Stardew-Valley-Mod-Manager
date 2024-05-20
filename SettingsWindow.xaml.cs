using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StardewValley_Mod_Manager
{
    public partial class SettingsWindow : Window
    {
        public string SelectedPath { get; private set; }
        public string SmapiPath { get; private set; }
        public string SelectedFontResourceKey { get; private set; }

        public SettingsWindow()
        {
            InitializeComponent();
            SmapiPathTextBox.Text = ConfigManager.ReadSetting("SmapiPath");
            SelectedFontResourceKey = ConfigManager.ReadSetting("SelectedFont");
            FontComboBox.SelectedValue = SelectedFontResourceKey;
        }

        private void SmapiBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var fileDialog = new Ookii.Dialogs.Wpf.VistaOpenFileDialog();
            fileDialog.Filter = "Executable Files|*.exe";
            if (fileDialog.ShowDialog() == true)
            {
                SmapiPathTextBox.Text = fileDialog.FileName;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SmapiPath = SmapiPathTextBox.Text;
            SelectedFontResourceKey = ((ComboBoxItem)FontComboBox.SelectedItem).Tag.ToString();

            ConfigManager.WriteSetting("SmapiPath", SmapiPath);
            ConfigManager.WriteSetting("SelectedFont", SelectedFontResourceKey);
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}