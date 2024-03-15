using book.Tools;
using LuToolbox.Common;
using LuToolbox;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JsonSerializer = System.Text.Json.JsonSerializer;
using System.Globalization;
using System.Runtime;
using System.ComponentModel;
using Microsoft.IdentityModel.Tokens;


namespace book
{
    public class Run
    {
        public static ConcurrentDictionary<string, Run> Runs = new ConcurrentDictionary<string, Run>();
        public string Id;
        public RunInfo info;
        public string prompt;
        public string output;
        public ITool tool = null;
        public Task<(LlmResponse[] Response, long LatencyInMilliseconds)> response = null;

        public Run(string id, RunInfo info, string prompt, ITool tool)
        {
            this.Id = id;
            this.info = info;
            this.prompt = prompt;
            this.tool = tool;
            Directory.CreateDirectory(id);
            Runs[id] = this;
            using (TextWriter tw = new StreamWriter(Path.Combine(id, "info.json")))
            {
                string jsonString = JsonSerializer.Serialize(info);
                tw.WriteLine(jsonString);
            }

            using (TextWriter tw = new StreamWriter(Path.Combine(id, "prompt.md")))
            {
                tw.WriteLine(prompt);
            }
        }

        public Run? GetParent()
        {
            var parentId = this.info.Parent;
            if (parentId.IsNullOrEmpty()) return null;
            return Runs[parentId];
        }

        public static Run? Get(string id)
        {
            if (id.IsNullOrEmpty()) return null;
            if (Runs.TryGetValue(id, out var run)) return run;
            return null;
        }

        static TaskQueue<Run> taskQueue = new TaskQueue<Run>(8);
        static HashSet<string> executing = new HashSet<string>();
        public async Task<Run> Execute()
        {
            lock (executing)
            {
                executing.Add(this.Id);
            }
            var r = this;
            return await taskQueue.Queue(async () =>
            {
                return await r.DoExecute();
            });
        }

        public async Task<Run> DoExecute()
        {
            Console.WriteLine(DateTime.Now + " start "+this.Id + " "+this.info.Tool + " "+this.info.Budget);

            var query = new List<string> { this.prompt };
            LlmRequest requestData = LlmApiCompletionRequestHandling.CreateRequest(prompt: query, stop: string.Join(",", info.Stop),
                maxTokens: info.MaxTokens, temperature: info.Temperature, completionCount: info.N);
            var response = await LlmApiCompletionRequestHandling.SendRequestWithLatency(info.Model, requestData, 10);
            Console.WriteLine(DateTime.Now + " end " + this.Id);
            this.output = response.Response[0].Choices[0]?.Text;

            List<string> cleanList = new List<string>();

            Console.WriteLine("processing " + this.Id);
            this.info.InputTokens = LlmHelper.GetGptTokenCount(this.prompt, this.info.Model);
            this.info.OutputTokens = LlmHelper.GetGptTokenCount(this.output, this.info.Model);
            this.info.Latency = response.LatencyInMilliseconds;

            try
            {
                tool.OnCompletion(this);
            }
            catch (Exception ex)
            {
                this.info.Error = "ex.Message";
                Console.WriteLine("could not process " + this.Id);
            }

            if (this.info.Error != null)
            {
                Console.WriteLine($"Error {this.Id} {this.info.Error}");
            }

            cleanList.Add(Path.Combine(this.Id, "output.txt"));
            using (TextWriter log = new StreamWriter(Path.Combine(this.Id, "output.txt")))
            {
                log.WriteLine(output);
            }

            using (TextWriter tw = new StreamWriter(Path.Combine(this.Id, "info.json")))
            {
                string jsonString = JsonSerializer.Serialize(info);
                tw.WriteLine(jsonString);
            }


            using (TextWriter log = new StreamWriter(Path.Combine(this.Id, "log.log")))
            {
                cleanList.Add(Path.Combine(this.Id, "log.log"));
                log.WriteLine($"latency: {response.LatencyInMilliseconds}");
                log.WriteLine($"prompt tokens: {LlmHelper.GetGptTokenCount(this.prompt, this.info.Model)}");
                log.WriteLine($"result tokens: {LlmHelper.GetGptTokenCount(this.output, this.info.Model)}");
            }

            using (TextWriter bat = new StreamWriter(Path.Combine(this.Id, "clean.bat")))
            {
                foreach (var key2 in cleanList)
                {
                    bat.WriteLine("del " + Path.Combine(Directory.GetCurrentDirectory(), key2));
                }
            }


            lock(executing)
            {
                executing.Remove(this.Id);
            }

            return this;
        }

        public static bool IsDone()
        {
            lock (executing)
            {
                return executing.Count == 0;
            }
        }

        public Run(string id, bool start)
        {
            this.Id = id;
            using (TextReader tr = new StreamReader(Path.Combine(id, "info.json")))
            {
                string jsonString = tr.ReadToEnd();
                this.info = JsonSerializer.Deserialize<RunInfo>(jsonString);
                if (this.info.Tool == "VerySmallOutline")
                {
                    this.info.Tool = "Outline";
                }
                if (this.info.Tool == "SmallOutline")
                {
                    this.info.Tool = "Outline";
                }
                this.tool = info.GetTool();
            }

            using (TextReader tr = new StreamReader(Path.Combine(id, "prompt.md")))
            {
                this.prompt = tr.ReadToEnd();
            }

            string f = Path.Combine(this.Id, "output.txt");
            if (File.Exists(f)) 
            {
                this.output = File.ReadAllText(f);
            }

            if (this.output == null && start)
            {
                _ = Execute();
            }

            Runs[id] = this;
        }

        public static void Scan(bool start)
        {
            Console.WriteLine("Scanning");
            List<(string path, DateTime dt)> runs = new List<(string path, DateTime dt)>();
            foreach (var d in Directory.GetDirectories(Directory.GetCurrentDirectory()))
            {
                if (File.Exists(Path.Combine(d,"info.json")))
                {
                    DateTime age = File.GetCreationTime(Path.Combine(d, "info.json"));
                    runs.Add((Path.GetFileName(d),age));
                }
            }

            runs.Sort((a, b) => a.dt.CompareTo(b.dt));

            foreach (var run in runs)
            {
                new Run(run.path, start);
            }

            Console.WriteLine("Done Scanning");
        }

        public static string Increment(string id)
        {
            int pos = id.LastIndexOf('.') + 1;
            int n = int.Parse(id.Substring(pos));
            return id.Substring(0, pos) + ++n;
        }

        public static string Child(string id, int n)
        {
            char c = (char)('A' + n);
            return id + "." + c + ".1";
        }
    }
}
