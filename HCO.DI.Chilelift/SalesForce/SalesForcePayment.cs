using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HCO.DI.Chilelift.SalesForce
{
    class SalesForcePayment
    {
        public string Nombre_de_la_factura_SAP__c { get; set; }
        public string Codigo_en_SAP__c { get; set; }
        public double Total_pagado__c { get; set; }
        public string Nombre_de_la_cuenta_SAP__c { get; set; }
    }
}
