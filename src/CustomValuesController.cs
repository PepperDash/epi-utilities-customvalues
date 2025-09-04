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
    public class CustomValuesController : ReconfigurableDevice, IBridgeAdvanced
    {
        private readonly CustomValuesConfigObject _props;
        private readonly CTimer _saveTimer;
        private readonly CCriticalSection _sync = new CCriticalSection();

        private JObject data;

        public CustomValuesController(DeviceConfig config)
            : base(config)
        {
            _saveTimer = new CTimer(_ => WithLock(() =>
            {
                if (string.IsNullOrEmpty(_props.FilePath))
                {
                    Debug.Console(0, this, "File path not specified... saving values locally");
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
                    Debug.Console(0, this, Debug.ErrorLogLevel.Warning, "Failed to save custom values data: '{0}'", ex);
                }
            }),
                Timeout.Infinite);

            _props = config.Properties.ToObject<CustomValuesConfigObject>();

            data = string.IsNullOrEmpty(_props.FilePath)
                ? _props.Data
                : SeedData(_props.FilePath, _props.Seed);

            if (data == null)
            {
                Debug.Console(0, this, "No data found in the config or file, using an empty JObject.");
                data = new JObject();
            }
            else
            {
                Debug.Console(1, this, "Loaded data from file: {0}", _props.FilePath);
            }
        }

        public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            if (data == null)
                throw new NullReferenceException("LinkToApi: data is null.  Check file path or a 'data' object is defined in the config");

            var feedbacks = new List<Feedback>();
            DataSaved += (sender, args) => feedbacks.ForEach(f => f.FireUpdate());

            var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);
            if (customJoins == null)
            {
                Debug.Console(0, this, "Custom Joins not found!!!");
                return;
            }

            Debug.Console(1, this, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
            Debug.Console(1, this, "Linking to Bridge Type {0}", GetType().Name);

            foreach (var customJoin in customJoins)
            {
                var path = customJoin.Key;
                var index = customJoin.Value.JoinNumber;
                var join = (ushort)(index + joinStart - 1);

                if (index == 0)
                {
                    Debug.Console(0, this, "Cannot map path: '{0}', missing join number", path);
                    continue;
                }

                Debug.Console(1, this, "Attempting to map path: '{1}' to join: '{0}'", join, path);
                var value = data.SelectToken(path);
                if (value == null)
                {
                    Debug.Console(0, this, "No value found for path: '{0}' in '{1}', ignoring path, update customValues if needed.", path, _props.FilePath);
                    continue;
                }

                Debug.Console(1, this, "Mapping path: '{1}' to join: '{0}' as type: '{2}' with value: '{3}'",
                    join, path, value.Type.ToString(), value);

                switch (value.Type)
                {
                    case JTokenType.Integer:
                        {
                            trilist.SetUShortSigAction(@join, x => WithLock(() =>
                            {
                                data.SelectToken(path).Replace(x);
                                _saveTimer.Reset(1000);
                            }));

                            var feedback = new IntFeedback(() => data.SelectToken(path).Value<int>());
                            feedback.LinkInputSig(trilist.UShortInput[@join]);
                            feedbacks.Add(feedback);
                            feedback.FireUpdate();
                            feedback.OutputChange +=
                                (sender, args) =>
                                    Debug.Console(1, this, "Value for path:{0} updated to {1}", path, args.IntValue);

                            break;
                        }
                    case JTokenType.String:
                        {
                            trilist.SetStringSigAction(@join, x => WithLock(() =>
                            {
                                data.SelectToken(path).Replace(x);
                                _saveTimer.Reset(1000);
                            }));

                            var feedback = new StringFeedback(() => data.SelectToken(path).Value<string>());
                            feedback.LinkInputSig(trilist.StringInput[@join]);
                            feedbacks.Add(feedback);
                            feedback.FireUpdate();
                            feedback.OutputChange +=
                                (sender, args) =>
                                    Debug.Console(1, this, "Value for path:{0} updated to {1}", path, args.StringValue);

                            break;
                        }
                    case JTokenType.Object:
                        {
                            trilist.SetStringSigAction(@join, x =>
                            {
                                data.SelectToken(path).Replace(x);
                                _saveTimer.Reset(1000);
                            });

                            var feedback = new StringFeedback(() => data.SelectToken(path).Value<string>());
                            feedback.LinkInputSig(trilist.StringInput[@join]);
                            feedbacks.Add(feedback);
                            feedback.FireUpdate();
                            feedback.OutputChange +=
                                (sender, args) =>
                                    Debug.Console(1, this, "Value for path:{0} updated to {1}", path, args.StringValue);

                            break;
                        }
                    case JTokenType.Boolean:
                        {
                            trilist.SetBoolSigAction(@join, x =>
                            {
                                data.SelectToken(path).Replace(x);
                                _saveTimer.Reset(1000);
                            });

                            var feedback = new BoolFeedback(() => data.SelectToken(path).Value<bool>());
                            feedback.LinkInputSig(trilist.BooleanInput[@join]);
                            feedbacks.Add(feedback);
                            feedback.FireUpdate();
                            feedback.OutputChange +=
                                (sender, args) =>
                                    Debug.Console(1, this, "Value for path:{0} updated to {1}", path, args.BoolValue);

                            break;
                        }
                    default:
                        {
                            Debug.Console(0, this, "Cannot map path: '{0}', unsupported type: {1}", path, value.Type);
                            continue;
                        }
                }
            }
        }

        private event EventHandler DataSaved;

        private void WithLock(Action a)
        {
            _sync.Enter();
            try
            {
                a();
            }
            catch (Exception ex)
            {
                Debug.Console(0, Debug.ErrorLogLevel.Warning, "Caught an exception within the lock:{0}", ex);
                throw;
            }
            finally
            {
                _sync.Leave();
            }
        }

        private static JObject SeedData(string fileName, JObject seed)
        {
            var filePath = Path.Combine(Global.FilePathPrefix, fileName);
            Debug.Console(0, "Attemping to find a file at path:{0}", filePath);

            if (File.Exists(filePath))
            {
                using (var fs = File.OpenRead(filePath))
                using (var stream = new StreamReader(fs))
                using (var json = new JsonTextReader(stream))
                {
                    return JObject.Load(json);
                }
            }

            Debug.Console(0, "Didn't find a file at path:{0}, creating...", filePath);
            var dataToSeed = seed ?? new JObject();
            using (var fs = File.Create(filePath))
            using (var stream = new StreamWriter(fs))
            using (var writer = new JsonTextWriter(stream))
            {
                dataToSeed.WriteTo(writer);
            }

            return dataToSeed;
        }

        // Updated to ensure file is fully replaced (prevent trailing stale JSON)
        private static void SaveData(string fileName, JToken data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            var filePath = Path.Combine(Global.FilePathPrefix, fileName);
            var tempPath = filePath + ".tmp";

            Debug.Console(0, "Attempting to write a file at path:{0}", filePath);

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
                Debug.Console(0, Debug.ErrorLogLevel.Warning, "Failed replacing original file with temp: {0}", ex);
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

            //Debug.Console(0, "Wrote {1} at path:{0}", filePath, fileName);
            //Debug.Console(0, "File Data: {0}", data.ToString(Formatting.None));
        }
    }
}