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
        public MainWindow()
        {
            InitializeComponent();

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

            var parentFolderElement = ConfigManager.GetFolderElement(FolderListView.SelectedItem.ToString());
            var folderElement = parentFolderElement.Elements("Inner_Folder")
                                                   .FirstOrDefault(f => f.Attribute("name").Value == folderName);

            if (folderElement != null)
            {
                foreach (var innerFolder in folderElement.Elements("Inner_Folder"))
                {
                    InnerFolderContents.Add(new FileItem
                    {
                        Name = innerFolder.Attribute("name").Value,
                        Path = innerFolder.Attribute("path").Value, // 파일 경로 설정
                        IsChecked = false,
                        IsFolder = true
                    });
                }

                foreach (var file in folderElement.Elements("File"))
                {
                    string filePath = Path.Combine(folderElement.Attribute("path").Value, file.Attribute("name").Value);
                    InnerFolderContents.Add(new FileItem
                    {
                        Name = file.Attribute("name").Value,
                        Path = filePath, // 파일 경로 설정
                        IsChecked = false,
                        IsFolder = false
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
                    var parentFolderElement = ConfigManager.GetFolderElement(FolderListView.SelectedItem.ToString());
                    var folderElement = parentFolderElement.Elements("Inner_Folder")
                                                           .FirstOrDefault(f => f.Attribute("name").Value == selectedFolder);

                    if (folderElement != null)
                    {
                        string folderPath = folderElement.Attribute("path").Value;
                        Process.Start("explorer.exe", folderPath);
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
            public string Path { get; set; }
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
        private void FolderListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FolderListView.SelectedItem != null)
            {
                string selectedFolder = FolderListView.SelectedItem.ToString();
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
            ConfigManager.WriteSetting("FontSizeIndex", FontSizeComboBox.SelectedIndex);
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
