using Microsoft.Bot.Connector;

namespace RockPaperScissors3.Models
{
    public static class ActivityExtensions
    {
        public static Activity BuildMessageActivity(
            this Activity userActivity, string message, string locale = "en-US")
        {
            IMessageActivity replyActivity = new Activity(ActivityTypes.Message)
            {
                From = new ChannelAccount
                {
                    Id = userActivity.Recipient.Id,
                    Name = userActivity.Recipient.Name
                },
                Recipient = new ChannelAccount
                {
                    Id = userActivity.From.Id,
                    Name = userActivity.From.Name
                },
                Conversation = new ConversationAccount
                {
                    Id = userActivity.Conversation.Id,
                    Name = userActivity.Conversation.Name,
                    IsGroup = userActivity.Conversation.IsGroup
                },
                ReplyToId = userActivity.Id,
                Text = message,
                Locale = locale
            };

            return (Activity)replyActivity;
        }
    }
}