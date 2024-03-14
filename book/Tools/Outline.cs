using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace book.Tools
{
    public class Outline : ITool
    {
        public void OnCompletion(Run run)
        {
            _ = new Split(Run.Increment(run.Id), run.info.Title, PromptBuilder.CreateContextStack(run, run.output + $"\n you have a budget of {run.info.Budget} tokens\n"), run.Id, run.info.Budget);
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
                MaxTokens = 1000,
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
