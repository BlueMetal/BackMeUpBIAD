using System;
using HalBot9000.State;
using Microsoft.Bot.Builder;

namespace HalBot9000
{
    public class HalBot9000Accessors
    {
        public HalBot9000Accessors(UserState userState)
        {
            UserState = userState ?? throw new ArgumentNullException(nameof(userState));
        }

        public static string UserProfileStateName { get; } = $"{nameof(HalBot9000Accessors)}.UserProfileState";

        public IStatePropertyAccessor<UserProfile> UserProfile { get; set; }

        public UserState UserState { get; }
    }
}
