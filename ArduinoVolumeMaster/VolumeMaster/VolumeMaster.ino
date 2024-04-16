#include <ezButton.h>
int pins[2] = { A0, A1 };
int lastVolume[2] = { -10, -10 };
bool initialized;
ezButton button(7);

void setup() {
  Serial.begin(9600);
  initialized = false;
  button.setDebounceTime(30);
}


void loop() {
  button.loop();
  
  for (int i = 0; i < (sizeof(pins) / sizeof(int)); i++) {
    int currentVolume = analogRead(pins[i]);
    Serial.print(currentVolume);
    if (i != (sizeof(lastVolume) / sizeof(int)) - 1) {
      Serial.print("|");
    }
  }
  
  Serial.println();

  if (button.isPressed()) {
    Serial.println("VM.changePreset");
  }


  delay(20);

  //for(int i = 0; i != -1; i++){
  //  Serial.print(i);
  //  Serial.print("|");
  //  Serial.print(i);
  //  Serial.println();
  //}
}
