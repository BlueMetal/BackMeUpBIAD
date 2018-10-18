using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BackMeUp.Dialogs;
using BackMeUp.Dialogs.BackPain;
using BackMeUp.Messages;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace BackMeUp
{
    public class BackMeUp : IBot
    {
        // Module 2
        private readonly DialogAccessors _accessors;
        private readonly DialogSet _dialogs;

        // Module 2 add accessors to signature
        public BackMeUp(DialogAccessors accessors, BackPainDialogFactory backPainDialogFactory, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            // Module 2
            _accessors = accessors;
            _dialogs = new DialogSet(_accessors.DialogState);
            _dialogs.Add(backPainDialogFactory.Configure(_dialogs));

            ILogger logger = loggerFactory.CreateLogger<BackMeUp>();
            logger.LogTrace("EchoBot turn start.");
        }

        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                var dialogContext = await _dialogs.CreateContextAsync(turnContext, cancellationToken);
                var dialogStatus = DialogTurnStatus.Empty;
                if (dialogContext.Dialogs != null)
                {
                    var results = await dialogContext.ContinueDialogAsync(cancellationToken);
                    dialogStatus = results.Status;
                }

                // We are not in a dialog, so resume turns as normal
                if (dialogStatus == DialogTurnStatus.Empty)
                {
                    var activityText = turnContext.Activity.Text?.Trim()?.ToLowerInvariant();

                    if (activityText == null)
                    {
                        var jsonData = turnContext.Activity.Value as JObject;
                        var postBackAction = jsonData?.GetValue("ActionType")?.Value<string>();
                        if (postBackAction == null)
                        {
                            return;
                        }

                        switch (postBackAction)
                        {
                            case PostBackActions.StartBackPainSurvey:
                                await dialogContext.BeginDialogAsync(
                                    BackPainDialogFactory.DialogId,
                                    cancellationToken: cancellationToken);
                                break;
                            case PostBackActions.SubmitBackPainData:
                                var backPainDemographics = jsonData["Data"].ToObject<BackPainDemographics>();
                                Debugger.Break();
                                break;
                            case PostBackActions.Default:
                                break;
                            default:
                                throw new InvalidOperationException($"The PostBack action type {postBackAction} was not recognized.");
                        }
                    }
                    else if (new[] { "back pain", "start" }.Any(t => t == activityText))
                    {
                        // start the dialog. We'll do better when we integrate LUIS
                        await dialogContext.BeginDialogAsync(
                            BackPainDialogFactory.DialogId,
                            cancellationToken: cancellationToken);
                    }
                    else
                    {
                        var responseMessage = MessageFactory.Text($"You said \"{turnContext.Activity.Text}\"");
                        await turnContext.SendActivityAsync(responseMessage, cancellationToken);
                    }
                }

                await _accessors.ConversationState.SaveChangesAsync(turnContext, cancellationToken: cancellationToken);
            }
            else
            {
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected", cancellationToken: cancellationToken);
            }
        }
    }
}
