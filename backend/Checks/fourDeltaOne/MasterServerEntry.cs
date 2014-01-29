using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Newtonsoft.Json;

namespace fdocheck.Checks
{
    //http://geolite.maxmind.com/download/geoip/database/GeoLiteCountry/GeoIP.dat.gz
    public class MasterServerEntry
    {
        [JsonIgnore()]
        public IPAddress IP { get; set; }
        public string IPString { get { return IP.ToString(); } }
        public int[] Ports { get; set; }
        public MasterServerEntry(IPAddress ip, params int[] ports)
        {
            IP = ip;
            Ports = ports;
        }
        public MasterServerEntry(IPAddress ip, int ports)
        {
            IP = ip;
            Ports = new int[1] { ports };
        }
    }
}
