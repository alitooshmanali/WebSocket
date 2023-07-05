namespace Fandom.Service.V3.Models
{
    public class LeagueResponse
    {
        public League League { get; set; }

        public Country Country { get; set; }

        public List<Season> Seasons { get; set; }
    }
}
