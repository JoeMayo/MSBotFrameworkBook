using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Resource;
using WineBotLib;

namespace WineBotConfiguration
{
    [Serializable]
    public class WineForm
    {
        public WineType WineType { get; set; }
        public RatingType Rating { get; set; }
        public StockingType InStock { get; set; }

        public IForm<WineForm> BuildForm()
        {
            var builder = new FormBuilder<WineForm>();
            ConfigureFormBuilder(builder);

            return builder
                .Message(
                    "I have a few questions on your wine search. " +
                    "You can type \"help\" at any time for more info.")
                .OnCompletion(DoSearch)
                .Build();
        }

        void ConfigureFormBuilder(FormBuilder<WineForm> builder)
        {
            FormConfiguration buildConfig = builder.Configuration;

            buildConfig.Yes = "Yes;y;sure;ok;yep;1;good".SplitList();

            TemplateAttribute tmplAttr = buildConfig.Template(TemplateUsage.EnumSelectOne);
            tmplAttr.Patterns = new[] {"What {&} would you like? {||}"};

            buildConfig.Commands[FormCommand.Quit].Help =
                "Quit: Quit the form without completing it. " +
                "Warning - this will clear your previous choices!";
        }

        async Task DoSearch(IDialogContext context, WineForm wineInfo)
        {
            List[] wines =
                await new WineApi().SearchAsync(
                    (int)WineType,
                    (int)Rating,
                    InStock == StockingType.InStock,
                    "");

            string message;

            if (wines.Any())
                message = "Here are the top matching wines: " +
                          string.Join(", ", wines.Select(w => w.Name));
            else
                message = "Sorry, No wines found matching your criteria.";

            await context.PostAsync(message);
        }
    }
}