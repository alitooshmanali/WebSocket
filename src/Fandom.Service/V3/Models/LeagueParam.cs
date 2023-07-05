using Fandom.Service.V3.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fandom.Service.V3.Models
{
    public class LeagueParam
    {
        public int? Id { get; set; }

        public string Name { get; set; }

        public LeagueType? Type { get; set; }

        public string Country { get; set; }

        public int? Season { get; set; }

        public bool? Current { get; set; }

        public int? Team { get; set; }

        public string Code { get; set; }

        public int? Result { get; set; }
    }
}
