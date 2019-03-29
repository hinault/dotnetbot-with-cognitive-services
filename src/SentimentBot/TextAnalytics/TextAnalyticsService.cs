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



namespace SentimentBot.TextAnalytics
{
    public class TextAnalyticsService : ITextAnalyticsService
    {

        public ITextAnalyticsClient TextAnalytics;

        public TextAnalyticsService(BotConfiguration botConfiguration)
        {
            var textAnalyticsConfig = botConfiguration.Services.Where(x => x.Name == "textanalytics");


            TextAnalytics = new TextAnalyticsClient(new ApiKeyServiceClientCredentials("f158c058756e4253a8aa4592a46e1888"))
            {
                Endpoint = "https://canadacentral.api.cognitive.microsoft.com",
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
