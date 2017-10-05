using Microsoft.Bot.Connector.DirectLine;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleChannel
{
    class Program
    {
        static void Main() => new Program().MainAsync().GetAwaiter().GetResult();

        async Task MainAsync()
        {
            var cancelSource = new CancellationTokenSource();

            AuthenticationResults results =
                await Authenticate.StartConversationAsync(cancelSource.Token);

            await Listen.RetrieveMessagesAsync(results.Conversation, cancelSource);

            await Configure.RefreshTokensAsync(results.Conversation, results.Client, cancelSource.Token);

            await Prompt.GetUserInputAsync(results.Conversation, results.Client, cancelSource);
        }
    }
}
