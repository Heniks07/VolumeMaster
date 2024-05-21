# Volume Master
## Introduction 
Volume Master is a project inspired by [omriharel/deej](https://github.com/omriharel/deej) but since I have never done anything in Go I chose to redo everything.
VolumeMaster can change the volume level of single applications or whole devices with an Arduino and potentiometers. It also has multiple presets to quickly change what application/device the potentiometers are supposed to control. To see what each Poti does, there is a 8x8 LET matrix. What the matrix displays can also be changed according to the selected preset.

## Features
| Feature                         | Windows                            | Linux                              |
|---------------------------------|------------------------------------|------------------------------------| 
| Volume change for applications  | <ul><li>[x] </li></ul>             | <ul><li>[x] </li></ul>             |
| Volume change for Devices/Sinks | <ul><li>[x] </li></ul>             | <ul><li>[x] </li></ul>             |
| Preset                          | <ul><li>[x] </li></ul>             | <ul><li>[x] </li></ul>             |
| LED matrix                      | <ul><li>[ ] coming soon </li></ul> | <ul><li>[ ] coming soon </li></ul> |
|                                 | Multimedia                         |                                    | 
| Play button                     | <ul><li>[ ] coming soon </li></ul> | <ul><li>[x] </li></ul>             |
| Previous button                 | <ul><li>[ ] coming soon </li></ul> | <ul><li>[x] </li></ul>             |
| Next button                     | <ul><li>[ ] coming soon </li></ul> | <ul><li>[x] </li></ul>             |
| Stop button                     | <ul><li>[ ] coming soon </li></ul> | <ul><li>[x] </li></ul>             |
|                                 | Android Remote                     |                                    |
| Manual override                 | <ul><li>[x] </li></ul>             | <ul><li>[ ] coming soon </li></ul> |
| Play/Pause                      | <ul><li>[x] </li></ul>             | <ul><li>[ ] coming soon </li></ul> |
| Previous/Next                   | <ul><li>[x] </li></ul>             | <ul><li>[ ] coming soon </li></ul> |
| Update Config                   | <ul><li>[ ] coming soon </li></ul> | <ul><li>[ ] coming soon </li></ul> |

### Disclaimer
I don't have a particular order in which I develop these features. I just work on what I need or what I just feel like developing. Please don't ask me for features. Should you need some, try to implement them yourself and send a push request. 

## Requirements
### Linux
- playerctl    (for deteail on how to install, check out their [github repo](https://github.com/altdesktop/playerctl))
- Pulse audio / PipeWire + pipewire-pulse (make sure pactl works)

