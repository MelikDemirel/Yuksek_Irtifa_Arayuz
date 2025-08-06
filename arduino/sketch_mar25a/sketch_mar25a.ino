void setup() {
  Serial.begin(9600);
}

void loop() {
  // Simulate reading data from a sensor
  int sensorValue = analogRead(A0);
  Serial.println(sensorValue);
  delay(100); // Adjust delay as needed
}
