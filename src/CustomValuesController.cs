using Crestron.SimplSharp;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharpPro.DeviceSupport;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Config;
using PepperDash.Essentials.Core.Devices;
using System;
using System.Collections.Generic;
using Feedback = PepperDash.Essentials.Core.Feedback;

namespace UtilitiesCustomValues
{
    /// <summary>
    /// Controller implementation for the CustomValues plugin. Handles JSON data lifecycle
    /// (loading, mapping, staging, and persistence) and exposes bridge joins for both control
    /// gating (EnableSaving) and readiness feedback (SavingReadyFb). Boolean data join numbering
    /// optionally applies an offset unless legacy behavior is enabled.
    /// </summary>
    public class CustomValuesController : ReconfigurableDevice, IBridgeAdvanced
    {
        private readonly CustomValuesConfigObject _props;
        private readonly CTimer _saveTimer;
        private readonly CCriticalSection _sync = new CCriticalSection();

        private JObject data;

        // Control join constants (do not reuse the same join number for input and output; many bridge/processor paths treat them distinctly)
        private const ushort ControlJoinEnableSavingInput = 1; // Input: EnableSaving (SIMPL -> EPI)
        private const ushort ControlJoinSavingReadyOutput = 2; // Output: SavingReadyFb (EPI readiness + EnableSaving asserted)
        private const ushort DigitalJoinBaseOffset = 101; // Starting join for custom boolean data outputs when legacy behavior disabled

            // State flags
        private bool _enableSaving; // saving gate
        private bool _pluginReady; // internal mapping complete
        private bool _savingReady; // composite (pluginReady && enableSaving)
        private bool _dirtyWhileDisabled; // changes occurred while saving disabled (only tracked if tracking flag true)

        // Stored references
        private readonly List<Feedback> _allFeedbacks = new List<Feedback>();
        private BasicTriList _trilist; // saved for control output updates

        // Feedback objects for control outputs
        private BoolFeedback _savingReadyFeedback; // join 3

        /// <summary>
        /// Constructs the controller, loading initial data either from a file (when a filePath
        /// is supplied) or the in-config data object, seeding the file if necessary.
        /// </summary>
        /// <param name="config">Essentials device configuration object containing plugin properties.</param>
        public CustomValuesController(DeviceConfig config)
            : base(config)
        {
            _saveTimer = new CTimer(_ => WithLock(() =>
            {
                if (string.IsNullOrEmpty(_props.FilePath))
                {
                    Debug.LogInformation(this, "File path not specified... saving values locally");
                    return;
                }

                try
                {
                    SaveData(_props.FilePath, data);
                    var handler = DataSaved;
                    if (handler != null)
                    {
                        handler(this, EventArgs.Empty);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning(this, "Failed to save custom values data: '{0}'", ex);
                }
            }),
                Timeout.Infinite);

            _props = config.Properties.ToObject<CustomValuesConfigObject>();

            data = string.IsNullOrEmpty(_props.FilePath)
                ? _props.Data
                : SeedData(_props.FilePath, _props.Seed);

            if (data == null)
            {
                Debug.LogInformation(this, "No data found in the config or file, using an empty JObject.");
                data = new JObject();
            }
            else
            {
                Debug.LogDebug(this, "Loaded data from file: {0}", _props.FilePath);
            }

            // Initialize control feedback objects early
            _savingReadyFeedback = new BoolFeedback(() => _savingReady);
        }

        /// <summary>
        /// Maps JSON tokens defined in the advanced join map to SIMPL joins, wiring signal actions
        /// for incoming changes and creating feedback objects for outward state. Also wires control
        /// joins for enabling saving and reporting readiness.
        /// </summary>
        /// <param name="trilist">The tri-list instance representing the EISC/processor bridge.</param>
        /// <param name="joinStart">Starting join offset provided by the bridge binding.</param>
        /// <param name="joinMapKey">Key used to resolve the advanced join map definition.</param>
        /// <param name="bridge">Bridge instance (may be null in some contexts).</param>
        public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = new EssentialsPluginBridgeJoinMapTemplate(joinStart);

            if (data == null)
                throw new NullReferenceException("LinkToApi: data is null.  Check file path or a 'data' object is defined in the config");

            _trilist = trilist;
            DataSaved += (sender, args) => _allFeedbacks.ForEach(f => f.FireUpdate());

            var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);

            if (customJoins == null)
            {
                Debug.LogInformation(this, "Custom Joins not found!!!");
                return;
            }

            if (customJoins != null)
            { joinMap.SetCustomJoinData(customJoins); }

