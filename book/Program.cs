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

                _ = new Start("1", "Review User Prompt", prompt, string.Empty);

            }

            DateTime startTime = DateTime.Now;
            while (!Run.IsDone())
            {
                Thread.Sleep(1000);
            }

            DateTime endTime = DateTime.Now;
            var elapsed = endTime-startTime;

            using (TextWriter tw = new StreamWriter("elapsed.txt"))
            {
                tw.WriteLine($"elapsed time = {elapsed.TotalMinutes} minutes");
            }

            Console.WriteLine($"elapsed time = {elapsed.TotalMinutes} minutes");
            Console.WriteLine("All Work Complete");
            
        }
    }
}