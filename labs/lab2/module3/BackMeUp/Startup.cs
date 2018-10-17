using System;
using System.Linq;
using BackMeUp.Dialogs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BackMeUp
{
    public class Startup
    {
        private ILoggerFactory _loggerFactory;
        private bool _isProduction = false;

        public Startup(IHostingEnvironment env)
        {
            _isProduction = env.IsProduction();
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            var secretKey = Configuration.GetSection("botFileSecret")?.Value;
            var botFilePath = Configuration.GetSection("botFilePath")?.Value;

            // Loads .bot configuration file and adds a singleton that your Bot can access through dependency injection.
            var botConfig = BotConfiguration.Load(botFilePath ?? @".\BotConfiguration.bot", secretKey);
            services.AddSingleton(sp => botConfig ?? throw new InvalidOperationException($"The .bot config file could not be loaded. ({botConfig})"));

            // Retrieve current endpoint.
            var environment = _isProduction ? "production" : "development";

            ICredentialProvider credentialProvider = null;

            foreach (var service in botConfig.Services)
            {
                switch (service.Type)
                {
                    case ServiceTypes.Endpoint:
                        if (service is EndpointService endpointService)
                        {
                            credentialProvider = new SimpleCredentialProvider(endpointService.AppId, endpointService.AppPassword);
                        }

                        break;
                }
            }

            services.AddBot<BackMeUp>(opt => ConfigureBot<BackMeUp>(opt, credentialProvider, _loggerFactory));
            services.AddSingleton(CreateDialogAccessors);
        }

        public DialogAccessors CreateDialogAccessors(IServiceProvider serviceProvider)
        {
            var options = serviceProvider.GetRequiredService<IOptions<BotFrameworkOptions>>().Value;
            if (options == null)
            {
                throw new InvalidOperationException(
                    "BBotFrameworkOptions must be configured prior to setting up the state accessors");
            }

            var conversationState = options.State.OfType<ConversationState>().FirstOrDefault();
            if (conversationState == null)
            {
                throw new InvalidOperationException(
                    "ConversationState must be defined and added before adding conversation-scoped state accessors.");
            }

            var accessors = new DialogAccessors(conversationState)
            {
                DialogState = conversationState.CreateProperty<DialogState>(DialogAccessors.DialogStateName),
            };

            return accessors;
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseBotFramework();
        }

        private static void ConfigureBot<T>(BotFrameworkOptions options, ICredentialProvider credentialProvider, ILoggerFactory loggerFactory)
        {
            // Set the CredentialProvider for the bot. It uses this to authenticate with the QnA service in Azure
            options.CredentialProvider = credentialProvider
                ?? throw new InvalidOperationException("Missing endpoint information from bot configuraiton file.");

            // Creates a logger for the application to use.
            ILogger logger = loggerFactory.CreateLogger<T>();

            // Catches any errors that occur during a conversation turn and logs them.
            options.OnTurnError = async (context, exception) =>
            {
                logger.LogError($"Exception caught : {exception}");
                await context.SendActivityAsync("Sorry, it looks like something went wrong.");
            };

            // The Memory Storage used here is for local bot debugging only. When the bot
            // is restarted, everything stored in memory will be gone.
            IStorage dataStore = new MemoryStorage();

            // Create Conversation State object.
            // The Conversation State object is where we persist anything at the conversation-scope.
            var conversationState = new ConversationState(dataStore);

            options.State.Add(conversationState);
        }
    }
}
