using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace NeuMonProbe
{
    class Program
    {                
        static void Main(string[] args)
        {

			/*
			string host = "sex.com";
			string dns = "202.134.1.10";
			string ip = "206.125.164.82";
			String resolvedIP = Utils.ResolveOne(host,dns,ip);

			Console.WriteLine("res:" + resolvedIP);

			return;
			*/

            Options.Read();

            if (args.Length != 0)
                Options.ReadCommandLine(args);
                        
            for (; ; )
            {
                try
                {
                    


                    Engine.Run();
                }
                catch (Exception e)
                {
                    Engine.Log(e.Message);
                    Engine.Log("Sleep 10 minutes.");
                    Thread.Sleep(60*10*1000);
                }

                if (Engine.Exit)
                    break;
            }
        }

        
    }
}
