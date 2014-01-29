using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Net.Sockets
{
    static class NetworkSocketEx
    {
        public static byte[] ReadBytes(this NetworkStream ns, int byteCount)
        {
            byte[] bytes = new byte[byteCount];
            ns.Read(bytes, 0, byteCount);
            return bytes;
        }
    }
}
