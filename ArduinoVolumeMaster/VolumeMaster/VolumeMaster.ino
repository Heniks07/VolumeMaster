int pins[2] = { A0, A1};
int lastVolume[2] = { -10, -10};
bool initialized;
;

void setup() {
  Serial.begin(9600);
  initialized = false;
}


void loop() {
  // put your main code here, to run repeatedly:
  //Serial.println(analogRead(A0));
  if(Serial.available()){
    String input = Serial.readStringUntil('\n');
  
    if(input.equals("getVolume"))
      printVolume();
  }

  if (!initialized)
    delay(1000);

  bool doPrint = false;
  for (int i = 0; i < (sizeof(pins) / sizeof(int)); i++) {
    int currentVolume = analogRead(pins[i]);

    if (abs(currentVolume - lastVolume[i]) > 5) {
      lastVolume[i] = currentVolume;
      doPrint = true;
    }
  }

  if (doPrint || !initialized) {
    printVolume();
    initialized = true;
  }
  delay(12);
}

void printVolume(){
  for (int i = 0; i < (sizeof(lastVolume) / sizeof(int)); i++) {
      Serial.print(lastVolume[i]);
      if (i != (sizeof(lastVolume) / sizeof(int)) - 1) {
        Serial.print("|");
      }
    }
    Serial.println();
}