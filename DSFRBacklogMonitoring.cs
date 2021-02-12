using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Text;
using System.Timers;
using static System.Net.Mime.MediaTypeNames;

namespace DFSRBacklogMonitoring
{
    public partial class DSFRBacklogMonitoring : ServiceBase
    {
        private EventLog eventLog1;
        private int eventId = 1;
        private string rgname;
        private string rfname;
        private string sendmember;
        private string recmember;
        private string metricsurl;
        private string eventsurl;
        private string thishost;
        private string appdMachAgent;
        private string metricsendpoint;
        private string eventsendpoint;
        private string configFile;
        private int checkInterval;
        private int pTimeOut;
        public DSFRBacklogMonitoring(string[] args)
        {
            InitializeComponent();

            this.configFile = AppDomain.CurrentDomain.BaseDirectory + "config.properties";
            var data = new Dictionary<string, string>();

            try
            {
                foreach (var row in File.ReadAllLines(configFile))
                {
                    data.Add((row.Split('=')[0]).Trim(), (row.Split('=')[1]).Trim());
                }
                string eventSourceName = data["eventlogsource"];
                string logName = data["eventloglogname"];

                if (!EventLog.SourceExists(eventSourceName))
                {
                    EventLog.CreateEventSource(eventSourceName, logName);
                }
                this.eventLog1 = new EventLog();
                this.eventLog1.Source = eventSourceName;
                this.eventLog1.Log = logName;

                this.rgname = data["rgname"];
                this.rfname = data["rfname"];
                this.sendmember = data["sendmember"];
                this.thishost = Environment.MachineName;
                this.recmember = this.thishost;
                this.appdMachAgent = data["appdagent"];
                this.metricsendpoint = data["metricsendpoint"];
                this.metricsurl = appdMachAgent + metricsendpoint;
                this.eventsendpoint = data["eventsendpoint"];
                this.eventsurl = appdMachAgent + eventsendpoint;
                this.checkInterval = Convert.ToInt32(data["checkinterval"]);
                this.pTimeOut = Convert.ToInt32(data["processtimeout"]);
            }catch (Exception ex)
            {
                using (StreamWriter writer = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "error.log"))
                {
                    writer.WriteLine(DateTime.UtcNow.ToString() + ";;" + this.thishost);
                    writer.WriteLine("Make sure " + this.configFile + " exists.\nIt must contain values for\nrgname =\nrfname =\nsendmember =\nappdagent =\nmetricsendpoint =\neventsendpoint =\ncheckinterval =\nprocesstimeout =\n");
                    writer.WriteLine("Pre Requisites:\n.NET Framework 4.0\nUser must be added to Distributed COM Users local group\nUser must have wmi permissions for Microsoft Dfs (wmimgmt.msc)\nUser must have delegate permissions for the DFS replication group\n");
                    writer.WriteLine(ex.ToString());

                }
                throw new Exception("See error.log", ex);
            }
        }

