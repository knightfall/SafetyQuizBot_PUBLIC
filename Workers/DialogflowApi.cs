using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Dialogflow.V2;
using Google.Protobuf.WellKnownTypes;
using Grpc.Auth;
using Microsoft.ApplicationInsights;
using SafetyQuizBot.EF;
using SafetyQuizBot.Models;

namespace SafetyQuizBot.Workers
{
    public class Dialogflow
    {
        private GraphApi graph = new GraphApi();
        public int DetectIntentFromTexts(
            string sessionId,
            string text,
            string languageCode = "en-US")
        {
           
            string projectId = "INSERT_PROJECT_ID_HERE";
            var creds = GoogleCredential.FromJson("INSERT_KEY_HERE");
            var channel = new Grpc.Core.Channel(SessionsClient.DefaultEndpoint.Host, creds.ToChannelCredentials());
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var client = SessionsClient.Create(channel);

            var response = client.DetectIntent(
                session: new SessionName(projectId, sessionId),
                queryInput: new QueryInput()
                {
                    Text = new TextInput()
                    {
                        Text = text,
                        LanguageCode = languageCode
                    }
                }
            );
            var queryResult = response.QueryResult;
            var detectedIntent = queryResult.Intent.DisplayName;
            var state = graph.GetStateAsync(sessionId);


            if (state == null)
            {
                graph.SendHelloAsync(sessionId).Wait();
            }
            else if (state.CurrentContext == Startup.BotContext.Quiz.ToString())
            {
                QuizContextProcessing(sessionId, detectedIntent, queryResult, state);
            }
            //else if (state.CurrentContext == Startup.BotContext.Feedback.ToString())
            //{
            //    FeedbackProcessing(sessionId, detectedIntent, queryResult);
            //}
            else if (state.CurrentContext == Startup.BotContext.Email.ToString())
            {

                EmailProcessing(sessionId, detectedIntent, queryResult);
            }
            else if (state.CurrentContext == Startup.BotContext.Ineligible.ToString())
            {
                IneligibleContextProcessing(sessionId, detectedIntent, queryResult);
            }
            else if (state.CurrentContext == Startup.BotContext.Welcome.ToString())
            {
                WelcomeContextProcessing(sessionId, detectedIntent, queryResult);
            }

            return 0;
        }


