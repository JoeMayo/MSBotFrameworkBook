using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace WineBot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
                await Conversation.SendAsync(activity, () => new WineSearchDialog());
            else
                await HandleSystemMessageAsync(activity);

            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        async Task HandleSystemMessageAsync(Activity message)
        {
            if (message.Type == ActivityTypes.ConversationUpdate)
            {
                const string WelcomeMessage =
                    "Welcome to WineBot! You can type \"catalog\" to search wines.";

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
    }
}