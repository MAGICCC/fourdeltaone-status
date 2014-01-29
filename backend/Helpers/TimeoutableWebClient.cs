using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System.Net
{
    public class TimeoutableWebClient : WebClient
    {
        public TimeoutableWebClient()
        {
            Timeout = 4000;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest webRequest = base.GetWebRequest(address);
            webRequest.Timeout = this.Timeout;
            return webRequest;
        }

        public int Timeout { set; get; }
    }
}
