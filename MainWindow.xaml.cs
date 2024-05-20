using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Xml.Linq;
using Microsoft.Win32;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Effects;



namespace StardewValley_Mod_Manager
{
    public partial class MainWindow : Window
    {
        private SoundPlayer soundPlayer;
        private string smapiExecutablePath = string.Empty;
        public ObservableCollection<FileItem> FolderContents { get; set; }
        public ObservableCollection<FileItem> InnerFolderContents { get; set; }

        private bool allSelected = true; // 전체 선택 상태

        public MainWindow()
        {
            InitializeComponent();
            // SMAPI 경로 설정 여부 확인 및 설정 창 표시
            if (string.IsNullOrEmpty(ConfigManager.ReadSetting("SmapiPath")))
            {
                string detectedPath = DetectSmapiPath();
                if (!string.IsNullOrEmpty(detectedPath))
                {
                    smapiExecutablePath = detectedPath;
                    SaveConfig("SmapiPath", smapiExecutablePath);
                }
                else
                {
                    MessageBox.Show("SMAPi가 설치되어있지 않거나 StardewValley 폴더를 찾을 수 없습니다 수동으로 설정 해 주세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    HandleSettings();
                }
            }
            FolderContents = new ObservableCollection<FileItem>();
            InnerFolderContents = new ObservableCollection<FileItem>();
            FolderContentsListView.ItemsSource = FolderContents;
            InnerFolderContentsListView.ItemsSource = InnerFolderContents;

            try
            {
                ConfigManager.ValidateConfig(); // config.xml 검사 및 유효하지 않은 항목 제거
                LoadConfig();
            }
            catch (Exception ex)
            {
                // 예외가 발생하면 설정 창을 먼저 띄우고 설정을 완료한 후 프로그램 재시작
                MessageBox.Show("설정 파일을 불러올 수 없습니다. 설정을 먼저 완료해주세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                HandleSettings();
                return;
            }

            soundPlayer = new SoundPlayer(Properties.Resources.Cowboy);
            FontSizeComboBox.SelectedIndex = 0;
            ApplyFontSize();

            foreach (var item in FolderContents)
            {
                item.IsChecked = allSelected;
                item.ImageSource = new BitmapImage(new Uri(allSelected ? "/Resources/Checkboxcheck.png" : "/Resources/Checkbox.png", UriKind.RelativeOrAbsolute));
            }
            FolderContentsListView.Items.Refresh();
        }

        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            soundPlayer.Play();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }
        private void HandleSettings()
        {
            // 설정 창을 먼저 띄우고 설정을 완료한 후 프로그램 재시작
            var settingsWindow = new SettingsWindow();
            if (settingsWindow.ShowDialog() == true)
            {
                // 설정 완료 후 관리자 권한으로 프로그램 재시작
                var processInfo = new ProcessStartInfo
                {
                    FileName = Process.GetCurrentProcess().MainModule.FileName,
                    UseShellExecute = true,
                    Verb = "runas"  // 관리자 권한으로 실행
                };
                try
                {
                    Process.Start(processInfo);
                }
                catch (Exception ex2)
                {
                    MessageBox.Show("프로그램을 관리자 권한으로 재시작할 수 없습니다: " + ex2.Message, "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                Application.Current.Shutdown();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }
        private string DetectSmapiPath()
        {
            List<string> possiblePaths = new List<string>();

            // 모든 드라이브 문자에 대해 가능한 경로 추가
            foreach (var drive in DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Fixed))
            {
                string driveRoot = drive.RootDirectory.FullName;
                possiblePaths.Add(Path.Combine(driveRoot, "Program Files (x86)", "Steam", "steamapps", "common", "Stardew Valley", "StardewModdingAPI.exe"));
                possiblePaths.Add(Path.Combine(driveRoot, "Program Files", "Steam", "steamapps", "common", "Stardew Valley", "StardewModdingAPI.exe"));
                possiblePaths.Add(Path.Combine(driveRoot, "GOG Galaxy", "Games", "Stardew Valley", "StardewModdingAPI.exe"));
            }

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    return path;
                }
            }

            return null;
        }
        private void LoadConfig()
        {
            if (!File.Exists(ConfigManager.ConfigFilePath) || IsConfigFileEmpty())
            {
                File.Delete(ConfigManager.ConfigFilePath);
                HandleSettings();
                return;
            }

            ConfigManager.ValidateConfig(); // config.xml 검사 및 유효하지 않은 항목 제거

            smapiExecutablePath = ConfigManager.ReadSetting("SmapiPath");
            LoadFolders();

            if (FolderComboBox.Items.Count > 0)
            {
                FolderComboBox.SelectedIndex = 0;
            }
        }


        //private void LoadConfig()
        //{
        //    if (!File.Exists(ConfigManager.ConfigFilePath) || IsConfigFileEmpty())
        //    {
        //        File.Delete(ConfigManager.ConfigFilePath);
        //        HandleSettings();
        //        return;
        //    }

        //    selectedFolderPath = ConfigManager.ReadSetting("LinkPath");
        //    smapiExecutablePath = ConfigManager.ReadSetting("SmapiPath");
        //    LoadFolders();

        //    if (FolderComboBox.Items.Count > 0)
        //    {
        //        FolderComboBox.SelectedIndex = 0;
        //    }
        //}

        private bool IsConfigFileEmpty()
        {
            try
            {
                var doc = XDocument.Load(ConfigManager.ConfigFilePath);
                return doc.Root == null || !doc.Root.HasElements;
            }
            catch
            {
                return true;
            }
        }

        private void SaveConfig(string key, string value)
        {
            ConfigManager.WriteSetting(key, value);
        }

        private void DisplayFolderContents(string folderName)
        {
            FolderContents.Clear();
            var folderElement = ConfigManager.GetFolderElement(folderName);
            if (folderElement != null)
            {
                foreach (var innerFolder in folderElement.Elements("Inner_Folder"))
                {
                    FolderContents.Add(new FileItem
                    {
                        Name = innerFolder.Attribute("name").Value,
                        IsChecked = false,
                        IsFolder = true,
                        ImageSource = new BitmapImage(new Uri("/Resources/Checkbox.png", UriKind.RelativeOrAbsolute))
                    });
                }
            }
        }
        private void DisplayInnerFolderContents(string folderName)
        {
            InnerFolderContents.Clear();
            var parentFolderElement = ConfigManager.GetFolderElement(FolderComboBox.SelectedItem.ToString());
            var folderElement = parentFolderElement.Elements("Inner_Folder")
                                                   .FirstOrDefault(f => f.Attribute("name").Value == folderName);

            if (folderElement != null)
            {
                foreach (var innerFolder in folderElement.Elements("Inner_Folder"))
                {
                    InnerFolderContents.Add(new FileItem
                    {
                        Name = innerFolder.Attribute("name").Value,
                        IsChecked = false,
                        IsFolder = true,
                        ImageSource = new BitmapImage(new Uri("/Resources/Checkbox.png", UriKind.RelativeOrAbsolute))
                    });
                }

                foreach (var file in folderElement.Elements("File"))
                {
                    InnerFolderContents.Add(new FileItem
                    {
                        Name = file.Attribute("name").Value,
                        IsChecked = false,
                        IsFolder = false,
                        ImageSource = new BitmapImage(new Uri("/Resources/Checkbox.png", UriKind.RelativeOrAbsolute))
                    });
                }
            }
        }
        private void FolderContentsListView_Click(object sender, MouseButtonEventArgs e)
        {
            if (FolderContentsListView.SelectedItem is FileItem selectedItem && selectedItem.IsFolder)
            {
                string selectedFolder = selectedItem.Name;
                DisplayInnerFolderContents(selectedFolder);
            }
        }

        private void FolderContentsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (FolderContentsListView.SelectedItem is FileItem selectedItem && selectedItem.IsFolder)
            {
                string selectedFolder = selectedItem.Name;
                var parentFolderElement = ConfigManager.GetFolderElement(FolderComboBox.SelectedItem.ToString());
                var folderElement = parentFolderElement.Elements("Inner_Folder")
                                                       .FirstOrDefault(f => f.Attribute("name").Value == selectedFolder);

                if (folderElement != null)
                {
                    string folderPath = folderElement.Attribute("path").Value;
                    Process.Start("explorer.exe", folderPath);
                }
            }
        }
        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is FileItem fileItem)
            {
                fileItem.IsChecked = !fileItem.IsChecked;
                fileItem.ImageSource = new BitmapImage(new Uri(fileItem.IsChecked ? "/Resources/Checkboxcheck.png" : "/Resources/Checkbox.png", UriKind.RelativeOrAbsolute));
                FolderContentsListView.Items.Refresh();
                InnerFolderContentsListView.Items.Refresh();
            }
        }

