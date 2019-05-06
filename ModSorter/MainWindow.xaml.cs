using System;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
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
        private List<CheckBox> backedUpView;

        public MainWindow()
        {
            InitializeComponent();
            try
            {
                modConfig = XmlFileReaderUtility.GetModsConfig();
                PopulateMainModList();
                SetVersion();
                OpenFileSelectDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Issue starting ModSorter.");
                Application.Current.Shutdown();
            }
        }

        private void SetVersion()
        {
            version.Content = "RW config version: " + XmlFileReaderUtility.GetModsConfigVersion();
        }

        private void OpenFileSelectDialog()
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

            PurgeModsThatWentMissing();

            backedUpView = new List<CheckBox>(mainModList.Items.Count);
            foreach (CheckBox checkBox in mainModList.Items.Cast<CheckBox>())
            {
                backedUpView.Add(checkBox);
            }
        }

        private void PurgeModsThatWentMissing()
        {
            List<object> errors = new List<object>();
            for (int i = mainModList.Items.Count - 1; i >= 0; i--)
            {
                var item = mainModList.Items[i];
                if (item is CheckBox)
                    continue;
                errors.Add(item);
                mainModList.Items.RemoveAt(i);
                activeMods.Remove((string)item);
            }
            if (errors.Any())
            {
                if (errors.Count == 1)
                {
                    MessageBox.Show($"{errors.First().ToString()} was found active in ModsConfig, but mod folder could not be found. Renamed or removed from Steam?",
                        "Mod not found");
                    return;
                }
                MessageBox.Show($"The following mods were found active in ModsConfig, but their folders could not be found. Renamed or removed from Steam?" +
                    $"{Environment.NewLine + Environment.NewLine} {errors.Aggregate(Environment.NewLine, (x, y) => x + y.ToString() + Environment.NewLine)}",
                    "Mods not found");
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

                if (mainModList.Items.Contains(mod.folder))
                {
                    int pos = mainModList.Items.IndexOf(mod.folder);
                    mainModList.Items.Insert(pos, toggleMod);
                    mainModList.Items.Remove(mod.folder);
                    return;
                }
            }
            mainModList.Items.Add(toggleMod);
        }

        private CheckBox CreateCheckBox(Mod mod)
        {
            CheckBox toggleMod = new CheckBox
            {
                Content = mod,
                IsChecked = mod.active,
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
            mainModList.Items.Insert(Math.Min(activeMods.Count, mainModList.Items.Count), freshBox);
            mainModList.Items.Remove(checkbox);

            if (toToggle.active)
            {
                activeMods.Add(toToggle.folder);
            }
            else
            {
                activeMods.Remove(toToggle.folder);
            }

            if (!string.IsNullOrWhiteSpace(SearchField.Text) && SearchField.Text != "Search...")
                ResortModList(SearchField.Text);
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

            if (pos <= activeMods.Count)
            {
                box.IsChecked = true;
                mod.active = true;

                if (activeMods.Contains(mod.folder))
                {
                    activeMods.Remove(mod.folder);
                    activeMods.Insert(pos - 1, mod.folder);
                }
                else
                {
                    activeMods.Add(mod.folder);
                }
            }

            CheckBox freshBox = DeCoupleModFromOldCheckBox(item);

            mainModList.Items.Insert(mainModList.SelectedIndex - 1, freshBox);
            mainModList.Items.Remove(item);
            mainModList.SelectedItem = mainModList.Items.GetItemAt(pos - 1);

            if (!string.IsNullOrWhiteSpace(SearchField.Text) && SearchField.Text != "Search...")
                ResortModList(SearchField.Text);
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
            mainModList.Items.Remove(item);
            mainModList.SelectedItem = mainModList.Items.GetItemAt(pos + 1);

            if (!string.IsNullOrWhiteSpace(SearchField.Text) && SearchField.Text != "Search...")
                ResortModList(SearchField.Text);
        }

        private CheckBox DeCoupleModFromOldCheckBox(object item)
        {
            CheckBox box = (CheckBox)item;
            Mod mod = (Mod)box.Content;
            box.Content = null;

            CheckBox freshBox = CreateCheckBox(mod);

            if (backedUpView.Contains(box))
            {
                int oldPos = backedUpView.IndexOf(box);
                backedUpView.Remove(box);
                backedUpView.Insert(oldPos, freshBox);

                return freshBox;
            }

            CheckBox oldCheckBox = backedUpView.First(x => Equals((Mod)x.Content, mod));

            int pos = backedUpView.IndexOf(oldCheckBox);
            backedUpView.Remove(oldCheckBox);
            backedUpView.Insert(pos, freshBox);

            return freshBox;
        }

        private void Reset(object sender, RoutedEventArgs e)
        {
            allMods = new List<Mod>(backedUpAllMods);
            activeMods = new List<string>(backedUpActiveMods);
            mainModList.Items.Clear();
            foreach (CheckBox box in backedUpView)
            {
                Mod mod = (Mod)box.Content;
                box.IsChecked = activeMods.Contains(mod.folder);
                mainModList.Items.Add(box);
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
            MessageBox.Show(
                "If you need help, ask a friend." + Environment.NewLine
                + Environment.NewLine
                + "If you want professional support, buy me a coffee first." + Environment.NewLine
                + Environment.NewLine
                + "Known issues:" + Environment.NewLine
                + "- None currently. Found any? Submit a PR!" + Environment.NewLine

                , caption: "There is no help here");
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox thing = (TextBox)sender;
            if (thing.Text == "Search...")
                return;

            ResortModList(thing.Text);
        }

        private void TextBox_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SearchField.Text = "";
            ResortModList(SearchField.Text);
        }

        private void ResortModList(string filter)
        {
            filter = filter.ToUpper();
            mainModList.Items.Clear();
            foreach (var activeMod in activeMods)
            {
                Mod toAdd = allMods.Find(x => x.active && x.folder == activeMod);
                if (toAdd == null)
                {
                    MessageBox.Show($"{activeMod} not found");
                    continue;
                }
                if (toAdd.name.ToUpper().Contains(filter))
                {
                    var box = CreateCheckBox(toAdd);
                    mainModList.Items.Add(box);
                }
            }
            foreach (var inactiveMod in allMods)
            {
                if (!inactiveMod.active && inactiveMod.name.ToUpper().Contains(filter))
                {
                    var box = CreateCheckBox(inactiveMod);
                    mainModList.Items.Add(box);
                }
            }
        }

        private void LoadModsFromSave(object sender, RoutedEventArgs e)
        {
            var dir = Directory.Exists(XmlFileReaderUtility.directory) ? XmlFileReaderUtility.directory : string.Empty;

            Ookii.Dialogs.Wpf.VistaOpenFileDialog dialog = new Ookii.Dialogs.Wpf.VistaOpenFileDialog
            {
                Filter = "yoursavefile.rws (*.rws)|*.rws",
                InitialDirectory = dir
            };
            if (dialog.ShowDialog() == true)
            {
                var list = XmlFileReaderUtility.ReadModsFromSaveFile(dialog.FileName);

                if (list.Any())
                    LoadModsFromList(list);
            }
        }

        private void LoadModsFromList(IEnumerable<string> modList)
        {
            activeMods.Clear();
            activeMods.AddRange(modList);
            ResortModList(string.Empty);
        }

        private void ResetToCore(object sender, RoutedEventArgs e)
        {
            activeMods.Clear();
            activeMods.Add("Core");
            ResortModList(string.Empty);
        }
    }
}
