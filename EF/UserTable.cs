using System;
using System.Collections.Generic;

namespace SafetyQuizBot.EF
{
    public partial class UserTable
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string ProfilePic { get; set; }
        public string Email { get; set; }
        public string Verified { get; set; }
    }
    public class JwtModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
    }
}
