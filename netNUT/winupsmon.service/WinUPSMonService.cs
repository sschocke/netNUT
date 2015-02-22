using ScorpioTech.netNUT.upsmon.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace ScorpioTech.netNUT.winupsmon.service
{
    public partial class WinUPSMonService : ServiceBase, ILogger
    {
        private static TextWriter debug_fs = null;
        private static TextWriter serverlog_fs = null;

        public WinUPSMonService()
        {
            InitializeComponent();

            new UPSMonThreads(this);
        }

        protected override void OnStart(string[] args)
        {
            this.EventLog.WriteEntry("Starting ScorpioTech Windows UPS Monitor Thread", EventLogEntryType.Information);
            serverlog_fs = StreamWriter.Synchronized(new StreamWriter(File.Open("WinUPSMon_Server.log", FileMode.Append, FileAccess.Write)));
            serverlog_fs.WriteLine();
            serverlog_fs.WriteLine();
            serverlog_fs.WriteLine("---------------------------------------------------------");
            serverlog_fs.WriteLine("ScorpioTech Windows UPS Monitor starting up @ " + DateTime.Now.ToString());
#if DEBUG
            if (debug_fs == null)
            {
                debug_fs = StreamWriter.Synchronized(new StreamWriter(File.Open("debug.log", FileMode.Create, FileAccess.Write)));
                debug_fs.WriteLine("Starting new Debug Session @ " + DateTime.Now.ToString());
            }
#endif
            UPSMonThreads.Instance.Start();
        }

        protected override void OnStop()
        {
            this.EventLog.WriteEntry("Terminating ScorpioTech Windows UPS Monitor Thread", EventLogEntryType.Information);
            UPSMonThreads.Instance.Stop();

            serverlog_fs.WriteLine("ScorpioTech Windows UPS Monitor stopped @ " + DateTime.Now.ToString());
            serverlog_fs.Close();
            if (debug_fs != null)
            {
                debug_fs.Close();
            }
        }

        protected override void OnShutdown()
        {
            this.RequestAdditionalTime(5000);
            this.OnStop();
        }

        public void AppendLog(string line)
        {
            string file_line = DateTime.Now.ToString() + ": " + line;
            serverlog_fs.WriteLine(file_line);

            this.EventLog.WriteEntry(line, EventLogEntryType.Information);
        }

        public void ErrorLog(string line)
        {
            string file_line = DateTime.Now.ToString() + ": " + line;
            serverlog_fs.WriteLine(file_line);

            this.EventLog.WriteEntry(line, EventLogEntryType.Error);
        }

        public void DebugLog(string line)
        {
#if DEBUG
            debug_fs.WriteLine(DateTime.Now.ToString() + ": " + line);
            this.EventLog.WriteEntry(line, EventLogEntryType.Information);
#endif
        }

        public void TraceLog(string line)
        {
#if TRACE
            DebugLog(line);
#endif
        }
    }
}
