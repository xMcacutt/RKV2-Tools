using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RKV2_Tools
{
    public static class Utility
    {
        public static string ReadString(byte[] bytes, int position)
        {
            int endOfString = Array.IndexOf<byte>(bytes, 0x0, position);
            if (endOfString == position) return string.Empty;
            string s = Encoding.ASCII.GetString(bytes, position, endOfString - position);
            return s.Replace(" ", @"___");
        }
    }
}