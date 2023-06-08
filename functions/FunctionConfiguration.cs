using Microsoft.Extensions.Configuration;

namespace Function
{
    public class FunctionConfiguration
    {
        public readonly string OpenAIServiceAccounts;

        public FunctionConfiguration(IConfiguration config)
        {
            OpenAIServiceAccounts = config["OPENAI_SERVICE_ACCOUNTS"];
        }
    }
}
