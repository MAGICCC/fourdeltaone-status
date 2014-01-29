using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Fleck;
using Fleck.Handlers;

namespace fdocheck.Server
{
    public class APIServer
    {
        JsonSerializerSettings SerializationSettings = new JsonSerializerSettings();
        WebSocketServer listener = null;
        log4net.ILog log;
        int port = 29001;

        public Dictionary<string, object> Content = new Dictionary<string, object>();
        public Dictionary<string, object> ServerLists = new Dictionary<string, object>();
        public Dictionary<string, BasicStatusIndicator> StatusIndicators = new Dictionary<string, BasicStatusIndicator>();

        public int ClientsConnected { get; set; }

        public APIServer(int port = 29001)
        {
            log = log4net.LogManager.GetLogger(this.GetType());

            SerializationSettings.Converters.Add(new IsoDateTimeConverter());
            SerializationSettings.Converters.Add(new KeyValuePairConverter());
            SerializationSettings.Converters.Add(new StringEnumConverter());

            this.port = port;
        }

        private Task CheckServerForSocket(IWebSocketConnection socket, IPAddress ip, int port)
        {
            Console.WriteLine("Called CheckServerForSocket with {0}:{1}", ip, port);
            var serverConnection = new Q3ServerConnection(ip, port);
            return Task.Factory.StartNew(() =>
            {
                try
                {
                    /*
                    socket.SendJson(new APIResponse(new ServerUpdateResponse()
                    {
                        Status = ServerUpdateStatus.FetchingServerInfo,
                        Data = new
                        {
                            ServerInfo = serverConnection.EndPoint.ToString()
                        }
                    }));
                     */
                    var ping = serverConnection.Ping;
                    if (ping < 0)
                    {
                        socket.SendJson(new APIResponse(new ServerUpdateResponse()
                        {
                            Status = ServerUpdateStatus.TimedOut,
                            Data = new
                            {
                                ServerInfo = serverConnection.EndPoint.ToString()
                            }
                        }));
                        return;
                    }
                    var infoVars = serverConnection.GetStatusDvars();
                    infoVars = infoVars.Concat(serverConnection.GetInfoDvars().Where(x => !infoVars.ContainsKey(x.Key))).ToDictionary(item => item.Key, item => item.Value);
                    var players = serverConnection.GetPlayers();
                    socket.SendJson(new APIResponse(new ServerUpdateResponse()
                    {
                        Status = ServerUpdateStatus.Success,
                        Data = new
                        {
                            ServerInfo = serverConnection.EndPoint.ToString(),
                            Ping = ping,
                            MaxPing = infoVars.GetValueDefault("sv_maxPing"),
                            IsHardcore = infoVars.GetValueDefault("g_hardcore") == "1",
                            HasKillcams = infoVars.GetValueDefault("scr_game_allowkillcam") == "1",
                            IW4M_Secure = infoVars.GetValueDefault("iw4m_secure") == "1",
                            IW4M_RemoteKick = infoVars.GetValueDefault("iw4m_remoteKick") == "1",
                            Name = infoVars.GetValueDefault("sv_hostname"),
                            Gametype = infoVars.GetValueDefault("g_gametype"),
                            Map = infoVars.GetValueDefault("mapname"),
                            Website = infoVars.GetValueDefault("_Website"),
                            Version = infoVars.GetValueDefault("shortversion"),
                            PlayersMax = infoVars.GetValueDefault("sv_maxclients"),
                            PlayersCurrent = players.Count(),
                            Players = players
                        }
                    }));
                }
                catch
                {
                    socket.SendJson(new ServerUpdateResponse()
                    {
                        Status = ServerUpdateStatus.Error,
                        Data = new
                        {
                            ServerInfo = serverConnection.EndPoint.ToString()
                        }
                    });
                }
            });
        }

