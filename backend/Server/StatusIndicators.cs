using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using fdocheck.Checks;

namespace fdocheck.Server
{
    public class BasicStatusIndicator
    {
        public BasicStatusIndicator()
        {
        }
    }

    class Iw4mStatusIndicator : BasicStatusIndicator
    {
        Iw4mCheck iw4m;

        public Iw4mStatusIndicator(ref Iw4mCheck iw4m)
        {
            this.iw4m = iw4m;
        }
        
        public bool NP { get { return iw4m.NPOnline; } }
        public bool Master { get { return iw4m.MasterOnline; } }
    }

    class Iw5mStatusIndicator : BasicStatusIndicator
    {
        Iw5mCheck iw5m;

        public Iw5mStatusIndicator(ref Iw5mCheck iw5m)
        {
            this.iw5m = iw5m;
        }
        
        public bool NP { get { return iw5m.NPOnline; } }
        public bool Master { get { return iw5m.MasterOnline; } }
    }

    class KmshostStatusIndicator : BasicStatusIndicator
    {
        KmshostCheck kms;

        public KmshostStatusIndicator(ref KmshostCheck kms)
        {
            this.kms = kms;
        }
        
        public bool Online { get { return kms.KmshostOnline; } }
    }

    class WebStatusIndicator : BasicStatusIndicator
    {
        WebCheck web;

        public WebStatusIndicator(ref WebCheck web)
        {
            this.web = web;
        }
        
        public bool Online { get { return web.Online; } }
    }

    class LoginStatusIndicator : BasicStatusIndicator
    {
        FDOAuthServerCheck auth;

        public LoginStatusIndicator(ref FDOAuthServerCheck auth)
        {
            this.auth = auth;
        }
        
        public bool Auth { get { return auth.AuthOnline; } }
        public bool AuthInternal { get { return auth.AuthInternalOnline; } }
    }
}
