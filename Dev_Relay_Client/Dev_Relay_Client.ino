
#define LED_Relay D0
#define LED_info 16
#include <ESP8266WiFi.h>
#include <PubSubClient.h>

const char* ssid = "MQTT_Broker_Esp8266"; 
const char* password =  "";

const char* mqtt_server = "mqtt.MQTTBrokerEsp8266";
const int mqtt_port = 1883;

String mqtt_Nai_topic = "Dev/Relay/";
String Relay_Name = "Relay-1";
int staite = 0;

WiFiClient espClient;
PubSubClient client(espClient);

String getValue(String data, char separator, int index)
{
  int found = 0;
  int strIndex[] = {0, -1};
  int maxIndex = data.length()-1;

  for(int i=0; i<=maxIndex && found<=index; i++){
    if(data.charAt(i)==separator || i==maxIndex){
        found++;
        strIndex[0] = strIndex[1]+1;
        strIndex[1] = (i == maxIndex) ? i+1 : i;
    }
  }

  return found>index ? data.substring(strIndex[0], strIndex[1]) : "";
}

void MQTTcallback(char* topic, byte* payload, unsigned int length) 
{
  Serial.print("Message received in topic: ");
  Serial.println(topic);
  Serial.print("Message:");
  String message;
  for (int i = 0; i < length; i++) 
  {
    message = message + (char)payload[i];
  }
  Serial.print(message);
  if (message == "Get") 
  {
   client.publish(mqtt_Nai_topic.c_str(), (Relay_Name +"|"+String(staite)).c_str());
  }
  else 
  {
   String Status = getValue(message,'|',1);
   if(Status == "1"){
     digitalWrite(LED_Relay, HIGH);
     staite = 1;
   }
   else if(Status == "0")
   {
     digitalWrite(LED_Relay, LOW);
     staite = 0;
   }
  }
  Serial.println();
  Serial.println("-----------------------");
}

void setup() {
  pinMode(LED_Relay, OUTPUT);
  pinMode(LED_info, OUTPUT);
  mqtt_Nai_topic += WiFi.macAddress();
  WiFi.begin(ssid, password);
  Serial.begin(115200);

  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.println("Connecting to WiFi..");
     digitalWrite(LED_info, LOW);
      delay(100);
     digitalWrite(LED_info, HIGH);
       delay(100);
      digitalWrite(LED_info, LOW);
      delay(100);
     digitalWrite(LED_info, HIGH);
  }
  Serial.print("Connected to WiFi :");
  Serial.println(WiFi.SSID());

   client.setServer(mqtt_server, mqtt_port);
  client.setCallback(MQTTcallback);
  while (!client.connected()) 
  {
    Serial.println("Connecting to MQTT...");
   
    if (client.connect(mqtt_Nai_topic.c_str()))
    {
      Serial.println("connected");
    }
    else
    {
      Serial.print("failed with state ");
      Serial.println(client.state());
      delay(1000);
      digitalWrite(LED_info, LOW);
      delay(1000);
      digitalWrite(LED_info, HIGH);
    }
  }
  client.subscribe(mqtt_Nai_topic.c_str());
  client.publish(mqtt_Nai_topic.c_str(), (Relay_Name +"|"+String(staite)).c_str());
}



void loop() {
  // put your main code here, to run repeatedly:
client.loop();
}
