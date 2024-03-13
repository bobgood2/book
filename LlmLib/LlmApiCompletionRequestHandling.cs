namespace LuToolbox.Common
{
    using Microsoft.Identity.Client;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    public class LlmApiCompletionRequestHandling
    {
        const string DeviceCodeFile = @"c:\Users\bobgood\OneDrive - Microsoft\DeviceCodeRefresh.html";
        const string DeviceCodeStatus = @"c:\Users\bobgood\OneDrive - Microsoft\DeviceCodeStatus.html";

        const string Endpoint = "https://fe-26.qas.bing.net/sdf/completions";
        //const string Endpoint = "https://httpqas26-frontend-qasazap-prod-dsm02p.qas.binginternal.com/completions";
        const string AuthTokenRefreshInProgress = "AuthTokenRefreshInProgress";

        private static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

        public static IEnumerable<string> Scopes =
            new List<string>() {
                "api://68df66a4-cad9-4bfd-872b-c6ddde00d6b2/access"
            };

        public static IPublicClientApplication app = PublicClientApplicationBuilder.Create("68df66a4-cad9-4bfd-872b-c6ddde00d6b2")
            .WithAuthority("https://login.microsoftonline.com/72f988bf-86f1-41af-91ab-2d7cd011db47")
            .Build();

        public static async Task<string> GetToken()
        {
            AuthenticationResult result = null;
            await semaphoreSlim.WaitAsync();

            try
            {
                var accounts = await app.GetAccountsAsync();

                if (accounts.Any())
                {
                    var chosen = accounts.First();
                    result = await app.AcquireTokenSilent(Scopes, chosen).ExecuteAsync();
                }

                if (result == null)
                {
                    result = await app.AcquireTokenWithDeviceCode(Scopes,
                        deviceCodeResult =>
                        {
                            // This will print the message on the console which tells the user where to go sign-in using
                            // a separate browser and the code to enter once they sign in.
                            // The AcquireTokenWithDeviceCode() method will poll the server after firing this
                            // device code callback to look for the successful login of the user via that browser.
                            // This background polling (whose interval and timeout data is also provided as fields in the
                            // deviceCodeCallback class) will occur until:
                            // * The user has successfully logged in via browser and entered the proper code
                            // * The timeout specified by the server for the lifetime of this code (typically ~15 minutes) has been reached
                            // * The developing application calls the Cancel() method on a CancellationToken sent into the method.
                            //   If this occurs, an OperationCanceledException will be thrown (see catch below for more details).
                            Console.WriteLine(deviceCodeResult.Message);

                            try
                            {
                                File.WriteAllText(DeviceCodeFile, $"<html><body><h1><a href=\"{deviceCodeResult.VerificationUrl}\">{deviceCodeResult.VerificationUrl}</a></h1><h1>{deviceCodeResult.UserCode}</h1></body></html>");
                            }
                            catch { }

                            return Task.FromResult(0);
                        }).ExecuteAsync();

                }

                try
                {
                    var encodedToken = new Microsoft.IdentityModel.JsonWebTokens.JsonWebToken(result.AccessToken);
                    var decodedPayload = JObject.Parse(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encodedToken.EncodedPayload)));
                    decodedPayload.Add("StartTime", DateTimeOffset.FromUnixTimeSeconds(decodedPayload.Value<int>("iat")).DateTime.ToString());
                    decodedPayload.Add("ExpireTime", DateTimeOffset.FromUnixTimeSeconds(decodedPayload.Value<int>("exp")).DateTime.ToString());

                    Console.WriteLine($"Token start time: {DateTimeOffset.FromUnixTimeSeconds(decodedPayload.Value<int>("iat")).DateTime}");
                    Console.WriteLine($"Token expire time: {DateTimeOffset.FromUnixTimeSeconds(decodedPayload.Value<int>("exp")).DateTime}");

                    File.WriteAllText("AuthTokenCacheDebug", decodedPayload.ToString(Formatting.Indented));
                    File.WriteAllText(DeviceCodeStatus, $"<html><body><pre>{decodedPayload.ToString(Formatting.Indented)}</pre></body></html>");
                }
                catch (Exception ex)
                {
                    //Console.WriteLine("Failed to write token debug info");
                    //Console.WriteLine(ex.ToString());
                }
            }
            finally
            {
                try
                {
                    File.Delete(DeviceCodeFile);
                }
                catch { }

                semaphoreSlim.Release();
            }

            return result.AccessToken;
        }

        public static void RefreshAuthToken(bool useAuthCache = true)
        {
            var token = string.Empty;

            if (useAuthCache && File.Exists("AuthTokenCache"))
            {
                token = File.ReadAllText("AuthTokenCache");

                try
                {
                    var result = SendRequest("dev-ppo", new LlmRequest(), 1, true).Result;
                }
                catch (Exception ex)
                {
                }
            }

            if (string.IsNullOrEmpty(token))
            {
                token = GetToken().Result;
                File.WriteAllText("AuthTokenCache", token);
            }
        }

        public static async Task<(HttpResponseMessage Response, long LatencyInMilliseconds)> SendRequest(string modelType, string requestData, bool useAuthCache = true)
        {
            var token = string.Empty;

            if (useAuthCache && File.Exists("AuthTokenCache"))
            {
                token = File.ReadAllText("AuthTokenCache");
            }

            if (string.IsNullOrEmpty(token))
            {
                token = await GetToken();

                File.WriteAllText("AuthTokenCache", token);
            }

            var httpClient = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Post, Endpoint);
            request.Content = new StringContent(requestData, Encoding.UTF8, "application/json");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            request.Headers.Add("X-ModelType", modelType);

            // Scenario GUID for "Prompt-Technique Experimentation"
            // request.Headers.Add("X-ScenarioGUID", "0d160540-b402-4d85-a737-da74093b8600");

            // Scenario GUID for "Search - People"
            // request.Headers.Add("X-ScenarioGUID", "8c87be22-a80f-437e-a453-cc696452ae12");

            // Scenario GUID for "EnterpriseSydney"
            request.Headers.Add("X-ScenarioGUID", "fb8d773d-7ef8-4ec0-a117-179f88add510");

            var latencyStopwatch = Stopwatch.StartNew();
            var httpResponse = await httpClient.SendAsync(request);
            latencyStopwatch.Stop();

            if (httpResponse.StatusCode == System.Net.HttpStatusCode.Unauthorized && useAuthCache)
            {
                return await SendRequest(modelType, requestData, false);
            }

            return (httpResponse, latencyStopwatch.ElapsedMilliseconds);
        }

        public static LlmRequest CreateRequest(string prompt, string stop, int maxTokens = 300, double temperature = 0, int completionCount = 1)
        {
            return new LlmRequest
            {
                Prompt = new List<string>() { prompt },
                MaxTokens = maxTokens,
                Temperature = temperature,
                TopP = 1,
                N = completionCount,
                Stream = false,
                Stop = stop.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(item => item.Trim()).ToList()
            };
        }

        public static LlmRequest CreateRequest(List<string> prompt, string stop, int maxTokens = 300, double temperature = 0, int completionCount = 1)
        {
            return new LlmRequest
            {
                Prompt = prompt,
                MaxTokens = maxTokens,
                Temperature = temperature,
                TopP = 1,
                N = completionCount,
                Stream = false,
                Stop = stop.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(item => item.Trim()).ToList(),
                PresencePenalty = 0
            };
        }

        public async static Task<LlmResponse[]> SendRequest(string modelName, LlmRequest prompt, int maxRetries, bool supressErrors = false)
        {
            return (await SendRequestWithLatency(modelName, prompt, maxRetries, supressErrors)).Response;
        }

        public async static Task<(LlmResponse[] Response, long LatencyInMilliseconds)> SendRequestWithLatency(string modelName, LlmRequest prompt, int maxRetries, bool supressErrors = false)
        {
            var requestData = JsonConvert.SerializeObject(prompt);

            //"LLM API: Throttling enforced. Request for scenario GUID Default and scenario name Default was throttled. Policy limit was exceeded for scope User. Retry after 0 seconds"
            int count = 0;

            while (true)
            {
                if (count >= maxRetries)
                {
                    Console.WriteLine("max retries of:" + maxRetries.ToString() + " reached!");
                    return (null, 0);
                }

                try
                {
                    var response = await SendRequest(modelName, requestData);

                    var responseContent = response.Response.Content.ReadAsStringAsync().Result;

                    try
                    {
                        if (response.Response.StatusCode == (HttpStatusCode)429)
                        {
                            var retryAfterMs =
                                response.Response.Headers.TryGetValues("retry-after-ms", out var o_retryAfterMs) ?
                                int.Parse(o_retryAfterMs.First()) :
                                response.Response.Headers.TryGetValues("retry-after", out var o_retryAfter) ?
                                int.Parse(o_retryAfter.First()) * 1000 :
                                60000
                                ;

                            var headersToRetrieve = new string[]
                            {
                                "ms-azureml-model-error-reason",
                                "azure-openai-deployment-utilization",
                                "x-ms-region",
                                "x-ratelimit-remaining-requests",
                                "x-ratelimit-remaining-tokens",
                                "x-ms-throttle-scope",
                            };

                            var headersToDisplay = new List<string>();
                            
                            foreach(var header in headersToRetrieve)
                            {
                                if(response.Response.Headers.TryGetValues(header, out var headerValue))
                                {
                                    if(headerValue.Count() > 1)
                                    {
                                        headersToDisplay.Add($"{header}: [{string.Join(", ", headerValue)}]");
                                    }
                                    else
                                    {
                                        headersToDisplay.Add($"{header}: {headerValue.First()}");
                                    }
                                }
                            }

                            Console.WriteLine($"Throttled, sleeping for {retryAfterMs}ms. " + string.Join(", ", headersToDisplay));

                            Thread.Sleep(retryAfterMs);
                            continue;
                        }
                        else if (responseContent.Contains("The service is temporarily unable to process your request. Please try again later"))
                        {
                            Console.WriteLine("Service temporarily unavailable, trying again in 1 minute");
                            Thread.Sleep(60000);
                            continue;
                        }
                        else
                        {
                            try
                            {
                                var deserializedResponse = JsonConvert.DeserializeObject<LlmResponse>(responseContent);

                                if (deserializedResponse.Error != null)
                                {
                                    if (!supressErrors)
                                    {
                                        Console.WriteLine($"LLM API ERROR: {deserializedResponse.Error.Message}");
                                    }

                                    if (deserializedResponse.Error.Message.Contains("overloaded with other requests") && deserializedResponse.Error.Message.Contains("You can retry your request"))
                                    {
                                        Console.WriteLine("Waiting 15 seconds and trying again...");

                                        // Sleep 15 seconds
                                        Thread.Sleep(15000);

                                        count++;
                                        continue;
                                    }

                                    return (null, 0);
                                }

                                if (prompt.Prompt?.Count > 1)
                                {
                                    if (deserializedResponse.Choices.Count != prompt.Prompt.Count * prompt.N)
                                    {
                                        if (!supressErrors)
                                        {
                                            Console.WriteLine($"Unexpected number of choices in LLM response. Got {deserializedResponse.Choices.Count} expected {prompt.Prompt.Count * prompt.N}");
                                        }
                                        return (null, 0);
                                    }

                                    List<LlmResponse> responses = new List<LlmResponse>();
                                    for (int i = 0; i < prompt.Prompt.Count; i++)
                                    {
                                        var r = new LlmResponse();
                                        r.Object = deserializedResponse.Object;
                                        r.Model = deserializedResponse.Model;
                                        r.Created = deserializedResponse.Created;
                                        r.Id = deserializedResponse.Id;
                                        r.Choices = new List<Choice>();
                                        r.Usage = deserializedResponse.Usage;

                                        for (int j = 0; j < prompt.N; j++)
                                        {
                                            r.Choices.Add(deserializedResponse.Choices[i * prompt.N + j]);
                                        }

                                        responses.Add(r);
                                    }

                                    return (responses.ToArray(), response.LatencyInMilliseconds);
                                }
                                else
                                {
                                    return (new[] { deserializedResponse }, response.LatencyInMilliseconds);
                                }
                            }
                            catch (Exception ex)
                            {
                                throw ex;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("retrying...");
                }

                count++;
            }
        }
    }
}