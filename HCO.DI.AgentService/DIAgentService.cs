
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using HCO.DI.IntegrationFramework;
using HCO.DI.Common;
using System.Threading;

namespace HCO.DI.AgentService
{
    public partial class DIAgentService : ServiceBase
    {

        private System.Threading.Timer _executeTimer;
        private static object _locker = null;
        private IntegrationManager _integrationManager;

        public DIAgentService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            StartService();
        }

        protected override void OnStop()
        {
            StopService();
        }


        private void StartService()
        {
            _integrationManager = new IntegrationManager();

            //Inicia los Jobs de Quartz
            _integrationManager.StartJobs();

            _locker = new object();

            //Iniciar el timer
            TimerCallback executeTimerDelegate = new TimerCallback(Execute);
            _executeTimer = new System.Threading.Timer(executeTimerDelegate, null, 0, 1000);

        }

        private void StopService()
        {
            //Inicia los Jobs de Quartz
            _integrationManager.StopJobs();
        }


        private void Execute(Object eventState)
        {
            //Garantiza que un hilo no pueda ejecutar el código crítico si otro hilo ya lo está ejecutando
            if (Monitor.TryEnter(_locker))
            {//Inicio del bloqueo del código crítico

                try
                {

                    _integrationManager.StartWorkFlow();
                }
                catch (Exception ex)
                {
                    Utility.WriteEntryToLog(EventLogEntryType.Error, this.GetType().Name + " - " + ex.Message + " - " + ex.StackTrace);
                }

                Monitor.Exit(_locker); //Libera el bloqueo una vez haya terminado de replicar

            }//Fin del bloqueo del código crítico

        }

    }
}
