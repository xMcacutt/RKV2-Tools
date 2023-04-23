using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RKV2_Tools
{
    public class Addendum
    {
        public string Path;
        public int AddendumTableOffset;
        public int EntryNameTableOffset;
        public Entry Entry;
        public int TimeStamp;
    }
}
