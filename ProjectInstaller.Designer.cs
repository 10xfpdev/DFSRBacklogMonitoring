namespace DFSRBacklogMonitoring
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
            this.DSFRBacklogMonitoringInstaller_ = new System.ServiceProcess.ServiceProcessInstaller();
            this.DSFRBacklogMonitoringInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // DSFRBacklogMonitoringInstaller_
            // 
            this.DSFRBacklogMonitoringInstaller_.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.DSFRBacklogMonitoringInstaller_.Password = null;
            this.DSFRBacklogMonitoringInstaller_.Username = null;
            this.DSFRBacklogMonitoringInstaller_.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.DSFRBacklogMonitoringInstaller__AfterInstall);
            // 
            // DSFRBacklogMonitoringInstaller
            // 
            this.DSFRBacklogMonitoringInstaller.Description = "Service to monitor current DFS replication backlog. It connects to AppD Machine A" +
    "gent on localhost:9999";
            this.DSFRBacklogMonitoringInstaller.DisplayName = "AppD DSFRBacklogMonitoring";
            this.DSFRBacklogMonitoringInstaller.ServiceName = "AppD DSFRBacklogMonitoring";
            this.DSFRBacklogMonitoringInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            this.DSFRBacklogMonitoringInstaller.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.DSFRBacklogMonitoringInstaller_AfterInstall);
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.DSFRBacklogMonitoringInstaller_,
            this.DSFRBacklogMonitoringInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller DSFRBacklogMonitoringInstaller_;
        private System.ServiceProcess.ServiceInstaller DSFRBacklogMonitoringInstaller;
    }
}