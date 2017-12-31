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
    public class BrowseDialog : IDialog<object>
    {
        const string DoneCommand = "Done";
        List<GenreItem> genres = new List<GenreItem>();

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            genres = new SpotifyService().GetGenres();
            genres.Add(new GenreItem { Id = "Done", Name = "Done" });
            var genreNames = genres.Select(genre => genre.Name).ToList().ToList();

            PromptDialog.Choice(context, ResumeAfterGenreAsync, genreNames, "Which music category?");
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
                .CreateReply($"## Browsing Top 5 Tracks in {genre} genre");

            List<HeroCard> cards = GetHeroCardsForTracks(genre);
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

        List<HeroCard> GetHeroCardsForTracks(string genre)
        {
            var genreID =
                (from genreItem in genres
                 where genreItem.Name == genre
                 select genreItem.Id)
                .SingleOrDefault();

            List<Track> tracks = new SpotifyService().GetTracks(genreID);

            var cards =
                (from track in tracks
                 let artists =
                     string.Join(", ",
                        from artist in track.Artists
                        select artist.Name)
                 select new HeroCard
                 {
                     Title = track.Name,
                     Subtitle = artists,
                     Images = new List<CardImage>
                     {
                         new CardImage
                         {
                             Alt = track.Name,
                             Tap = BuildBuyCardAction(track),
                             Url = track.Album.Images.FirstOrDefault()?.Url ??
                                new FileService().GetBinaryUrl("Smile.png")
                         }
                     },
                     Buttons = new List<CardAction>
                     {
                         BuildBuyCardAction(track)
                     }
                 })
                .ToList();
            return cards;
        }

        CardAction BuildBuyCardAction(Track track)
        {
            return new CardAction
            {
                Type = ActionTypes.OpenUrl,
                Title = "Buy",
                Value = track.Uri
            };
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