using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;

namespace Trellexa.WebAPI.Controllers
{
    public class TrellexaAuthController : Controller
    {
        string _appKey = "TODO Set Trello App Key";
        string _baseUri = "https://trellexa.tech";
        //string _baseUri = "http://localhost:7858";


        // Called by Alexa to link accounts
        public ActionResult Authorize(string client_id, string response_type, string redirect_uri, string scope, string state)
        {
            var returnUrl = string.Format("{1}/TrellexaAuth/Return?redirect={0}&state={2}", redirect_uri, _baseUri, state);
            returnUrl = HttpUtility.UrlEncode(returnUrl);
            return new RedirectResult(string.Format("https://trello.com/1/authorize?callback_method=fragment&name=Trellexa&key={0}&scope=read,write&expiration=1day&return_url={1}", _appKey, returnUrl));
        }

        // Called by Trello after user logs in 
        // This action converts the user token from a anchor on the Url to querystring parameter
        public ActionResult Return()
        {
            return View("Return");
        }

        // Called by Return action with token as query string
        // Redirects back to Alexa
        public ActionResult Callback(string redirect, string state, string token)
        {
            return new RedirectResult(string.Format("{0}&state={2}&token_type=bearer&expires_in=86400#access_token={1}", redirect, token, state));
        }
    }
}