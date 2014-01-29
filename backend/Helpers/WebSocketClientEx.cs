using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fleck;
using Fleck.Handlers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Fleck
{
    static class FleckEx
    {
        public static void SendApiResponse(this IWebSocketConnection c, object data, JsonSerializerSettings serializerSetup = null)
        {
            c.SendJson(data, serializerSetup);
        }

        public static void SendJson(this IWebSocketConnection c, object data, JsonSerializerSettings serializerSetup = null)
        {
            if (serializerSetup == null)
                serializerSetup = new JsonSerializerSettings();
            c.Send(
                JsonConvert.SerializeObject(
                    data,
                    serializerSetup
                )
            );
        }
    }
}
