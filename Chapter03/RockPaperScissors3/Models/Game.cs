using System;
using System.Collections.Generic;

namespace RockPaperScissors3.Models
{
    public class Game
    {
        readonly Dictionary<PlayType, string> rockPlays =
            new Dictionary<PlayType, string>
            {
                [PlayType.Paper] = "Paper covers rock - You lose!",
                [PlayType.Scissors] = "Rock crushes scissors - You win!"
            };
        readonly Dictionary<PlayType, string> paperPlays =
            new Dictionary<PlayType, string>
            {
                [PlayType.Rock] = "Paper covers rock - You win!",
                [PlayType.Scissors] = "Scissors cuts paper - You lose!"
            };
        readonly Dictionary<PlayType, string> scissorsPlays =
            new Dictionary<PlayType, string>
            {
                [PlayType.Rock] = "Rock crushes scissors - You lose!",
                [PlayType.Paper] = "Scissors cut paper - You win!"
            };

        public string Play(string userText)
        {
            string message = "";

            PlayType userPlay;
            bool isValidPlay = Enum.TryParse(
                userText, ignoreCase: true, result: out userPlay);

            if (isValidPlay)
            {
                PlayType botPlay = GetBotPlay();
                message = Compare(userPlay, botPlay);
            }
            else
            {
                message = "Type \"Rock\", \"Paper\", or \"Scissors\" to play.";
            }

            return message;
        }

        public PlayType GetBotPlay()
        {
            long seed = DateTime.Now.Ticks;
            var rnd = new Random(unchecked((int)seed));
            int position = rnd.Next(maxValue: 3);

            return (PlayType) position;
        }

        public string Compare(PlayType userPlay, PlayType botPlay)
        {
            string plays = $"You: {userPlay}, Bot: {botPlay}";
            string result = "";

            if (userPlay == botPlay)
                result = "Tie.";
            else
                switch (userPlay)
                {
                    case PlayType.Rock:
                        result = rockPlays[botPlay];
                        break;
                    case PlayType.Paper:
                        result = paperPlays[botPlay];
                        break;
                    case PlayType.Scissors:
                        result = scissorsPlays[botPlay];
                        break;
                }

            return $"{plays}. {result}";
        }
    }
}