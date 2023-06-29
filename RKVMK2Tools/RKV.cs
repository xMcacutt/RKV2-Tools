using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using DamienG.Security.Cryptography;
using Microsoft.VisualBasic.ApplicationServices;
using System.Collections.Immutable;
using RKVMK2Tools;

namespace RKV2_Tools
{
    internal class RKV
    {
        private const int ENTRY_SIZE = 0x14;
        private const int ADDENDUM_SIZE = 0x10;

        public byte[]? Data;
        public List<Entry> Entries = new();
        public List<Addendum> Addenda = new();
        public List<int> EntryNameTableOffsets = new();
        public List<int> AddendumTableOffsets = new();
        public List<int> FileDataOffsets = new();
        public string? Signature;
        public int EntryCount;
        public int EntryNameTableLength;
        public int EntryNameTableOffset;
        public int AddendumCount;
        public int AddendumDataOffset;
        public int AddendumTableLegnth;
        public int AddendumTableOffset;
        public int MetadataTableOffset;
        public int MetaDataTableLength;
        public byte[]? EntryNameTable;
        public byte[]? AddendumTable;
        public byte[]? FileData;

        public bool LoadRKV(string path)
        {
            Data = File.ReadAllBytes(path);
            Signature = Encoding.UTF8.GetString(Data, 0x0, 4);
            if (Signature != "RKV2") return false;
            EntryCount = BitConverter.ToInt32(Data, 0x4);
            EntryNameTableLength = BitConverter.ToInt32(Data, 0x8);
            AddendumCount = BitConverter.ToInt32(Data, 0xC);
            AddendumTableLegnth = BitConverter.ToInt32(Data, 0x10);
            MetadataTableOffset = BitConverter.ToInt32(Data, 0x14);
            MetaDataTableLength = BitConverter.ToInt32(Data, 0x18);
            EntryNameTableOffset = MetadataTableOffset + (ENTRY_SIZE * EntryCount);
            AddendumDataOffset = EntryNameTableOffset + EntryNameTableLength;
            AddendumTableOffset = AddendumDataOffset + (ADDENDUM_SIZE * AddendumCount);

            //// Create a StringBuilder to hold the extracted strings
            //StringBuilder entryNameBuilder = new StringBuilder();

            //// Iterate over the byte array to extract the strings
            //for (int i = AddendumTableOffset; i < Data.Length; i++)
            //{
            //    if (Data[i] == 0)
            //    {
            //        // Found a null terminator, add the extracted string to the StringBuilder
            //        string entryName = Encoding.UTF8.GetString(Data, AddendumTableOffset, i - AddendumTableOffset);
            //        entryNameBuilder.AppendLine(entryName);

            //        // Move the offset to the next string
            //        AddendumTableOffset = i + 1;
            //    }
            //}

            //// Write the extracted strings to a file
            //string outputPath = "AdTableRep.txt";  // Path to the output file
            //File.WriteAllText(outputPath, entryNameBuilder.ToString());

            //Console.WriteLine("EntryNameTable extracted and written to file: " + outputPath);
            //Console.ReadLine();

            float completion;
            //ENTRIES
            for (int i = 0; i < EntryCount; i++)
            {
                Entry entry = new()
                {
                    NameTableOffset = BitConverter.ToInt32(Data, MetadataTableOffset + (ENTRY_SIZE * i)),
                    GroupingReference = BitConverter.ToInt32(Data, MetadataTableOffset + (ENTRY_SIZE * i) + 0x4),
                    Size = BitConverter.ToInt32(Data, MetadataTableOffset + (ENTRY_SIZE * i) + 0x8),
                    Offset = BitConverter.ToInt32(Data, MetadataTableOffset + (ENTRY_SIZE * i) + 0xC),
                    crc32eth = BitConverter.ToInt32(Data, MetadataTableOffset + (ENTRY_SIZE * i) + 0x10)
                };
                int endOfString = Array.IndexOf<byte>(Data, 0x0, EntryNameTableOffset + entry.NameTableOffset);
                entry.Name = Encoding.UTF8.GetString(Data, EntryNameTableOffset + entry.NameTableOffset, endOfString - (EntryNameTableOffset + entry.NameTableOffset));
                completion = (float)((float)(i + 1) / (float)EntryCount) * 100;
                Console.WriteLine($"Loading Entry Data {completion}%           ");
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Entries.Add(entry);
            }
            Console.WriteLine("");
            string outputPath = "AdDataListRep.txt";  // Path to the output file
            //using(var f = File.CreateText(outputPath))
            //{

            //ADDENDA
            for (int i = 0; i < AddendumCount; i++)
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                Addendum addendum = new()
                {
                    AddendumTableOffset = BitConverter.ToInt32(Data, AddendumDataOffset + (ADDENDUM_SIZE * i)),
                    TimeStamp = BitConverter.ToInt32(Data, AddendumDataOffset + (ADDENDUM_SIZE * i) + 0x8),
                    EntryNameTableOffset = BitConverter.ToInt32(Data, AddendumDataOffset + (ADDENDUM_SIZE * i) + 0xC)
                };
                int endOfString = Array.IndexOf<byte>(Data, 0x0, AddendumTableOffset + addendum.AddendumTableOffset);
                addendum.Path = Utility.ReadString(Data, AddendumTableOffset + addendum.AddendumTableOffset);
                addendum.Entry = Entries.FirstOrDefault(E => E.NameTableOffset == addendum.EntryNameTableOffset);

                //f.WriteLine(addendum.Path);

                completion = (float)((float)(i + 1) / (float)AddendumCount) * 100;
                Console.WriteLine($"Loading Addendum Data {completion}%        ");
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Addenda.Add(addendum);
            }
           // }
            Console.WriteLine("");
            return true;
        }

