using Newtonsoft.Json;
using RestSharp;
using SportScore.Builders;
using SportScore.Models;

namespace SportScore
{
    public static class SportScoreClient
    {
        private static readonly ClientRest _client = new ClientRest();

        public static async Task<ApiResponse<List<Sport>>> GetAllSports()
        {
            return await _client.SendGetRequest<List<Sport>>("sports");
        }
    }
}
