using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpToken;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.IO;
using LuToolbox.Models;

namespace LuToolbox.Common
{
    public static class LlmHelper
    {
        public const string ChatStart = "<|im_start|>";
        public const string ChatStartSystem = ChatStart + "system\n";
        public const string ChatStartUser = ChatStart + "user\n";
        public const string ChatStartAssistant = ChatStart + "assistant\n";
        public const string ChatEnd = "<|im_end|>\n";

        public static int GetGptTokenCount(string text, string model)
        {
            GptEncoding encoder = null;

            switch (model)
            {
                case "text-davinci-001":
                    encoder = GptEncoding.GetEncoding("r50k_base");
                    break;

                case "text-davinci-002":
                case "text-davinci-003":
                    encoder = GptEncoding.GetEncoding("p50k_base");
                    break;

                default:
                    encoder = GptEncoding.GetEncoding("cl100k_base");
                    break;
            }

            var encoding = encoder.Encode(text);

            return encoding.Count;
        }

        public static Dictionary<string, string> ProcessChatMlResponses(string assistantResponses, string type = "assistant")
        {
            return
                assistantResponses
                .Trim()
                .Split($"[{type}]", StringSplitOptions.RemoveEmptyEntries)
                .Select(item =>
                {
                    var match = Regex.Match(item, "\\(#(.*?)\\)(.*?)$", RegexOptions.Singleline);
                    if (match.Success)
                    {
                        return (Key: match.Groups[1].Value.Trim().Replace("_", "").ToLowerInvariant(), Value: match.Groups[2].Value.Trim());
                    }
                    else
                    {
                        return (Key: string.Empty, Value: item.Trim());
                    }
                })
                .GroupBy(item => item.Key)
                .ToDictionary(item => item.Key, item => item.First().Value);
        }

        public static string NormalizePrompt(string promptContents)
        {
            return promptContents.Replace("\r\n", " ").Replace("\n", " ");
        }

