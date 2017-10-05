using Microsoft.Bot.Connector.DirectLine;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleChannel
{
    class Configure
    {
        public static async Task RefreshTokensAsync(
            Conversation conversation, DirectLineClient client, CancellationToken cancelToken)
        {
            const int ToMilliseconds = 1000;
            const int BeforeExpiration = 60000;

            var runTask = Task.Run(async () =>
            {
                try
                {
                    int millisecondsToRefresh =
                        ((int)conversation.ExpiresIn * ToMilliseconds) - BeforeExpiration;

                    while (true)
                    {
                        await Task.Delay(millisecondsToRefresh);

                        await client.Conversations.ReconnectToConversationAsync(
                            conversation.ConversationId,
                            Message.Watermark,
                            cancelToken);
                    }
                }
                catch (OperationCanceledException oce)
                {
                    Console.WriteLine(oce.Message);
                }
            });
            await Task.FromResult(0);
        }
    }
}
