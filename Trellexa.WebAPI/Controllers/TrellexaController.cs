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
using Trellexa.WebAPI.Lib;
using System.Net.Http;

namespace Trellexa.WebAPI.Controllers
{
    public class TrellexaController: ApiController
    {
        // Alexa entry point
        [System.Web.Http.HttpPost]
        [System.Web.Http.HttpGet]
        public async Task<HttpResponseMessage> Test()
        {
            // All the real logic is in this Speechlet
            var speechlet = new TrellexaSpeechlet();
            return await speechlet.GetResponseAsync(ControllerContext.Request);
        }
    }
}