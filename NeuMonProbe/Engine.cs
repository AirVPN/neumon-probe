using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;

namespace NeuMonProbe
{
    public class Engine
    {
        public static string ProbeVersion = "20";

        public static List<Job> JobsQueue = new List<Job>();
        public static List<Job> JobsRunning = new List<Job>();
        public static List<Job> JobsCompleted = new List<Job>();

        public static bool Exit = false;        
        public static object LockJobs = new object();
        private static object LockLog = new object();

        public static void Run()
        {
            Log("------------------------");
            Log("NeuMonProbe v" + ProbeVersion);
                        
            Log("Request jobs.");                        
            String data = ContactService("requests","");

            //Log("Data: " + data);
            int nThreads = Options.GetInt("threads",50);
            

            String id = Utils.ReadLine(ref data);
            Options.Set("id", id);
            String password = Utils.ReadLine(ref data);
            Options.Set("password", password);

            Options.SaveIfNotExists();

            String probeNotes = Utils.ReadLine(ref data);
            String probeClass = Utils.ReadLine(ref data);
            String probeDNS = Utils.ReadLine(ref data);            
            
            Log("Probe ID: " + id);
            Log("Password: " + password);
            Log("Class: " + probeClass);
            Log("Notes: " + probeNotes);
            Log("DNS: " + probeDNS);

            // Parsing response
            for (; ; )
            {
                string action = Utils.ReadLine(ref data);

                if (action == "")
                    break;

                //Log("Action: " + action);

                if (action == "Request")
                {
                    Job j = new Job();
                    j.Params["type"] = "request";
                    j.Params["url"] = Utils.ReadLine(ref data);
                    j.Params["ip"] = Utils.ReadLine(ref data);
                    j.Params["mode"] = Utils.ReadLine(ref data);
                    j.Params["class"] = probeClass;
                    j.Params["dns"] = probeDNS;

                    JobsQueue.Add(j);
                }
                else if (action == "Wait")
                {
                    String sSec = Utils.ReadLine(ref data);

                    int iSec = Convert.ToInt32(sSec);

                    Log("Waiting for " + iSec.ToString() + " secs.");

                    Thread.Sleep(iSec * 1000);
                }
                else
                    throw new Exception("Unknown action.");
            }

            // Jobs running
            int logLast = Environment.TickCount;
            for (; ; )
            {
                lock (LockJobs)
                {
                    if (Environment.TickCount - logLast > 1000)
                    {
                        string logMsg = "";
                        logMsg = "Jobs, queue: " + JobsQueue.Count.ToString() + ", running: " + JobsRunning.Count.ToString() + ", completed: " + JobsCompleted.Count.ToString() + ", details: ";
                        foreach (Job j in JobsRunning)
                        {
                            logMsg += j.Params["url"] + ";";
                        }
                        Log(logMsg);
                        logLast = Environment.TickCount;
                    }

                    if ((JobsQueue.Count == 0) && (JobsRunning.Count == 0))
                    {
                        Log("All jobs done.");
                        break;
                    }

                    for (; ; )
                    {
                        if ((JobsQueue.Count > 0) && (JobsRunning.Count < nThreads))
                        {
                            // Start job
                            Job j = JobsQueue[0];
                            JobsQueue.RemoveAt(0);
                            JobsRunning.Add(j);

                            j.T = new Thread(new ThreadStart(j.Run));
                            j.T.Start();
                        }
                        else
                            break;
                    }
                }

                Thread.Sleep(10);
            }

            // Send results
            lock(LockJobs)
            {
                string results = "";

                foreach (Job j in JobsCompleted)
                    results += j.GetResultReport() + "\n";

                JobsCompleted.Clear();
                
                //string postData = "data=" + System.Uri.EscapeUriString(results);
                string postData = "data=" + HttpUtility.UrlEncode(results);

                Log("Sending results.");
                string responseResult = ContactService("responses", postData);
            }

            Log("Done, sleep 10 seconds.");

            Thread.Sleep(1000);
        }

        public static void Log(string s)
        {
            lock (LockLog)
            {
                string l = DateTime.UtcNow.ToString() + " - " + s;
                Console.WriteLine(l);

                if ((Options.Get("id", "") != "") && (Options.Get("logmode", "") == "row"))
                {
                    File.WriteAllText(Utils.GetFullPath(Options.Get("id", "") + ".log"), l);
                }
            }
        }

        public static void LogFatal(string s)
        {
            Log(s);
            lock (LockLog)
            {
                using (StreamWriter w = File.AppendText(Utils.GetFullPath("errors.log")))
                {
                    w.WriteLine(s);
                }
            }
        }

        

        static string GetServiceUrl(string Action)
        {
            return "http://www.neumon.org/probe.php?action=" + Action + "&version=" + ProbeVersion.ToString() + "&probe=" + Options.Get("id","") + "&password=" + Options.Get("password","");
        }

        public static string ContactService(string action, string postData)
        {
            String url = GetServiceUrl(action);

            if (Options.GetBool("debug", false))
                Log("Contact service: " + url);

            String data = GetContentBody(GetContent(url, postData));

            String serverMessage = Utils.ReadLine(ref data);            
            String status = Utils.ReadLine(ref data);

            if(serverMessage.StartsWith("#Error"))
                throw new Exception(serverMessage);

            if ((status == "") || (status.StartsWith("#Error")))
            {
                throw new Exception("Cannot contact project server.");
            }

            if (status == "0")
                throw new Exception(serverMessage);

            Log("Server message: " + serverMessage);

            return data;
        }

        static public string GetContent(string Url)
        {
            return GetContent(Url, "", "");
        }

