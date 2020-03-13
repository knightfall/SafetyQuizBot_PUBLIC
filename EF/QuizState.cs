using System;
using System.Collections.Generic;

namespace SafetyQuizBot.EF
{
    public partial class QuizState
    {
        public string Uid { get; set; }
        public int? LastState { get; set; }
        public string CurrentContext { get; set; }
    }
}
