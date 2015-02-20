using ScorpioTech.Framework.LogServer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;

namespace ScorpioTech.netNUT.upsmon.Shared
{
    public class UPSMonThreads : IUPSMonPluginHost
    {
        /// <summary>
        /// Possible System Status values
        /// </summary>
        public enum SystemStatus
        {
            /// <summary>
            /// There is something seriously wrong
            /// </summary>
            ERROR,
            /// <summary>
            /// Some non-critical part of the system has failed
            /// </summary>
            WARNING,
            /// <summary>
            /// Everything is OK
            /// </summary>
            OK
        }

        private Thread thread;
        private volatile bool running = false;
        private ILogger log_facility;

        public static UPSMonThreads Instance { get; private set; }
        public static LogServer LoggingServer { get; private set; }

        public static UPSMonSetting Settings { get; private set; }
        public static PowerSupplyStatus PowerSupply { get; private set; }

#if DEBUG
        public const int MAXLOG = 500;
#else
        public const int MAXLOG = 250;
#endif

        public UPSMonThreads(ILogger log)
        {
            Instance = this;
            this.thread = new Thread(_exec);
            this.log_facility = log;

            string local_dir = System.Environment.GetCommandLineArgs()[0];
            local_dir = System.IO.Path.GetDirectoryName(local_dir);
            System.Environment.CurrentDirectory = local_dir;

            LoggingServer = new LogServer("netNUT upsmon Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version + " - Log Server", MAXLOG);
            LoggingServer.onClientConnect += new LogServer.ClientConnectedEvent(logServer_onClientConnect);
            LoggingServer.onClientDisconnect += new LogServer.ClientDisconnectedEvent(logServer_onClientDisconnect);
            LoggingServer.onServerException += new LogServer.LogServerExceptionEvent(logServer_onServerException);
        }

        private static void ReadConfigFile()
        {
            XmlReaderSettings readerSettings = new XmlReaderSettings();
            readerSettings.IgnoreComments = true;
            readerSettings.IgnoreWhitespace = true;

            XmlReader reader = XmlReader.Create("upsmon.xml", readerSettings);
            reader.MoveToContent();
            reader.ReadStartElement("upsmon");
            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.EndElement) break;
                if (reader.NodeType != XmlNodeType.Element)
                {
                    throw new XmlException("Malformed XML found in configuration file!");
                }

                switch (reader.Name)
                {
                    case "settings":
                        reader.ReadStartElement("settings");
                        Settings = new UPSMonSetting(reader);
                        reader.ReadEndElement();
                        break;
                    case "ups":
                        PowerSupply = new PowerSupplyStatus();
                        reader.ReadStartElement("ups");
                        while (reader.NodeType == XmlNodeType.Element && reader.Name == "monitor")
                        {
                            MonitoredUPS monups = new MonitoredUPS(reader);
                            PowerSupply.Add(monups);
                        }
                        reader.ReadEndElement();
                        break;
                }
            }
            reader.ReadEndElement();

            if(PowerSupply.Count < 1)
            {
                throw new XmlException("Invalid configuration found! You need to have at least one UPS monitored. Check <ups> section in config file.");
            }
        }

        #region Thread Management / Execution
        public void Start()
        {
            this.log_facility.DebugLog("Staring upsmon threads");
            this.thread.Start();
        }

        public void Stop()
        {
            this.Stop(30000);
        }

        public void Stop(int timeout)
        {
            this.log_facility.DebugLog("Stopping upsmon threads");
            this.running = false;
            this.thread.Join(timeout);
        }

