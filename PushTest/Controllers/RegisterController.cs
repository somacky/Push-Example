using Microsoft.Azure.NotificationHubs;
using Microsoft.Azure.NotificationHubs.Messaging;
using PushTest.Models;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Net;
using System.Linq;
using System.Net.Http;
using System.Collections.Generic;
using Newtonsoft.Json;



namespace PushTest.Controllers
{

    public class RegisterController : ApiController
    {

        private NotificationHubClient hub;

        public RegisterController()
        {
            hub = Notifications.Instance.Hub;
        }

        public class DeviceRegistration
        {
            public string Platform { get; set; }
            public string Handle { get; set; }
            public string[] Tags { get; set; }
        }

        public class HandleRequest
        {
            public string Handle { get; set; }
        }

        // POST api/register
        // This creates a registration id
        [HttpPost]
        [ActionName("Run")]
        public static async Task<HttpResponseMessage> Run(HttpRequestMessage req)
        {
            //log.Info("C# HTTP trigger function processed a request.");
            string name = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "name", true) == 0)
                .Value;
            var hub = NotificationHubClient.CreateClientFromConnectionString("Endpoint=sb://popbookings.servicebus.windows.net/;SharedAccessKeyName=DefaultFullSharedAccessSignature;SharedAccessKey=YMcwcPP8fCefQR/5cjope5OMM39gZr9kY5P5aB1VX3U=", "pb-nh-eastus");
            string newRegistrationId = null;
            if (name != "" && name != null)
            {

                var registrations = await hub.GetRegistrationsByChannelAsync(name, 100);
            }
            if (newRegistrationId == null)
                newRegistrationId = await hub.CreateRegistrationIdAsync();
           

            return name == null
                ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
                //: req.CreateResponse(HttpStatusCode.OK, "Id: " + newRegistrationId);
                : req.CreateResponse(HttpStatusCode.OK, "name: " + name);
        }


        public async Task<HttpResponseMessage> Post( HttpRequestMessage req)
        {
            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);           
            //log.Info("C# HTTP trigger function processed a request.");
            string name = data.name;
            var hub = NotificationHubClient.CreateClientFromConnectionString("Endpoint=sb://popbookings.servicebus.windows.net/;SharedAccessKeyName=DefaultFullSharedAccessSignature;SharedAccessKey=YMcwcPP8fCefQR/5cjope5OMM39gZr9kY5P5aB1VX3U=", "pb-nh-eastus");
            string newRegistrationId = null;
            if (!string.IsNullOrEmpty(name))
            {

                var registrations = await hub.GetRegistrationsByChannelAsync(name, 100);
                foreach (RegistrationDescription registration in registrations)
                {
                    if (newRegistrationId == null)
                    {
                        newRegistrationId = registration.RegistrationId;
                    }
                    else
                    {
                        await hub.DeleteRegistrationAsync(registration);
                    }
                }
            }
            if (newRegistrationId == null)
                newRegistrationId = await hub.CreateRegistrationIdAsync();

            // parse query parameter


            // Get request body
            //dynamic data = await req.Content.ReadAsAsync<object>();

            // Set name to query string or body data
            //name = name ?? data?.name;

            return name == null
                ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
                //: req.CreateResponse(HttpStatusCode.OK, "Id: " + newRegistrationId);
                : req.CreateResponse(HttpStatusCode.OK, "Id: " + newRegistrationId);
            /*string newRegistrationId = null;

            // make sure there are no existing registrations for this push handle (used for iOS and Android)
            if (request != null)
            {
                var registrations = await hub.GetRegistrationsByChannelAsync(request.Handle, 100);

                foreach (RegistrationDescription registration in registrations)
                {
                    if (newRegistrationId == null)
                    {
                        newRegistrationId = registration.RegistrationId;
                    }
                    else
                    {
                        await hub.DeleteRegistrationAsync(registration);
                    }
                }
            }

            if (newRegistrationId == null)
                newRegistrationId = await hub.CreateRegistrationIdAsync();

            return newRegistrationId;*/
        }

        // PUT api/register/5
        // This creates or updates a registration (with provided channelURI) at the specified id

        //there are two ways be registered in the notifications hub 
        //1 is the device directly make the connection
        //2 the Back-end connects the device
        public async Task<HttpResponseMessage> Put(HttpRequestMessage req)
        {            
            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);
            RegistrationDescription registration = null;
            string platform = data.platform;
            //handle is the channel uri for the connection with the device, there are different code lines for each OS
            //Ex. var handle = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync(); for Windows app
            string handle = data.handle;
            switch (platform)
            {
                case "mpns":
                    registration = new MpnsRegistrationDescription(handle);
                    break;
                case "wns":
                    registration = new WindowsRegistrationDescription(handle);
                    break;
                case "apns":
                    registration = new AppleRegistrationDescription(handle);
                    break;
                case "gcm":
                    registration = new GcmRegistrationDescription(handle);
                    break;
                default:
                    throw new HttpResponseException(HttpStatusCode.BadRequest);
            }
            string id = data.id;
            registration.RegistrationId = id;
            string username = data.username;
            string[] tags = data.tags;
            // add check if user is allowed to add these tags
            registration.Tags = new HashSet<string>(tags);
            //username can be repeated many times due to the devices he has
            registration.Tags.Add("username:" + username);

            try
            {
                await hub.CreateOrUpdateRegistrationAsync(registration);
            }
            catch (MessagingException e)
            {
                ReturnGoneIfHubResponseIsGone(e);
            }

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        // DELETE api/register/5
        public async Task<HttpResponseMessage> Delete(string id)
        {
            await hub.DeleteRegistrationAsync(id);
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        private static void ReturnGoneIfHubResponseIsGone(MessagingException e)
        {
            var webex = e.InnerException as WebException;
            if (webex.Status == WebExceptionStatus.ProtocolError)
            {
                var response = (HttpWebResponse)webex.Response;
                if (response.StatusCode == HttpStatusCode.Gone)
                    throw new HttpRequestException(HttpStatusCode.Gone.ToString());
            }
        }
    }

}
