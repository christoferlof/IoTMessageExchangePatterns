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

        private Connection connection;

        private SenderLink sender;

        private Session senderSession;

        private static void TraceWrite(string format, params object[] args)
        {
            Debug.Print(format);
            foreach (object o in args)
            {
                Debug.Print(" - " + o);
            }
        }

        private void InitializeSendLink()
        {
            const string Issuer = "[issuer]";
            const string Key = "[key]";

            var address = new Address("[namespace].servicebus.windows.net", Issuer, Key);

            TraceWrite("Initializing Send Link", address.Password);

            connection = new Connection(address);
            senderSession = new Session(connection);
            sender = new SenderLink(senderSession, "send-linkt1", "t1");
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

            TraceWrite("Program started");
        }

        private void SampleTimerOnTick(GT.Timer timer)
        {
            Joystick.Position position = joystick.GetPosition();

            TraceWrite("enqueue ", position.X, position.Y);
            sendQueue.Enqueue(new PositionSample { X = position.X, Y = position.Y, Ts = DateTime.UtcNow.Ticks });
        }

        private void SendMessage(PositionSample position)
        {
            var message = new Message();
            message.Properties = new Properties { GroupId = "1" };
            message.ApplicationProperties = new ApplicationProperties();
            message.ApplicationProperties["x"] = position.X;
            message.ApplicationProperties["y"] = position.Y;
            message.ApplicationProperties["ts"] = position.Ts;

            TraceWrite("Send", message.ApplicationProperties["ts"]);
            sender.Send(message, MessageOutcomeCallback, null);
        }

        private void SendTimerOnTick(GT.Timer timer)
        {
            timer.Stop();

            if (sender == null)
            {
                InitializeSendLink();
            }

            while (sendQueue.Count > 0)
            {
                var position = (PositionSample)sendQueue.Dequeue();
                TraceWrite("dequeue ", position.X, position.Y, sendQueue.Count);

                SendMessage(position);
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