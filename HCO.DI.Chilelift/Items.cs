using System;
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
    class Items : IJob
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
            string refKey = "ItemCode";
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

                DateTime lastExecution = Utility.GetLastIntegrationDate(scenarioId, interfaceId);
                string strLastExecution = lastExecution.ToString("yyyy-MM-dd");
                string strTimeExecution = "00:00:00";
                if (new DateTime(lastExecution.Year, lastExecution.Month, lastExecution.Day) == DateTime.Today)
                    strTimeExecution = lastExecution.ToString("HH:mm:ss");

                string nextLink = null;
                string queryOptions = "filter=UpdateDate ge '" + strLastExecution + "' " +
                    "and UpdateTime ge '" + strTimeExecution + "' and U_HCO_IdSalesForce eq NULL";

                do
                {
                    List<Item> responseList = slClient.Get<List<Item>>(SB1ServiceLayerSDK.SAPB1.BoObjectTypes.oItems, queryOptions, out nextLink, 20);
                    queryOptions = nextLink;

                    if(responseList.Count > 0)
                    {
                        SalesForce.Auth auth = new SalesForce.Auth();
                        auth.auth(scenarioParams);

                        foreach (Item data in responseList)
                        {
                            SalesForce.SalesForceItem salesForceItem = new SalesForce.SalesForceItem();
                            salesForceItem.ProductCode = data.ItemCode;
                            salesForceItem.Name = data.ItemName;
                            salesForceItem.Family = data.ItemsGroupCode.ToString();
                            salesForceItem.IsActive = data.Valid.Equals("tYES") ? true : false;
                            salesForceItem.Description = data.User_Text;
                            SB1ServiceLayerSDK.SAPB1.ItemGroups itemGroups = slClient.GetByKey<SB1ServiceLayerSDK.SAPB1.ItemGroups>(data.ItemsGroupCode, SB1ServiceLayerSDK.SAPB1.BoObjectTypes.oItemGroups);
                            salesForceItem.Marca__c = itemGroups.GroupName;

                            request = JsonConvert.SerializeObject(salesForceItem);

                            try
                            {
                                response = SalesForce.ConsumeService.POST(salesForceItem, scenarioParams, auth.Authentication, "Product2");

                                JObject json = JObject.Parse(response);
                                string idSalesForce = json["id"].ToString();

                                slClient.Update(data.ItemCode, new ItemEx() { ItemCode = data.ItemCode, U_HCO_IdSalesForce = idSalesForce }, SB1ServiceLayerSDK.SAPB1.BoObjectTypes.oItems);

                                string queryOptionsPriceList = "filter=U_HCO_Estandar eq 'Y'";
                                List<PriceListEx> priceLists = slClient.Get<List<PriceListEx>>(SB1ServiceLayerSDK.SAPB1.BoObjectTypes.oPriceLists, queryOptionsPriceList);

                                if(priceLists.Count > 0)
                                {
                                    SalesForce.SalesForcePricebookEntry salesForcePricebookEntry = new SalesForce.SalesForcePricebookEntry();
                                    salesForcePricebookEntry.Pricebook2Id = priceLists.FirstOrDefault().U_HCO_IdSalesForce;
                                    salesForcePricebookEntry.Product2Id = idSalesForce;
                                    salesForcePricebookEntry.PricebookId_SAP__c = priceLists.FirstOrDefault().PriceListNo.ToString();
                                    salesForcePricebookEntry.ProductId_SAP__c = data.ItemCode;
                                    salesForcePricebookEntry.UnitPrice = data.ItemPrices.Where(i => i.PriceList == priceLists.FirstOrDefault().PriceListNo).FirstOrDefault().Price.ToString();
                                    salesForcePricebookEntry.IsActive = true;

                                    SalesForce.ConsumeService.POST(salesForcePricebookEntry, scenarioParams, auth.Authentication, "PricebookEntry");
                                }
                                else
                                {
                                    throw new Exception("No se encontró la lista de precios estandar en SAP");
                                }

                                message = interfaceParams.Name + " procesados correctamente";

                                if (writeInfoToLog)
                                    Utility.WriteToLog(scenarioId, scenarioName, sourceName, destinationName, interfaceId, interfaceName, LogDA.Status.Successful, refKey, data.ItemCode, LogDA.ContenTypes.Json, request, response, message);
                            }
                            catch (Exception ex)
                            {
                                message = ex.Message;
                                if (writeErroToLog)
                                    Utility.WriteToLog(scenarioId, scenarioName, sourceName, destinationName, interfaceId, interfaceName, LogDA.Status.Failed, refKey, data.ItemCode, LogDA.ContenTypes.Json, request, message, message);

                                updateLastIntegrationDate = false;
                            }
                        }                        
                    }                    
                }
                while (nextLink != null);

                if (updateLastIntegrationDate)
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

    public class ItemEx : Item
    {
        public string U_HCO_IdSalesForce { get; set; }
    }
}
