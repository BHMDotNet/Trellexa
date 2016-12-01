using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AlexaSkillsKit.Speechlet;
using AlexaSkillsKit.Slu;
using AlexaSkillsKit.UI;
using AlexaSkillsKit.Authentication;
using AlexaSkillsKit.Json;
using System.Threading.Tasks;
using System.Web.Http;
using Manatee.Trello;
using Manatee;
using Manatee.Trello.ManateeJson;
using Manatee.Trello.WebApi;
using Humanizer;

namespace Trellexa.WebAPI.Lib
{
    public class TrellexaSpeechlet : SpeechletAsync, ISpeechletAsync
    {
        public TrellexaSpeechlet()
        {
            var serializer = new ManateeSerializer();
            TrelloConfiguration.Serializer = serializer;
            TrelloConfiguration.Deserializer = serializer;
            TrelloConfiguration.JsonFactory = new ManateeFactory();
            TrelloConfiguration.RestClientProvider = new WebApiClientProvider();
            TrelloAuthorization.Default.AppKey = "TODO Set Trello App Key";
            //TrelloAuthorization.Default.UserToken =  "TODO Set Trello User Token for testing";
        }
        public override async Task<SpeechletResponse> OnIntentAsync(IntentRequest intentRequest, Session session)
        {
            // Set user from the linked account in Alexa
            TrelloAuthorization.Default.UserToken = session.User.AccessToken;
            var member = Member.Me;

            // Set default Trello board
            if(member.Boards.FirstOrDefault(x => x.Name == "Trellexa") == null)
            {
                member.Boards.Add("Trellexa");
            }

            Intent intent = intentRequest.Intent;
            string intentName = (intent != null) ? intent.Name : null;
           
            var speechOutput = string.Empty;

            // Intent logic
            if ("Test".Equals(intentName))
            {
                speechOutput = "Atomic batteries to power. Turbines to speed. All systems online and nominal";
                var response = await BuildSpeechletResponse("Success", speechOutput, true);
                return response;

            }
            else if ("CreateBoard".Equals(intentName))
            {

                
                if(intent.Slots.FirstOrDefault(x => x.Key == "BoardName").Value.Value == null)
                {
                    speechOutput = "Ok, what do you want to call this board?" ;
                    var response = await BuildSpeechletResponse("Success", speechOutput, false);
                    return response;

                }
                else
                {
                    var board = intent.Slots.FirstOrDefault(x => x.Key == "BoardName").Value.Value.ToString().Humanize(LetterCasing.Title);
                    member.Boards.Add(board);

                    speechOutput = string.Format("{0}, I've created a new board called {1}", member.FullName.Split(' ').FirstOrDefault(), board);
                    var response = await BuildSpeechletResponse("Success", speechOutput, true);
                    return response;

                }

                
            }
            else if("CreateItem".Equals(intentName))
            {
                

                var board = GetBoard(intent, member);

                var list = board.Lists.FirstOrDefault();

                if (intent.Slots.FirstOrDefault(x => x.Key == "ItemName").Value.Value == null)
                {
                    return await BuildSpeechletResponse("Success", "I'm sorry but I wasn't able to understand the name of the task. Say the command again and I will pay closer attention this time.", false);

                }
                list.Cards.Add(intent.Slots.FirstOrDefault(x => x.Key == "ItemName").Value.Value.ToString().Humanize(LetterCasing.Title));

                speechOutput = string.Format("{0}, I've created a new card called {1} and added it to the {2} column on the {3} board. {4}", member.FullName.Split(' ').FirstOrDefault(), intent.Slots.FirstOrDefault().Value.Value.ToString(), list.Name, board,GetRandomCompliment());
                var response = await BuildSpeechletResponse("Success", speechOutput, true);
                return response;
            }
            else if ("MoveItem".Equals(intentName))
            {
                var itemName = intent.Slots.FirstOrDefault().Value.Value.ToString();
                var stageName = intent.Slots.FirstOrDefault(x => x.Key == "Stage").Value.Value.ToString().ToLower();

                Board board = GetBoard(intent, member);
                var lists = board.Lists.FirstOrDefault(x => x.Name.ToLower().Contains(stageName));

                var card = board.Cards.FirstOrDefault(x => x.Name.ToLower().Contains(itemName));

                if (card != null)
                {

                    lists.Cards.Add(card);
                    card.Delete();
                    speechOutput = string.Format("{0} has been moved to {1}. {2}", itemName, stageName, GetRandomCompliment());
                }
                else
                {
                    speechOutput = string.Format("I couldn't find an item called {0}?");
                }


                var response = await BuildSpeechletResponse("Success", speechOutput, true);
                return response;
            }
            else if ("GetNextItem".Equals(intentName))
            {

                var board = GetBoard(intent, member);
                var list = GetList(intent, board);
                var card = list?.Cards?.FirstOrDefault();

                speechOutput = string.Format("{0}, your next item up is {1}. {2}", member.FullName.Split(' ').FirstOrDefault(), card.Name, GetRandomCompliment());
                var response = await BuildSpeechletResponse("Success", speechOutput, true);
                return response;
            }
            else if ("GetActiveItems".Equals(intentName))
            {
                speechOutput = "Get Active Item Called";
                var response = await BuildSpeechletResponse("Success", speechOutput, true);
                return response;
            }
            else if ("ReviewItems".Equals(intentName))
            {
                speechOutput = "Here is your to do list.<break time='1s'/> ";

                var board = GetBoard(intent, member);

                foreach (var list in board.Lists)
                {
                    speechOutput += string.Format("In the {0} column: <break time='1s'/>", list.Name);
                    foreach(var card in list.Cards)
                    {
                        speechOutput += card.Name + ". <break time='1s'/>";

                        if(card.Members.Any())
                        {
                            speechOutput += "It's assigned to: ";
                            foreach(var assignedMember in card.Members)
                            {
                                speechOutput += assignedMember.FullName + " <break time='1s'/>";
                            }
                     
                        }

                        if (card.DueDate.HasValue)
                        {
                            speechOutput += string.Format("It's due on {1} <break time='1s'/>", card.DueDate);
                        }
                    }
                }
                speechOutput += GetRandomCompliment();
                var response = await BuildSpeechletResponse("Success", speechOutput, true);
                return response;
            }
            else
            {

                throw new SpeechletException("Invalid Intent");

            }
        }


