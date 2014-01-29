using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using GeoIP;

namespace fdocheck
{
    public class Q3ServerPlayer
    {
        /// <summary>
        /// The nickname of the player on the server.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The score in points which the player has earned on the server.
        /// </summary>
        public int Score { get; private set; }

        /// <summary>
        /// The ping in milliseconds of the player on the server.
        /// </summary>
        public int Ping { get; private set; }

        internal static Q3ServerPlayer FromReplyLine(string rawline)
        {
            string[] args = rawline.Split(" \t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            return new Q3ServerPlayer()
            {
                Score = int.Parse(args[0]),
                Ping = int.Parse(args[1]),
                Name = string.Join(" ", args.Skip(2)).Trim('"')
            };
        }
    }

    public class Q3ServerConnection
    {
        private static GeoIPCountry _geoSingletonInstance = null;
        private static GeoIPCountry Geo
        {
            get
            {
                if (_geoSingletonInstance != null || !System.IO.File.Exists("GeoIP.dat"))
                    return null;
                
                return _geoSingletonInstance = new GeoIPCountry("GeoIP.dat");
            }
        }

        private Socket _conn;
        private Stopwatch _stopwatch;

        public EndPoint EndPoint
        {
            get;
            private set;
        }
        public long Ping
        {
            get { return GetPing(); }
        }
        public string Country
        {
            get
            {
                if (Geo == null)
                    return default(string);
                else
                {
                    try
                    {
                        return Geo.GetCountryCode(((IPEndPoint)EndPoint).Address);
                    }
                    catch
                    {
                        return default(string);
                    }
                }
            }
        }

        public Q3ServerConnection(string hostname, int port)
        {
            EndPoint = new DnsEndPoint(hostname, port);
            _conn = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _conn.Connect(EndPoint);
        }

        public Q3ServerConnection(IPAddress ip, int port)
        {
            EndPoint = new IPEndPoint(ip, port);
            _conn = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _conn.Connect(EndPoint);
        }

        private void SendCommand(string command)
        {
            this.SendCommand(new byte[4] { 0xFF, 0xFF, 0xFF, 0xFF }, command);
        }

        private void SendCommand(byte[] header, string command)
        {
            _conn.Send(header.Concat(Encoding.ASCII.GetBytes(command)).ToArray<byte>());
        }

        private long GetPing()
        {
            try
            {
                var c = Encoding.ASCII.GetBytes("ping\n");
                _stopwatch = new Stopwatch();
                byte[] buffer = new byte[128]; // will contain "disconnect" usually
                var remoteEP = EndPoint;

                _conn.Send(buffer);
                _stopwatch.Start();
                _conn.ReceiveFrom(buffer, ref remoteEP);
                _stopwatch.Stop();

                return _stopwatch.ElapsedMilliseconds;
            }
            catch(Exception m)
            {
                Console.WriteLine(m);
                return -1;
            }
        }

        private byte[] ReadRawReply()
        {
            byte[] buffer = new byte[512 * 1024];
            var remoteEP = EndPoint;
            var length = _conn.ReceiveFrom(buffer, ref remoteEP);
            byte[] data = new byte[length];
            Array.Copy(buffer, data, length);
            return data;
        }

        private string[] ReadTextReply()
        {
            var b =
                (from line in Encoding.ASCII.GetString(ReadRawReply()).Substring(4).Split('\n')
                 select line.TrimEnd('\r', '\0'))
                .ToArray<string>();
            //foreach (var a in b)
            //    Console.WriteLine("[Reply from {0}] {1}", EndPoint, a);
            return b;
        }

        public Dictionary<string, string> GetInfoDvars()
        {
            this.SendCommand("getinfo");
            var reply = ReadTextReply();
            if (reply[0].Equals("infoResponse", StringComparison.OrdinalIgnoreCase))
                return ParseParams(reply[1].Split('\\'));
            else
                return null;
        }

        public Dictionary<string, string> GetStatusDvars()
        {
            this.SendCommand("getstatus");
            var reply = ReadTextReply();
            if (reply[0].Equals("statusResponse", StringComparison.OrdinalIgnoreCase))
                return ParseParams(reply[1].Split('\\'));
            else
                return null;
        }

        public Q3ServerPlayer[] GetPlayers()
        {
            this.SendCommand("getstatus");
            var reply = ReadTextReply();
            if (reply[0].Equals("statusResponse", StringComparison.OrdinalIgnoreCase))
                return (from line in reply.Skip(2)
                        where !string.IsNullOrEmpty(line)
                        select Q3ServerPlayer.FromReplyLine(line))
                       .ToArray<Q3ServerPlayer>();
            else
                return null;
        }

        private static Dictionary<string, string> ParseParams(string[] parts)
        {
            /*
            splittedLine = splittedLine.Where(line => !string.IsNullOrEmpty(line)).ToArray();
            return splittedLine.Where((value, index) => index % 2 != 0) // Keys
            .Zip(
                splittedLine.Where((value, index) => index % 2 == 0), // Values
                (Name, Value) => new { Name, Value }
            ).ToDictionary(Item => Item.Name, Item => Item.Value);
             */

            string key, val;
            var paras = new Dictionary<string, string>();

            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length == 0)
                {
                    continue;
                }

                key = parts[i++];
                val = parts[i];

                paras[key] = val;
            }

            return paras;

        }
    }
}
