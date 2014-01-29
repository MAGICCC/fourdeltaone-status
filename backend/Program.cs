using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using fdocheck.Server;
using fdocheck.Checks;
using log4net;
using log4net.Config;
using System.Xml;

namespace fdocheck
{
    static class Program
    {
        static readonly ILog log = LogManager.GetLogger(typeof(Program));

        public static string BackendName = "StatusAPI";
        static APIServer api;
        static FDOAuthServerCheck auth;
        static Iw4mCheck iw4m;
        static Iw5mCheck iw5m;
        static WebCheck forum;
        static KmshostCheck kmshost;

        static XmlDocument config;

        static int Main(string[] args)
        {
            Console.WriteLine("API server initializing...");

            config = new XmlDocument();
            config.Load("config.xml");

            var cancel = new CancellationTokenSource();

            var appender = new log4net.Appender.ManagedColoredConsoleAppender();
            appender.Threshold = log4net.Core.Level.All;
            var x = (XmlElement)config.SelectSingleNode("//backend/log4net");
            if (x == null)
            {
                Console.WriteLine("Error: log4net configuration node not found. Check your config.xml");
                Console.ReadKey();
                return -1;
            }
            XmlConfigurator.Configure(x);

            api = new APIServer();
            auth = new FDOAuthServerCheck();
            iw4m = new Iw4mCheck(auth);
            iw5m = new Iw5mCheck(auth);
            forum = new WebCheck("http://fourdeltaone.net/index.php", 60);
            kmshost = new KmshostCheck();

            auth.TestUsername = config.SelectSingleNode("//backend/auth-username").InnerText;
            auth.TestPassword = config.SelectSingleNode("//backend/auth-password").InnerText;

            Console.WriteLine("Authenticating via login server...");
            auth.CheckAuth();
            if (string.IsNullOrEmpty(auth.SessionID))
            {
                Console.WriteLine("Could not log in to 4D1 login server, make sure your login details in the config.xml are correct.");
                return -2;
            }

            BackendName = config.SelectSingleNode("//backend/backend-name").InnerText;

            api.Content.Add("login", auth);
            api.Content.Add("iw4m", iw4m);
            api.Content.Add("iw5m", iw5m);
            api.Content.Add("forum", forum);
            api.Content.Add("kmshost", kmshost);
            api.Content.Add("backend-name", BackendName);
            api.ServerLists.Add("iw4m", iw4m.AccessibleMasterServers[0].ServersListed);
            api.ServerLists.Add("iw5m", iw5m.ListedServers);
            api.StatusIndicators.Add("iw4m", new Iw4mStatusIndicator(ref iw4m));
            api.StatusIndicators.Add("iw5m", new Iw5mStatusIndicator(ref iw5m));
            api.StatusIndicators.Add("login", new LoginStatusIndicator(ref auth));
            api.StatusIndicators.Add("forum", new WebStatusIndicator(ref forum));
            api.StatusIndicators.Add("kmshost", new KmshostStatusIndicator(ref kmshost));

            api.Start();

            Console.WriteLine("API server starting, regular checks are now enabled.");

            while (true)
            {
                var task = Task.Factory.StartNew(() => CheckNow(), cancel.Token);
                Thread.Sleep(30 * 1000);
                task.Wait();
                task.Dispose();
            }
        }

        static void CheckNow()
        {
            try
            {
                // I could have done it in an even more gay way, but nvm.
                Task.Factory.StartNew(() => ((FDOAuthServerCheck)api.Content["login"]).CheckAuth());
                Task.Factory.StartNew(() => ((FDOAuthServerCheck)api.Content["login"]).CheckAuthInternal());
                Task.Factory.StartNew(() => ((Iw4mCheck)api.Content["iw4m"]).CheckNP());
                Task.Factory.StartNew(() => ((Iw4mCheck)api.Content["iw4m"]).CheckMaster());
                Task.Factory.StartNew(() => ((Iw5mCheck)api.Content["iw5m"]).CheckNP());
                Task.Factory.StartNew(() => ((Iw5mCheck)api.Content["iw5m"]).CheckMaster());
                Task.Factory.StartNew(() => ((WebCheck)api.Content["forum"]).Check());
                Task.Factory.StartNew(() => ((KmshostCheck)api.Content["kmshost"]).CheckKmshost());
                // Fixes server lists not being refreshed on the API server for now
                // TODO: Make a wrapper for server lists
                api.ServerLists["iw4m"] = iw4m.AccessibleMasterServers[0].ServersListed;
                api.ServerLists["iw5m"] = iw5m.ListedServers;

            }
            catch (OperationCanceledException)
            {
                log.Error("Checks aborted by main thread.");
            }
            catch (Exception err)
            {
                log.Fatal("Regular checks failed", err);
            }
        }
    }
}
