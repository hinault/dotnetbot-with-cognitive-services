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
using Newtonsoft.Json;

namespace SentimentBot.TextAnalytics
{
    public class TextAnalyticsService : ITextAnalyticsService
    {

        public ITextAnalyticsClient TextAnalytics;

        public TextAnalyticsService(BotConfiguration botConfiguration)
        {
            var textAnalyticsConfig = botConfiguration.Services.Where(x => x.Name == "sentiment")?.FirstOrDefault();

            var subscriptionKey = textAnalyticsConfig.Properties.SelectToken("subscriptionKey").ToString();

            var endPoint = textAnalyticsConfig.Properties.SelectToken("endpoint").ToString();

            TextAnalytics = new TextAnalyticsClient(new ApiKeyServiceClientCredentials(subscriptionKey))
            {
                Endpoint = endPoint,
            };
        }


        public async Task<string> Sentiment(string text)
        {

            //Get the sentiment
            var result = await TextAnalytics.SentimentAsync(multiLanguageBatchInput:new MultiLanguageBatchInput(
                                                        new List<MultiLanguageInput>()
                                                        {
                                                          new MultiLanguageInput("fr", "0", text)
                                                      }));

           return result.Documents?[0].Score?.ToString();
        }

    }


  
}
