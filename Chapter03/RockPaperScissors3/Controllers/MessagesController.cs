using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Connector;
using RockPaperScissors3.Models;

namespace RockPaperScissors3
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                var connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                string message = await GetMessage(connector, activity);

                Activity reply = activity.BuildMessageActivity(message);

                await connector.Conversations.ReplyToActivityAsync(reply);
            }

            HttpResponseMessage response = Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        async Task<string> GetMessage(ConnectorClient connector, Activity activity)
        {
            var state = new GameState();

            string userText = activity.Text.ToLower();
            string message = "";

            if (userText.Contains(value: "score"))
            {
                message = await state.GetScoresAsync(activity);
            }
            else if (userText.Contains(value: "delete"))
            {
                message = await state.DeleteScoresAsync(activity);
            }
            else
            {
                var game = new Game();
                message = game.Play(userText);

                bool isValidInput = !message.StartsWith("Type");
                if (isValidInput)
                {
                    if (message.Contains(value: "Tie"))
                    {
                        await state.AddTieAsync(activity);
                    }
                    else
                    {
                        bool userWin = message.Contains(value: "win");
                        await state.UpdateScoresAsync(activity, userWin);
                    }
                }
            }

            return message;
        }
    }
}