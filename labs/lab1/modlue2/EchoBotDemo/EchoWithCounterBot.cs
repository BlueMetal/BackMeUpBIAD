// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace EchoBotDemo
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
    public class EchoWithCounterBot : IBot
    {
        private readonly EchoBotAccessors _accessors;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="EchoWithCounterBot"/> class.
        /// </summary>
        /// <param name="accessors">A class containing <see cref="IStatePropertyAccessor{T}"/> used to manage state.</param>
        /// <param name="loggerFactory">A <see cref="ILoggerFactory"/> that is hooked to the Azure App Service provider.</param>
        /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-2.1#windows-eventlog-provider"/>
        public EchoWithCounterBot(EchoBotAccessors accessors, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<EchoWithCounterBot>();
            _logger.LogTrace("EchoBot turn start.");
            _accessors = accessors ?? throw new System.ArgumentNullException(nameof(accessors));
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
            // Handle Message activity type, which is the main activity type for shown within a conversational interface
            // Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
            // see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                // Get the conversation state from the turn context.
                var state = await _accessors.CounterState.GetAsync(turnContext, () => new CounterState());

                // Bump the turn count for this conversation.
                state.TurnCount++;

                // Set the property using the accessor.
                await _accessors.CounterState.SetAsync(turnContext, state);

                // Bonus Exercise 1
                // Get an invariant version of the text that has been trimmed of whitespace and punctuation
                var activityText = turnContext.Activity.Text.TrimEnd(",.?".ToCharArray()).Trim().ToLowerInvariant();

                // Bonus Exercise 2
                // If the user giving us a name, put it in state.
                var acknowledgeUserName = false;

                // Gets a new or existing instance of Lab1State
                var lab1State = await _accessors.Lab1State.GetAsync(turnContext, () => new Lab1State(), cancellationToken: cancellationToken);

                // Check to see if the user is sharing their name. This will get much easier with LUIS
                if (activityText.StartsWith("my name is"))
                {
                    // Extract name from the text
                    var name = turnContext.Activity.Text.Trim().Substring(10).TrimStart();

                    // add the name to the state object
                    lab1State.Name = name;
                    lab1State.WaitingForName = false;

                    // Update the state in the Accessors instance
                    await _accessors.Lab1State.SetAsync(turnContext, lab1State, cancellationToken);
                    acknowledgeUserName = true;
                }
                else if (lab1State.WaitingForName)
                {
                    lab1State.Name = turnContext.Activity.Text;
                    lab1State.WaitingForName = false;

                    // Update the state in the Accessors instance
                    await _accessors.Lab1State.SetAsync(turnContext, lab1State, cancellationToken);
                    acknowledgeUserName = true;
                }

                // Bonus Exercise 1
                string responseMessage;
                if (activityText == "marco")
                {
                    responseMessage = "Polo";
                }

                // Bonus Exercise 2
                else if (acknowledgeUserName)
                {
                    responseMessage = $"Greetings {lab1State.Name}. I'll remember your name.";
                }

                // Bonus Exercise 3
                else if (!lab1State.HasSaidHello && activityText == "hello")
                {
                    if (string.IsNullOrEmpty(lab1State.Name))
                    {
                        responseMessage = "Hello. What is your name?";
                        lab1State.WaitingForName = true;
                    }
                    else
                    {
                        responseMessage = $"Hello {lab1State.Name}. It's a pleasure to speak with you.";
                    }

                    lab1State.HasSaidHello = true;
                    await _accessors.Lab1State.SetAsync(turnContext, lab1State, cancellationToken);
                }
                else
                {
                    // Echo back to the user whatever they typed.
                    responseMessage = $"Turn {state.TurnCount}: You sent '{turnContext.Activity.Text}'\n";
                }

                // Only save state once
                await _accessors.ConversationState.SaveChangesAsync(turnContext, cancellationToken: cancellationToken);

                await turnContext.SendActivityAsync(responseMessage);
            }
            else
            {
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected");
            }
        }
    }
}
