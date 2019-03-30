using Microsoft.Bot.Configuration;
using Microsoft.Bot.Configuration.Encryption;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SentimentBot.TextAnalytics
{
    public class TextAnalyticsConfig: ConnectedService
    {
        private string _hostname;

        /// <summary>
        /// Initializes a new instance of the <see cref="QnAMakerService"/> class.
        /// </summary>
        public TextAnalyticsConfig()
            : base("textanalytics")
        {
        }


        /// <summary>
        /// Gets or sets subscriptionKey.
        /// </summary>
        [JsonProperty("subscriptionKey")]
        public string SubscriptionKey { get; set; }

        
        /// <summary>
        /// Gets or sets endpointKey.
        /// </summary>
        [JsonProperty("endpoint")]
        public string Endpoint{ get; set; }

        // <inheritdoc/>
        public override void Encrypt(string secret)
        {
            base.Encrypt(secret);

            if (!string.IsNullOrEmpty(this.Endpoint))
            {
                this.Endpoint = this.Endpoint.Encrypt(secret);
            }

            if (!string.IsNullOrEmpty(this.SubscriptionKey))
            {
                this.SubscriptionKey = this.SubscriptionKey.Encrypt(secret);
            }
        }

        /// <inheritdoc/>
        public override void Decrypt(string secret)
        {
            base.Decrypt(secret);

            if (!string.IsNullOrEmpty(this.Endpoint))
            {
                this.Endpoint = this.Endpoint.Decrypt(secret);
            }

            if (!string.IsNullOrEmpty(this.SubscriptionKey))
            {
                this.SubscriptionKey = this.SubscriptionKey.Decrypt(secret);
            }
        }
    }

}