        private void _exec()
        {
            try
            {
                this.running = true;

                Debug("Starting Log Server Thread...");
                LoggingServer.Start();
                DateTime timeoutStart = DateTime.Now;
                while (LoggingServer.Active == false)
                {
                    TimeSpan tsTimeout = DateTime.Now - timeoutStart;
                    if (tsTimeout.TotalMilliseconds > 1500)
                    {
                        throw new UPSMonFatalException("Log Server could not start!! Terminating!");
                    }
                    Thread.Sleep(1);
                }
                Debug("Log Server Thread Started");

                AppendLog("Reading configuration file...");
                try
                {
                    ReadConfigFile();
                    Debug("Configuration file successfully read");
                }
                catch(XmlException xmlex)
                {
                    AppendLog("Error reading Configuration File" + xmlex.Message);
                    this.running = false;
                }

                if( this.running)
                {
                    PowerSupply.Initialize();
                }

                bool critical = false;
                bool updatedCritical = false;
                while (this.running)
                {
                    Thread.Sleep(100);
                    PowerSupply.Update();
                    updatedCritical = PowerSupply.GoneCritical;
                    if (updatedCritical != critical)
                    {
                        if (updatedCritical == true)
                        {
                            AppendLog("Power Supply has gone critical... Shutting down!");
                            ExecuteShutdown();
                        }
                        critical = updatedCritical;
                    }
                }

                PowerSupply.End();

                Debug("Stopping Log Server Thread...");
                LoggingServer.Stop();
                LoggingServer.Join();
                Debug("Log Server Thread Stopped");
            }
            catch (UPSMonFatalException fatalEx)
            {
                throw new Exception(fatalEx.Message);
            }
            catch (Exception ex)
            {
                this.log_facility.ErrorLog("Unhandled Exception in Xelpro AMR Threads!! " + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }
        #endregion

        private void ExecuteShutdown()
        {
#if DEBUG
            this.log_facility.AppendLog("Would execute '" + Settings.ShutdownCommand.FileName + " " + Settings.ShutdownCommand.Arguments + "'");
#else
            this.log_facility.AppendLog("Executing '" + Settings.ShutdownCommand.FileName + " " + Settings.ShutdownCommand.Arguments + "'");
            Process.Start(Settings.ShutdownCommand);
#endif
        }

        void logServer_onServerException(Exception ex)
        {
            this.log_facility.ErrorLog("Log Server Error: " + ex.Message + ex.StackTrace);
        }
        void logServer_onClientConnect(string client)
        {
            Debug("New Log Server Client " + client);
        }
        void logServer_onClientDisconnect(string client)
        {
            Debug("Log Server Client " + client + " disconnected!");
        }

        public static SystemStatus Status
        {
            get
            {
                //if ((DBConn.Instance.Connection.State == ConnectionState.Closed) ||
                //    (DBConn.Instance.Connection.State == ConnectionState.Broken))
                //{
                //    return SystemStatus.ERROR;
                //}

                //if ((DataStore.Running == false) || (DataHandler.Running == false) ||
                //    (GPRSServer.Running == false))
                //{
                //    return SystemStatus.ERROR;
                //}

                //if ((WebServer.Running == false) || (AlarmManager.Running == false) ||
                //    (ConfigManager.Running == false))
                //{
                //    return SystemStatus.WARNING;
                //}

                //if (ServerThreadExtensions.DeadExtension == true)
                //{
                //    return SystemStatus.WARNING;
                //}
                return SystemStatus.OK;
            }
        }

        [Conditional("DEBUG")]
        public static void Debug(string text)
        {
            if ((LoggingServer != null) && (LoggingServer.Active == true))
            {
                LoggingServer.Log(DateTime.Now.ToString() + ": DEBUG: " + text);
            }

            Instance.log_facility.DebugLog(text);
        }

        public static void AppendLog(string line)
        {
            if ((LoggingServer != null) && (LoggingServer.Active == true))
            {
                LoggingServer.Log(DateTime.Now.ToString() + ":" + line);
            }

            Instance.log_facility.AppendLog(line);
        }

        public void DebugLog(IUPSMonPlugin sender, string line)
        {
            throw new NotImplementedException();
        }

        public void AppendLog(IUPSMonPlugin sender, string line)
        {
            throw new NotImplementedException();
        }

        public void PlugInError(string error)
        {
            throw new NotImplementedException();
        }

        public void ThrowException(Exception ex)
        {
            throw new NotImplementedException();
        }
    }
}
