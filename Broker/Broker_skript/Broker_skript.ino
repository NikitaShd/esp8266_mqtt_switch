

#include <ESP8266WiFi.h>
#include "uMQTTBroker.h"
#include <DNSServer.h>
#include <ESP8266WebServer.h>

char ssid[] = "MQTT_Broker_Esp8266";     // your network SSID (name)
char pass[] = ""; // your network password
//bool WiFiAP = true;      // Do yo want the ESP as AP?

IPAddress apIP(192, 168, 1, 4);   
DNSServer dnsServer;
const char *server_name = "www.MQTT_Broker_Esp8266.local"; 
const byte DNS_PORT = 53;
/*
 * Custom broker class with overwritten callback functions
 */
class myMQTTBroker: public uMQTTBroker
{
public:
    virtual bool onConnect(IPAddress addr, uint16_t client_count) {
      Serial.println(addr.toString()+" connected");
      return true;
    }

    virtual void onDisconnect(IPAddress addr, String client_id) {
      Serial.println(addr.toString()+" ("+client_id+") disconnected");
    }

    virtual bool onAuth(String username, String password, String client_id) {
      Serial.println("Username/Password/ClientId: "+username+"/"+password+"/"+client_id);
      return true;
    }
    
    virtual void onData(String topic, const char *data, uint32_t length) {
      char data_str[length+1];
      os_memcpy(data_str, data, length);
      data_str[length] = '\0';
      
      Serial.println("received topic '"+topic+"' with data '"+(String)data_str+"'");
      //printClients();
    }

    // Sample for the usage of the client info methods

    virtual void printClients() {
      for (int i = 0; i < getClientCount(); i++) {
        IPAddress addr;
        String client_id;
         
        getClientAddr(i, addr);
        getClientId(i, client_id);
        Serial.println("Client "+client_id+" on addr: "+addr.toString());
      }
    }
};

myMQTTBroker myBroker;

/*
 * WiFi init stuff
 */
void startWiFiClient()
{
  Serial.println("Connecting to "+(String)ssid);
  WiFi.mode(WIFI_STA);
  WiFi.begin(ssid, pass);
  
  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");
  }
  Serial.println("");
  
  Serial.println("WiFi connected");
  Serial.println("IP address: " + WiFi.localIP().toString());
}

void startWiFiAP()
{
  WiFi.mode(WIFI_AP);
  WiFi.softAP(ssid, pass);
  delay(100);
  
  WiFi.softAPConfig(apIP, apIP, IPAddress(255, 255, 255, 0));

  dnsServer.start(DNS_PORT, server_name, apIP);

  Serial.println("AP started");
  Serial.println("Address: " + (String)server_name);
}

void setup()
{
  Serial.begin(115200);
  Serial.println();
  Serial.println();

  // Start WiFi
 // if (WiFiAP)
    startWiFiAP();
 // else
  //  startWiFiClient();

  // Start the broker
  Serial.println("Starting MQTT broker");
  myBroker.init();
/*
 * Subscribe to anything
 */
  myBroker.subscribe("broker/#");
}

int UpTime = 0;

void loop()
{
/*
 * Publish the counter value as String
 */
  dnsServer.processNextRequest();
  Serial.println("===========================================================");
  myBroker.publish("broker/UpTime", (String)(UpTime=UpTime + 1)+":seconds");

  myBroker.publish("broker/ClientCount", (String)myBroker.getClientCount());

  myBroker.printClients();
  // wait a second
  delay(1000);
}
