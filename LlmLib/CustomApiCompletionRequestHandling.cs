namespace LuToolbox.Common
{
    using LuToolbox.Models;
    using Microsoft.Identity.Client;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using JsonSerializer = System.Text.Json.JsonSerializer;

    public class CustomApiCompletionRequestHandling
    {
        const string DefaultEndpoint = "https://api.openai.com/v1/chat/completions";

        public static async Task<string> SendRequest(
            string requestData,
            string token,
            string endpoint,
            LlmServiceAuthType authType)
        {
            var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Content = new StringContent(requestData, Encoding.UTF8, "application/json");

            switch (authType)
            {
                case LlmServiceAuthType.ApiKey:
                    httpClient.DefaultRequestHeaders.Add("api-key", token);
                    break;

                case LlmServiceAuthType.Bearer:
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    break;
            }

            var httpResponse = await httpClient.SendAsync(request);

            return await httpResponse.Content.ReadAsStringAsync();
        }

        public async static Task<(LlmResponse[] Response, long AverageLatencyInMilliseconds)> SendRequestWithLatency(
            string modelName,
            LlmRequest request,
            int maxRetries,
            string token,
            bool supressErrors = false,
            ConcurrentQueue<string> progressMessages = null,
            string endpoint = DefaultEndpoint,
            LlmServiceAuthType authType = LlmServiceAuthType.Bearer)
        {
            if (endpoint == string.Empty)
            {
                endpoint = DefaultEndpoint;
            }

            var response = new List<LlmResponse>();

            var emptyResponse = new LlmResponse()
            {
                Choices = new List<Choice>()
                {
                    new Choice()
                    {
                        Text = ""
                    }
                }
            };

            var requestQueue = new Stack<(string Prompt, int FailCount)>();

            // Chat completions do not support the same type of batching, so we have to ... unbatch
            request.Prompt.Reverse();
            foreach (var prompt in request.Prompt)
            {
                requestQueue.Push((prompt, 0));
            }

            int count = 1, total = requestQueue.Count;
            long totalLatency = 0;

            while (requestQueue.Count > 0)
            {
                var current = requestQueue.Pop();

                var legacyCompletionRequest = new LegacyCompletionRequest()
                {
                    Model = modelName,
                    Prompt = current.Prompt,
                    MaximumTokens = request.MaxTokens,
                    NumberOfChoices = request.N,
                    TopP = request.TopP,
                    Temperature = request.Temperature,
                    StopSequence = request.Stop,
                };

                var requestData = JsonConvert.SerializeObject(legacyCompletionRequest);

                var currentLatency = Stopwatch.StartNew();

                var rawResponse = await SendRequest(requestData, token, endpoint, authType);
                try
                {
                    if (rawResponse.Contains("(429) Too Many Requests"))
                    {
                        // If rate limit exceeded, wait based on number of failures and try again
                        if (current.FailCount < 10)
                        {
                            Thread.Sleep(current.FailCount * 5000);
                            requestQueue.Push((current.Prompt, current.FailCount + 1));
                        }
                        else
                        {
                            Console.WriteLine($"Max failures reached");
                        }
                    }
                    else
                    {
                        var legacyResponse = JsonConvert.DeserializeObject<LlmResponse>(rawResponse);

                        if (legacyResponse.Choices?.Count > 0)
                        {

                            response.Add(legacyResponse);

                            // Only insert progress message if successful
                            if (progressMessages != null)
                            {
                                progressMessages.Enqueue($"Processed request {count} of {total}");
                            }

                            count++;
                            totalLatency += currentLatency.ElapsedMilliseconds;
                        }
                        else if (legacyResponse.Error != null && (legacyResponse.Error.Code == "rate_limit_exceeded" || legacyResponse.Error.Code == "429"))
                        {
                            // If rate limit exceeded, wait based on number of failures and try again
                            if (current.FailCount < 10)
                            {
                                Thread.Sleep(current.FailCount * 5000);
                                requestQueue.Push((current.Prompt, current.FailCount + 1));
                            }
                            else
                            {
                                Console.WriteLine($"Max failures reached");
                            }
                        }
                        else if (legacyResponse.Error != null)
                        {
                            throw new Exception($"ERROR: {legacyResponse.Error.Message}");
                        }
                        else
                        {
                            throw new Exception("Something went wrong");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"EXCEPTION: {ex}");

                    response.Add(emptyResponse);
                }
            }

            // Only measure latency of successful calls
            return (response.ToArray(), totalLatency / count);
        }
    }
}