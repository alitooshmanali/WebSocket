namespace Fandom.Service.V3.Models
{
    public class Coverage
    {
        public bool Standings { get; set; }

        public bool Players { get; set; }

        public bool TopScores { get; set; }

        public bool TopAssists { get; set; }

        public bool TopCards { get; set; }

        public bool Injuries { get; set; }

        public bool Predictions { get; set; }

        public bool Odds { get; set; }

        public Fixture Fixture { get; set; }
    }
}
