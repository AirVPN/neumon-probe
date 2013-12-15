using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace NeuMonProbe
{    
    public class Utils
    {
        public static Random RandomSeed = new Random();

        public static string GetFullPath(string filename)
        {
            return AppDomain.CurrentDomain.BaseDirectory + filename;
        }

        public static string ReadLine(ref string data)
        {
            string line = "";
            int posEnd = data.IndexOf("\n");
            if (posEnd == -1)
            {
                line = data;
                data = "";
            }
            else
            {
                line = data.Substring(0, posEnd);
                data = data.Substring(posEnd + 1);
            }
            return line.Trim();
        }

		public static string ReadFirstLine(string data)
		{
			string line = "";
			int posEnd = data.IndexOf("\n");
			if (posEnd == -1)
			{
				line = data;				
			}
			else
			{
				line = data.Substring(0, posEnd);
				
			}
			return line.Trim();
		}

        public static bool IsIP(string v)
        {
            Match match = Regex.Match(v, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}");
            return match.Success;
        }

        public static void Resolve(string host, string dnsServer, ref List<string> results)
        {            
            if ((dnsServer != "") && (Platform.IsUnix() == false))
                throw new Exception("DNS resolve supported only under linux.");

            try
            {

                if( (Platform.IsUnix()) && (dnsServer != "") )
                {
                    string digCmd = "dig @" + dnsServer + " " + host + " +short +tcp";
                    string digOut = Utils.Shell(digCmd);
                    
                    if (digOut.IndexOf("connection timed out") != -1)
                    {
                    }
                    else if (digOut.IndexOf(" ") != -1)
                    {
                        throw new Exception("Unexpected dig output: " + digOut + ", cmd: " + digCmd);
                    }
                    else
                    {
                        //Console.WriteLine("Cmd: " + cmd + ", Out: " + digOut);
                        string[] rows = digOut.Split('\n');
                        foreach (string row in rows)
                        {
                            string v = row.Trim();

                            if (IsIP(v))
                            {
                                if (results.Contains(v) == false)
                                    results.Add(v);
                            }
                            else if (v != "")
                            {
                                Resolve(v, dnsServer, ref results);
                            }
                        }
                    }
                }
                else if (dnsServer == "") // .Net don't support queries to specific server. Maybe implemented with nslookup.
                {
                    IPHostEntry hostEntry;
                    try
                    {
                        hostEntry = Dns.GetHostEntry(host);
                    }
                    catch (Exception e)
                    {
                        hostEntry = null;
                    }

                    if (hostEntry != null)
                    {
                        foreach (IPAddress ip in hostEntry.AddressList)
                        {
                            string v = ip.ToString();
                            if (results.Contains(v) == false)
                                results.Add(v);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Engine.LogFatal("Error:" + e.Message);
            }
        }

        public static string ResolveOne(string host, string dnsServer, string ip)
        {
            List<string> results = new List<string>();            
            Resolve(host, dnsServer, ref results);

            if (results.Count == 0)
                return "";

            foreach (string r in results)
            {
                if (r == ip)
                    return r;
            }

            return results[0];
        }

        static public String Shell(string cmdLine)
        {
            if (Platform.IsUnix())
                return Shell("/bin/sh", String.Format("-c '{0}'", cmdLine), "", true, false);
            else if (Platform.IsWindows())
                return Shell("cmd.exe", String.Format("/c {0}", cmdLine), "", true, false);
            else
                Platform.NotImplemented();
            
            return "";
        }

        static public String Shell(string FileName, string Arguments, string WorkingDirectory, bool WaitEnd, bool ShowWindow)
        {
            try
            {
                // #start a new process
                Process p = new Process();

                //#my command arguments, i.e. what site to ping
                p.StartInfo.Arguments = Arguments;

                if (WorkingDirectory != "")
                    p.StartInfo.WorkingDirectory = WorkingDirectory;

                //#the command to invoke under MSDOS
                p.StartInfo.FileName = FileName;

                if (ShowWindow == false)
                {
                    //#do not show DOS window
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                }

                if (WaitEnd)
                {
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardOutput = true;
                }

                p.Start();

                if (WaitEnd)
                {
                    string Output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    return Output;
                }
                else
                {
                    return "";
                }
            }
            catch (Exception E)
            {
                Engine.LogFatal("Shell exception:" + E.ToString());
                return E.Message;
            }


        }

    }
}