        private int InsertComments(string PSID, string comment)
        {
            try
            {
                if (comment == Startup.Force_Wrong)
                {
                    return 100;
                }
                else
                {
                    using (var context = new safetyquizbotContext())
                    {
                        context.FeedBack.Add(new FeedBack()
                        {
                            FeedbackId =
                                PSID + (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds,
                            UserId = PSID,
                            Comment = comment
                        });
                        context.SaveChanges();
                    }

                    return 1;
                }
            }
            catch (Exception e)
            {
                if (e.InnerException != null && e.InnerException.Message.Contains("Incorrect string value"))
                {
                    return 99;
                }
                else return 0;
            }

        }
        private void FeedbackProcessing(string PSID, string detectedIntent, QueryResult queryResult)
        {
            var val = InsertComments(PSID, queryResult.QueryText);
            if (val == 1)
            {

                if (detectedIntent == "No")
                {
                    graph.SendMessage(GenericMessage("Thank you all the same", PSID).ToJson());
                }
                else
                {
                    graph.SendMessage(GenericMessage("Thank you. Your feedback has been recorded", PSID).ToJson());
                }
                graph.SetState(PSID, -1, Startup.BotContext.Email.ToString());
                EmailPre(PSID);
            }
            else if (val == 99)
            {
                graph.SendMessage(GenericMessage("Sorry, emojis aren't valid feedback. \n" +
                                                 "Please type in text only", PSID).ToJson());
            }
            else if (val == 100)
            {
                graph.SendMessage(GenericMessage("Sorry, attachments and emojis aren't valid feedback. \n" +
                                                 "Please type in text only", PSID).ToJson());
            }
            else
            {
                graph.SendMessage(GenericMessage("Sorry, something went wrong while saving your feedback. Would you please submit your feedback again?", PSID).ToJson());
            }
        }
        private static bool ContainsEmoji(string text)
        {
            Regex rgx = new Regex(@"\p{Cs}");
            return rgx.IsMatch(text);
        }
        private void IneligibleContextProcessing(string PSID, string detectedIntent, QueryResult queryResult)
        {
            if (detectedIntent == "Exit" || detectedIntent == "No")
            {
                graph.SendMessage(GenericMessage($"Thanks for taking the quiz. Goodbye, and Stay Safe!", PSID).ToJson());
                graph.SetState(PSID, -1, Startup.BotContext.Welcome.ToString());
            }
            else if (detectedIntent == "Start")
            {
                RestartQuiz(PSID);
            }
            else
            {
                graph.SendMessage(GenericMessage($"Thanks for taking the quiz. Goodbye, and Stay Safe!", PSID).ToJson());
                graph.SetState(PSID, -1, Startup.BotContext.Welcome.ToString());
            }

        }
        private void EmailPre(string PSID)
        {
            var xt = new safetyquizbotContext().UserTable.Where(e => e.Id == PSID).Select(p => p).LastOrDefault();

            if (graph.GetScore(PSID))
            {
                if (xt.Verified == "false")
                {
                    graph.SendMessage(GenericMessage("Do you want to enter the lucky draw? If yes, type in your email address", PSID).ToJson());
                    graph.SendMessage(GenericMessage("If not, type no", PSID).ToJson());
                }
                else
                {
                    graph.SendMessage(GenericMessage("Thank you for taking the quiz.\nWe have your email address. " +
                                                     "So your name is in the lucky draw.", PSID).ToJson());
                    graph.SetState(PSID, -1, Startup.BotContext.Welcome.ToString());
                }

            }
            else
            {
                if (xt.Verified == "false")
                {
                    graph.SendMessage(GenericMessage("If you answer all questions correctly, " +
                                                 "you will be eligible to enter a lucky draw to win one of the three JB Hi-Fi gift cards (1x $100 & 2x $50). " +
                                                 "\nIf you’d like to retake the quiz, enter Restart", PSID).ToJson());
                    graph.SetState(PSID, -1, Startup.BotContext.Ineligible.ToString());
                }
                else
                {
                    graph.SendMessage(GenericMessage("Thank you for taking the quiz.\nYou had answered all the questions correctly before." +
                                                     " So, your name is already in the lucky draw.", PSID).ToJson());
                    graph.SendMessage(GenericMessage("If you’d like to retake the quiz, enter Restart", PSID).ToJson());
                    graph.SetState(PSID, -1, Startup.BotContext.Welcome.ToString());
                }
            }
        }
        private void EmailProcessing(string sessionId, string detectedIntent, QueryResult queryResult)
        {
            if (detectedIntent == "Email")
            {
                string email = queryResult.Parameters.Fields["email"].StringValue;
                if (email.Contains("@student.uts.edu.au") || email.Contains("@uts.edu.au"))
                {
                    // graph.SendMessage(GenericMessage($"Sending verification email to {email}", sessionId).ToJson());
                    //graph.Typing(sessionId);
                    graph.VerifyEmail(sessionId, email);
                    // graph.SendVerificationEmail(email, sessionId).Wait();
                    //  graph.SendMessage((GenericMessage("Please verify your email address within 24 hours by clicking the link sent to you", sessionId).ToJson()));
                    graph.SetState(sessionId, -1, Startup.BotContext.Welcome.ToString());


                }
                else
                {
                    graph.SendMessage((GenericMessage("Please provide your UTS email address.\n" +
                                                      "It will be like 1234567@student.uts.edu.au where 1234567 is your student ID", sessionId).ToJson()));
                }

            }
            else if (detectedIntent == "Exit" || detectedIntent == "No")
            {
                graph.SendMessage(GenericMessage($"Thanks for taking the quiz. Goodbye, and Stay Safe!", sessionId).ToJson());
                graph.SetState(sessionId, -1, Startup.BotContext.Welcome.ToString());
            }
            else
            {
                if (queryResult.QueryText == Startup.Force_Wrong)
                {
                    graph.SendMessage((GenericMessage("Sorry, attachments and emojis aren't valid input for email addresses", sessionId).ToJson()));
                }
                graph.SendMessage((GenericMessage("Please input a valid email address", sessionId).ToJson()));
                graph.SendMessage((GenericMessage("If you are not interested to enter the lucky draw, type exit to finish the quiz", sessionId).ToJson()));
            }
        }
        private void QuizContextProcessing(string sessionId, string detectedIntent, QueryResult queryResult, QuizState state)
        {
            switch (detectedIntent)
            {
                case "Answer":
                    if (queryResult.Parameters.Fields["alpha"].ListValue.Values.ToArray().Length > 0)
                    {
                        if (!validitycheck(state.LastState.GetValueOrDefault(),
                            queryResult.Parameters.Fields["alpha"].ListValue.Values.ToArray()))
                        {
                            QuizWrongInput(sessionId, state.LastState.GetValueOrDefault());

                        }
                        else if (state.LastState <= Startup.quiz.Questions.Count - 2)
                        {
                            QuizReplyProcessing(sessionId, queryResult, state);
                        }
                        else
                        {
                            EndQuiz(sessionId, queryResult, state);
                        }
                        break;
                    }
                    else
                    {
                        QuizWrongInput(sessionId, state.LastState.GetValueOrDefault());
                        break;
                    }
                case "Exit":
                    ExitQuiz(sessionId);
                    break;
                case "Start":
                    RestartQuiz(sessionId);
                    break;
                default:
                    if (queryResult.QueryText == Startup.Force_Wrong)
                    {
                        graph.SendMessage((GenericMessage("Sorry, I can't process attachments or emojis", sessionId).ToJson()));
                    }
                    QuizWrongInput(sessionId, state.LastState.GetValueOrDefault());
                    break;

            }
        }
        private static bool ContainsRestart(string text)
        {
            string rgx = @"restart";
            RegexOptions options = RegexOptions.IgnoreCase;
            return Regex.IsMatch(text, rgx, options);
            //return rgx.IsMatch(text);
        }
        private void WelcomeContextProcessing(string sessionId, string detectedIntent, QueryResult queryResult)
        {
            var acx = queryResult.Action;
            var asd = acx.Contains("smalltalk");
            if (ContainsRestart(queryResult.QueryText) && detectedIntent == "Start")
            {
                RestartQuiz(sessionId);
            }
            else if (detectedIntent == "Start")
            {
                StartQuiz(sessionId);
            }
            else if (queryResult.Action.Contains("smalltalk") || queryResult.Intent.DisplayName.Contains("smalltalk"))
            {
                Console.WriteLine(queryResult.FulfillmentText);
                SmallTalkAgent(queryResult.FulfillmentText, sessionId);
            }
            else if (queryResult.Intent.DisplayName.Contains("time.get"))
            {
                SmallTalkAgent(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("E. Australia Standard Time")).ToString("h:mm:ss tt"), sessionId);
            }
            else
            {
                StartWrongInput(queryResult.FulfillmentText, sessionId, queryResult.QueryText);
            }
        }
        private void QuizReplyProcessing(string sessionId, QueryResult queryResult, QuizState state)
        {
            AnswerResponse(sessionId, queryResult.Parameters.Fields["alpha"].ListValue.Values.ToArray(),
                state.LastState.GetValueOrDefault());
            //graph.Typing(sessionId);
            Thread.Sleep(Startup.LONG_PAUSE);
            NextQuestionAsync(sessionId, state.LastState.GetValueOrDefault() + 1, Startup.BotContext.Quiz.ToString());
        }

