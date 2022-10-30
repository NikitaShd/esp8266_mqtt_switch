

#include <ESP8266WiFi.h>
#include "uMQTTBroker.h"
#include <DNSServer.h>
#include <ESP8266WebServer.h>
#include <string>
#include <list>

char ssid[] = "MQTT_Broker_Esp8266";     // your network SSID (name)
char pass[] = ""; // your network password
//bool WiFiAP = true;      // Do yo want the ESP as AP?

IPAddress apIP(192, 168, 1, 4);  
WiFiServer server(80);
DNSServer dnsServer;
const char *server_name = "www.MQTT_Broker_Esp8266.local"; 
const byte DNS_PORT = 53;

int UpTime = 0;

/*
 * Custom broker class with overwritten callback functions
 */
class myMQTTBroker: public uMQTTBroker
{public:
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

    virtual String printClients() {
      String temp = "";
      for (int i = 0; i < getClientCount(); i++) {
        String client_id;
        getClientId(i, client_id);
        temp += "{"+client_id+"}" ;
        //Serial.println("Client "+client_id+" on addr: "+addr.toString());
      }
      return temp;
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

  Serial.println("AP started");
  Serial.println("Address: " + (String)server_name);
}

void startServesis(){
  dnsServer.start(DNS_PORT, server_name, apIP);
  server.begin();
}


void setup()
{
  Serial.begin(115200);
  Serial.println();
  Serial.println();

   pinMode(16, OUTPUT);

   Serial.println("Starting WifiAP");
  // Start WiFi
 // if (WiFiAP)
    startWiFiAP();
 // else
  //  startWiFiClient();
  Serial.println("Starting Servers");
  startServesis();
  // Start the broker
  Serial.println("Starting MQTT broker");
  myBroker.init();
/*
 * Subscribe to anything
 */
  myBroker.subscribe("broker/#");
}

void webUpdate(){
   //ожидание входящих клиентов
  WiFiClient client = server.available();
  if (client) {
    Serial.println("New client"); //появился клиент (соединение)
    //http запрос заканчивается пустой строкой
    boolean currentLineIsBlank = true;
    while (client.connected()) {
      if (client.available()) {
        char c = client.read();
        Serial.write(c);
        //если достигнут конец строки (символ новой строки)
        // и строка пустая, это говорит о том, что запрос закончился,
        // так что можно отослать ответ
        if (c == '\n' && currentLineIsBlank) {
          Serial.println("Sending response");
 
          //отослать стандартный заголовок ответа на http запрос
          // используйте символы \r\n в конце очередной строки,
          // вместо команды println, чтобы увеличить скорость передачи данных
          client.print(
            "HTTP/1.1 200 OK\r\n"
            "Content-Type: text/html\r\n"
            "Connection: close\r\n"  //соединение будет закрыто после завершения ответа
            "\r\n");
          client.print("<!DOCTYPE HTML>\r\n");
          client.print("<html>\r\n");
          client.print("<head>\r\n");
          client.print("<meta charset=\"utf-8\">\r\n");
          //client.print("<meta http-equiv=\"refresh\" content=\"30\">\r\n");//обновлять страницу автоматически каждые 30 секунд
          client.print("<title>ServerData</title>\r\n");
          client.print("</head>\r\n");
          client.print("<h1>ESP MQTT Server</h1>\r\n");
   
          client.print("Server tiks: ");
          client.print(UpTime);
          client.print("<br>\r\n");
           client.print("Clients Caunt: ");
          client.print((String)myBroker.getClientCount()); 
          client.print("<br>\r\n");
          client.print("Clients: ");
          client.print( (String)myBroker.printClients()); 
          client.print("<br>\r\n");
          client.print("getFreeHeap: ");
          client.println(ESP.getFreeHeap());
          client.print("<br>\r\n");
          client.print("getHeapFragmentation: ");
          client.println(ESP.getHeapFragmentation());
          client.print("<br>\r\n");
          client.print("getMaxFreeBlockSize: ");
          client.println(ESP.getMaxFreeBlockSize());
          client.print("<br>\r\n");
           client.print("</html>\r\n");
          break;
        }
        if (c == '\n') {
          //начало новой строки
          currentLineIsBlank = true;
        }
        else if (c != '\r') {
          // получен очередной символ текущей строки
          currentLineIsBlank = false;
        }
      }
    }
    //время для получения браузером ответа
    delay(10);
 
    //закрытие соединения
    client.stop();
    Serial.println("Client disconnected");
  }
 
}

void loop()
{
/*
 * Publish the counter value as String
 */
  dnsServer.processNextRequest();
  Serial.println("===========================================================");
  myBroker.publish("broker/UpTime", (String)(UpTime=UpTime + 1)+":seconds");
  myBroker.publish("broker/ClientCount",(String)myBroker.getClientCount());
  myBroker.publish("broker/Clients",(String)myBroker.printClients());
  myBroker.printClients();
  // wait a second
  Serial.print("getFreeHeap: ");
  Serial.println(ESP.getFreeHeap());
  Serial.print("getHeapFragmentation: ");
  Serial.println(ESP.getHeapFragmentation());
  Serial.print("getMaxFreeBlockSize: ");
  Serial.println(ESP.getMaxFreeBlockSize());
  delay(500);
  digitalWrite(16, HIGH);
  delay(500);
  digitalWrite(16, LOW);
  
  webUpdate();
}
