using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Web.Hosting;

namespace WineBot
{
    [Serializable]
    class WineSearchDialog : IDialog<object>
    {
        public Refinement[] WineCategories { get; set; }
        public string WineType { get; set; }
        public long Rating { get; set; }
        public bool InStock { get; set; }
        public string SearchTerms { get; private set; }

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        async Task MessageReceivedAsync(
            IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            var activity = await result;

            if (activity.Text.Contains("catalog"))
            {
                WineCategories = await new WineApi().GetWineCategoriesAsync();
                var categoryNames = WineCategories.Select(c => c.Name).ToList();

                PromptDialog.Choice(
                    context: context, 
                    resume: WineTypeReceivedAsync, 
                    options: categoryNames,
                    prompt: "Which type of wine?",
                    retry: "Please select a valid wine type: ",
                    attempts: 4,
                    promptStyle: PromptStyle.PerLine);
            }
            else
            {
                await context.PostAsync(
                    "Currently, the only thing I can do is search the catalog. " +
                    "Type \"catalog\" if you would like to do that");
            }
        }

        async Task WineTypeReceivedAsync(
            IDialogContext context, IAwaitable<string> result)
        {
            WineType = await result;

            PromptDialog.Number(
                context: context,
                resume: RatingReceivedAsync,
                prompt: "What is the minimum rating?",
                retry: "Please enter a number between 1 and 100.",
                attempts: 4);
        }

        async Task RatingReceivedAsync(
            IDialogContext context, IAwaitable<long> result)
        {
            Rating = await result;

            PromptDialog.Confirm(
                context: context,
                resume: InStockReceivedAsync,
                prompt: "Show only wines in stock?",
                retry: "Please reply with either Yes or No.");
        }

        async Task InStockReceivedAsync(
            IDialogContext context, IAwaitable<bool> result)
        {
            InStock = await result;

            PromptDialog.Text(
                context: context, 
                resume: SearchTermsReceivedAsync, 
                prompt: "Which search terms (type \"none\" if you don't want to add search terms)?");
        }


        async Task SearchTermsReceivedAsync(
            IDialogContext context, IAwaitable<string> result)
        {
            SearchTerms = (await result)?.Trim().ToLower() ?? "none";

            PromptDialog.Confirm(
                context: context,
                resume: UploadConfirmedReceivedAsync,
                prompt: "Would you like to upload your favorite wine image?",
                retry: "Please reply with either Yes or No.");
        }

        async Task UploadConfirmedReceivedAsync(
            IDialogContext context, IAwaitable<bool> result)
        {
            bool shouldUpload = await result;

            if (shouldUpload)
                PromptDialog.Attachment(
                    context: context,
                    resume: AttachmentReceivedAsync,
                    prompt: "Please upload your image.");
            else
                await DoSearchAsync(context);
        }

        async Task AttachmentReceivedAsync(
            IDialogContext context, IAwaitable<IEnumerable<Attachment>> result)
        {
            Attachment attachment = (await result).First();

            byte[] imageBytes = 
                await new WineApi().GetUserImageAsync(attachment.ContentUrl);

            string hostPath = HostingEnvironment.MapPath(@"~/");
            string imagePath = Path.Combine(hostPath, "images");
            if (!Directory.Exists(imagePath))
                Directory.CreateDirectory(imagePath);

            string fileName = context.Activity.From.Name;
            string extension = Path.GetExtension(attachment.Name);
            string filePath = Path.Combine(imagePath, $"{fileName}{extension}");

            File.WriteAllBytes(filePath, imageBytes);

            await DoSearchAsync(context);
        }

        async Task DoSearchAsync(IDialogContext context)
        {
            await context.PostAsync(
                $"You selected Wine Type: {WineType}, " +
                $"Rating: {Rating}, " +
                $"In Stock: {InStock}, and " +
                $"Search Terms: {SearchTerms}");

            int wineTypeID =
                (from cat in WineCategories
                 where cat.Name == WineType
                 select cat.Id)
                .FirstOrDefault();

            List[] wines = 
                await new WineApi().SearchAsync(
                    wineTypeID, Rating, InStock, SearchTerms);

            string message;

            if (wines.Any())
                message = "Here are the top matching wines: " + 
                          string.Join(", ", wines.Select(w => w.Name));
            else
                message = "Sorry, No wines found matching your criteria.";

            await context.PostAsync(message);

            context.Wait(MessageReceivedAsync);
        }
    }
}
