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
    public class TextAnalyticsService
    {

        public ITextAnalyticsClient TextAnalytics;

        public TextAnalyticsService(BotConfiguration botConfiguration)
        {
            var textAnalyticsConfig = botConfiguration.Services.Where(x => x.Name == "textanalytics");


            TextAnalytics = new TextAnalyticsClient(new ApiKeyServiceClientCredentials())
            {
                Endpoint = "https://canadacentral.api.cognitive.microsoft.com",
            };
        }


        public string Sentiment(string text)
        {


            var result = TextAnalytics.SentimentAsync(true,
                                      new MultiLanguageBatchInput(
                                                        new List<MultiLanguageInput>()
                                                        {
                                                          new MultiLanguageInput("fr", "0", text)
                                                      })).Result;

           return result.Documents?[0].Score?.ToString("#.#");
        }

    }


  
}
