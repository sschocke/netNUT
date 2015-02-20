using System;

namespace ScorpioTech.netNUT.upsmon.Shared
{
    public abstract class UPSMonPlugin : IUPSMonPlugin
    {
        public string Name
        {
            get;
            protected set;
        }

        public string Author
        {
            get;
            protected set;
        }
        public string Description
        {
            get;
            protected set;
        }
        public Version Version
        {
            get;
            protected set;
        }

        public IUPSMonPluginHost Host
        {
            get;
            private set;
        }

        public virtual void Initialize(IUPSMonPluginHost host)
        {
            this.Host = host;
        }
    }
}