        private void EndQuiz(string sessionId, QueryResult queryResult, QuizState state)
        {
            AnswerResponse(sessionId, queryResult.Parameters.Fields["alpha"].ListValue.Values.ToArray(), state.LastState.GetValueOrDefault());
            Thread.Sleep(Startup.LONG_PAUSE);
            graph.PrintScore(sessionId);
            //graph.SendMessage((GenericMessage("What are other information related to safety and emergency that you want to know? " +
            //                                  "Do you have any other comments? Just type in.", sessionId).ToJson()));
            //graph.SetState(sessionId, -1, Startup.BotContext.Feedback.ToString());
            graph.SetState(sessionId, -1, Startup.BotContext.Email.ToString());
            EmailPre(sessionId);
        }


        private void SmallTalkAgent(string message, string PSID)
        {
            graph.SendMessage(GenericMessage(message, PSID)
                .ToJson());

        }
        private void StartWrongInput(string message, string PSID, string QueryText)
        {
            if (QueryText == Startup.Force_Wrong)
            {
                graph.SendMessage((GenericMessage("Sorry, I can't process attachments or emojis", PSID).ToJson()));
            }
            else
            {
                graph.SendMessage(GenericMessage(message, PSID)
                    .ToJson());
            }

            Thread.Sleep(Startup.LONG_PAUSE);
            graph.SendMessage(GenericMessage("Type in \"start\" to start the quiz", PSID).ToJson());
        }

