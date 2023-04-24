using System;
using System.Windows.Forms;

namespace RKV2_Tools
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("RKV2 Tools by xMcacutt. Type \"help\" to get started");
            string input;
            while (true)
            {
                input = Console.ReadLine();
                switch (input.Split(' ')[0])
                {
                    case "help":
                        PrintHelp();
                        break;
                    case "extract":
                        Extract();
                        break;
                    case "repack":
                        Repack();
                        break;
                }
                Console.Clear();
                Console.WriteLine("RKV2 Tools by xMcacutt. Type \"help\" to get started");
            }
        }

        public static void PrintHelp()
        {
            Console.Clear();
            Console.WriteLine("EXTRACTION");
            Console.WriteLine("\"extract\" - extract either the entirety of an RKV or select a file to extract\n" +
                "All files will be output to the selected output directory with the filesystem intact.");
            Console.WriteLine(" ");
            Console.WriteLine("REPACKING");
            Console.WriteLine("\"repack\" - repack an entire directory into an RKV\n" +
                               "All files (even in nested directories) will be repacked into the RKV.\n" +
                               "The RKV will be output to the output directory and will inherit the name of the input directory.\n");
            Console.WriteLine("Press Enter to return to the main interface.");
            Console.ReadLine();
        }

        public static void Extract()
        {
            Console.WriteLine("Select RKV...");
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "RKV Files (*.rkv)|*.rkv|All Files (*.*)|*.*";
            openFileDialog.Title = "Select RKV";
            DialogResult result = openFileDialog.ShowDialog();
            if (result != DialogResult.OK)
            {
                Console.WriteLine("RKV Not selected, cancelling extraction");
                return;
            }

            RKV rkv = new RKV();
            if (!rkv.LoadRKV(openFileDialog.FileName))
            {
                Console.WriteLine($"Signature does not match: {rkv.Signature} != RKV2");
                return;
            }
            Console.WriteLine("RKV Loaded!\nExtract Entire RKV? (Y/N) Type \"cancel\" to cancel.");
            string input = Console.ReadLine();
            while (input != "cancel")
            {
                switch (input)
                {
                    case "Y":
                        rkv.ExtractFull();
                        return;
                    case "N":
                        rkv.ExtractSingle();
                        return;
                }
                Console.ReadLine();
            }
        }

        public static void Repack()
        {
            Console.WriteLine("Select Input Directory...");
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            DialogResult inputResult = folderBrowserDialog.ShowDialog();
            if (inputResult != DialogResult.OK)
            {
                Console.WriteLine("Folder Not selected, cancelling extraction");
                return;
            }
            string input = folderBrowserDialog.SelectedPath;
            Console.WriteLine("Select Output Directory...");
            DialogResult outputResult = folderBrowserDialog.ShowDialog();
            if (outputResult != DialogResult.OK)
            {
                Console.WriteLine("Folder Not selected, cancelling extraction");
                return;
            }
            string output = folderBrowserDialog.SelectedPath;
            RKV rkv = new RKV();
            rkv.Repack(input, output);
        }
    }
}