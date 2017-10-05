using Microsoft.Bot.Connector.DirectLine;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleChannel
{
    class Listen
    {
        public static async Task RetrieveMessagesAsync(
            Conversation conversation, CancellationTokenSource cancelSource)
        {
            const int ReceiveChunkSize = 1024;

            var webSocket = new ClientWebSocket();
            await webSocket.ConnectAsync(
                new Uri(conversation.StreamUrl), cancelSource.Token);

            var runTask = Task.Run(async () =>
            {
                try
                {
                    while (webSocket.State == WebSocketState.Open)
                    {
                        var allBytes = new List<byte>();
                        var result = new WebSocketReceiveResult(0, WebSocketMessageType.Text, false);
                        byte[] buffer = new byte[ReceiveChunkSize];

                        while (!result.EndOfMessage)
                        {
                            result = await webSocket.ReceiveAsync(
                                new ArraySegment<byte>(buffer), cancelSource.Token);

                            allBytes.AddRange(buffer);
                            buffer = new byte[ReceiveChunkSize];
                        }

                        string message = Encoding.UTF8.GetString(allBytes.ToArray()).Trim();
                        ActivitySet activitySet = JsonConvert.DeserializeObject<ActivitySet>(message);

                        if (activitySet != null)
                            Message.Watermark = activitySet.Watermark;

                        List<Activity> activities;
                        if (CanDisplayMessage(message, activitySet, out activities))
                        {
                            Console.WriteLine();
                            activities.ForEach(activity => Console.WriteLine(activity.Text));
                            Message.WritePrompt();
                        }
                    }
                }
                catch (OperationCanceledException oce)
                {
                    Console.WriteLine(oce.Message);
                }
            });
        }

        static bool CanDisplayMessage(string message, ActivitySet activitySet, out List<Activity> activities)
        {
            if (activitySet == null)
                activities = new List<Activity>();
            else
                activities =
                    (from activity in activitySet.Activities
                     where activity.From.Id == Message.ChatbotID &&
                           !string.IsNullOrWhiteSpace(activity.Text)
                     select activity)
                    .ToList();

            SuppressRepeatedActivities(activities);

            return !string.IsNullOrWhiteSpace(message) && activities.Any();
        }

        static Queue<string> processedActivities = new Queue<string>();
        const int MaxQueueSize = 10;

        static void SuppressRepeatedActivities(List<Activity> activities)
        {
            foreach (var activity in activities)
            {
                if (processedActivities.Contains(activity.Id))
                {
                    activities.Remove(activity);
                }
                else
                {
                    if (processedActivities.Count >= 10)
                        processedActivities.Dequeue();

                    processedActivities.Enqueue(activity.Id);
                }
            };
        }
    }
}
