using book.Tools;
using Microsoft.IdentityModel.Abstractions;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Diagnostics;

namespace book
{
    internal class Program
    {
        static public string home;
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Viewer <path> <htmlpath> <- to read a book within the directory <path> and create html doc at <htmlpath>");

                return;
            }


            home = args[0];
            var dst = home+"\\book.html"; // @"C:\Users\bobgood\OneDrive - Microsoft\Shared with Everyone\mybook\book.html";
            if (args.Length>1)
            {
                dst = args[1];
            }


            string html = File.ReadAllText("HTML/HtmlPage1.html");
            var parts = html.Split("CONTENT");

            Directory.SetCurrentDirectory(home);
            Run.Scan(false);

            var top = parts[0];
            Run r = Run.Get("1");
            top = top.Replace("TITLE", r.info.Title);
            top = top.Replace("AUTHOR", "Authored By "+r.info.Author);
            top = top.Replace("DATA", GetData());

            var bottom = parts[1];

            BuildHeirarchy();
            using (TextWriter tw = new StreamWriter(dst))
            {
                tw.WriteLine(top);
                Print("book", tw);
                tw.WriteLine(bottom);
            }

            if (args.Length == 1)
            {
                Process.Start(new ProcessStartInfo(dst) { UseShellExecute = true });
            }
        }

        class LLMStat
        {
            public long inputTokens;
            public long outputTokens;
            public double costPerInput;
            public double costPerOutput;

            public double Cost()
            {
                return costPerOutput * outputTokens + costPerInput * inputTokens;
            }
        }

        static string GetData()
        {
            Dictionary<string, LLMStat> models = new Dictionary<string, LLMStat>
            {
                { "dev-gpt4-32k", new LLMStat
                    {
                    costPerInput= .06 / 1000,
                    costPerOutput= .12 / 1000,
                    }
                },
                { "dev-gpt-4-turbo", new LLMStat
                    {
                    costPerInput= .03 / 1000,
                    costPerOutput= .06 / 1000,
                    }
                },
            };
            long inputTokens = 0;
            long outputTokens = 0;
            long proseTokens = 0;
            long latency = 0;
            foreach (var run in Run.Runs.Values)
            {
                if (run.info.Tool == typeof(Prose).Name)
                {
                    proseTokens += run.info.OutputTokens;
                }

                if (models.TryGetValue(run.info.Model, out LLMStat lLMStat))
                {
                    lLMStat.outputTokens += run.info.OutputTokens;
                    lLMStat.inputTokens += run.info.InputTokens;
                }
                else throw new Exception();

                outputTokens += run.info.OutputTokens;
                inputTokens += run.info.InputTokens;
                latency += run.info.Latency;
            }
            double cost = 0;
            foreach (var v in models.Values)
            {
                cost += v.Cost();
            }   
                  
            return $"tokens = {proseTokens:#,##0} ({proseTokens / 600} pages)<br/>"
            + $"tokens used: input {inputTokens / 1e6:0.00}M output  {outputTokens / 1e6:0.00}M<br/>"
            + $"cost: {cost:$#,##0.00} per OpenAI pricing"
            + $"Total LLM Time {(double)latency / 3600 / 1000:0.0} hours";
        }

        static void Print(string root, TextWriter tw)
        {
            List<string> children = null;
            heirarchy.TryGetValue(root, out children);
            Run r = Run.Get(root);

            if (r == null && children!= null && children.Count==1)
            {
                root = children[0];
                // skip a level if it is an empty header with only a single child
                r = Run.Get(root);
                children = null;
            }

            if (r == null)
            {
                tw.WriteLine($"<li><span class=\"treeSpan\"><a href=\"#\"><u class=\"treeSpan\">{root}</u></a><span> Reason </span></span>");
                Run r1 = Run.Get(root + ".1");
                if (root == "book")
                {
                    r1 = Run.Get("1");
                }

                    var inp = r1?.info.Input;
                if (inp != null)
                {
                    string b = inp.Trim(); //.Replace("\n", "<br/>");
                    // <span class="toggleSpan">This is a togglable span element.</span>
                    tw.WriteLine("<span class=\"toggleSpan\"><br/>" + b+ "</span>");
                }
            }
            else
            {
                if (r.info.Error != null)
                {
                    tw.WriteLine($"<li><span class=\"treeSpan\"><a href=\"#\"><u class=\"treeSpan\">{root}</u></a> <span><div class=\"highlight-text\">{r.info.Error}</div></span><span> {r.info.Title} ({r.info.Tool} {r.info.Budget} tokens)</span></span>");
                }
                else
                {
                    tw.WriteLine($"<li><span class=\"treeSpan\"><a href=\"#\"><u class=\"treeSpan\">{root}</u></a><span> {r.info.Title} ({r.info.Tool} {r.info.Budget} tokens)</span></span>");
                }
                if (r.output != null)
                {
                    string b = r.output.Trim().Replace("\n", "<br/>");
                    if (r.info.Tool!="Prose")
                    {
                        b = "<span class=\"toggleSpan\">" + b + "</span>";
                    }    
                    tw.WriteLine("<br/>" + b +"<br/>");
                }
            }
            bool leaf = children.IsNullOrEmpty();
            if (!leaf)
            {
                tw.WriteLine("<ul>");
                children.Sort();
                foreach (var n in children)
                {
                    Print(n, tw);
                }
                tw.WriteLine("</ul>");
            }
            tw.WriteLine("</li><br/>");
        }

        static Dictionary<string, List<string>> heirarchy = new Dictionary<string, List<string>>();
        static void BuildHeirarchy()
        {
            foreach (Run? run in Run.Runs.Values)
            {
                var id = run.Id;
                var sections = id.Split('.');
                List<string> h = new List<string>();
                string h1 = "book";
                for (int i = 0; i < sections.Length; i++)
                {
                    string h2 = h1 + "." + sections[i];
                    if (h1 == "book") h2 = sections[i];
                    if (!heirarchy.TryGetValue(h1, out var list))
                    {
                        list = new List<string>();
                        heirarchy[h1] = list;
                    }

                    if (!list.Contains(h2))
                        list.Add(h2);
                    h1 = h2;
                }
            }
        }
    }
}