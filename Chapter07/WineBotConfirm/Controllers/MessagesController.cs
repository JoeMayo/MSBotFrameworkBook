using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.FormFlow;
using System;

namespace WineBotConfirm
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
                try
                {
                    await Conversation.SendAsync(activity, BuildWineDialog);
                }
                catch (FormCanceledException fcEx)
                {
                    Activity reply = activity.CreateReply("Form Canceled.");
                    var connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                    connector.Conversations.ReplyToActivity(reply);
                }

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        IDialog<WineForm> BuildWineDialog()
        {
            return FormDialog.FromForm(new WineForm().BuildForm);
        }
    }
}