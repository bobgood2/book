namespace LuToolbox
{
    using LuToolbox.Models;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    public class LlmRequest
    {
        [JsonProperty(PropertyName = "prompt", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Prompt { get; set; } = null;

        [JsonProperty(PropertyName = "messages", NullValueHandling = NullValueHandling.Ignore)]
        public List<ChatCompletionMessage> Message { get; set; } = null;

        [JsonProperty(PropertyName = "max_tokens", NullValueHandling = NullValueHandling.Ignore)]
        public int MaxTokens { get; set; }

        [JsonProperty(PropertyName = "temperature", NullValueHandling = NullValueHandling.Ignore)]
        public double Temperature { get; set; }

        [JsonProperty(PropertyName = "top_p", NullValueHandling = NullValueHandling.Ignore)]
        public int TopP { get; set; }

        [JsonProperty(PropertyName = "n", NullValueHandling = NullValueHandling.Ignore)]
        public int N { get; set; }

        [JsonProperty(PropertyName = "stream", NullValueHandling = NullValueHandling.Ignore)]
        public bool Stream { get; set; }

        //[JsonProperty(PropertyName = "logprobs", NullValueHandling = NullValueHandling.Ignore)]
        //public object? LogProbs { get; set; }

        [JsonProperty(PropertyName = "stop", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> Stop { get; set; }

        [JsonProperty(PropertyName = "presence_penalty", NullValueHandling = NullValueHandling.Ignore)]
        public double PresencePenalty { get; set; }
    }

    public class Choice
    {
        [JsonProperty(PropertyName = "text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }

        [JsonProperty(PropertyName = "message", NullValueHandling = NullValueHandling.Ignore)]
        public ChatCompletionMessage Message { get; set; }

        [JsonProperty(PropertyName = "index", NullValueHandling = NullValueHandling.Ignore)]
        public int Index { get; set; }

        [JsonProperty(PropertyName = "logprobs", NullValueHandling = NullValueHandling.Ignore)]
        public object LogProbs { get; set; }

        [JsonProperty(PropertyName = "finish_reason", NullValueHandling = NullValueHandling.Ignore)]
        public string FinishReason { get; set; }
    }

    public class LlmResponse
    {
        [JsonProperty(PropertyName = "id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "object", NullValueHandling = NullValueHandling.Ignore)]
        public string Object { get; set; }

        [JsonProperty(PropertyName = "created", NullValueHandling = NullValueHandling.Ignore)]
        public int Created { get; set; }

        [JsonProperty(PropertyName = "choices", NullValueHandling = NullValueHandling.Ignore)]
        public List<Choice> Choices { get; set; }

        [JsonProperty(PropertyName = "model", NullValueHandling = NullValueHandling.Ignore)]
        public string Model { get; set; }

        [JsonProperty(PropertyName = "usage", NullValueHandling = NullValueHandling.Ignore)]
        public Usage Usage { get; set; }

        [JsonProperty(PropertyName = "error", NullValueHandling = NullValueHandling.Ignore)]
        public Error Error { get; set; }
    }

    public class Error
    {
        [JsonProperty(PropertyName = "message", NullValueHandling = NullValueHandling.Ignore)]
        public string Message { get; set; }

        [JsonProperty(PropertyName = "type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "param", NullValueHandling = NullValueHandling.Ignore)]
        public string Parameter { get; set; }

        [JsonProperty(PropertyName = "code", NullValueHandling = NullValueHandling.Ignore)]
        public string Code { get; set; }
    }

    public class Usage
    {
        [JsonProperty(PropertyName = "prompt_tokens", NullValueHandling = NullValueHandling.Ignore)]
        public int PromptTokenCount { get; set; }

        [JsonProperty(PropertyName = "completion_tokens", NullValueHandling = NullValueHandling.Ignore)]
        public int CompletionTokenCount { get; set; }

        [JsonProperty(PropertyName = "total_tokens", NullValueHandling = NullValueHandling.Ignore)]
        public int TotalTokenCount { get; set; }
    }
}