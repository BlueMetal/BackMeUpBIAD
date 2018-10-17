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

        public IStatePropertyAccessor<DialogState> DialogState { get; set; }

        public ConversationState ConversationState { get; }
    }
}
