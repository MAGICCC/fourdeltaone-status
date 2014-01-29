using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Xml;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace fdocheck.Checks
{
    public class Iw4mCheck : BasicCheck
    {
        public bool UpdateOnline = false;

        [JsonIgnore()]
        public FDOAuthServerCheck Authentication { get; set; }

        public bool NPOnline { get { return NPCounter > 2; } }
        public int NPCounter = 3;

        public bool MasterOnline { get { return MasterCounter > 2; } }
        public int MasterCounter = 3;

        public string NPServer { get; set; }
        public int NPPort { get; set; }
        public string MasterServer { get; set; }
        public int MasterPort { get; set; }

        public Iw4mCheck(FDOAuthServerCheck authentication)
        {
            NPServer = "iw4.prod.fourdeltaone.net";
            NPPort = 3025;
            MasterServer = "iw4.prod.fourdeltaone.net";
            MasterPort = 20810;

            // 4D1
            AddMasterServer("4D1", "iw4.prod.fourdeltaone.net");

            Authentication = authentication;
        }

        public void CheckUpdate()
        {
            if (!NeedToUpdate("update", 120)) return;
            for (int i = 0; i < 4; i++)
            {
                try
                {
                    while (wc.IsBusy)
                        Thread.Sleep(500);

                    string xml = wc.DownloadString("http://iw4.prod.fourdeltaone.net/content/caches.xml");
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(xml);

                    /*
                    UpdateVersion = "Rev " + wc.DownloadString(ReleaseVersionUrl).Trim();
                     */
                    UpdateOnline = true;
                    log.Info("Update server is online");
                    return;
                }
                catch (Exception k)
                {
                    log.Error("Update server is eventually offline: " + k);
                }
            }
            UpdateOnline = false;
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
                log.Debug("Initiating NP connection...");
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
                var sidContent = Encoding.ASCII.GetBytes(Authentication.SessionID); // SID ASCII-encoded
                var preContent = new byte[] {
                    0xde, 0xc0, 0xad, 0xde, 0x22, 0x00, 0x00, 0x00, 
                    0xeb, 0x03, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 
                    0x0a
                }; // Contains some stuff for SID check
                var iw4statContent = new byte[] {
                    0xde, 0xc0, 0xad, 0xde, 0x13, 0x00, 0x00, 0x00, 
                    0x4e, 0x04, 0x00, 0x00, 0x03, 0x00, 0x00, 0x00, 
                    0x0a, 0x08, 0x69, 0x77, 0x34, 0x2e, 0x73, 0x74, 
                    0x61, 0x74, 0x11, 0xbb, 0x16, 0x00, 0x00, 0x01, 
                    0x00, 0x10, 0x01
                }; // Contains the request for "iw4.stat"

                ns.Write(preContent, 0, preContent.Length); // SID check header
                ns.WriteByte((byte)sidContent.Length); // SID length
                ns.Write(sidContent, 0, sidContent.Length); // SID
                ns.Flush();
                ns.Read(buffer, 0, 28); // header of response
                ns.Read(buffer, 0, ns.ReadByte()); // (Length +) SID
                while (ns.DataAvailable)
                    ns.ReadByte(); // still need to find out what that additional n x 18 bytes content is
                log.Debug("Server successfully responded to SID check request.");
                // Send iw4.stat request
                ns.Write(iw4statContent, 0, iw4statContent.Length);
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
            catch(Exception err)
            {
                if (NPCounter > 0) NPCounter--;
                log.Fatal("NP connection test failed: " + err);
            }
        }

        public int MasterServersRealListed { get; private set; }
        public int MasterServersListed { get { int i = 0; foreach (var ams in AccessibleMasterServers) if(ams.ServersListed != null) i += ams.ServersListed.Length; return i; } }
        public int LegacyServersCount { get; private set; }

        [JsonIgnore()]
        MasterServerEntry[] cachedList = { };
        [JsonIgnore()]
        public MasterServerEntry[] CachedMergedServerList { get { return cachedList; } }

        public void AddMasterServer(string name, string host, int port = 20810, bool legacy = false)
        {
            AccessibleMasterServers.Add(new MasterServerQuery(name, host, port, "IW4", legacy ? 142 : 61586));
        }

        public List<MasterServerQuery> AccessibleMasterServers = new List<MasterServerQuery>();

        private MasterServerEntry[] JoinServerLists(params MasterServerEntry[][] serverlists)
        {
            LegacyServersCount = 0;
            log.Debug("Joining master server lists...");
            List<MasterServerEntry> joinedlist = new List<MasterServerEntry>();
            bool isLegacy = false;
            foreach (MasterServerEntry[] serverlist in serverlists)
            {
                if (serverlist == null)
                    continue;
                foreach (MasterServerEntry server in serverlist)
                    if ((from s in joinedlist where s.IP.Equals(server.IP) && s.Ports.Equals(s.Ports) select s).Count() == 0)
                    {
                        joinedlist.Add(server);
                        if (isLegacy)
                            LegacyServersCount++;
                    }
                isLegacy = true;
            }
            MasterServersRealListed = joinedlist.Count;
            return joinedlist.ToArray();
        }

        public MasterServerEntry[] GetServers()
        {
            if (!NeedToUpdate("serverlist", 60))
                return cachedList;

            List<MasterServerEntry[]> serverlists = new List<MasterServerEntry[]>();
            foreach (MasterServerQuery master in AccessibleMasterServers)
            {
                try
                {
                    MasterServerEntry[] servers = master.GetServers();
                    if (servers == null)
                        continue;
                    serverlists.Add(servers);
                }
                catch (Exception exc)
                {
                    log.Error("Fail at master query: " + exc.Message);
                }
            }
            return cachedList = JoinServerLists(serverlists.ToArray());
        }

        public void CheckMaster()
        {
            if (!NeedToUpdate("master", 60)) return;

            log.Debug("Checking master server");
            this.GetServers();
            if (this.AccessibleMasterServers[0].GetServers().Length > 0)
                MasterCounter++;
            else
                MasterCounter--;


            if (MasterCounter < 0) MasterCounter = 0;
            if (MasterCounter > 7) MasterCounter = 7;
        }
    }
}
