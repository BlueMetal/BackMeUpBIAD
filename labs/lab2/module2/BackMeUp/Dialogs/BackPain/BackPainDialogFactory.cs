using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Newtonsoft.Json;

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
                PromptForBiologicalSexAsync,
                PromptForCancerHistoryAsync,
                PromptForPsychCareHistoryAsync,
                PromptForPhysicalTherapyHistoryAsync,
                PromptForCognitiveBehavioralTherapyHistoryAsync,
                PromptForPreviousBackSurgeryHistoryAsync,
                PromptForFeverHistoryAsync,
                PromptForFecalIncontinenceHistoryAsync,
                PromptForOpioidUseAsync,
                PromptForLevelOfPainAsync,
                PromptForRaceStepAsync,
                SummaryAsync,
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

        private async Task<bool> ValidateAgeAsync(PromptValidatorContext<int> context, CancellationToken cancellationToken)
        {
            // Validates the age provided by the user. This will prevent invalid data.
            var value = context.Recognized.Value;
            if (value <= 0)
            {
                await context.Context.SendActivityAsync(
                    MessageFactory.Text("You must be older than zero years old."),
                    cancellationToken);
                return false;
            }

            // if the user is older than 120, make the age -1. This will cause them to exit out on the next step.
            if (value <= 120)
            {
                return true;
            }

            await context.Context.SendActivityAsync(
                MessageFactory.Text($"Congratulations for making it to {value} years of age. Unfortunately, our data set doesn't have the data necessary to help you."),
                cancellationToken);
            context.Recognized.Value = -1;

            return true;
        }

        private async Task<DialogTurnResult> PromptForBiologicalSexAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var demographics = await _accessors.BackPainDemographics.GetAsync(
                stepContext.Context,
                () => new BackPainDemographics(),
                cancellationToken);

            var age = (int)stepContext.Result;

            // if they were identified as too old during the age validation, we end the dialog here.
            if (age == -1)
            {
                return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
            }

            demographics.Age = age.ToString();

            return await stepContext.PromptAsync(
                Prompts.BiologicalSex,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Which option most closely approximates your biological sex."),
                    Choices = BackPainTranslations.BiologicalSexes.Select(kv => kv.Value.choice).ToList(),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForCancerHistoryAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var demographics = await _accessors.BackPainDemographics.GetAsync(
                stepContext.Context,
                () => new BackPainDemographics(),
                cancellationToken);
            demographics.BiologicalSex = BackPainTranslations
                .BiologicalSexes[((FoundChoice)stepContext.Result).Value]
                .code;
            return await stepContext.PromptAsync(
                Prompts.CancerHistory,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Have you ever had cancer?"),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForPsychCareHistoryAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var demographics = await _accessors.BackPainDemographics.GetAsync(
                stepContext.Context,
                () => new BackPainDemographics(),
                cancellationToken);
            demographics.CancerHistory = BackPainTranslations.YesNo((bool)stepContext.Result);
            return await stepContext.PromptAsync(
                Prompts.PsychCareHistory,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Have you ever received psychiatric care?"),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForPhysicalTherapyHistoryAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var demographics = await _accessors.BackPainDemographics.GetAsync(
                stepContext.Context,
                () => new BackPainDemographics(),
                cancellationToken);
            var psychiatricCare = (bool)stepContext.Result;
            demographics.PsychiatricCare = BackPainTranslations.YesNo(psychiatricCare);
            if (psychiatricCare)
            {
                await stepContext.Context.SendActivityAsync(MessageFactory.Text("I know that can be hard to admit some times, but sharing that information will definitely help us with the diagnosis."), cancellationToken);
                await Task.Delay(150, cancellationToken);
            }

            return await stepContext.PromptAsync(
                Prompts.PhysicalTherapyHistory,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Have you ever had physical therapy?"),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForCognitiveBehavioralTherapyHistoryAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var demographics = await _accessors.BackPainDemographics.GetAsync(
                stepContext.Context,
                () => new BackPainDemographics(),
                cancellationToken);
            demographics.HadPhysicalTherapy = BackPainTranslations.YesNo((bool)stepContext.Result);
            return await stepContext.PromptAsync(
                Prompts.CognitiveBehavioralTherapyHistory,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Have you ever had cognitive behavioral therapy?"),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForPreviousBackSurgeryHistoryAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var demographics = await _accessors.BackPainDemographics.GetAsync(
                stepContext.Context,
                () => new BackPainDemographics(),
                cancellationToken);
            demographics.CognitiveBehavioralTherapy = BackPainTranslations.YesNo((bool)stepContext.Result);
            return await stepContext.PromptAsync(
                Prompts.PreviousBackSurgeryHistory,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Have you had back surgery before?"),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForFeverHistoryAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var demographics = await _accessors.BackPainDemographics.GetAsync(
                stepContext.Context,
                () => new BackPainDemographics(),
                cancellationToken);
            demographics.PreviousBackSurgery = BackPainTranslations.YesNo((bool)stepContext.Result);
            return await stepContext.PromptAsync(
                Prompts.FeverHistory,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Is or was your back pain associated with fever?"),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForFecalIncontinenceHistoryAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var demographics = await _accessors.BackPainDemographics.GetAsync(
                stepContext.Context,
                () => new BackPainDemographics(),
                cancellationToken);
            demographics.Fever = BackPainTranslations.YesNo((bool)stepContext.Result);
            return await stepContext.PromptAsync(
                Prompts.FecalIncontinenceHistory,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("I know the next question stinks, but I have to ask. Have you suffered from fecal incontinence in association with your back pain?"),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForOpioidUseAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var demographics = await _accessors.BackPainDemographics.GetAsync(
                stepContext.Context,
                () => new BackPainDemographics(),
                cancellationToken);
            demographics.FecalIncontinence = BackPainTranslations.YesNo((bool)stepContext.Result);
            return await stepContext.PromptAsync(
                Prompts.OpioidUse,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("Are you, or have you recently been an opioid user?"),
                },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForLevelOfPainAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var demographics = await _accessors.BackPainDemographics.GetAsync(
                stepContext.Context,
                () => new BackPainDemographics(),
                cancellationToken);
            demographics.OpioidUse = BackPainTranslations.YesNo((bool)stepContext.Result);
            return await stepContext.PromptAsync(
                Prompts.LevelOfPain,
                new PromptOptions
            {
                Prompt = MessageFactory.Text("How much pain are you in (1 is lowest, 10 is highest)?"),
                Choices = BackPainTranslations.PainLevelOptions,
            },
                cancellationToken);
        }

        private async Task<DialogTurnResult> PromptForRaceStepAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            BackPainDemographics demographics = await _accessors.BackPainDemographics.GetAsync(
                stepContext.Context,
                () => new BackPainDemographics(),
                cancellationToken);
            FoundChoice choice = (FoundChoice)stepContext.Result;
            demographics.LevelOfPain = choice.Value;
            return await stepContext.PromptAsync(
                Prompts.Race,
                new PromptOptions
                {
                    Prompt = MessageFactory.Text("What is your race?"),
                    Choices = BackPainTranslations.Races.Select(kv => kv.Value.choice).ToList(),
                }, cancellationToken);
        }

        private async Task<DialogTurnResult> SummaryAsync(
            WaterfallStepContext stepContext,
            CancellationToken cancellationToken)
        {
            var state = await _accessors.BackPainDemographics.GetAsync(
                stepContext.Context,
                () => new BackPainDemographics(),
                cancellationToken);
            var race = (FoundChoice)stepContext.Result;
            state.Race = BackPainTranslations.Races[race.Value].code;

            // reflect the user's answers back. This is retrieved from state
            var serialized = JsonConvert.SerializeObject(state, Formatting.Indented);
            await stepContext.Context.SendActivityAsync(MessageFactory.Text(serialized), cancellationToken);
            return await stepContext.EndDialogAsync(cancellationToken: cancellationToken);
        }

        private void RegisterPrompts(DialogSet dialogSet)
        {
            dialogSet.Add(new ConfirmPrompt(Prompts.ConfirmStart));
            dialogSet.Add(new NumberPrompt<int>(Prompts.Age, ValidateAgeAsync));
            dialogSet.Add(new ChoicePrompt(Prompts.BiologicalSex));
            dialogSet.Add(new ConfirmPrompt(Prompts.CancerHistory));
            dialogSet.Add(new ConfirmPrompt(Prompts.PsychCareHistory));
            dialogSet.Add(new ConfirmPrompt(Prompts.PhysicalTherapyHistory));
            dialogSet.Add(new ConfirmPrompt(Prompts.CognitiveBehavioralTherapyHistory));
            dialogSet.Add(new ConfirmPrompt(Prompts.PreviousBackSurgeryHistory));
            dialogSet.Add(new ConfirmPrompt(Prompts.FeverHistory));
            dialogSet.Add(new ConfirmPrompt(Prompts.FecalIncontinenceHistory));
            dialogSet.Add(new ConfirmPrompt(Prompts.OpioidUse));
            dialogSet.Add(new ChoicePrompt(Prompts.LevelOfPain));
            dialogSet.Add(new ChoicePrompt(Prompts.Race));
        }

        private static class Prompts
        {
            public const string ConfirmStart = DialogId + "_confrim-start";
            public const string Age = DialogId + "_age";
            public const string BiologicalSex = DialogId + "_biological-sex";
            public const string CancerHistory = DialogId + "_cancer-history";
            public const string PsychCareHistory = DialogId + "_psych-care-history";
            public const string PhysicalTherapyHistory = DialogId + "_physical-therapy-history";
            public const string CognitiveBehavioralTherapyHistory = DialogId + "_cognitive-behavioral-therapy-history";
            public const string PreviousBackSurgeryHistory = DialogId + "_previous-back-surgery-history";
            public const string FeverHistory = DialogId + "_fever-history";
            public const string FecalIncontinenceHistory = DialogId + "_fecal-incontinence-history";
            public const string OpioidUse = DialogId + "_opioid-use";
            public const string LevelOfPain = DialogId + "_level-of-pain";
            public const string Race = DialogId + "_race";
        }
    }
}
