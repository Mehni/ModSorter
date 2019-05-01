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
            TryLoadFolder(textBox.Text);
            SetVersion();
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
                    foreach (var item in XmlFileReaderUtility.GetModNamesFromFiles(workShopFolder))
                    {
                        allMods.Add(item);
                        if (activeMods.Contains(item.folderName))
                        {
                            var pos = mainModList.Items.IndexOf(item.folderName);
                            mainModList.Items.Insert(pos, item.name);
                            mainModList.Items.Remove(item.folderName);
                        }
                        else
                            mainModList.Items.Add(item.name);
                    }
                }
            }
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

            mainModList.Items.Insert(mainModList.SelectedIndex - 1, item);
            mainModList.Items.RemoveAt(pos + 1);
            mainModList.SelectedItem = mainModList.Items.GetItemAt(pos - 1);
        }

        private void MoveDownClicked(object sender, RoutedEventArgs e)
        {
            object item = mainModList.SelectedItem;
            if (item == null)
                return;

            int pos = mainModList.Items.IndexOf(item);
            if (pos == mainModList.Items.Count - 1)
                return;

            mainModList.Items.Insert(mainModList.SelectedIndex + 2, item);
            mainModList.Items.RemoveAt(pos);
            mainModList.SelectedItem = mainModList.Items.GetItemAt(pos + 1);
        }
    }
}
