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
using uPLibrary.Networking.M2Mqtt;
using System.Net;
using uPLibrary.Networking.M2Mqtt.Messages;


namespace MQTT_Client;

public partial class MainPage : ContentPage
{
    public class DevRelai
    {
        IDispatcher Dispatcher;
        public string naim
        {
            get { return "Dev/Relay/" + lbl2.Text; }
            set { lbl2.Text = value.Remove(0,10); }
        }
        public bool stait
        {
            get { return sv.IsToggled; }
            set {
                Dispatcher.Dispatch(() =>
                { sv.IsToggled = value; });
            }
        }
        public string label
        {
            get { return lbl.Text; }
            set {
                Dispatcher.Dispatch(() =>
                { lbl.Text = value; }); 
            }
        }
        private Frame core;

        private Switch sv;
        private Label lbl;
        private Label lbl2;
        private Image img;
        uPLibrary.Networking.M2Mqtt.MqttClient client;
        public DevRelai(string temp, uPLibrary.Networking.M2Mqtt.MqttClient clientinf,IDispatcher DispatcherS)
        {
            Dispatcher = DispatcherS;
            client = clientinf;
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
            //lbl2.Text = "00:1B:44:11:3A:B7";

            lbl.ZIndex = 1;
            lbl.HorizontalOptions = LayoutOptions.Center;
            AbsoluteLayout.SetLayoutBounds(lbl, new Rect(0, 0, 100, 50));
            AbsoluteLayout.SetLayoutFlags(lbl, Microsoft.Maui.Layouts.AbsoluteLayoutFlags.PositionProportional);

            lbl2.ZIndex = 1;
            lbl2.HorizontalOptions = LayoutOptions.Center;
            lbl2.FontSize = 9;
            AbsoluteLayout.SetLayoutBounds(lbl2, new Rect(1, 4, 100, 59));
            AbsoluteLayout.SetLayoutFlags(lbl2,Microsoft.Maui.Layouts.AbsoluteLayoutFlags.PositionProportional);

            sv.ZIndex = 1;
            sv.HorizontalOptions = LayoutOptions.Center;
            sv.Toggled += Changered;
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
        private void Changered(object sender, EventArgs e)
        {
            if (stait)
            {
                client.Publish(naim, System.Text.Encoding.UTF8.GetBytes(label + "|0"));
            }
            else
            {
                client.Publish(naim, System.Text.Encoding.UTF8.GetBytes(label +"|1"));
            }
            
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
    const string server = "mqtt.MQTTBrokerEsp8266";
    const int port = 1883;

    uPLibrary.Networking.M2Mqtt.MqttClient client = new uPLibrary.Networking.M2Mqtt.MqttClient(server);




     public MainPage()
	{
		InitializeComponent();
        client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

        string clientId = Guid.NewGuid().ToString();
        
            client.Connect(clientId);
       
       
        // subscribe to the topic "/home/temperature" with QoS 2 
        client.Subscribe(new string[] { "broker/Clients" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });


        Application.Current.UserAppTheme = AppTheme.Light;


        


    }
    int f = 0;
     async void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
    {
        Console.WriteLine("reply topic  :" + e.Topic);
        Console.WriteLine("reply payload:" + e.Message.ToString());
        if (e.Topic == "broker/Clients")
        {
            string devs = System.Text.Encoding.UTF8.GetString(e.Message); 
            string[] subs = devs.Split("}{");
         
            for (int i = 0; i < subs.Length; i++)
            {
                subs[i] = subs[i].Trim('{').Trim('}');
                string dev = subs[i].Substring(0, 4);
                if (dev == "Dev/")
                {
                    
                    DevRelai temp = new DevRelai(subs[i],client, Dispatcher);
                    bool iset = true;
                    foreach (DevRelai item in Conect_Devs)
                    {
                        if (item.naim == subs[i]) iset = false;
                    }
                    if (iset)
                    {
                        Conect_Devs.Add(temp);
                        client.Subscribe(new string[] { subs[i] }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
                        client.Publish(subs[i], System.Text.Encoding.UTF8.GetBytes("Get"));
                        Dispatcher.Dispatch(() =>
                        {
                            FlexContainer.Children.Add(Conect_Devs[Conect_Devs.Count-1].Get_IView());

                        });
                    }
                   
                    // FlexContainer.Children.Add(temp.core);
                }
            }
        }
        else
        {
            if (e.Topic.Contains("Relay") && (System.Text.Encoding.UTF8.GetString(e.Message) != "Get"))
            {
                for (int i = 0; i < Conect_Devs.Count; ++i)
                {
                    if (e.Topic == Conect_Devs[i].naim)
                    {
                        string message = System.Text.Encoding.UTF8.GetString(e.Message);
                        string[] temp = message.Split("|");
                     
                           
                            Conect_Devs[i].label = temp[0];
                            Conect_Devs[i].stait = (temp[1] == "0");
                  
                    }
                }
            }
        }
        
    }

    Task _mqttClient_ConnectedAsync(MqttClientConnectedEventArgs arg)
    {
       
        Dispatcher.Dispatch(() =>
        {
           
            statuslab.Text = "Conected";
        });

        return Task.CompletedTask;
    }

    Task _mqttClient_ConnectingFailedAsync(ConnectingFailedEventArgs arg)
    {
     
        Dispatcher.Dispatch(() =>
        {
            
            statuslab.Text = "disconect";
        });
      
        return Task.CompletedTask;
    }

    Task _mqttClient_DisconnectedAsync(MqttClientDisconnectedEventArgs arg)
    {
        
        Dispatcher.Dispatch(() =>
        {
            
            statuslab.Text = "disconect";
        });

        return Task.CompletedTask;
    }

    private async void ContentPage_Loaded(object sender, EventArgs e)
    {
     
    }

    private void Frame_Focused(object sender, FocusEventArgs e)
    {
        statuslab.Text = "disconect";
    }

    
}

