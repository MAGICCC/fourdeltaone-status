using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json;
using log4net;

namespace fdocheck.Checks
{
    public class BasicCheck
    {
        internal ILog log { get { return LogManager.GetLogger(this.GetType().Name); } }

        [JsonIgnore()]
        internal const string HTTP_TESTPACKET = "POST / HTTP/1.0\r\nHost: {0}\r\nUser-Agent: 4D1ServerCheck/1.0\r\nContent-length: 0\r\nConnection: close\r\n\r\n";

        [JsonIgnore()]
        internal byte[] NP_TESTPACKET { get { return new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0x20 }.Concat(Encoding.ASCII.GetBytes("getservers")).ToArray(); } }

        [JsonProperty("CacheDates")]
        Dictionary<string, DateTime> cache = new Dictionary<string, DateTime>();

        [JsonProperty("CacheIntervals")]
        Dictionary<string, int> cacheInt = new Dictionary<string, int>();

        [JsonIgnore()]
        protected TimeoutableWebClient wc = new TimeoutableWebClient();

        public DateTime InstanceCreateTime = DateTime.Now;

        public bool NeedToUpdate(string id, int seconds)
        {
            if (cache.ContainsKey(id))
            {
                if ((DateTime.Now - cache[id]).TotalSeconds >= seconds)
                {
                    lock (cacheInt)
                    {
                        lock (cache)
                        {
                            cache[id] = DateTime.Now;
                            cacheInt[id] = seconds;
                        }
                    }
                    log.Debug("Cache needs to be refreshed for " + id);
                    return true;
                }
                else
                    return false;
            }
            else
            {
                lock (cacheInt)
                {
                    lock (cache)
                    {
                        cacheInt.Add(id, seconds);
                        cache.Add(id, DateTime.Now);
                    }
                }
                log.Debug("Cache needs to be generated for " + id);
                return true;
            }
        }
        public bool TcpCheck(string host, int port)
        { return TcpCheck(new byte[] { }, host, port); }
        public bool TcpCheck(byte[] testpacket, string host, int port)
        {
            try
            {
                TcpClient tcp = new TcpClient();
                tcp.ReceiveTimeout = 600;
                tcp.SendTimeout = 600;
                tcp.NoDelay = true;
                tcp.Client.ReceiveTimeout = 600;
                tcp.Client.SendTimeout = 600;
                var async = tcp.BeginConnect(host, port, null, null);
                DateTime dt = DateTime.Now;
                log.Debug("[" + host + ":" + port + " via TCP] connecting");
                while ((DateTime.Now - dt).TotalSeconds < 1 && !async.IsCompleted)
                    System.Threading.Thread.Sleep(40);
                if (!async.IsCompleted)
                {
                    log.Debug("[" + host + ":" + port + " via TCP] connection failed, aborting");
                    tcp.Client.Disconnect(false);
                    log.Debug("[" + host + ":" + port + " via TCP] connection aborted");
                    return false;
                }
                log.Debug("[" + host + ":" + port + " via TCP] connected");
                bool r = true;
                if (testpacket != null && testpacket.Length > 0)
                {
                    NetworkStream ns = tcp.GetStream();
                    ns.Write(testpacket, 0, testpacket.Length);
                    ns.Flush();
                    r = (ns.ReadByte() != -1);
                }
                log.Debug(string.Format("[" + host + ":" + port + " via TCP] Server status: CONN={0}; RESP={1}; REQLEN={2}", tcp.Connected, r, testpacket.Length));
                return tcp.Connected && r;
            }
            catch (Exception n)
            {
                log.Debug(string.Format("[" + host + ":" + port + " via TCP] Server status: Exception of type {1} ({0})", n.Message, n.GetType().Name));
                return false;
            }
        }
        public bool UdpCheck(byte[] testpacket, string host, int port)
        {
            UdpClient udp = new UdpClient();
            try
            {
                udp.Client.ReceiveTimeout = 600;
                udp.Client.SendTimeout = 600;
                udp.Connect(host, port);
            }
            catch (Exception n)
            {
                log.Debug(string.Format("[" + host + ":" + port + " via UDP] Socket init error ({0})", n.Message));
                return false;
            }
            for (int tries = 0; tries < 4; tries++)
            {
                try
                {
                    log.Debug(string.Format("[" + host + ":" + port + " via UDP] Request, try #{0}", tries + 1));


                    udp.Send(testpacket, testpacket.Length);
                    var remoteEP = udp.Client.RemoteEndPoint as IPEndPoint;
                    bool r = (udp.Receive(ref remoteEP).Length > 0);
                    log.Debug(string.Format("[" + host + ":" + port + " via UDP] Server status: RESP={0}; REQLEN={1}", r, testpacket.Length));
                    return r;
                }
                catch (Exception n)
                {
                    try
                    {
                        udp.Close();
                    }
                    catch { { } }
                    log.Debug(string.Format("[" + host + ":" + port + " via UDP] Server status: Exception of type {1} ({0})", n.Message, n.GetType().Name));
                }
            }
            return false;
        }
        public bool UdpCheck(string testpacket, string host, int port)
        { return UdpCheck(Encoding.ASCII.GetBytes(testpacket), host, port); }
        public bool TcpCheck(string testpacket, string host, int port)
        { return TcpCheck(Encoding.ASCII.GetBytes(testpacket), host, port); }

    }
}
