# Internal Essentials Plugin Template (c) 2022
## License
Provided under MIT license

## Overview
CustomValues EPI is a plugin that will allow one to read and write Json config values. Values can be stored in the DEvice config "Data" object or in a seperate file.

## New Control and Behavior (Extended Saving Workflow)
Beginning with the updated implementation (post Oct 2025 changes), the plugin introduces explicit control joins to manage when values are eligible to be saved.

### Control Digital Joins (SIMPL <-> Plugin)
| Join | Direction | Name | Purpose |
|------|-----------|------|---------|
| 1 | Input | EnableSaving | Hold HIGH to allow persistence of changes. When LOW, saving disabled; if `trackChangesWhileSavingDisabled` is true changes are staged in memory, otherwise ignored. |
| 2 | Output | SavingReadyFb | HIGH when plugin is internally mapped/ready AND EnableSaving asserted. LOW otherwise. |

Control join metadata (capabilities and descriptions) is now declared in the advanced join map (`EssentialsPluginBridgeJoinMapTemplate`). Input and output do NOT share join 1; using distinct join 2 for feedback avoids collisions with some bridge pathways that do not permit a single digital to act as both directions simultaneously.

### Digital Join Offset (Data Boolean Join Remapping)
DEFAULTS UPDATED (Oct 2025+): `legacyDigitalJoinBehavior` now defaults to TRUE (you can omit it). When TRUE, boolean data joins are NOT offset and use their configured join numbers relative to `joinStart` (legacy behavior).

To opt-in to the safer high-range offset, explicitly set:
```json
"legacyDigitalJoinBehavior": false
```
When set false, all bridged boolean (digital) data values begin at join 101 (offset base) to reduce collision risk with low-number control joins. Integer, string, and object-based values always use their configured join numbering relative to `joinStart`.

### Behavior Sequence Summary
1. Plugin loads JSON data from file or config `data` object.
2. `LinkToApi` maps joins; control outputs (SavingReadyFb) start LOW.
3. SIMPL asserts EnableSaving HIGH:
    - SavingReadyFb set HIGH (plugin ready + saving enabled).
4. Subsequent value changes on the bridge:
    - If EnableSaving HIGH: changes schedule a save (debounced ~1s).
    - If EnableSaving LOW AND `trackChangesWhileSavingDisabled` true: changes update RAM/feedbacks only; flagged dirty until re-enabled.
    - If EnableSaving LOW AND `trackChangesWhileSavingDisabled` false: changes are ignored entirely (no staging, no feedback updates).
5. When EnableSaving transitions LOW -> HIGH again: if tracking flag true pending staged changes are flushed (250ms debounce) and SavingReadyFb returns HIGH.

### Notes & Edge Cases
- Turning EnableSaving LOW immediately drops SavingReadyFb, suppressing further saves.
- Analog/String/Object changes while disabled:
    - If `trackChangesWhileSavingDisabled` true (DEFAULT): not persisted but remain staged and will be saved after enabling.
    - If `trackChangesWhileSavingDisabled` false: ignored completely.
- Boolean join offset only affects JTokenType.Boolean mapped joins.
- The plugin does not automatically re-load from disk during runtime; it uses in-memory state. To force a reload, restart the device or extend logic (future enhancement).

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
                },
                "legacyDigitalJoinBehavior": true,
                "trackChangesWhileSavingDisabled": true
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

### Updated Join Mapping Example (Boolean Offset)
If you explicitly set `"legacyDigitalJoinBehavior": false` and `joinStart` is 1, a boolean path configured with `"joinNumber": 1` will map to digital join **101** on the bridge instead of 1. (When the property is omitted or true, it maps directly to join 1.) Control joins 1-2 remain reserved.

### Minimal Config Without File (In-Memory Only)
```json
{
    "devices": [
        {
            "key": "CustomValues",
            "uid": 1,
            "name": "Custom Values Essentials Plugin",
            "type": "CustomValues",
            "group": "plugin",
            "properties": {
                "data": { "boolValue": true, "analogValue": 5 }
            }
        }
    ]
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