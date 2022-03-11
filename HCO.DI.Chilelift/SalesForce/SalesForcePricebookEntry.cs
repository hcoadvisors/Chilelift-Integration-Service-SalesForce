using HCO.DI.Entities;
using Newtonsoft.Json;
using RestSharp;
using System;

namespace HCO.DI.Chilelift.SalesForce
{
    class SalesForcePricebookEntry
    {
        public string Pricebook2Id { get; set; }
        public string Product2Id { get; set; }
        public string PricebookId_SAP__c { get; set; }
        public string ProductId_SAP__c { get; set; }
        public string UnitPrice { get; set; }
        public bool IsActive { get; set; }
    }
}
