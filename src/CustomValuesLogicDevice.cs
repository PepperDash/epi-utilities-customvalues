using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core.Devices;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Config;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp;


namespace UtilitiesCustomValues
{
	/// <summary>
	/// Plugin device template for logic devices that don't communicate outside the program
	/// </summary>
	public class CustomValuesDevice : ReconfigurableBridgableDevice, IBridgeAdvanced
	{
		/// <summary>
		/// It is often desirable to store the config
		/// </summary>
		private readonly DeviceConfig _config;
		public static CTimer WriteTimer;
		public const long WriteTimeout = 15000;
		public Dictionary<string, PepperDash.Essentials.Core.Feedback> Feedbacks;
		bool _initialized = false;

		private static readonly CCriticalSection FileLock = new CCriticalSection();

		private CustomValuesConfigObject Properties
		{
			get
			{
				try
				{
					return _config.Properties.ToObject<CustomValuesConfigObject>();
				}
				catch (Exception)
				{
					throw new FormatException(string.Format("ERROR:Unable to convert CustomValuesConfigObject\n}"));
				}
			}
		}

		private JObject _fileData;
		private JObject FileData
		{
			get
			{
				return UseFile ? _fileData : Properties.Data;
			}
			set
			{
				if (UseFile)
				{
					_fileData = value;
				}
				else
				{
					_config.Properties.ToObject<CustomValuesConfigObject>().Data = value;

				}
			}

		}

		private bool UseFile
		{
			get
			{
				return !string.IsNullOrEmpty(Properties.FilePath);
			}
		}

		/// <summary>
        /// Constructs the legacy logic device variant used primarily for simple value storage
        /// and bridging with delayed write semantics.
        /// </summary>
        /// <param name="key">Logical key for the device.</param>
        /// <param name="name">Friendly name.</param>
        /// <param name="config">Device configuration.</param>
		public CustomValuesDevice(string key, string name, DeviceConfig config)
			: base(config)
		{
			Debug.LogInformation(this, "Constructing new {0} instance", name);
			_config = config;
			CrestronConsole.AddNewConsoleCommand(ConsoleCommand, "CustomValues", "gets/sets a CustomValue [path] ([NewValue])", ConsoleAccessLevelEnum.AccessOperator);
			Feedbacks = new Dictionary<string, PepperDash.Essentials.Core.Feedback>();
		}

		/// <inheritdoc />
		public override bool CustomActivate()
		{
			if (!UseFile) return true;

			try
			{

				if (File.Exists(Global.FilePathPrefix + Properties.FilePath))
				{
					Debug.LogVerbose(this, "Reading existing file");
					FileData = JObject.Parse(FileIO.ReadDataFromFile(Properties.FilePath));
				}
				else
				{
					CreateFile();
				}
			}
			catch (Exception e)
			{
				Debug.LogInformation(this, "Error Processing File: {0}", e);
			}
			return true;
		}

		void CreateFile()
		{
			FileLock.Enter();
			try
			{
				Debug.LogVerbose(this, "Creating new file");
				var seed = Properties.Seed == null ? "{}" : Properties.Seed.ToString();

				FileIO.WriteDataToFile(seed, Properties.FilePath);

				var filePath = Properties.FilePath;
				Debug.LogInformation(this, "File created at path:{0}", filePath);
				var file = FileIO.GetFile(filePath);
				var data = FileIO.ReadDataFromFile(file);

				FileData = JObject.Parse(data);
				Debug.LogInformation(this, "Current data:{0}", FileData);
			}
			catch (Exception ex)
			{
				Debug.LogError(this, "Caught an exception creating a file:{0}", ex);
				throw;
			}
			finally
			{
				FileLock.Leave();
			}
		}

