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
        private List<Mod> activeMods;
        private List<Mod> allMods;

        public MainWindow()
        {
            InitializeComponent();
            modConfig = XmlFileReaderUtility.GetModsConfig();
            TryLoadFolder(textBox.Text);
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
                listBox.Items.Add(item);
            }
            if (path.ToUpper().Contains("STEAM"))
            {
                string workShopFolder = XmlFileReaderUtility.GetWorkShopFolder(path);

                if (Directory.Exists(workShopFolder))
                {
                    foreach (var item in XmlFileReaderUtility.GetModNamesFromFiles(workShopFolder))
                    {
                        listBox.Items.Add(item.name);
                    }
                }
                else
                {
                    MessageBox.Show("Thought there was a Steam workshop folder here, but can't find it.");
                }
            }
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            listBox.Items.Clear();
            foreach (string item in XmlFileReaderUtility.ReadModsFromModsConfig())
            {
                listBox.Items.Add(item);
            }
            if (listBox.Items.Count == 0)
                listBox.Items.Add("No mods found");
        }

        private void MoveUpClicked(object sender, RoutedEventArgs e)
        {
            object item = listBox.SelectedItem;
            if (item == null)
                return;

            int pos = listBox.Items.IndexOf(item);
            if (pos == 0)
                return;

            listBox.Items.Insert(listBox.SelectedIndex - 1, item);
            listBox.Items.RemoveAt(pos + 1);
            listBox.SelectedItem = listBox.Items.GetItemAt(pos - 1);
        }

        private void MoveDownClicked(object sender, RoutedEventArgs e)
        {
            object item = listBox.SelectedItem;
            if (item == null)
                return;

            int pos = listBox.Items.IndexOf(item);
            if (pos == listBox.Items.Count - 1)
                return;

            listBox.Items.Insert(listBox.SelectedIndex + 2, item);
            listBox.Items.RemoveAt(pos);
            listBox.SelectedItem = listBox.Items.GetItemAt(pos + 1);
        }
    }
}
