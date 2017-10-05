using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Connector;

namespace WineBot2
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                try
                {
                    await Conversation.SendAsync(activity, BuildWineDialog);
                }
                catch (FormCanceledException ex)
                {
                    HandleCanceledForm(activity, ex);
                }
            }
            else
            {
                await HandleSystemMessageAsync(activity);
            }

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        IDialog<WineForm> BuildWineDialog()
        {
            return FormDialog.FromForm(new WineForm().BuildForm);
        }

        async Task HandleSystemMessageAsync(Activity message)
        {
            if (message.Type == ActivityTypes.ConversationUpdate)
            {
                const string WelcomeMessage =
                    "Welcome to WineBot! " +
                    "Through a series of questions, WineBot can do a " +
                    "search and return wines that match your answers. " +
                    "You can type \"start\" to get started.";

                Func<ChannelAccount, bool> isChatbot =
                    channelAcct => channelAcct.Id == message.Recipient.Id;

                if (message.MembersAdded?.Any(isChatbot) ?? false)
                {
                    Activity reply = message.CreateReply(WelcomeMessage);

                    var connector = new ConnectorClient(new Uri(message.ServiceUrl));
                    await connector.Conversations.ReplyToActivityAsync(reply);
                }
            }
        }

        void HandleCanceledForm(Activity activity, FormCanceledException ex)
        {
            string responseMessage =
                $"Your conversation ended on {ex.Last}. " +
                "The following properties have values: " +
                string.Join(", ", ex.Completed);

            var connector = new ConnectorClient(new Uri(activity.ServiceUrl));
            var response = activity.CreateReply(responseMessage);
            connector.Conversations.ReplyToActivity(response);
        }
    }
}