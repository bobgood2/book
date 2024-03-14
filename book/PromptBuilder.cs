using book.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace book
{
    public static class PromptBuilder
    {
        static Dictionary<string, string> promptFiles = null;

        public static void Init()
        {
            promptFiles = ReadTemplates();
        }
        public static Dictionary<string, string> ReadTemplates()
        {
            Dictionary<string, string> promptFiles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in Directory.GetFiles("Prompts", "*.md"))
            {
                promptFiles[Path.GetFileNameWithoutExtension(t)] = File.ReadAllText(t);
            }

            return promptFiles;
        }

        public static string Build(string name, Dictionary<string, string> templates)
        {
            string result = promptFiles[name];
            bool done = false;
            while (!done)
            {
                done = true;
                foreach (var kvp in templates)
                {
                    var key = kvp.Key.ToUpper();
                    if (result.Contains(key))
                    {
                        result = result.Replace(key, kvp.Value);
                        done = false;
                    }
                }

                foreach (var kvp in promptFiles)
                {
                    var key = kvp.Key.ToUpper();
                    if (result.Contains(key))
                    {
                        result = result.Replace(key, kvp.Value);
                        done = false;
                    }
                }
            }

            return result;
        }

        private static string LocalContext(string output, Run run, int level)
        {
            var totalBudget = Run.Get("2").info.Budget;
            double pct = 100 * (double)run.info.Budget / totalBudget;
            string prefix = "";
            string pcts = $"{pct:0}%";
            if (pct < .4)
            {
                pcts = $"{pct:0.00}%";
            }
            else if (pct < 3)
            {
                pcts = $"{pct:0.00}%";
            }
            if (level == 0)
            {
                prefix = $"This is the context for the a subsection of the document you are authoring.  This represents {pcts} of the document\n\n";
            }
            else if (pct == 100)
            {
                prefix = "This is the context for the entire document.   Do not generate this section.\n\n";
            }
            else
            {
                prefix = $"This is the context for level {level} of the document you are helping create, where higher levels are more global. This section represents {pcts} of the whole document.   Do not generate this section.\n\n";
            }

            return prefix + output.Trim();

        }

        public static string CreateContextStack(Run run, string content)
        {
            const string delimeter = "\n\n---\n\n";
            List<string> contextStack = new List<string>();
            int level = 0;

            contextStack.Add(LocalContext(content, run, level));
            for (Run r = run.GetParent(); r != null; r = r.GetParent())
            {
                level++;
                if (r.tool is Outline)
                {
                    contextStack.Add(LocalContext(r.output, r, level));
                }
                else if (r.tool is Start)
                {
                    contextStack.Add(LocalContext(r.info.Input + "\n" + r.output, r, level));
                }
            }

            contextStack.Reverse();
            var stack= string.Join(delimeter, contextStack) + delimeter + content;
            return stack;
        }
    }
}
