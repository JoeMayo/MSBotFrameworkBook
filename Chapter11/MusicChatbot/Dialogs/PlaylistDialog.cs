using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using MusicChatbot.Models;
using MusicChatbot.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MusicChatbot.Dialogs
{
    [Serializable]
    public class PlaylistDialog : IDialog<object>
    {
        const string DoneCommand = "Done";

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            List<string> genres = new GrooveService().GetGenres();
            genres.Add("Done");
            PromptDialog.Choice(context, ResumeAfterGenreAsync, genres, "Which music category?");
            return Task.CompletedTask;
        }

        async Task ResumeAfterGenreAsync(IDialogContext context, IAwaitable<string> result)
        {
            string genre = await result;

            if (genre == DoneCommand)
            {
                context.Done(this);
                return;
            }

            var reply = (context.Activity as Activity)
                .CreateReply($"## Viewing Top 5 Tracks in {genre} genre");

            List<AudioCard> cards = GetAudioCardsForPreviews(genre);
            cards.ForEach(card =>
                reply.Attachments.Add(card.ToAttachment()));

            ThumbnailCard doneCard = GetThumbnailCardForDone();
            reply.Attachments.Add(doneCard.ToAttachment());

            reply.AttachmentLayout = AttachmentLayoutTypes.Carousel;

            await
                new ConnectorClient(new Uri(reply.ServiceUrl))
                    .Conversations
                    .SendToConversationAsync(reply);

            context.Wait(MessageReceivedAsync);
        }

        ThumbnailCard GetThumbnailCardForDone()
        {
            return new ThumbnailCard
            {
                Title = DoneCommand,
                Subtitle = "Click/Tap to exit",
                Images = new List<CardImage>
                {
                    new CardImage
                    {
                        Alt = "Smile",
                        Tap = BuildDoneCardAction(),
                        Url = new FileService().GetBinaryUrl("Smile.png")
                    }
                },
                Buttons = new List<CardAction>
                {
                    BuildDoneCardAction()
                }
            };
        }

        List<AudioCard> GetAudioCardsForPreviews(string genre)
        {
            var grooveSvc = new GrooveService();
            List<Item> tracks = grooveSvc.GetTracks(genre);

            var cards =
                (from track in tracks
                 let artists =
                     string.Join(", ",
                        from artist in track.Artists
                        select artist.Artist.Name)
                 let preview = grooveSvc.GetPreview(track.Id)
                 select new AudioCard
                 {
                     Title = track.Name,
                     Subtitle = artists,
                     Media = new List<MediaUrl>
                     {
                         new MediaUrl(preview.Url)
                     }
                 })
                .ToList();

            return cards;
        }

        CardAction BuildDoneCardAction()
        {
            return new CardAction
            {
                Type = ActionTypes.PostBack,
                Title = DoneCommand,
                Value = DoneCommand
            };
        }
    }
}