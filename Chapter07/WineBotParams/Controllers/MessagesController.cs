using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.FormFlow;

namespace WineBotParams
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
                await Conversation.SendAsync(activity, BuildWineDialog);

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        IDialog<WineForm> BuildWineDialog()
        {
            return FormDialog.FromForm(new WineForm().BuildForm);
        }
    }
}