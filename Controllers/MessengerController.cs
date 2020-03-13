using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.EntityFrameworkCore;
using Newtonsoft.Json;
using SafetyQuizBot.EF;
using SafetyQuizBot.Models;
using SafetyQuizBot.Workers;
using Coravel.Queuing.Interfaces;
using Google.Protobuf.WellKnownTypes;


namespace SafetyQuizBot.Controllers
{
    [Route("webhook/")]
    [ApiController]
    public class MessengerController : ControllerBase
    {
        // Quiz quiz;
        IQueue _queue;
        private GraphApi graph;
        public MessengerController(IQueue queue)
        {
            this._queue = queue;
            // quiz = Quiz.FromJson(Startup.Questions);
            this.graph = new GraphApi();
            Console.WriteLine("Initiated");
        }
        private readonly string VerifyToken = "";



        [Route("listener")]
        [HttpGet]
        public string Get([FromQuery(Name = "hub.mode")] string mode,
            [FromQuery(Name = "hub.challenge")] string challenge,
            [FromQuery(Name = "hub.verify_token")] string token)
        {
            try
            {
                if (mode == "subscribe" && token == VerifyToken)
                {
                    return challenge.ToString();
                }
                else
                {
                    return HttpStatusCode.Forbidden.ToString();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return HttpStatusCode.BadRequest.ToString();
            }
        }

        [Route("listener")]
        [HttpPost]
        public async Task<HttpStatusCode> RecieveAsync()
        {

            // var xt = graph.GetPageProfileAsync(PSID).Result;

            string request = "";
            //  GraphApi graph = new GraphApi();
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8, true, 1024, true))
            {
                request = await reader.ReadToEndAsync();
            }
            var messengerEvent = MessengerEvent.FromJson(request);
            var PSID = messengerEvent.Entry[0].Messaging[0].Sender.Id;
            if (messengerEvent.Entry[0].Messaging[0].Postback != null)
            {
                if (messengerEvent.Entry[0].Messaging[0].Postback.Payload == "GET_STARTED" || messengerEvent.Entry[0].Messaging[0].Postback.Payload == "RESTART_BOT")
                {
                    try
                    {

                        this._queue.QueueAsyncTask(() => graph.SendHelloAsync(PSID));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                else if (messengerEvent.Entry[0].Messaging[0].Postback.Payload == "RESTART_QUIZ")
                {
                    this._queue.QueueAsyncTask(() => graph.ProfileInfoHandler(PSID));
                    this._queue.QueueTask(() => graph.FinishScorecard(PSID));
                    //this._queue.QueueTask(() => graph.Typing(PSID));
                    this._queue.QueueTask(() => graph.CreateScorecard(PSID));
                    this._queue.QueueTask(() => graph.SetState(PSID, 0, Startup.BotContext.Quiz.ToString()));
                    this._queue.QueueTask(() => graph.SendMessage((GenericMessage($"Alright.\nThere will be {Startup.quiz.Questions.Count} questions.\nSome questions have multiple answers.", PSID).ToJson())));

                    this._queue.QueueTask(() => Thread.Sleep(300));

                    this._queue.QueueTask(() => graph.SendMessage((GenericMessage("You may either tap or type in your answers. When typing in answers, options should be separated by a space or a comma, such as \"A, B\" or \"A B\", but not \"AB\".", PSID).ToJson())));

                    this._queue.QueueTask(() => Thread.Sleep(300));

                    this._queue.QueueTask(() => graph.SendMessage((GenericMessage("First Question for you", PSID).ToJson())));

                    this._queue.QueueTask(() => Thread.Sleep(300));

                    this._queue.QueueTask(() => NextQuestionAsync(PSID, 0, Startup.BotContext.Quiz.ToString()));
                }

            }
            else if (messengerEvent.Entry[0].Messaging[0].Message.Text == null)
            {
                this._queue.QueueTask(() =>
                    new Dialogflow().DetectIntentFromTexts(PSID, Startup.Force_Wrong));
            }
            else
            {
                //this._queue.QueueTask(() =>graph.Typing(PSID));
                this._queue.QueueTask(() =>
                    new Dialogflow().DetectIntentFromTexts(PSID, messengerEvent.Entry[0].Messaging[0].Message.Text));
            }
            return HttpStatusCode.OK;
        }
        //trigger build

        public void NextQuestionAsync(string PSID, int questionno, string chatcontext)
        {

            // var graph = new GraphApi();
            graph.SetState(PSID, questionno, chatcontext);
            var ques = GenericMessage(Startup.quiz.Questions[questionno].QuestionQuestion, PSID);

            graph.SendMessage(ques.ToJson());

            Thread.Sleep(Startup.SHORT_PAUSE);

            foreach (var opt in Startup.quiz.Questions[questionno].Responses)
            {
                var msg = GenericMessage($"{opt.Title}. {opt.Option}", PSID);
                // 
                graph.SendMessage(msg.ToJson());

                Thread.Sleep(Startup.SHORT_PAUSE);
            }

            graph.SendMessage(GenericMessage("Please type in the appropriate answers", PSID).ToJson());

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
        private MessageResponse HelloMessage(string PSID)
        {
            // GraphApi graph = new GraphApi();
            var xt = graph.GetPageProfileAsync(PSID).Result;
            var messageResponse = new MessageResponse();
            messageResponse.ResponseMessage = new ResponseMessage();
            messageResponse.ResponseMessage.RText =
                $"Hello {xt.FirstName},\nI'm a Safety Awareness Chatbot in the Faculty of Engineering and IT at UTS. \nWhy not take this short quiz to see how much you know about health and safety in our faculty?";
            messageResponse.MessagingType = "RESPONSE";
            messageResponse.Recipient = new Recipient();
            messageResponse.Recipient.Id = PSID;


            return messageResponse;
        }

    }
}
