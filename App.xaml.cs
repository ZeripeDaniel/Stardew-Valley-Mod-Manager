using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace StardewValley_Mod_Manager
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string fontSetting = ConfigManager.ReadSetting("SelectedFont");
            if (!string.IsNullOrEmpty(fontSetting))
            {
                FontFamily fontFamily = (FontFamily)Application.Current.Resources[fontSetting];
                if (fontFamily != null)
                {
                    ApplyFont(fontFamily);
                }
            }

            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
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
        }
    }
}