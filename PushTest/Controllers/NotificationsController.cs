using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.Azure.NotificationHubs;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace PushTest.Controllers
{
    public class NotificationsController : ApiController
    {
        public class Notifications
        {
            public static Notifications Instance = new Notifications();

            public NotificationHubClient Hub { get; set; }

            private Notifications()
            {
                Hub = NotificationHubClient.CreateClientFromConnectionString ("Endpoint=sb://popbookings.servicebus.windows.net/;SharedAccessKeyName=DefaultFullSharedAccessSignature;SharedAccessKey=YMcwcPP8fCefQR/5cjope5OMM39gZr9kY5P5aB1VX3U=",
                                                                             "pb-nh-eastus");
            }
        }
        
        public async Task<HttpResponseMessage> Post(HttpRequestMessage req)
        {
            var hub = NotificationHubClient.CreateClientFromConnectionString("Endpoint=sb://popbookings.servicebus.windows.net/;SharedAccessKeyName=DefaultFullSharedAccessSignature;SharedAccessKey=YMcwcPP8fCefQR/5cjope5OMM39gZr9kY5P5aB1VX3U=","pb-nh-eastus");
            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);            
            string[] userTag = new string[2];
            userTag[0] = "username:" + data.toUser;
            userTag[1] = "from:" + data.fromUser;

            var pns = data.pns;
            var fromUser = data.fromUser;
            var message = data.message;
            NotificationOutcome outcome = null;
            HttpStatusCode ret = HttpStatusCode.InternalServerError;

            switch (pns.ToLower())
            {
                case "wns":
                    // Windows 8.1 / Windows Phone 8.1
                    var toast = @"<toast><visual><binding template=""ToastText01""><text id=""1"">" +
                                "From " + fromUser + ": " + message + "</text></binding></visual></toast>";
                    outcome = await hub.SendWindowsNativeNotificationAsync(toast, userTag);
                    break;
                case "apns":
                    // iOS
                    var alert = "{\"aps\":{\"alert\":\"" + "From " + fromUser + ": " + message + "\"}}";
                    outcome = await hub.SendAppleNativeNotificationAsync(alert, userTag);
                    break;
                case "gcm":
                    // Android
                    var notif = "{ \"data\" : {\"message\":\"" + "From " + fromUser + ": " + message + "\"}}";
                    outcome = await hub.SendGcmNativeNotificationAsync(notif, userTag);
                    break;
            }

            if (outcome != null)
            {
                if (!((outcome.State == NotificationOutcomeState.Abandoned) ||
                    (outcome.State == NotificationOutcomeState.Unknown)))
                {
                    ret = HttpStatusCode.OK;
                }
            }

            return Request.CreateResponse(ret);
        }
    }
}
