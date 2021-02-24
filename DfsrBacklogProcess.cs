using System;
using System.Diagnostics;
using System.Threading.Tasks;

class DfsrBacklogProcess
{
    private Process p;
    private TaskCompletionSource<bool> eventHandled;
    public string stdOutput;
    public string stdError;
    public int exitCode;
    public DateTime exitTime;
    public DateTime startTime;

    public async Task RetrieveBacklog(string arguments, int timeOut)
    {
        eventHandled = new TaskCompletionSource<bool>();

        using (p = new Process())
        {
            try
            {
                // Start a process to print a file and raise an event when done.
                p.StartInfo.FileName = @"c:\Windows\system32\dfsrdiag.exe";
                p.StartInfo.Arguments = arguments;
                p.StartInfo.CreateNoWindow = true;
                p.EnableRaisingEvents = true;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.Exited += new EventHandler(p_Exited);
                p.Start();
            }
            catch (Exception ex)
            {
                throw new Exception("Error in DfsrBacklogProcess", ex);
            }

            // Wait for Exited event, but not more than 30 seconds.
            await Task.WhenAny(eventHandled.Task, Task.Delay(timeOut));
        }
    }

    // Handle Exited event and display process information.
    private void p_Exited(object sender, System.EventArgs e)
    {
        this.startTime = p.StartTime;
        this.exitTime = p.ExitTime;
        this.exitCode = p.ExitCode;
        this.stdOutput = p.StandardOutput.ReadToEnd();
        this.stdError = p.StandardError.ReadToEnd();
        eventHandled.TrySetResult(true);
    }

}