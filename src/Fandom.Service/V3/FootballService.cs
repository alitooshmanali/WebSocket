using Fandom.Service.V3.Models;

namespace Fandom.Service.V3
{
    public class FootballService : IFootballService
    {
        private readonly IClientService clientService;

        public FootballService(IClientService clientService)
        {
            this.clientService = clientService;
        }

        public async Task<ApiResponse<List<Country>>> GetAllCountry()
        {
            return await clientService.SendGetRequest<List<Country>>("/countries");
        }

        public async Task<ApiResponse<List<LeagueResponse>>> GetAllLeague()
        {
            return await clientService.SendGetRequest<List<LeagueResponse>>("leagues");
        }

        public async Task<ApiResponse<List<LeagueResponse>>> GetAllLeague(LeagueParam? param)
        {
            var queryParams = param
                .GetType().GetProperties()
                .Where(i=> i.GetValue(param) is not null) 
                .ToDictionary(x => x.Name.ToLower(), x => x.GetValue(param)?.ToString() ?? "");

            return await clientService.SendGetRequest<List<LeagueResponse>>("leagues", queryParams);
        }

        public async Task<ApiResponse<List<TeamResponse>>> GetAllTeam(TeamParam? param)
        {
            var queryParams = param
                .GetType().GetProperties()
                .Where(i => i.GetValue(param) is not null)
                .ToDictionary(x => x.Name.ToLower(), x => x.GetValue(param)?.ToString() ?? "");

            return await clientService.SendGetRequest<List<TeamResponse>>("teams", queryParams);
        }

        public async Task<ApiResponse<List<Venue>>> GetAllVenue(VenueParam param)
        {
            var queryParams = param
                .GetType().GetProperties()
                .Where(i => i.GetValue(param) is not null)
                .ToDictionary(x => x.Name.ToLower(), x => x.GetValue(param)?.ToString() ?? "");

            return await clientService.SendGetRequest<List<Venue>>("venues", queryParams);
        }

        public async Task<ApiResponse<List<Country>>> GetCountryByCode(string code)
        {
            var queryParams = new Dictionary<string, string>();
            queryParams.Add("code", code);

            return await clientService.SendGetRequest<List<Country>>($"/countries", queryParams);
        }
    }
}