        //Helper methods
        private List GetList(Intent intent, Board board)
        {
            var listId = board.Lists.FirstOrDefault(x => x.Name == "To Do").Id;

            if (intent.Slots.Count > 0 && intent.Slots.FirstOrDefault(x => x.Key == "StageName").Value.Value != null)
            {
                listId = board.Lists.FirstOrDefault(x => x.Name.ToLower() == intent.Slots.FirstOrDefault(y => y.Key.Contains("StageName")).Value.Value.ToLower().ToString()).Id;
            }

            var list = new List(listId);
            return list;
        }

        private string GetRandomCompliment()
        {
     
            string[] compliments = {
                "You rock!",
                "You're on fire!",
                "You are a getting things done master!",
                "How do you get so much stuff done?",
                "Slow down! You're working too fast!",
                "You deserve a raise at the rate you're knocking out tasks!",
                "Keep it up!",
                "How are you so awesome?"};

            var rndComp = new Random();
            var randomCompliment = rndComp.Next(compliments.Length);

            return compliments[randomCompliment];
        }

        private static Board GetBoard(Intent intent, Member member)
        {
            var boardId = member.Boards.FirstOrDefault(x => x.Name == "Trellexa").Id;

            if (intent.Slots.Count > 0 && intent.Slots.FirstOrDefault(x => x.Key == "BoardName").Value.Value != null && member.Boards.FirstOrDefault(x => x.Name.ToLower() == intent.Slots.FirstOrDefault(y => y.Key.Contains("BoardName") && x.IsClosed == false).Value.Value.ToLower().ToString()) != null)
            {
                boardId = member.Boards.FirstOrDefault(x => x.Name.ToLower() == intent.Slots.FirstOrDefault(y => y.Key.Contains("BoardName") && x.IsClosed == false).Value.Value.ToLower().ToString()).Id;
            }

            var board = new Board(boardId);
            return board;
        }

        public override Task<SpeechletResponse> OnLaunchAsync(LaunchRequest launchRequest, Session session)
        {
            return BuildSpeechletResponse("Welcome to Trellexa", "Welcome to Trellexa! What can I do to help you be more awesome!", false);
        }

        public bool OnRequestValidation(SpeechletRequestValidationResult result, DateTime referenceTimeUtc, SpeechletRequestEnvelope requestEnvelope)
        {
            return true;
        }

        public override Task OnSessionEndedAsync(SessionEndedRequest sessionEndedRequest, Session session)
        {
            return Task.Run(() => { });
        }

        public override Task OnSessionStartedAsync(SessionStartedRequest sessionStartedRequest, Session session)
        {
            return Task.Run(() => { });
        }

        private async Task<SpeechletResponse> BuildSpeechletResponse(string title, string output, bool shouldEndSession)
        {

            // Create the Simple card content.

            SimpleCard card = new SimpleCard();

            card.Title = String.Format("SessionSpeechlet - {0}", title);

            card.Subtitle = String.Format("SessionSpeechlet - Sub Title");

            card.Content = String.Format("SessionSpeechlet - {0}", output);



            // Create the plain text output.

            //PlainTextOutputSpeech speech = new PlainTextOutputSpeech();
            SsmlOutputSpeech speech = new SsmlOutputSpeech();
            speech.Ssml = string.Format("<speak>{0}</speak>", output);
            



            // Create the speechlet response.
            
            SpeechletResponse response = new SpeechletResponse();
           

            response.ShouldEndSession = shouldEndSession;

            response.OutputSpeech = speech;

            response.Card = card;
            return response;

        }
    }
}