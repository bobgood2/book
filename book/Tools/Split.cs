using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace book.Tools
{
    public class Split : ITool
    {
        const string generalTitle = "General Instructions";
        
        public void OnCompletion(Run run)
        {
            Dictionary<string, string> g1 = new Dictionary<string, string>();
            Dictionary<string, string> g2 = new Dictionary<string, string>();
            Dictionary<string, string> g1b = new Dictionary<string, string>();
            Dictionary<string, string> g2b = new Dictionary<string, string>();
            string key1 = null;
            string key2 = null;
            foreach (var line0 in run.output.Split('\n'))
            {
                if (line0.StartsWith('#'))
                {
                    key1 = line0[1..].Trim();
                    if (key1.StartsWith(generalTitle)) {
                        key1 = generalTitle;
                    }

                    key2 = null;
                    g1[key1] = "";
                    g1b[key1] = "";
                }
                else if (line0.StartsWith("##"))

                {
                    key2 = line0[2..].Trim();
                    if (key2.StartsWith(generalTitle)) {
                        key2 = generalTitle;
                    }

                    g2[key2] = "";
                    g2b[key2] = "";
                    if (key1 != null)
                    {
                        g1[key1] += line0.Trim() + '\n';
                    }

                }
                else if (line0.Trim().Length > 0 && line0[0] == '-')
                {
                    if (key1 != null)
                    {
                        g1[key1] += line0.Trim() + '\n';
                    }
                    if (key2 != null)
                    {
                        g2[key2] += line0.Trim() + '\n';
                    }
                } else if (line0.Trim().Length>0)
                {
                    if (key1 != null)
                    {
                        g1b[key1] += line0.Trim() + '\n';
                    }
                    if (key2 != null)
                    {
                        g2b[key2] += line0.Trim() + '\n';
                    }
                }
            }

            if (g1.Count <= 1 && g2.Count > g1.Count)
            {
                g1 = g2;
                g1b = g2b;
            }

            int cnt = 0;

            string general = "";
            if (!g1.TryGetValue(generalTitle, out general))
            { 
            }

            foreach (var hdr in g1.Keys)
            {
                var v = g1[hdr];
                (string h1, int budget) = GetBudget(hdr);
                if (v.Length==0)
                {
                    run.info.Error = $"could not instructions for {hdr}";

                    v = g1b[hdr];
                }
                string content = h1 + "\n" + v;
                if (general!=null)
                {
                    content += "\n" + general;
                }

                if (budget > 5000)
                {
                    _ = new Outline(Run.Child(run.Id, cnt++), hdr, PromptBuilder.CreateContextStack(run, content), run.Id, budget);
                }
                else if (budget>1000)
                {
                    _ = new SmallOutline(Run.Child(run.Id, cnt++), hdr, PromptBuilder.CreateContextStack(run, content), run.Id, budget); 
                }
                else if (budget>0)
                {
                    _ = new Prose(Run.Child(run.Id, cnt++), hdr, content, run.Id, budget); 
                }
                else if (hdr == generalTitle)
                {

                }
                else
                {
                    run.info.Error = $"could not find token count for {hdr}";
                    // default???
                    //_ = new Prose(Run.Child(run.Id, cnt++), hdr, content, run.Id, 200);
                }
            }
        }

        private (string hdr, int budget) GetBudget(string hdr)
        {
            var h1 = hdr.Trim();
            if (h1.EndsWith(")"))
            {
                int pos = h1.LastIndexOf("(");
                var par = h1[(pos + 1)..^1];
                h1 = h1.Substring(0, pos - 1).Trim();
                int pos2 = par.IndexOf(' ');
                if (int.TryParse(par[0..pos2], out int budget))
                {
                    return (h1, budget);
                }
            }
            else if (h1.ToLower().EndsWith("tokens"))
            {
                int pos = h1.LastIndexOf(" ");
                int pos2 = h1.LastIndexOf(" ", pos - 1);
                if (int.TryParse(h1[(pos + 1)..(pos2 - 1)], out int budget))
                {
                    return (h1[0..(pos - 1)], budget);
                }
            }

            return (h1, 0);
        }

        public Split()
        {

        }

        public Split(string id, string title, string prompt, string parent, int budget)
        {
            var info = new RunInfo()
            {
                Tool = this.GetType().Name,
                Parent = parent,
                Model = "dev-gpt-4-turbo",
                MaxTokens = 2000,
                Temperature = 0,
                Budget = budget,
                TopP = 1,
                N = 1,
                Stop = new List<string>() { "<|im_end|>" },
                Input = prompt,
                Title=title,
            };

            Dictionary<string, string> templates = new Dictionary<string, string>()
            {
                {"CONTEXT_STACK", BuildContextStack(parent) },
            };

            var run = new Run(id, info, PromptBuilder.Build(info.Tool, templates), this);
            _ = run.Execute();
        }

        public static string BuildContextStack(string id)
        {
            List<Run> stack = new List<Run>();
            Run first = Run.Get(id);
            for (Run parent = first; parent != null; parent = parent.GetParent())
            {
                if (parent.info.Tool == typeof(Outline).Name)
                {
                    stack.Add(parent);
                }
            }

            stack.Reverse();
            var root = stack.First();
            string result = root.info.Input;
            foreach (var level in stack)
            {
                result += "<|im_end|>\n";
                result += "<|im_start|>assistant\n";
                result += level.output + "\n";
            }
            result += "<|im_end|>\n";
            result += "<|im_start|>user\n";

            result += $"you have a budget of {first.info.Budget} tokens\n";
            return result;
        }
    }
}