        public class FileItem
        {
            public string Name { get; set; }
            public bool IsChecked { get; set; }
            public bool IsFolder { get; set; }
            public BitmapImage ImageSource { get; set; }
        }


        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void OpenPopupButton_Click(object sender, RoutedEventArgs e)
        {
            Credit popup = new Credit();
            popup.ShowDialog();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            if (settingsWindow.ShowDialog() == true)
            {
                smapiExecutablePath = settingsWindow.SmapiPath;
                SaveConfig("SmapiPath", smapiExecutablePath);
            }
        }

        private void LoadFolders()
        {
            FolderComboBox.Items.Clear();
            var folders = ConfigManager.GetFolders();
            foreach (var folder in folders)
            {
                FolderComboBox.Items.Add(folder);
            }
        }


        private void FolderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FolderComboBox.SelectedItem != null)
            {
                string selectedFolder = FolderComboBox.SelectedItem.ToString();
                DisplayFolderContents(selectedFolder);
                DisplayInnerFolderContents(selectedFolder);
                allSelected = true;
                foreach (var item in FolderContents)
                {
                    item.IsChecked = allSelected;
                    item.ImageSource = new BitmapImage(new Uri(allSelected ? "/Resources/Checkboxcheck.png" : "/Resources/Checkbox.png", UriKind.RelativeOrAbsolute));
                }
                FolderContentsListView.Items.Refresh();
            }
        }


        private void LoadFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string folderPath = dialog.SelectedPath;
                string folderName = new DirectoryInfo(folderPath).Name;

                ConfigManager.SaveFolder(folderName, folderPath);
                LoadFolders();
            }
        }

        private async void RunWithModsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(smapiExecutablePath))
                {
                    ShowOverlay();
                    var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = smapiExecutablePath,
                        Arguments = "%command%",
                        UseShellExecute = true
                    });

                    if (process != null)
                    {
                        await MonitorProcessAsync(process);
                    }

                    HideOverlay();
                }
                else
                {
                    MessageBox.Show("SMAPI 실행 파일을 찾을 수 없습니다. 경로를 확인하세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"SMAPI 실행 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void RunMods()
        {
            try
            {
                if (File.Exists(smapiExecutablePath))
                {
                    if (Path.GetFileName(smapiExecutablePath) != "StardewModdingAPI.exe")
                    {
                        MessageBox.Show("SMAPI 실행 파일이 아닙니다. 경로를 확인하세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    ShowOverlay();
                    var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = smapiExecutablePath,
                        Arguments = "%command%",
                        UseShellExecute = true
                    });

                    if (process != null)
                    {
                        await MonitorProcessAsync(process);
                    }

                    HideOverlay();
                }
                else
                {
                    MessageBox.Show("SMAPI 실행 파일을 찾을 수 없습니다. 경로를 확인하세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"SMAPI 실행 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RunNoneModsButton_Click(object sender, RoutedEventArgs e)
        {
            string stardewExecutablePath = Path.Combine(Path.GetDirectoryName(smapiExecutablePath), "Stardew Valley.exe");

            try
            {
                if (File.Exists(stardewExecutablePath))
                {
                    ShowOverlay();
                    var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = stardewExecutablePath,
                        UseShellExecute = true
                    });

                    if (process != null)
                    {
                        await MonitorProcessAsync(process);
                    }

                    HideOverlay();
                }
                else
                {
                    MessageBox.Show("스타듀밸리 실행 파일을 찾을 수 없습니다. 경로를 확인하세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"스타듀밸리 실행 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void FontSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFontSize();
        }
        private void ApplyFontSize()
        {
            if (FontSizeComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                if (double.TryParse(selectedItem.Tag.ToString(), out double fontSize))
                {
                    FolderContentsListView.FontSize = fontSize;
                    InnerFolderContentsListView.FontSize = fontSize;
                }
            }
        }

        private async void CopyAndLinkButton_Click(object sender, RoutedEventArgs e)
        {
            await CopyAndLinkAsync();
        }
        private async Task CopyAndLinkAsync()
        {
            if (string.IsNullOrEmpty(smapiExecutablePath))
            {
                MessageBox.Show("SMAPI 경로가 설정되지 않았습니다. 설정에서 경로를 지정해주세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string smapiDirectory = Path.GetDirectoryName(smapiExecutablePath);

            if (string.IsNullOrEmpty(smapiDirectory) || !Directory.Exists(smapiDirectory))
            {
                MessageBox.Show("SMAPI 경로가 올바르지 않습니다. 설정에서 경로를 확인해주세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValleyModManager");
            string tempPath = Path.Combine(basePath, "Modtemp");
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, true);
            }

            Directory.CreateDirectory(tempPath);

            if (FolderComboBox.SelectedItem == null)
            {
                MessageBox.Show("폴더를 선택해주세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string selectedFolder = FolderComboBox.SelectedItem.ToString();
            var folderElement = ConfigManager.GetFolderElement(selectedFolder);
            if (folderElement == null)
            {
                MessageBox.Show("선택된 폴더를 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var items = FolderContents.Where(f => f.IsChecked).ToList(); // 체크된 항목만 선택
            int totalItems = items.Count;

            // 프로그래스바 팝업 창 생성
            var progressPopup = new ProgressPopup();
            progressPopup.ProgressBar.Maximum = totalItems;
            progressPopup.Show();

            int itemIndex = 0;
            foreach (var item in items)
            {
                if (progressPopup.IsCancelled)
                {
                    MessageBox.Show("작업이 취소되었습니다.", "취소됨", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                }

                // 'config.xml'에서 'Inner_Folder'의 'name'을 사용하여 경로를 찾기
                var innerFolderElement = folderElement.Elements("Inner_Folder")
                                                      .FirstOrDefault(x => (string)x.Attribute("name") == item.Name);
                if (innerFolderElement == null)
                {
                    MessageBox.Show($"선택된 Inner_Folder를 찾을 수 없습니다: {item.Name}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }

                string sourcePath = (string)innerFolderElement.Attribute("path");
                string destinationPath = Path.Combine(tempPath, item.Name);

                if (!Directory.Exists(sourcePath))
                {
                    MessageBox.Show($"소스 경로를 찾을 수 없습니다: {sourcePath}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }

                await Task.Run(() => DirectoryCopy(sourcePath, destinationPath, true));

                itemIndex++;
                progressPopup.UpdateProgress(itemIndex, totalItems);
            }

            progressPopup.Close();

            if (!progressPopup.IsCancelled)
            {
                string modsPath = Path.Combine(smapiDirectory, "Mods");
                if (Directory.Exists(modsPath))
                {
                    Directory.Delete(modsPath, true);
                }

                bool success = CreateSymbolicLink(modsPath, tempPath, SymbolicLink.Directory);
                if (!success)
                {
                    MessageBox.Show("심볼릭 링크 생성에 실패했습니다. 관리자 권한이 필요할 수 있습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    RunMods();
                    //MessageBox.Show("Mods 폴더가 성공적으로 링크되었습니다.", "완료", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        //private async Task CopyAndLinkAsync()
        //{
        //    if (string.IsNullOrEmpty(smapiExecutablePath))
        //    {
        //        MessageBox.Show("SMAPI 경로가 설정되지 않았습니다. 설정에서 경로를 지정해주세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        //        return;
        //    }

        //    string smapiDirectory = Path.GetDirectoryName(smapiExecutablePath);

        //    if (string.IsNullOrEmpty(smapiDirectory) || !Directory.Exists(smapiDirectory))
        //    {
        //        MessageBox.Show("SMAPI 경로가 올바르지 않습니다. 설정에서 경로를 확인해주세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        //        return;
        //    }

        //    string basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValleyModManager");
        //    string tempPath = Path.Combine(basePath, "Modtemp");
        //    if (Directory.Exists(tempPath))
        //    {
        //        Directory.Delete(tempPath, true);
        //    }

        //    Directory.CreateDirectory(tempPath);

        //    if (FolderComboBox.SelectedItem == null)
        //    {
        //        MessageBox.Show("폴더를 선택해주세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        //        return;
        //    }

        //    string selectedFolder = FolderComboBox.SelectedItem.ToString();
        //    var folderElement = ConfigManager.GetFolderElement(selectedFolder);
        //    if (folderElement == null)
        //    {
        //        MessageBox.Show("선택된 폴더를 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        //        return;
        //    }

        //    var items = FolderContents.Where(f => f.IsChecked).ToList(); // 체크된 항목만 선택
        //    int totalItems = items.Count;

        //    // 프로그래스바 팝업 창 생성
        //    var progressPopup = new ProgressPopup();
        //    progressPopup.ProgressBar.Maximum = totalItems;
        //    progressPopup.Show();

        //    int itemIndex = 0;
        //    foreach (var item in items)
        //    {
        //        if (progressPopup.IsCancelled)
        //        {
        //            MessageBox.Show("작업이 취소되었습니다.", "취소됨", MessageBoxButton.OK, MessageBoxImage.Information);
        //            break;
        //        }

        //        // 'config.xml'에서 'Inner_Folder'의 'name'을 사용하여 경로를 찾기
        //        var innerFolderElement = folderElement.Elements("Inner_Folder")
        //                                              .FirstOrDefault(x => (string)x.Attribute("name") == item.Name);
        //        if (innerFolderElement == null)
        //        {
        //            MessageBox.Show($"선택된 Inner_Folder를 찾을 수 없습니다: {item.Name}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        //            continue;
        //        }

        //        string sourcePath = (string)innerFolderElement.Attribute("path");
        //        string destinationPath = Path.Combine(tempPath, item.Name);

        //        if (!Directory.Exists(sourcePath))
        //        {
        //            MessageBox.Show($"소스 경로를 찾을 수 없습니다: {sourcePath}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        //            continue;
        //        }

        //        await Task.Run(() => DirectoryCopy(sourcePath, destinationPath, true));

        //        itemIndex++;
        //        progressPopup.UpdateProgress(itemIndex, totalItems);
        //    }

        //    progressPopup.Close();

        //    if (!progressPopup.IsCancelled)
        //    {
        //        string modsPath = Path.Combine(smapiDirectory, "Mods");
        //        if (Directory.Exists(modsPath))
        //        {
        //            Directory.Delete(modsPath, true);
        //        }

        //        bool success = CreateSymbolicLink(modsPath, tempPath, SymbolicLink.Directory);
        //        if (!success)
        //        {
        //            MessageBox.Show("심볼릭 링크 생성에 실패했습니다. 관리자 권한이 필요할 수 있습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        //        }
        //        else
        //        {
        //            RunMods();
        //            //MessageBox.Show("Mods 폴더가 성공적으로 링크되었습니다.", "완료", MessageBoxButton.OK, MessageBoxImage.Information);
        //        }
        //    }
        //}
        private void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            Directory.CreateDirectory(destDirName);

            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, false);
            }

            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }
        private void ToggleSelectAll_Click(object sender, RoutedEventArgs e)
        {
            allSelected = !allSelected;
            foreach (var item in FolderContents)
            {
                item.IsChecked = allSelected;
                item.ImageSource = new BitmapImage(new Uri(allSelected ? "/Resources/Checkboxcheck.png" : "/Resources/Checkbox.png", UriKind.RelativeOrAbsolute));
            }
            FolderContentsListView.Items.Refresh();
        }
        private async Task MonitorProcessAsync(Process process)
        {
            await Task.Run(() =>
            {
                process.WaitForExit();
            });

            // 프로세스가 종료되면 UI 스레드에서 Overlay를 숨깁니다.
            Dispatcher.Invoke(() => HideOverlay());
        }

        private void ShowOverlay()
        {
            OverlayGrid.Visibility = Visibility.Visible;
            MainGrid.Effect = new BlurEffect { Radius = 10 };
        }

        private void HideOverlay()
        {
            OverlayGrid.Visibility = Visibility.Collapsed;
            MainGrid.Effect = null;
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);

        enum SymbolicLink
        {
            File = 0,
            Directory = 1
        }

    }
}
