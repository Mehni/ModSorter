﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Xml.Linq;
using System.Diagnostics;

namespace ModSorter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly XElement modConfig;
        private List<string> activeMods = new List<string>();
        private List<Mod> allMods = new List<Mod>();

        private List<string> backedUpActiveMods;
        private List<Mod> backedUpAllMods;
        private List<object> backedUpView;

        public MainWindow()
        {
            InitializeComponent();
            modConfig = XmlFileReaderUtility.GetModsConfig();
            PopulateMainModList();
            SetVersion();
            OpenFileSelectDialog();
        }

        private void SetVersion()
        {
            version.Content = "RW config version: " + XmlFileReaderUtility.GetModsConfigVersion();
        }

        private void OpenFileSelectDialog()
        {
            try
            {
                if (!Directory.Exists(textBox.Content.ToString()))
                    textBox.Content = string.Empty;

                Ookii.Dialogs.Wpf.VistaOpenFileDialog dialog = new Ookii.Dialogs.Wpf.VistaOpenFileDialog
                {
                    Filter = "RimWorld.exe (*.exe)|*.exe",
                    InitialDirectory = textBox.Content.ToString()
                };
                if (dialog.ShowDialog() == true)
                {
                    textBox.Content = dialog.FileName;
                    TryLoadFolder(Path.GetDirectoryName(dialog.FileName));
                }
                else
                {
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void TryLoadFolder(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            if (!Directory.Exists(path))
                return;

            foreach (Mod item in XmlFileReaderUtility.GetModNamesFromFiles(path + Path.DirectorySeparatorChar + "Mods"))
            {
                AddModToLists(item);
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
                    foreach (Mod mod in XmlFileReaderUtility.GetModNamesFromFiles(workShopFolder))
                    {
                        AddModToLists(mod);
                    }
                }
            }
            backedUpActiveMods = new List<string>(activeMods);
            backedUpAllMods = new List<Mod>(allMods);
            backedUpView = new List<object>(mainModList.Items.Count);
            foreach (object checkBox in mainModList.Items)
            {
                backedUpView.Add(checkBox);
            }
        }

        private void AddModToLists(Mod mod)
        {
            allMods.Add(mod);
            CheckBox toggleMod = CreateCheckBox(mod);

            if (activeMods.Contains(mod.folder))
            {
                mod.active = true;
                toggleMod.IsChecked = true;

                int pos = mainModList.Items.IndexOf(mod.folder);
                mainModList.Items.Insert(pos, toggleMod);
                mainModList.Items.Remove(mod.folder);
            }
            else
                mainModList.Items.Add(toggleMod);
        }

        private CheckBox CreateCheckBox(Mod mod)
        {
            CheckBox toggleMod = new CheckBox
            {
                Content = mod
            };
            toggleMod.Click += ToggleMod_Click;
            toggleMod.Background = mod.IsCompatible() ? default : System.Windows.Media.Brushes.Gainsboro;
            return toggleMod;
        }

        private void ToggleMod_Click(object sender, RoutedEventArgs e)
        {
            CheckBox checkbox = (CheckBox)sender;
            Mod toToggle = (Mod)checkbox.Content;
            toToggle.active = checkbox.IsChecked ?? false;

            CheckBox freshBox = DeCoupleModFromOldCheckBox(sender);
            mainModList.Items.Insert(activeMods.Count, freshBox);
            mainModList.Items.Remove(checkbox);

            if (toToggle.active)
            {
                activeMods.Add(toToggle.folder);
            }
            else
            {
                activeMods.Remove(toToggle.folder);
            }
        }

        private void PopulateMainModList()
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

            CheckBox box = (CheckBox)item;
            Mod mod = (Mod)box.Content;

            if (pos <= activeMods.Count && !activeMods.Contains(mod.folder))
            {
                box.IsChecked = true;
                mod.active = true;
                activeMods.Add(mod.folder);
            }

            CheckBox freshBox = DeCoupleModFromOldCheckBox(item);

            mainModList.Items.Insert(mainModList.SelectedIndex - 1, freshBox);
            mainModList.Items.RemoveAt(pos + 1);
            mainModList.SelectedItem = mainModList.Items.GetItemAt(pos - 1);
        }

        private void MoveDownClicked(object sender, RoutedEventArgs e)
        {
            object item = mainModList.SelectedItem;
            if (item == null)
                return;

            int pos = mainModList.Items.IndexOf(item);

            if (pos > activeMods.Count || pos >= mainModList.Items.Count)
                return;

            if (pos >= activeMods.Count - 1)
            {
                CheckBox box = (CheckBox)item;
                Mod mod = (Mod)box.Content;
                box.IsChecked = false;
                mod.active = false;
                activeMods.Remove(mod.folder);
                return;
            }

            CheckBox freshBox = DeCoupleModFromOldCheckBox(item);

            mainModList.Items.Insert(mainModList.SelectedIndex + 2, freshBox);
            mainModList.Items.RemoveAt(pos);
            mainModList.SelectedItem = mainModList.Items.GetItemAt(pos + 1);
        }

        private CheckBox DeCoupleModFromOldCheckBox(object item)
        {
            CheckBox box = (CheckBox)item;
            Mod mod = (Mod)box.Content;
            box.Content = null;

            CheckBox freshBox = CreateCheckBox(mod);
            freshBox.IsChecked = box.IsChecked;

            int pos = backedUpView.IndexOf(item);
            backedUpView.Remove(item);
            backedUpView.Insert(pos, freshBox);

            return freshBox;
        }

        private void Reset(object sender, RoutedEventArgs e)
        {
            allMods = new List<Mod>(backedUpAllMods);
            activeMods = new List<string>(backedUpActiveMods);
            mainModList.Items.Clear();
            foreach (object item in backedUpView)
            {
                CheckBox box = (CheckBox)item;
                Mod mod = (Mod)box.Content;
                box.IsChecked = activeMods.Contains(mod.folder);
                mainModList.Items.Add(item);
            }
        }

        private void Save(object sender, RoutedEventArgs e)
        {
            try
            {
                XmlFileReaderUtility.WriteModsToConfig(activeMods, modConfig);
                MessageBox.Show("File saved succesfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void BuyMeACoffee(object sender, RoutedEventArgs e)
        {
            Process.Start("https://ko-fi.com/mehnicreates");
        }

        private void HelpClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("If you need help, ask a friend." + Environment.NewLine + Environment.NewLine + "If you want professional support, buy me a coffee first.", "There is no help here");
        }
    }
}
