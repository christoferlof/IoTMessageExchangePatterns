namespace CompanionApp
{
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Navigation;

    using Amqp;
    using Amqp.Framing;

    using Microsoft.Phone.Controls;
    using Microsoft.Phone.Info;
    using Microsoft.Phone.Tasks;

    using OpticalReaderLib;

    public partial class MainPage : PhoneApplicationPage
    {
        private Connection connection;

        private string deviceId;

        private OpticalReaderResult opticalReaderResult;

        private SenderLink sender;

        private Session session;

        public MainPage()
        {
            InitializeComponent();

            Trace.TraceLevel = TraceLevel.Frame | TraceLevel.Verbose;
            Trace.TraceListener = TraceWrite;
        }

        
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (opticalReaderResult != null && opticalReaderResult.TaskResult == TaskResult.OK)
            {
                deviceId = opticalReaderResult.Text;
                DeviceIdTextBlock.Text = "Device Id: " + deviceId;
                FadeLedButton.IsEnabled = true;
            }

            opticalReaderResult = null;
        }

        private void InitializeCommunicationLink()
        {
            if (sender != null)
            {
                return;
            }

            const string Issuer = "[issuer]";
            const string Key = "[key]";
            const string Entity = "inbound";

            var address = new Address("chrislofsb.servicebus.windows.net", Issuer, Key);

            connection = new Connection(address);
            session = new Session(connection);
            sender = new SenderLink(session, "send-link" + Entity, Entity);
            sender.OnClosed += (o, error) => Debug.WriteLine("SenderLink closed: {0}", error);
        }

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            var opticalReaderTask = new OpticalReaderTask();
            opticalReaderTask.Completed += (o, result) => opticalReaderResult = result;
            opticalReaderTask.Show();
        }

        private void OnFadeLedButtonClick(object o, RoutedEventArgs e)
        {
            InitializeCommunicationLink();

            SendCommand();
        }

        private void OnMessageOutcome(Message message, Outcome outcome, object state)
        {
            Debug.WriteLine("message oucome - {0}", outcome);
        }

        private void SendCommand()
        {
            var message = new Message();
            message.Properties = new Properties();
            message.Properties.To = deviceId;
            message.ApplicationProperties = new ApplicationProperties();
            message.ApplicationProperties["message-type"] = "command";
            message.ApplicationProperties["fade"] = 3;
            message.ApplicationProperties["respondTo"] = DeviceStatus.DeviceName;
            //just get something for now.. 
            sender.Send(message, OnMessageOutcome, null);
        }

        private void TraceWrite(string format, object[] args)
        {
            Debug.WriteLine(format, args);
        }
    }
}