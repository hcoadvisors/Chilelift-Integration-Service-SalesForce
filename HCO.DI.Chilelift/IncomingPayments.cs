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
    class IncomingPayments : IJob
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
                string strTimeExecution = "00:00:00";
                if (new DateTime(lastExecution.Year, lastExecution.Month, lastExecution.Day) == DateTime.Today)
                    strTimeExecution = lastExecution.ToString("HH:mm:ss");

                string nextLink = null;
                string queryOptions = "filter=U_HCO_IdSalesForce eq null";

                do
                {
                    List<PaymentEx> responseList = slClient.Get<List<PaymentEx>>(SB1ServiceLayerSDK.SAPB1.BoObjectTypes.oIncomingPayments, queryOptions, out nextLink, 20);
                    queryOptions = nextLink;

                    if (responseList.Count > 0)
                    {
                        SalesForce.Auth auth = new SalesForce.Auth();
                        auth.auth(scenarioParams);

                        foreach (PaymentEx data in responseList)
                        {
                            foreach (var line in data.PaymentInvoices)
                            {
                                SalesForce.SalesForcePayment salesForcePayment = new SalesForce.SalesForcePayment();
                                salesForcePayment.Nombre_de_la_factura_SAP__c = line.DocEntry.ToString();
                                salesForcePayment.Codigo_en_SAP__c = data.CardCode;
                                salesForcePayment.Total_pagado__c = (double)line.SumApplied;
                                salesForcePayment.Nombre_de_la_cuenta_SAP__c = data.TransferAccount;

                                request = JsonConvert.SerializeObject(salesForcePayment);

                                try
                                {
                                    response = SalesForce.ConsumeService.POST(salesForcePayment, scenarioParams, auth.Authentication, "pagos__c");
                                    JObject json = JObject.Parse(response);
                                    data.U_HCO_IdSalesForce = json["id"].ToString();
                                    line.U_HCO_IdSalesForce = json["id"].ToString();

                                    slClient.Update(data.DocEntry, data, SB1ServiceLayerSDK.SAPB1.BoObjectTypes.oIncomingPayments);

                                    message = interfaceParams.Name + " procesados correctamente";

                                    if (writeInfoToLog)
                                        Utility.WriteToLog(scenarioId, scenarioName, sourceName, destinationName, interfaceId, interfaceName, LogDA.Status.Successful, refKey, data.DocEntry + " - " + line.DocEntry, LogDA.ContenTypes.Json, request, response, message);

                                }
                                catch (Exception ex)
                                {
                                    message = ex.Message;
                                    if (writeErroToLog)
                                        Utility.WriteToLog(scenarioId, scenarioName, sourceName, destinationName, interfaceId, interfaceName, LogDA.Status.Failed, refKey, data.DocEntry + " - " + line.DocEntry, LogDA.ContenTypes.Json, request, message, message);

                                    updateLastIntegrationDate = false;
                                }
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

    public class PaymentEx : Payment
    {
        public string U_HCO_IdSalesForce { get; set; }
        public List<PaymentInvoicesEx> PaymentInvoices { get; set; }
    }

    public class PaymentInvoicesEx : PaymentInvoice
    {
        public string U_HCO_IdSalesForce { get; set; }
    }
}