            Debug.LogDebug(this, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
            Debug.LogDebug(this, "Linking to Bridge Type {0}", GetType().Name);

            // Link control outputs (BoolFeedback -> BooleanInput drives outward state in Essentials)
            // Prefer using the constant so if join map custom data not found we still know intended default
            var savingReadyJoin = joinMap.SavingReadyFb.JoinNumber;
            if (savingReadyJoin == 0) savingReadyJoin = ControlJoinSavingReadyOutput;
            _savingReadyFeedback.LinkInputSig(trilist.BooleanInput[savingReadyJoin]);
            _allFeedbacks.Add(_savingReadyFeedback);
            _pluginReady = false;
            _savingReady = false;
            _savingReadyFeedback.FireUpdate();

            // Control inputs wiring
            var enableSavingJoin = joinMap.EnableSaving.JoinNumber;
            if (enableSavingJoin == 0) enableSavingJoin = ControlJoinEnableSavingInput;
            trilist.SetBoolSigAction(enableSavingJoin, state =>
            {
                WithLock(() =>
                {
                    if (_enableSaving == state) return; // no change
                    _enableSaving = state;
                    Debug.LogDebug(this, "EnableSaving changed -> {0}", _enableSaving);
                    if (_enableSaving)
                    {
                        _savingReady = _pluginReady && _enableSaving;
                        _savingReadyFeedback.FireUpdate();
                        // If tracked changes occurred while disabled (only tracked when flag true), flush them now
                        if (_props.TrackChangesWhileSavingDisabled && _dirtyWhileDisabled)
                        {
                            _dirtyWhileDisabled = false;
                            _saveTimer.Reset(250); // quick write after enabling
                        }
                    }
                    else
                    {
                        _savingReady = false;
                        _savingReadyFeedback.FireUpdate();
                    }
                });
            });

            foreach (var customJoin in customJoins)
            {
                var path = customJoin.Key;
                var index = customJoin.Value.JoinNumber;
                var join = (ushort)(index + joinStart - 1);

                if (index == 0)
                {
                    Debug.LogInformation(this, "Cannot map path: '{0}', missing join number", path);
                    continue;
                }

                Debug.LogDebug(this, "Attempting to map path: '{1}' to join: '{0}'", join, path);
                var value = data.SelectToken(path);
                if (value == null)
                {
                    Debug.LogInformation(this, "No value found for path: '{0}' in '{1}', ignoring path, update customValues if needed.", path, _props.FilePath);
                    continue;
                }

                Debug.LogDebug(this, "Mapping path: '{1}' to join: '{0}' as type: '{2}' with value: '{3}'",
                    join, path, value.Type.ToString(), value);

                switch (value.Type)
                {
                    case JTokenType.Integer:
                        {
                            trilist.SetUShortSigAction(@join, x => WithLock(() =>
                            {
                                if (!_enableSaving && !_props.TrackChangesWhileSavingDisabled) return; // ignore while disabled
                                data.SelectToken(path).Replace(x);
                                if (_enableSaving)
                                    _saveTimer.Reset(1000);
                                else if (_props.TrackChangesWhileSavingDisabled)
                                    _dirtyWhileDisabled = true;
                            }));

                            var feedback = new IntFeedback(() => data.SelectToken(path).Value<int>());
                            feedback.LinkInputSig(trilist.UShortInput[@join]);
                            _allFeedbacks.Add(feedback);
                            feedback.FireUpdate();
                            feedback.OutputChange +=
                                (sender, args) =>
                                    Debug.LogDebug(this, "Value for path:{0} updated to {1}", path, args.IntValue);

                            break;
                        }
                    case JTokenType.String:
                        {
                            trilist.SetStringSigAction(@join, x => WithLock(() =>
                            {
                                if (!_enableSaving && !_props.TrackChangesWhileSavingDisabled) return;
                                data.SelectToken(path).Replace(x);
                                if (_enableSaving)
                                    _saveTimer.Reset(1000);
                                else if (_props.TrackChangesWhileSavingDisabled)
                                    _dirtyWhileDisabled = true;
                            }));

                            var feedback = new StringFeedback(() => data.SelectToken(path).Value<string>());
                            feedback.LinkInputSig(trilist.StringInput[@join]);
                            _allFeedbacks.Add(feedback);
                            feedback.FireUpdate();
                            feedback.OutputChange +=
                                (sender, args) =>
                                    Debug.LogDebug(this, "Value for path:{0} updated to {1}", path, args.StringValue);

                            break;
                        }
                    case JTokenType.Object:
                        {
                            trilist.SetStringSigAction(@join, x =>
                            {
                                if (!_enableSaving && !_props.TrackChangesWhileSavingDisabled) return;
                                data.SelectToken(path).Replace(x);
                                if (_enableSaving)
                                    _saveTimer.Reset(1000);
                                else if (_props.TrackChangesWhileSavingDisabled)
                                    _dirtyWhileDisabled = true;
                            });

                            var feedback = new StringFeedback(() => data.SelectToken(path).Value<string>());
                            feedback.LinkInputSig(trilist.StringInput[@join]);
                            _allFeedbacks.Add(feedback);
                            feedback.FireUpdate();
                            feedback.OutputChange +=
                                (sender, args) =>
                                    Debug.LogDebug(this, "Value for path:{0} updated to {1}", path, args.StringValue);

                            break;
                        }
                    case JTokenType.Boolean:
                        {
                            // Apply new digital join offset unless legacy behavior enabled
                            if (!_props.LegacyDigitalJoinBehavior)
                            {
                                join = (ushort)(DigitalJoinBaseOffset + index - 1);
                            }

                            trilist.SetBoolSigAction(@join, x =>
                            {
                                if (!_enableSaving && !_props.TrackChangesWhileSavingDisabled) return;
                                data.SelectToken(path).Replace(x);
                                if (_enableSaving)
                                    _saveTimer.Reset(1000);
                                else if (_props.TrackChangesWhileSavingDisabled)
                                    _dirtyWhileDisabled = true;
                            });

                            var feedback = new BoolFeedback(() => data.SelectToken(path).Value<bool>());
                            feedback.LinkInputSig(trilist.BooleanInput[@join]);
                            _allFeedbacks.Add(feedback);
                            feedback.FireUpdate();
                            feedback.OutputChange +=
                                (sender, args) =>
                                    Debug.LogDebug(this, "Value for path:{0} updated to {1}", path, args.BoolValue);

                            break;
                        }
                    default:
                        {
                            Debug.LogInformation(this, "Cannot map path: '{0}', unsupported type: {1}", path, value.Type);
                            continue;
                        }
                }
            }

            _pluginReady = true;
            _savingReady = _pluginReady && _enableSaving;
            _savingReadyFeedback.FireUpdate();
            Debug.LogDebug(this, "Finished mapping joins. SavingReady: {0}", _savingReady);
        }

