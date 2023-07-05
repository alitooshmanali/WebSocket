using Fandom.Service.V3.Models;
using Newtonsoft.Json;

namespace Fandom.Service
{
    public class ApiResponse<T>
    {
        [JsonIgnore]
        public Dictionary<string, string> Parameters { get; set; }

        [JsonIgnore]
        public Dictionary<string, string> Errors { get; set; }

        public string Get { get; set; }

        public int Results { get; set; }

        public Paging Paging { get; set; }

        public T Response { get; set; }
    }
}
