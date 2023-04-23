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