using Microsoft.Azure.NotificationHubs;

namespace PushTest.Models
{
    public class Notifications
    {
        public static Notifications Instance = new Notifications();

        public NotificationHubClient Hub { get; set; }

        private Notifications()
        {
            Hub = NotificationHubClient.CreateClientFromConnectionString("Endpoint=sb://popbookings.servicebus.windows.net/;SharedAccessKeyName=DefaultFullSharedAccessSignature;SharedAccessKey=YMcwcPP8fCefQR/5cjope5OMM39gZr9kY5P5aB1VX3U=", "pb-nh-eastus");
        }
    }
}