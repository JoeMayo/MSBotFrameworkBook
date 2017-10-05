using Autofac;
using Microsoft.Bot.Builder.Dialogs;
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
            });
        }
    }
}
