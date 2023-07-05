using RestSharp;
using System.Text;

namespace Fandom.Service.Builders
{
    internal class RequestBuilder
    {
        private string _resource;

        private Dictionary<string, string> _queries;

        private TimeSpan _timeout = TimeSpan.FromSeconds(3);

        public RequestBuilder WithResource(string value)
        {
            _resource = value;

            return this;
        }

        public RequestBuilder WithQueryArguments(Dictionary<string, string>? queries)
        {
            _queries = queries;
            return this;
        }

        public RequestBuilder WithTimeout(TimeSpan timeSpan)
        {
            _timeout = timeSpan;
            return this;
        }

        public RestRequest Build()
        {
            SetQueries();

            var request = new RestRequest(_resource, Method.Get)
            {
                Timeout = Convert.ToInt32(_timeout.TotalMilliseconds),
            };

            
            return request;
        }

        private void SetQueries()
        {
            if (_queries == null || !_queries.Any())
                return;

            var stringBuilder = new StringBuilder(_resource);

            if (!_resource.EndsWith('?'))
                stringBuilder.Append('?');

            foreach (var query in _queries)
            {
                stringBuilder.Append($"{query.Key}={query.Value}&");
            }

            _resource = stringBuilder.ToString();
        }
    }
}
