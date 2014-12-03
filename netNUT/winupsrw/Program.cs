using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ScorpioTech.Framework.netNUTClient;
using System.Net.Sockets;

namespace ScorpioTech.netNUT.winupsrw
{
    class Program
    {
        private static string authUser = null;
        private static string authPass = null;
        private static string variable = null;
        private static string newValue = null;
        private static string ups = null;

        static void Main(string[] args)
        {
            if( (args.Length == 0) || (args[0] == "-h") || (args[0] == "-?") || (args[0] == "--help"))
            {
                HelpText();
                return;
            }

            if (AnalyzeCommandLineParams(args) == false)
            {
                return;
            }

            UPS target = new UPS(ups);
            UPSDClient client = new UPSDClient(target.Host);
            try
            {
                client.Connect();
                if( String.IsNullOrEmpty(authUser) == false)
                {
                    if( client.SetUsername(authUser) == false )
                    {
                        Console.WriteLine("Error: Could not set username for authentication");
                        return;
                    }
                }
                if (String.IsNullOrEmpty(authPass) == false)
                {
                    if (client.SetPassword(authPass) == false)
                    {
                        Console.WriteLine("Error: Could not set password for authentication");
                        return;
                    }
                }
                if (String.IsNullOrEmpty(variable))
                {
                    ListVariables(target, client);
                    return;
                }

                SetVariable(target, client);
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

        private static bool AnalyzeCommandLineParams(string[] args)
        {
            for (int idx = 0; idx < args.Length; )
            {
                switch (args[idx])
                {
                    case "-u":
                        if (idx + 1 >= args.Length)
                        {
                            HelpText();
                            return false;
                        }
                        authUser = args[idx + 1];
                        idx += 2;
                        break;
                    case "-p":
                        if (idx + 1 >= args.Length)
                        {
                            HelpText();
                            return false;
                        }
                        authPass = args[idx + 1];
                        idx += 2;
                        break;
                    case "-s":
                        if (idx + 1 >= args.Length)
                        {
                            HelpText();
                            return false;
                        }
                        variable = args[idx + 1];
                        idx += 2;
                        break;
                    default:
                        ups = args[idx];
                        idx++;
                        break;
                }
            }
            if (String.IsNullOrEmpty(ups))
            {
                HelpText();
                return false;
            }

            return true;
        }

        private static void SetVariable(UPS target, UPSDClient client)
        {
            string[] variableParts = variable.Split(new char[] { '=' }, 2);
            variable = variableParts[0];
            if (variableParts.Length < 2)
            {
                Console.Write("Enter value for " + variable + ":");
                newValue = Console.ReadLine();
            }
            if (client.SetUPSVariable(target.Name, variable, newValue) == false)
            {
                Console.WriteLine("UNKNOWN ERROR");
                return;
            }
            Console.WriteLine("OK");
        }

        private static void ListVariables(UPS target, UPSDClient client)
        {
            List<UPS.VariableDescription> vars = client.ListUPSReadWrite(target.Name);
            foreach (UPS.VariableDescription rwVar in vars)
            {
                Console.WriteLine("[" + rwVar.Name + "]");
                Console.WriteLine(rwVar.Description);
                Console.WriteLine("Type: " + rwVar.Type);
                Console.WriteLine("Value: " + rwVar.Value);
                Console.WriteLine();
            }
        }

        private static void HelpText()
        {
            #region Help Text
            Console.WriteLine("Windows Network UPS Tools winupsrw " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
            Console.WriteLine();
            Console.WriteLine("Usage : winupsrw [-h]");
            Console.WriteLine("        winupsrw [-s <variable>] [-u <username>] [-p <password>] <ups>");
            Console.WriteLine();
            Console.WriteLine("Demo program to set variables within UPS hardware.");
            Console.WriteLine("  -h            - display this help text");
            Console.WriteLine("  -s <variable> - specify the variable to be changed");
            Console.WriteLine("                  use -s VAR=VALUE to avoid prompting for value");
            Console.WriteLine("  -u <username> - set username for command authentication");
            Console.WriteLine("  -p <password> - set password for command authentication");
            Console.WriteLine();
            Console.WriteLine("  <ups>         - upsd server, <upsname>[@<hostname>[:<port>]] form");
            Console.WriteLine();
            Console.WriteLine("Call without -s to show all possible read/write variables.");
            #endregion
        }
    }
}