        static public string GetContent(string Url, string Post)
        {
            return GetContent(Url, Post, "");
        }

        static public string GetContent(string url, string post, string ip)
        {
            int timeout = 10000;
            try
            {
                //Log("Contact " + url);

                Uri uri = new Uri(url);

                string path = uri.PathAndQuery;
                string host = uri.Host;
                int port = uri.Port;
                if (ip == "")
                {
                    IPHostEntry hostEntry = Dns.GetHostEntry(uri.Host);
                    if (hostEntry.AddressList.Length > 0)
                        ip = hostEntry.AddressList[0].ToString();
                    else
                        throw new Exception("Can't resolve");
                }

                /*
                string sContents = string.Empty;
                System.Net.WebClient wc = new System.Net.WebClient();
                byte[] response = wc.DownloadData(Url);
                sContents = System.Text.Encoding.ASCII.GetString(response);
                return sContents;            
                */



                string headers = "";

                if (post != "")
                {
                    headers += "POST";
                }
                else
                {
                    headers += "GET";
                }

                headers += " " + path + " HTTP/1.0\r\n";
                headers += "Host: " + host + "\r\n";
                headers += "User-Agent: Mozilla/5.0 (Windows; U; Windows NT 5.1; it; rv:1.9.0.6) Gecko/2009011913 Firefox/3.0.6\r\n";
                headers += "Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8\r\n";
                headers += "Accept-Language: it-it,it;q=0.8,en-us;q=0.5,en;q=0.3\r\n";
                headers += "Accept-Encoding: \r\n";
                headers += "Accept-Charset: ISO-8859-1,utf-8;q=0.7,*;q=0.7\r\n";
                headers += "Connection: close\r\n";
                if (post != "")
                {
                    headers += "Content-Type: application/x-www-form-urlencoded\r\n";
                    headers += "Content-length: " + post.Length.ToString() + "\r\n";
                }
                headers += "\r\n";
                if (post != "")
                    headers += post;

                IPHostEntry ipAddress = Dns.GetHostEntry(host);
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

                Socket s = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                if (url.IndexOf("neumon.org") == -1)
                {
                    s.ReceiveTimeout = timeout;
                    s.SendTimeout = timeout;
                }
                //s.Connect(ipEndPoint);

                IAsyncResult result = s.BeginConnect(ipEndPoint, null, null);

                bool success = result.AsyncWaitHandle.WaitOne(timeout, true);

                if (!success)
                {
                    s.Close();
                    throw new Exception("timeout.");
                }

                byte[] headersBytes = System.Text.Encoding.ASCII.GetBytes(headers);
                s.Send(headersBytes, SocketFlags.None);

                string response = "";

                byte[] bytes = new byte[1024];
                for (; ; )
                {
                    int l = s.Receive(bytes, 0, 1024, SocketFlags.None);
                    if (l == 0)
                        break;
                    response += Encoding.ASCII.GetString(bytes, 0, l);
                }

                return response;
            }
            catch (SocketException e)
            {
                return "#Error: " + e.SocketErrorCode.ToString();
            }
            catch (Exception e)
            {
                return "#Error: " + e.Message;
            }
        }

        static public string GetContentBody(string Body)
        {
            int pos = Body.IndexOf("\r\n\r\n");
            if (pos != -1)
                return Body.Substring(pos + 4);
            else
                return Body;
        }

        static public string GetSpecialBody(string response)
        {
            string result = "";

            if (response.Trim() == "")
                return "#Error: Empty";
            if (response.StartsWith("#Error"))
                return response;


            string headers = "";
            string body = "";
            int posBody = response.IndexOf("\r\n\r\n");
            if (posBody == -1)
                body = response.ToLowerInvariant();
            else
            {
                headers = response.Substring(0, posBody).ToLowerInvariant();
                body = response.Substring(posBody + 4).ToLowerInvariant();
            }

            // HTTP Code
            {
                Match matchHttp = System.Text.RegularExpressions.Regex.Match(headers,"http/[0-9\\.]+\\s([0-9]+?)\\s",RegexOptions.IgnoreCase);
                if( (matchHttp.Success) && (matchHttp.Groups.Count>=2) )
                {
                    if (result != "")
                        result += ",";
                    result += "c:" + matchHttp.Groups[1];                    
                }
            }

            // Redirect?
            {
                int posLocation = headers.IndexOf("location:");
                if (posLocation != -1)
                {
                    int posLocationEnd = headers.IndexOf("\n", posLocation);
                    string url;
                    if (posLocationEnd == -1)
                        url = headers.Substring(posLocation + 9);
                    else
                        url = headers.Substring(posLocation + 9, posLocationEnd - posLocation - 9).Trim();

                    if (result != "")
                        result += ",";
                    result += "l:" + url.Trim();
                    return result;
                }
            }

            // Title?
            {
                int posTitle = body.IndexOf("<title>");
                if (posTitle != -1)
                {
                    int posTitleEnd = body.IndexOf("</title>", posTitle);
                    if (posTitleEnd != -1)
                    {
                        string title = body.Substring(posTitle + 7, posTitleEnd - posTitle - 7);

                        if (result != "")
                            result += ",";
                        result += "t:" + title.Replace(";",",");
                        return result;
                    }
                }
            }

            System.Security.Cryptography.SHA1 hash = System.Security.Cryptography.SHA1CryptoServiceProvider.Create();
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(response);
            byte[] hashBytes = hash.ComputeHash(plainTextBytes);

            if (result != "")
                result += ",";
            result += "h:" + BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            return result;
        }
    }
}