        /// <summary>
        /// Removes trailing <|im_end|> and <|im_sep|> from end of text
        /// </summary>
        public static string RemoveStopSequencesFromEnd(string text)
        {
            return Regex.Replace(text, "(.*?)<\\|im_(end|sep)\\|>$", "$1", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Converts a LLM prompt that contains ChatML (i.e. <|im_*|> tags) to a chat completion message
        /// </summary>
        /// <param name="prompt"></param>
        /// <returns></returns>
        public static List<ChatCompletionMessage> ConvertPromptToMessages(string prompt)
        {
            var result = new List<ChatCompletionMessage>();

            foreach (var match in Regex.Matches(prompt, "<\\|im_start\\|>(?<role>system|user|assistant)(?<content>.*?)<\\|im_end\\|>", RegexOptions.Singleline).Cast<Match>())
            {
                result.Add(
                    new ChatCompletionMessage()
                    {
                        Content = match.Groups["content"].Value,
                        Role = match.Groups["role"].Value,
                    });
            }

            return result;
        }

        static ConcurrentBag<DateTime> requestTimings = new ConcurrentBag<DateTime>();

        public static List<string> ExecuteLlmPromptsInParallel(
            List<string> promptsToExecute,
            int batchSize,
            int maximumConcurrency,
            string modelName,
            string stopSequence,
            int maxTokens,
            double temperature,
            int completionCount,
            LlmServiceType llmServiceType,
            string llmServiceAuthToken = "",
            LlmServiceAuthType llmServiceAuthType = LlmServiceAuthType.Bearer,
            string llmServiceEndpoint = "",
            StreamWriter progressiveOutput = null,
            bool outputProgress = false,
            bool refreshAuthToken = true,
            int retryCount = 10
            )
        {
            var results = new ConcurrentBag<(int Index, string Result)>();
            var resultsProgressive = new ConcurrentQueue<(int Index, string Result)>();
            var progressMessages = new ConcurrentQueue<string>();
            int totalRequestsSent = 0;
            int totalRequestsCompleted = 0;
            int currentlyRunning = 0;
            int progressCount = 0;

            // Refreshing auth token
            if (llmServiceType == LlmServiceType.LlmApi_Completion && refreshAuthToken)
            {
                while (requestTimings.Count(requestTime => requestTime >= DateTime.Now - TimeSpan.FromSeconds(65)) >= maximumConcurrency)
                {
                    Thread.Sleep(50);
                }

                Console.Write("Refreshing auth token...");
                LlmApiCompletionRequestHandling.RefreshAuthToken();
                Console.WriteLine("...refreshed");
            }

            var batches =
                promptsToExecute
                .Select((query, index) => (index / batchSize, index, query))
                .GroupBy(item => item.Item1);

            foreach (var batch in batches)
            {
                // LLM API throttles to 5 requests per minute, so need custom logic to ensure we don't exceed that
                var cutoff = DateTime.Now - TimeSpan.FromSeconds(650);
                while (llmServiceType == LlmServiceType.LlmApi_Completion && (currentlyRunning >= maximumConcurrency * 2 || requestTimings.Count(requestTime => requestTime >= cutoff) >= maximumConcurrency))
                {
                    Thread.Sleep(50);

                    cutoff = DateTime.Now - TimeSpan.FromSeconds(650);
                }

                totalRequestsSent++;
                currentlyRunning++;

                // Add current request to timings list to ensure we don't go over MaximumConcurrency requests per minute
                requestTimings.Add(DateTime.Now);

                Task.Run(() =>
                {
                    try
                    {
                        var query = batch.ToList();
                        var prompts = query.Select(item => item.query).ToList();

                        LlmRequest requestData = LlmApiCompletionRequestHandling.CreateRequest(prompts, stopSequence, maxTokens: maxTokens, temperature: temperature, completionCount: completionCount);

                        (LlmResponse[] Response, long LatencyInMilliseconds) response;

                        switch (llmServiceType)
                        {
                            case LlmServiceType.CustomApi_Chat:
                                response = CustomApiChatRequestHandling.SendRequestWithLatency(modelName, requestData, retryCount, llmServiceAuthToken, progressMessages: progressMessages, endpoint: llmServiceEndpoint, authType: llmServiceAuthType).Result;
                                break;

                            case LlmServiceType.CustomApi_Completion:
                                response = CustomApiCompletionRequestHandling.SendRequestWithLatency(modelName, requestData, retryCount, llmServiceAuthToken, endpoint: llmServiceEndpoint, authType: llmServiceAuthType).Result;
                                break;

                            case LlmServiceType.LlmApi_Completion:
                            default:
                                response = LlmApiCompletionRequestHandling.SendRequestWithLatency(modelName, requestData, retryCount).Result;
                                break;
                        }

                        if (response.Response == null)
                        {
                            Console.WriteLine("LLM service returned empty object, skipping this batch.");
                            return;
                        }

                        for (var promptIndex = 0; promptIndex < query.Count(); promptIndex++)
                        {
                            var r = response.Response[promptIndex];

                            var result = (Index: query[promptIndex].index, Result: r.Choices.First().Text);
                            results.Add(result);
                            resultsProgressive.Enqueue(result);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                    finally
                    {
                        Interlocked.Increment(ref totalRequestsCompleted);
                        Interlocked.Decrement(ref currentlyRunning);
                    }
                });

                while ((llmServiceType == LlmServiceType.CustomApi_Completion || llmServiceType == LlmServiceType.CustomApi_Chat) && currentlyRunning >= maximumConcurrency)
                {
                    Thread.Sleep(50);
                }

                if (resultsProgressive.Count > 0)
                {
                    while (resultsProgressive.TryDequeue(out var response))
                    {
                        if (progressiveOutput != null)
                        {
                            progressiveOutput.WriteLine(response.Result);
                            progressiveOutput.Flush();
                        }

                        if (outputProgress)
                        {
                            Console.WriteLine($"Total requests completed: {++progressCount}");
                        }
                    }
                }
            }

            // Don't finish until all threads complete
            while (totalRequestsCompleted < totalRequestsSent)
            {
                Thread.Sleep(10);

                while (progressMessages.TryDequeue(out var message))
                {
                    if (outputProgress)
                    {
                        Console.WriteLine($"{++progressCount}: {message}");
                    }
                }
            }

            return results.OrderBy(item => item.Index).Select(item => item.Result).ToList();
        }
    }
}
