using SimpleWifi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace WiFiAnalyzer
{
    internal static class Program
    {
        private static Wifi _wifi;

        public static void Main()
        {
            _wifi = new Wifi();

            if (_wifi.NoWifiAvailable)
                Console.WriteLine("\r\n-- NO WIFI CARD WAS FOUND --");

            string command;
            do
            {
                Console.WriteLine("\r\n-- COMMAND LIST --");
                Console.WriteLine("L. List access points");
                Console.WriteLine("C. Connect");
                Console.WriteLine("D. Disconnect");
                Console.WriteLine("S. Status");
                Console.WriteLine("X. Print profile XML");
                Console.WriteLine("R. Remove profile");
                Console.WriteLine("I. Show access point information");
                Console.WriteLine("F. Find number of devices in access point");
                Console.WriteLine("Q. Quit");

                command = Console.ReadLine()?.ToLower();

                Execute(command);
            } while (command != "q");
        }


        private static void Execute(string command)
        {
            switch (command)
            {
                case "l":
                    List();
                    break;
                case "d":
                    _wifi.Disconnect();
                    break;
                case "c":
                    Connect();
                    break;
                case "s":
                    Console.WriteLine("\r\n-- CONNECTION STATUS --");
                    Console.WriteLine(_wifi.ConnectionStatus == WifiStatus.Connected ? "You are connected to a wifi" : "You are not connected to a wifi");
                    break;
                case "x":
                    ProfileXml();
                    break;
                case "r":
                    DeleteProfile();
                    break;
                case "i":
                    ShowInfo();
                    break;
                case "f":
                    Finder();
                    break;
                case "q":
                    break;
                default:
                    Console.WriteLine("\r\nIncorrect command.");
                    break;
            }
        }
        
        
        private static IEnumerable<AccessPoint> List()
        {
            Console.WriteLine("\r\n-- Access point list --");
            IEnumerable<AccessPoint> accessPoints = _wifi.GetAccessPoints().OrderByDescending(ap => ap.SignalStrength);

            int i = 0;
            var enumerable = accessPoints.ToList();
            foreach (var ap in enumerable) Console.WriteLine("{0}. {1} {2}% Connected: {3}", i++, ap.Name, ap.SignalStrength, ap.IsConnected);

            return enumerable;
        }
        
        private static void Connect()
        {
            var accessPoints = List();

            Console.Write("\r\nEnter the index of the network you wish to connect to: ");

            var selectedIndex = int.Parse(Console.ReadLine() ?? throw new InvalidOperationException());
            var enumerable = accessPoints.ToList();
            if (selectedIndex > enumerable.ToArray().Length || enumerable.ToArray().Length == 0)
            {
                Console.Write("\r\nIndex out of bounds");
                return;
            }
            var selectedAp = enumerable.ToList()[selectedIndex];

            // Auth
            var authRequest = new AuthRequest(selectedAp);
            var overwrite = true;

            if (authRequest.IsPasswordRequired)
            {
                if (selectedAp.HasProfile)
                    // If there already is a stored profile for the network, we can either use it or overwrite it with a new password.
                {
                    Console.Write("\r\nA network profile already exist, do you want to use it (y/n)? ");
                    if (Console.ReadLine()?.ToLower() == "y")
                    {
                        overwrite = false;
                    }
                }

                if (overwrite)
                {
                    if (authRequest.IsUsernameRequired)
                    {
                        Console.Write("\r\nPlease enter a username: ");
                        authRequest.Username = Console.ReadLine();
                    }

                    authRequest.Password = PasswordPrompt(selectedAp);

                    if (authRequest.IsDomainSupported)
                    {
                        Console.Write("\r\nPlease enter a domain: ");
                        authRequest.Domain = Console.ReadLine();
                    }
                }
            }

            selectedAp.ConnectAsync(authRequest, overwrite, OnConnectedComplete);
        }
        
        private static string PasswordPrompt(AccessPoint selectedAp)
        {
            string password = string.Empty;

            bool validPassFormat = false;

            while (!validPassFormat)
            {
                Console.Write("\r\nPlease enter the wifi password: ");
                password = Console.ReadLine();

                validPassFormat = selectedAp.IsValidPassword(password);

                if (!validPassFormat)
                    Console.WriteLine("\r\nPassword is not valid for this network type.");
            }

            return password;
        }
        private static void ProfileXml()
        {
            var accessPoints = List();

            Console.Write("\r\nEnter the index of the network you wish to print XML for: ");

            var selectedIndex = int.Parse(Console.ReadLine() ?? throw new InvalidOperationException());
            var enumerable = accessPoints.ToList();
            if (selectedIndex > enumerable.ToArray().Length || enumerable.ToArray().Length == 0)
            {
                Console.Write("\r\nIndex out of bounds");
                return;
            }
            var selectedAp = enumerable.ToList()[selectedIndex];

            Console.WriteLine("\r\n{0}\r\n", selectedAp.GetProfileXML());
        }

        private static void DeleteProfile()
        {
            var accessPoints = List();

            Console.Write("\r\nEnter the index of the network you wish to delete the profile: ");

            var selectedIndex = int.Parse(Console.ReadLine() ?? throw new InvalidOperationException());
            var enumerable = accessPoints.ToList();
            if (selectedIndex > enumerable.ToArray().Length || enumerable.ToArray().Length == 0)
            {
                Console.Write("\r\nIndex out of bounds");
                return;
            }
            var selectedAp = enumerable.ToList()[selectedIndex];

            selectedAp.DeleteProfile();
            Console.WriteLine("\r\nDeleted profile for: {0}\r\n", selectedAp.Name);
        }


        private static void ShowInfo()
        {
            var accessPoints = List();

            Console.Write("\r\nEnter the index of the network you wish to see info about: ");

            var selectedIndex = int.Parse(Console.ReadLine() ?? throw new InvalidOperationException());
            var enumerable = accessPoints.ToList();
            if (selectedIndex > enumerable.ToArray().Length || enumerable.ToArray().Length == 0)
            {
                Console.Write("\r\nIndex out of bounds");
                return;
            }
            var selectedAp = enumerable.ToList()[selectedIndex];

            Console.WriteLine("\r\n{0}\r\n", selectedAp);
        }

        private static void wifi_ConnectionStatusChanged(object sender, WifiStatusEventArgs e)
        {
            Console.WriteLine("\nNew status: {0}", e.NewStatus.ToString());
        }

        private static void OnConnectedComplete(bool success)
        {
            Console.WriteLine("\nOnConnectedComplete, success: {0}", success);
        }

        private static void Finder()
        {
            string subnet;
            if (_wifi.ConnectionStatus == WifiStatus.Connected)
            {
                Console.Write("\r\nYou are already connected to a network, do you want to change (y/n)? ");
                if (Console.ReadLine()?.ToLower() == "y")
                {
                    Connect();
                }
                else
                {
                    Console.WriteLine("Initiating network scan");
                    subnet = GetSubnet(GetLocalIpAddress());
                    Console.WriteLine(subnet);
                    CheckHosts(subnet);
                }
            }
            Connect();
            if (_wifi.ConnectionStatus != WifiStatus.Connected) System.Threading.Thread.Sleep(10000);
            Console.WriteLine("Initiating network scan");
            subnet = GetSubnet(GetLocalIpAddress());
            Console.WriteLine(subnet);
            CheckHosts(subnet);
        }


        private static string GetSubnet(string currentIp) {
            var firstSeparator = currentIp.LastIndexOf('/');
            var lastSeparator = currentIp.LastIndexOf('.');
            return currentIp.Substring(firstSeparator + 1, lastSeparator + 1);
        }


        private static string GetLocalIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        private static int CheckHosts(string subnet)
        {
            var devices = 0;
            
            var p = new Ping();
            
            for (var i = 1; i < 255; i++){
                var host = subnet + i;
                var rep = p.Send(host);
                
                if (rep != null && rep.Status == IPStatus.Success)
                {
                    Console.WriteLine(rep.Address.ToString());
                    devices++;
                }
            }

            return devices;
        }
    }
}

