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
            string parameter = "DFSRBacklogMonitoring\" \"AppDynamics";
            Context.Parameters["assemblypath"] = "\"" + Context.Parameters["assemblypath"] + "\" \"" + parameter + "\"";
            base.OnBeforeInstall(savedState);
        }
    }
}