        private void QuizWrongInput(string PSID, int laststate)
        {
            switch (Startup.quiz.Questions[laststate].Limit)
            {
                case 2:
                    graph.SendMessage(GenericMessage($"Answers can be A or B. When typing in answers, options should be separated by a space or a comma, such as \"A, B\" or \"A B\", but not \"AB\".", PSID)
                        .ToJson());
                    break;
                case 3:
                    graph.SendMessage(GenericMessage($"Answers can be A, B or C. When typing in answers, options should be separated by a space or a comma, such as \"A, B\" or \"A B\", but not \"AB\".", PSID)
                        .ToJson());
                    break;
                case 4:
                    graph.SendMessage(GenericMessage($"Answers can be A, B, C or D. When typing in answers, options should be separated by a space or a comma, such as \"A, B\" or \"A B\", but not \"AB\".", PSID)
                        .ToJson());
                    break;
                case 5:
                    graph.SendMessage(GenericMessage($"Answers can be A, B, C to E. When typing in answers, options should be separated by a space or a comma, such as \"A, B\" or \"A B\", but not \"AB\".", PSID)
                        .ToJson());
                    break;
                case 6:
                    graph.SendMessage(GenericMessage($"Answers can be A, B, C to F. When typing in answers, options should be separated by a space or a comma, such as \"A, B\" or \"A B\", but not \"AB\".", PSID)
                        .ToJson());
                    break;
                case 7:
                    graph.SendMessage(GenericMessage($"Answers can be A, B, C to G. When typing in answers, options should be separated by a space or a comma, such as \"A, B\" or \"A B\", but not \"AB\".", PSID)
                        .ToJson());
                    break;
            }
            Thread.Sleep(Startup.LONG_PAUSE);

            graph.SendMessage(GenericMessage("Please type in the appropriate options", PSID).ToJson());


        }

        private void ExitQuiz(string PSID)
        {

            graph.FinishScorecard(PSID);
            Thread.Sleep(Startup.LONG_PAUSE);
            graph.SendMessage((GenericMessage("Quiz finished. You can always take it later", PSID).ToJson()));
            graph.SetState(PSID, -1, Startup.BotContext.Welcome.ToString());
        }

