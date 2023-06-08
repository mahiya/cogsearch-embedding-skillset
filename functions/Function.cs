using Azure;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Function
{
    public class Function
    {
        readonly List<OpenAIServiceAccount> _openAIServiceAccounts;
        int _openAIServiceAccountIndex = 0;

        public Function(FunctionConfiguration config)
        {
            _openAIServiceAccounts = JsonConvert.DeserializeObject<List<OpenAIServiceAccount>>(config.OpenAIServiceAccounts);
        }

        [FunctionName(nameof(Function))]
        public async Task<IActionResult> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req,
            ILogger log)
        {
            // Azure Cognitive Search からの入力を読み込む
            var body = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogInformation("Input from Azure Cognitive Search Skill Set:");
            log.LogInformation(body);

            // Azure Cognitive Search からの入力(JSON)を解析する
            var inputValues = JsonConvert.DeserializeObject<CustomSkillRequest>(body).Values;

            // 出力を生成する何かしらの処理を行う (レコードIDごとに)
            var outputValues = new List<OutputValue>();
            foreach (var inputValue in inputValues)
            {
                var embeddings = await GetEmbeddingsAsync(inputValue.Data.Input, log);
                var outputValue = new OutputValue
                {
                    RecordId = inputValue.RecordId,
                    Data = new OutputsData { Output = embeddings }
                };
                outputValues.Add(outputValue);
            }

            // 処理結果を Output として返す
            return new OkObjectResult(new CustomSkillResponse { Values = outputValues });
        }

        async Task<float[]> GetEmbeddingsAsync(string text, ILogger log)
        {
            var tryCount = 0;
            while (true)
            {
                try
                {
                    var account = _openAIServiceAccounts[_openAIServiceAccountIndex++ % _openAIServiceAccounts.Count];
                    log.LogInformation($"Try: {tryCount}, Account: {account.Name}, {account.Key}, {account.DeployName}");
                    return await GetEmbeddingsAsync(account, text);
                }
                catch (RequestFailedException e)
                {
                    if (e.Status != 429) throw e;
                    await Task.Delay(250);
                }
                
                if (++tryCount > _openAIServiceAccounts.Count * 5) 
                    throw new Exception("Could not get embeddings using Azure OpenAI Service.");
            }
        }

        async Task<float[]> GetEmbeddingsAsync(OpenAIServiceAccount account, string text)
        {
            var client = new OpenAIClient(new Uri($"https://{account.Name}.openai.azure.com/"), new AzureKeyCredential(account.Key));
            var resp = await client.GetEmbeddingsAsync(account.DeployName, new EmbeddingsOptions(text));
            var embeddings = resp.Value.Data[0].Embedding.ToArray();
            return embeddings;
        }
    }
}
