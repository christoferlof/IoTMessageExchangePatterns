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
    using Microsoft.SPOT.Presentation.Controls;

    public partial class Program
    {
        #region Fields

        private readonly Queue sendQueue = new Queue();

        private bool isWarning;

        private GT.Timer ledTimer;

        private ReceiverLink receiver;

        private Connection receiverConnection;

        private Session receiverSession;

        private GT.Timer sendTimer;

        private SenderLink sender;

        private Connection senderConnection;

        private Session senderSession;

        private bool shouldWarnOnHarshMove = true;

        private Text statusText;

        #endregion

        #region Methods

        private static void TraceWrite(string format, params object[] args)
        {
            Debug.Print(format);
            foreach (object o in args)
            {
                Debug.Print(" - " + o);
            }
        }

        private Message CreateMessage(string messageType)
        {
            var message = new Message();
            message.ApplicationProperties = new ApplicationProperties();
            message.ApplicationProperties["message-type"] = messageType;
            message.ApplicationProperties["device-id"] = "device001";
            return message;
        }

        private void HandleCommand(Message message)
        {
            if (message.ApplicationProperties["fade"] == null)
            {
                return;
            }

            var fade = (int)message.ApplicationProperties["fade"];
            SetStatus("Received Fade Command " + fade + "s");
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

        private void HandleInquiryResponse(Message message)
        {
            if (message.ApplicationProperties["should-warn"] == null)
            {
                return;
            }

            shouldWarnOnHarshMove = (bool)message.ApplicationProperties["should-warn"];
            SetStatus("Should warn on harsh move ? " + shouldWarnOnHarshMove);
        }

        private void HandleNotification(Message message)
        {
            if (message.ApplicationProperties["text"] == null)
            {
                return;
            }

            SetStatus("Notification: " + message.ApplicationProperties["text"]);
        }

        private void InitializeCommunicationLinks()
        {
            const string Issuer = "[issuer]";
            const string Key = "[key]";
            const string OutEntity = "outbound";
            const string InEntity = "inbound/Subscriptions/device001";

            var address = new Address("[ns].servicebus.windows.net", Issuer, Key);

            SetStatus("Initializing communication links", address.Host);

            senderConnection = new Connection(address);
            senderSession = new Session(senderConnection);
            sender = new SenderLink(senderSession, "send-link" + OutEntity, OutEntity);
            sender.OnClosed += (o, error) => TraceWrite("Send Link Closed", error);

            receiverConnection = new Connection(address);
            receiverSession = new Session(receiverConnection);
            receiver = new ReceiverLink(receiverSession, "receive-link" + InEntity, InEntity);
            receiver.Start(50, OnInboundMessage);
            receiver.OnClosed += (o, error) => TraceWrite("Receive Link Closed", error);

            SetStatus("Communication links initialized");
        }

        private void InitializeDisplay()
        {
            var canvas = new Canvas();

            statusText = new Text(Resources.GetFont(Resources.FontResources.NinaB), "Status message goes here");
            canvas.Children.Add(statusText);

            var qrCode =
                new Image(new Bitmap(Resources.GetBytes(Resources.BinaryResources.qrcode), Bitmap.BitmapImageType.Jpeg));
                //device001
            Canvas.SetTop(qrCode, 20);
            Canvas.SetLeft(qrCode, 10);
            canvas.Children.Add(qrCode);

            display_T35.WPFWindow.Child = canvas;
        }

        private void LedTimerOnTick(GT.Timer timer)
        {
            ledTimer.Stop();
            multicolorLed.TurnOff();
            isWarning = false;
        }

        private void OnButtonPressed(Button o, Button.ButtonState state)
        {
            sendQueue.Enqueue(CreateMessage("inquiry"));
        }

        private void OnInboundMessage(ReceiverLink receiverLink, Message message)
        {
            TraceWrite("Inbound message", message);

            if (message.ApplicationProperties["message-type"] == null)
            {
                return;
            }

            var messageType = (string)message.ApplicationProperties["message-type"];

            if (messageType == "command")
            {
                HandleCommand(message);
            }

            if (messageType == "inquiry-response")
            {
                HandleInquiryResponse(message);
            }

            if (messageType == "notification")
            {
                HandleNotification(message);
            }

            receiverLink.Accept(message);
        }

        private void OnOutboundMessageOutcome(Message message, Outcome outcome, object state)
        {
            TraceWrite("Message Outcome", message.ApplicationProperties["ts"], outcome);
            // todo: handle rejected messages with retrires

            SendNextMessage();
        }

        private void ProgramStarted()
        {
            InitializeDisplay();

            SetStatus("Program starting");

            var sampleTimer = new GT.Timer(1000);
            sampleTimer.Tick += SampleTimerOnTick;
            sampleTimer.Start();

            sendTimer = new GT.Timer(2000);
            sendTimer.Tick += SendTimerOnTick;
            sendTimer.Start();

            ledTimer = new GT.Timer(1000);
            ledTimer.Tick += LedTimerOnTick;

            button.ButtonPressed += OnButtonPressed;

            SetStatus("Program started");
        }

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
                ledTimer.Interval = new TimeSpan(0, 0, 0, 2);
                ledTimer.Start();
            }
        }

        private void SendMessage(Message message)
        {
            TraceWrite("Send", message);
            sender.Send(message, OnOutboundMessageOutcome, null);
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

        private void SendTimerOnTick(GT.Timer timer)
        {
            timer.Stop();

            if (sender == null)
            {
                InitializeCommunicationLinks();
            }

            SendNextMessage();
        }

        private void SetStatus(string statusMessage, params object[] args)
        {
            TraceWrite(statusMessage, args);
            statusText.Dispatcher.Invoke(
                new TimeSpan(0, 0, 1),
                o =>
                    {
                        statusText.TextContent = statusMessage;
                        return null;
                    },
                null);
        }

        #endregion
    }
}