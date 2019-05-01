﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace ModSorter
{
    public class Mod
    {
        public readonly List<Version> supportedVersions;
        public readonly string name;
        public readonly string folderName;

        public Mod(string name, string versionString, string folder)
        {
            this.name = name;
            this.folderName = folder;
            if (TryParseVersionString(versionString, out var version))
            {
                this.supportedVersions = new List<Version> { version };
            }
        }

        public Mod(string name, IEnumerable<XElement> versions, string folder)
        {
            this.name = name;
            this.folderName = folder;
            this.supportedVersions = new List<Version>();

            foreach (var item in versions.Select(x => x?.Value))
            {
                if (TryParseVersionString(item, out var version))
                {
                    this.supportedVersions.Add(version);
                }
            }
        }

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
    }
}
