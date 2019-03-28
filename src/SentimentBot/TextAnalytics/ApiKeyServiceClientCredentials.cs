using Microsoft.Rest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SentimentBot.TextAnalytics
{
    public class ApiKeyServiceClientCredentials : ServiceClientCredentials
    {
        private string _apiKey;

        public ApiKeyServiceClientCredentials(string apikey)
        {

            this._apiKey = apikey;
        }


        public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);
            return base.ProcessHttpRequestAsync(request, cancellationToken);
        }
    }
}
