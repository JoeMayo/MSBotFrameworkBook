using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.FormFlow;
using System;
using WineBotLib;

namespace WineBotFields
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
                try
                {
                    IDialog<WineForm> wineDialog = await BuildWineDialogAsync();
                    await Conversation.SendAsync(activity, () => wineDialog);
                }
                catch (FormCanceledException<WineForm> fcEx)
                {
                    Activity reply = activity.CreateReply(fcEx.Message);
                    var connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                    connector.Conversations.ReplyToActivity(reply);
                }

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        Refinement[] wineCategories;

        async Task<IDialog<WineForm>> BuildWineDialogAsync()
        {
            if (wineCategories == null)
                wineCategories = await new WineApi().GetWineCategoriesAsync();

            var wineForm = new WineForm
            {
                WineCategories =
                    wineCategories,
                InStock = StockingType.InStock,
                Rating = 75,
                Vintage = 2010
            };

            return new FormDialog<WineForm>(
                wineForm, 
                wineForm.BuildForm, 
                FormOptions.PromptFieldsWithValues);
        }
    }
}