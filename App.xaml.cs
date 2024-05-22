using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.IO;

#pragma warning disable CS8604

namespace StardewValley_Mod_Manager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            base.OnStartup(e);

            if (ConfigManager.IsFirstRun())
            {
                // 최초 실행 시 필요한 작업 수행
                MessageBox.Show("프로그램을 처음 실행했습니다. 초기 설정을 진행합니다. 이 창은 모드로 게임을 실행하기 전까지 계속 실행됩니다.", "환영합니다", MessageBoxButton.OK, MessageBoxImage.Information);

                // 최초 실행이 완료되었음을 표시하는 파일 생성
                //ConfigManager.SetFirstRun();
            }
            //string modsFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "StardewValley", "Mods");
            //ConfigManager.CheckAndBackupModsFolder(modsFolderPath);

            string fontSetting = ConfigManager.ReadSetting("SelectedFont");
            if (!string.IsNullOrEmpty(fontSetting))
            {
                FontFamily fontFamily = (FontFamily)Application.Current.Resources[fontSetting];
                if (fontFamily != null)
                {
                    ApplyFont(fontFamily);
                }
            }
            //MainWindow mainWindow = new MainWindow();
            //mainWindow.Show();
        }
        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogException(e.Exception);
            MessageBox.Show("Unhandled exception occurred. Check the log for details.");
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogException(e.ExceptionObject as Exception);
        }

        private void LogException(Exception ex)
        {
            if (ex != null)
            {
                string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "error.log");
                File.AppendAllText(logPath, $"{DateTime.Now}: {ex.ToString()}{Environment.NewLine}");
            }
        }


        public static void ApplyFont(FontFamily fontFamily)
        {
            Application.Current.Resources["BaseLabelStyle"] = new Style(typeof(Label))
            {
                BasedOn = (Style)Application.Current.Resources["BaseLabelStyle"],
                Setters =
                {
                    new Setter(Label.FontFamilyProperty, fontFamily)
                }
            };

            Application.Current.Resources["BaseTextBlockStyle"] = new Style(typeof(TextBlock))
            {
                BasedOn = (Style)Application.Current.Resources["BaseTextBlockStyle"],
                Setters =
                {
                    new Setter(TextBlock.FontFamilyProperty, fontFamily)
                }
            };

            Application.Current.Resources["BaseTextBoxStyle"] = new Style(typeof(TextBox))
            {
                BasedOn = (Style)Application.Current.Resources["BaseTextBoxStyle"],
                Setters =
                {
                    new Setter(TextBox.FontFamilyProperty, fontFamily)
                }
            };

            Application.Current.Resources["BaseComboBoxStyle"] = new Style(typeof(ComboBox))
            {
                BasedOn = (Style)Application.Current.Resources["BaseComboBoxStyle"],
                Setters =
                {
                    new Setter(ComboBox.FontFamilyProperty, fontFamily)
                }
            };

            Application.Current.Resources["BaseButtonStyle"] = new Style(typeof(Button))
            {
                BasedOn = (Style)Application.Current.Resources["BaseButtonStyle"],
                Setters =
                {
                    new Setter(Button.FontFamilyProperty, fontFamily)
                }
            };

            Application.Current.Resources["BaseListViewStyle"] = new Style(typeof(ListView))
            {
                BasedOn = (Style)Application.Current.Resources["BaseListViewStyle"],
                Setters =
                {
                    new Setter(ListView.FontFamilyProperty, fontFamily)
                }
            };

            Application.Current.Resources["BaseListViewItemStyle"] = new Style(typeof(ListViewItem))
            {
                BasedOn = (Style)Application.Current.Resources["BaseListViewItemStyle"],
                Setters =
                {
                    new Setter(ListViewItem.FontFamilyProperty, fontFamily)
                }
            };
            Application.Current.Resources["BaseCheckBoxStyle"] = new Style(typeof(CheckBox))
            {
                BasedOn = (Style)Application.Current.Resources["BaseCheckBoxStyle"],
                Setters =
        {
            new Setter(CheckBox.FontFamilyProperty, fontFamily)
        }
            };
        }
    }
}