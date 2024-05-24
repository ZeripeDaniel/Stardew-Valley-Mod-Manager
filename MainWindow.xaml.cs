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
using System.Net.Http;

#pragma warning disable CS8618
#pragma warning disable CS8600
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
    public partial class MainWindow : Window
    {
        #region 사운드인잇
        private SoundPlayer Cowboy = new SoundPlayer(Properties.Resources.Cowboy); // soundPlayer 초기화
        private SoundPlayer bigDeSelect = new SoundPlayer(Properties.Resources.bigDeSelect); // soundPlayer 초기화
        private SoundPlayer bigSelect = new SoundPlayer(Properties.Resources.bigSelect); // soundPlayer 초기화
        private SoundPlayer Cowboy_footstop = new SoundPlayer(Properties.Resources.Cowboy_Footstep); // soundPlayer 초기화
        private SoundPlayer Duck = new SoundPlayer(Properties.Resources.Duck); // soundPlayer 초기화
        private SoundPlayer Junimo = new SoundPlayer(Properties.Resources.junimo); // soundPlayer 초기화
        private SoundPlayer Leaf = new SoundPlayer(Properties.Resources.Leaf); // soundPlayer 초기화
        #endregion

        /// <summary>
        /// 경로 선언
        /// </summary>
        private string smapiExecutablePath = string.Empty;
        /// <summary>
        /// 폴더 컨텐츠 get set
        /// </summary>
        public ObservableCollection<FileItem> FolderContents { get; set; }
        /// <summary>
        /// 하단 뷰 내부폴더 컨텐츠 get set
        /// </summary>
        public ObservableCollection<FileItem> InnerFolderContents { get; set; }
        /// <summary>
        /// 전체 선택 상태
        /// </summary>
        private bool allSelected = true;

        private NexusModsApi api; // API 객체 추가
        #region 초기화
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += Window_Loaded; // Window_Loaded 이벤트 핸들러 연결

            // NexusModsApi 인스턴스 생성
            api = new NexusModsApi("fUuT6ZIEKgD4RnI6iZGpg6GFWHXCbDiXPGwnXcDp33qcm6sxSEqe--AcqV2pCTjHRwnomK--DMk9yoDD8D5MSrLw8v2JdQ==", "stardewvalley"); // 고정값 "stardewvalley"로 설정

            FolderContents = new ObservableCollection<FileItem>();
            InnerFolderContents = new ObservableCollection<FileItem>();
            FolderContentsListView.ItemsSource = FolderContents;
            InnerFolderContentsListView.ItemsSource = InnerFolderContents;

            try
            {
                LoadConfig();
            }
            catch (Exception)
            {
                // 예외가 발생하면 설정 창을 먼저 띄우고 설정을 완료한 후 프로그램 재시작
                MessageBox.Show("설정 파일을 불러올 수 없습니다. 설정을 먼저 완료해주세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                HandleSettings();
                return;
            }
            ConfigManager.RefreshFolders();

            LoadFolders();

            if (FolderListView.Items.Count > 0)
            {
                FolderListView.SelectedIndex = 0;
            }

            foreach (var item in FolderContents)
            {
                item.IsChecked = allSelected;
                item.ImageSource = new BitmapImage(new Uri(allSelected ? "/Resources/Checkboxcheck.png" : "/Resources/Checkbox.png", UriKind.RelativeOrAbsolute));
            }
            FolderContentsListView.Items.Refresh();
        }
        #endregion
        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            Cowboy.Play();
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
                    FileName = Environment.ProcessPath,
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

                // 설정 완료 후 선택된 폰트를 즉시 적용
                if (!string.IsNullOrEmpty(settingsWindow.SelectedFontResourceKey))
                {
                    ApplyFont((FontFamily)Application.Current.Resources[settingsWindow.SelectedFontResourceKey]);
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
                possiblePaths.Add(Path.Combine(driveRoot, "SteamLibrary", "steamapps", "common", "Stardew Valley", "StardewModdingAPI.exe"));
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
            try
            {
                // config.xml 검사 및 유효하지 않은 항목 제거
                ConfigManager.ValidateConfig();

                // SMAPI 경로 설정 여부 확인 및 설정 창 표시
                string smapiPath = ConfigManager.ReadSetting("SmapiPath");
                if (string.IsNullOrEmpty(smapiPath))
                {
                    string detectedPath = DetectSmapiPath();
                    if (!string.IsNullOrEmpty(detectedPath))
                    {
                        smapiExecutablePath = detectedPath;
                        ConfigManager.WriteSetting("SmapiPath", smapiExecutablePath);
                    }
                    else
                    {
                        MessageBox.Show("SMAPI가 설치되어 있지 않거나 Stardew Valley 폴더를 찾을 수 없습니다. 수동으로 설정해 주세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                        HandleSettings();
                        return;
                    }
                }
                else
                {
                    smapiExecutablePath = smapiPath;
                }

                // SelectedFont 설정 확인 및 기본값 설정
                string selectedFont = ConfigManager.ReadSetting("SelectedFont");
                if (string.IsNullOrEmpty(selectedFont))
                {
                    selectedFont = "F_SDMisaeng"; // 기본값 설정
                    ConfigManager.WriteSetting("SelectedFont", selectedFont);
                }

                int fontSizeIndex;
                if (int.TryParse(ConfigManager.ReadSetting("FontSizeIndex"), out fontSizeIndex))
                {
                    FontSizeComboBox.SelectedIndex = fontSizeIndex;
                }
                else
                {
                    FontSizeComboBox.SelectedIndex = 0; // 기본 폰트 크기
                }

                if (selectedFont == "F_SDMisaeng")
                {
                    foreach (ComboBoxItem item in FontSizeComboBox.Items)
                    {
                        int originalValue;
                        if (int.TryParse(item.Tag.ToString(), out originalValue))
                        {
                            item.Tag = (originalValue + 10).ToString();
                            //item.Content = "기본" + (originalValue + 10); // 기본으로 시작하는 항목 수정
                        }
                    }
                    AllSelectNDeSelect.FontSize = 20;
                    LaunchedGame.FontSize = 20;
                    FolderListView.FontSize = 20;
                    FontSizeComboBox.FontSize = 20;
                }

                ApplyFontSize();
                ApplyFont((FontFamily)Application.Current.Resources[selectedFont]); // 저장된 폰트 설정 적용
            }
            catch (Exception ex)
            {
                // 예외를 로그 파일에 기록
                ConfigManager.LogException(ex);
            }
        }
        private void SaveConfig(string key, string value)
        {
            ConfigManager.WriteSetting(key, value);
        }
        //private void DisplayFolderContents(string folderName)
        //{
        //    FolderContents.Clear();
        //    var folderElement = ConfigManager.GetFolderElement(folderName);
        //    if (folderElement != null)
        //    {
        //        foreach (var innerFolder in folderElement.Elements("Inner_Folder"))
        //        {
        //            var isChecked = bool.Parse(innerFolder.Attribute("IsChecked")?.Value ?? "true");
        //            var fileItem = new FileItem
        //            {
        //                Name = innerFolder.Attribute("name").Value,
        //                IsChecked = isChecked,
        //                IsFolder = true,
        //                ImageSource = new BitmapImage(new Uri(isChecked ? "/Resources/Checkboxcheck.png" : "/Resources/Checkbox.png", UriKind.RelativeOrAbsolute)),
        //                CurrentVersion = innerFolder.Attribute("version")?.Value,
        //                LatestVersion = innerFolder.Attribute("LatestVersion")?.Value ?? "버전정보없음",
        //                UpdateKey = innerFolder.Attribute("UpdateKey")?.Value
        //            };

        //            FolderContents.Add(fileItem);
        //        }
        //    }

        //    FolderContentsListView.Items.Refresh(); // FolderContents가 변경되었음을 UI에 알림
        //}
        private void DisplayFolderContents(string folderName)
        {
            FolderContents.Clear();
            var doc = XDocument.Load(ConfigManager.ConfigFilePath); // config.xml을 다시 불러옴
            var folderElement = doc.Descendants("Folder")
                                   .FirstOrDefault(f => f.Attribute("name").Value == folderName);

            if (folderElement != null)
            {
                foreach (var innerFolder in folderElement.Elements("Inner_Folder"))
                {
                    var isChecked = bool.Parse(innerFolder.Attribute("IsChecked")?.Value ?? "true");
                    var isUpdateCheckEnabled = bool.Parse(ConfigManager.ReadSetting("CheckForUpdatesOnStart") ?? "false");
                    var fileItem = new FileItem
                    {
                        Name = innerFolder.Attribute("name").Value,
                        IsChecked = isChecked,
                        IsFolder = true,
                        ImageSource = new BitmapImage(new Uri(isChecked ? "/Resources/Checkboxcheck.png" : "/Resources/Checkbox.png", UriKind.RelativeOrAbsolute)),
                        CurrentVersion = innerFolder.Attribute("version")?.Value ?? "버전정보없음",
                        LatestVersion = innerFolder.Attribute("LatestVersion")?.Value ?? "버전정보없음",
                        UpdateKey = innerFolder.Attribute("UpdateKey")?.Value,
                        IsUpdateCheckEnabled = isUpdateCheckEnabled
                    };

                    FolderContents.Add(fileItem);
                }
            }

            FolderContentsListView.Items.Refresh(); // FolderContents가 변경되었음을 UI에 알림
        }



        private void DisplayInnerFolderContents(string folderName)
        {
            InnerFolderContents.Clear();

            var doc = XDocument.Load(ConfigManager.ConfigFilePath); // config.xml을 다시 불러옴
            var parentFolderElement = doc.Descendants("Folder")
                                         .FirstOrDefault(f => f.Attribute("name").Value == FolderListView.SelectedItem.ToString());
            var folderElement = parentFolderElement.Elements("Inner_Folder")
                                                   .FirstOrDefault(f => f.Attribute("name").Value == folderName);

            if (folderElement != null)
            {
                foreach (var innerFolder in folderElement.Elements("Inner_Folder"))
                {
                    var isChecked = bool.Parse(innerFolder.Attribute("IsChecked")?.Value ?? "true");
                    InnerFolderContents.Add(new FileItem
                    {
                        Name = innerFolder.Attribute("name").Value,
                        Path = innerFolder.Attribute("path").Value,
                        IsChecked = isChecked,
                        IsFolder = true,
                        ImageSource = new BitmapImage(new Uri(isChecked ? "/Resources/Checkboxcheck.png" : "/Resources/Checkbox.png", UriKind.RelativeOrAbsolute)),
                        CurrentVersion = innerFolder.Attribute("version")?.Value,
                        UniqueID = innerFolder.Attribute("UniqueID")?.Value,
                        UpdateKey = innerFolder.Attribute("UpdateKey")?.Value
                    });
                }

                foreach (var file in folderElement.Elements("File"))
                {
                    string filePath = Path.Combine(folderElement.Attribute("path").Value, file.Attribute("name").Value);
                    InnerFolderContents.Add(new FileItem
                    {
                        Name = file.Attribute("name").Value,
                        Path = filePath,
                        IsChecked = false, // 파일은 기본적으로 체크되지 않은 상태로 설정
                        IsFolder = false
                    });
                }
            }

            InnerFolderContentsListView.Items.Refresh(); // InnerFolderContents가 변경되었음을 UI에 알림
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
                var parentFolderElement = ConfigManager.GetFolderElement(FolderListView.SelectedItem.ToString());
                var folderElement = parentFolderElement.Elements("Inner_Folder")
                                                       .FirstOrDefault(f => f.Attribute("name").Value == selectedFolder);

                if (folderElement != null)
                {
                    string folderPath = folderElement.Attribute("path").Value;
                    Process.Start("explorer.exe", folderPath);
                }
            }
        }
        private void InnerFolderContentsListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (InnerFolderContentsListView.SelectedItem is FileItem selectedItem)
            {
                if (selectedItem.IsFolder)
                {
                    // 폴더를 여는 경우
                    string selectedFolder = selectedItem.Name;
                    selectedItem = null;
                    if (FolderContentsListView.SelectedItem is FileItem selectedItem2)
                    {
                        string selectedFolder2 = selectedItem2.Name;
                        var parentFolderElement = ConfigManager.GetFolderElement(FolderListView.SelectedItem.ToString());
                        var folderElement = FindFolderElement(parentFolderElement, selectedFolder, selectedFolder2);

                        if (folderElement != null)
                        {
                            string folderPath = folderElement.Attribute("path").Value;
                            Process.Start("explorer.exe", folderPath);
                        }
                        else
                        {
                            MessageBox.Show("폴더를 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    // 파일을 여는 경우
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = selectedItem.Path, // 여기서 파일의 전체 경로를 사용합니다.
                            UseShellExecute = true
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"파일을 여는 중 오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        private XElement FindFolderElement(XElement parentElement, string folderName, string topfolderName)
        {
            foreach (var folderElement in parentElement.Elements("Inner_Folder"))
            {
                string currentFolderName = folderElement.Attribute("name").Value;
                if (currentFolderName == topfolderName)
                {
                    // topfolderName과 일치하는 폴더를 찾은 경우, 그 폴더 내에서 folderName을 찾음
                    return FindFolderElementRecursive(folderElement, folderName);
                }
            }
            return null;
        }
        private XElement FindFolderElementRecursive(XElement parentElement, string folderName)
        {
            foreach (var folderElement in parentElement.Elements("Inner_Folder"))
            {
                if (folderElement.Attribute("name").Value == folderName)
                {
                    return folderElement;
                }

                // 재귀적으로 하위 폴더에서 찾기
                var subFolderElement = FindFolderElementRecursive(folderElement, folderName);
                if (subFolderElement != null)
                {
                    return subFolderElement;
                }
            }
            return null;
        }
        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.DataContext is FileItem fileItem)
            {
                fileItem.IsChecked = !fileItem.IsChecked;
                fileItem.ImageSource = new BitmapImage(new Uri(fileItem.IsChecked ? "/Resources/Checkboxcheck.png" : "/Resources/Checkbox.png", UriKind.RelativeOrAbsolute));
                FolderContentsListView.Items.Refresh();
                InnerFolderContentsListView.Items.Refresh();

                // IsChecked 상태를 config.xml에 저장
                string folderName = FolderListView.SelectedItem.ToString();
                ConfigManager.UpdateIsChecked(folderName, fileItem.Name, fileItem.IsChecked);
            }
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
                // 설정 완료 후 선택된 폰트를 즉시 적용
                if (!string.IsNullOrEmpty(settingsWindow.SelectedFontResourceKey))
                {
                    ApplyFont((FontFamily)Application.Current.Resources[settingsWindow.SelectedFontResourceKey]);
                }
                // 설정 완료 후 프로그램 재시작
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
        }
        private void LoadFolders()
        {
            FolderListView.Items.Clear();
            var folders = ConfigManager.GetFolders();
            foreach (var folder in folders)
            {
                FolderListView.Items.Add(folder);
            }
        }
        //private void FolderListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (FolderListView.SelectedItem != null)
        //    {
        //        string selectedFolder = FolderListView.SelectedItem.ToString();
        //        DisplayFolderContents(selectedFolder);
        //        DisplayInnerFolderContents(selectedFolder);
        //        allSelected = true;
        //        foreach (var item in FolderContents)
        //        {
        //            item.IsChecked = allSelected;
        //            item.ImageSource = new BitmapImage(new Uri(allSelected ? "/Resources/Checkboxcheck.png" : "/Resources/Checkbox.png", UriKind.RelativeOrAbsolute));
        //        }
        //        FolderContentsListView.Items.Refresh();
        //    }
        //}
        private void FolderListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FolderListView.SelectedItem != null)
            {
                string selectedFolder = FolderListView.SelectedItem.ToString();
                DisplayFolderContents(selectedFolder);
                DisplayInnerFolderContents(selectedFolder);
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
        private void DeleteFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (FolderListView.SelectedItem == null)
            {
                MessageBox.Show("삭제할 폴더를 선택해주세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string selectedFolder = FolderListView.SelectedItem.ToString();

            // 사용자 확인 대화 상자 표시
            MessageBoxResult result = MessageBox.Show($"정말로 '{selectedFolder}' 리스트를 삭제하시겠습니까? 실제 파일은 지워지지 않습니다.", "폴더 삭제 확인", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            // 사용자가 'Yes'를 클릭한 경우에만 삭제 작업 수행
            if (result == MessageBoxResult.Yes)
            {
                var folderElement = ConfigManager.GetFolderElement(selectedFolder);

                if (folderElement != null)
                {
                    // config.xml에서 폴더 제거
                    var doc = XDocument.Load(ConfigManager.ConfigFilePath);
                    var foldersElement = doc.Root.Element("Folders");
                    if (foldersElement != null)
                    {
                        var folderToRemove = foldersElement.Elements("Folder")
                                                           .FirstOrDefault(f => f.Attribute("name").Value == selectedFolder);
                        if (folderToRemove != null)
                        {
                            folderToRemove.Remove();
                            doc.Save(ConfigManager.ConfigFilePath);
                        }
                    }

                    // ListView에서 폴더 제거
                    FolderListView.Items.Remove(selectedFolder);

                    // FolderContentsListView와 InnerFolderContentsListView 초기화
                    FolderContents.Clear();
                    InnerFolderContents.Clear();
                    FolderContentsListView.Items.Refresh();
                    InnerFolderContentsListView.Items.Refresh();
                }
                else
                {
                    MessageBox.Show("선택된 폴더를 찾을 수 없습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        /// <summary>
        /// 사용하지는 않지만 원래는 모드 실행버튼이였음. 혹시 나중에 또 용도가있을까 싶어 살려줌
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void RunWithModsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists(smapiExecutablePath))
                {
                    ShowOverlay("게임이 실행중입니다.");
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

                    ShowOverlay("게임이 실행중입니다.");
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
                    ShowOverlay("게임이 실행중입니다.");
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
            ConfigManager.WriteSetting("FontSizeIndex", FontSizeComboBox.SelectedIndex);
        }
        private async void CopyAndLinkButton_Click(object sender, RoutedEventArgs e)
        {
            await CopyAndLinkAsync();
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
        //        // 디렉토리를 삭제하기 전에 모든 파일의 읽기 전용 속성을 해제
        //        var directoryInfo = new DirectoryInfo(tempPath);
        //        foreach (var file in directoryInfo.GetFiles("*", SearchOption.AllDirectories))
        //        {
        //            file.IsReadOnly = false;
        //        }
        //        Directory.Delete(tempPath, true);
        //    }

        //    Directory.CreateDirectory(tempPath);

        //    if (FolderListView.SelectedItem == null)
        //    {
        //        MessageBox.Show("폴더를 선택해주세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        //        return;
        //    }

        //    string selectedFolder = FolderListView.SelectedItem.ToString();
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

        //        try
        //        {
        //            await Task.Run(() => DirectoryCopy(sourcePath, destinationPath, true));
        //        }
        //        catch (UnauthorizedAccessException ex)
        //        {
        //            MessageBox.Show($"접근 권한이 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        //            return;
        //        }
        //        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
        //        {
        //            MessageBox.Show("잠시 후 다시 시도해 주세요", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        //            return;
        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show($"오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        //            return;
        //        }

        //        itemIndex++;
        //        progressPopup.UpdateProgress(itemIndex, totalItems);
        //    }

        //    progressPopup.Close();

        //    if (!progressPopup.IsCancelled)
        //    {
        //        string modsPath = Path.Combine(smapiDirectory, "Mods");
        //        if (Directory.Exists(modsPath))
        //        {
        //            // 디렉토리를 삭제하기 전에 모든 파일의 읽기 전용 속성을 해제
        //            var modsDirectoryInfo = new DirectoryInfo(modsPath);
        //            foreach (var file in modsDirectoryInfo.GetFiles("*", SearchOption.AllDirectories))
        //            {
        //                file.IsReadOnly = false;
        //            }
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
        //        }
        //    }
        //}
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
                // 디렉토리를 삭제하기 전에 모든 파일의 읽기 전용 속성을 해제
                var directoryInfo = new DirectoryInfo(tempPath);
                foreach (var file in directoryInfo.GetFiles("*", SearchOption.AllDirectories))
                {
                    file.IsReadOnly = false;
                }
                Directory.Delete(tempPath, true);
            }

            Directory.CreateDirectory(tempPath);

            if (FolderListView.SelectedItem == null)
            {
                MessageBox.Show("폴더를 선택해주세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string selectedFolder = FolderListView.SelectedItem.ToString();
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

                try
                {
                    await Task.Run(() => DirectoryCopy(sourcePath, destinationPath, true));
                }
                catch (UnauthorizedAccessException ex)
                {
                    MessageBox.Show($"접근 권한이 없습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
                {
                    MessageBox.Show("잠시 후 다시 시도해 주세요", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                itemIndex++;
                progressPopup.UpdateProgress(itemIndex, totalItems);
            }

            progressPopup.Close();

            if (!progressPopup.IsCancelled)
            {
                string modsPath = Path.Combine(smapiDirectory, "Mods");
                if (ConfigManager.IsFirstRun())
                {
                    // 최초 실행 시 Mods 폴더를 ModsBackup으로 이름 변경
                    string backupFolderPath = Path.Combine(smapiDirectory, "ModsBackup");
                    if (Directory.Exists(modsPath))
                    {
                        Directory.Move(modsPath, backupFolderPath);
                    }

                    // 최초 실행 완료로 표시
                    ConfigManager.SetFirstRun();
                }
                else
                {
                    if (Directory.Exists(modsPath))
                    {
                        // 디렉토리를 삭제하기 전에 모든 파일의 읽기 전용 속성을 해제
                        var modsDirectoryInfo = new DirectoryInfo(modsPath);
                        foreach (var file in modsDirectoryInfo.GetFiles("*", SearchOption.AllDirectories))
                        {
                            file.IsReadOnly = false;
                        }
                        Directory.Delete(modsPath, true);
                    }
                }

                bool success = CreateSymbolicLink(modsPath, tempPath, SymbolicLink.Directory);
                if (!success)
                {
                    MessageBox.Show("심볼릭 링크 생성에 실패했습니다. 관리자 권한이 필요할 수 있습니다.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    RunMods();
                }
            }
        }

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

                // IsChecked 상태를 config.xml에 저장
                string folderName = FolderListView.SelectedItem.ToString();
                ConfigManager.UpdateIsChecked(folderName, item.Name, item.IsChecked);
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
        public void ApplyFont(FontFamily? fontFamily = null)
        {
            if (fontFamily == null)
            {
                string fontSetting = ConfigManager.ReadSetting("SelectedFont");
                if (!string.IsNullOrEmpty(fontSetting) && Application.Current.Resources.Contains(fontSetting))
                {
                    fontFamily = (FontFamily)Application.Current.Resources[fontSetting];
                }
            }

            if (fontFamily != null)
            {
                Resources["BaseLabelStyle"] = new Style(typeof(Label))
                {
                    BasedOn = (Style)Application.Current.Resources["BaseLabelStyle"],
                    Setters =
                    {
                        new Setter(Label.FontFamilyProperty, fontFamily)
                    }
                };

                Resources["BaseTextBlockStyle"] = new Style(typeof(TextBlock))
                {
                    BasedOn = (Style)Application.Current.Resources["BaseTextBlockStyle"],
                    Setters =
                    {
                        new Setter(TextBlock.FontFamilyProperty, fontFamily)
                    }
                };

                Resources["BaseTextBoxStyle"] = new Style(typeof(TextBox))
                {
                    BasedOn = (Style)Application.Current.Resources["BaseTextBoxStyle"],
                    Setters =
                    {
                        new Setter(TextBox.FontFamilyProperty, fontFamily)
                    }
                };

                Resources["BaseComboBoxStyle"] = new Style(typeof(ComboBox))
                {
                    BasedOn = (Style)Application.Current.Resources["BaseComboBoxStyle"],
                    Setters =
                    {
                        new Setter(ComboBox.FontFamilyProperty, fontFamily)
                    }
                };

                Resources["BaseButtonStyle"] = new Style(typeof(Button))
                {
                    BasedOn = (Style)Application.Current.Resources["BaseButtonStyle"],
                    Setters =
                    {
                        new Setter(Button.FontFamilyProperty, fontFamily)
                    }
                };

                Resources["BaseListViewStyle"] = new Style(typeof(ListView))
                {
                    BasedOn = (Style)Application.Current.Resources["BaseListViewStyle"],
                    Setters =
                    {
                        new Setter(ListView.FontFamilyProperty, fontFamily)
                    }
                };

                Resources["BaseListViewItemStyle"] = new Style(typeof(ListViewItem))
                {
                    BasedOn = (Style)Application.Current.Resources["BaseListViewItemStyle"],
                    Setters =
                    {
                        new Setter(ListViewItem.FontFamilyProperty, fontFamily)
                    }
                };
            }
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
        private void ShowOverlay(string message)
        {
            LaunchedGame.Text = message; // 전달된 메시지로 텍스트 업데이트
            OverlayGrid.Visibility = Visibility.Visible;
            MainGrid.Effect = new BlurEffect { Radius = 10 };
        }

        private void HideOverlay()
        {
            OverlayGrid.Visibility = Visibility.Collapsed;
            MainGrid.Effect = null;
        }

        public class FileItem
        {
            public string Name { get; set; }
            public string Path { get; set; }
            public bool IsChecked { get; set; }
            public bool IsFolder { get; set; }
            public BitmapImage ImageSource { get; set; }
            public string CurrentVersion { get; set; }
            public string LatestVersion { get; set; }
            public string UniqueID { get; set; }
            public string UpdateKey { get; set; } // 새로운 속성 추가
            public bool IsUpdateCheckEnabled { get; set; }
        }
        // 새로운 메소드: 모드 버전 업데이트 확인
        private async void CheckForUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            // 오버레이 표시
            ShowOverlay("업데이트 확인 중...");

            var updatesAvailable = new List<string>();
            var itemsToUpdate = FolderContents.Where(item => !string.IsNullOrEmpty(item.UpdateKey)
                                                              && !string.IsNullOrEmpty(item.CurrentVersion)
                                                              && item.UpdateKey != "-1"
                                                              && item.UpdateKey != "???").ToList();

            int totalItems = itemsToUpdate.Count;

            // 프로그래스바 팝업 창 생성
            var progressPopup = new ProgressPopup();
            progressPopup.ProgressBar.Maximum = totalItems;
            progressPopup.Show();

            int itemIndex = 0;
            foreach (var item in itemsToUpdate)
            {
                if (progressPopup.IsCancelled)
                {
                    MessageBox.Show("업데이트 확인이 취소되었습니다.", "취소됨", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;
                }

                item.LatestVersion = await api.GetLatestModVersionAsync(item.UpdateKey);
                if (item.CurrentVersion != item.LatestVersion)
                {
                    updatesAvailable.Add(item.Name);
                }

                itemIndex++;
                progressPopup.UpdateProgress(itemIndex, totalItems);
            }

            progressPopup.Close();
            HideOverlay();

            if (!progressPopup.IsCancelled && updatesAvailable.Count > 0)
            {
                var result = MessageBox.Show($"{string.Join(", ", updatesAvailable)}에 새로운 버전이 있습니다! 지금 다운로드할까요?", "Update Available", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    // 다운로드 및 업데이트 로직 구현
                    foreach (var item in FolderContents)
                    {
                        if (updatesAvailable.Contains(item.Name))
                        {
                            string url = $"https://www.nexusmods.com/stardewvalley/mods/{item.UpdateKey}";
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                            {
                                FileName = url,
                                UseShellExecute = true
                            });
                        }
                    }
                }
            }
            else if (!progressPopup.IsCancelled)
            {
                MessageBox.Show("모든 모드가 최신 버전입니다!", "No Updates", MessageBoxButton.OK);
            }
        }



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
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // 설정에서 시작 시 업데이트 확인 여부를 읽어옴
            bool startUpUpdateCheck = bool.Parse(ConfigManager.ReadSetting("StartUpUpdateCheck") ?? "false");
            
            if (startUpUpdateCheck)
            {
                await CheckAllFoldersForUpdatesAsync();
            }
            else
            {
                // 업데이트 확인을 건너뛰지만 기존의 LatestVersion 값은 유지
                var allFolders = ConfigManager.GetAllFolders();
                foreach (var folderElement in allFolders)
                {
                    var innerFolders = folderElement.Elements("Inner_Folder");
                    foreach (var innerFolder in innerFolders)
                    {
                        var fileItem = new FileItem
                        {
                            Name = innerFolder.Attribute("name")?.Value,
                            CurrentVersion = innerFolder.Attribute("version")?.Value,
                            UpdateKey = innerFolder.Attribute("UpdateKey")?.Value,
                            LatestVersion = innerFolder.Attribute("LatestVersion")?.Value
                        };

                        // FolderContents에 추가하거나 적절한 UI 갱신 로직을 구현하세요
                    }
                }
            }
        }
        private async void SelfWindow_Loaded(object sender, RoutedEventArgs e)
        {
           
                await CheckAllFoldersForUpdatesAsync();
           
        }

        //private async Task CheckAllFoldersForUpdatesAsync()
        //{
        //    var allFolders = ConfigManager.GetAllFolders();
        //    int totalItems = allFolders.Count;

        //    ShowOverlay("업데이트를 확인 중입니다..."); // 메시지를 전달하여 오버레이 표시
        //    var progressPopup = new ProgressPopup();
        //    progressPopup.UpdateProgress(0, totalItems); // 프로그래스 바 초기화
        //    progressPopup.Show();

        //    int itemIndex = 0;
        //    foreach (var folderElement in allFolders)
        //    {
        //        var folderName = folderElement.Attribute("name")?.Value;
        //        var innerFolders = folderElement.Elements("Inner_Folder");

        //        foreach (var innerFolder in innerFolders)
        //        {
        //            var fileItem = new FileItem
        //            {
        //                Name = innerFolder.Attribute("name").Value,
        //                CurrentVersion = innerFolder.Attribute("version")?.Value,
        //                UpdateKey = innerFolder.Attribute("UpdateKey")?.Value
        //            };

        //            try
        //            {
        //                // LatestVersion을 API에서 받아오기
        //                if (!string.IsNullOrEmpty(fileItem.UpdateKey) && !string.IsNullOrEmpty(fileItem.CurrentVersion) && fileItem.UpdateKey != "-1" && fileItem.UpdateKey != "???")
        //                {
        //                    await Task.Delay(500); // 여유시간 추가
        //                    string latestVersion = await api.GetLatestModVersionAsync(fileItem.UpdateKey);
        //                    fileItem.LatestVersion = latestVersion ?? "버전정보없음";

        //                    // 업데이트된 정보를 innerFolder에 저장
        //                    var latestVersionAttr = innerFolder.Attribute("LatestVersion");
        //                    if (latestVersionAttr == null)
        //                    {
        //                        innerFolder.Add(new XAttribute("LatestVersion", fileItem.LatestVersion));
        //                    }
        //                    else
        //                    {
        //                        latestVersionAttr.Value = fileItem.LatestVersion;
        //                    }

        //                    // 변경된 innerFolder를 저장
        //                    ConfigManager.SaveFolder(folderName, folderElement.Attribute("path").Value, innerFolder);
        //                }
        //                else
        //                {
        //                    fileItem.LatestVersion = "버전정보없음";
        //                }
        //            }
        //            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
        //            {
        //                fileItem.LatestVersion = "버전정보없음";
        //            }
        //            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        //            {
        //                fileItem.LatestVersion = "버전정보없음";
        //                MessageBox.Show("잠시 후 다시 시도해 주세요", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        //            }
        //            catch (Exception ex)
        //            {
        //                MessageBox.Show($"오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
        //            }
        //        }

        //        itemIndex++;
        //        progressPopup.UpdateProgress(itemIndex, totalItems);

        //        if (progressPopup.IsCancelled)
        //        {
        //            MessageBox.Show("작업이 취소되었습니다.", "취소됨", MessageBoxButton.OK, MessageBoxImage.Information);
        //            break;
        //        }
        //    }

        //    progressPopup.Close();
        //    HideOverlay(); // 작업 완료 후 오버레이 제거
        //    if (FolderListView.SelectedItem != null)
        //    {
        //        string selectedFolder = FolderListView.SelectedItem.ToString();
        //        DisplayFolderContents(selectedFolder);
        //    }
        //}
        private async Task CheckAllFoldersForUpdatesAsync()
        {
            var allFolders = ConfigManager.GetAllFolders();
            int totalItems = allFolders.Sum(f => f.Elements("Inner_Folder").Count()); // 모든 Inner_Folder 요소의 개수를 계산

            ShowOverlay("업데이트를 확인 중입니다..."); // 메시지를 전달하여 오버레이 표시
            var progressPopup = new ProgressPopup();
            progressPopup.UpdateProgress(0, totalItems); // 프로그래스 바 초기화
            progressPopup.Show();

            int itemIndex = 0;
            foreach (var folderElement in allFolders)
            {
                var folderName = folderElement.Attribute("name")?.Value;
                var innerFolders = folderElement.Elements("Inner_Folder");

                foreach (var innerFolder in innerFolders)
                {
                    var fileItem = new FileItem
                    {
                        Name = innerFolder.Attribute("name").Value,
                        CurrentVersion = innerFolder.Attribute("version")?.Value,
                        UpdateKey = innerFolder.Attribute("UpdateKey")?.Value
                    };

                    try
                    {
                        // LatestVersion을 API에서 받아오기
                        if (!string.IsNullOrEmpty(fileItem.UpdateKey) && !string.IsNullOrEmpty(fileItem.CurrentVersion) && fileItem.UpdateKey != "-1" && fileItem.UpdateKey != "???")
                        {
                            await Task.Delay(500); // 여유시간 추가
                            string latestVersion = await api.GetLatestModVersionAsync(fileItem.UpdateKey);
                            fileItem.LatestVersion = latestVersion ?? "버전정보없음";

                            // 업데이트된 정보를 innerFolder에 저장
                            var latestVersionAttr = innerFolder.Attribute("LatestVersion");
                            if (latestVersionAttr == null)
                            {
                                innerFolder.Add(new XAttribute("LatestVersion", fileItem.LatestVersion));
                            }
                            else
                            {
                                latestVersionAttr.Value = fileItem.LatestVersion;
                            }
                            ConfigManager.SaveFolder(folderName, folderElement.Attribute("path").Value, innerFolder);
                        }
                        else
                        {
                            fileItem.LatestVersion = "버전정보없음";
                        }
                    }
                    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
                    {
                        fileItem.LatestVersion = "버전정보없음";
                    }
                    catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        fileItem.LatestVersion = "버전정보없음";
                        MessageBox.Show("잠시 후 다시 시도해 주세요", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"오류가 발생했습니다: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    itemIndex++;
                    progressPopup.UpdateProgress(itemIndex, totalItems);

                    if (progressPopup.IsCancelled)
                    {
                        MessageBox.Show("작업이 취소되었습니다.", "취소됨", MessageBoxButton.OK, MessageBoxImage.Information);
                        break;
                    }
                }
            }

            progressPopup.Close();
            HideOverlay(); // 작업 완료 후 오버레이 제거
            if (FolderListView.SelectedItem != null)
            {
                string selectedFolder = FolderListView.SelectedItem.ToString();
                DisplayFolderContents(selectedFolder);
            }
        }










        #region 심볼릭 링크 플래그와 Enum
        [System.Runtime.InteropServices.DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, SymbolicLink dwFlags);

        enum SymbolicLink
        {
            File = 0,
            Directory = 1
        }
        #endregion

    }
}