        /// <summary>
        /// Event raised after JSON data has been persisted to disk.
        /// </summary>
        private event EventHandler DataSaved;

        /// <summary>
        /// Executes an action within a critical section to guard shared state (data object,
        /// internal flags and timers) against concurrent access.
        /// </summary>
        /// <param name="a">Action to execute under lock.</param>
        private void WithLock(Action a)
        {
            _sync.Enter();
            try
            {
                a();
            }
            catch (Exception ex)
            {
                Debug.LogWarning(this, "Caught an exception within the lock:{0}", ex);
                throw;
            }
            finally
            {
                _sync.Leave();
            }
        }

        /// <summary>
        /// Loads a JSON file if present; otherwise creates it (seeding with provided JSON or empty object)
        /// and returns the resulting <see cref="JObject"/>.
        /// </summary>
        /// <param name="fileName">Relative file path (within Essentials Global.FilePathPrefix).</param>
        /// <param name="seed">Optional seed object to write when the file does not yet exist.</param>
        /// <returns>The loaded or newly created JSON object.</returns>
        private static JObject SeedData(string fileName, JObject seed)
        {
            var filePath = Path.Combine(Global.FilePathPrefix, fileName);
            Debug.LogInformation("CustomValues", "Attemping to find a file at path:{0}", filePath);

            if (File.Exists(filePath))
            {
                using (var fs = File.OpenRead(filePath))
                using (var stream = new StreamReader(fs))
                using (var json = new JsonTextReader(stream))
                {
                    return JObject.Load(json);
                }
            }

            Debug.LogInformation("CustomValues", "Didn't find a file at path:{0}, creating...", filePath);
            var dataToSeed = seed ?? new JObject();
            using (var fs = File.Create(filePath))
            using (var stream = new StreamWriter(fs))
            using (var writer = new JsonTextWriter(stream))
            {
                dataToSeed.WriteTo(writer);
            }

            return dataToSeed;
        }

        /// <summary>
        /// Persists JSON data atomically by writing to a temporary file first and then replacing
        /// the target file. Falls back to direct truncation write if replacement fails.
        /// </summary>
        /// <param name="fileName">Relative file path (within Essentials Global.FilePathPrefix).</param>
        /// <param name="data">JSON token to serialize.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is null.</exception>
        private static void SaveData(string fileName, JToken data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            var filePath = Path.Combine(Global.FilePathPrefix, fileName);
            var tempPath = filePath + ".tmp";

            Debug.LogInformation("CustomValues", "Attempting to write a file at path:{0}", filePath);

            // Write to temp file first to avoid partial/corrupt writes
            using (var fs = File.Create(tempPath)) // FileMode.Create -> truncates or creates new
            using (var stream = new StreamWriter(fs))
            using (var writer = new JsonTextWriter(stream))
            {
                data.WriteTo(writer);
                writer.Flush();
                stream.Flush();
                fs.Flush();
            }

            try
            {
                // Replace original atomically (best effort)
                if (File.Exists(filePath))
                    File.Delete(filePath);

                File.Move(tempPath, filePath);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("CustomValues", "Failed replacing original file with temp: {0}", ex);
                // Fallback: attempt direct write (truncating) if temp replace failed
                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                using (var stream = new StreamWriter(fs))
                using (var writer = new JsonTextWriter(stream))
                {
                    data.WriteTo(writer);
                }
            }
            finally
            {
                // Clean up stray temp file if it still exists
                if (File.Exists(tempPath))
                {
                    try { File.Delete(tempPath); } catch { }
                }
            }
        }
    }
}