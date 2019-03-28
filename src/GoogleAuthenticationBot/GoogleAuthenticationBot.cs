// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Microsoft.Recognizers.Text;

namespace GoogleAuthenticationBot
{
    /// <summary>
    /// Represents a bot that processes incoming activities.
    /// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    /// This is a Transient lifetime service.  Transient lifetime services are created
    /// each time they're requested. For each Activity received, a new instance of this
    /// class is created. Objects that are expensive to construct, or have a lifetime
    /// beyond the single turn, should be carefully managed.
    /// For example, the <see cref="MemoryStorage"/> object and associated
    /// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
    public class GoogleAuthenticationBot : IBot
    {
        private readonly GoogleAuthenticationBotAccessors _accessors;
        private readonly ILogger _logger;
        private DialogSet _dialogs;
        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="conversationState">The managed conversation state.</param>
        /// <param name="loggerFactory">A <see cref="ILoggerFactory"/> that is hooked to the Azure App Service provider.</param>
        /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-2.1#windows-eventlog-provider"/>
        public GoogleAuthenticationBot(GoogleAuthenticationBotAccessors accessors, ILoggerFactory loggerFactory)
        {

            _accessors = accessors ?? throw new ArgumentNullException(nameof(accessors));

           

            if (loggerFactory == null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<GoogleAuthenticationBot>();
            _logger.LogTrace("Turn start.");


            // The DialogSet needs a DialogState accessor, it will call it when it has a turn context.
            _dialogs = new DialogSet(accessors.ConversationDialogState);

            // This array defines how the Waterfall will execute.
            var waterfallSteps = new WaterfallStep[]
            {
               PromptStepAsync,
               LoginStepAsync,
               LogoutStepAsync
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            _dialogs.Add(new WaterfallDialog("details", waterfallSteps));
            _dialogs.Add(new OAuthPrompt("auth", new OAuthPromptSettings { ConnectionName = "Google", Text = "Login Google", Title = "Login Google", Timeout = 300000 }, null));
            _dialogs.Add(new ConfirmPrompt("confirm", defaultLocale: Culture.French));
        }

        /// <summary>
        /// Every conversation turn for our Echo Bot will call this method.
        /// There are no dialogs used, since it's "single turn" processing, meaning a single
        /// request and response.
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

           if(turnContext == null)
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
            else if (turnContext.Activity.Type == ActivityTypes.Message || turnContext.Activity.Type == ActivityTypes.Event)
            {
                // if(dialogContext.ActiveDialog.Id=="")

                // Continues execution of the active dialog, if there is one
                var results = await dialogContext.ContinueDialogAsync(cancellationToken);

                // If the DialogTurnStatus is Empty we should start a new dialog.
                if (results.Status == DialogTurnStatus.Empty)
                {
                    await dialogContext.BeginDialogAsync("details", null, cancellationToken);
                }
            }

            // Save the dialog state into the conversation state.
            await _accessors.ConversationState.SaveChangesAsync(turnContext, false, cancellationToken);

        }


        /// <summary>
        /// This <see cref="WaterfallStep"/> prompts the user to log in.
        /// </summary>
        /// <param name="step">A <see cref="WaterfallStepContext"/> provides context for the current waterfall step.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the operation result of the operation.</returns>
        private static async Task<DialogTurnResult> PromptStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            return await step.BeginDialogAsync("auth", cancellationToken: cancellationToken);
        }

        /// <summary>
        /// In this step we check that a token was received and prompt the user as needed.
        /// </summary>
        /// <param name="step">A <see cref="WaterfallStepContext"/> provides context for the current waterfall step.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the operation result of the operation.</returns>
        private static async Task<DialogTurnResult> LoginStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            // Get the token from the previous step. Note that we could also have gotten the
            // token directly from the prompt itself. There is an example of this in the next method.
            var tokenResponse = (TokenResponse)step.Result;
            if (tokenResponse != null)
            {
                await step.Context.SendActivityAsync($"Vous êtes connecté. Voici votre jeton {tokenResponse.Token}", cancellationToken: cancellationToken);

                return await step.PromptAsync("confirm", new PromptOptions { Prompt = MessageFactory.Text("Voulez-vous nous laisser vos commentaires ?") }, cancellationToken);

                //return await step.PromptAsync("logout", new PromptOptions { Prompt = MessageFactory.Text("Voulez-vous vous déconnecter ?") }, cancellationToken);
             
            }

            await step.Context.SendActivityAsync("Echec de la connexion.", cancellationToken: cancellationToken);
            return Dialog.EndOfTurn;
        }

        /// <summary>
        /// This <see cref="WaterfallStep"/> prompts the user to log in.
        /// </summary>
        /// <param name="step">A <see cref="WaterfallStepContext"/> provides context for the current waterfall step.</param>
        /// <param name="cancellationToken" >(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> representing the operation result of the operation.</returns>
        private static async Task<DialogTurnResult> LogoutStepAsync(WaterfallStepContext step, CancellationToken cancellationToken)
        {
            if ((bool)step.Result)
            {
                //var botAdapter = (BotFrameworkAdapter)step...Adapter;
                //await botAdapter.SignOutUserAsync(turnContext, ConnectionName, cancellationToken: cancellationToken);

                await step.Context.SendActivityAsync("Merci, vous avez été déconnecté!", cancellationToken: cancellationToken);
            }
            else
            {
                await step.Context.SendActivityAsync("Merci, a plus tard!", cancellationToken: cancellationToken);
                
            }

            return Dialog.EndOfTurn;
        }



        private static async Task SendWelcomeMessageAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {

            await turnContext.SendActivityAsync(
        "Bienvenue. Ce bot permet de recueillir vos avis.",
        cancellationToken: cancellationToken);

        }

    }
}
