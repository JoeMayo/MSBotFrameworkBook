using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.IO;

namespace RockPaperScissorsNotifier1
{
    class Program
    {
        public static string MicrosoftAppId { get; set; }
            = ConfigurationManager.AppSettings["MicrosoftAppId"];
        public static string MicrosoftAppPassword { get; set; }
            = ConfigurationManager.AppSettings["MicrosoftAppPassword"];

        static void Main()
        {
            ConversationReference convRef = GetConversationReference();

            var serviceUrl = new Uri(convRef.ServiceUrl);

            var connector = new ConnectorClient(serviceUrl, MicrosoftAppId, MicrosoftAppPassword);

            Console.Write(value: "Choose 1 for existing conversation or 2 for new conversation: ");
            ConsoleKeyInfo response = Console.ReadKey();

            if (response.KeyChar == '1')
                SendToExistingConversation(convRef, connector.Conversations);
            else
                StartNewConversation(convRef, connector.Conversations);
        }

        static void SendToExistingConversation(ConversationReference convRef, IConversations conversations)
        {
            var existingConversationMessage = convRef.GetPostToUserMessage();
            existingConversationMessage.Text = 
                $"Hi, I've completed that long-running job and emailed it to you.";

            conversations.SendToConversation(existingConversationMessage);
        }

        static void StartNewConversation(ConversationReference convRef, IConversations conversations)
        {
            ConversationResourceResponse convResponse = 
                conversations.CreateDirectConversation(convRef.Bot, convRef.User);

            var notificationMessage = convRef.GetPostToUserMessage();
            notificationMessage.Text = 
                $"Hi, I haven't heard from you in a while. Want to play?";
            notificationMessage.Conversation = new ConversationAccount(id: convResponse.Id);

            conversations.SendToConversation(notificationMessage);
        }

        static ConversationReference GetConversationReference()
        {
            string convRefJson = File.ReadAllText(path: @"..\..\ConversationReference.json");
            ConversationReference convRef = JsonConvert.DeserializeObject<ConversationReference>(convRefJson);

            return convRef;
        }
    }
}
