#include <ezButton.h>
const int numberPins = 5;
const int pins[numberPins] = { A0, A1, A2, A3, A4};
int lastVolume[numberPins] = {};
bool initialized;
ezButton changePreset(10);
ezButton playPause(6);
ezButton next(4);
ezButton previous(8);
ezButton stop(2);



void setup() {
  Serial.begin(9600);
  initialized = false;
  changePreset.setDebounceTime(30);
  playPause.setDebounceTime(30);
  next.setDebounceTime(30);
  previous.setDebounceTime(30);
  stop.setDebounceTime(30);
}


void loop() {
  changePreset.loop();
  playPause.loop();
  next.loop();
  previous.loop();
  stop.loop();
  
  for (int i = 0; i < (sizeof(pins) / sizeof(int)); i++) {
    int currentVolume = analogRead(pins[i]);
    char buffer[8];
    sprintf(buffer,"%04d", currentVolume);
    Serial.print(buffer);
    if (i != (sizeof(lastVolume) / sizeof(int)) - 1) {
      Serial.print("|");
    }
  }
  
  Serial.println();

  if (changePreset.isPressed()) {
    Serial.println("VM.changePreset");
    Serial.println("VM.changePreset");
  }
  if(playPause.isPressed()){
    Serial.println("VM.playPause");
    Serial.println("VM.playPause");
  }
  if(next.isPressed()){
    Serial.println("VM.next");
    Serial.println("VM.next");
  }
  if(previous.isPressed()){
    Serial.println("VM.previous");
    Serial.println("VM.previous");
  }
  if(stop.isPressed()){
    Serial.println("VM.stop");
    Serial.println("VM.stop");
  }


  //delay(20);

  //for(int i = 0; i != -1; i++){
  //  Serial.print(i);
  //  Serial.print("|");
  //  Serial.print(i);
  //  Serial.println();
  //}
}
