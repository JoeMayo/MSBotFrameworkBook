using System;

namespace ConsoleChannel
{
    static class Message
    {
        public const string ClientID = "ConsoleChannel";
        public const string ChatbotID = "WineChatbot";

        volatile static string watermark;

        public static string Watermark
        {
            get { return watermark; }
            set { watermark = value; }
        }

        public static void WritePrompt() => Console.Write("Console Channel> ");
    }
}
