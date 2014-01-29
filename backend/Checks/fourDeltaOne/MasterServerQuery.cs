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
    public class MasterServerQuery
    {
        internal MasterServerQuery() { }

        public MasterServerQuery(string mastername, string server, int port, string servertype, int protocol)
        {
            Name = mastername;
            MasterServer = server;
            MasterPort = port;
            ServerType = servertype;
            Protocol = protocol;
        }

        public string Name;
        public string MasterServer;
        public int MasterPort;
        public int Protocol = 61586;
        public string ServerType = "IW4";
        [JsonIgnore()]
        public MasterServerEntry[] ServersListed = { };
        public string End = " \n";

        internal ILog log { get { return LogManager.GetLogger("MasterServerQuery(" + MasterServer + ":" + MasterPort + ")"); } }

        public MasterServerEntry[] GetServers(string keys = "full empty")
        {
            for (int ixi = 0; ixi < 5; ixi++)
            {
#if !DEBUG
                try
                {
#endif
                    log.Debug("Fetching servers...");
                    byte[] content =
                        new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }
                        .Concat(Encoding.ASCII.GetBytes("getservers " + ServerType + " " + Protocol + " " + keys + End))
                        .ToArray()
                        ; // getservers command
                    log.Debug("Sending request: " + Encoding.ASCII.GetString(content).Trim() + " to: " + MasterServer + ", port " + MasterPort);

                    UdpClient udp = new UdpClient();
                    udp.Client.ReceiveTimeout = 4000;
                    udp.Client.SendTimeout = 3000;
                    udp.Connect(MasterServer, MasterPort);

                    var servers = new List<MasterServerEntry>();
                    var data = new List<byte>();

                    // Get all packets and merge them.
                    var remoteEP = udp.Client.RemoteEndPoint as IPEndPoint;
                    udp.Send(content, content.Length);
                    try
                    {
                        while (true)
                        {
                            DateTime dt = DateTime.Now;
                            while (DateTime.Now.Subtract(dt).TotalMilliseconds < udp.Client.ReceiveTimeout && udp.Available == 0)
                                System.Threading.Thread.Sleep(50);
                            if (udp.Available == 0)
                                throw new Exception("Detected connection timeout");
                            byte[] thispack = udp.Receive(ref remoteEP);
                            int length = thispack.Length;
                            if (thispack[0] == (byte)0xFF
                                && thispack[1] == (byte)0xFF
                                && thispack[2] == (byte)0xFF
                                && thispack[3] == (byte)0xFF)
                                thispack = thispack.Skip("####getserversResponse".Length).ToArray<byte>();
                            data.AddRange(thispack);
                            udp.Client.ReceiveTimeout = 1000;
                            udp.Client.SendTimeout = 1000;

                            if (length < 1394) // Found out by testing that every non-ending packet has 1394 bytes maximum. Works on every master server.
                                break;
                            if (Encoding.ASCII.GetString(thispack).Contains("\\EOT"))
                                break;
                        }
                    }
                    catch (Exception n)
                    {
                        log.Warn("Receive error after " + data.Count + " bytes: " + n);
                        log.WarnFormat("Receive error reason: {0}", n.InnerException);
                        log.Warn("Emptying server list, invalid or erroneous response received.");
                        try
                        {
                            udp.Close();
                        }
                        catch { { } }
                        continue;
                    }

                    System.IO.File.WriteAllBytes("serverlist_" + this.MasterServer + ".bin", data.ToArray<byte>());

                    // A modified ported version of the 4D1 server list parser
                    // Original written by NTAuthority in C++
                    // Again modified from the [4D1] bot source to also parse IW5M servers
                    int buffptr = 0;
                    int buffend = buffptr + 29;

                    while (buffptr + 1 < buffend)
                    {

                        while (++buffptr < data.Count - 6)
                            if (data[buffptr] == '\\') // byte 0
                                break;

                        if (buffptr >= data.Count - 6)
                            break;

                        if ((data[buffptr + 1] == 'E'
                            && data[buffptr + 2] == 'O'
                            && data[buffptr + 3] == 'T'
                            ))
                            break;


                        // parse out ip
                        var ip = new IPAddress(new byte[] {
                            data[++buffptr], // byte 1
                            data[++buffptr], // byte 2
                            data[++buffptr], // byte 3
                            data[++buffptr]  // byte 4
                        });


                        // parse out ports
                        List<int> ports = new List<int>();
                        for (int x = 0; x < (ServerType.Equals("IW4") ? 1 : 2); x++)
                        {
                            /*
                            ushort portx = (ushort)((data[++buffptr]) << 8);
                            int port = portx + ((data[++buffptr]) & 0xFF);
                             */
                            ushort port = (ushort)IPAddress.NetworkToHostOrder((short)BitConverter.ToUInt16(data.ToArray<byte>(), ++buffptr));
                            buffptr++;
                            ports.Add(port);
                        }

                        servers.Add(new MasterServerEntry(ip, ports.ToArray<int>()));

                        buffend = buffptr + 8;
                    }

                    log.Info("Found a total of " + servers.Count + " servers on this master server");
                    System.Threading.Thread.Sleep(1500);
                    return ServersListed = servers.ToArray();
#if !DEBUG
                }
                catch { continue; }
#endif
            }
            return new MasterServerEntry[] { };
            //return ServersListed = new MasterServerEntry[0];
        }
    }
}
