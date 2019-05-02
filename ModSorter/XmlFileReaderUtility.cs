using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ModSorter
{
    internal class XmlFileReaderUtility
    {
        private const string filename = "ModsConfig.xml";
        private static readonly string directory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low" + Path.DirectorySeparatorChar + GetFilePath();

        public static string GetWorkShopFolder(string installDirectory)
        {
            //if install directory is D:\SteamLibrary\steamapps\common\RimWorld
            //then workshop folder is D:\SteamLibrary\steamapps\workshop\content\294100
            DirectoryInfo common = Directory.GetParent(installDirectory);
            DirectoryInfo steamApps = common.Parent;
            string workshop = steamApps.FullName + Path.DirectorySeparatorChar + "workshop";
            string content = workshop + Path.DirectorySeparatorChar + "content";
            string mods = content + Path.DirectorySeparatorChar + "294100";
            return mods;
        }

        public static void WriteModsToConfig(IEnumerable<string> mods, XElement modsConfig)
        {
            modsConfig.Element("activeMods").RemoveAll();

            foreach (string item in mods)
            {
                modsConfig.Element("activeMods").Add(new XElement("li", item));
            }
            string file = Path.Combine(directory, filename);
            modsConfig.Save(file);
        }

        public static XElement GetModsConfig()
        {
            string file = Path.Combine(directory, filename);
            return XElement.Load(file);
        }

        public static Version GetModsConfigVersion()
        {
            if (Mod.TryParseVersionString(GetModsConfig().Element("version").Value, out Version ver))
                return ver;

            return new Version(0, 0);
        }

        public static IEnumerable<string> ReadModsFromModsConfig()
        {
            try
            {
                return GetModsConfig().Element("activeMods").Descendants().Select(x => x.Value);
            }
            catch (Exception ex)
            {
                return new List<string> { ex.ToString() };
            }
        }

        private static string GetFilePath()
            => $"Ludeon Studios{Path.DirectorySeparatorChar}RimWorld by Ludeon Studios{Path.DirectorySeparatorChar}Config";

        public static IEnumerable<Mod> GetModNamesFromFiles(string folder)
        {
            foreach (string subFolder in Directory.GetDirectories(folder))
            {
                string About = subFolder + Path.DirectorySeparatorChar + "About";
                if (!Directory.Exists(About))
                    continue;

                XElement aboutxml = XElement.Load(About + Path.DirectorySeparatorChar + "About.xml");

                IEnumerable<XElement> version = aboutxml?.Element("supportedVersions")?.Descendants()
                              ?? new List<XElement> { aboutxml?.Element("targetVersion") };
                yield return new Mod(aboutxml.Element("name").Value, version, subFolder);
            }
        }
    }
}
