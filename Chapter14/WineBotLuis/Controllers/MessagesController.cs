using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using WineBotLuis.Dialogs;
using System.Linq;
using AILib;

namespace WineBotLuis
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
            {
                await DetectAndTranslateAsync(activity);
                await Conversation.SendAsync(activity, () => new Dialogs.WineBotDialog());
            }
            else
            {
                await HandleSystemMessageAsync(activity);
            }
            var response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        async Task DetectAndTranslateAsync(Activity activity)
        {
            var cogSvc = new CognitiveService();

            string language = await cogSvc.DetectLanguageAsync(activity.Text);

            if (!language.StartsWith("en"))
            {
                activity.Text = await cogSvc.TranslateTextAsync(activity.Text, "en");
                activity.Locale = language;
            }
        }

        async Task HandleSystemMessageAsync(Activity message)
        {
            if (message.Type == ActivityTypes.ConversationUpdate)
            {
                if (message.Type == ActivityTypes.ConversationUpdate)
                {
                    Func<ChannelAccount, bool> isChatbot =
                                channelAcct => channelAcct.Id == message.Recipient.Id;

                    if (message.MembersAdded?.Any(isChatbot) ?? false)
                    {
                        Activity reply = (message as Activity).CreateReply(
                            "# Welcome to Wine Chatbot!\n" + WineBotDialog.ExampleText);
                        var connector = new ConnectorClient(new Uri(message.ServiceUrl));
                        await connector.Conversations.ReplyToActivityAsync(reply);
                    }
                }
            }
        }
    }
}