# Internal Essentials Plugin Template (c) 2022

## License
Provided under MIT license

## Overview
CustomValues EPI is a plugin that will allow one to read and write Json config values. Values can be stored in the DEvice config "Data" object or in a seperate file. 

### Simpl Bridging 
Any exsisting value can be bridged to simpl windows using a standard EiscBridgeAdvanced and a custom join map. When bridging you must define a custom joinMap as well as the key to the joinMap in the bridge. values are automaticcly types based on their entry in the config. booleans come out as digitals, integers come out as analogs and strings come out as serials. You can dig down into a Json object using stnadard dot syntax (see example "Dict.label01"). You can also bridge to an oblect and it will propegrate the whole object as a Serial join on the bridge (see example "Dict"). 

### Console Command 
All values can also be set and retrived using the console command "customvalues [path] ([value])". 

## Nuget
You must have nuget.exe installed and in the PATH environment variable to use the following command. Nuget.exe is available at nuget.org.  It is recommended to use [Scoop](https://scoop.sh/) to install nuget using the command: 
```
scoop install nuget
```
### Manually Installing Dependencies
To install dependencies once nuget.exe is installed, after cloning the template or creating your template repo, run the following command: 
```
nuget install .\packages.config -OutputDirectory .\packages -excludeVersion
```
Verify you are using the proper "\\" or "/" per the console used.  To verify that the packages installed correctly, open Essentials and make sure that all references are found, then try and build it.  **This issue will be found when using WSL on Windows10.**
Once the nuget package has been installed locally you will need to update your project references.
1. Right click on **References**
2. Select **Add Reference**
3. Browse to the **packages** folder 
4. Select the required references.
## Device Specific Information

### Plugin Configuration Object
Update the configuration object as needed for the plugin being developed.
```json
{
    "devices": [
        {
            "key": "essentialsPluginKey",
            "name": "Essentials Plugin Name",
            "type": "essentialsPluginTypeName",
            "group": "pluginDevices",
            "properties": {
                "control": {
                    "method": "See PepperDash.Core.eControlMethod for valid control methods",
                    "controlPortDevKey": "exampleControlPortDevKey",
                    "controlPortNumber": 1,
                    "comParams": {
                        "baudRate": 9600,
                        "dataBits": 8,
                        "stopBits": 1,
                        "parity": "None",
                        "protocol": "RS232",
                        "hardwareHandshake": "None",
                        "softwareHandshake": "None"
                    },
                    "tcpSshProperties": {
                        "address": "172.22.0.101",
                        "port": 22,
                        "username": "admin",
                        "password": "password",
                        "autoReconnect": true,
                        "autoReconnectIntervalMs": 10000
                    }
                },
                "pollTimeMs": 30000,
                "warningTimeoutMs": 180000,
                "errorTimeoutMs": 300000,
                "pluginCollection": {
                    "item1": {
                        "name": "Item 1",
                        "value": 1
                    },
                    "item2": {
                        "name": "Item 2",
                        "value": 2
                    }
                }
            }
        }       
    ]
}
```
### Plugin Bridge Configuration Object
Update the bridge configuration object as needed for the plugin being developed.
```json
{
    "devices": [
        {
            "key": "essentialsPluginBridgeKey",
            "name": "Essentials Plugin Bridge Name",
            "group": "api",
            "type": "eiscApiAdvanced",
            "properties": {
                "control": {
                    "ipid": "1A",
                    "tcpSshProperties": {
                        "address": "127.0.0.2",
                        "port": 0
                    }
                },
                "devices": [
                    {
                        "deviceKey": "essentialsPluginKey",
                        "joinStart": 1
                    }
                ]
            }
        }
    ]
}
```
### SiMPL EISC Bridge Map
The selection below documents the digital, analog, and serial joins used by the SiMPL EISC. Update the bridge join maps as needed for the plugin being developed.
#### Digitals
| dig-o (Input/Triggers)                | I/O | dig-i (Feedback) |
|---------------------------------------|-----|------------------|
|                                       | 1   | Is Online        |
| Connect (Held) / Disconnect (Release) | 2   | Connected        |
|                                       | 3   |                  |
|                                       | 4   |                  |
|                                       | 5   |                  |
#### Analogs
| an_o (Input/Triggers) | I/O | an_i (Feedback) |
|-----------------------|-----|-----------------|
|                       | 1   | Socket Status   |
|                       | 2   | Monitor Status  |
|                       | 3   |                 |
|                       | 4   |                 |
|                       | 5   |                 |
#### Serials
| serial-o (Input/Triggers) | I/O | serial-i (Feedback) |
|---------------------------|-----|---------------------|
|                           | 1   | Device Name         |
|                           | 2   |                     |
|                           | 3   |                     |
|                           | 4   |                     |
|                           | 5   |                     |