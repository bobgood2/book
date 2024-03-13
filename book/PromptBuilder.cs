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
        static Dictionary<string, string> promptFiles= null;

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
            while(!done)
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

    }
}
