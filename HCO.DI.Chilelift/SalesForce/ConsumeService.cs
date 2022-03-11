using HCO.DI.Entities;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HCO.DI.Chilelift.SalesForce
{
    static class ConsumeService
    {
        public static string POST(object objeto, Settings.ScenarioRow scenarioParams, string authorization, string endPoint)
        {
            var client = new RestClient(scenarioParams.RestAPI_Endpoint2);
            var request = new RestRequest($"/" + endPoint, Method.POST);
            request.AddHeader("Authorization", "Bearer " + authorization);
            request.RequestFormat = DataFormat.Json;
            request.AddParameter("application/json", JsonConvert.SerializeObject(objeto), ParameterType.RequestBody);
            var response = client.Execute(request);
            if (response.StatusCode != System.Net.HttpStatusCode.Created)
                throw new Exception(response.StatusCode + " : " + response.Content);

            return response.Content;
        }

    }
}
