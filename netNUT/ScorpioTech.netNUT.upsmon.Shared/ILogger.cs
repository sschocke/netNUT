namespace ScorpioTech.netNUT.upsmon.Shared
{
    public interface ILogger
    {
        void AppendLog(string line);
        void ErrorLog(string line);
        void DebugLog(string line);
        void TraceLog(string line);
    }
}
