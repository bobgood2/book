using book.Tools;
using LuToolbox.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
    using JsonSerializer = System.Text.Json.JsonSerializer;

namespace book
{
    public class RunInfo
    {
        [JsonProperty(PropertyName = "parent", NullValueHandling = NullValueHandling.Ignore)]
        public string Parent { get; set; } = null;

        [JsonProperty(PropertyName = "tool", NullValueHandling = NullValueHandling.Ignore)]
        public string Tool { get; set; } = null;

        [JsonProperty(PropertyName = "model", NullValueHandling = NullValueHandling.Ignore)]
        public string Model { get; set; } = null;

        [JsonProperty(PropertyName = "max_tokens", NullValueHandling = NullValueHandling.Ignore)]
        public int MaxTokens { get; set; }

        [JsonProperty(PropertyName = "temperature", NullValueHandling = NullValueHandling.Ignore)]
        public double Temperature { get; set; }

        [JsonProperty(PropertyName = "top_p", NullValueHandling = NullValueHandling.Ignore)]
        public int TopP { get; set; }

        [JsonProperty(PropertyName = "n", NullValueHandling = NullValueHandling.Ignore)]
        public int N { get; set; }

        [JsonProperty(PropertyName = "budget", NullValueHandling = NullValueHandling.Ignore)]
        public int Budget { get; set; }

        [JsonProperty(PropertyName = "stop", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Stop { get; set; }

        [JsonProperty(PropertyName = "input", NullValueHandling = NullValueHandling.Ignore)]
        public string Input { get; set; }

        [JsonProperty(PropertyName = "title", NullValueHandling = NullValueHandling.Ignore)]
        public string Title { get; set; }

        static List<ITool> tools = new List<ITool>()
        {
            new Start(),
            new Outline(),
            new Split(),
        };

        ITool GetTool()
        {
            foreach (var t in tools)
            {
                if (this.Tool == t.GetType().Name)

                {
                    return t;
                }
            }

            throw new NotImplementedException();
        }
    }
}
