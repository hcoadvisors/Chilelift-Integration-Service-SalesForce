﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HCO.DI.Common;
using HCO.DI.Entities;
using HCO.DI.DA;
using Quartz;
using HCO.SB1ServiceLayerSDK;
using HCO.SB1ServiceLayerSDK.SAPB1;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace HCO.DI.Chilelift
{
    [DisallowConcurrentExecution]
    class PriceList : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            JobKey key = context.JobDetail.Key;
            JobDataMap dataMap = context.JobDetail.JobDataMap;

            Settings.ScenarioRow scenarioParams = (Settings.ScenarioRow)dataMap.Get("scenarioParams");
            Settings.InterfaceRow interfaceParams = (Settings.InterfaceRow)dataMap.Get("interfaceParams");

            int scenarioId = scenarioParams.Id;
            string scenarioName = scenarioParams.ScenarioName;
            string sourceName = scenarioParams.Sb1_Database;
            string destinationName = "SalesForce";
            int interfaceId = interfaceParams.Id;
            string interfaceName = interfaceParams.Name;
            string refKey = "ListNum";
            string request = string.Empty;
            string response = string.Empty;
            string message = string.Empty;
            bool writeInfoToLog = scenarioParams.LogInfo;
            bool writeErroToLog = scenarioParams.LogError;
            bool updateLastIntegrationDate = true;

            ServiceLayerClient slClient = null;

            try
            {
                string endpoint = scenarioParams.Sb1_ServiceLayerEndpoint;

                slClient = new ServiceLayerClient(endpoint);

                slClient.Login(scenarioParams.Sb1_Database,
                               scenarioParams.Sb1_User,
                               Utility.Decrypt(scenarioParams.Sb1_Password));                

                string nextLink = null;
                string queryOptions = "filter=U_HCO_IdSalesForce eq NULL";

                do
                {
                    List<PriceListEx> responseList = slClient.Get<List<PriceListEx>>(SB1ServiceLayerSDK.SAPB1.BoObjectTypes.oPriceLists, queryOptions, out nextLink, 20);
                    queryOptions = nextLink;

                    if (responseList.Count > 0)
                    {
                        SalesForce.Auth auth = new SalesForce.Auth();
                        auth.auth(scenarioParams);

                        foreach (PriceListEx data in responseList)
                        {
                            SalesForce.SalesForcePriceList salesForcePriceList = new SalesForce.SalesForcePriceList();
                            salesForcePriceList.Codigo_en_SAP__c = data.PriceListNo.ToString();
                            salesForcePriceList.Name = data.PriceListName;
                            salesForcePriceList.IsActive = true;

                            request = JsonConvert.SerializeObject(salesForcePriceList);

                            try
                            {
                                response = SalesForce.ConsumeService.POST(salesForcePriceList, scenarioParams, auth.Authentication, "Pricebook2");

                                JObject json = JObject.Parse(response);
                                string idSalesForce = json["id"].ToString();

                                slClient.Update(data.PriceListNo, new PriceListEx() { PriceListNo = data.PriceListNo, U_HCO_IdSalesForce = idSalesForce }, SB1ServiceLayerSDK.SAPB1.BoObjectTypes.oPriceLists);                                

                                message = interfaceParams.Name + " procesados correctamente";

                                if (writeInfoToLog)
                                    Utility.WriteToLog(scenarioId, scenarioName, sourceName, destinationName, interfaceId, interfaceName, LogDA.Status.Successful, refKey, data.PriceListNo.ToString(), LogDA.ContenTypes.Json, request, response, message);

                            }
                            catch (Exception ex)
                            {
                                message = ex.Message;
                                if (writeErroToLog)
                                    Utility.WriteToLog(scenarioId, scenarioName, sourceName, destinationName, interfaceId, interfaceName, LogDA.Status.Failed, refKey, data.PriceListNo.ToString(), LogDA.ContenTypes.Json, request, message, message);

                                updateLastIntegrationDate = false;
                            }
                        }
                    }
                }
                while (nextLink != null);

                if(updateLastIntegrationDate)
                    Utility.UpdateLastIntegrationDate(scenarioId, interfaceId, DateTime.Now);
            }
            catch (ServiceLayerException ex)
            {
                message = string.Format("Error Code: {0} - Message: {1}", ex.ErrorCode, ex.Message);
                if (writeErroToLog)
                    Utility.WriteToLog(scenarioId, scenarioName, sourceName, destinationName, interfaceId, interfaceName, LogDA.Status.Failed, String.Empty, String.Empty, LogDA.ContenTypes.Json, String.Empty, String.Empty, message);
            }
            catch (Exception ex)
            {
                message = string.Format("Error Code: {0} - Message: {1}", ex.HResult, ex.Message);
                if (writeErroToLog)
                    Utility.WriteToLog(scenarioId, scenarioName, sourceName, destinationName, interfaceId, interfaceName, LogDA.Status.Failed, String.Empty, String.Empty, LogDA.ContenTypes.Json, String.Empty, String.Empty, message);
            }
            finally
            {
                if (slClient != null)
                    slClient.Logout();  //Cierra la sesión
            }
            return Task.CompletedTask;
        }
    }
}
