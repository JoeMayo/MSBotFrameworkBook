using Microsoft.Bot.Builder.FormFlow;
using System;
using WineBotLib;

namespace WineBotChain.Dialogs
{
    [Serializable]
    public class WineForm
    {
        public WineType WineType { get; set; }
        public RatingType Rating { get; set; }
        public StockingType InStock { get; set; }

        public IForm<WineForm> BuildForm()
        {
            return new FormBuilder<WineForm>()
                .Message(
                    "I have a few questions on your wine search. " +
                    "You can type \"help\" at any time for more info.")
                .Build();
        }
    }
}