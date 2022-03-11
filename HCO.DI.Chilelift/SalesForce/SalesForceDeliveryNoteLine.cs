using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HCO.DI.Chilelift.SalesForce
{
    class SalesForceDeliveryNoteLine
    {
        public string Nombre_de_la_entrega__c { get; set; }
        public string id_producto_SAP__c { get; set; }
        public double Quantity__c { get; set; }
    }
}
