using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;

namespace RockPaperScissors4.Models
{
    public class GameState
    {
        [Serializable]
        class PlayScore
        {
            public DateTime Date { get; set; } = DateTime.Now;
            public bool UserWin { get; set; }
        }

        public async Task<string> GetScoresAsync(ConnectorClient connector, Activity activity)
        {
            Activity typingActivity = activity.BuildTypingActivity();
            await connector.Conversations.ReplyToActivityAsync(typingActivity);
            await Task.Delay(millisecondsDelay: 10000);

            using (StateClient stateClient = activity.GetStateClient())
            {
                IBotState chatbotState = stateClient.BotState;
                BotData chatbotData = await chatbotState.GetUserDataAsync(
                    activity.ChannelId, activity.From.Id);

                Queue<PlayScore> scoreQueue =
                    chatbotData.GetProperty<Queue<PlayScore>>(property: "scores");

                if (scoreQueue == null)
                    return "Try typing Rock, Paper, or Scissors to play first.";

                int plays = scoreQueue.Count;
                int userWins = scoreQueue.Where(q => q.UserWin).Count();
                int chatbotWins = scoreQueue.Where(q => !q.UserWin).Count();

                int ties = chatbotData.GetProperty<int>(property: "ties");

                return $"Out of the last {plays} contests, " +
                       $"you scored {userWins} and " +
                       $"Chatbot scored {chatbotWins}. " +
                       $"You've also had {ties} ties since playing.";
            }
        }

        public async Task UpdateScoresAsync(Activity activity, bool userWin)
        {
            using (StateClient stateClient = activity.GetStateClient())
            {
                IBotState chatbotState = stateClient.BotState;
                BotData chatbotData = await chatbotState.GetUserDataAsync(
                    activity.ChannelId, activity.From.Id);

                Queue<PlayScore> scoreQueue =
                    chatbotData.GetProperty<Queue<PlayScore>>(property: "scores");

                if (scoreQueue == null)
                    scoreQueue = new Queue<PlayScore>();

                if (scoreQueue.Count >= 10)
                    scoreQueue.Dequeue();

                scoreQueue.Enqueue(new PlayScore { UserWin = userWin });

                chatbotData.SetProperty<Queue<PlayScore>>(property: "scores", data: scoreQueue);
                await chatbotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, chatbotData);
            }
        }

        public async Task<string> DeleteScoresAsync(Activity activity)
        {
            using (StateClient stateClient = activity.GetStateClient())
            {
                IBotState chatbotState = stateClient.BotState;

                await chatbotState.DeleteStateForUserAsync(activity.ChannelId, activity.From.Id);

                return "All scores deleted.";
            }
        }

        public async Task AddTieAsync(Activity activity)
        {
            using (StateClient stateClient = activity.GetStateClient())
            {
                IBotState chatbotState = stateClient.BotState;
                BotData chatbotData = await chatbotState.GetUserDataAsync(
                    activity.ChannelId, activity.From.Id);

                int ties = chatbotData.GetProperty<int>(property: "ties");

                chatbotData.SetProperty<int>(property: "ties", data: ++ties);

                await chatbotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, chatbotData);
            }
        }
    }
}