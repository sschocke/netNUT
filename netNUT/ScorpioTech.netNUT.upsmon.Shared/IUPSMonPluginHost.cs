using ScorpioTech.Framework.PlugIn;

namespace ScorpioTech.netNUT.upsmon.Shared
{
    public interface IUPSMonPluginHost : IPlugInHost
    {
        void DebugLog(IUPSMonPlugin sender, string line);
        void AppendLog(IUPSMonPlugin sender, string line);
    }
}
