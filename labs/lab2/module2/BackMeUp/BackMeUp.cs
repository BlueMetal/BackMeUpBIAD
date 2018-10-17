using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BackMeUp.Dialogs;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace BackMeUp
{
    public class BackMeUp : IBot
    {
        private readonly ILogger _logger;

        // Module 2
        private readonly DialogAccessors _accessors;
        private readonly DialogSet _dialogs;

        // Module 2 add accessors to signature
        public BackMeUp(DialogAccessors accessors, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            // Module 2
            _accessors = accessors;
            _dialogs = new DialogSet(_accessors.DialogState);
            _dialogs.Add(new ConfirmPrompt("does-it-hurt"));

            _logger = loggerFactory.CreateLogger<BackMeUp>();
            _logger.LogTrace("EchoBot turn start.");
        }

        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                var dialogContext = await _dialogs.CreateContextAsync(turnContext, cancellationToken);
                var results = await dialogContext.ContinueDialogAsync(cancellationToken);

                // We are not in a dialog, so resume turns as normal
                if (results.Status == DialogTurnStatus.Empty)
                {
                    var activityText = turnContext.Activity.Text.Trim().ToLowerInvariant();

                    // start the dialog. We'll do better when we integrate LUIS
                    if (new[] { "back pain", "start" }.Any(t => t == activityText))
                    {
                        // the prompt to show to the user when presenting them with the dialog
                        var prompt = new PromptOptions
                        {
                            Prompt = MessageFactory.Text("Would you like to take a survey?"),
                        };

                        // Starts the dialog using the provided prompt
                        var result = await dialogContext.PromptAsync("does-it-hurt", prompt, cancellationToken);
                    }
                    else
                    {
                        var responseMessage = MessageFactory.Text($"You said \"{turnContext.Activity.Text}\"");
                        await turnContext.SendActivityAsync(responseMessage, cancellationToken);
                    }
                }
            }
            else
            {
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected");
            }
        }
    }
}
