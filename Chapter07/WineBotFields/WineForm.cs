using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Bot.Builder.FormFlow.Advanced;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WineBotLib;

namespace WineBotFields
{
    [Serializable]
    public class WineForm
    {
        public string WineType { get; set; }
        public int Rating { get; set; }
        public StockingType InStock { get; set; }
        public int Vintage { get; set; }
        public string SearchTerms { get; set; }

        public Refinement[] WineCategories { get; set; }

        public IForm<WineForm> BuildForm()
        {
            var form = new FormBuilder<WineForm>()
                .Message("Welcome to WineBot!")
                .Field(nameof(InStock), wineForm => DateTime.Now.DayOfWeek == DayOfWeek.Wednesday)
                .Field(new FieldReflector<WineForm>(nameof(WineType))
                    .SetType(null)
                    .SetFieldDescription("Type of Wine")
                    .SetDefine(async (wineForm, field) =>
                    {
                        foreach (var category in WineCategories)
                            field
                                .AddDescription(category.Name, category.Name)
                                .AddTerms(category.Name, Language.GenerateTerms(category.Name, 6));

                        return await Task.FromResult(true);
                    }))
                .Field(
                    name: nameof(Rating),
                    prompt: new PromptAttribute("What is your preferred {&} (1 to 100)?"),
                    active: wineForm => true,
                    validate: async (wineForm, response) =>
                    {
                        var result = new ValidateResult { IsValid = true, Value = response };

                        result.IsValid =
                            int.TryParse(response.ToString(), out int rating) &&
                            rating > 0 && rating <= 100;

                        if (!result.IsValid)
                        {
                            //result.FeedbackCard =
                            //    new FormPrompt
                            //    {
                            //        Prompt = $"'{response}' isn't a valid option!",
                            //        Buttons =
                            //            new List<DescribeAttribute>
                            //            {
                            //                new DescribeAttribute("25"),
                            //                new DescribeAttribute("50"),
                            //                new DescribeAttribute("75")
                            //            }
                            //    };
                            result.Feedback = $"'{response}' isn't a valid option!";
                            result.Choices =
                                new List<Choice>
                                {
                                    new Choice
                                    {
                                        Description = new DescribeAttribute("25"),
                                        Value = 25,
                                        Terms = new TermsAttribute("25")
                                    },
                                     new Choice
                                    {
                                        Description = new DescribeAttribute("50"),
                                        Value = 50,
                                        Terms = new TermsAttribute("50")
                                    },
                                    new Choice
                                    {
                                        Description = new DescribeAttribute("75"),
                                        Value = 75,
                                        Terms = new TermsAttribute("75")
                                    }
                               };
                        }

                        return await Task.FromResult(result);
                    })
                .AddRemainingFields(new[] { nameof(Vintage) });

            if (!form.HasField(nameof(Vintage)))
                form.Field(nameof(Vintage));

            form.OnCompletion(DoSearch);

            return form.Build();
        }

        async Task DoSearch(IDialogContext context, WineForm wineInfo)
        {
            int wineType =
                (from refinement in WineCategories
                 where refinement.Name == wineInfo.WineType
                 select refinement.Id)
                .SingleOrDefault();

            List[] wines =
                await new WineApi().SearchAsync(
                    wineType,
                    wineInfo.Rating,
                    wineInfo.InStock == StockingType.InStock,
                    wineInfo.SearchTerms);

            string message;

            if (wines.Any())
                message = "Here are the top matching wines: " +
                          string.Join(", ", wines.Select(w => w.Name));
            else
                message = "Sorry, No wines found matching your criteria.";

            await context.PostAsync(message);

            context.EndConversation(EndOfConversationCodes.CompletedSuccessfully);
        }
    }
}