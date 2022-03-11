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
    class SalesForceItem
    {
        public string ProductCode { get; set; }
        public string Name { get; set; }
        public string Ficha_tecnica__c { get; set; }
        public string Family { get; set; }
        public string Marca__c { get; set; }
        public bool IsActive { get; set; }
        public string Description { get; set; }        
    }
}
