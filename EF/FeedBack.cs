using System;
using System.Collections.Generic;

namespace SafetyQuizBot.EF
{
    public partial class FeedBack
    {
        public string FeedbackId { get; set; }
        public string UserId { get; set; }
        public string Comment { get; set; }
    }
}
