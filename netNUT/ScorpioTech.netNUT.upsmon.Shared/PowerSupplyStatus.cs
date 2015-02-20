using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScorpioTech.netNUT.upsmon.Shared
{
    public class PowerSupplyStatus : List<MonitoredUPS>
    {

        internal void Initialize()
        {
            Parallel.ForEach<MonitoredUPS>(this, monups =>
            {
                monups.Connect();
                if (monups.Connected == true)
                {
                    monups.UpdateStatus();
                }
            });

            foreach (MonitoredUPS monups in this)
            {
                UPSMonThreads.Debug("UPS " + monups.Device.ToString() + "(" + monups.Device.Description + ") Status:" + monups.Status);
            }
        }

        internal void End()
        {
            Parallel.ForEach<MonitoredUPS>(this, monups =>
            {
                monups.Disconnect();
            });
        }

        internal void Update()
        {
            Parallel.ForEach<MonitoredUPS>(this, monups =>
            {
                if (monups.Connected == false)
                {
                    monups.Connect();
                }
                if (monups.Connected == true)
                {
                    TimeSpan pollInterval = (monups.Alert == true ? UPSMonThreads.Settings.PollFrequencyAlert : UPSMonThreads.Settings.PollFrequency);
                    if( monups.LastUpdate + pollInterval < DateTime.Now)
                    {
                        //UPSMonThreads.Debug("Polling status of " + monups.Device.ToString());
                        UPSMonStatus curStatus = monups.Status;
                        monups.UpdateStatus();
                        if( monups.Status != curStatus)
                        {
                            UPSMonThreads.AppendLog(monups.Device.ToString() + " : New Status=" + monups.Status);
                        }
                    }
                }
            });
        }

        public int HealthyCount
        {
            get
            {
                int healthy = 0;
                foreach (MonitoredUPS monups in this)
                {
                    if( monups.GoneCritical == false)
                    {
                        healthy += monups.PowerValue;
                    }
                }

                return healthy;
            }
        }

        public bool GoneCritical
        {
            get
            {
                return (this.HealthyCount < UPSMonThreads.Settings.MinSupplies);
            }
        }
    }
}
