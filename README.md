# CS2QuickSetup
# GXRWes cs2 setup

## How to install

- clone git repo or just copy the files i guess
- enable console in cs2
- copy desired cfg (cherry pick or just jump the entire content of cs2 folder) to ```C:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive\game\csgo\cfg```
- open console and type 
    ```exec "config u want to exec"```
    for eg:
    ```exec a ```

## add to autostart

- open steam/libary
- rightclick on cs2
- go to properties
- then in "general" add this to the startup
    ```-exec a```
    i have added some extra startup stuff aswell in my startup, still need to reconfigure for cs2
    ```-novid -exec a -freq 144 -nojoy -high```

## practice config
- launch a map (via console with ```map de_....```) or via the practice window
- open console and type ```exec p```
- some instructions are in the console
- enjoy