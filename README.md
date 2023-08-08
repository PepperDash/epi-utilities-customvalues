# Internal Essentials Plugin Template (c) 2022

## License
Provided under MIT license

## Overview
CustomValues EPI is a plugin that will allow one to read and write Json config values. Values can be stored in the DEvice config "Data" object or in a seperate file.

### Simpl Bridging
Any exsisting value can be bridged to simpl windows using a standard EiscBridgeAdvanced and a custom join map. When bridging you must define a custom joinMap as well as the key to the joinMap in the bridge. values are automaticcly types based on their entry in the config. booleans come out as digitals, integers come out as analogs and strings come out as serials. You can dig down into a Json object using stnadard dot syntax (see example "Dict.label01"). You can also bridge to an oblect and it will propegrate the whole object as a Serial join on the bridge (see example "Dict").

### Console Command
All values can also be set and retrived using the console command "customvalues [path] ([value])".

### Plugin Configuration Object
```json
{
    "devices":
    [
        {
            "key": "CustomValues",
            "uid": 1,
            "name": "Custom Values Essentials Plugin",
            "type": "CustomValues",
            "group": "plugin",
            "properties":
            {
                "FilePath-EXAMPLE": "/user/program1/testFile.test",
                "FilePathComment": "If FilePath (without -EXAMPLE) is included it will be used and Data object below will be ignored",
                "Data":
                {
                    "Dict":
                        {
                            "label01": "MainSourceSelector",
                            "instanceTag": "NAMED_CONTROL"
                        },
                    "analogValue": 37,
                    "boolValue": true,
                    "stringValue": "SomeString!"
                }
            }
        },
        {
            "key": "CustomValuesBridge",
            "uid": 4,
            "name": "CustomValuesBridge",
            "group": "api",
            "type": "eiscApiAdvanced",
            "properties":
            {
                "control":
                {
                    "tcpSshProperties":
                    {
                        "address": "127.0.0.2",
                        "port": 0
                    },
                    "ipid": "03",
                    "method": "ipidTcp"
                },
                "devices":
                [
                    {
                        "deviceKey": "CustomValues",
                        "joinStart": 1,
                        "joinMapKey": "customValues"
                    }
                ]
            }
        }
    ],
    "joinMaps":
    {
        "customValues":
        {
            "Dict":
            {
                "joinNumber": 1
            },
            "Dict.label01":
            {
                "joinNumber": 2
            },
            "analogValue":
            {
                "joinNumber": 1
            },
            "boolValue":
            {
                "joinNumber": 1
            }
        }
    },
}
```

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