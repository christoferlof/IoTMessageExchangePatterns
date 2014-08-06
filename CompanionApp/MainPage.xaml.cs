namespace CompanionApp
{
    using System.Diagnostics;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Navigation;

    using Microsoft.AspNet.SignalR.Client;
    using Microsoft.Phone.Controls;
    using Microsoft.Phone.Tasks;

    using OpticalReaderLib;

    public partial class MainPage : PhoneApplicationPage
    {

        private string deviceId;

        private OpticalReaderResult opticalReaderResult;

        private HubConnection connection;

        private IHubProxy hubProxy;

        public MainPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            HandleQrResponse();
        }

        private void HandleQrResponse()
        {
            if (opticalReaderResult != null && opticalReaderResult.TaskResult == TaskResult.OK)
            {
                deviceId = opticalReaderResult.Text;
                DeviceIdTextBlock.Text = "Device Id: " + deviceId;
                FadeLedButton.IsEnabled = true;
            }

            opticalReaderResult = null;
        }

        private async Task EnsureCommunicationLink()
        {
            if (connection == null)
            {
                connection = new HubConnection("http://localhost:3081/");
                hubProxy = connection.CreateHubProxy("TelemetryHub");
                await connection.Start();
            }
        }

        private void OnButtonClick(object sender, RoutedEventArgs e)
        {
            var opticalReaderTask = new OpticalReaderTask();
            opticalReaderTask.Completed += (o, result) => opticalReaderResult = result;
            opticalReaderTask.Show();
        }

        private async void OnFadeLedButtonClick(object o, RoutedEventArgs e)
        {
            await EnsureCommunicationLink();

            await SendCommand();
        }

        private async Task SendCommand()
        {
            await hubProxy.Invoke("SendCommand", 5 /* led fade duration */ , "My Companion App Id" /* respond to */);
        }
    }
}