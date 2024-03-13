using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace LuToolbox.Models
{
    public class BaseCompletionRequest
    {
        [JsonProperty(PropertyName = "model", NullValueHandling = NullValueHandling.Ignore)]
        public string? Model { get; set; }

        [JsonProperty(PropertyName = "functions", NullValueHandling = NullValueHandling.Ignore)]
        public List<ChatFunction>? Functions { get; set; }

        [JsonProperty(PropertyName = "function_call", NullValueHandling = NullValueHandling.Ignore)]
        public string? FunctionCall { get; set; }

        [JsonProperty(PropertyName = "temperature", NullValueHandling = NullValueHandling.Ignore)]
        public double? Temperature { get; set; }

        [JsonProperty(PropertyName = "top_p", NullValueHandling = NullValueHandling.Ignore)]
        public double? TopP { get; set; }

        [JsonProperty(PropertyName = "n", NullValueHandling = NullValueHandling.Ignore)]
        public int? NumberOfChoices { get; set; }

        [JsonProperty(PropertyName = "stop", NullValueHandling = NullValueHandling.Ignore)]
        public List<string> StopSequence { get; set; }

        [JsonProperty(PropertyName = "max_tokens", NullValueHandling = NullValueHandling.Ignore)]
        public int? MaximumTokens { get; set; }

        [JsonProperty(PropertyName = "presence_penalty", NullValueHandling = NullValueHandling.Ignore)]
        public double? PresencePenalty { get; set; }

        [JsonProperty(PropertyName = "frequency_penalty", NullValueHandling = NullValueHandling.Ignore)]
        public double? FrequencyPenalty { get; set; }

        [JsonProperty(PropertyName = "logit_bias", NullValueHandling = NullValueHandling.Ignore)]
        public double? LogitBias { get; set; }
    }

    public class ChatCompletionRequest : BaseCompletionRequest
    {
        [JsonProperty(PropertyName = "messages", NullValueHandling = NullValueHandling.Ignore)]
        public List<ChatCompletionMessage>? Messages { get; set; }

    }

    public class LegacyCompletionRequest : BaseCompletionRequest
    {
        [JsonProperty(PropertyName = "prompt", NullValueHandling = NullValueHandling.Ignore)]
        public string? Prompt { get; set; }

    }

    public class BaseCompletionResponse
    {
        [JsonProperty(PropertyName = "id", NullValueHandling = NullValueHandling.Ignore)]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "object", NullValueHandling = NullValueHandling.Ignore)]
        public string Object { get; set; }

        [JsonProperty(PropertyName = "created", NullValueHandling = NullValueHandling.Ignore)]
        public int Created { get; set; }

        [JsonProperty(PropertyName = "model", NullValueHandling = NullValueHandling.Ignore)]
        public string Model { get; set; }

        [JsonProperty(PropertyName = "usage", NullValueHandling = NullValueHandling.Ignore)]
        public Usage Usage { get; set; }

        [JsonProperty(PropertyName = "error", NullValueHandling = NullValueHandling.Ignore)]
        public Error Error { get; set; }
    }

    public class BaseCompletionChoice
    {
        [JsonProperty(PropertyName = "index", NullValueHandling = NullValueHandling.Ignore)]
        public int Index { get; set; }

        [JsonProperty(PropertyName = "logprobs", NullValueHandling = NullValueHandling.Ignore)]
        public object LogProbs { get; set; }

        [JsonProperty(PropertyName = "finish_reason", NullValueHandling = NullValueHandling.Ignore)]
        public string FinishReason { get; set; }
    }

    public class ChatCompletionChoice : BaseCompletionChoice
    {
        [JsonProperty(PropertyName = "message", NullValueHandling = NullValueHandling.Ignore)]
        public ChatCompletionMessage Message { get; set; }
    }

    public class ChatCompletionResponse : BaseCompletionResponse
    {
        [JsonProperty(PropertyName = "choices", NullValueHandling = NullValueHandling.Ignore)]
        public List<ChatCompletionChoice> Choices { get; set; }
    }

    public class LegacyCompletionChoice : BaseCompletionChoice
    {
        [JsonProperty(PropertyName = "text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }
    }

    public class LegacyCompletionResponse : BaseCompletionResponse
    {
        [JsonProperty(PropertyName = "choices", NullValueHandling = NullValueHandling.Ignore)]
        public List<LegacyCompletionChoice> Choices { get; set; }
    }

    public class ChatCompletionMessage
    {
        [JsonProperty(PropertyName = "role", NullValueHandling = NullValueHandling.Ignore)]
        public string? Role { get; set; }

        [JsonProperty(PropertyName = "content", NullValueHandling = NullValueHandling.Ignore)]
        public string? Content { get; set; }

        [JsonProperty(PropertyName = "name", NullValueHandling = NullValueHandling.Ignore)]
        public string? Name { get; set; }

        [JsonProperty(PropertyName = "function_call", NullValueHandling = NullValueHandling.Ignore)]
        public string? FunctionCall { get; set; }
    }

    public class ChatFunctionCall
    {
        [JsonProperty(PropertyName = "name", NullValueHandling = NullValueHandling.Ignore)]
        public string? Name { get; set; }

        [JsonProperty(PropertyName = "arguments", NullValueHandling = NullValueHandling.Ignore)]
        public string? Arguments { get; set; }
    }

    public class ChatFunction
    {
        [JsonProperty(PropertyName = "name", NullValueHandling = NullValueHandling.Ignore)]
        public string? Name { get; set; }

        [JsonProperty(PropertyName = "description", NullValueHandling = NullValueHandling.Ignore)]
        public string? Description { get; set; }

        [JsonProperty(PropertyName = "parameters", NullValueHandling = NullValueHandling.Ignore)]
        public JObject? Parameters { get; set; }
    }
}
