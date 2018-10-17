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
                NextStepAsync,
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

        public async Task<DialogTurnResult> NextStepAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var result = (bool)stepContext.Result;
            if (result)
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("Great. Let's get started!"),
                    cancellationToken);
            }
            else
            {
                await stepContext.Context.SendActivityAsync(
                    MessageFactory.Text("Okay, I understand."),
                    cancellationToken);
            }

            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private void RegisterPrompts(DialogSet dialogSet)
        {
            dialogSet.Add(new ConfirmPrompt(Prompts.ConfirmStart));
        }

        private static class Prompts
        {
            public const string ConfirmStart = DialogId + "_confrim-start";
        }
    }
}
