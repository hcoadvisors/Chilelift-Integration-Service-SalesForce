using HCO.SB1ServiceLayerSDK.SAPB1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HCO.DI.Chilelift
{
    class PriceListEx : SB1ServiceLayerSDK.SAPB1.PriceList
    {
        public string U_HCO_Estanadar { get; set; }
        public string U_HCO_IdSalesForce{ get; set; }

    }
}