        protected override void OnStart(string[] args)
        {
            // Update the service state to Start Pending.
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_START_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            eventLog1.WriteEntry("In OnStart.", EventLogEntryType.Information, eventId);
            // Set up a timer that triggers every minute.
            Timer timer = new Timer();
            timer.Interval = this.checkInterval;
            timer.Elapsed += new ElapsedEventHandler(OnTimer);
            timer.Start();
            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        private void OnTimer(object sender, ElapsedEventArgs e)
        {
            eventId += 1;
            eventLog1.WriteEntry("StartingMonitoring the System", EventLogEntryType.Information, eventId);

            string output = "";
            int backlog = 0;
            bool error = false;
            string json = "";
            Process p;
            p = new Process();
            try
            {
                p.StartInfo.FileName = @"c:\Windows\system32\dfsrdiag.exe";
                p.StartInfo.Arguments = "backlog /RGName:" + this.rgname + " /RFName:" + this.rfname + " /ReceivingMember:" + this.recmember + " /SendingMember:" + this.sendmember;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                p.StartInfo.CreateNoWindow = true;
                p.Start();
                p.WaitForExit(this.pTimeOut);
                output = p.StandardOutput.ReadToEnd();
            }
            catch (Exception ex)
            {
                eventLog1.WriteEntry(ex.ToString(), EventLogEntryType.Error, eventId);
                json = "[  { \"eventSeverity\": \"ERROR\", \"type\": \"DFS_Replication\", \"summaryMessage\": \"" + ex.ToString() + "\", \"properties\": { \"Server\": {" + "\"" + thishost + "\" }, \"RGroup\": { \"" + rgname + "\" }, \"Folder\": { \"" + rfname + "\" } }, \"details\": { \"Error\": \"" + output + "\", \"Server\": \"" + thishost + "\", \"RGroup\": \"" + rgname + "\", \"Folder\": \"" + rfname + "\"}  }]";
                SendToAppD(json, this.eventsurl);
            }

            if (output != "")
            {
                string[] outputlst = output.Split('\n');
                foreach (string line in outputlst)
                {
                    if (line.Contains("Backlog File Count"))
                    {
                        backlog = Convert.ToInt32(((line.Split(':'))[1]).Trim());
                        error = false;
                        break;
                    }
                    if (line.Contains("No Backlog"))
                    {
                        error = false;
                        break;
                    }
                    else
                    {
                        error = true;
                    }
                }
            }
            else
            {
                error = true;
            }

            if (!error)
            {
                // Send the backlog count to appdynamics
                json = "[  {    \"metricName\": \"Custom Metrics|DFSR Backlog|" + rfname + "|" + thishost + "\",  \"aggregatorType\": \"OBSERVATION\", \"value\":" + backlog + "} ]";
                eventLog1.WriteEntry(json, EventLogEntryType.Information, eventId);
                SendToAppD(json, this.metricsurl);
            }
            else
            {
                eventLog1.WriteEntry(output, EventLogEntryType.Error, eventId);
                // Send the error to appdynamics
                json = "[  { \"eventSeverity\": \"ERROR\", \"type\": \"DFS_Replication\", \"summaryMessage\": \"" + output + "\", \"properties\": { \"Server\": {" + "\"" + thishost + "\" }, \"RGroup\": { \"" + rgname + "\" }, \"Folder\": { \"" + rfname + "\" } }, \"details\": { \"Error\": \"" + output + "\", \"Server\": \"" + thishost + "\", \"RGroup\": \"" + rgname + "\", \"Folder\": \"" + rfname + "\"}  }]";
                SendToAppD(json, this.eventsurl);

            }
        }

        private void SendToAppD(string json, string url)
        {
            eventLog1.WriteEntry("Sending to AppD;;" + json + ";;" + url, EventLogEntryType.Information, eventId);
            try
            {
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(json);
                }

                var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                    eventLog1.WriteEntry(result, EventLogEntryType.Information, eventId);
                }
            }
            catch (Exception ex)
            {
                eventLog1.WriteEntry(ex.ToString() + '\n' + url + '\n' + json, EventLogEntryType.Error, eventId);
            }
        }

        protected override void OnStop()
        {
            // Update the service state to Stop Pending.
            ServiceStatus serviceStatus = new ServiceStatus();
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOP_PENDING;
            serviceStatus.dwWaitHint = 100000;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
            eventLog1.WriteEntry("In OnStop.", EventLogEntryType.Information, eventId);
            // Update the service state to Stopped.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_STOPPED;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }
        protected override void OnContinue()
        {
            eventLog1.WriteEntry("In OnContinue.", EventLogEntryType.Information, eventId);
        }
        public enum ServiceState
        {
            SERVICE_STOPPED = 0x00000001,
            SERVICE_START_PENDING = 0x00000002,
            SERVICE_STOP_PENDING = 0x00000003,
            SERVICE_RUNNING = 0x00000004,
            SERVICE_CONTINUE_PENDING = 0x00000005,
            SERVICE_PAUSE_PENDING = 0x00000006,
            SERVICE_PAUSED = 0x00000007,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct ServiceStatus
        {
            public int dwServiceType;
            public ServiceState dwCurrentState;
            public int dwControlsAccepted;
            public int dwWin32ExitCode;
            public int dwServiceSpecificExitCode;
            public int dwCheckPoint;
            public int dwWaitHint;
        };
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);
    }
}
