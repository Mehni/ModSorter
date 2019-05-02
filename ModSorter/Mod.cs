using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ModSorter
{
    public class Mod
    {
        public readonly List<Version> supportedVersions;
        public readonly string name;
        public readonly string folder;
        public bool active;

        public Mod(string name, IEnumerable<XElement> versions, string folder)
        {
            this.name = name;
            this.folder = folder.Split(Path.DirectorySeparatorChar).Last();

            supportedVersions = new List<Version>();

            foreach (string item in versions.Select(x => x?.Value))
            {
                if (TryParseVersionString(item, out Version version))
                {
                    supportedVersions.Add(version);
                }
            }

            if (name == "Core" && this.folder == "Core")
            {
                supportedVersions.Add(ExtractVersionFromCore(folder));
            }
        }

        private Version ExtractVersionFromCore(string folder)
        {
            //if install directory is D:\SteamLibrary\steamapps\common\RimWorld\Mods\Core
            //then version file is in D:\SteamLibrary\steamapps\common\RimWorld\Version.txt
            DirectoryInfo Mods = Directory.GetParent(folder);
            DirectoryInfo RimWorld = Mods.Parent;
            string text = "0, 1";
            if (File.Exists(RimWorld.FullName + Path.DirectorySeparatorChar + "Version.txt"))
            {
                text = File.ReadAllText(RimWorld.FullName + Path.DirectorySeparatorChar + "Version.txt");
            }

            TryParseVersionString(text, out Version version);
            return version;
        }

        public bool IsCompatible()
            => supportedVersions.Any(x => x == XmlFileReaderUtility.GetModsConfigVersion());

        public override string ToString()
            => $"[{MaxSupportedVersion.ToString()}] {name}";

        private Version MaxSupportedVersion
            => supportedVersions.Any(x => x == XmlFileReaderUtility.GetModsConfigVersion())
                ? supportedVersions.FirstOrDefault(x => x == XmlFileReaderUtility.GetModsConfigVersion())
                : supportedVersions.OrderByDescending(x => x?.Major)?.ThenBy(x => x?.Minor).FirstOrDefault() ?? new Version(0,0);

        public static bool TryParseVersionString(string str, out Version version)
        {
            version = null;
            if (str == null)
            {
                return false;
            }
            string[] array = str.Split('.');
            if (array.Length < 2)
            {
                return false;
            }
            for (int i = 0; i < 2; i++)
            {
                if (!int.TryParse(array[i], out int result))
                {
                    return false;
                }
                if (result < 0)
                {
                    return false;
                }
            }
            version = new Version(int.Parse(array[0]), int.Parse(array[1]));
            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj is Mod == false)
                return false;
            return ((Mod)obj).name == name && folder == ((Mod)obj).folder;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return name.GetHashCode() ^ folder.GetHashCode();
            }
        }
    }
}
