using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Builder.Luis;
using WineBotLib;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.Bot.Connector;

namespace WineBotLuis.Dialogs
{
    [LuisModel(
        modelID: "",
        subscriptionKey: "")]
    [Serializable]
    public class WineBotDialog : LuisDialog<object>
    {
        [LuisIntent("")]
        public async Task NoneIntent(IDialogContext context, LuisResult result)
        {
            string message = @"
Sorry, I didn't get that. 
Here are a couple examples that I can recognize: 
'What type of red wine do you have with a rating of 70?' or
'Please search for champaigne.'";

            await context.PostAsync(message);
            context.Wait(MessageReceived);
        }

        [LuisIntent("Searching")]
        public async Task SearchingIntent(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            if (!result.Entities.Any())
            {
                await NoneIntent(context, result);
                return;
            }

            int wineCategory;
            int rating;
            ExtractEntities(result, out wineCategory, out rating);

            var wines = await new WineApi().SearchAsync(
                wineCategory, rating, inStock: true, searchTerms: string.Empty);
            string message;

            if (wines.Any())
                message = "Here are the top matching wines: " +
                          string.Join(", ", wines.Select(w => w.Name));
            else
                message = "Sorry, No wines found matching your criteria.";

            await context.PostAsync(message);

            context.Wait(MessageReceived);
        }

        void ExtractEntities(LuisResult result, out int wineCategory, out int rating)
        {
            const string RatingEntity = "builtin.number";
            const string WineTypeEntity = "WineType";

            rating = 1;
            result.TryFindEntity(RatingEntity, out EntityRecommendation ratingEntityRec);
            if (ratingEntityRec?.Resolution != null)
                int.TryParse(ratingEntityRec.Resolution["value"] as string, out rating);

            wineCategory = 0;
            result.TryFindEntity(WineTypeEntity, out EntityRecommendation wineTypeEntityRec);

            if (wineTypeEntityRec != null)
            {
                string wineType = wineTypeEntityRec.Entity;

                wineCategory =
                    (from wine in WineTypeTable.Keys
                     let matches = new Regex(WineTypeTable[wine]).Match(wineType)
                     where matches.Success
                     select (int)wine)
                    .FirstOrDefault();
            }
        }

        Dictionary<WineType, string> WineTypeTable =
            new Dictionary<WineType, string>
            {
                [WineType.ChampagneAndSparkling] = "champaign and sparkling|champaign|sparkling",
                [WineType.DessertSherryAndPort] = "dessert sherry and port|desert|sherry|port",
                [WineType.RedWine] = "red wine|red|reds|cabernet|merlot",
                [WineType.RoseWine] = "rose wine|rose",
                [WineType.Sake] = "sake",
                [WineType.WhiteWine] = "white wine|white|whites|chardonnay"
            };
    }
}