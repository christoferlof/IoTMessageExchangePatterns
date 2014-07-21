using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;

namespace TelemetryDevice
{
    using System;
    using System.Collections;
    using System.Threading;

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

        private bool shouldWarnOnHarshMove = true;

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
            sender.OnClosed += (o, error) => TraceWrite("Send Link Closed", error);

            receiverConnection = new Connection(address);
            receiverSession = new Session(receiverConnection);
            receiver = new ReceiverLink(receiverSession, "receive-link" + InEntity, InEntity);
            receiver.Start(50, OnInboundMessage);
            receiver.OnClosed += (o, error) => TraceWrite("Receive Link Closed", error);

        }

        private void OnInboundMessage(ReceiverLink receiverLink, Message message)
        {
            TraceWrite("inbound message", message.ApplicationProperties);

            if (message.ApplicationProperties["message-type"] == null)
            {
                return;
            }

            string messageType = (string)message.ApplicationProperties["message-type"];
            
            if (messageType == "command")
            {
                HandleCommand(message);    
            }

            if (messageType == "inquiry-response")
            {
                HandleInquiryResponse(message);
            }

            receiverLink.Accept(message);
        }

        private void HandleInquiryResponse(Message message)
        {
            if (message.ApplicationProperties["should-warn"] == null)
            {
                return;
            }

            shouldWarnOnHarshMove = (bool)message.ApplicationProperties["should-warn"];
        }

        private void HandleCommand(Message message)
        {
            if (message.ApplicationProperties["fade"] == null)
            {
                return;
            }

            int fade = (int)message.ApplicationProperties["fade"];
            var timeSpan = new TimeSpan(0, 0, 0, fade);
            multicolorLed.FadeOnce(GT.Color.Cyan, timeSpan, GT.Color.Green);

            //turn of led
            ledTimer.Interval = timeSpan;
            ledTimer.Start();

            // send response
            Message response = CreateMessage("command-response");
            response.ApplicationProperties["status"] = "ok";
            response.ApplicationProperties["receiptient"] = message.ApplicationProperties["respondTo"];
            SendMessage(response);
        }

        private void OnOutboundMessageOutcome(Message message, Outcome outcome, object state)
        {
            TraceWrite("Message Outcome", message.ApplicationProperties["ts"], outcome);
            // todo: handle rejected messages with retrires
            
            SendNextMessage();
        }

        private void ProgramStarted()
        {
            TraceWrite("Program starting");

            var sampleTimer = new GT.Timer(1000);
            sampleTimer.Tick += SampleTimerOnTick;
            sampleTimer.Start();

            sendTimer = new GT.Timer(2000);
            sendTimer.Tick += SendTimerOnTick;
            sendTimer.Start();

            ledTimer = new GT.Timer(1000);
            ledTimer.Tick += LedTimerOnTick;

            button.ButtonPressed += OnButtonPressed;

            TraceWrite("Program started");
        }

        private void OnButtonPressed(Button o, Button.ButtonState state)
        {
            sendQueue.Enqueue(CreateMessage("inquiry"));
        }

        private void LedTimerOnTick(GT.Timer timer)
        {
            ledTimer.Stop();
            multicolorLed.TurnOff();
            isWarning = false;
        }

        private bool isWarning;

        private GT.Timer sendTimer;

        private void SampleTimerOnTick(GT.Timer timer)
        {
            Joystick.Position position = joystick.GetPosition();
            Message message = CreateMessage("telemetry");
            message.ApplicationProperties["x"] = position.X;
            message.ApplicationProperties["y"] = position.Y;
            message.ApplicationProperties["ts"] = DateTime.UtcNow.Ticks;
            sendQueue.Enqueue(message);

            if (shouldWarnOnHarshMove && (position.X > 0.9 || position.Y > 0.9))
            {
                if (isWarning)
                {
                    return;
                }

                //harsh turn, acceleration on braking
                isWarning = true;
                var pulse = new TimeSpan(0, 0, 0, 0, 250);
                multicolorLed.BlinkRepeatedly(GT.Color.Black, pulse, GT.Color.Red, pulse);

                //turn of led in 2sec
                ledTimer.Interval = new TimeSpan(0,0,0,2);
                ledTimer.Start();
            }
        }

        private void SendMessage(Message message)
        {
            TraceWrite("Send", message.ApplicationProperties);
            sender.Send(message, OnOutboundMessageOutcome, null);
        }

        private Message CreateMessage(string messageType)
        {
            var message = new Message();
            message.ApplicationProperties = new ApplicationProperties();
            message.ApplicationProperties["message-type"] = messageType;
            message.ApplicationProperties["device-id"] = "device001";
            return message;
        }

        private void SendTimerOnTick(GT.Timer timer)
        {
            timer.Stop();

            if (sender == null)
            {
                InitializeCommunicationLinks();
            }

            SendNextMessage();
        }

        private void SendNextMessage()
        {
            if (sendQueue.Count == 0)
            {
                sendTimer.Start();
                return;
            }

            SendMessage((Message)sendQueue.Dequeue());
        }
    }
}