using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics;
using Microsoft.Azure.CognitiveServices.Language.TextAnalytics.Models;

namespace SentimentBot
{
    public class TextAnalyticsService
    {

        public ITextAnalyticsClient TextAnalytics;

        public TextAnalyticsService()
        {
            TextAnalytics = new TextAnalyticsClient()
        }

    }
}
