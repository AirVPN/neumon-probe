using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace NeuMonProbe
{
    public static class Platform
    {
        public static String UName;
        public static String Architecture;

        public static void Ensure()
        {
            if (UName != "")
                return;

            if (IsUnix()) // Linux and OSX
            {
                UName = ShellPlatformIndipendent("sh", "-c 'uname'", "", true, false).Trim();
                Architecture = ShellPlatformIndipendent("sh", "-c 'uname -m'", "", true, false).Trim();
            }
            else if (IsWindows())
            {
                UName = "Windows";
                Architecture = "Unknown.";
            }
        }

        public static void NotImplemented()
        {
            throw new Exception("Not implemented.");
        }

        public static String ShellPlatformIndipendent(string FileName, string Arguments, string WorkingDirectory, bool WaitEnd, bool ShowWindow)
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
                return E.Message;
            }


        }

        static public bool IsUnix()
        {
            return (Environment.OSVersion.Platform.ToString() == "Unix");
        }

        static public bool IsWindows()
        {
            return (Environment.OSVersion.VersionString.IndexOf("Windows") != -1);
        }

        static public bool IsLinux()
        {
            Ensure();

            return UName == "Linux";
        }

        static public bool IsOSX()
        {
            Ensure();

            return UName == "Darwin";
        }
    }
}
