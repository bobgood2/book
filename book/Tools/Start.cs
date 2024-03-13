using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace book.Tools
{
    public class Start : ITool
    {
        public void Process(Run run)
        {
            throw new NotImplementedException();
        }

        public Start()
        {

        }

        public Start(string id, string prompt, string parent)
        {
            var info = new RunInfo()
            {
                Tool = this.GetType().Name,
                Parent = parent,
                Model = "gpt-35-turbo",
                MaxTokens = 400,
                Temperature = 0,
                TopP = 1,
                N = 1,
                Stop = new List<string>() { "<|im_end|>" },
            };

            Dictionary<string, string> templates = new Dictionary<string, string>()
            {
                {"CONTEXT_STACK", prompt },
            };

            var run = new Run(id, info, PromptBuilder.Build("start", templates), this);
            _ = run.Execute();
        }

    }
}
