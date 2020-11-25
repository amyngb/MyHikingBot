using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Language.LUIS.Runtime.Models;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.BotBuilderSamples
{
    public class DispatchBot : ActivityHandler
    {
        private readonly ILogger<DispatchBot> _logger;
        private readonly IBotServices _botServices;

        private static readonly HttpClient client = new HttpClient();

        private readonly string[] _cards = {
            Path.Combine (".", "Cards", "hikesNearMe.json"),
            Path.Combine (".", "Cards", "weather.json"),
        };

        public DispatchBot(IBotServices botServices, ILogger<DispatchBot> logger)
        {
            _logger = logger;
            _botServices = botServices;
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            // First, we use the dispatch model to determine which cognitive service (LUIS or QnA) to use.
            var recognizerResult = await _botServices.Dispatch.RecognizeAsync(turnContext, cancellationToken);
            // Top intent tell us which cognitive service to use.
            var topIntent = recognizerResult.GetTopScoringIntent();

            // Next, we call the dispatcher with the top intent.
            await DispatchToTopIntentAsync(turnContext, topIntent.intent, recognizerResult, cancellationToken);
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            const string WelcomeText = "Ask a hiking question, search for hikes near Columbus or find out the weather.";

            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(MessageFactory.Text($"Welcome to HikerBot. {WelcomeText}"), cancellationToken);
                }
            }
        }

        private async Task DispatchToTopIntentAsync(ITurnContext<IMessageActivity> turnContext, string intent, RecognizerResult recognizerResult, CancellationToken cancellationToken)
        {
            switch (intent)
            {
                case "l_hikes":
                    await ProcessHikeInfoAsync(turnContext, recognizerResult.Properties["luisResult"] as LuisResult, cancellationToken);
                    break;
                case "q_hikes":
                    await ProcessHikeQnAAsync(turnContext, cancellationToken);
                    break;
                default:
                    _logger.LogInformation($"HikerBot didn't understand you.");
                    await turnContext.SendActivityAsync(MessageFactory.Text($"HikerBot did not understand.  Please ask a question about hiking."), cancellationToken);
                    break;
            }
        }

        private async Task ProcessHikeInfoAsync(ITurnContext<IMessageActivity> turnContext, LuisResult luisResult, CancellationToken cancellationToken)
        {
            _logger.LogInformation("ProcessHikeInfoAsync");

            // Retrieve LUIS result for Process Automation.
            var result = luisResult.ConnectedServiceResult;
            var topIntent = result.TopScoringIntent.Intent;
            if (topIntent == "hikesNearby")
            {
                var url = $"https://www.hikingproject.com/data/get-trails?lat=39.9611&lon=-82.9987&maxDistance=10&key=108770750-1c4e83240a4223b7b975b0aa2e6eedde";
                var repositories = ProcessHikesRepo(url);

                var hikesNearMeCardRead = readFileforUpdate_jobj(_cards[0]);

                for (int i = 0; i <= 3; i++)
                {
                    JToken Name = hikesNearMeCardRead.SelectToken($"body[1].facts[{i}].title");
                    JToken Summary = hikesNearMeCardRead.SelectToken($"body[1].facts[{i}].value");

                    Name.Replace(repositories.Trails[i].Name);
                    Summary.Replace(repositories.Trails[i].Summary);
                }

                var hikesNearMeCardFinal = UpdateAdaptivecardAttachment(hikesNearMeCardRead);
                var response = MessageFactory.Attachment(hikesNearMeCardFinal, ssml: "Hikes Near Me card!");
                await turnContext.SendActivityAsync(response, cancellationToken);
            }
            else
            if (topIntent == "hikingWeather")
            {
                            }
            else
            {
                _logger.LogInformation($"Luis unrecognized intent.");
                await turnContext.SendActivityAsync(MessageFactory.Text($"HikerBot didn't recognize your inputs, kindly reply as 'hikes nearby'."), cancellationToken);
            }
        }

        private async Task ProcessHikeQnAAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            _logger.LogInformation("ProcessSampleQnAAsync");

            var results = await _botServices.SampleQnA.GetAnswersAsync(turnContext);
            if (results.Any())
            {
                await turnContext.SendActivityAsync(MessageFactory.Text(results.First().Answer), cancellationToken);
            }
            else
            {
                await turnContext.SendActivityAsync(MessageFactory.Text("Sorry, could not find an answer in the Q and A, kindly type a question as 'what is the symptoms of covid19'."), cancellationToken);
            }
        }

        private static JObject readFileforUpdate_jobj(string filepath)
        {
            var json = File.ReadAllText(filepath);
            var jobj = JsonConvert.DeserializeObject(json);
            JObject Jobj_card = JObject.FromObject(jobj) as JObject;
            return Jobj_card;
        }

        private static Attachment UpdateAdaptivecardAttachment(JObject updateAttch)
        {
            var adaptiveCardAttch = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(updateAttch.ToString()),
            };
            return adaptiveCardAttch;
        }

        private static HikesNearMeRepo ProcessHikesRepo(string Url)
        {
            var webRequest = WebRequest.Create(Url) as HttpWebRequest;
            HikesNearMeRepo repositories = new HikesNearMeRepo();
            webRequest.ContentType = "application/json";
            webRequest.UserAgent = "User-Agent";
            using (var s = webRequest.GetResponse().GetResponseStream())
            {
                using (var sr = new StreamReader(s))
                {
                    var contributorsAsJson = sr.ReadToEnd();
                    repositories = JsonConvert.DeserializeObject<HikesNearMeRepo>(contributorsAsJson);
                }
            }
            return repositories;
        }
    }
}