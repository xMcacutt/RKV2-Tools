using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RKVMK2Tools
{
    internal class FileNameComparer : IComparer<string>
    {
        public int Compare(string str1, string str2)
        {
            string x = Path.GetFileName(str1);
            string y = Path.GetFileName(str2);

            int minLength = Math.Min(x.Length, y.Length);

            for (int i = 0; i < minLength; i++)
            {
                char charX = x[i];
                char charY = y[i];

                if (charX == charY)
                    continue;

                // Check if the characters are letters and ignore case
                if (char.IsLetter(charX) && char.IsLetter(charY))
                {
                    charX = char.ToUpperInvariant(charX);
                    charY = char.ToUpperInvariant(charY);
                }

                return charX.CompareTo(charY);
            }

            // If the common characters match, shorter string comes first
            return x.Length.CompareTo(y.Length);
        }
    }
}
