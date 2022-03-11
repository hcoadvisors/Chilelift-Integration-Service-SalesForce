using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HCO.DI.DA;
using HCO.DI.IntegrationFramework;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {

            IntegrationManager oIntegrationManager = new IntegrationManager();

            oIntegrationManager.StartWorkFlow();

            oIntegrationManager.StartJobs();

            Console.WriteLine("Presione cualquier tecla para finalizar");

            Console.ReadLine();
        }
    }
}
