using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RKV2_Tools
{
    public class Entry
    {
        public string? Name;
        public int NameTableOffset;
        public int Size;
        public int Offset;
        public int crc32eth;
        public bool Extracted;
    }
}
