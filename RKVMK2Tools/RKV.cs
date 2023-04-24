using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using DamienG.Security.Cryptography;

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

            float completion;
            //ENTRIES
            for(int i = 0; i < EntryCount; i++)
            {
                Entry entry = new()
                {
                    NameTableOffset = BitConverter.ToInt32(Data, MetadataTableOffset + (ENTRY_SIZE * i)),
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
            //ADDENDA
            for(int i = 0; i < AddendumCount; i++)
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                Addendum addendum = new()
                {
                    AddendumTableOffset = BitConverter.ToInt32(Data, AddendumDataOffset + (ADDENDUM_SIZE * i)),
                    TimeStamp = BitConverter.ToInt32(Data, AddendumDataOffset + (ADDENDUM_SIZE * i) + 0x8),
                    EntryNameTableOffset = BitConverter.ToInt32(Data, AddendumDataOffset + (ADDENDUM_SIZE * i) + 0xC)
                };
                int endOfString = Array.IndexOf<byte>(Data, 0x0, AddendumTableOffset + addendum.AddendumTableOffset);
                addendum.Path = Encoding.UTF8.GetString(Data, AddendumTableOffset + addendum.AddendumTableOffset, endOfString - (AddendumTableOffset + addendum.AddendumTableOffset));
                addendum.Entry = Entries.FirstOrDefault(E => E.NameTableOffset == addendum.EntryNameTableOffset);
                completion = (float)((float)(i + 1) / (float)AddendumCount) * 100;
                Console.WriteLine($"Loading Addendum Data {completion}%        ");
                Console.SetCursorPosition(0, Console.CursorTop - 1);
                Addenda.Add(addendum);
            }
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
                string file_dir = addendum.Path;
                int index = file_dir.LastIndexOf('\\');
                file_dir = file_dir[..index];
                Directory.CreateDirectory(Path.Combine(output_dir, file_dir));
                if (addendum.Entry == null || addendum.Entry.Name == null) continue;
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

        public void GenerateEntryTable(string[] files)
        {
            // Calculate the total length of the name table
            EntryNameTableLength = files.Sum(f => 1 + Path.GetFileName(f).Length) + 1;
            // Create a memory stream to hold the byte array
            using MemoryStream stream = new(EntryNameTableLength);
            foreach (string file in files)
            {
                // Get the file name as a byte array in UTF-8 encoding
                byte[] nameBytes = Encoding.UTF8.GetBytes(Path.GetFileName(file));

                // Write the 0x0 byte separator
                stream.WriteByte(0x0);

                // Write the file name to the stream
                EntryNameTableOffsets.Add((int)stream.Position);
                stream.Write(nameBytes, 0, nameBytes.Length);
            }
            stream.WriteByte(0x0);

            // Get the byte array from the stream
            EntryNameTable = stream.ToArray();
        }

        public void GenerateAddendumTable(string[] files, string inputDir)
        {
            List<string> relativePaths = new();
            foreach(string file in files)
            {
                string relativePath = Path.GetRelativePath(inputDir, file);
                if (relativePath != Path.GetFileName(file))
                {
                    relativePaths.Add(relativePath);
                }
            }
            AddendumTableLegnth = relativePaths.Sum(p => 1 + p.Length) + 1;

            using MemoryStream stream = new(AddendumTableLegnth);
            foreach (string path in relativePaths)
            {
                // Get the file name as a byte array in UTF-8 encoding
                byte[] pathBytes = Encoding.UTF8.GetBytes(Path.GetFileName(path));

                // Write the 0x0 byte separator
                stream.WriteByte(0x0);

                // Write the file name to the stream
                AddendumTableOffsets.Add((int)stream.Position);
                stream.Write(pathBytes, 0, pathBytes.Length);
            }
            stream.WriteByte(0x0);

            // Get the byte array from the stream
            AddendumTable = stream.ToArray();
        }

        public void GenerateFileData(string[] files)
        {
            MemoryStream combinedStream = new();
            foreach (string file in files)
            {
                using FileStream fileStream = new(file, FileMode.Open, FileAccess.Read);
                FileDataOffsets.Add((int)combinedStream.Position);
                fileStream.CopyTo(combinedStream);
            }
            FileData = combinedStream.ToArray();
        }

        public void Repack(string inputDir, string outputDir)
        {
            Signature = "RKV2";
            string[] files = Directory.GetFiles(inputDir, "*", SearchOption.AllDirectories);
            EntryCount = files.Length;
            GenerateEntryTable(files);
            GenerateAddendumTable(files, inputDir);
            GenerateFileData(files);
            AddendumCount = EntryCount - Directory.GetFiles(inputDir).Length;
            foreach (string file in Directory.GetFiles(inputDir, "*", SearchOption.AllDirectories))
            {
                var crc32 = new Crc32();
                int fileCrc32;
                string entryName = Path.GetFileName(file);
                int stringIndex = Array.IndexOf(files, entryName);
                using (var fs = File.Open(file, FileMode.Open)) fileCrc32 = BitConverter.ToInt32(crc32.ComputeHash(fs));
                Entry entry = new()
                {
                    Name = entryName,
                    crc32eth = fileCrc32,
                    NameTableOffset = EntryNameTableOffsets[stringIndex],
                    Offset = FileDataOffsets[stringIndex] + 0x80
                };
                Entries.Add(entry);

                string relativePath = Path.GetRelativePath(inputDir, file);
                List<string> addenda = new();
                if (relativePath != Path.GetFileName(file))
                {
                    addenda.Add(file);
                    int addendIndex = Array.IndexOf(addenda.ToArray(), file);
                    Addendum addendum = new()
                    {
                        Path = relativePath,
                        AddendumTableOffset = AddendumTableOffsets[addendIndex],
                        Entry = entry,
                        EntryNameTableOffset = entry.NameTableOffset,
                        TimeStamp = 0
                    };
                }
            }
            MetadataTableOffset = 0x80 + FileData.Length;
            MetaDataTableLength = ENTRY_SIZE * EntryCount;
            EntryNameTableOffset = MetadataTableOffset + MetaDataTableLength;
            AddendumTableOffset = EntryNameTableOffset + EntryNameTableLength;
            using var rkv = File.Create(Path.Combine(outputDir, Path.GetDirectoryName(inputDir) + ".RKV"));
            rkv.Write(Encoding.UTF8.GetBytes(Signature), 0x0, 1);
            rkv.Write(BitConverter.GetBytes(EntryCount), 0x4, 1);
            rkv.Write(BitConverter.GetBytes(EntryNameTableLength), 0x8, 1);
            rkv.Write(BitConverter.GetBytes(AddendumCount), 0xC, 1);
            rkv.Write(BitConverter.GetBytes(AddendumTableLegnth), 0x10, 1);
            rkv.Write(BitConverter.GetBytes(MetadataTableOffset), 0x14, 1);
            rkv.Write(BitConverter.GetBytes(MetaDataTableLength), 0x18, 1);
            rkv.Write(new byte[] { 0x0F, 0x07, 0x0F, 0x07, 0x30, 0x03 }, 0x1C, 1);
            rkv.Write(Enumerable.Repeat((byte)0x0, 0x5E).ToArray(), 0x22, 1);
            rkv.Write(FileData, 0x80, 1);
            int entryIndex = 0;
            foreach(Entry entry in Entries)
            {
                rkv.Write(BitConverter.GetBytes(entry.NameTableOffset), MetadataTableOffset + (ENTRY_SIZE * entryIndex), 1);
                rkv.Write(BitConverter.GetBytes(entry.Size), MetadataTableOffset + (ENTRY_SIZE * entryIndex) + 0x8, 1);
                rkv.Write(BitConverter.GetBytes(entry.Offset), MetadataTableOffset + (ENTRY_SIZE * entryIndex) + 0xC, 1);
                rkv.Write(BitConverter.GetBytes(entry.crc32eth), MetadataTableOffset + (ENTRY_SIZE * entryIndex) + 0x10, 1);
                entryIndex++;
            }
            rkv.Write(EntryNameTable, EntryNameTableOffset, 1);
            int addendumIndex = 0;
            foreach(Addendum addendum in Addenda)
            {
                rkv.Write(BitConverter.GetBytes(addendum.AddendumTableOffset), AddendumDataOffset + (ADDENDUM_SIZE * addendumIndex), 1);
                rkv.Write(BitConverter.GetBytes(addendum.TimeStamp), AddendumDataOffset + (ADDENDUM_SIZE * addendumIndex) + 0x8, 1);
                rkv.Write(BitConverter.GetBytes(addendum.EntryNameTableOffset), AddendumDataOffset + (ADDENDUM_SIZE * addendumIndex) + 0xC, 1);
                addendumIndex++;
            }
            rkv.Write(AddendumTable, AddendumTableOffset, 1);
        }
    }
}
