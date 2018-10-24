using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BackMeUp.AzureML;
using BackMeUp.Dialogs;
using BackMeUp.Dialogs.BackPain;
using BackMeUp.Messages;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace BackMeUp
{
    public class BackMeUp : IBot
    {
        private readonly LuisRecognizer _luis;
        private readonly DialogAccessors _accessors;
        private readonly HealthOutcomeService _healthOutcomeService;
        private readonly DialogSet _dialogs;

        public BackMeUp(
            LuisRecognizer luis,
            DialogAccessors accessors,
            BackPainDialogFactory backPainDialogFactory,
            HealthOutcomeService healthOutcomeService,
            ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            _luis = luis;
            _accessors = accessors;
            _healthOutcomeService = healthOutcomeService;
            _dialogs = new DialogSet(_accessors.DialogState);
            _dialogs.Add(backPainDialogFactory.Configure(_dialogs));

            ILogger logger = loggerFactory.CreateLogger<BackMeUp>();
            logger.LogTrace("EchoBot turn start.");
        }

        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                var luisResult = turnContext.Activity.Text == null
                    ? null
                    : await _luis.RecognizeAsync(turnContext, cancellationToken);
                var (luisIntent, _) = luisResult?.GetTopScoringIntent() ?? (null, 0.0);

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
                                await PredictBackPainTreatmentAsync(turnContext, backPainDemographics, cancellationToken);
                                break;
                            case PostBackActions.Default:
                                break;
                            default:
                                throw new InvalidOperationException($"The PostBack action type {postBackAction} was not recognized.");
                        }
                    }
                    else if (luisIntent == "Root_Command")
                    {
                        await ProcessCommandAsync(turnContext, luisResult, dialogContext, cancellationToken);
                    }
                    else
                    {
                        var responseMessage = MessageFactory.Text($"I'm sorry. I don't know how to do that.");
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

        private static async Task ProcessCommandAsync(
            ITurnContext turnContext,
            RecognizerResult luisResult,
            DialogContext dialogContext,
            CancellationToken cancellationToken)
        {
            var commands = luisResult?.Entities["Command"]?.ToObject<string[][]>();
            if (commands == null || commands.Length == 0 || commands[0].Length == 0)
            {
                var responseMessage = MessageFactory.Text(
                    "We're here to help. Try typing \"help me with back pain\"");
                await turnContext.SendActivityAsync(responseMessage, cancellationToken);
                return;
            }

            var theCommand = commands[0][0];
            switch (theCommand)
            {
                case "back pain":
                    await dialogContext.BeginDialogAsync(
                        BackPainDialogFactory.DialogId,
                        cancellationToken: cancellationToken);
                    break;
                default:
                    var responseMessage = MessageFactory.Text(
                        "I'm sorry. I can't help you with that. Try typing \"help me with back pain\"");
                    await turnContext.SendActivityAsync(responseMessage, cancellationToken);
                    break;
            }
        }

        private async Task PredictBackPainTreatmentAsync(
            ITurnContext turnContext,
            BackPainDemographics demographics,
            CancellationToken cancellationToken)
        {
            // Let them know we're working on getting their data
            await turnContext.SendActivityAsync(
                MessageFactory.Text("Thank you. I'm working on an answer for you. It may take a few seconds."),
                cancellationToken);

            var treatments = BackPainTranslations.Treatments;

            // Get treatment options from Azure Machine Learning
            var results = await _healthOutcomeService
                .GetOutcomesForTreatmentsAsync(
                    demographics,
                    treatments,
                    cancellationToken);

            if (results == null || !results.Any())
            {
                // if there are no results, let them know
                await turnContext.SendActivityAsync(
                    MessageFactory.Text("I'm sorry. I could not get any results suggesting care options."),
                    cancellationToken);
            }
            else
            {
                // get the best result that is considered a success
                var theResult = results.FirstOrDefault(y => y.ResultStrength < 3);

                if (theResult == null)
                {
                    if (results.Count == 1)
                    {
                        // if there are no success options and there is only one result
                        var bestOption = results.Single();
                        var message = "Unfortunately, it seems it is unlikely that any of the treatment options will be successful. " +
                            $"But your best option seems to be **{treatments[bestOption.Treatment]}**. " +
                            "I suggest you discuss this with your doctor.";
                        await turnContext.SendActivityAsync(
                            MessageFactory.Text(message),
                            cancellationToken);
                    }
                    else
                    {
                        // if there are two or more results, offer up the top two
                        var bestOptions = results.Take(2).ToArray();
                        var message = "Unfortunately, it seems it is unlikely that any of the treatment options will be successful. " +
                            $"But your best options seem to be **{treatments[bestOptions[0].Treatment]}** or **{treatments[bestOptions[1].Treatment]}**. " +
                            "I suggest you discuss these with your doctor.";
                        await turnContext.SendActivityAsync(
                            MessageFactory.Text(message),
                            cancellationToken);
                    }
                }
                else
                {
                    // if there was at least one successful option, return the highest ranked one
                    var message = $"Your best treatment option with a likely successful result is **{treatments[theResult.Treatment]}**. " +
                        "I suggest you discuss it with your doctor.";
                    await turnContext.SendActivityAsync(MessageFactory.Text(message), cancellationToken);
                }
            }
        }
    }
}
