using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HCO.DI.IntegrationFramework.Contracts;
using HCO.DI.Common;
using HCO.DI.Entities;
using HCO.DI.DA;
using HCO.DI.SB1DIAPIHelper;
using SAPbobsCOM;
using Quartz;
using Quartz.Impl;
using HCO.SB1ServiceLayerSDK;
using HCO.SB1ServiceLayerSDK.SAPB1;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HCO.DI.Chilelift
{
    [DisallowConcurrentExecution]
    class DeliveryNotes : IJob
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
            string destinationName = "SmartMaps";
            int interfaceId = interfaceParams.Id;
            string interfaceName = interfaceParams.Name;
            string refKey = "DocEntry";
            string refValue = string.Empty;
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
                string strTimeExecution = "000000";
                if (new DateTime(lastExecution.Year, lastExecution.Month, lastExecution.Day) == DateTime.Today)
                    strTimeExecution = lastExecution.ToString("HH:mm:ss").Replace(":", "");

                string nextLink = null;
                string queryOptions = "filter=UpdateDate ge '" + strLastExecution + "' " +
                    "and UpdateTS ge " + strTimeExecution + " and U_HCO_IdSalesForce ne NULL " +
                    "and U_HCO_IdSalesForce_1 eq null";

                do
                {
                    List<SalesForceDeliveryNoteView> responseList = slClient.GetHANAView<List<SalesForceDeliveryNoteView>>("SALESFORCEDELIVERYNOTE", queryOptions, out nextLink, 20);
                    queryOptions = nextLink;

                    if (responseList.Count > 0)
                    {
                        SalesForce.Auth auth = new SalesForce.Auth();
                        auth.auth(scenarioParams);

                        var invoices = (from l in responseList
                                        where l.DocEntry_1 != 0
                                        group l by new { l.DocEntry_1, l.DocEntry} into i
                                        select new { BaseEntry = i.Key.DocEntry_1, DocEntry = i.Key.DocEntry });

                        foreach(var invoice in invoices)
                        {
                            var lines = responseList.Where(l => l.DocEntry_1 == invoice.BaseEntry && l.DocEntry == invoice.DocEntry).ToList();

                            SalesForce.SalesForceDeliveryNote salesForceDeliveryNote = new SalesForce.SalesForceDeliveryNote();
                            salesForceDeliveryNote.id_cliente__c = lines.FirstOrDefault().CardCode;
                            salesForceDeliveryNote.client_name__c = lines.FirstOrDefault().CardName;
                            salesForceDeliveryNote.Direcci_n_de_env_o_SAP__c = lines.FirstOrDefault().Address.Replace("\r", "");
                            salesForceDeliveryNote.Direcci_n_de_facturaci_n_SAP__c = lines.FirstOrDefault().Address2.Replace("\r", "");
                            salesForceDeliveryNote.Nombre_de_la_factura__c = lines.FirstOrDefault().U_HCO_IdSalesForce;

                            request = JsonConvert.SerializeObject(salesForceDeliveryNote);

                            try
                            {
                                DocumentEx documentEx = new DocumentEx() { DocEntry = lines.FirstOrDefault().DocEntry};
                                response = SalesForce.ConsumeService.POST(salesForceDeliveryNote, scenarioParams, auth.Authentication, "entrega__c");

                                JObject json = JObject.Parse(response);
                                documentEx.U_HCO_IdSalesForce = json["id"].ToString();

                                message = interfaceParams.Name + " procesados correctamente";

                                if (writeInfoToLog)
                                    Utility.WriteToLog(scenarioId, scenarioName, sourceName, destinationName, interfaceId, interfaceName, LogDA.Status.Successful, refKey, lines.FirstOrDefault().DocEntry.ToString(), LogDA.ContenTypes.Json, request, response, message);

                                documentEx.DocumentLines = new List<DocumentLineEx>();

                                foreach(var line in lines)
                                {
                                    SalesForce.SalesForceDeliveryNoteLine salesForceDeliveryNoteLine = new SalesForce.SalesForceDeliveryNoteLine();
                                    salesForceDeliveryNoteLine.Nombre_de_la_entrega__c = documentEx.U_HCO_IdSalesForce;
                                    salesForceDeliveryNoteLine.id_producto_SAP__c = line.ItemCode;
                                    salesForceDeliveryNoteLine.Quantity__c = (double)line.Quantity;

                                    request = JsonConvert.SerializeObject(salesForceDeliveryNoteLine);

                                    response = SalesForce.ConsumeService.POST(salesForceDeliveryNoteLine, scenarioParams, auth.Authentication, "Producto_entregado__c");

                                    json = JObject.Parse(response);
                                    documentEx.DocumentLines.Add(new DocumentLineEx() { LineNum = line.LineNum, U_HCO_IdSalesForce = json["id"].ToString() });

                                    message = interfaceParams.Name + " procesados correctamente";

                                    if (writeInfoToLog)
                                        Utility.WriteToLog(scenarioId, scenarioName, sourceName, destinationName, interfaceId, interfaceName, LogDA.Status.Successful, refKey, line.DocEntry + " - " + line.LineNum.ToString(), LogDA.ContenTypes.Json, request, response, message);
                                }

                                slClient.Update(documentEx.DocEntry, documentEx, SB1ServiceLayerSDK.SAPB1.BoObjectTypes.oDeliveryNotes);

                            }
                            catch (Exception ex)
                            {
                                message = ex.Message;
                                if (writeErroToLog)
                                    Utility.WriteToLog(scenarioId, scenarioName, sourceName, destinationName, interfaceId, interfaceName, LogDA.Status.Failed, refKey, lines.FirstOrDefault().DocEntry.ToString(), LogDA.ContenTypes.Json, request, message, message);

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
}
