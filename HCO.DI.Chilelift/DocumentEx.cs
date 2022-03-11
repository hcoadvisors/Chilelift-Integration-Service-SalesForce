using HCO.SB1ServiceLayerSDK.SAPB1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HCO.DI.Chilelift
{
    class DocumentEx : Document
    {        
        public string U_HCO_IdSalesForce { get; set; }
        public List<DocumentLineEx> DocumentLines { get; set; }
    }

    class DocumentLineEx : DocumentLine
    {
        public string U_HCO_IdSalesForce { get; set; }
    }
}
