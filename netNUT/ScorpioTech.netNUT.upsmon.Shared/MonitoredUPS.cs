using ScorpioTech.Framework.netNUTClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Xml;

namespace ScorpioTech.netNUT.upsmon.Shared
{
    public class MonitoredUPS
    {
        public enum MonMode
        {
            MASTER,
            SLAVE
        }

        public string Host { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
        public int PowerValue { get; private set; }
        public MonMode Mode { get; private set; }

        public UPS Device { get; private set; }
        public UPSMonStatus Status { get; private set; }
        public DateTime LastUpdate { get; private set; }

        public bool OnBattery
        {
            get
            {
                return this.Status.HasFlag(UPSMonStatus.ONBATT);
            }
        }
        public bool GoneCritical
        {
            get
            {
                // If this UPS is reporting either a Low Battery, Power Off or Forced Shutdown then we
                // can assume that power will not be available much longer, or already gone
                if (this.Status.HasFlag(UPSMonStatus.LOWBATT) || this.Status.HasFlag(UPSMonStatus.OFF)
                    || this.Status.HasFlag(UPSMonStatus.FSD))
                {
                    return true;
                }

                return false;
            }
        }
        public bool Alert
        {
            get
            {
                return (!this.Status.HasFlag(UPSMonStatus.ONLINE));
            }
        }

        private UPSDClient upsd;

        protected MonitoredUPS()
        {
            this.Mode = MonMode.SLAVE;
            this.Status = UPSMonStatus.NOCOMMS;
            this.PowerValue = 1;
            this.LastUpdate = DateTime.MinValue;
        }

        public MonitoredUPS(XmlReader reader)
            : this()
        {
            if (reader.NodeType == XmlNodeType.EndElement) return;
            if (reader.NodeType != XmlNodeType.Element || reader.Name != "monitor")
            {
                throw new XmlException("Malformed XML found in configuration file!");
            }
            string host = reader.GetAttribute("host");
            string user = reader.GetAttribute("username");
            string pass = reader.GetAttribute("password");
            string pwrval = reader.GetAttribute("powervalue");
            string mode = reader.GetAttribute("mode");
            if (host == null || user == null || pass == null)
            {
                throw new XmlException("Malformed XML found in configuration file! Invalid <monitor> declaration!");
            }
            this.Host = host;
            this.Username = user;
            this.Password = pass;

            if (mode != null)
            {
                MonMode temp;
                if (Enum.TryParse<MonMode>(mode.ToUpper(), out temp) == false)
                {
                    throw new XmlException("Malformed XML found in configuration file! Invalid <monitor> mode attribute!");
                }

                this.Mode = temp;
            }
            if( pwrval != null )
            {
                int valtemp;
                if( Int32.TryParse(pwrval, out valtemp) == false)
                {
                    throw new XmlException("Malformed XML found in configuration file! Invalid <monitor> powervalue attribute!");
                }

                this.PowerValue = valtemp;
            }

            this.Device = new UPS(this.Host);

            this.upsd = new UPSDClient(this.Device.Host);

            reader.ReadStartElement("monitor");
        }

        ~MonitoredUPS()
        {
            if (this.upsd != null) this.upsd.Disconnect();
        }

        public override string ToString()
        {
            return this.Device.ToString() + " - " + this.Status.ToString();
        }

        internal void Connect()
        {
            try
            {
                upsd.Connect();
                this.Status &= ~UPSMonStatus.COMMBAD;
                this.Status |= UPSMonStatus.COMMOK;
                this.Device.Description = upsd.GetUPSDescription(this.Device.Name);
                upsd.SetUsername(this.Username);
                upsd.SetPassword(this.Password);
                upsd.Login(this.Device.Name);
            }
            catch (SocketException sockex)
            {
                UPSMonThreads.AppendLog("Could not connect to UPS " + this.Device.ToString() + "! " + sockex.Message);
                if( this.Status != UPSMonStatus.NOCOMMS)
                {
                    this.Status &= ~UPSMonStatus.COMMOK;
                    this.Status |= UPSMonStatus.COMMBAD;
                }
                upsd.Disconnect();
            }
            catch (UPSException upsex)
            {
                UPSMonThreads.AppendLog("Error on UPS " + this.Device.ToString() + "! " + upsex.Code.ToString() + ":" + upsex.Description
                    + (String.IsNullOrEmpty(upsex.Message) ? "" : " (" + upsex.Message + ")"));
                if (this.Status != UPSMonStatus.NOCOMMS)
                {
                    this.Status &= ~UPSMonStatus.COMMOK;
                    this.Status |= UPSMonStatus.COMMBAD;
                }
            }
        }
        internal void Disconnect()
        {
            if (this.upsd != null)
            {
                this.upsd.Logout();
                this.upsd.Disconnect();
            }
        }
        public bool Connected
        {
            get
            {
                if (this.upsd == null) return false;

                return this.upsd.Connected;
            }
        }

        public void UpdateStatus()
        {
            try
            {
                if( this.Connected == false)
                {
                    this.Status = UPSMonStatus.NOCOMMS;
                    return;
                }

                string upsdStatus = upsd.GetUPSVar(this.Device.Name, "ups.status");
                this.Status &= ~UPSMonStatus.ALLUPSD;

                string[] parts = upsdStatus.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string status in parts)
                {
                    switch (status.ToUpper())
                    {
                        case "OL":
                            this.Status |= UPSMonStatus.ONLINE;
                            break;
                        case "OB":
                            this.Status |= UPSMonStatus.ONBATT;
                            break;
                        case "LB":
                            this.Status |= UPSMonStatus.LOWBATT;
                            break;
                        case "HB":
                            this.Status |= UPSMonStatus.HIGHBATT;
                            break;
                        case "RB":
                            this.Status |= UPSMonStatus.REPLACEBATT;
                            break;
                        case "CHRG":
                            this.Status |= UPSMonStatus.CHARGING;
                            break;
                        case "DISCHRG":
                            this.Status |= UPSMonStatus.DISCHARGING;
                            break;
                        case "BYPASS":
                            this.Status |= UPSMonStatus.BYPASS;
                            break;
                        case "CAL":
                            this.Status |= UPSMonStatus.CALIBRATION;
                            break;
                        case "OFF":
                            this.Status |= UPSMonStatus.OFF;
                            break;
                        case "OVER":
                            this.Status |= UPSMonStatus.OVERLOADED;
                            break;
                        case "TRIM":
                            this.Status |= UPSMonStatus.TRIM;
                            break;
                        case "BOOST":
                            this.Status |= UPSMonStatus.BOOST;
                            break;
                        case "FSD":
                            this.Status |= UPSMonStatus.FSD;
                            break;
                    }
                }

                this.LastUpdate = DateTime.Now;
            }
            catch (SocketException sockex)
            {
                UPSMonThreads.AppendLog("Network error communicating to UPS " + this.Device.ToString() + "! " + sockex.Message);
                if (this.Status != UPSMonStatus.NOCOMMS)
                {
                    this.Status &= ~UPSMonStatus.COMMOK;
                    this.Status |= UPSMonStatus.COMMBAD;
                }
                upsd.Disconnect();
            }
            catch (UPSException upsex)
            {
                UPSMonThreads.AppendLog("Error on UPS " + this.Device.ToString() + "! " + upsex.Message);
                if (this.Status != UPSMonStatus.NOCOMMS)
                {
                    this.Status &= ~UPSMonStatus.COMMOK;
                    this.Status |= UPSMonStatus.COMMBAD;
                }
            }
        }
    }
}
