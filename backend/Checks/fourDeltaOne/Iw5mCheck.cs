using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Xml;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using log4net;

namespace fdocheck.Checks
{
    public class Iw5mCheck : BasicCheck
    {
        public bool MasterOnline { get { return MasterCounter > 5; } }
        public bool NPOnline { get { return NPCounter > 5; } }

        public string MasterServer { get; set; }
        public int MasterPort { get; set; }
        public string NPServer { get; set; }
        public int NPPort { get; set; }
        public int MasterCounter = 5;
        public int NPCounter = 5;

        [JsonIgnore()]
        public FDOAuthServerCheck Authentication { get; set; }

        [JsonIgnore()]
        public MasterServerEntry[] ListedServers = new MasterServerEntry[0];

        public Iw5mCheck(FDOAuthServerCheck authentication)
        {
            NPServer = "iw5.prod.fourdeltaone.net";
            NPPort = 3025;
            MasterServer = "iw5.prod.fourdeltaone.net";
            MasterPort = 27950;
            Authentication = authentication;
        }

        public MasterServerEntry[] GetServers()
        {
            if (!this.NeedToUpdate("serverlist", 60))
                return ListedServers;

            var msq = new MasterServerQuery("4D1", MasterServer, MasterPort, "IW5", 19816);
            msq.ServerType = "IW5";
            msq.Protocol = 19816;
            msq.End = " \n";
            return ListedServers = msq.GetServers();
        }

        public void CheckMaster()
        {
            if (!NeedToUpdate("master", 60)) return;

            log.Debug("Checking master server...");
            if (GetServers().Length > 0)
            {
                log.Debug("Master server online.");
                if(MasterCounter < 7) MasterCounter++;
            }
            else
            {
                log.Fatal("Master server offline.");
                if (MasterCounter > 0) MasterCounter--;
            }
        }

        public void CheckNP()
        {
            if (!NeedToUpdate("np", 60)) return;

            if (Authentication.SessionID == null)
            {
                Authentication.CheckAuth();
                if (Authentication.SessionID == null)
                {
                    if (NPCounter > 0) NPCounter--;
                    log.Error("Can not check NP connection without valid test login.");
                    return;
                }
            }


            TcpClient tcp = new TcpClient();
            try
            {
                tcp.ReceiveTimeout = 4000;
                tcp.SendTimeout = 4000;
                log.Info("Initiating NP connection...");
                var res = tcp.BeginConnect(NPServer, NPPort, null, null);
                var start = DateTime.Now;
                log.Debug("Now waiting for connection...");
                while (DateTime.Now.Subtract(start).TotalMilliseconds < tcp.ReceiveTimeout && !res.IsCompleted)
                    System.Threading.Thread.Sleep(50);
                if (!res.IsCompleted)
                    throw new Exception("Connection timeout detected");

                var ns = tcp.GetStream();
                //var sr = new StreamReader(ns);
                //var sw = new StreamWriter(ns);
                var buffer = new byte[1024];
                
                tcp.ReceiveTimeout = 2000;
                tcp.SendTimeout = 2000;
                log.Info("Testing if server is reacting...");
                var sidContent = Encoding.ASCII.GetBytes(Authentication.SessionID);
                var preContent = new byte[] {
                    0xde, 0xc0, 0xad, 0xde, 0x22, 0x00, 0x00, 0x00, 
                    0xeb, 0x03, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 
                    0x0a
                }; // Contains some stuff for SID check
                var motdContent = new byte[] {
                    0xde, 0xc0, 0xad, 0xde, 0x12, 0x00, 0x00, 0x00, 
                    0x4d, 0x04, 0x00, 0x00, 0x07, 0x00, 0x00, 0x00, 
                    0x0a, 0x10, 0x6d, 0x6f, 0x74, 0x64, 0x2d, 0x65, 
                    0x6e, 0x67, 0x6c, 0x69, 0x73, 0x68, 0x2e, 0x74, 
                    0x78, 0x74
                }; // Contains the request for "motd-english.txt"

                ns.Write(preContent, 0, preContent.Length); // SID check header
                ns.WriteByte((byte)sidContent.Length); // SID length
                ns.Write(sidContent, 0, sidContent.Length); // SID
                ns.Flush();
                ns.Read(buffer, 0, 28); // header of response
                ns.Read(buffer, 0, ns.ReadByte()); // (Length +) SID
                while(ns.DataAvailable)
                    ns.ReadByte(); // still need to find out what that additional n x 18 bytes content is
                log.Debug("Server successfully responded to SID check request.");
                // Send motd-english.txt request
                ns.Write(motdContent, 0, motdContent.Length);
                ns.Flush();
                ns.Read(buffer, 0, 1024); // header + content
                // This should be enough.
                try
                {
                    ns.Close();
                    ns.Dispose();
                    tcp.Close();
                }
                catch { { } }
                log.Info("NP connection test succeeded.");

                if (NPCounter < 7) NPCounter++;
            }
            catch (Exception err)
            {
                if (NPCounter > 0) NPCounter--;
                log.Fatal("NP connection test failed: " + err);
            }
        }
    }
}
