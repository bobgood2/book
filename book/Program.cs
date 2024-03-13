using LuToolbox.Common;
using LuToolbox;
using System;
using book.Tools;

namespace book
{
    internal class Program
    {
        static public string home;
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Book <path> <- to write a book within the directory <path> or to continue a stopped job");
                Console.WriteLine("  first create a file in that directory called 'prompt.txt'");
                Console.WriteLine("    the first line is the budget i.e.:\n100000 tokens");
                Console.WriteLine("    then a description of what you want written");
                Console.WriteLine();
                Console.WriteLine("Book <path> restart  <- to restart job from scratch");

                return;
            }

            PromptBuilder.Init();

            home = args[0];
            Directory.SetCurrentDirectory(home);
            if (args.Length > 1 && args[1] == "restart")
            {
                foreach (var dir in Directory.GetDirectories(home))

                {
                    Directory.Delete(dir, true);
                }        
            }

            Run.Scan(true);

            if (Run.Runs.IsEmpty)
            {
                string prompt = File.ReadAllText("prompt.txt");
                int pos = prompt.IndexOf('\n');
                string line1 = prompt[0..pos].Trim();
                int pos2 = line1.IndexOf(" ");
                int budget = int.Parse(line1[0..pos2].Trim());

                _ = new Outline("1", "Book", prompt[(pos+1)..], string.Empty, budget);

            }

            while (!Run.IsDone())
            {
                Thread.Sleep(1000);
            }

            Console.WriteLine("All Work Complete");
            
        }
    }
}