using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Linq;
using MusicChatbot.Dialogs;

namespace MusicChatbot
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
                await Conversation.SendAsync(activity, () => new RootDialog());
            else
                await HandleSystemMessageAsync(activity);

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        async Task HandleSystemMessageAsync(Activity message)
        {
            if (message.Type == ActivityTypes.ConversationUpdate)
            {
                Func<ChannelAccount, bool> isChatbot =
                            channelAcct => channelAcct.Id == message.Recipient.Id;

                if (message.MembersAdded?.Any(isChatbot) ?? false)
                {
                    Activity reply = (message as Activity).CreateReply(RootDialog.WelcomeMessage);
                    var connector = new ConnectorClient(new Uri(message.ServiceUrl));
                    await connector.Conversations.ReplyToActivityAsync(reply);
                }
            }
        }
    }
}