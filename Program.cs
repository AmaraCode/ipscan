using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;



namespace ipscan
{
    class Program
    {
        static CountdownEvent countdown;
        static int upCount = 0;
        static object lockObj = new object();

        static List<string> IPList = new List<string>();
        static List<string> UpIp = new List<string>();


        //ok it isn't fancy, but it is fun



        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {

            //setup things
            countdown = new CountdownEvent(1);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            string ipBase = "";
            string hostName = Dns.GetHostName(); // Retrive the Name of HOST  

            Console.WriteLine($"Local Computer Name: {hostName}");

            //I like to clear the console but will bomb in debug
            //Console.Clear();

            //loop all the ip addresses found
            foreach (IPAddress ipAddress in Dns.GetHostEntry(hostName).AddressList)
            {
                //doing this will make all ip6 addresses into ip4 addresses
                IPAddress add = ipAddress.MapToIPv4();

                if (add.ToString().StartsWith("10.") || add.ToString().StartsWith("192."))
                {

                    //add our string to the list
                    IPList.Add($"({ipAddress.AddressFamily}) IP Address is :" + add);

                    //get the base portion of the IP Address
                    ipBase = GetIPBase(add.ToString());

                    //now loop each address and do the pings
                    for (int i = 0; i < 255; i++)
                    {
                        string ip = ipBase + i.ToString();
                        Ping p = new Ping();
                        p.PingCompleted += new PingCompletedEventHandler(p_PingCompleted);
                        countdown.AddCount();
                        p.SendAsync(ip, 100, ip);

                    }

                }
                else
                {
                    IPList.Add($"({ipAddress.AddressFamily}) IP Address Not Pinged: {ipAddress.ToString()}");
                }


            }


            //clean-up and wait
            countdown.Signal();
            countdown.Wait();
            sw.Stop();


            //now display what are in the two static lists
            Console.WriteLine("IP Addresses found...");
            foreach (string line in IPList)
            {
                Console.WriteLine(line);
            }
            Console.WriteLine("");
            Console.WriteLine("Ping started...");
            foreach (string line in UpIp)
            {
                Console.WriteLine(line);
            }



            //finally write the time results
            TimeSpan span = new TimeSpan(sw.ElapsedTicks);
            Console.WriteLine("");
            Console.WriteLine($"Took {sw.ElapsedMilliseconds} milliseconds {upCount} hosts active.");

            //PressAnyKey();

        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        static string GetIPBase(string ipAddress)
        {
            int dotCount = 0;
            int charCount = 0;
            string baseIP = "";

            if (!string.IsNullOrEmpty(ipAddress))
            {
                foreach (char character in ipAddress)
                {



                    if (character == '.')
                    {
                        dotCount++;
                    }

                    if (dotCount == 3)
                    {
                        baseIP = ipAddress.Substring(0, charCount) + ".";
                        return baseIP;
                    }

                    //increment our counter
                    charCount++;
                }


            }
            else
            {
                return "";
            }

            return "";

        }


        /// <summary>
        /// 
        /// </summary>
        static void PressAnyKey()
        {
            Console.WriteLine("Press any key to continue...");
            Console.ReadLine();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void p_PingCompleted(object sender, PingCompletedEventArgs e)
        {
            string ip = (string)e.UserState;
            if (e.Reply != null && e.Reply.Status == IPStatus.Success)
            {
                string name;
                try
                {
                    IPHostEntry hostEntry = Dns.GetHostEntry(ip);
                    name = hostEntry.HostName;
                    UpIp.Add($"{ip} ({name}) is up: ({e.Reply.RoundtripTime} ms)");
                }
                catch (SocketException ex)
                {
                    name = "?";
                    UpIp.Add($"ERROR: {ip} ({ex.Message}) is up: ({e.Reply.RoundtripTime} ms)");


                }

                lock (lockObj)
                {
                    upCount++;
                }
            }
            else if (e.Reply == null)
            {
                //Console.WriteLine($"Pinging {ip} failed.");
                IPList.Add($"Pinging {ip} failed.");
            }
            countdown.Signal();

        }
    }
}