        public void ExtractFull()
        {
            Console.WriteLine("Select output path...");
            FolderBrowserDialog folderBrowserDialog = new();
            DialogResult result = folderBrowserDialog.ShowDialog();
            if (result != DialogResult.OK)
            {
                Console.WriteLine("Folder Not selected, cancelling extraction");
                return;
            }
            string output_dir = folderBrowserDialog.SelectedPath;

            float completion;
            int i = 0;
            foreach(Addendum addendum in Addenda)
            {
                string file_dir = addendum.Path[..addendum.Path.LastIndexOf('\\')];
                Directory.CreateDirectory(Path.Combine(output_dir, file_dir));
                if (addendum.Entry == null) continue;
                //|| addendum.Entry.Name == null
                string filepath = Path.Combine(output_dir, file_dir, addendum.Entry.Name);
                Entries.FirstOrDefault(e => e == addendum.Entry).Extracted = true;
                using (FileStream fileStream = new FileStream(filepath, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    fileStream.Write(Data, addendum.Entry.Offset, addendum.Entry.Size);
                    fileStream.Close();
                }
                completion = (float)((float)(i + 1) / (float)EntryCount) * 100;
                Console.WriteLine($"Extracting {completion}%           ");
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                i++;
            }
            foreach(Entry entry in Entries)
            {
                if (!entry.Extracted)
                {
                    using (FileStream fileStream = new(Path.Combine(output_dir, entry.Name), FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        fileStream.Write(Data, entry.Offset, entry.Size);
                        fileStream.Close();
                    }
                    completion = (float)((float)(i + 1) / (float)EntryCount) * 100;
                    Console.WriteLine($"Extracting {completion}%           ");
                    Console.SetCursorPosition(0, Console.CursorTop - 1);
                    i++;
                }
            }
            Console.WriteLine("Extraction Complete!");
            return;
        }

        public void ExtractSingle()
        {
            Console.WriteLine("Which file would you like to extract? Type \"list\" for a list of files in this RKV");
            string input = "";
            while(input != "cancel")
            {
                input = Console.ReadLine();
                if (input == "cancel") return;
                if (input == "list")
                {
                    foreach (Entry entry in Entries) Console.WriteLine(entry.Name);
                }
                else
                {
                    Entry entry = Entries.FirstOrDefault(e => e.Name == input);
                    if (entry == null)
                    {
                        Console.WriteLine("That file does not exist in this rkv.");
                        continue;
                    }
                    Console.WriteLine("Select output path...");
                    FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
                    DialogResult result = folderBrowserDialog.ShowDialog();
                    if (result != DialogResult.OK)
                    {
                        Console.WriteLine("Folder Not selected, cancelling extraction");
                        return;
                    }
                    string output_dir = folderBrowserDialog.SelectedPath;
                    Console.WriteLine("Extracting: " + entry.Name);

                    string filePath = Path.Combine(output_dir, entry.Name);
                    using (FileStream fileStream = new(filePath, FileMode.OpenOrCreate, FileAccess.Write))
                    {
                        fileStream.Write(Data, entry.Offset, entry.Size);
                        fileStream.Close();
                    }
                    Console.WriteLine("Extraction Complete!");
                    Console.WriteLine("Extract Another File? (Y/N)");
                    if (Console.ReadLine() == "Y") continue;
                    return;
                }
            }
        }

        public void GenerateEntryNameTable(string[] files)
        {
            float completion;
            // Calculate the total length of the name table
            EntryNameTableLength = files.Sum(f => 1 + Path.GetFileName(f).Length) + 1;
            // Create a memory stream to hold the byte array
            using MemoryStream stream = new(EntryNameTableLength);
            foreach (string file in files)
            {
                // Get the file name as a byte array in UTF-8 encoding
                byte[] nameBytes = Encoding.ASCII.GetBytes(Path.GetFileName(file));

                // Write the 0x0 byte separator
                stream.WriteByte(0x0);

                // Write the file name to the stream
                EntryNameTableOffsets.Add((int)stream.Position);
                stream.Write(nameBytes, 0, nameBytes.Length);

                completion = (float)((float)(Array.IndexOf(files, file) + 1) / (float)files.Length) * 100;
                Console.WriteLine($"Generating Entry Table {completion}%        ");
                Console.SetCursorPosition(0, Console.CursorTop - 1);
            }
            Console.WriteLine("");
            stream.WriteByte(0x0);

            // Get the byte array from the stream
            EntryNameTable = stream.ToArray();
        }

        public void GenerateAddendumTable(string[] files, string inputDir)
        {
            List<string> aliases = new();
            foreach(string file in files)
            {
                string relativePath = Path.GetRelativePath(inputDir, file);
                if (relativePath != Path.GetFileName(file))
                {
                    if(FileExtAlias.Aliases.TryGetValue(Path.GetExtension(file).ToLower(), out string aliasExt))
                    {
                        if (aliasExt == ".m3d" && FileExtAlias.MdlExceptions.Contains(Path.GetFileNameWithoutExtension(file))) aliasExt = ".FBX";
                        relativePath = relativePath.Replace(Path.GetExtension(file), aliasExt);
                    }
                    aliases.Add(relativePath);
                }
            }
            AddendumTableLegnth = aliases.Sum(p => 1 + p.Length) + 1;

            using MemoryStream stream = new(AddendumTableLegnth);
            string[] aliasesArray = aliases.ToArray();
            foreach (string path in aliases)
            {
                float completion;
                // Get the file name as a byte array in UTF-8 encoding
                byte[] pathBytes = Encoding.ASCII.GetBytes(path);

                // Write the 0x0 byte separator
                stream.WriteByte(0x0);

                // Write the file name to the stream
                AddendumTableOffsets.Add((int)stream.Position);
                stream.Write(pathBytes, 0, pathBytes.Length);

                completion = (float)((float)(Array.IndexOf(aliasesArray, path) + 1) / (float)aliasesArray.Length) * 100;
                Console.WriteLine($"Generating Addendum Table {completion}%        ");
                Console.SetCursorPosition(0, Console.CursorTop - 1);
            }
            Console.WriteLine("");
            stream.WriteByte(0x0);

            // Get the byte array from the stream
            AddendumTable = stream.ToArray();
        }

        public void GenerateFileData(string[] files)
        {
            float completion;
            MemoryStream combinedStream = new();
            foreach (string file in files)
            {
                using FileStream fileStream = new(file, FileMode.Open, FileAccess.Read);
                FileDataOffsets.Add((int)combinedStream.Position);
                fileStream.CopyTo(combinedStream);

                int bytesToPad = 16 - (int)combinedStream.Position % 16;
                combinedStream.Write(new byte[bytesToPad]);

                completion = (float)((float)(Array.IndexOf(files, file) + 1) / (float)files.Length) * 100;
                Console.WriteLine($"Generating File Data {completion}%        ");
                Console.SetCursorPosition(0, Console.CursorTop - 1);
            }
            Console.WriteLine("");
            FileData = combinedStream.ToArray();
        }

        public void Repack(string inputDir, string outputDir)
        {
            Console.WriteLine("Repacking RKV...");
            float completion;
            Signature = "RKV2";
            string[] files = Directory.GetFiles(inputDir, "*", SearchOption.AllDirectories);
            EntryCount = files.Length;
            GenerateEntryNameTable(files);
            GenerateAddendumTable(files, inputDir);
            GenerateFileData(files);
            List<string> entryStrings = new();
            DateTimeOffset now = DateTime.Now;
            int timestamp = (int)now.ToUnixTimeSeconds();
            List<string> addenda = new();
            foreach (string file in files)
            {
                var crc32 = new Crc32();
                int fileCrc32;
                string entryName = Path.GetFileName(file);
                int stringIndex = Array.IndexOf(files, file);
                FileInfo fileInfo = new FileInfo(file);
                using (var fs = File.Open(file, FileMode.Open)) fileCrc32 = BitConverter.ToInt32(crc32.ComputeHash(fs).Reverse().ToArray());
                Entry entry = new()
                {
                    Name = entryName,
                    Size = (int)fileInfo.Length,
                    crc32eth = fileCrc32,
                    NameTableOffset = EntryNameTableOffsets[stringIndex],
                    Offset = FileDataOffsets[stringIndex] + 0x80
                };
                if (entryStrings.Contains(Path.GetFileNameWithoutExtension(entryName)))
                {
                    entry.GroupingReference = Entries.FindLast(x => Path.GetFileNameWithoutExtension(x.Name) == Path.GetFileNameWithoutExtension(entryName)).NameTableOffset;
                }
                Entries.Add(entry);
                entryStrings.Add(Path.GetFileNameWithoutExtension(entryName));

                string relativePath = Path.GetRelativePath(inputDir, file);
                if (relativePath != Path.GetFileName(file))
                {
                    if(FileExtAlias.Aliases.TryGetValue(Path.GetExtension(file).ToLower(), out string aliasExt))
                    {
                        if (aliasExt == ".m3d" && FileExtAlias.MdlExceptions.Contains(Path.GetFileNameWithoutExtension(file))) aliasExt = ".FBX";
                        relativePath = relativePath.Replace(Path.GetExtension(file), aliasExt);
                    }
                    addenda.Add(file);
                    Addendum addendum = new()
                    {
                        Path = relativePath,
                        AddendumTableOffset = AddendumTableOffsets[addenda.Count - 1],
                        Entry = entry,
                        EntryNameTableOffset = entry.NameTableOffset,
                        TimeStamp = timestamp
                    };
                    Addenda.Add(addendum);
                }
                AddendumCount = addenda.Count;

                completion = (float)(((float)(Array.IndexOf(files, file) + 1) / (float)files.Length) * 100);
                Console.WriteLine($"Generating File Metadata {completion}%        ");
                Console.SetCursorPosition(0, Console.CursorTop - 1);
            }
            Console.WriteLine("");
            MetadataTableOffset = 0x80 + FileData.Length;
            MetaDataTableLength = (ENTRY_SIZE * EntryCount) + (ADDENDUM_SIZE * AddendumCount) + EntryNameTable.Length + AddendumTable.Length;
            MetaDataTableLength += 16 - (int)MetaDataTableLength % 16;
            EntryNameTableOffset = MetadataTableOffset + MetaDataTableLength;
            AddendumTableOffset = EntryNameTableOffset + EntryNameTableLength;

            Console.WriteLine("Writing RKV...");
            byte[] zeroInt = new byte[] { 0, 0, 0, 0 };
            using var rkv = File.Create(outputDir + "\\" + Path.GetFileName(inputDir) + ".rkv");
            rkv.Write(Encoding.UTF8.GetBytes(Signature));
            rkv.Write(BitConverter.GetBytes(EntryCount));
            rkv.Write(BitConverter.GetBytes(EntryNameTableLength));
            rkv.Write(BitConverter.GetBytes(AddendumCount));
            rkv.Write(BitConverter.GetBytes(AddendumTableLegnth));
            rkv.Write(BitConverter.GetBytes(MetadataTableOffset));
            rkv.Write(BitConverter.GetBytes(MetaDataTableLength));
            rkv.Write(new byte[] { 0x0F, 0x07, 0x0F, 0x07, 0x30, 0x03 });
            rkv.Write(Enumerable.Repeat((byte)0x0, 0x5E).ToArray());
            rkv.Write(FileData);
            int entryIndex = 0;
            Entries.Sort((a1, a2) => string.Compare(a1.Name, a2.Name, StringComparison.OrdinalIgnoreCase));
            foreach(Entry entry in Entries)
            {
                rkv.Write(BitConverter.GetBytes(entry.NameTableOffset));
                rkv.Write(BitConverter.GetBytes(entry.GroupingReference));
                rkv.Write(BitConverter.GetBytes(entry.Size));
                rkv.Write(BitConverter.GetBytes(entry.Offset));
                rkv.Write(BitConverter.GetBytes(entry.crc32eth));
                entryIndex++;
            }
            rkv.Write(EntryNameTable);
            int addendumIndex = 0;
            Addenda.Sort((a1, a2) => string.Compare(a1.Path, a2.Path, StringComparison.OrdinalIgnoreCase));
            foreach (Addendum addendum in Addenda)
            {
                rkv.Write(BitConverter.GetBytes(addendum.AddendumTableOffset));
                rkv.Write(zeroInt);
                rkv.Write(BitConverter.GetBytes(addendum.TimeStamp));
                rkv.Write(BitConverter.GetBytes(addendum.EntryNameTableOffset));
                addendumIndex++;
            }
            rkv.Write(AddendumTable);
            int bytesToPad = 16 - (int)rkv.Position % 16;
            rkv.Write(new byte[bytesToPad]);
            Console.WriteLine("Repack Complete! Press Enter to return to the main interface.");
            Console.ReadLine();
        }
    }
}
