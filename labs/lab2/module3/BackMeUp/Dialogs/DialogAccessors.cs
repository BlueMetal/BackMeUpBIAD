using BackMeUp.Dialogs.BackPain;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;

namespace BackMeUp.Dialogs
{
    public class DialogAccessors
    {
        public DialogAccessors(ConversationState conversationState)
        {
            ConversationState = conversationState;
        }

        public static string DialogStateName { get; } = $"{nameof(DialogAccessors)}.{nameof(DialogState)}";

        public static string BackPainDemographicsName { get; } = $"{nameof(DialogAccessors)}.{nameof(BackPainDemographics)}";

        public IStatePropertyAccessor<DialogState> DialogState { get; set; }

        public IStatePropertyAccessor<BackPainDemographics> BackPainDemographics { get; set; }

        public ConversationState ConversationState { get; }
    }
}
