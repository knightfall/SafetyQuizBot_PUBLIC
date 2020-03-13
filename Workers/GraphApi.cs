using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SafetyQuizBot.Models;
using SafetyQuizBot.EF;
using SendGrid;
using SendGrid.Helpers.Mail;
using Trivial.Security;

namespace SafetyQuizBot.Workers
{
    public class GraphApi
    {
        private readonly string QuizbotAccessToken = "INSERT_TOKEN_HERE";
       

        private HttpClient client = new HttpClient();
        public async Task<ProfileInfo> GetPageProfileAsync(string ID)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri =
                    new Uri(
                        $"https://graph.facebook.com/{ID}?fields=first_name,last_name,profile_pic&access_token={QuizbotAccessToken}"),
                Headers =
                {
                    {"Origin", "INSERT_ORIGIN_HERE"},
                    {HttpRequestHeader.AcceptEncoding.ToString(), "gzip, deflate, br"},
                    {HttpRequestHeader.AcceptLanguage.ToString(), "en-US,en;q=0.5"},
                    {HttpRequestHeader.ContentType.ToString(), "application/json"}
                }
            };
            //  var client = _clientFactory.CreateClient();
            var t = await client.SendAsync(request).Result.Content.ReadAsStringAsync();
            var xt = JsonConvert.DeserializeObject<ProfileInfo>(t);
            return xt;
        }

        public void SendMessage(string response)
        {
            Stopwatch stop = new Stopwatch();
            stop.Start();
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri =
                    new Uri(
                        $"https://graph.facebook.com/v3.3/me/messages?access_token={QuizbotAccessToken}"),
                Headers =
                {
                    {"Origin", "https://beta.qbot.knightfall.me"},
                    {HttpRequestHeader.AcceptEncoding.ToString(), "gzip, deflate, br"},
                    {HttpRequestHeader.AcceptLanguage.ToString(), "en-US,en;q=0.5"},
                    {HttpRequestHeader.ContentType.ToString(), "application/json"}
                },
                Content = new StringContent(response, Encoding.Default, "application/json")

            };

            var t = client.SendAsync(request).Result.Content.ReadAsStringAsync();
            Console.WriteLine(t.Result);
            Console.WriteLine($"Send Message: {stop.Elapsed.Seconds}");
            stop.Stop();

        }

        public void VerifyEmail(string id, string email)
        {
            using (var context = new safetyquizbotContext())
            {
                var user = context.UserTable.Single(e => e.Id == id);

                if (user.Verified == "true")
                {
                    SendMessage(GenericMessage("We already have your email address.\n" +
                                               "Thanks for taking the quiz. Goodbye, and Stay Safe!", id).ToJson());
                }
                else
                {
                    context.UserTable.Attach(user);
                    user.Email = email;
                    user.Verified = "true";

                    
                    context.Entry(user).Property(x => x.Verified).IsModified = true;
                    context.Entry(user).Property(x => x.Email).IsModified = true;
                    context.SaveChanges();
                    SendMessage(GenericMessage($"Your email address has been recorded", user.Id).ToJson());
                    SendMessage(
                        GenericMessage($"Thanks for taking the quiz. Goodbye, and Stay Safe!", user.Id).ToJson());
                }
            }
        }

        public string GenerateToken(string PSID, string email)
        {


            // Create a payload.
            // So you need a class as the JWT payload.
            // You can also inherit from JsonWebTokenPayload class if you need which contains some useful fields.
            // Suppose we have a class Model here to use.
            var model = new JwtModel()
            {
                Id = PSID,
                Email = email
            };

            // Create a JWT instance.
            return new JsonWebToken<JwtModel>(model, Startup.sign).ToEncodedString();
        }
        public void SetState(string PSID, int state, string chatcontext)
        {
            using (safetyquizbotContext context = new safetyquizbotContext())
            {
                if (context.QuizState.Where(e => e.Uid == PSID).Select(p => p).FirstOrDefault() == null)
                {
                    context.QuizState.Add(new QuizState()
                    {
                        Uid = PSID,
                        LastState = -1,
                        CurrentContext = chatcontext

                    });
                    context.SaveChanges();
                }
                else
                {
                    var user = context.QuizState.Where(e => e.Uid == PSID).Select(p => p).FirstOrDefault();
                    user.LastState = state;
                    user.CurrentContext = chatcontext;
                    context.SaveChanges();
                    Console.WriteLine("Passed context handler");
                }
            }
        }

        public async Task SendHelloAsync(string PSID)
        {
            FinishScorecard(PSID);

            await ProfileInfoHandler(PSID);

            SetState(PSID, -1, Startup.BotContext.Welcome.ToString());

            var xt = GetPageProfileAsync(PSID).Result;

            SendMessage((GenericMessage($"Hello {xt.FirstName},\nI'm a Safety Awareness Chatbot in the Faculty of Engineering and IT at UTS." +
                                        $"\nWhy not take this short quiz to see how much you know about health and safety in our faculty?", PSID).ToJson())); // Sending the first Welcome message.

            Thread.Sleep(Startup.SHORT_PAUSE);

            SendMessage(GenericMessage("Type in Start to start the quiz", PSID).ToJson()); // sending the click start button
        }

        public void Typing(string PSID)
        {
            SendMessage(
                $@"{{""messaging_type"":""RESPONSE"",""recipient"":{{""id"":""{PSID}""}},""sender_action"":""typing_on""}}");
        }
        public void NotTyping(string PSID)
        {
            SendMessage(
                $@"{{""messaging_type"":""RESPONSE"",""recipient"":{{""id"":""{PSID}""}},""sender_action"":""typing_off""}}");
        }

        public QuizState GetStateAsync(string PSID)
        {

            using (safetyquizbotContext context = new safetyquizbotContext())
            {
                var quizstate = context.QuizState.Where(e => e.Uid == PSID).Select(p => p).FirstOrDefault();
                Console.WriteLine("passed quizstate");
                return quizstate;

            }

        }
        public void CreateScorecard(string PSID)
        {

            var context = new safetyquizbotContext();
            context.ScoreBoard.Add(new ScoreBoard()
            {
                SessionId = PSID + (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds,
                Timestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds,
                Psid = PSID,
                CorrectInput = 0,
                CorrectTap = 0,
                IncorrectInput = 0,
                IncorrectTap = 0,
                PartialInput = 0,
                State = Startup.BotContext.Start.ToString(),
                Tap = 0,
                Input = 0
            });
            context.SaveChanges();
            Console.WriteLine("Created Scorecard");

        }
        public bool GetScore(string PSID)
        {
            var context = new safetyquizbotContext();
            var lastScore = context.ScoreBoard.Where(e => (e.Psid == PSID) && (e.State == Startup.BotContext.Finish.ToString())).Select(p => p).LastOrDefault();

            if (lastScore.CorrectTap == Startup.quiz.Questions.Count)
            {
                Typing(PSID);
                return true;
            }
            else
            {
                Typing(PSID);
                return false;
            }

        }
        public void PrintScore(string PSID)
        {
            var context = new safetyquizbotContext();
            var lastScore = context.ScoreBoard.Where(e => (e.Psid == PSID) && (e.State == Startup.BotContext.Start.ToString())).Select(p => p).LastOrDefault();

            SendMessage(GenericMessage("Now, it is marking time ...", PSID).ToJson());
            if (lastScore.CorrectTap >= 2)
            {
                SendMessage(GenericMessage($"Out of {Startup.quiz.Questions.Count}, you have answered {lastScore.CorrectTap} questions correctly", PSID).ToJson());
            }
            else
            {
                SendMessage(GenericMessage($"Out of {Startup.quiz.Questions.Count}, you have answered {lastScore.CorrectTap} question correctly", PSID).ToJson());
            }
            Typing(PSID);
            FinishScorecard((PSID));
        }
        public string GetName(string PSID)
        {
            var user = new safetyquizbotContext().UserTable.Where(e => e.Id == PSID).Select(p => p).LastOrDefault();
            return user.FirstName + " " + user.LastName;

        }
        public async Task SendVerificationEmail(string email, string PSID)
        {
            var verifyToken = GenerateToken(PSID, email);
            var name = GetName(PSID);
            var apiKey = "INSERT_SENDGRID_API_KEY_HERE";
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress("INSERT_EMAIL_ADDRESS_HERE", "School Safety Chatbot");
            var subject = "School Safety Chatbot lucky draw verification";
            var to = new EmailAddress($"{email}", $"{name}");
            var plainTextContent = $"Please verify your email address by clicking the verification link. \n<a href=\"https://beta.*********.***/webhook/eVerify?code={verifyToken}\">Verification link</a>";
            var htmlContent = $"Please verify your email address by clicking the verification link. \n<a href=\"https://beta.********.***/webhook/eVerify?code={verifyToken}\">Verification link</a>";
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            var response = await client.SendEmailAsync(msg);
            var tt = await response.Body.ReadAsStringAsync();
            Console.WriteLine(tt);
        }
        private MessageResponse GenericMessage(string message, string PSID)
        {
            var messageResponse = new MessageResponse();
            messageResponse.ResponseMessage = new ResponseMessage();
            messageResponse.ResponseMessage.RText = message;
            messageResponse.MessagingType = "RESPONSE";
            messageResponse.Recipient = new Recipient();
            messageResponse.Recipient.Id = PSID;
            return messageResponse;

        }
        public void CalculateInputScore(string PSID, bool correct)
        {
            var context = new safetyquizbotContext();
            var lastScore = context.ScoreBoard.Where(e => (e.Psid == PSID) && (e.State == Startup.BotContext.Start.ToString())).Select(p => p).LastOrDefault();
            lastScore.Input++;
            if (correct)
            {
                lastScore.CorrectTap += 1;
            }
            else
            {
                lastScore.IncorrectTap += 1;
            }

            context.SaveChanges();
        }
        public void FinishScorecard(string PSID)
        {
            var context = new safetyquizbotContext();
            if (context.ScoreBoard.Where(e => (e.Psid == PSID) && (e.State == Startup.BotContext.Start.ToString())).Select(p => p).LastOrDefault() != null)
            {
                var lastScore = context.ScoreBoard.Where(e => (e.Psid == PSID) && (e.State == Startup.BotContext.Start.ToString())).Select(p => p).LastOrDefault();
                lastScore.State = Startup.BotContext.Finish.ToString();
                context.SaveChanges();
                Console.WriteLine("Passed Scorecard");
            }
        }


        public async Task ProfileInfoHandler(string ID)
        {
            try
            {
                Stopwatch stop = new Stopwatch();
                stop.Start();
                using (var context = new safetyquizbotContext())
                {
                    if (context.UserTable.Where(e => e.Id == ID).Select(p => p).FirstOrDefault() == null)
                    {
                        var xt = await GetPageProfileAsync(ID);
                        xt.Verified = "false";
                        context.UserTable.Add(xt);
                        context.SaveChanges();
                        Console.WriteLine("Added ProfileInfo");
                    }
                    Console.WriteLine($"Profileinfo: {stop.Elapsed.Seconds}");
                    stop.Stop();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
