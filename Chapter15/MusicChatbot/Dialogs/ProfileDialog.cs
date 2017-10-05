using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using MusicChatbot.Models;
using MusicChatbot.Services;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace MusicChatbot.Dialogs
{
    [Serializable]
    public class ProfileDialog : IDialog<object>
    {
        public string Name { get; set; }
        public byte[] Image { get; set; }

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            ShowMainMenu(context);
            return Task.CompletedTask;
        }

        void ShowMainMenu(IDialogContext context)
        {
            var options = Enum.GetValues(typeof(ProfileMenuItem)).Cast<ProfileMenuItem>().ToArray();

            string promptMessage = "What would you like to do?";
            string retryMessage = "I don't know about that option, please select an item in the list.";

            var promptOptions =
                new PromptOptions<ProfileMenuItem>(
                    prompt: promptMessage,
                    retry: retryMessage,
                    options: options,
                    speak: promptMessage,
                    retrySpeak: retryMessage);

            PromptDialog.Choice(
                context: context,
                resume: ResumeAfterChoiceAsync,
                promptOptions: promptOptions);
        }

        async Task ResumeAfterChoiceAsync(IDialogContext context, IAwaitable<ProfileMenuItem> result)
        {
            ProfileMenuItem choice = await result;

            switch (choice)
            {
                case ProfileMenuItem.Display:
                    await DisplayAsync(context);
                    break;
                case ProfileMenuItem.Update:
                    await UpdateAsync(context);
                    break;
                case ProfileMenuItem.Done:
                default:
                   context.Done(this);
                    break;
            }
        }

        Task UpdateAsync(IDialogContext context)
        {
            PromptDialog.Text(context, ResumeAfterNameAsync, "What is your name?");
            return Task.CompletedTask;
        }

        async Task ResumeAfterNameAsync(IDialogContext context, IAwaitable<string> result)
        {
            Name = await result;
            string message = "Please upload your profile image.";
            await context.SayAsync(text: message, speak: message, 
                options: new MessageOptions
                {
                    InputHint = InputHints.AcceptingInput
                });
            context.Wait(UploadAsync);
        }

        async Task UploadAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;

            if (activity.Attachments.Any())
            {
                Attachment userImage = activity.Attachments.First();
                Image = await new HttpClient().GetByteArrayAsync(userImage.ContentUrl);

                StateClient stateClient = activity.GetStateClient();
                BotData userData = await stateClient.BotState.GetUserDataAsync(activity.ChannelId, activity.From.Id);
                userData.SetProperty(nameof(Name), Name);
                userData.SetProperty(nameof(Image), Image);
                await stateClient.BotState.SetUserDataAsync(activity.ChannelId, activity.From.Id, userData);
            }
            else
            {
                string message = "Sorry, I didn't see an image in the attachment.";
                await context.SayAsync(text: message, speak: message,
                    options: new MessageOptions
                    {
                        InputHint = InputHints.IgnoringInput
                    });
            }

            ShowMainMenu(context);
        }

        async Task DisplayAsync(IDialogContext context)
        {
            Activity activity = context.Activity as Activity;

            StateClient stateClient = activity.GetStateClient();
            BotData userData = 
                await stateClient.BotState.GetUserDataAsync(
                    activity.ChannelId, activity.From.Id);

            if ((userData.Data as JObject)?.HasValues ?? false)
            {
                string name = userData.GetProperty<string>(nameof(Name));

                await context.SayAsync(text: name, speak: name);

                byte[] image = userData.GetProperty<byte[]>(nameof(Image));

                var fileSvc = new FileService();
                string imageName = $"{context.Activity.From.Id}_Image.png";

                string imageFilePath = fileSvc.GetFilePath(imageName);
                File.WriteAllBytes(imageFilePath, image);

                string contentUrl = fileSvc.GetBinaryUrl(imageName);
                var agenda = new Attachment("image/png", contentUrl, imageName);

                Activity reply = activity.CreateReply();
                reply.Attachments.Add(agenda);

                await
                    new ConnectorClient(new Uri(reply.ServiceUrl))
                        .Conversations
                        .SendToConversationAsync(reply);
            }
            else
            {
                string message = "Profile not available. Please update first.";
                await context.SayAsync(text: message, speak: message);
            }

            ShowMainMenu(context);
        }
    }
}