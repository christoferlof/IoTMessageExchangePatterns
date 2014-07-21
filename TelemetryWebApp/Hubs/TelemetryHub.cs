namespace TelemetryWebApp.Hubs
{
    using System;
    using System.Configuration;

    using Microsoft.AspNet.SignalR;
    using Microsoft.ServiceBus.Messaging;

    public class TelemetryHub : Hub
    {
        private MessageReceiver telemetryReceiver;

        private TopicClient commandClient;

        public TelemetryHub()
        {
            var factory = MessagingFactory.CreateFromConnectionString(
                ConfigurationManager.AppSettings["sb:connectionString"]);

            telemetryReceiver = factory.CreateMessageReceiver(
                ConfigurationManager.AppSettings["sb:outboundEntityPath"],
                ReceiveMode.ReceiveAndDelete);

            telemetryReceiver.OnMessage(OnMessage, new OnMessageOptions { AutoComplete = true });
            commandClient = factory.CreateTopicClient(ConfigurationManager.AppSettings["sb:inboundEntityPath"]);
        }

        private void OnMessage(BrokeredMessage message)
        {
            if (!message.Properties.ContainsKey("message-type"))
            {
                return;
            }

            string messageType = (string)message.Properties["message-type"];
            IHubContext context = GlobalHost.ConnectionManager.GetHubContext<TelemetryHub>();

            if (messageType == "telemetry")
            {
                var epoch = (long)message.Properties["ts"] / 10000;
                context.Clients.All.OnPositionMessage(
                    epoch, 
                    message.Properties["x"], 
                    message.Properties["y"]);
            }

            if (messageType == "command-response")
            {
                context.Clients.All.OnCommandResponseMessage(
                    message.Properties["status"],
                    message.Properties["receiptient"]);
            }

            if (messageType == "inquiry")
            {
                context.Clients.All.OnInquiry(message.Properties["device-id"]);
            }
        }

        public void SendCommand(int fadeDuration, string respondTo)
        {
            var message = new BrokeredMessage();
            message.TimeToLive = new TimeSpan(0,0,0,30);
            message.Properties.Add("message-type", "command");
            message.Properties.Add("fade", fadeDuration);
            message.Properties.Add("respondTo", respondTo);
            commandClient.Send(message);
        }

        public void SendInquiryResponse(bool shouldWarn)
        {
            var message = new BrokeredMessage();
            message.TimeToLive = new TimeSpan(0, 0, 0, 30);
            message.Properties.Add("message-type", "inquiry-response");
            message.Properties.Add("should-warn", shouldWarn);
            commandClient.Send(message);
            
        }

        public void SendNotification(string notification)
        {
            var message = new BrokeredMessage();
            message.Properties.Add("message-type", "notification");
            message.Properties.Add("text", notification);
            commandClient.Send(message);
        }

    }
}