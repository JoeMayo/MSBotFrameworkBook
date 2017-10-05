using System.Collections.Generic;

namespace AILib.Models
{
    public class QnAResponse
    {
        public List<QnAAnswer> Answers { get; set; }
    }

    public class QnAAnswer
    {
        public string Answer { get; set; }
        public double Score { get; set; }
    }
}
