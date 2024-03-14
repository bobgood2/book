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
            var dst = home; // @"C:\Users\bobgood\OneDrive - Microsoft\Shared with Everyone\mybook";
            if (args.Length>1)
            {
                dst = args[1];
                if (!Directory.Exists(dst))
                {
                    Directory.CreateDirectory(dst);
                }
            }

            string html = File.ReadAllText("HTML/HtmlPage1.html");
            var parts = html.Split("CONTENT");

            Directory.SetCurrentDirectory(home);
            Run.Scan(false);

            var top = parts[0];
            Run r = Run.Get("1");
            top = top.Replace("TITLE", r.info.Title);
            top = top.Replace("AUTHOR", "Authored By "+r.info.Author);

            var bottom = parts[1];

            BuildHeirarchy();
            using (TextWriter tw = new StreamWriter(Path.Combine(dst, "book.html")))
            {
                tw.WriteLine(top);
                Print("book", tw);
                tw.WriteLine(bottom);
            }

            if (args.Length == 1)
            {
                Process.Start(new ProcessStartInfo(Path.Combine(dst, "book.html")) { UseShellExecute = true });
            }
        }

        static void Print(string root, TextWriter tw)
        {
            List<string> children = null;
            heirarchy.TryGetValue(root, out children);
            Run r = Run.Get(root);
            if (r == null)
            {
                tw.WriteLine($"<li><a href=\"#\"><b>{root}</b></a><span> Reason </span>");
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
                    tw.WriteLine($"<li><a href=\"#\"><b>{root}</b></a> <span><div class=\"highlight-text\">{r.info.Error}</div></span><span> {r.info.Title} ({r.info.Tool} {r.info.Budget} tokens)</span>");
                }
                else
                {
                    tw.WriteLine($"<li><a href=\"#\"><b>{root}</b></a><span> {r.info.Title} ({r.info.Tool} {r.info.Budget} tokens)</span>");
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