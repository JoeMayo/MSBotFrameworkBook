using AILib;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using MusicChatbot.Models;
using MusicChatbot.Services;
using Newtonsoft.Json;
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
        const string NewsCommand = "News";
        List<GenreItem> genres = new List<GenreItem>();

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as IMessageActivity;
            string heroCardValue = activity?.Text;
            if (!string.IsNullOrWhiteSpace(heroCardValue) && heroCardValue.StartsWith("{"))
            {
                var news = JsonConvert.DeserializeObject<Track>(heroCardValue);
                string artistName = news.Artists.First().Name;
                await context.Forward(
                    new NewsDialog(artistName),
                    MessageReceivedAsync,
                    activity);
            }
            else
            {
                genres = new SpotifyService().GetGenres();
                genres.Add(new GenreItem { Id = "Done", Name = "Done" });
                var genreNames = genres.Select(genre => genre.Name).ToList().ToList();

                PromptDialog.Choice(context, ResumeAfterGenreAsync, genreNames, "Which music category?");
            }
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

            List<HeroCard> cards = await GetHeroCardsForTracksAsync(genre);
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

        async Task<List<HeroCard>> GetHeroCardsForTracksAsync(string genre)
        {
            var genreID =
                (from genreItem in genres
                 where genreItem.Name == genre
                 select genreItem.Id)
                .SingleOrDefault();

            List<Track> tracks = new SpotifyService().GetTracks(genreID);

            var cogSvc = new CognitiveService();

            foreach (var track in tracks)
                track.ImageAnalysis = await cogSvc.AnalyzeImageAsync(
                    track.Album.Images.FirstOrDefault()?.Url ?? new FileService().GetBinaryUrl("Smile.png"));

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
                     Text = track.ImageAnalysis.Description?.Captions?.First()?.Text ?? "No Description",
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
                         BuildBuyCardAction(track),
                         BuildNewsCardAction(track)
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

        CardAction BuildNewsCardAction(Track track)
        {
            return new CardAction
            {
                Type = ActionTypes.PostBack,
                Title = NewsCommand,
                Value = JsonConvert.SerializeObject(track)
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