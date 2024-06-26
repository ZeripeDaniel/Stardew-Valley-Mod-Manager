﻿using System.Windows;
using System.Windows.Controls;

#pragma warning disable CS8618
#pragma warning disable CS8601
#pragma warning disable CS8602
#pragma warning disable CS8603
#pragma warning disable CS8604
#pragma warning disable CS8605
#pragma warning disable CS8606
#pragma warning disable CS8607
#pragma warning disable CS8608
#pragma warning disable CS8609

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

            // Config에서 SelectedFont 값 읽기
            SelectedFontResourceKey = ConfigManager.ReadSetting("SelectedFont");

            // 설정된 SelectedFont 값이 null 또는 빈 문자열이면 0으로 설정
            if (string.IsNullOrEmpty(SelectedFontResourceKey))
            {
                FontComboBox.SelectedIndex = 0;
            }
            else
            {
                // ComboBox에서 태그와 일치하는 값을 찾아서 선택
                foreach (ComboBoxItem item in FontComboBox.Items)
                {
                    if (item.Tag.ToString() == SelectedFontResourceKey)
                    {
                        FontComboBox.SelectedItem = item;
                        break;
                    }
                }
            }

            // 시작 시 업데이트 확인 설정 읽기
            UpdateCheckBox.IsChecked = bool.Parse(ConfigManager.ReadSetting("StartUpUpdateCheck") ?? "false");
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
            // SMAPi 경로가 비어 있는지 확인하고 비어 있으면 오류 메시지를 표시하고 리턴
            if (string.IsNullOrWhiteSpace(SmapiPathTextBox.Text))
            {
                MessageBox.Show("SMAPi 경로를 설정해주세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SmapiPath = SmapiPathTextBox.Text;

            // 폰트 ComboBox에서 선택된 항목이 있는지 및 선택된 항목의 Tag 값이 있는지 확인
            if (FontComboBox.SelectedItem == null || ((ComboBoxItem)FontComboBox.SelectedItem).Tag == null)
            {
                MessageBox.Show("폰트를 선택해주세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 선택된 폰트의 Tag 값을 가져와서 설정
            SelectedFontResourceKey = ((ComboBoxItem)FontComboBox.SelectedItem).Tag.ToString();

            // 설정값을 ConfigManager를 통해 저장
            ConfigManager.WriteSetting("SmapiPath", SmapiPath);
            ConfigManager.WriteSetting("SelectedFont", SelectedFontResourceKey);

            // "시작 시 업데이트 확인" 설정 저장
            bool startUpUpdateCheck = (bool)UpdateCheckBox.IsChecked;
            ConfigManager.WriteSetting("StartUpUpdateCheck", startUpUpdateCheck.ToString());

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
