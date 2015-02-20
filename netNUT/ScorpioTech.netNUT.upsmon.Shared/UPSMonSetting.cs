using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;

namespace ScorpioTech.netNUT.upsmon.Shared
{
    [Flags]
    public enum UPSMonStatus
    {
        NOCOMMS =     0x000000,
        ONLINE  =     0x000001,
        ONBATT  =     0x000002,
        LOWBATT =     0x000004,
        HIGHBATT =    0x000008,
        REPLACEBATT = 0x000010,
        CHARGING =    0x000020,
        DISCHARGING = 0x000040,
        BYPASS =      0x000080,
        CALIBRATION = 0x000100,
        OFF =         0x000200,
        OVERLOADED =  0x000400,
        TRIM =        0x000800,
        BOOST =       0x001000,
        FSD =         0x002000,
        ALLUPSD =     0x00FFFF,
        COMMOK =      0x010000,
        COMMBAD =     0x020000,
        SHUTDOWN =    0x040000
    }
    [Flags]
    public enum UPSMonNotifyFlag
    {
        IGNORE = 0,
        SYSLOG = 1,
        EXEC = 2,
        WALL = 4
    }
    public class UPSMonSetting
    {
        public int MinSupplies { get; private set; }
        public ProcessStartInfo ShutdownCommand { get; private set; }
        public string NotifyCommand { get; private set; }
        public TimeSpan PollFrequency { get; private set; }
        public TimeSpan PollFrequencyAlert { get; private set; }
        public TimeSpan HostSync { get; private set; }
        public TimeSpan DeadTime { get; private set; }
        public TimeSpan ReplaceBattWarnTime { get; private set; }
        public TimeSpan NoCommsWarnTime { get; private set; }
        public TimeSpan FinalDelay { get; private set; }
        public Dictionary<UPSMonStatus, string> NotifyMessage { get; private set; }
        public Dictionary<UPSMonStatus, UPSMonNotifyFlag> NotifyFlag { get; private set; }

        protected UPSMonSetting()
        {
            this.MinSupplies = 1;
            this.ShutdownCommand = new ProcessStartInfo();
            this.ShutdownCommand.FileName = "shutdown.exe";
            this.ShutdownCommand.Arguments = "/p /f /d u:6:12 /c \"upsmon shutdown due to critical UPS battery\"";
            this.ShutdownCommand.WorkingDirectory = Environment.SystemDirectory;
            this.NotifyCommand = String.Empty;
            this.PollFrequency = new TimeSpan(0, 0, 5);
            this.PollFrequencyAlert = new TimeSpan(0, 0, 5);
            this.HostSync = new TimeSpan(0, 0, 15);
            this.DeadTime = new TimeSpan(0, 0, 15);
            this.ReplaceBattWarnTime = new TimeSpan(0, 0, 43200);
            this.NoCommsWarnTime = new TimeSpan(0, 0, 300);
            this.FinalDelay = new TimeSpan(0, 0, 5);
            this.NotifyMessage = new Dictionary<UPSMonStatus, string>();
            this.NotifyMessage.Add(UPSMonStatus.ONLINE, "UPS %s on line power");
            this.NotifyMessage.Add(UPSMonStatus.ONBATT, "UPS %s on battery");
            this.NotifyMessage.Add(UPSMonStatus.LOWBATT, "UPS %s battery is low");
            this.NotifyMessage.Add(UPSMonStatus.FSD, "UPS %s: forced shutdown in progress");
            this.NotifyMessage.Add(UPSMonStatus.COMMOK, "Communications with UPS %s established");
            this.NotifyMessage.Add(UPSMonStatus.COMMBAD, "Communications with UPS %s lost");
            this.NotifyMessage.Add(UPSMonStatus.SHUTDOWN, "Auto logout and shutdown proceeding");
            this.NotifyMessage.Add(UPSMonStatus.REPLACEBATT, "UPS %s battery needs to be replaced");
            this.NotifyMessage.Add(UPSMonStatus.NOCOMMS, "UPS %s is unavailable");
            this.NotifyFlag = new Dictionary<UPSMonStatus, UPSMonNotifyFlag>();
            this.NotifyFlag.Add(UPSMonStatus.ONLINE, UPSMonNotifyFlag.SYSLOG | UPSMonNotifyFlag.WALL);
            this.NotifyFlag.Add(UPSMonStatus.ONBATT, UPSMonNotifyFlag.SYSLOG | UPSMonNotifyFlag.WALL);
            this.NotifyFlag.Add(UPSMonStatus.LOWBATT, UPSMonNotifyFlag.SYSLOG | UPSMonNotifyFlag.WALL);
            this.NotifyFlag.Add(UPSMonStatus.FSD, UPSMonNotifyFlag.SYSLOG | UPSMonNotifyFlag.WALL);
            this.NotifyFlag.Add(UPSMonStatus.COMMOK, UPSMonNotifyFlag.SYSLOG | UPSMonNotifyFlag.WALL);
            this.NotifyFlag.Add(UPSMonStatus.COMMBAD, UPSMonNotifyFlag.SYSLOG | UPSMonNotifyFlag.WALL);
            this.NotifyFlag.Add(UPSMonStatus.SHUTDOWN, UPSMonNotifyFlag.SYSLOG | UPSMonNotifyFlag.WALL);
            this.NotifyFlag.Add(UPSMonStatus.REPLACEBATT, UPSMonNotifyFlag.SYSLOG | UPSMonNotifyFlag.WALL);
            this.NotifyFlag.Add(UPSMonStatus.NOCOMMS, UPSMonNotifyFlag.SYSLOG | UPSMonNotifyFlag.WALL);
        }

