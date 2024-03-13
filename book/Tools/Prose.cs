using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace book.Tools
{
    public class Prose : ITool
    {
        public void OnCompletion(Run run)
        {
        }

        public Prose()
        {

        }

        public Prose(string id, string title, string input, string parent, int budget)
        {
            var info = new RunInfo()
            {
                Tool = this.GetType().Name,
                Parent = parent,
                Model = "dev-gpt4-32k",
                MaxTokens = 2000,
                Temperature = .2,
                Budget= budget,
                TopP = 1,
                N = 1,
                Stop = new List<string>() { "<|im_end|>" },
                Input=input,
                Title=title,
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
