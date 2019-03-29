using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SentimentBot.TextAnalytics
{
    public interface ITextAnalyticsService
    {
          Task<string> Sentiment(string text);
    }
}
