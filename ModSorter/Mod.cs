using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModSorter
{
    public class Mod
    {
        public readonly string version;
        public readonly string name;

        public Mod(string name, string version)
        {
            this.version = version;
            this.name = name;
        }
    }
}
