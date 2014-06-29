namespace TelemetryWebApp.Hubs
{
    using System;
    using System.Configuration;
    using System.Web;

    using Microsoft.AspNet.SignalR;
    using Microsoft.ServiceBus.Messaging;

    public class TelemetryHub : Hub
    {
        private MessageReceiver receiver;

        public TelemetryHub()
        {
            var factory = MessagingFactory.CreateFromConnectionString(
                ConfigurationManager.AppSettings["sb:connectionString"]);

            receiver = factory.CreateMessageReceiver(
                ConfigurationManager.AppSettings["sb:entityPath"],
                ReceiveMode.ReceiveAndDelete);

            receiver.OnMessage(OnMessage, new OnMessageOptions { AutoComplete = true });
        }

        private void OnMessage(BrokeredMessage message)
        {
            if (!message.Properties.ContainsKey("ts"))
            {
                return;
            }

            var context = GlobalHost.ConnectionManager.GetHubContext<TelemetryHub>();
            //var timeStamp = new DateTime((long)message.Properties["ts"]).ToLongTimeString();
            var epoch = (long)message.Properties["ts"] / 10000; //device is at year 0
            context.Clients.All.onPositionMessage(epoch, message.Properties["x"], message.Properties["y"]);
        }

    }
}