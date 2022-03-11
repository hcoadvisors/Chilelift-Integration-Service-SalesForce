using HCO.DI.Common;
using HCO.DI.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;

namespace HCO.DI.Chilelift.SalesForce
{
    class Auth
    {
        public string Authentication { get; set; }

        public void auth(Settings.ScenarioRow scenarioParams)
        {
            var client = new RestClient(scenarioParams.RestAPI_Endpoint1);
            var request = new RestRequest($"/token?client_id=" +
                scenarioParams.RestAPI_Account +
                "&client_secret=" + Utility.Decrypt(scenarioParams.RestAPI_APIKey) + 
                "&username=" + scenarioParams.RestAPI_User +
                "&password=" + Utility.Decrypt(scenarioParams.RestAPI_Password) +
                "&grant_type=password", Method.POST);

            var response = client.Execute(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
                throw new Exception(response.StatusCode + " : " + response.Content);

            JObject json = JObject.Parse(response.Content);
            this.Authentication = json["access_token"].ToString();
        }
    }
}
