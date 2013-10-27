using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Web;

namespace NeuMonProbe
{
    public class Job
    {
        public Dictionary<string, string> Params = new Dictionary<string, string>();

        public Thread T;

        public void Run()
        {   
            int sleepMax = Options.GetInt("sleepmax",0);
            if(sleepMax != 0)
                Thread.Sleep(Utils.RandomSeed.Next(0, sleepMax));

            if (Params["type"] == "request")
            {
                Uri url = new Uri(Params["url"]);

                

                Params["cnt_n"] = "";
                Params["cnt_c"] = "";
                Params["resolved_ip"] = "";

                String resolvedIP = Utils.ResolveOne(url.Host, Params["dns"], Params["ip"]);
                
                Params["resolved_ip"] = resolvedIP;

                if (Params["mode"] == "full")
                {
                    if ((resolvedIP != "") && (resolvedIP != "127.0.0.1"))
                    {                        
                        Params["cnt_n"] = Engine.GetSpecialBody(Engine.GetContent(Params["url"]));
                        //Console.Write("cnt_n:" + Params["cnt_n"]);
                    }

                    if (Params["class"] == "reference")
                    {
                        Params["cnt_c"] = Params["cnt_n"];
                        //Console.Write("cnt_c:" + Params["cnt_c"]);
                    }
                    else if (Params["ip"] != "")
                    {
                        Params["cnt_c"] = Engine.GetSpecialBody(Engine.GetContent(Params["url"], Params["ip"]));                        
                    }
                }
                
            }

            lock (Engine.LockJobs)
            {
                Engine.JobsRunning.Remove(this);
                Engine.JobsCompleted.Add(this);
            }
        }

        public string GetResultReport()
        {
            string row = "";

            row += Params["mode"] + ";";
            row += Params["url"] + ";";
            row += Params["resolved_ip"] + ";";

            if (Params["mode"] == "full")
            {
                row += Params["cnt_n"] + ";";
                row += Params["cnt_c"] + ";";
            }

            return row;
        }
    }
}
