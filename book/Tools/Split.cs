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
        public void Process(Run run)
        {
            Dictionary<string, string> g1 = new Dictionary<string, string>();
            Dictionary<string, string> g2 = new Dictionary<string, string>();
            string key1 = null;
            string key2 = null;
            foreach (var line0 in run.output.Split('\n'))
            {
                if (line0.StartsWith('#'))
                {
                    key1 = line0;
                    key2 = null;
                    g1[key1] = "";
                }
                else if (line0.StartsWith("##"))

                {
                    key2 = line0;
                    key1 = null;
                    g2[key2] = "";

                }
                else if (line0.Trim().Length > 0)
                {
                    if (key1 != null)
                    {
                        g1[key1] += line0.Trim() + '\n';
                    }
                    if (key2 != null)
                    {
                        g2[key2] += line0.Trim() + '\n';
                    }
                }
            }

            if (g1.Count <= 1 && g2.Count > g1.Count)
            {
                g1 = g2;
            }

            int cnt = 0;
            foreach (var hdr in g1.Keys)
            {
                var v = g1[hdr];
                (string h1, int budget) = GetBudget(hdr);
                string content = h1 + "\n" + v;
                if (budget>500)
                {
                    _ = new Outline(Run.Child(run.Id, cnt++), content, run.Id, budget); 
                }
                else
                {
                    _ = new Prose(Run.Child(run.Id, cnt++), content, run.Id, budget); )
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
                int pos2 = h1.IndexOf(' ');
                if (int.TryParse(h1[0..pos2], out int budget))
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

        public Split(string id, string prompt, string parent, int budget)
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
