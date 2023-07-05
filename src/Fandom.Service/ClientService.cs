using Fandom.Service.Builders;
using Newtonsoft.Json;
using RestSharp;

namespace Fandom.Service
{
    public sealed class ClientService : IClientService
    {
        private readonly IRestClient restClient;

        public ClientService()
        {
            this.restClient = new RestClient(BaseApiOption.ApiUrl)
                .AddDefaultHeader("x-apisports-key", BaseApiOption.ApiKey);
        }

        public void Dispose()
        {
            restClient?.Dispose();
        }

        public async Task<ApiResponse<T>> SendGetRequest<T>(
            string address,
            Dictionary<string, string>? queryArguments = null,
            CancellationToken cancellationToken = default)
            where T : class
        {
            var restRequest = new RequestBuilder()
           .WithResource(address)
           .WithQueryArguments(queryArguments)
           .Build();

            var response = await restClient.ExecuteAsync(restRequest, cancellationToken: cancellationToken);

            return JsonConvert.DeserializeObject<ApiResponse<T>>(response.Content);
        }
    }
}
