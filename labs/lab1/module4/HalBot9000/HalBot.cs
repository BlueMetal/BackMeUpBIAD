// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HalBot9000.State;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace HalBot9000
{
    public class HalBot : IBot
    {
        private readonly HalBot9000Accessors _accessors;
        private readonly ILogger _logger;
        private readonly QnAMaker _qnAMaker;

        public HalBot(HalBot9000Accessors accessors, ILoggerFactory loggerFactory, QnAMaker qnAMaker)
        {
            if (loggerFactory == null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<HalBot>();
            _logger.LogTrace("Turn start.");
            _accessors = accessors ?? throw new System.ArgumentNullException(nameof(accessors));
            _qnAMaker = qnAMaker;
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
            // we only care about messages in this exercise
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                // It's possible to get blank messages.
                if (string.IsNullOrWhiteSpace(turnContext.Activity.Text))
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text("This doesn't work unless you say something first."), cancellationToken);
                    return;
                }

                // Here's where we actually call out to the QnA Maker service to get an answer.
                var results = await _qnAMaker.GetAnswersAsync(turnContext).ConfigureAwait(false);

                if (results.Any())
                {
                    // If there is a result, we get the answer with the highest score (best match)
                    var topResult = results.First();
                    await turnContext.SendActivityAsync(MessageFactory.Text(topResult.Answer), cancellationToken);
                }
                else
                {
                    // If there's no answer, say so.
                    await turnContext.SendActivityAsync(MessageFactory.Text("I'm sorry Dave, I don't understand you."), cancellationToken);
                }
            }
            else
            {
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected", cancellationToken: cancellationToken);
            }
        }
    }
}
