using ScorpioTech.Framework.netNUTClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace ScorpioTech.netNUT.winupscmd
{
    class Program
    {
        private static string authUser = null;
        private static string authPass = null;
        private static string command = null;
        private static string addParam = null;
        private static string ups = null;
        private static bool listMode = false;

        static void Main(string[] args)
        {
            if ((args.Length == 0) || (args[0] == "-h") || (args[0] == "-?") || (args[0] == "--help"))
            {
                HelpText();
                return;
            }

            if (AnalyzeCommandLineParams(args) == false)
            {
                return;
            }

            Console.WriteLine("ups={0}, command={1}, user={2}, password={3}, addParam={4}, listMode={5}",
                new object[] { ups, command, authUser, authPass, addParam, listMode });

            UPS target = new UPS(ups);
            UPSDClient client = new UPSDClient(target.Host);
            try
            {
                client.Connect();
                if( listMode)
                {
                    ListCommands(target, client);
                    return;
                }

                if (String.IsNullOrEmpty(authUser) == true)
                {
                    Console.Write("Username: ");
                    authUser = Console.ReadLine();
                }
                if (client.SetUsername(authUser) == false)
                {
                    Console.WriteLine("Error: Could not set username for authentication");
                    return;
                }

                if (String.IsNullOrEmpty(authPass) == true)
                {
                    Console.Write("Password: ");
                    authPass = Console.ReadLine();
                }
                if (client.SetPassword(authPass) == false)
                {
                    Console.WriteLine("Error: Could not set password for authentication");
                    return;
                }

                ExecuteCommand(target, client);
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

        private static void ExecuteCommand(UPS target, UPSDClient client)
        {
            if( client.InstantCommand(target.Name, command, addParam) == false)
            {
                Console.WriteLine("UNKNOWN ERROR");
                return;
            }
            Console.WriteLine("OK");
        }

        private static void ListCommands(UPS target, UPSDClient client)
        {
            Dictionary<string, string> commands = client.ListUPSCommands(target.Name);
            Console.WriteLine("Instant commands supported on UPS [{0}]:", target.Name);
            Console.WriteLine();
            foreach (KeyValuePair<string, string> item in commands)
            {
                Console.WriteLine(item.Key + " - " + item.Value);
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
                    case "-l":
                        if (idx + 1 >= args.Length)
                        {
                            HelpText();
                            return false;
                        }
                        listMode = true;
                        ups = args[idx + 1];
                        idx += 2;
                        break;
                    default:
                        if (idx + 1 >= args.Length)
                        {
                            HelpText();
                            return false;
                        }
                        ups = args[idx];
                        command = args[idx + 1];
                        idx += 2;
                        if (idx < args.Length)
                        {
                            addParam = args[idx];
                            idx++;
                        }
                        break;
                }
            }
            if (String.IsNullOrEmpty(ups))
            {
                HelpText();
                return false;
            }
            if ((listMode == false) && (String.IsNullOrEmpty(command) == true))
            {
                HelpText();
                return false;
            }

            return true;
        }

        private static void HelpText()
        {
            #region Help Text
            Console.WriteLine("Windows Network UPS Tools winupscmd " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
            Console.WriteLine();
            Console.WriteLine("Usage : winupscmd [-h]");
            Console.WriteLine("        winupscmd [-l <ups>]");
            Console.WriteLine("        winupscmd [-u <username>] [-p <password>] <ups> <command> [<value>]");
            Console.WriteLine();
            Console.WriteLine("Administration program to initiate instant commands on UPS hardware.");
            Console.WriteLine();
            Console.WriteLine("  -h            - display this help text");
            Console.WriteLine("  -l <ups>      - show available commands on UPS <ups>");
            Console.WriteLine("  -u <username> - set username for command authentication");
            Console.WriteLine("  -p <password> - set password for command authentication");
            Console.WriteLine();
            Console.WriteLine("  <ups>         - upsd server, <upsname>[@<hostname>[:<port>]] form");
            Console.WriteLine("  <command>     - Valid instant command - test.panel.start, etc.");
            Console.WriteLine("  [<value>]     - Additional data for command - number of seconds, etc.");
            #endregion
        }
    }
}
