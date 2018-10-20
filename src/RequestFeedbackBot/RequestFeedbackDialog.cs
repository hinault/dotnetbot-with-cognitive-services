using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text;

namespace RequestFeedbackBot
{
    public class RequestFeedbackDialog : IBot
    {
        private readonly BotAccessors _accessors;
        private readonly ILogger _logger;
        private DialogSet _dialogs;

        public RequestFeedbackDialog(BotAccessors accessors, ILoggerFactory loggerFactory)
        {

            _accessors = accessors ?? throw new ArgumentNullException(nameof(accessors));

            if (loggerFactory == null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            //Création des traces pour le dialogue RequestFeedbackDialog
            _logger = loggerFactory.CreateLogger<RequestFeedbackDialog>();
            _logger.LogTrace("RequestFeedbackDialogBot turn start.");

            //Initialisation du dialoge. Ce dernier à besoin DialogState qui sera appelé dans le Turn Context.
            _dialogs = new DialogSet(accessors.ConversationDialogState);

            //Création d'un tableau, avec les differentes fonctions qui seront éxecutée en cascade.
            var waterfallSteps = new WaterfallStep[]
            {
                RequestStepAsync,
                NameStepAsync,
                NameConfirmStepAsync,
                EmailConfirmStepAsync,
                SummaryStepAsync,
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            _dialogs.Add(new WaterfallDialog("details", waterfallSteps));
            _dialogs.Add(new ConfirmPrompt("confirm",defaultLocale:Culture.French));
            _dialogs.Add(new TextPrompt("nom"));
            _dialogs.Add(new TextPrompt("email", EmailPromptValidatorAsync));
            _dialogs.Add(new TextPrompt("message"));
        }

        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

           // turnContext.Activity.Locale = Culture.French;

            var dialogContext = await _dialogs.CreateContextAsync(turnContext, cancellationToken);
            var results = await dialogContext.ContinueDialogAsync(cancellationToken);

            if (turnContext.Activity.Type == ActivityTypes.Message)
            {

                //If the DialogTurnStatus is Empty we should start a new dialog.
                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dialogContext.BeginDialogAsync("details", null, cancellationToken);
                }
            }
            else if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                if (turnContext.Activity.MembersAdded.Any())
                {
                    foreach (var member in turnContext.Activity.MembersAdded)
                    {
                        if (member.Id != turnContext.Activity.Recipient.Id)
                        {
                            // Envois du message de bienvenue
                            await SendWelcomeMessageAsync(turnContext, cancellationToken);

                            // If the DialogTurnStatus is Empty we should start a new dialog.
                            if (results.Status == DialogTurnStatus.Empty)
                            {
                                await dialogContext.BeginDialogAsync("details", null, cancellationToken);
                            }
                        }
                    }
                }
            }
            else
            {
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected", cancellationToken: cancellationToken);
            }

            // Save the dialog state into the conversation state.
            await _accessors.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);

            // Save the user profile updates into the user state.
            await _accessors.UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        private static async Task SendWelcomeMessageAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            
                    await turnContext.SendActivityAsync(
                "Bienvenue. Ce bot permet de recueillir vos avis.",
                cancellationToken: cancellationToken);
             
        }

        private async Task<DialogTurnResult> RequestStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            return await stepContext.PromptAsync("confirm", new PromptOptions { Prompt = MessageFactory.Text("Voulez-vous dddd?") }, cancellationToken);
        }


        private async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
                // Running a prompt here means the next WaterfallStep will be run when the users response is received.
                return await stepContext.PromptAsync("nom", new PromptOptions { Prompt = MessageFactory.Text("Veuillez saisir votre nom ?") }, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Merci"), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);

            }
        }

        private async Task<DialogTurnResult> NameConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the current profile object from user state.
            var feedbackData = await _accessors.FeedbackData.GetAsync(stepContext.Context, () => new FeedbackData(), cancellationToken);

            // Update the profile.
            feedbackData.Name = (string)stepContext.Result;

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            return await stepContext.PromptAsync("email", new PromptOptions { Prompt = MessageFactory.Text("Veuillez saisir votre adresse email ?") }, cancellationToken);
        }


        private async Task<DialogTurnResult> EmailConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the current profile object from user state.
            var feedbackData = await _accessors.FeedbackData.GetAsync(stepContext.Context, () => new FeedbackData(), cancellationToken);

            // Update the profile.
            feedbackData.Email = (string)stepContext.Result;

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            return await stepContext.PromptAsync("message", new PromptOptions { Prompt = MessageFactory.Text("Veuillez saisir votre message ?") }, cancellationToken);
        }


        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            // Get the current profile object from user state.
            var feedbackData = await _accessors.FeedbackData.GetAsync(stepContext.Context, () => new FeedbackData(), cancellationToken);

            // Update the profile.
            feedbackData.Message = (string)stepContext.Result;

            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Votre nom {feedbackData.Name}, votre email {feedbackData.Email} et votre message {feedbackData.Message}."), cancellationToken);

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog, here it is the end.
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private bool IsValidEmail(string source)
        {
            return new EmailAddressAttribute().IsValid(source);
        }

        private Task<bool> EmailPromptValidatorAsync(PromptValidatorContext<string> promptContext, CancellationToken cancellationToken)
        {
            var result = promptContext.Recognized.Value;

            // This condition is our validation rule.
            if (IsValidEmail(result))
            {
                // Success is indicated by passing back the value the Prompt has collected. You must pass back a value even if you haven't changed it.
                return Task.FromResult(true);
            }

            // Not calling End indicates validation failure. This will trigger a RetryPrompt if one has been defined.

            // Note you are free to do async IO from within a validator. Here we had no need so just complete.
            return Task.FromResult(false);
        }
    }
}
