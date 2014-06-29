using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleTool
{
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.Remoting.Messaging;

    using Amqp;
    using Amqp.Framing;

    using TraceLevel = Amqp.TraceLevel;

    class Program
    {
        static void Main(string[] args)
        {
            Amqp.Trace.TraceLevel = TraceLevel.Frame | TraceLevel.Verbose;
            Amqp.Trace.TraceListener = TraceWrite;

            var issuer = "[issuer]";
            var key = "[key]";
            var ns = "[namespace]";

            var broker = "amqps://{0}:{1}@{2}.servicebus.windows.net";
            var address = new Address(string.Format(broker, issuer, key, ns));
            
            var connection = new Connection(address);
            var senderSession = new Session(connection);
            var sender = new SenderLink(senderSession, "send-linkt1", "t1");

            for (int i = 0; i < 200; ++i)
            {
                var message = new Message();
                message.Properties = new Properties() { GroupId = "1" };
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
    }
}
