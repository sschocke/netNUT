using ScorpioTech.Framework.netNUTClient;
using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace ScorpioTech.netNUT.winupsc
{
    class Program
    {
        static void Main(string[] args)
        {
            if( args.Length == 0)
            {
                Console.WriteLine("Error: invalid UPS definition.");
                Console.WriteLine("Required format: upsname[@hostname[:port]]");
                Console.WriteLine("or type '{0} -h' for help", System.Diagnostics.Process.GetCurrentProcess().ProcessName);
                return;
            }
            if ((args[0] == "-h") || (args[0] == "-?") || (args[0] == "--help"))
            {
                #region Help Text
                Console.WriteLine("Windows Network UPS Tools winupsc " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
                Console.WriteLine();
                Console.WriteLine("Usage : winupsc -l | -L [<hostname>[:port]]");
                Console.WriteLine("        winupsc <ups> [<variable>]");
                Console.WriteLine("        winupsc -c <ups>");
                Console.WriteLine();
                Console.WriteLine("First form (lists UPSes):");
                Console.WriteLine("  -l           - lists each UPS on <hostname>, one per line.");
                Console.WriteLine("  -L           - lists each UPS followed by its description (from ups.conf).");
                Console.WriteLine("                 Default hostname: localhost");
                Console.WriteLine();
                Console.WriteLine("Second form (lists variables and values):");
                Console.WriteLine("  <ups>        - upsd server, <upsname>[@<hostname>[:<port>]] form");
                Console.WriteLine("  <variable>   - optional, display this variable only.");
                Console.WriteLine("                 Default: list all variables for <ups>");
                Console.WriteLine();
                Console.WriteLine("Third form (lists clients connected to a device):");
                Console.WriteLine("  -c           - lists each client connected on <ups>, one per line.");
                Console.WriteLine("  <ups>        - upsd server, <upsname>[@<hostname>[:<port>]] form");
                #endregion
                return;
            }
            if ((args[0] == "-l") || (args[0] == "-L"))
            {
                string upsdServer = "localhost";
                if( args.Length > 1)
                {
                    upsdServer = args[1];
                }

                PrintUPSList(upsdServer, (args[0] == "-L"));
                return;
            }
            UPS ups = null;
            if (args[0] == "-c")
            {
                if (args.Length < 2)
                {
                    Console.WriteLine("Error: invalid UPS definition.");
                    Console.WriteLine("Required format: upsname[@hostname[:port]]");
                    Console.WriteLine("or type '{0} -h' for help", System.Diagnostics.Process.GetCurrentProcess().ProcessName);
                    return;
                }

                ups = new UPS(args[1]);
                PrintUPSClients(ups);

                return;
            }

            ups = new UPS(args[0]);
            if( args.Length == 2)
            {
                string varName = args[1];
                PrintUPSVar(ups, varName);
            }
            else
            {
                PrintUPSVarList(ups);
            }
        }

        private static void PrintUPSClients(UPS ups)
        {
            UPSDClient client = new UPSDClient(ups.Host);
            try
            {
                client.Connect();
                List<string> clients = client.ListUPSClient(ups.Name);
                foreach (string upsClient in clients)
                {
                    Console.WriteLine(upsClient);
                }
            }
            catch(SocketException sockex)
            {
                Console.WriteLine("Error: " + sockex.Message);
            }
            catch (UPSException upsex)
            {
                Console.WriteLine("Error: " + upsex.Description);
            }
            finally
            {
                client.Disconnect();
            }
        }
        private static void PrintUPSVar(UPS ups, string varName)
        {
            UPSDClient client = new UPSDClient(ups.Host);
            try
            {
                client.Connect();
                string result = client.GetUPSVar(ups.Name, varName);
                Console.WriteLine(result);
            }
            catch (SocketException sockex)
            {
                Console.WriteLine("Error: " + sockex.Message);
            }
            catch (UPSException upsex)
            {
                Console.WriteLine("Error: " + upsex.Description);
            }
            finally
            {
                client.Disconnect();
            }
        }
        private static void PrintUPSVarList(UPS ups)
        {
            UPSDClient client = new UPSDClient(ups.Host);
            try
            {
                client.Connect();
                Dictionary<string, string> vars = client.ListUPSVar(ups.Name);
                foreach (KeyValuePair<string, string> item in vars)
                {
                    Console.WriteLine(item.Key + ": " + item.Value);
                }
            }
            catch (SocketException sockex)
            {
                Console.WriteLine("Error: " + sockex.Message);
            }
            catch (UPSException upsex)
            {
                Console.WriteLine("Error: " + upsex.Description);
            }
            finally
            {
                client.Disconnect();
            }
        }
        private static void PrintUPSList(string upsdServer, bool displayDesc)
        {
            UPSDClient client = new UPSDClient(upsdServer);
            try
            {
                client.Connect();
                List<UPS> upsList = client.ListUPS();
                foreach (UPS ups in upsList)
                {
                    Console.Write(ups.Name);
                    if (displayDesc)
                    {
                        Console.Write(": " + ups.Description);
                    }
                    Console.WriteLine();
                }
            }
            catch (SocketException sockex)
            {
                Console.WriteLine("Error: " + sockex.Message);
            }
            catch (UPSException upsex)
            {
                Console.WriteLine("Error: " + upsex.Description);
            }
            finally
            {
                client.Disconnect();
            }
        }
    }
}
