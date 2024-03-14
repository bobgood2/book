using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace book.Tools
{
    public class Outline : ITool
    {
        const string delimeter = "\n---\n";
        public void OnCompletion(Run run)
        {
            List<string> contextStack = new List<string>();
            for (Run r = run.GetParent(); r != null; r = r.GetParent())
            {
                if (r.tool is Outline)
                {
                    contextStack.Add(r.output.Trim()); ;
                }
                else if (r.tool is Start)
                {
                    contextStack.Add((r.info.Input+"\n"+ r.output).Trim());
                }
            }

            var content = run.output;
            if (contextStack.Count > 0)
            {
                contextStack.Reverse();
                content = string.Join("delimeter", contextStack) + "delimeter" + content;
            }

            _ = new Split(Run.Increment(run.Id), run.info.Title, content, run.Id, run.info.Budget);
        }

        public Outline()
        {

        }

        public Outline(string id, string title, string input, string parent, int budget)
        {
            var info = new RunInfo()
            {
                Tool = this.GetType().Name,
                Parent = parent,
                Model = "dev-gpt-4-turbo",
                MaxTokens = 2000,
                Temperature = .2,
                Budget= budget,
                TopP = 1,
                N = 1,
                Stop = new List<string>() { "<|im_end|>" },
                Input=input,
                Title=title
            };


            Dictionary<string, string> templates = new Dictionary<string, string>()
            {
                {"CONTEXT_STACK", input },
            };

            var run = new Run(id, info, PromptBuilder.Build(info.Tool, templates), this);
            _ = run.Execute();
        }

    }
}
