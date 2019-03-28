using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;
using Microsoft.Bot.Configuration;
using Microsoft.Rest;



namespace SentimentBot
{
    public class TextAnalyticsService
    {

        public ITextAnalyticsClient TextAnalytics;

        public TextAnalyticsService(BotConfiguration botConfiguration)
        {
            var textAnalyticsConfig = botConfiguration.Services.Where(x => x.Name == "textanalytics");


            TextAnalytics = new TextAnalyticsClient(new ApiKeyServiceClientCredentials());
        }

    }


    class ApiKeyServiceClientCredentials : ServiceClientCredentials
    {
        public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add("Ocp-Apim-Subscription-Key", "");
            return base.ProcessHttpRequestAsync(request, cancellationToken);
        }
    }
}
