using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace RKVMK2Tools
{
    internal class FileExtAlias
    {
        public static Dictionary<string, string> Aliases = new(StringComparer.OrdinalIgnoreCase)
        {
            { ".tex", ".tga" },
            { ".fontinfo", ".FontInfo" },
            { ".bbi", ".bad" },
            { ".anm", ".a3d" },
            { ".fgr", ".fgr" },
            { ".bni", "" },
            { ".dds", ".dds" },
            { ".tbl", ".tbl" },
            { ".pkg", ".pkg" },
            { ".mcd", ".mcd" },
            { ".wmh", ".wmh" },
            { ".csv", ".csv" },
            { ".txt", ".txt" },
            { ".mdl", ".m3d" },
            { ".qsm", ".m3d" }
        };

        public static List<string> MdlExceptions = new()
        {
            "BackerRoo",
            "P7000_CraftyChunks",
            "P0645_PoincianaFeature",
            "P0824_OpalBag",
            "R_Camerang",
            "R_CraftyRang"
        };
    }
}