        public bool Start()
        {
            try
            {
                log.Info("Listener starting...");
                listener = new WebSocketServer(string.Format("{0}://{1}:{2}/", "ws", IPAddress.Any, port));
                listener.Start(socket =>
                {
                    socket.OnOpen = () =>
                    {
                        log.DebugFormat("API client {0} connected", socket.ConnectionInfo.ClientIpAddress);
                        ClientsConnected++;
                    };

                    // Trigger whenever a client disconnects from the API.
                    socket.OnClose = () =>
                    {
                        log.DebugFormat("API client {0} disconnected", socket.ConnectionInfo.ClientIpAddress);
                        ClientsConnected--;
                    };

                    // Triggered when a client requests the API to do something.
                    socket.OnMessage = text =>
                    {
                        try
                        {
                            // Empty? Null?
                            if (string.IsNullOrEmpty(text))
                            {
                                socket.SendJson(new APIResponse(null, true, "No command given."), SerializationSettings);
                                return;
                            }

                            log.DebugFormat("API client {0} sent {1}", socket.ConnectionInfo.ClientIpAddress, text);

                            // Analyze for command name and command arguments
                            var args = text.Split(' ');
                            var cmd = args.First();
                            args = args.Skip(1).ToArray<string>();

                            switch (cmd.ToLower())
                            {
                                #region Actual API commands
                                case "clients":
                                    switch (args.Length)
                                    {
                                        case 0:
                                            socket.SendJson(new APIResponse(ClientsConnected), SerializationSettings);
                                            break;
                                        default:
                                            socket.SendJson(new APIResponse(null, true, string.Format("Invalid argument count: {0}.", args.Length)), SerializationSettings);
                                            break;
                                    }
                                    break;
                                case "clients/?":
                                    socket.SendJson(new APIResponse("Returns amount of clients right now connected to the API."), SerializationSettings);
                                    break;
                                case "cache":
                                    switch (args.Length)
                                    {
                                        case 0:
                                            socket.SendJson(new APIResponse(Content), SerializationSettings);
                                            break;
                                        case 1:
                                            if (!Content.ContainsKey(args[0]))
                                            {
                                                socket.SendJson(new APIResponse(null, true, string.Format("Invalid status indicator ID: {0}. Possible IDs are: {1}.", args[0], string.Join(", ", Content.Keys))), SerializationSettings);
                                                break;
                                            }

                                            socket.SendJson(new APIResponse(Content[args[0]]), SerializationSettings);
                                            break;
                                        default:
                                            socket.SendJson(new APIResponse(null, true, string.Format("Invalid argument count: {0}.", args.Length)), SerializationSettings);
                                            break;
                                    }
                                    break;
                                case "iw4m.getservers/?":
                                    socket.SendJson(new APIResponse("Gives back the full server list including all server data asynchronously."));
                                    break;
                                case "iw4m.getservers":
                                    {
                                        socket.SendJson(new APIResponse(new ServerUpdateResponse() { Status = ServerUpdateStatus.FetchingServersFromMasterServer }));
                                        fdocheck.Checks.MasterServerQuery q = new Checks.MasterServerQuery("FourDeltaOne", "iw4.prod.fourdeltaone.net", 20810, "IW4", 61586);
                                        List<Task> tasks = new List<Task>();
                                        var servers = q.GetServers();

                                        socket.SendJson(new APIResponse(new ServerUpdateResponse()
                                        {
                                            Status = ServerUpdateStatus.ServerListInit,
                                            Data = new
                                            {
                                                ServerInfo = (from s in servers select string.Format("{0}:{1}", s.IP, s.Ports.First())).ToArray()
                                            }
                                        }));
                                        foreach (var server in servers)
                                        {
                                            Console.WriteLine("Adding {0}", server.IP);
                                            tasks.Add(CheckServerForSocket(socket, server.IP, server.Ports.First()));
                                        }

                                        Task.WaitAll(tasks.ToArray(), 12000);
                                        tasks.Clear();
                                    }
                                    break;
                                case "status/?":
                                    socket.SendJson(new APIResponse("Gives information about the status of server interfaces. Can be called with indicator ID or with indicator ID and property ID."), SerializationSettings);
                                    break;
                                case "status":
                                    switch (args.Length)
                                    {
                                        case 0:
                                            socket.SendJson(new APIResponse(StatusIndicators), SerializationSettings);
                                            break;
                                        case 1:
                                            if (!StatusIndicators.ContainsKey(args[0]))
                                            {
                                                socket.SendJson(new APIResponse(null, true, string.Format("Invalid status indicator ID: {0}. Possible IDs are: {1}.", args[0], string.Join(", ", StatusIndicators.Keys))), SerializationSettings);
                                                break;
                                            }

                                            socket.SendJson(new APIResponse(StatusIndicators[args[0]]), SerializationSettings);
                                            break;
                                        case 2:
                                            if (!StatusIndicators.ContainsKey(args[0]))
                                            {
                                                socket.SendJson(new APIResponse(null, true, string.Format("Invalid status indicator ID: {0}. Possible IDs are: {1}.", args[0], string.Join(", ", StatusIndicators.Keys))), SerializationSettings);
                                                break;
                                            }

                                            var cStatusIndicator = StatusIndicators[args[0]];

                                            if (!cStatusIndicator.GetType().GetProperties().Select(p => p.Name).Contains(args[1], StringComparer.OrdinalIgnoreCase))
                                            {
                                                socket.SendJson(new APIResponse(null, true, string.Format("Invalid status indicator property ID: {0}. Possible IDs are: {1}.", args[1], string.Join(", ", cStatusIndicator.GetType().GetProperties().Select(p => p.Name)))), SerializationSettings);
                                                break;
                                            }

                                            socket.SendJson(new APIResponse(cStatusIndicator.GetType().GetProperty(args[1], System.Reflection.BindingFlags.IgnoreCase).GetValue(cStatusIndicator, null)), SerializationSettings);
                                            break;
                                        default:
                                            socket.SendJson(new APIResponse(null, true, string.Format("Invalid argument count: {0}.", args.Length)), SerializationSettings);
                                            break;
                                    }
                                    break;
                                case "servers/?":
                                    socket.SendJson(new APIResponse("Lists all server endpoints found in all master servers or, with argument, from a specific master server."), SerializationSettings);
                                    break;
                                case "servers":
                                    switch (args.Length)
                                    {
                                        case 0:
                                            socket.SendJson(new APIResponse(ServerLists), SerializationSettings);
                                            break;
                                        case 1:
                                            if (!ServerLists.ContainsKey(args[0]))
                                            {
                                                socket.SendJson(new APIResponse(null, true, string.Format("Invalid server list ID: {0}. Possible IDs are: {1}.", args[0], string.Join(", ", ServerLists.Keys))), SerializationSettings);
                                                break;
                                            }

                                            socket.SendJson(new APIResponse(ServerLists[args[0]]), SerializationSettings);
                                            break;
                                        default:
                                            socket.SendJson(new APIResponse(null, true, string.Format("Invalid argument count: {0}.", args.Length)), SerializationSettings);
                                            break;
                                    }
                                    break;
                                default:
                                    socket.SendJson(new APIResponse(null, true, string.Format("Invalid command: {0}.", cmd)), SerializationSettings);
                                    break;
                                #endregion
                            }
                        }
                        catch (Exception error)
                        {
                            try
                            {
                                socket.SendJson(new APIResponse(null, true, "Internal server error"), SerializationSettings);
                            }
                            catch { { } }
                            log.Error("Error in Receive event", error);
                        }
                    };
                });
                log.Info("Listener started.");
                return true;
            }
            catch(Exception err)
            {
                log.Error("Could not start listener for API. The backend won't provide information on any port.", err);
                return false;
            }
        }

        public void Stop()
        {
            log.Info("Listener stopping...");
            listener.Dispose();
            log.Info("Listener stopped.");
        }
    }
}
