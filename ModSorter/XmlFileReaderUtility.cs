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
        public static string GetWorkShopFolder(string installDirectory)
        {
            //if install directory is D:\SteamLibrary\steamapps\common\RimWorld
            //then workshop folder is D:\SteamLibrary\steamapps\workshop\content\294100
            var common = Directory.GetParent(installDirectory);
            var steamApps = common.Parent;
            var workshop = steamApps.FullName + Path.DirectorySeparatorChar + "workshop";
            var content = workshop + Path.DirectorySeparatorChar + "content";
            var mods = content + Path.DirectorySeparatorChar + "294100";
            return mods;
        }

        public static XElement GetModsConfig()
        {
            string filename = "ModsConfig.xml";
            string directory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low" + Path.DirectorySeparatorChar + GetFilePath();
            string file = Path.Combine(directory, filename);
            return XElement.Load(file);
        }

        public static Version GetGameVersion()
        {
            if (Mod.TryParseVersionString(GetModsConfig().Element("version").Value, out Version ver))
                return ver;

            return new Version("unknown");
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
            foreach (var item in Directory.GetDirectories(folder))
            {
                string folderName = item.Split(Path.DirectorySeparatorChar).Last();
                string About = item + Path.DirectorySeparatorChar + "About";
                if (!Directory.Exists(About))
                    continue;

                var aboutxml = XElement.Load(About + Path.DirectorySeparatorChar + "About.xml");

                IEnumerable<XElement> version = aboutxml?.Element("supportedVersions")?.Descendants()
                              ?? new List<XElement> { aboutxml?.Element("targetVersion") };
                yield return new Mod(aboutxml.Element("name").Value, version, folderName);
            }
        }
    }
}
