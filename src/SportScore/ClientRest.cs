using Newtonsoft.Json;
using RestSharp;
using SportScore.Builders;

namespace SportScore
{
    internal sealed class ClientRest : IDisposable
    {
        private readonly IRestClient _restClient = new RestClient(BaseOption.BaseUrl);

        public void Dispose()
        {
            _restClient.Dispose();
        }

        public async Task<ApiResponse<T>> SendGetRequest<T>(
           string address,
           Dictionary<string, string>? queryArguments = null,
           CancellationToken cancellationToken = default) where T : class
        {
            var restRequest = new RequestBuilder()
          .WithResource(address)
          .WithQueryArguments(queryArguments)
          .Build();

            var response = await _restClient.ExecuteAsync(restRequest, cancellationToken: cancellationToken);

            return JsonConvert.DeserializeObject<ApiResponse<T>>(response.Content);
        }
    }
}
