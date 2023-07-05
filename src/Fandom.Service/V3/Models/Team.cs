namespace Fandom.Service.V3.Models
{
    public class Team
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Code { get; set; }

        public string Country { get; set; }

        public int Founded { get; set; }

        public bool National { get; set; }

        public string Logo { get; set; }
    }
}
