namespace HCO.DI.AgentService
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.DIAgentServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.DIAgentServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // DIAgentServiceProcessInstaller
            // 
            this.DIAgentServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.DIAgentServiceProcessInstaller.Password = null;
            this.DIAgentServiceProcessInstaller.Username = null;
            // 
            // DIAgentServiceInstaller
            // 
            this.DIAgentServiceInstaller.Description = "HCO DI Agent Service";
            this.DIAgentServiceInstaller.DisplayName = "HCO DI Agent Service";
            this.DIAgentServiceInstaller.ServiceName = "HCODIAgent";
            this.DIAgentServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.DIAgentServiceProcessInstaller,
            this.DIAgentServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller DIAgentServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller DIAgentServiceInstaller;
    }
}