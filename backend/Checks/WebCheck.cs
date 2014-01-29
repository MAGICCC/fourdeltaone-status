using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Newtonsoft.Json;

namespace fdocheck.Checks
{
    class WebCheck
    {
        [JsonIgnore()]
        TimeoutableWebClient wc = new TimeoutableWebClient();
        [JsonIgnore()]
        private Uri Uri;

        [JsonProperty("LastCheck")]
        DateTime lastCheck = DateTime.MinValue;

        [JsonProperty("CheckInterval")]
        public int Interval = 90;

        [JsonProperty("LastError")]
        public string Error = "";

        public bool Online = false;

        public WebCheck(Uri url, int iv)
        {
            Uri = url;
            Interval = iv;
        }
        public WebCheck(string url, int iv)
        {
            Uri = new Uri(url);
            Interval = iv;
        }
        public WebCheck(Uri url)
        {
            Uri = url;
        }
        public WebCheck(string url)
        {
            Uri = new Uri(url);
        }

        public void Check()
        {
            // Caching
            if ((DateTime.Now - lastCheck).TotalSeconds < Interval)
                return;

            lastCheck = DateTime.Now;

            // Actual check
            for (int i = 0; i < 2; i++)
            {
                try
                {
                    wc.DownloadString(Uri);
                    Online = true;
                    break;
                }
                catch (WebException we)
                {
                    Online = false;
                    Error = "HTTP error - " + we.Status.ToString();
                }
                catch (Exception)
                {
                    Online = false;
                    Error = "Unknown error";
                }
            }
        }
    }
}
