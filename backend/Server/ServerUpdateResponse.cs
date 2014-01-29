using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace fdocheck.Server
{
    public class ServerUpdateResponse
    {
        public ServerUpdateStatus Status { get; set; }
        public object Data { get; set; }
    }

    public enum ServerUpdateStatus
    {
        Finished = 0x0,

        FetchingServersFromMasterServer = 0x1,
        FetchingServerInfo = 0x2,

        TimedOut = 0x3,
        Success = 0x4,

        ServerListInit = 0x5,

        Error = 0xF
    }
}
