using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace fdocheck.Checks
{
    public class FDOAuthServerCheck : BasicCheck
    {
        public FDOAuthServerCheck()
        {
            AuthServer = "auth.iw4.prod.fourdeltaone.net";
            AuthPort = 1337;
            AuthInternalPort = 3105;
        }

        public string AuthServer { get; set; }
        public int AuthPort { get; set; }
        public int AuthInternalPort { get; set; }

        public bool AuthOnline { get { return AuthCounter > 3; } }
        public int AuthCounter = 3;

        public bool AuthInternalOnline { get { return AuthInternalCounter > 3; } }
        public int AuthInternalCounter = 3;

        [JsonIgnore()]
        internal string SessionID { get; set; }
        [JsonIgnore()]
        internal string TestUsername = "test";
        [JsonIgnore()]
        internal string TestPassword = "test";

        public void CheckAuthInternal()
        {
            if (!NeedToUpdate("auth-internal", AuthInternalCounter > 5 ? 500 : 120)) return;

            if (!UdpCheck(Encoding.ASCII.GetBytes("checkSession 0 0\n"), AuthServer, AuthInternalPort))
            {
                if (AuthInternalCounter > 0) AuthInternalCounter--;
            }
            else
                if (AuthInternalCounter < 6) AuthInternalCounter++;
        }

        public void CheckAuth()
        {
            if (!NeedToUpdate("auth", AuthCounter > 5 ? 500 : 120)) return;

            // Do a test login via HTTP
            log.Debug("Testing auth login");
            try
            {
                string[] res = wc.UploadString("http://" + AuthServer + ":" + AuthPort + "/remauth.php", TestUsername + "&&" + TestPassword).Split(new[] { "\x0d\x0a" }, StringSplitOptions.RemoveEmptyEntries);
                //string statusCode = res[0];
                //string[] loginInfo = res[1].Split('#');
                string[] loginInfo = res[0].Split('#'); // what the actual fuck
                string loginStatus = loginInfo[0];
                //string loginStatusText = loginInfo[1];
                //string loginUserId = loginInfo[2];
                //string loginUsername = loginInfo[3];
                //string loginEmail = loginInfo[4];
                string loginSessionId = loginInfo[5];
                loginInfo = null;

                if (loginStatus != "ok")
                {
                    if (AuthCounter > 0) AuthCounter--;
                    log.Fatal("Auth server refused test login, server returned: " + res[0]);
                }
                else
                {
                    if (AuthCounter < 6) AuthCounter++;
                    SessionID = loginSessionId;
                    log.Info("Test login on auth server succeeded, sessionId is " + SessionID + ".");
                }
            }
            catch (Exception n)
            {
                if (AuthCounter > 0) AuthCounter--;
                log.Fatal("Auth server did not react: " + n.Message);
            }
        }
    }
}
