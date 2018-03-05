using Autofac;
using Microsoft.Bot.Builder.Azure;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Builder.Scorables;
using Microsoft.Bot.Connector;
using ScorableHelp.Dialogs;
using System.Web.Http;

namespace ScorableHelp
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);

            Conversation.UpdateContainer(builder =>
            {
                builder.RegisterType<HelpScorable>()
                    .As<IScorable<IActivity, double>>()
                    .InstancePerLifetimeScope();

                var store = new InMemoryDataStore();

                builder.Register(c => store)
                    .Keyed<IBotDataStore<BotData>>(AzureModule.Key_DataStore)
                    .AsSelf()
                    .SingleInstance();
            });
        }
    }
}
