using AdaptiveCards;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using MusicChatbot.Models;
using MusicChatbot.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MusicChatbot.Dialogs
{
    [Serializable]
    public class SearchDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var card = new AdaptiveCard();

            card.Body.AddRange(
                new List<CardElement>
                {
                    new Container
                    {
                        Items = BuildHeader()
                    },
                    new TextBlock { Text = "Query (max 200 chars):"},
                    new TextInput
                    {
                        Id = "query",
                        MaxLength = 200,
                        IsRequired = true,
                        Placeholder = "Query"
                    },
                    new TextBlock { Text = "Max Items (1 to 25):"},
                    new NumberInput
                    {
                        Id = "maxItems",
                        Min = 1,
                        Max = 25,
                        IsRequired = true
                    },
                    new TextBlock { Text = "Filters:"},
                    new ChoiceSet
                    {
                        Id = "filters",
                        Choices = BuildFilterChoices(),
                        IsRequired = false,
                        Style = ChoiceInputStyle.Compact
                    },
                    new TextBlock { Text = "Source:"},
                    new ChoiceSet
                    {
                        Id = "source",
                        Choices = BuildSourceChoices(),
                        IsMultiSelect = false,
                        IsRequired = false,
                        Style = ChoiceInputStyle.Expanded
                    }
                });

            card.Actions.Add(new SubmitAction
            {
                Title = "Search"
            });

            Activity reply = (context.Activity as Activity).CreateReply();
            reply.Attachments.Add(
                new Attachment()
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = card
                });

            await
                new ConnectorClient(new Uri(reply.ServiceUrl))
                    .Conversations
                    .SendToConversationAsync(reply);

            context.Wait(PerformSearchAsync);
        }

        async Task PerformSearchAsync(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            IMessageActivity activity = await result;

            string values = activity.Value?.ToString();
            var searchArgs = JsonConvert.DeserializeObject<SearchArguments>(values);

            var results = new SpotifyService().Search(searchArgs);

            context.Done(this);
        }

        List<Choice> BuildFilterChoices()
        {
            return new List<Choice>
            {
                new Choice
                {
                    Title = "artist",
                    Value = "artist"
                },
                new Choice
                {
                    Title = "track",
                    Value = "track"
                },
                new Choice
                {
                    Title = "playlist",
                    Value = "playlist"
                }
            };
        }

        List<Choice> BuildSourceChoices()
        {
            return new List<Choice>
            {
                new Choice
                {
                    Title = "catalog",
                    Value = "catalog"
                },
                new Choice
                {
                    Title = "collection",
                    Value = "collection"
                }
            };
        }

        List<CardElement> BuildHeader()
        {
            string contentUrl = new FileService().GetBinaryUrl("Smile.png");

            return new List<CardElement>
            {
                new ColumnSet
                {
                    Columns = new List<Column>
                    {
                        new Column
                        {
                            Items = new List<CardElement>
                            {
                                new TextBlock()
                                {
                                    Text = "Music Search",
                                    Size = TextSize.Large,
                                    Weight = TextWeight.Bolder
                                },
                                new TextBlock()
                                {
                                    Text = "Fill in form and click Search button.",
                                    Color = TextColor.Accent
                                }
                            }
                        },
                        new Column
                        {
                            Items = new List<CardElement>
                            {
                                new Image()
                                {
                                    Url = contentUrl
                                }
                            }
                        }
                    }
                }
            };
        }
    }
}