		private void WriteValue(string path, ushort value)
		{
			if (!_initialized) return;

			try
			{
				Debug.LogVerbose(this, "Writing data {0} {1}", path, value);

				JToken tokenToReplace;
				if (UseFile)
				{
					tokenToReplace = FileData.SelectToken(path);
					tokenToReplace.Replace(value);
				}
				else
				{
					tokenToReplace = _config.Properties["Data"].SelectToken(path);
					tokenToReplace.Replace(value);
				}
				WriteFile();

				Feedbacks[path].FireUpdate();
			}
			catch (Exception e)
			{
				Debug.LogError(this, "Error WriteValue: {0}", e);
			}
		}

		private void WriteValue(string path, string value)
		{
			if (String.IsNullOrEmpty(value)) return;

			Debug.LogVerbose(this, "Writing data {0} {1}", path, value);

			JToken tokenToReplace;
			if (UseFile)
			{
				tokenToReplace = FileData.SelectToken(path);
				tokenToReplace.Replace(value);
			}
			else
			{
				tokenToReplace = _config.Properties["Data"].SelectToken(path);
				tokenToReplace.Replace(value);
			}
			Feedbacks[path].FireUpdate();
			WriteFile();
		}

		private void WriteValue(string path, bool value)
		{
			Debug.LogVerbose(this, "Writing data {0} {1}", path, value);
			JToken tokenToReplace;
			if (UseFile)
			{
				tokenToReplace = FileData.SelectToken(path);
				tokenToReplace.Replace(value);
			}
			else
			{
				tokenToReplace = _config.Properties["Data"].SelectToken(path);
				tokenToReplace.Replace(value);
			}
			WriteFile();
			Feedbacks[path].FireUpdate();
		}

		private void WriteFile()
		{
			try
			{
				if (FileLock.TryEnter())
				{
					if (WriteTimer == null)
						WriteTimer = new CTimer(WriteFileNow, WriteTimeout);

					WriteTimer.Reset(WriteTimeout);
					Debug.LogDebug(this, "Config File write timer has been reset.");
				}
				else
				{
					Debug.LogError(this, "FileIO Unable to enter FileLock");
				}

			}
			catch (Exception e)
			{
				Debug.LogError(this, "Error: FileIO read failed: \r{0}", e);
			}
			finally
			{
				if (FileLock != null && !FileLock.Disposed)
					FileLock.Leave();

			}
		}

		private void WriteFileNow(object o)
		{
			WriteFileNow();
		}
		private void WriteFileNow()
		{
			if (UseFile)
			{
				FileIO.WriteDataToFile(JsonConvert.SerializeObject(FileData), Properties.FilePath);
			}
			else
			{
				SetConfig(_config);
			}
		}

		/// <summary>
        /// Console helper command allowing get/set of values via: customvalues [path] ([value])
        /// </summary>
        /// <param name="command">Command arguments string.</param>
		public void ConsoleCommand(string command)
		{
			if (string.IsNullOrEmpty(command))
			{
				Debug.LogInformation(this, "CustomValue Path [NewValue] command requires an argument for Path and optionally a NewValue");
				return;
			}
			var commandArray = command.Split(' ');
			var path = commandArray[0];

			if (commandArray.Length == 1)
			{
				Debug.LogInformation(this, "CustomValue Path:{0} Value:{1}", path, Properties.Data.SelectToken(path));
			}
			else if (commandArray.Length == 2)
			{
				var value = commandArray[1];
				try
				{
					var number = ushort.Parse(value);
					WriteValue(path, value);
					return;
				}
				catch
				{
					Debug.LogInformation(this, "CustomValue command requires an argument for Path and optionally a NewValue");
				}
				if (value.ToLower() == "true" || value.ToLower() == "false")
				{
					WriteValue(path, bool.Parse(value));
				}
				else
				{
					WriteValue(path, value);
				}
			}
		}

		/// <inheritdoc />
		public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
		{
			var joinMap = new EssentialsPluginBridgeJoinMapTemplate(joinStart);

			// This adds the join map to the collection on the bridge
			if (bridge != null)
			{
				bridge.AddJoinMap(Key, joinMap);
			}
			bridge.Eisc.OnlineStatusChange += Eisc_OnlineStatusChange;
			var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);


			if (customJoins == null)
			{
				Debug.LogInformation(this, "Custom Joins not found!!!");
				return;
			}

