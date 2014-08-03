namespace ConsoleTool
{
    using System;
    using System.Diagnostics;

    using Amqp;
    using Amqp.Framing;

    using Trace = Amqp.Trace;
    using TraceLevel = Amqp.TraceLevel;

    internal class Program
    {
        #region Methods

        private static void Main(string[] args)
        {
            Trace.TraceLevel = TraceLevel.Frame | TraceLevel.Verbose;
            Trace.TraceListener = TraceWrite;

            string issuer = "[issuer]";
            string key = "[key]";
            string ns = "[namespace]";

            string broker = "amqps://{0}:{1}@{2}.servicebus.windows.net";
            var address = new Address(string.Format(broker, issuer, key, ns));

            var connection = new Connection(address);
            var senderSession = new Session(connection);
            var sender = new SenderLink(senderSession, "send-linkt1", "t1");

            for (int i = 0; i < 200; ++i)
            {
                var message = new Message();
                message.Properties = new Properties { GroupId = "1" };
                message.ApplicationProperties = new ApplicationProperties();
                message.ApplicationProperties["sn"] = i;
                TraceWrite("send: {0}", message.ApplicationProperties["sn"]);
                sender.Send(message, MessageOutComeCallback, null);
            }

            Console.Read();

            //var receiverSession = new Session(connection);
            //var receiver = new ReceiverLink(receiverSession, "receive-linkt1", "t1/Subscriptions/s1");
            //for (int i = 0; i < 200; ++i)
            //{
            //    if (i % 50 == 0) receiver.SetCredit(50);
            //    Message message = receiver.Receive();
            //    Console.WriteLine("receive: {0}", message.ApplicationProperties["sn"]);
            //    receiver.Accept(message);

            //}

            sender.Close();
            senderSession.Close();

            //receiver.Close();
            //receiverSession.Close();

            connection.Close();
        }

        private static void MessageOutComeCallback(Message message, Outcome outcome, object state)
        {
            TraceWrite("Accepted ?: {1} - {0}", outcome is Accepted, message.ApplicationProperties["sn"]);
        }

        private static void TraceWrite(string format, params object[] args)
        {
            Console.WriteLine(format, args);
            Debug.WriteLine(format, args);
        }

        #endregion
    }
}