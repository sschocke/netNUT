using ScorpioTech.netNUT.upsmon.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ScorpioTech.netNUT.winupsmon.server
{
    class Program : ILogger
    {
        static void Main(string[] args)
        {
            Program logger = new Program();

            Console.WriteLine("Starting upsmon");
            UPSMonThreads threads = new UPSMonThreads(logger);
            threads.Start();
            Console.ReadKey(false);
            threads.Stop();
        }

        public void AppendLog(string line)
        {
            Console.WriteLine(line);
        }

        public void ErrorLog(string line)
        {
            Console.WriteLine(line);
        }

        public void DebugLog(string line)
        {
            Console.WriteLine("DEBUG: " + line);
        }

        public void TraceLog(string line)
        {
            Console.WriteLine("TRACE: " + line);
        }
    }
}
