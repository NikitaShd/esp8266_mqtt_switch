using Microsoft.Maui.Controls;
using System.Linq;
using MQTTnet;
using MQTTnet.Client;
using System;
using System.Threading.Tasks;
using MQTTnet.Exceptions;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Server;
using System.Net.NetworkInformation;

namespace MQTT_Client;

public partial class MainPage : ContentPage
{
    public class DevRelai
    {
        public string naim
        {
            get { return lbl.Text; }
            set { lbl.Text = value; }
        }
        public bool stait
        {
            get { return sv.IsToggled; }
            set { sv.IsToggled = value; }
        }

        private Frame core;

        private Switch sv;
        private Label lbl;
        private Label lbl2;
        private Image img;
        public DevRelai(string temp)
        {
            core = new Frame();
            core.Margin = 5;
            core.Padding = 10;
            core.MaximumHeightRequest = 200;
            core.MaximumWidthRequest = 200;
            core.MinimumHeightRequest = 50;
            core.MinimumWidthRequest = 50;

            sv = new Switch();
            lbl = new Label();
            lbl2 = new Label();
            img = new Image();
            naim = temp;
            lbl2.Text = "00:1B:44:11:3A:B7";

            lbl.ZIndex = 1;
            lbl.HorizontalOptions = LayoutOptions.Center;
            AbsoluteLayout.SetLayoutBounds(lbl, new Rect(0, 0, 100, 50));
            AbsoluteLayout.SetLayoutFlags(lbl, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.PositionProportional);

            lbl2.ZIndex = 1;
            lbl2.HorizontalOptions = LayoutOptions.Center;
            lbl2.FontSize = 10;
            AbsoluteLayout.SetLayoutBounds(lbl2, new Rect(1, 4, 100, 59));
            AbsoluteLayout.SetLayoutFlags(lbl2,Microsoft.Maui.Layouts.AbsoluteLayoutFlags.PositionProportional);

            sv.ZIndex = 1;
            sv.HorizontalOptions = LayoutOptions.Center;
            AbsoluteLayout.SetLayoutBounds(sv, new Rect(4, 2, 100, 52));
            AbsoluteLayout.SetLayoutFlags(sv, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.PositionProportional);

            img.ZIndex = 0;
            img.Source = "method_draw_image.png";
            img.Opacity = 0.9;
            AbsoluteLayout.SetLayoutBounds(img, new Rect(4, 2, 100, 52));

            AbsoluteLayout st = new AbsoluteLayout();
            core.Content = st;
            st.Add(lbl);
            st.Add(lbl2);
            st.Add(sv);
            st.Add(img);
        }
        public IView Get_IView() {
            return core;
        }
    }

    static public string GetMACAddress()
    {
        NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
        String sMacAddress = string.Empty;
        foreach (NetworkInterface adapter in nics)
        {
            if (sMacAddress == String.Empty)// only return MAC Address from first card  
            {
                IPInterfaceProperties properties = adapter.GetIPProperties();
                sMacAddress = adapter.GetPhysicalAddress().ToString();
            }
        }
        return sMacAddress;
    }

    List<DevRelai> Conect_Devs = new List<DevRelai> { };

    static string clientid = "Client/" + GetMACAddress();
    const string server = "www.MQTT_Broker_Esp8266.local";
    const int port = 1883;

    IManagedMqttClient _mqttClient = new MqttFactory().CreateManagedMqttClient();

    // Create client options object
   static MqttClientOptionsBuilder builder = new MqttClientOptionsBuilder()
                                            .WithClientId(clientid)
                                            .WithTcpServer(server,port);

    ManagedMqttClientOptions options = new ManagedMqttClientOptionsBuilder()
                            .WithAutoReconnectDelay(TimeSpan.FromSeconds(60))
                            .WithClientOptions(builder.Build())
                            .Build();



   

   




    public MainPage()
	{
		InitializeComponent();
        _mqttClient.ConnectedAsync += _mqttClient_ConnectedAsync;
        _mqttClient.ConnectingFailedAsync += _mqttClient_ConnectingFailedAsync;
        _mqttClient.DisconnectedAsync += _mqttClient_DisconnectedAsync;

        Application.Current.UserAppTheme = AppTheme.Light;
        for (int i = 0; i < 10; i++)
        {
            DevRelai temp = new DevRelai(i.ToString());
            Conect_Devs.Add(temp);
            FlexContainer.Children.Add(Conect_Devs[i].Get_IView());
            // FlexContainer.Children.Add(temp.core);
        }

       
        
    }
    int f = 0;
    private async void Button_Clicked(object sender, EventArgs e)
    {
        ConectButton.IsEnabled = false;
        await _mqttClient.StartAsync(options);
       
        
    }

     Task _mqttClient_ConnectedAsync(MqttClientConnectedEventArgs arg)
    {
       
        return Task.CompletedTask;
    }

    Task _mqttClient_ConnectingFailedAsync(ConnectingFailedEventArgs arg)
    {
       
        Dispatcher.Dispatch(() =>
        {
            ConectButton.IsEnabled = true;
           
        });
      
        return Task.CompletedTask;
    }

    Task _mqttClient_DisconnectedAsync(MqttClientDisconnectedEventArgs arg)
    {
        Dispatcher.Dispatch(() =>
        {
            ConectButton.IsEnabled = true;
          
        });

        return Task.CompletedTask;
    }
}

