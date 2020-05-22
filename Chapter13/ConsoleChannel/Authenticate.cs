using Microsoft.Bot.Connector.DirectLine;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleChannel
{
    class AuthenticationResults
    {
        public Conversation Conversation { get; set; }
        public DirectLineClient Client { get; set; }
    }

    class Authenticate
    {
        public static async Task<AuthenticationResults> 
            StartConversationAsync(CancellationToken cancelToken)
        {
            System.Console.WriteLine(
                "\nConsole Channel Started\n" +
                "Type \"/exit\" to end the program\n");
            Message.WritePrompt();

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            string secret = ConfigurationManager.AppSettings["DirectLineSecretKey"];
            var client = new DirectLineClient(secret);
            Conversation conversation =
                await client.Conversations.StartConversationAsync(cancelToken);

            return 
                new AuthenticationResults
                {
                    Conversation = conversation,
                    Client = client
                };
        }
    }
}
