using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HCO.DI.Chilelift
{
    class SalesForceDeliveryNoteView
    {
        public string U_HCO_IdSalesForce { get; set; }
        public int DocEntry { get; set; }
        public string ItemCode { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string Address { get; set; }
        public string Address2 { get; set; }
        public int DocEntry_1 { get; set; }
        public int UpdateTS { get; set; }
        public string UpdateDate { get; set; }
        public double Quantity { get; set; }
        public int LineNum { get; set; }
        public string U_HCO_IdSalesForce_1 { get; set; }
    }
}
