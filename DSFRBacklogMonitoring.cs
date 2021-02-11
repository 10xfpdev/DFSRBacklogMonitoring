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
        public DSFRBacklogMonitoring(string[] args)
        {
            InitializeComponent();
            string eventSourceName = "MySource";
            string logName = "MyNewLog";

            /*
             * var data = new Dictionary<string, string>();
             * foreach (var row in File.ReadAllLines(PATH_TO_FILE))
             *      data.Add(row.Split('=')[0], string.Join("=",row.Split('=').Skip(1).ToArray()));
             * Console.WriteLine(data["ServerName"]); 
             */

            if (args.Length > 0)
            {
                eventSourceName = args[0];
            }

            if (args.Length > 1)
            {
                logName = args[1];
            }

            eventLog1 = new EventLog();

            if (!EventLog.SourceExists(eventSourceName))
            {
                EventLog.CreateEventSource(eventSourceName, logName);
            }

            eventLog1.Source = eventSourceName;
            eventLog1.Log = logName;

            this.rgname = "RG_TEST";
            this.rfname = "testdfs";
            this.sendmember = "win2016dfs2";
            this.recmember = Environment.MachineName;
            string server = "http://localhost:9999";
            string metricsendpoint = "/api/v1/metrics";
            this.metricsurl = server + metricsendpoint;
            string eventsendpoint = "/api/v1/events";
            this.eventsurl = server + eventsendpoint;
            this.thishost = Environment.MachineName;

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
            timer.Interval = 120000; // 300 seconds
            timer.Elapsed += new ElapsedEventHandler(OnTimer);
            timer.Start();
            // Update the service state to Running.
            serviceStatus.dwCurrentState = ServiceState.SERVICE_RUNNING;
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        private void OnTimer(object sender, ElapsedEventArgs e)
        {
            // TODO: Insert monitoring activities here.
            eventId += 1;
            eventLog1.WriteEntry("Monitoring the System", EventLogEntryType.Information, eventId);
           
            string output = "";
            int backlog = 0;
            bool error = false;

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
                p.WaitForExit(200000);
                output = p.StandardOutput.ReadToEnd();
                eventLog1.WriteEntry(output, EventLogEntryType.Information, eventId);
            }
            catch (Exception ex)
            {
                eventLog1.WriteEntry(ex.ToString(), EventLogEntryType.Error, eventId);
            }

            if (output != "")
            {
                string[] outputlst = output.Split('\n');
                foreach (string line in outputlst)
                {
                    if (line.Contains("Backlog File Count"))
                    {
                        backlog = Convert.ToInt32(((line.Split(':'))[1]).Trim());
                        break;
                    }
                    if (line.Contains("No Backlog"))
                    {
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

            if (error == true)
            {
                // Send the backlog count to appdynamics

                string json = "[  {    \"metricName\": \"Custom Metrics|DFSR Backlog|" + rfname + "|" + thishost + "\",  \"aggregatorType\": \"OBSERVATION\", \"value\":" + backlog + "} ]";
                eventLog1.WriteEntry(json, EventLogEntryType.Information, eventId);

                try
                {
                    var httpWebRequest = (HttpWebRequest)WebRequest.Create(this.metricsurl);
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
                } catch (Exception ex)
                {
                    eventLog1.WriteEntry(ex.ToString(), EventLogEntryType.Error, eventId);
                }
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
