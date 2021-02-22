using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Linq;

namespace DFSRBacklogMonitoring
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : System.Configuration.Install.Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        private void DSFRBacklogMonitoringInstaller__AfterInstall(object sender, InstallEventArgs e)
        {

        }
        protected override void OnBeforeInstall(IDictionary savedState)
        {
            Context.Parameters["assemblypath"] = "\"" + Context.Parameters["assemblypath"] + "\"";
            SetServiceName();
            base.OnBeforeInstall(savedState);
        }
        protected override void OnBeforeUninstall(IDictionary savedState)
        {
            SetServiceName();
            base.OnBeforeUninstall(savedState);
        }
        private void DSFRBacklogMonitoringInstaller_AfterInstall(object sender, InstallEventArgs e)
        {

        }
        private void SetServiceName()
        {
            if (Context.Parameters.ContainsKey("ServiceName"))
            {
                DSFRBacklogMonitoringInstaller.ServiceName = Context.Parameters["ServiceName"];
            }

            if (Context.Parameters.ContainsKey("DisplayName"))
            {
                DSFRBacklogMonitoringInstaller.DisplayName = Context.Parameters["DisplayName"];
            }
        }
    }
}
