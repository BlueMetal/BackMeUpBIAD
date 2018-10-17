using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace BackMeUp.Dialogs.BackPain
{
    public class BackPainDialogFactory
    {
        public const string DialogId = "back-pain";

        private readonly DialogAccessors _accessors;

        public BackPainDialogFactory(DialogAccessors accessors)
        {
            _accessors = accessors;
        }

        public Dialog Configure(DialogSet dialogSet)
        {
            RegisterPrompts(dialogSet);
            var steps = new WaterfallStep[]
            {
                ConfirmStartAsync,
                PromptForAgeAsync,
                NextStepAsync,
                FinalStepAsync,
            };
            var waterfallDialog = new WaterfallDialog(DialogId, steps);
            return waterfallDialog;
        }

        public async Task<DialogTurnResult> ConfirmStartAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            // Provides a disclaimer about privacy and prompts the user to agree before continuing
            await stepContext.Context.SendActivityAsync(
                MessageFactory.Text(
                    @"I'm about to ask you some questions to try to determine a course of care for your back pain.
Some of the questions will be very personal. While we do use the information you provide to improve our ability to help future patients, we will never try to identify individuals or share individual data with anyone."),
                cancellationToken);
            await Task.Delay(500, cancellationToken); // half-second between messages feels a little more natural
            await stepContext.Context.SendActivityAsync(MessageFactory.Text("We value your privacy above all other concerns."), cancellationToken);

            return await stepContext.PromptAsync(Prompts.ConfirmStart, new PromptOptions { Prompt = MessageFactory.Text("Do you wish to proceed?") }, cancellationToken);
        }

        public async Task<DialogTurnResult> PromptForAgeAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            // If the user answered "yes", then we'll continue
            var proceed = (bool)stepContext.Result;
            if (proceed)
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("Okay. Let's get started."),
                    cancellationToken);
                await Task.Delay(500, cancellationToken);
            }
            else
            {
                // user answered "no".
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("I understand. I hope you feel better."),
                    cancellationToken);
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }

            return await stepContext.PromptAsync(
                Prompts.Age,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("How old are you?"),
                },
                cancellationToken);
        }

        public async Task<DialogTurnResult> NextStepAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            // get the age from the last prompt
            var result = (int)stepContext.Result;

            // get the state (or create it if it is new)
            var state = await _accessors.BackPainDemographics.GetAsync(
                stepContext.Context,
                () => new BackPainDemographics(),
                cancellationToken);

            // set the age in the state object
            state.Age = result.ToString(CultureInfo.InvariantCulture);

            // for now, we're going to end this step without a prompt so we can see the state persistence
            // at work in the next step
            return await stepContext.NextAsync(cancellationToken: cancellationToken);
        }

        public async Task<DialogTurnResult> FinalStepAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var state = await _accessors.BackPainDemographics.GetAsync(
                stepContext.Context,
                () => new BackPainDemographics(),
                cancellationToken);

            // reflect the user's age back. This is retrieved from state
            await stepContext.Context.SendActivityAsync(MessageFactory.Text($"Your age is {state.Age}"), cancellationToken);
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private void RegisterPrompts(DialogSet dialogSet)
        {
            dialogSet.Add(new ConfirmPrompt(Prompts.ConfirmStart));
            dialogSet.Add(new NumberPrompt<int>(Prompts.Age));
        }

        private static class Prompts
        {
            public const string ConfirmStart = DialogId + "_confrim-start";
            public const string Age = DialogId + "_age";
        }
    }
}
