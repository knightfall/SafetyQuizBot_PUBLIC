using System;
using System.Collections.Generic;

namespace SafetyQuizBot.EF
{
    public partial class ScoreBoard
    {
        public string SessionId { get; set; }
        public string Psid { get; set; }
        public int? Timestamp { get; set; }
        public int? CorrectTap { get; set; }
        public int? IncorrectTap { get; set; }
        public int? CorrectInput { get; set; }
        public int? IncorrectInput { get; set; }
        public int? PartialInput { get; set; }
        public string State { get; set; }
        public int? Tap { get; set; }
        public int? Input { get; set; }
    }
}
