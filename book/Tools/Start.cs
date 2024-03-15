using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Runtime.Intrinsics.X86;
using System.Security.Principal;
using System.Text;
using System.Threading.Channels;
using System.Threading;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace book.Tools
{
    public class Start : ITool
    {
        public void OnCompletion(Run run)
        {
            // # Title: The Energy Odyssey: Illuminating the Path to Prosperity
            // # Author: The Copilot Collective
            // # Tokens: 500000
            // # General Instructions
            //             As you plan and write prose for this book, focus on providing factual and quantitative information about the impact of energy innovation on human wealth over the past two centuries.Highlight the areas where energy innovation is likely to continue and where it may have reached its peak. Discuss the risks, challenges, and benefits of potential new directions in energy innovation. Maintain an optimistic tone about the potential for significant advancements while acknowledging the limitations of predicting specific outcomes and timelines. Ensure that the content is detailed, honest, and avoids making calls to action or implying cultural values. Emulate the educational approach of "Guns, Germs, and Steel," and use this perspective to convey the excitement of our energy future.Avoid general statements and instead provide concrete examples and data to illustrate the improvements in health and other aspects of human life due to energy innovations.
            // 
            // # Specific Instructions for Each Section
            // 1.Introduction: Set the stage for the reader by explaining the book's purpose and the importance of energy innovation in human history. Provide a brief overview of the content and the approach taken in the book.
            // 2.Historical Impact: Detail the ways in which energy innovation has transformed human wealth over the past two centuries.Use specific examples and data to illustrate the magnitude of these changes.
            // 3.Current Innovations: Survey the current landscape of energy innovation, highlighting the most promising areas and technologies.Discuss the potential for continued improvement in these areas.
            // 4.Plateaus and Peaks: Identify areas where energy innovation may have reached its peak and explore the reasons behind this.Discuss the implications of these plateaus for future growth.
            // 5.The Next Generation: Speculate on the areas where energy innovation is likely to occur over the next generation.Discuss the challenges and opportunities associated with these potential advancements.
            // 6.Risks and Benefits: Analyze the risks and challenges associated with pursuing new directions in energy innovation.Contrast these with the potential benefits and improvements to human life.
            // 7.Conclusion: Summarize the key points made throughout the book and reiterate the optimistic outlook for the future of energy innovation.Leave the reader with a sense of excitement and curiosity about what lies ahead.
            // 
            // Remember to maintain a factual and quantitative approach throughout the book, providing concrete examples and data to support your points.The goal is to educate the reader and inspire them to appreciate the exciting possibilities of our energy future without resorting to calls to action or cultural value judgments.
            List<string> instructions = new List<string>();
            string title = null;
            string author = null;
            int budget = 10000;
            foreach (var line0 in run.output.Split('\n'))
            {
                var line = line0.Trim();
                if (line.StartsWith('#'))
                {
                    int n = line.IndexOf(":");
                    if (n>0)
                    {
                        var left = line[1..n].ToLower().Trim();
                        var right = line[(n + 1)..].Trim();
                        if (left == "title")
                        {
                            title = right;
                        }
                        else if (left == "author")
                        {
                            author = right;
                        }
                        else if (left == "tokens")
                        {
                            if (!int.TryParse(right, out budget))
                            {  }
                        }
                    }
                }
                else
                {
                    instructions.Add(line);
                }
            }


            var content = run.info.Input + "\n" + string.Join("\n", instructions);
            if (title == null) title = "My Document";
            if (author == null) author = "Copilot";
            _ = new Outline(Run.Increment(run.Id), title + " by " + author, content, run.Id, budget);

            run.info.Title = title;
            run.info.Author= author;
            run.info.Budget = budget;
        }

        public string GetPrompt(RunInfo info)
        {
            return this.GetType().Name;
        }

        public Start()
        {

        }

        public Start(string id, string title, string prompt, string parent)
        {
            var info = new RunInfo()
            {
                Tool = this.GetType().Name,
                Parent = parent,
                Model = "dev-gpt-4-turbo",
                MaxTokens = 2000,
                Temperature = 0,
                TopP = 1,
                N = 1,
                Title = title,
                Input = prompt,
                Stop = new List<string>() { "<|im_end|>" },
            };

            Dictionary<string, string> templates = new Dictionary<string, string>()
            {
                {"CONTEXT_STACK", prompt },
            };

            var run = new Run(id, info, PromptBuilder.Build(GetPrompt(info), templates), this);
            _ = run.Execute();
        }

    }
}
