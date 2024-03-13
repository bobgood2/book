using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace book.Tools
{
    public class Outline : ITool
    {
        public void Process(Run run)
        {
            _ = new Split(Run.Increment(run.Id), run.output, run.Id, run.info.Budget);
        }

        public Outline()
        {

        }

        public Outline(string id, string input, string parent, int budget)
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