			Debug.LogDebug(this, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
			Debug.LogInformation(this, "Linking to Bridge Type {0}", GetType().Name);

			foreach (var j in customJoins)
			{
				var path = j.Key;
				var index = j.Value.JoinNumber;
				var join = (ushort)(index + joinStart - 1);
				Debug.LogInformation(this, "Attempting to map join:{0} to path:{1}", join, path);

				var value = UseFile ? FileData.SelectToken(path) : Properties.Data.SelectToken(path);
				Debug.LogInformation(this, "Mapping to:{0}", value.ToString());

				// Validity Checks
				if (index == 0)
				{
					Debug.LogInformation(this, "Missing Join number for path:{0}", path);
					continue;
				}

				if (value == null)
				{
					Debug.LogInformation(this, "Missing value in config for path:{0}", path);
					continue;
				}

				//var path = map.Path;
				Debug.LogInformation(this, "Mapping join:{0} to value:{1} with path:{2}", join, value.Type.ToString());

				if (value.Type == Newtonsoft.Json.Linq.JTokenType.Integer)
				{
					Debug.LogVerbose(this, "I AM INT");
					trilist.SetUShortSigAction(join, (x) =>
					{
						WriteValue(path, x);
					});

					var newFeedback = new IntFeedback(() => { return (ushort)FileData.SelectToken(path); });
					Feedbacks.Add(path, newFeedback);
					newFeedback.LinkInputSig(trilist.UShortInput[join]);
					newFeedback.FireUpdate();
				}
				else if (value.Type == Newtonsoft.Json.Linq.JTokenType.String)
				{
					Debug.LogVerbose(this, "I AM STRING");

					StringFeedback newFeedback;
					trilist.SetStringSigAction(join, (x) =>
							{
								WriteValue(path, x);
							});
					newFeedback = new StringFeedback(() => { return (string)FileData.SelectToken(path); });
					Feedbacks.Add(path, newFeedback);
					newFeedback.LinkInputSig(trilist.StringInput[join]);
					newFeedback.FireUpdate();
				}
				else if (value.Type == Newtonsoft.Json.Linq.JTokenType.Object)
				{
					Debug.LogVerbose(this, "I AM OBJECT");

					StringFeedback newFeedback;
					trilist.SetStringSigAction(join, (x) =>
					{
						WriteValue(path, x);
					});
					newFeedback = new StringFeedback(() =>
					{
						return FileData.SelectToken(path).ToString(Formatting.None);
					});

					Feedbacks.Add(path, newFeedback);
					newFeedback.LinkInputSig(trilist.StringInput[join]);
					newFeedback.FireUpdate();
				}
				else if (value.Type == Newtonsoft.Json.Linq.JTokenType.Boolean)
				{
					Debug.LogVerbose(this, "I AM BOOL");

					BoolFeedback newFeedback;
					newFeedback = new BoolFeedback(() => { return (bool)FileData.SelectToken(path); });
					Feedbacks.Add(path, newFeedback);
					newFeedback.LinkInputSig(trilist.BooleanInput[join]);
					newFeedback.FireUpdate();
				}
			}

		}

		/// <summary>
		/// Handles remote EISC online/offline events, deferring device initialization until the
		/// EISC has remained online for a brief period.
		/// </summary>
		/// <param name="currentDevice">The reporting device.</param>
		/// <param name="args">Online/offline event arguments.</param>
		void Eisc_OnlineStatusChange(Crestron.SimplSharpPro.GenericBase currentDevice, Crestron.SimplSharpPro.OnlineOfflineEventArgs args)
		{
			if (args.DeviceOnLine)
			{
				Debug.LogVerbose(this, "EISC ONLINE");
				var init = new CTimer((o) => { Debug.LogVerbose(this, "INITIALIZED"); _initialized = true; }, 10000);
			}
			else if (!args.DeviceOnLine)
			{
				_initialized = false;
				Debug.LogVerbose(this, "EISC OFFLINE");
			}
		}
	}
}

