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

        /// <summary>
        /// The <see cref="DialogSet"/> that contains all the Dialogs that can be used at runtime.
        /// </summary>
        private DialogSet _dialogs;

        public RequestFeedbackDialog(BotAccessors accessors, ILoggerFactory loggerFactory)
        {

            _accessors = accessors ?? throw new ArgumentNullException(nameof(accessors));

            if (loggerFactory == null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }


            _logger = loggerFactory.CreateLogger<RequestFeedbackDialog>();
            _logger.LogTrace("RequestFeedbackDialogBot turn start.");

            // The DialogSet needs a DialogState accessor, it will call it when it has a turn context.
            _dialogs = new DialogSet(accessors.ConversationDialogState);

            // This array defines how the Waterfall will execute.
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
            _dialogs.Add(new ConfirmPrompt("confirm", defaultLocale: Culture.French));
            _dialogs.Add(new TextPrompt("nom"));
            _dialogs.Add(new TextPrompt("email", EmailPromptValidatorAsync));
            _dialogs.Add(new TextPrompt("message"));
        }

        /// <summary>
        /// Every conversation turn for our Bot will call this method.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn. </param>
        /// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        /// <seealso cref="BotStateSet"/>
        /// <seealso cref="ConversationState"/>
        /// <seealso cref="IMiddleware"/>
        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            // Run the DialogSet - let the framework identify the current state of the dialog from
            // the dialog stack and figure out what (if any) is the active dialog.
            var dialogContext = await _dialogs.CreateContextAsync(turnContext, cancellationToken);
           
            // Handle Message activity type, which is the main activity type for shown within a conversational interface
            // Processes ConversationUpdate Activities to welcome the user.
            if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                if (turnContext.Activity.MembersAdded.Any())
                {
                    foreach (var member in turnContext.Activity.MembersAdded)
                    {
                        if (member.Id != turnContext.Activity.Recipient.Id)
                        {
                            // Sends a welcome message to the user.
                            await SendWelcomeMessageAsync(turnContext, cancellationToken);
                            // Pushes a new dialog onto the dialog stack.
                            await dialogContext.BeginDialogAsync("details", null, cancellationToken);
                        }
                    }
                }
            }

            // Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
            // see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types
            else if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                // Continues execution of the active dialog, if there is one
               var results = await dialogContext.ContinueDialogAsync(cancellationToken);

                // If the DialogTurnStatus is Empty we should start a new dialog.
                if(results.Status == DialogTurnStatus.Empty)
                {
                    await dialogContext.BeginDialogAsync("details", null, cancellationToken);
                }
            }

            // Save the dialog state into the conversation state.
            await _accessors.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);

            // Save the  FeedbackData updates into the user state.
            await _accessors.UserState.SaveChangesAsync(turnContext, false, cancellationToken);
        }

        private static async Task SendWelcomeMessageAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {

            await turnContext.SendActivityAsync(
        "Bienvenue. Ce bot permet de recueillir vos avis.",
        cancellationToken: cancellationToken);

        }

        /// <summary>
        /// One of the functions that make up the <see cref="WaterfallDialog"/>.
        /// </summary>
        /// <param name="stepContext">The <see cref="WaterfallStepContext"/> gives access to the executing dialog runtime.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
        /// <returns>A <see cref="DialogTurnResult"/> to communicate some flow control back to the containing WaterfallDialog.</returns>
        private async Task<DialogTurnResult> RequestStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            return await stepContext.PromptAsync("confirm", new PromptOptions { Prompt = MessageFactory.Text("Voulez-vous nous laisser vos commentaires ?") }, cancellationToken);
        }

        /// <summary>
        /// One of the functions that make up the <see cref="WaterfallDialog"/>.
        /// </summary>
        /// <param name="stepContext">The <see cref="WaterfallStepContext"/> gives access to the executing dialog runtime.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
        /// <returns>A <see cref="DialogTurnResult"/> to communicate some flow control back to the containing WaterfallDialog.</returns>
        private async Task<DialogTurnResult> NameStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            if ((bool)stepContext.Result)
            {
                // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
                // Running a prompt here means the next WaterfallStep will be run when the users response is received.
                return await stepContext.PromptAsync("nom", new PromptOptions { Prompt = MessageFactory.Text("Veuillez saisir votre nom.") }, cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("Merci. Aurevoir."), cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);

            }
        }

        /// <summary>
        /// One of the functions that make up the <see cref="WaterfallDialog"/>.
        /// </summary>
        /// <param name="stepContext">The <see cref="WaterfallStepContext"/> gives access to the executing dialog runtime.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
        /// <returns>A <see cref="DialogTurnResult"/> to communicate some flow control back to the containing WaterfallDialog.</returns>
        private async Task<DialogTurnResult> NameConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the current feedbackData object from user state.
            var feedbackData = await _accessors.FeedbackData.GetAsync(stepContext.Context, () => new FeedbackData(), cancellationToken);

            // Update the feedbackData.
            feedbackData.Name = (string)stepContext.Result;

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            return await stepContext.PromptAsync("email", new PromptOptions { Prompt = MessageFactory.Text("Veuillez saisir votre adresse email.") }, cancellationToken);
        }

        /// <summary>
        /// One of the functions that make up the <see cref="WaterfallDialog"/>.
        /// </summary>
        /// <param name="stepContext">The <see cref="WaterfallStepContext"/> gives access to the executing dialog runtime.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
        /// <returns>A <see cref="DialogTurnResult"/> to communicate some flow control back to the containing WaterfallDialog.</returns>
        private async Task<DialogTurnResult> EmailConfirmStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {
            // Get the current feedbackData object from user state.
            var feedbackData = await _accessors.FeedbackData.GetAsync(stepContext.Context, () => new FeedbackData(), cancellationToken);

            // Update the feedbackData.
            feedbackData.Email = (string)stepContext.Result;

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog; here it is a Prompt Dialog.
            return await stepContext.PromptAsync("message", new PromptOptions { Prompt = MessageFactory.Text("Veuillez saisir votre message.") }, cancellationToken);
        }

        /// <summary>
        /// One of the functions that make up the <see cref="WaterfallDialog"/>.
        /// </summary>
        /// <param name="stepContext">The <see cref="WaterfallStepContext"/> gives access to the executing dialog runtime.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
        /// <returns>A <see cref="DialogTurnResult"/> to communicate some flow control back to the containing WaterfallDialog.</returns>
        private async Task<DialogTurnResult> SummaryStepAsync(WaterfallStepContext stepContext, CancellationToken cancellationToken)
        {

            // Get the current feedbackData object from user state.
            var feedbackData = await _accessors.FeedbackData.GetAsync(stepContext.Context, () => new FeedbackData(), cancellationToken);

            // Update the feedbackData.
            feedbackData.Message = (string)stepContext.Result;

            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Votre nom {feedbackData.Name}, votre email {feedbackData.Email} et votre message {feedbackData.Message}."), cancellationToken);

            // WaterfallStep always finishes with the end of the Waterfall or with another dialog, here it is the end.
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        /// <summary>
        /// This is an example of a custom validator. 
        /// Returning true indicates the recognized value is acceptable. Returning false will trigger re-prompt behavior.
        /// </summary>
        /// <param name="promptContext">The <see cref="PromptValidatorContext"/> gives the validator code access to the runtime, including the recognized value and the turn context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the operation result of the Turn operation.</returns>
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
            return Task.FromResult(false);
        }

        private bool IsValidEmail(string source)
        {
            return new EmailAddressAttribute().IsValid(source);
        }
    }
}
