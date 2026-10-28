![PepperDash Essentials Plugin Logo](/images/essentials-plugin-blue.png)

![PepperDash Essentials](https://img.shields.io/badge/PepperDash%20Essentials-≥%20v3.0.0-blue)
![.NET](https://img.shields.io/badge/.NET-8-512BD4)
![Crestron](https://img.shields.io/badge/Crestron-4--Series-lightgrey)
![License](https://img.shields.io/badge/license-MIT-green)

# Essentials CustomValues Plugin (c) 2026

## License

Provided under MIT license

## Overview

The CustomValues plugin reads and writes JSON values and bridges them to SIMPL Windows. Values are stored either in the device config `data` object (in memory) or in a separate JSON file that the plugin creates and keeps up to date.

## Control and Saving Behavior

The plugin uses explicit control joins to manage when values are eligible to be saved.

### Control Digital Joins (SIMPL <-> Plugin)

| Join | Direction | Name | Purpose |
|------|-----------|------|---------|
| 1 | Input | EnableSaving | Hold HIGH to allow persistence of changes. When LOW, saving is disabled; changes are still staged in memory and flushed when re-enabled. |
| 2 | Output | SavingReadyFb | HIGH when the plugin is mapped/ready AND EnableSaving is asserted. LOW otherwise. |

Control join metadata (capabilities and descriptions) is declared in the advanced join map (`CustomValuesBridgeJoinMap`). Input and output do NOT share join 1; using a distinct join 2 for feedback avoids collisions with bridge pathways that do not permit a single digital to act in both directions.

### Digital Join Offset (Data Boolean Join Remapping)

Boolean (digital) data joins are ALWAYS offset, starting at join 101. Integer and string values use their configured join numbers relative to `joinStart`.

### Behavior Sequence Summary

1. The plugin loads JSON data from the file or the config `data` object.
2. `LinkToApi` maps joins; the control output (SavingReadyFb) starts LOW.
3. SIMPL asserts EnableSaving HIGH:
    - SavingReadyFb is set HIGH (plugin ready + saving enabled).
4. Subsequent value changes on the bridge:
    - If EnableSaving is HIGH: changes schedule a save (debounced ~1s).
    - If EnableSaving is LOW: changes update RAM/feedbacks only and are flagged dirty until saving is re-enabled.
5. When EnableSaving transitions LOW -> HIGH again: staged changes are flushed (250ms debounce) and SavingReadyFb returns HIGH.

### Notes & Edge Cases

- Turning EnableSaving LOW immediately drops SavingReadyFb, suppressing further saves.
- Analog and serial changes made while saving is disabled are not persisted, but remain staged and are saved after enabling.
- The boolean join offset only affects boolean values.
- The plugin does not re-load from disk at runtime; it uses in-memory state. To force a reload, restart the program.

## SIMPL Bridging

Any existing value can be bridged to SIMPL Windows using a standard `eiscApiAdvanced` bridge and a custom join map. When bridging you must define a custom join map, as well as the key to that join map in the bridge.

Each join map entry is a JSON path to a **single value**. The join type comes from the value's type in the data:

| Value type | Join type | Join number |
|------------|-----------|-------------|
| Boolean | Digital | `100 + joinNumber` (offset, see above) |
| Integer | Analog | `joinStart + joinNumber - 1` |
| String | Serial | `joinStart + joinNumber - 1` |

Use dot syntax to reach a value nested inside an object, for example `"Dict.label01"`.

Paths that point to any other kind of value, such as a whole object (`"Dict"`), an array or a decimal number, are not mapped. The plugin logs `Cannot map path ... unsupported type` and skips that entry.

## Plugin Configuration Object

`filePath` is relative to the program's file path prefix (for example `/user/program1/`). When `filePath` is set, the `data` object is ignored and values are read from and saved to that file. If the file doesn't exist yet, it is created from the optional `seed` object.

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
                "data": {
                    "Dict": {
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
            "properties": {
                "control": {
                    "tcpSshProperties": {
                        "address": "127.0.0.2",
                        "port": 0
                    },
                    "ipid": "03",
                    "method": "ipidTcp"
                },
                "devices": [
                    {
                        "deviceKey": "CustomValues",
                        "joinStart": 1,
                        "joinMapKey": "customValues"
                    }
                ]
            }
        }
    ],
    "joinMaps": {
        "customValues": {
            "stringValue": {
                "joinNumber": 1
            },
            "Dict.label01": {
                "joinNumber": 2
            },
            "analogValue": {
                "joinNumber": 1
            },
            "boolValue": {
                "joinNumber": 1
            }
        }
    }
}
```

### Join Mapping Example (Boolean Offset)

With a `joinStart` of 1, the example above maps `boolValue` to digital **101**, `analogValue` to analog **1**, `stringValue` to serial **1** and `Dict.label01` to serial **2**. Bridge digital joins 1-2 are reserved for the control joins.

### File-Backed Config

```json
{
    "key": "CustomValues",
    "uid": 1,
    "name": "Custom Values Essentials Plugin",
    "type": "CustomValues",
    "group": "plugin",
    "properties": {
        "filePath": "customValues.json",
        "seed": {
            "analogValue": 37,
            "boolValue": true,
            "stringValue": "SomeString!"
        }
    }
}
```

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

## Dependencies

* Install the [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) and PowerShell 7+ (`pwsh`). `global.json` selects an installed stable .NET 8 SDK, rolling forward to its latest installed feature band/patch.
* The [Essentials](https://github.com/PepperDash/Essentials) dependency is pinned in `src/epi-utilities-customvalues.4Series.csproj` to **3.0.0-rc.19**, a prerelease available from [nuget.org](https://www.nuget.org/packages/PepperDashEssentials/3.0.0-rc.19). No authenticated feed is required.
* Deployment requires a .NET 8-capable Crestron 4-Series/VC-4 environment running the matching Essentials v3 runtime. A successful build does not verify firmware compatibility or plugin loading on hardware.

### Installing Dependencies and Building

Run from the repository root; `dotnet restore` installs the pinned dependencies:

```powershell
dotnet restore epi-utilities-customvalues.4Series.sln
dotnet build epi-utilities-customvalues.4Series.sln --no-restore --configuration Release
dotnet pack src/epi-utilities-customvalues.4Series.csproj --no-build --configuration Release
```

Builds generate a CPLZ and NuGet package in `output/`. The CPLZ includes the plugin output only, not the host-provided Essentials/Crestron dependencies (`ExcludeAssets=runtime`).

## Git Hooks (Husky.Net)

This repo uses [Husky.Net](https://alirezanet.github.io/Husky.Net/) to run a `pre-commit` hook. The hook runs `.github/scripts/Update-ReadmeBadges.ps1`, which keeps the `.NET` and `PepperDash Essentials` badges at the top of this README in sync with the project:

* `.NET` badge - `TargetFramework` in `src/*.4Series.csproj`
* `PepperDash Essentials` badge - the highest `MinimumEssentialsFrameworkVersion` in `src/*Factory.cs`

If the badges change, `README.md` is re-staged automatically so the update is part of your commit. A warning is printed if the `PepperDashEssentials` package version in the csproj differs from `MinimumEssentialsFrameworkVersion`. While the package is pinned to `3.0.0-rc.19` and the runtime minimum is the numeric `3.0.0`, that warning is expected; it does not make `-Check` fail when the badges are current.

### Setup

The hook is installed automatically the first time you do any of the following in a fresh clone:

* Open the solution in Visual Studio, or run `dotnet restore` / `dotnet build`
* Open the folder in VS Code and allow the "Install git hooks" automatic task

To install it manually:

```
dotnet tool restore
dotnet husky install
```

Verify with `git config core.hooksPath`, which should print `.husky`.

### Usage

* Commit as usual; the hook runs on every `git commit`.
* Run it on demand: `dotnet husky run`
* Check without modifying files (useful for CI): `pwsh -NoProfile -File .github/scripts/Update-ReadmeBadges.ps1 -Check`
* Skip the hook for a single commit: `git commit --no-verify`
* Skip the automatic install on restore: set the `HUSKY=0` environment variable. It is also skipped when `CI=true`.
<!-- START Minimum Essentials Framework Versions -->
### Minimum Essentials Framework Versions

- 3.0.0
<!-- END Minimum Essentials Framework Versions -->
<!-- START Config Example -->
### Config Example

```json
{
    "key": "GeneratedKey",
    "uid": 1,
    "name": "GeneratedName",
    "type": "CustomValues",
    "group": "Group",
    "properties": {
        "seed": "SampleValue",
        "filePath": "SampleString",
        "data": "SampleValue"
    }
}
```
<!-- END Config Example -->
<!-- START Supported Types -->
### Supported Types

- CustomValues
<!-- END Supported Types -->
<!-- START Join Maps -->
### Join Maps

#### Digitals

| Join | Type (RW) | Description |
| --- | --- | --- |
| 1 | R | Enable saving gate (HIGH allows persistence) |
| 2 | R | Feedback: plugin mapped and saving enabled |
<!-- END Join Maps -->
<!-- START Interfaces Implemented -->
### Interfaces Implemented

- IBridgeAdvanced
<!-- END Interfaces Implemented -->
<!-- START Base Classes -->
### Base Classes

- ReconfigurableDevice
- JoinMapBaseAdvanced
<!-- END Base Classes -->
<!-- START Public Methods -->
### Public Methods

- public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
<!-- END Public Methods -->
<!-- START Bool Feedbacks -->

<!-- END Bool Feedbacks -->
<!-- START Int Feedbacks -->

<!-- END Int Feedbacks -->
<!-- START String Feedbacks -->

<!-- END String Feedbacks -->
