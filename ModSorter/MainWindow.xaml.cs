using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Xml.Linq;

namespace ModSorter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private XElement modConfig;
        private List<string> activeMods = new List<string>();
        private List<Mod> allMods = new List<Mod>();

        public MainWindow()
        {
            InitializeComponent();
            modConfig = XmlFileReaderUtility.GetModsConfig();
            TryLoadFolder(@"D:\SteamLibrary\steamapps\common\RimWorld");
            SetVersion();
        }

        private void MainModList_DragLeave(object sender, DragEventArgs e)
        {
            MessageBox.Show(sender.ToString(), e.ToString());
        }

        private void MainModList_DragEnter(object sender, DragEventArgs e)
        {
            MessageBox.Show(sender.ToString(), e.ToString());
        }

        private void SetVersion()
        {
            version.Content = "RW version: " + XmlFileReaderUtility.GetGameVersion().ToString();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(textBox.Text))
                    textBox.Text = string.Empty;

                Ookii.Dialogs.Wpf.VistaOpenFileDialog dialog = new Ookii.Dialogs.Wpf.VistaOpenFileDialog
                {
                    Filter = "RimWorld.exe (*.exe)|*.exe",
                    InitialDirectory = textBox.Text
                };
                if ((bool)dialog.ShowDialog())
                {
                    textBox.Text = dialog.FileName;
                    TextBox_TextChanged(this, null);
                    TryLoadFolder(Path.GetDirectoryName(dialog.FileName));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void TryLoadFolder(string path)
        {
            if (!Directory.Exists(path))
                return;

            foreach (var item in Directory.GetDirectories(path + Path.DirectorySeparatorChar + "Mods"))
            {
                mainModList.Items.Add(item);
            }

            if (path.ToUpper().Contains("STEAM"))
            {
                string workShopFolder = XmlFileReaderUtility.GetWorkShopFolder(path);

                if (!Directory.Exists(workShopFolder))
                {
                    MessageBox.Show("Thought there was a Steam workshop folder here, but can't find it.");
                }
                else
                {
                    foreach (var mod in XmlFileReaderUtility.GetModNamesFromFiles(workShopFolder))
                    {
                        allMods.Add(mod);
                        CheckBox toggleMod = CreateCheckBox(mod);

                        if (activeMods.Contains(mod.folderName))
                        {
                            mod.active = true;
                            toggleMod.IsChecked = true;

                            var pos = mainModList.Items.IndexOf(mod.folderName);
                            mainModList.Items.Insert(pos, toggleMod);
                            mainModList.Items.Remove(mod.folderName);
                        }
                        else
                            mainModList.Items.Add(toggleMod);
                    }
                }
            }
        }

        private CheckBox CreateCheckBox(Mod mod)
        {
            CheckBox toggleMod = new CheckBox
            {
                Content = mod
            };
            toggleMod.Click += ToggleMod_Click;
            return toggleMod;
        }

        private void ToggleMod_Click(object sender, RoutedEventArgs e)
        {
            var checkbox = (CheckBox)sender;
            ToggleModActive(checkbox.Content, checkbox.IsChecked);
        }

        private void ToggleModActive(object mod, bool? active)
        {
            Mod toToggle = (Mod)mod;
            toToggle.active = active ?? false;
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            mainModList.Items.Clear();
            foreach (string item in XmlFileReaderUtility.ReadModsFromModsConfig())
            {
                activeMods.Add(item);
                mainModList.Items.Add(item);
            }
            if (mainModList.Items.Count == 0)
                mainModList.Items.Add("No mods found");
        }

        private void MoveUpClicked(object sender, RoutedEventArgs e)
        {
            object item = mainModList.SelectedItem;
            if (item == null)
                return;

            int pos = mainModList.Items.IndexOf(item);
            if (pos == 0)
                return;

            CheckBox freshBox = DeCoupleModFromOldCheckBox(item);

            mainModList.Items.Insert(mainModList.SelectedIndex - 1, freshBox);
            mainModList.Items.RemoveAt(pos + 1);
            mainModList.SelectedItem = mainModList.Items.GetItemAt(pos - 1);
        }

        private CheckBox DeCoupleModFromOldCheckBox(object item)
        {
            CheckBox box = (CheckBox)item;
            Mod mod = (Mod)box.Content;
            box.Content = null;

            CheckBox freshBox = CreateCheckBox(mod);
            return freshBox;
        }

        private void MoveDownClicked(object sender, RoutedEventArgs e)
        {
            object item = mainModList.SelectedItem;
            if (item == null)
                return;

            int pos = mainModList.Items.IndexOf(item);
            if (pos == mainModList.Items.Count - 1)
                return;

            CheckBox freshBox = DeCoupleModFromOldCheckBox(item);

            mainModList.Items.Insert(mainModList.SelectedIndex + 2, freshBox);
            mainModList.Items.RemoveAt(pos);
            mainModList.SelectedItem = mainModList.Items.GetItemAt(pos + 1);
        }
    }
}
