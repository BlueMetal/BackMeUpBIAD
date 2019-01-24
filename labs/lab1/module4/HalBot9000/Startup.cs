using System;
using System.Linq;
using HalBot9000.State;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HalBot9000
{
    /// <summary>
    ///     The Startup class configures services and the request pipeline.
    /// </summary>
    public class Startup
    {
        private readonly bool _isProduction;
        private ILoggerFactory _loggerFactory;

        public Startup(IHostingEnvironment env)
        {
            _isProduction = env.IsProduction();
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", true, true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        /// <summary>
        ///     Gets the configuration that represents a set of key/value application configuration properties.
        /// </summary>
        /// <value>
        ///     The <see cref="IConfiguration" /> that represents a set of key/value application configuration properties.
        /// </value>
        public IConfiguration Configuration { get; }

        /// <summary>
        ///     This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">
        ///     The <see cref="IServiceCollection" /> specifies the contract for a collection of service
        ///     descriptors.
        /// </param>
        /// <seealso cref="IStatePropertyAccessor{T}" />
        /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/web-api/overview/advanced/dependency-injection" />
        /// <seealso
        ///     cref="https://docs.microsoft.com/en-us/azure/bot-service/bot-service-manage-channels?view=azure-bot-service-4.0" />
        public void ConfigureServices(IServiceCollection services)
        {
            // We're going to use MemoryStorage here. This is really only suitable for development.
            // In a future lab, we'll switch to more robust options.
            IStorage storage = new MemoryStorage();

            // declare these here, so we can add them to the services collection outside of the "AddBot" block.
            // If you try to do "services.AddSingleton" inside of the AddBot block, the HalBot constructor will fail
            // to get the QnAMaker instance
            BotConfiguration botConfig = null;
            QnAMaker qnaMaker = null;

            // Configures the bot
            services.AddBot<HalBot>(options =>
            {
                var secretKey = Configuration.GetSection("botFileSecret")?.Value;
                var botFilePath = Configuration.GetSection("botFilePath")?.Value;

                // Loads .bot configuration file and adds a singleton that your Bot can access through dependency injection.
                botConfig = BotConfiguration.Load(botFilePath ?? @".\HalBot9000.bot", secretKey);
                
                // Retrieve current endpoint.
                var environment = _isProduction ? "production" : "development";

                foreach (var serviceConfig in botConfig.Services)
                {
                    switch (serviceConfig.Type)
                    {
                        case ServiceTypes.Endpoint:
                            if (serviceConfig is EndpointService endpointService)
                            {
                                // initialize the credential provider for the bot endpoint
                                options.CredentialProvider =
                                    new SimpleCredentialProvider(endpointService.AppId, endpointService.AppPassword);
                            }
                            break;
                        case ServiceTypes.QnA:
                            if (serviceConfig is QnAMakerService qnaMakerService)
                            {
                                // creates a QnA Maker endpoint and allows it to be injected as a singleton
                                var qnaEndpoint = new QnAMakerEndpoint
                                {
                                    Host = qnaMakerService.Hostname,
                                    EndpointKey = qnaMakerService.EndpointKey,
                                    KnowledgeBaseId = qnaMakerService.KbId
                                };
                                qnaMaker = new QnAMaker(qnaEndpoint);
                            }
                            break;
                        default:
                            throw new NotImplementedException($"The service type {serviceConfig.Type} is not supported by this bot.");
                    }
                }

                // Creates a logger for the application to use.
                ILogger logger = _loggerFactory.CreateLogger<HalBot>();

                // Catches any errors that occur during a conversation turn and logs them.
                options.OnTurnError = async (context, exception) =>
                {
                    logger.LogError($"Exception caught : {exception}");
                    await context.SendActivityAsync("Sorry, it looks like something went wrong.");
                };
            });

            services.AddSingleton(sp =>
                botConfig ??
                throw new InvalidOperationException("The .bot config file could not be loaded."));
            services.AddSingleton(sp =>
                qnaMaker ?? throw new InvalidOperationException(
                    "The QnAMaker was never initialized. Missing configuration in the .bot config file."));

            // This adds a singleton instance of HalBot9000Accessors to dependency injection.
            services.AddSingleton(sp =>
            {
                // Initializes the user state object. This manages the lifecycle of objects scoped to a user.
                var userState = new UserState(storage);

                // Creates a property accessor for UserProfile that is scoped to the user
                var userProfilePropertyAccessor =
                    userState.CreateProperty<UserProfile>(HalBot9000Accessors.UserProfileStateName);

                var accessors = new HalBot9000Accessors(userState)
                {
                    UserProfile = userProfilePropertyAccessor
                };

                return accessors;
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseBotFramework();
        }
    }
}