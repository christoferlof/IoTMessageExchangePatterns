using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;

namespace TelemetryDevice
{
    using System;
    using System.Collections;

    using Amqp;
    using Amqp.Framing;

    using Gadgeteer.Modules.GHIElectronics;

    using Microsoft.SPOT;

    public partial class Program
    {
        private readonly Queue sendQueue = new Queue();

        private Connection senderConnection;
        
        private Connection receiverConnection;

        private SenderLink sender;

        private ReceiverLink receiver;

        private Session senderSession;
        
        private Session receiverSession;

        private GT.Timer ledTimer;

        private static void TraceWrite(string format, params object[] args)
        {
            Debug.Print(format);
            foreach (object o in args)
            {
                Debug.Print(" - " + o);
            }
        }

        private void InitializeCommunicationLinks()
        {
            const string Issuer = "owner";
            const string Key = "[key]";
            const string OutEntity = "outbound";
            const string InEntity = "inbound/Subscriptions/device001";

            var address = new Address("[namespace].servicebus.windows.net", Issuer, Key);

            TraceWrite("Initializing communication links", address.Host);

            senderConnection = new Connection(address);
            senderSession = new Session(senderConnection);
            sender = new SenderLink(senderSession, "send-link" + OutEntity, OutEntity);

            receiverConnection = new Connection(address);            
            receiverSession = new Session(receiverConnection);
            receiver = new ReceiverLink(receiverSession, "receive-link" + InEntity, InEntity);
            receiver.Start(50, OnMessageCallback);
        }

        private void OnMessageCallback(ReceiverLink receiverLink, Message message)
        {
            if (message.ApplicationProperties["fade"] != null)
            {
                int fade = (int)message.ApplicationProperties["fade"];
                var timeSpan = new TimeSpan(0, 0, 0, fade);
                multicolorLed.FadeOnce(GT.Color.Cyan, timeSpan,GT.Color.Green);
                
                //turn of led
                ledTimer.Interval = timeSpan;
                ledTimer.Start();

                receiverLink.Accept(message);

                // send response
                Message response = CreateMessage("command-response");
                response.ApplicationProperties["status"] = "ok";
                response.ApplicationProperties["receiptient"] = message.ApplicationProperties["respondTo"];
                SendMessage(response);
            }
        }

        private void MessageOutcomeCallback(Message message, Outcome outcome, object state)
        {
            TraceWrite("Message Outcome", message.ApplicationProperties["ts"], outcome);
        }

        private void ProgramStarted()
        {
            TraceWrite("Program starting");

            var sampleTimer = new GT.Timer(500);
            sampleTimer.Tick += SampleTimerOnTick;
            sampleTimer.Start();

            var sendTimer = new GT.Timer(2000);
            sendTimer.Tick += SendTimerOnTick;
            sendTimer.Start();

            ledTimer = new GT.Timer(1000);
            ledTimer.Tick += LedTimerOnTick;

            TraceWrite("Program started");
        }

        private void LedTimerOnTick(GT.Timer timer)
        {
            ledTimer.Stop();
            multicolorLed.TurnOff();
        }

        private void SampleTimerOnTick(GT.Timer timer)
        {
            Joystick.Position position = joystick.GetPosition();

            TraceWrite("enqueue ", position.X, position.Y);
            sendQueue.Enqueue(new PositionSample { X = position.X, Y = position.Y, Ts = DateTime.UtcNow.Ticks });
        }

        private void SendMessage(Message message)
        {
            TraceWrite("Send", message.ApplicationProperties);
            sender.Send(message, MessageOutcomeCallback, null);
        }

        private Message CreateMessage(string messageType)
        {
            var message = new Message();
            message.ApplicationProperties = new ApplicationProperties();
            message.ApplicationProperties["message-type"] = messageType;
            return message;
        }

        private void SendTimerOnTick(GT.Timer timer)
        {
            timer.Stop();

            if (sender == null)
            {
                InitializeCommunicationLinks();
            }

            while (sendQueue.Count > 0)
            {
                var position = (PositionSample)sendQueue.Dequeue();
                TraceWrite("dequeue ", position.X, position.Y, sendQueue.Count);

                Message message = CreateMessage("telemetry");
                message.ApplicationProperties["x"] = position.X;
                message.ApplicationProperties["y"] = position.Y;
                message.ApplicationProperties["ts"] = position.Ts;
                SendMessage(message);
            }

            timer.Start();
        }

        public struct PositionSample
        {
            public long Ts { get; set; }

            public double X { get; set; }

            public double Y { get; set; }
        }
    }
}