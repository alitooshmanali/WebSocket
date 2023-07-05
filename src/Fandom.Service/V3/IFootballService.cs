using Fandom.Service.V3.Models;

namespace Fandom.Service.V3
{
    public interface IFootballService
    {
        Task<ApiResponse<List<Country>>> GetAllCountry();

        Task<ApiResponse<List<Country>>> GetCountryByCode(string code);

        Task<ApiResponse<List<LeagueResponse>>> GetAllLeague();

        Task<ApiResponse<List<LeagueResponse>>> GetAllLeague(LeagueParam param);

        Task<ApiResponse<List<TeamResponse>>> GetAllTeam(TeamParam param);

        Task<ApiResponse<List<Venue>>> GetAllVenue(VenueParam param);
    }
}