        public UPSMonSetting(XmlReader reader)
            : this()
        {
            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.EndElement) break;
                if (reader.NodeType != XmlNodeType.Element)
                {
                    throw new XmlException("Malformed XML found in configuration file!");
                }

                switch (reader.Name)
                {
                    case "minsupplies":
                        this.MinSupplies = reader.ReadElementContentAsInt();
                        break;
                    case "shutdowncmd":
                        reader.ReadStartElement("shutdowncmd");
                        this.ShutdownCommand = populateProgramDetails(reader, this.ShutdownCommand);
                        reader.ReadEndElement();
                        break;
                    case "notifycmd":
                        this.NotifyCommand = reader.ReadElementContentAsString();
                        break;
                    case "pollfreq":
                        int pollFreqSecs = reader.ReadElementContentAsInt();
                        this.PollFrequency = new TimeSpan(0, 0, pollFreqSecs);
                        break;
                    case "pollfreqalert":
                        int pollFreqAlertSecs = reader.ReadElementContentAsInt();
                        this.PollFrequencyAlert = new TimeSpan(0, 0, pollFreqAlertSecs);
                        break;
                    case "hostsync":
                        int hostSyncSecs = reader.ReadElementContentAsInt();
                        this.HostSync = new TimeSpan(0, 0, hostSyncSecs);
                        break;
                    case "deadtime":
                        int deadTimeSecs = reader.ReadElementContentAsInt();
                        this.DeadTime = new TimeSpan(0, 0, deadTimeSecs);
                        break;
                    case "rbwarntime":
                        int rbWarnSecs = reader.ReadElementContentAsInt();
                        this.ReplaceBattWarnTime = new TimeSpan(0, 0, rbWarnSecs);
                        break;
                    case "nocommwarntime":
                        int nocommWarnSecs = reader.ReadElementContentAsInt();
                        this.NoCommsWarnTime = new TimeSpan(0, 0, nocommWarnSecs);
                        break;
                    case "finaldelay":
                        int finalDelaySecs = reader.ReadElementContentAsInt();
                        this.FinalDelay = new TimeSpan(0, 0, finalDelaySecs);
                        break;
                    case "notifymsg":
                        reader.ReadStartElement("notifymsg");
                        populateNotificationMessages(reader);
                        reader.ReadEndElement();
                        break;
                    case "notifyflag":
                        reader.ReadStartElement("notifyflag");
                        populateNotificationFlags(reader);
                        reader.ReadEndElement();
                        break;
                }
            }
        }

        private ProcessStartInfo populateProgramDetails(XmlReader reader, ProcessStartInfo curProgram)
        {
            ProcessStartInfo program = curProgram;
            if (program == null)
            {
                program = new ProcessStartInfo();
            }
            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.EndElement) break;
                if (reader.NodeType != XmlNodeType.Element)
                {
                    throw new XmlException("Malformed XML found in configuration file!");
                }

                switch (reader.Name)
                {
                    case "program":
                        program.FileName = reader.ReadElementContentAsString();
                        break;
                    case "arguments":
                        program.Arguments = reader.ReadElementContentAsString();
                        break;
                    case "workdir":
                        program.WorkingDirectory = parseSpecialTokens(reader.ReadElementContentAsString());
                        break;
                }
            }

            return program;
        }

        private string parseSpecialTokens(string input)
        {
            string result = input;

            result = result.Replace("%SYSTEM%", Environment.SystemDirectory);
            result = result.Replace("%CURDIR%", Environment.CurrentDirectory);
            result = result.Replace("%WINDOWS%", Environment.GetFolderPath(Environment.SpecialFolder.Windows));
            result = result.Replace("%PROGRAMFILES%", Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));

            return result;
        }

        private void populateNotificationFlags(XmlReader reader)
        {
            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.EndElement) break;
                if (reader.NodeType != XmlNodeType.Element)
                {
                    throw new XmlException("Malformed XML found in configuration file!");
                }

                switch (reader.Name)
                {
                    case "online":
                        this.NotifyFlag[UPSMonStatus.ONLINE] = parseNotifyFlags(reader.ReadElementContentAsString());
                        break;
                    case "onbatt":
                        this.NotifyFlag[UPSMonStatus.ONBATT] = parseNotifyFlags(reader.ReadElementContentAsString());
                        break;
                    case "lowbatt":
                        this.NotifyFlag[UPSMonStatus.LOWBATT] = parseNotifyFlags(reader.ReadElementContentAsString());
                        break;
                    case "fsd":
                        this.NotifyFlag[UPSMonStatus.FSD] = parseNotifyFlags(reader.ReadElementContentAsString());
                        break;
                    case "commok":
                        this.NotifyFlag[UPSMonStatus.COMMOK] = parseNotifyFlags(reader.ReadElementContentAsString());
                        break;
                    case "commbad":
                        this.NotifyFlag[UPSMonStatus.COMMBAD] = parseNotifyFlags(reader.ReadElementContentAsString());
                        break;
                    case "shutdown":
                        this.NotifyFlag[UPSMonStatus.SHUTDOWN] = parseNotifyFlags(reader.ReadElementContentAsString());
                        break;
                    case "replacebatt":
                        this.NotifyFlag[UPSMonStatus.REPLACEBATT] = parseNotifyFlags(reader.ReadElementContentAsString());
                        break;
                    case "nocomm":
                        this.NotifyFlag[UPSMonStatus.NOCOMMS] = parseNotifyFlags(reader.ReadElementContentAsString());
                        break;
                }
            }
        }

        private UPSMonNotifyFlag parseNotifyFlags(string flags)
        {
            UPSMonNotifyFlag result = UPSMonNotifyFlag.IGNORE;
            UPSMonNotifyFlag temp;
            string[] splitFlags = flags.Split(new char[] {'+'}, StringSplitOptions.RemoveEmptyEntries);
            foreach (string flagStr in splitFlags)
            {
                if( Enum.TryParse<UPSMonNotifyFlag>(flagStr, out temp) == false)
                {
                    continue;
                }

                result |= temp;
            }

            return result;
        }

        private void populateNotificationMessages(XmlReader reader)
        {
            while (!reader.EOF)
            {
                if (reader.NodeType == XmlNodeType.EndElement) break;
                if (reader.NodeType != XmlNodeType.Element)
                {
                    throw new XmlException("Malformed XML found in configuration file!");
                }

                switch (reader.Name)
                {
                    case "online":
                        this.NotifyMessage[UPSMonStatus.ONLINE] = reader.ReadElementContentAsString();
                        break;
                    case "onbatt":
                        this.NotifyMessage[UPSMonStatus.ONBATT] = reader.ReadElementContentAsString();
                        break;
                    case "lowbatt":
                        this.NotifyMessage[UPSMonStatus.LOWBATT] = reader.ReadElementContentAsString();
                        break;
                    case "fsd":
                        this.NotifyMessage[UPSMonStatus.FSD] = reader.ReadElementContentAsString();
                        break;
                    case "commok":
                        this.NotifyMessage[UPSMonStatus.COMMOK] = reader.ReadElementContentAsString();
                        break;
                    case "commbad":
                        this.NotifyMessage[UPSMonStatus.COMMBAD] = reader.ReadElementContentAsString();
                        break;
                    case "shutdown":
                        this.NotifyMessage[UPSMonStatus.SHUTDOWN] = reader.ReadElementContentAsString();
                        break;
                    case "replacebatt":
                        this.NotifyMessage[UPSMonStatus.REPLACEBATT] = reader.ReadElementContentAsString();
                        break;
                    case "nocomm":
                        this.NotifyMessage[UPSMonStatus.NOCOMMS] = reader.ReadElementContentAsString();
                        break;
                }
            }
        }
    }

}
