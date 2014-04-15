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
