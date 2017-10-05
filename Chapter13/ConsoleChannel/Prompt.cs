using Microsoft.Bot.Connector.DirectLine;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleChannel
{
    class Prompt
    {
        public static async Task GetUserInputAsync(
            Conversation conversation, DirectLineClient client, CancellationTokenSource cancelSource)
        {
            string input = null;

            try
            {
                while (true)
                {
                    input = Console.ReadLine().Trim().ToLower();

                    if (input == "/exit")
                    {
                        await EndConversationAsync(conversation, client);
                        cancelSource.Cancel();
                        await Task.Delay(500);
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(input))
                    {
                        Message.WritePrompt();
                    }
                    else
                    {
                        IMessageActivity activity = Activity.CreateMessageActivity();
                        activity.From = new ChannelAccount(Message.ClientID);
                        activity.Text = input;

                        await client.Conversations.PostActivityAsync(
                            conversation.ConversationId,
                            activity as Activity,
                            cancelSource.Token);
                    }
                }
            }
            catch (OperationCanceledException oce)
            {
                Console.WriteLine(oce.Message);
            }
        }

        static async Task EndConversationAsync(Conversation conversation, DirectLineClient client)
        {
            IEndOfConversationActivity activity = Activity.CreateEndOfConversationActivity();
            activity.From = new ChannelAccount(Message.ClientID);

            await client.Conversations.PostActivityAsync(
                conversation.ConversationId, activity as Activity);
        }
    }
}
