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
using AILib;
using AILib.Models;
using System.Web;

namespace WineBotLuis.Dialogs
{
    [LuisModel(
        modelID: "",
        subscriptionKey: "")]
    [Serializable]
    public class WineBotDialog : LuisDialog<object>
    {
        public const string ExampleText = @"
Here are a couple examples that I can recognize: 
'What type of red wine do you have with a rating of 70?' or
'Please search for champaigne.'";

        readonly CognitiveService cogSvc = new CognitiveService();

        [LuisIntent("")]
        public async Task NoneIntent(IDialogContext context, LuisResult result)
        {
            QnAAnswer qnaAnswer = await cogSvc.AskWineChatbotFaqAsync(result.Query);

            string message =
                qnaAnswer.Score == 0 ?
                    @"Sorry, I didn't get that. " + ExampleText :
                    HttpUtility.HtmlDecode(qnaAnswer.Answer);

            message = await TranslateResponseAsync(context, message);

            await context.PostAsync(message);
            context.Wait(MessageReceived);
        }

        [LuisIntent("Searching")]
        public async Task SearchingIntent(IDialogContext context, LuisResult result)
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

            string message = wines.Any() ?
                "Here are the top matching wines" :
                "Sorry, No wines found matching your criteria.";

            message = await TranslateResponseAsync(context, message);

            if (wines.Any())
                message = 
                    $"## {message}:\n" +
                    $"{ string.Join("\n", wines.Select(w => $"* {w.Name}"))}";

            await context.PostAsync(message);

            context.Wait(MessageReceived);
        }

        void ExtractEntities(LuisResult result, out int wineCategory, out int rating)
        {
            const string RatingEntity = "builtin.number";
            const string WineTypeEntity = "WineType";

            rating = 1;
            EntityRecommendation ratingEntityRec;
            result.TryFindEntity(RatingEntity, out ratingEntityRec);
            if (ratingEntityRec?.Resolution != null)
                int.TryParse(ratingEntityRec.Resolution["value"] as string, out rating);

            wineCategory = 0;
            EntityRecommendation wineTypeEntityRec;
            result.TryFindEntity(WineTypeEntity, out wineTypeEntityRec);

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

        async Task<string> TranslateResponseAsync(IDialogContext context, string message)
        {
            var activity = context.Activity as IMessageActivity;
            if (!activity.Locale.StartsWith("en"))
                message = await cogSvc.TranslateTextAsync(message, activity.Locale);
            return message;
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