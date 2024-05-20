using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StardewValley_Mod_Manager
{
    public partial class SettingsWindow : Window
    {
        public string SelectedPath { get; private set; }
        public string SmapiPath { get; private set; }
        public FontFamily SelectedFont { get; private set; }

        public SettingsWindow()
        {
            InitializeComponent();
            SmapiPathTextBox.Text = ConfigManager.ReadSetting("SmapiPath");
            LoadFontSetting();
        }

        private void LoadFontSetting()
        {
            string fontSetting = ConfigManager.ReadSetting("SelectedFont");
            foreach (ComboBoxItem item in FontComboBox.Items)
            {
                if (item.Tag is FontFamily fontFamily && fontFamily.ToString() == fontSetting)
                {
                    FontComboBox.SelectedItem = item;
                    break;
                }
            }
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
            if (FontComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag is FontFamily fontFamily)
            {
                SelectedFont = fontFamily;
                ConfigManager.WriteSetting("SelectedFont", fontFamily.ToString());
            }

            ConfigManager.WriteSetting("SmapiPath", SmapiPath);
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
