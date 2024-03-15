using LuToolbox.Common;
using LuToolbox;
using System;
using book.Tools;
using System.Text;

namespace book
{
    internal class Program
    {
        static public string home;
        static async Task Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Book <path> <- to write a book within the directory <path> or to continue a stopped job");
                Console.WriteLine("  first create a file in that directory called 'prompt.txt'");
                Console.WriteLine("    the first line is the budget i.e.:\n100000 tokens");
                Console.WriteLine("    then a description of what you want written");
                Console.WriteLine();
                Console.WriteLine("Book <path> restart  <- to restart job from scratch");
                Console.WriteLine("Book <path> 3.A.2.D.2.C.2  <- to do one run");

                return;
            }

            PromptBuilder.Init();

            home = args[0];
            string only = null;
            string after = null;
            Directory.SetCurrentDirectory(home);

            if (args.Length > 1 && args[1] == "restart")
            {
                foreach (var dir in Directory.GetDirectories(home))

                {
                    Directory.Delete(dir, true);
                }

                Run.Scan(true);
            }
            else if (args.Length > 2 && args[1] == "trim")
            {
                only = args[2];
                Run.Scan(false);
                Trim(args[2], false);
            }
            else if (args.Length > 2 && args[1] == "after")
            {
                after = args[2];
                Run.Scan(false);
                Trim(args[2], true);
            }
            else if (args.Length > 1)
            {
                only = args[1];
                Run.Scan(false);
            }
            else
            {
                Run.Scan(true);
            }

            if (only != null)
            {
                Run r = Run.Get(only);
                await r.Execute();

            }

            if (after != null)
            {
                Run r = Run.Get(after);

                r.tool.OnCompletion(r);
            }

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
            var elapsed = endTime - startTime;

            using (TextWriter tw = new StreamWriter("elapsed.txt"))
            {
                tw.WriteLine($"elapsed time = {elapsed.TotalMinutes} minutes");
            }

            Console.WriteLine($"elapsed time = {elapsed.TotalMinutes} minutes");
            Console.WriteLine("All Work Complete");

        }

        static void Trim(string id, bool after)
        {
            Run run = Run.Get(id);
            if (run == null)
            {
                throw new Exception("no " + id);
            }

            if (!after)
            {
                File.Delete(Path.Combine(run.Id, "output.txt"));
                run.output = null;
            }

            HashSet<Run> list = new HashSet<Run>();
            Trim(run, list);
            foreach (var run2 in list)
            {
                if (run2 != run)
                {
                    Directory.Delete(run2.Id, true);
                }
            }
        }

        static void Trim(Run run, HashSet<Run> list)
        {
            if (list.Add(run))
            {
                foreach (var child in Run.Runs.Values)
                {
                    if (child.info.Parent == run.Id)
                    {
                        Trim(child, list);
                    }
                }
            }
        }
    }
}