using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;

namespace WineBotDialogStack
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
                    await Conversation.SendAsync(activity, () => new Dialogs.RootDialog());
                }
                catch (InvalidOperationException ex)
                {
                    var client = new ConnectorClient(new Uri(activity.ServiceUrl));
                    var reply = activity.CreateReply($"Reset Message: {ex.Message}");
                    client.Conversations.ReplyToActivity(reply);
                }
            }

            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }
    }
}