        private void AnswerResponse(string PSID, Value[] answers, int state)
        {
            //graph.Typing(PSID); // Calculation might take extra time. For positive user experience
            Thread.Sleep(Startup.SHORT_PAUSE); // just in case everything goes fast.
            int correctInput = 0;
            int incorrectInput = 0;

            if (Startup.quiz.Questions[state].True.Count != answers.Length)
            {
                incorrectInput += 1;

                PrintCorrectAnswers(PSID, state);
            }
            else
            {
                int x = 0;
                foreach (var ans in answers)
                {
                    var opt = getSelection(ans.StringValue);
                    if (opt < Startup.quiz.Questions[state].Limit)
                    {
                        if (!Startup.quiz.Questions[state].True.Contains(ans.StringValue.ToString()))
                        {
                            incorrectInput += 1;
                        }
                        else
                        {
                            correctInput += 1;
                        }

                    }
                    else
                    {
                        incorrectInput += 1;
                        Console.WriteLine(x);
                        x += 1;
                        break;
                    }
                }

                //  int i = 0;
                if (incorrectInput == 0)
                {
                    graph.SendMessage(GenericMessage("👍", PSID).ToJson());
                    graph.SendMessage(GenericMessage(Startup.quiz.Questions[state].Correct, PSID).ToJson());
                    graph.CalculateInputScore(PSID, true);
                }
                else
                {
                    PrintCorrectAnswers(PSID, state);
                    graph.CalculateInputScore(PSID, true);
                }
                //   i = 0;
            }

            //graph.Typing(PSID);
            if (state < Startup.quiz.Questions.Count - 2)
            {
                Thread.Sleep(700);
                graph.SendMessage(
                    GenericMessage(Startup.filler[new Random().Next(Startup.filler.Length)], PSID)
                        .ToJson());
            }

            else if (state == Startup.quiz.Questions.Count - 2)
            {
                graph.SendMessage(
                    (GenericMessage($"We are nearly there. Now the last question", PSID).ToJson()));
            }


        }
        void PrintCorrectAnswers(string psid, int i)
        {
            graph.SendMessage(
                    GenericMessage($"That's not quite right.\nThe correct answer to this question is {string.Join(", ", Startup.quiz.Questions[i].True)}",
                        psid).ToJson());
        }
        private void RestartQuiz(string PSID)
        {
            graph.ProfileInfoHandler(PSID).Wait();
            graph.FinishScorecard(PSID);
            graph.SetState(PSID, 0, Startup.BotContext.Quiz.ToString());
            graph.CreateScorecard(PSID);
            graph.SendMessage((GenericMessage($"You are taking the quiz with the Safety Awareness Chatbot in the Faculty of Engineering and IT at UTS.\nThere will be {Startup.quiz.Questions.Count} questions.\nSome questions have multiple answers.", PSID).ToJson()));

            Thread.Sleep(Startup.SHORT_PAUSE);
            graph.SendMessage(
                (GenericMessage("You have to type in the answers. When typing in the answers, options should be separated by a space or a comma, such as \"A, B\" or \"A B\", but not \"AB\".", PSID)
                    .ToJson()));
            //graph.Typing(PSID);
            Thread.Sleep(Startup.SHORT_PAUSE);
            graph.SendMessage((GenericMessage("First Question for you", PSID).ToJson()));

            Thread.Sleep(Startup.SHORT_PAUSE);

            NextQuestionAsync(PSID, 0, Startup.BotContext.Quiz.ToString());
        }
        private void StartQuiz(string PSID)
        {
            graph.ProfileInfoHandler(PSID).Wait();
            graph.FinishScorecard(PSID);
            graph.SetState(PSID, 0, Startup.BotContext.Quiz.ToString());
            graph.CreateScorecard(PSID);
            graph.SendMessage((GenericMessage($"Alright.\nThere will be {Startup.quiz.Questions.Count} questions.\nSome questions have multiple answers.", PSID).ToJson()));

            Thread.Sleep(Startup.SHORT_PAUSE);
            graph.SendMessage(
                (GenericMessage("You have to type in the answers. When typing in the answers, options should be separated by a space or a comma, such as \"A, B\" or \"A B\", but not \"AB\".", PSID)
                    .ToJson()));
            //graph.Typing(PSID);
            Thread.Sleep(Startup.SHORT_PAUSE);
            graph.SendMessage((GenericMessage("First Question for you", PSID).ToJson()));

            Thread.Sleep(Startup.SHORT_PAUSE);

            NextQuestionAsync(PSID, 0, Startup.BotContext.Quiz.ToString());
        }

        public void NextQuestionAsync(string PSID, int questionno, string chatcontext)
        {

            // var graph = new GraphApi();
            graph.SetState(PSID, questionno, chatcontext);
            var ques = GenericMessage(Startup.quiz.Questions[questionno].QuestionQuestion, PSID);
            graph.SendMessage(ques.ToJson());

            Thread.Sleep(Startup.LONG_PAUSE);
            foreach (var opt in Startup.quiz.Questions[questionno].Responses)
            {
                var msg = GenericMessage($"{opt.Title}. {opt.Option}", PSID);
                graph.SendMessage(msg.ToJson());

                Thread.Sleep(Startup.SHORT_PAUSE);
            }
            graph.SendMessage(GenericMessage("Please type in your answer(s)", PSID).ToJson());


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

        private bool validitycheck(int state, Value[] answers)
        {
            bool result = true;
            var d = Startup.quiz.Questions[state].Limit;
            foreach (var item in answers)
            {
                var x = getSelection(item.StringValue) + 1;
                if (d < x)
                {
                    result = false;
                    break;
                }
            }

            return result;
        }
        private int getSelection(string payload)
        {
            int val = 0;
            switch (payload)
            {
                case "A":
                    val = 0;
                    break;
                case "B":
                    val = 1;
                    break;
                case "C":
                    val = 2;
                    break;
                case "D":
                    val = 3;
                    break;
                case "E":
                    val = 4;
                    break;
                case "F":
                    val = 5;
                    break;
                case "G":
                    val = 6;
                    break;
                default:
                    val = 1063;
                    break;

            }
            return val;
        }

    }
}
