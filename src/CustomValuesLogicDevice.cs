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

		public CustomValuesDevice(string key, string name, DeviceConfig config)
			: base(config)
		{
			Debug.Console(0, this, "Constructing new {0} instance", name);
			_config = config;
			CrestronConsole.AddNewConsoleCommand(ConsoleCommand, "CustomValues", "gets/sets a CustomValue [path] ([NewValue])", ConsoleAccessLevelEnum.AccessOperator);
			Feedbacks = new Dictionary<string, PepperDash.Essentials.Core.Feedback>();
		}

		public override bool CustomActivate()
		{
			if (!UseFile) return true;

			try
			{

				if (File.Exists(Global.FilePathPrefix + Properties.FilePath))
				{
					Debug.Console(2, this, "Reading exsisting file");
					FileData = JObject.Parse(FileIO.ReadDataFromFile(Properties.FilePath));
				}
				else
				{
					CreateFile();
				}
			}
			catch (Exception e)
			{
				Debug.Console(0, this, "Error Processing File: {0}", e);
			}
			return true;
		}

		void CreateFile()
		{
			FileLock.Enter();
			try
			{
				Debug.Console(2, this, "Creating new file");
				var seed = Properties.Seed == null ? "{}" : Properties.Seed.ToString();

				FileIO.WriteDataToFile(seed, Properties.FilePath);

				var filePath = Properties.FilePath;
				Debug.Console(0, this, "File created at path:{0}", filePath);
				var file = FileIO.GetFile(filePath);
				var data = FileIO.ReadDataFromFile(file);

				FileData = JObject.Parse(data);
				Debug.Console(0, this, "Current data:{0}", FileData);
			}
			catch (Exception ex)
			{
				Debug.Console(0, Debug.ErrorLogLevel.Error, "Caught an exception creating a file:{0}", ex);
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
				Debug.Console(2, this, "Writing data {0} {1}", path, value);

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
				Debug.Console(0, this, "Error WriteValue: {0}", e);
			}
		}

		private void WriteValue(string path, string value)
		{
			if (String.IsNullOrEmpty(value)) return;

			Debug.Console(2, this, "Writing data {0} {1}", path, value);

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
			Debug.Console(2, this, "Writing data {0} {1}", path, value);
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
					Debug.Console(1, "Config File write timer has been reset.");
				}
				else
				{
					Debug.Console(0, Debug.ErrorLogLevel.Error, "FileIO Unable to enter FileLock");
				}

			}
			catch (Exception e)
			{
				Debug.Console(0, Debug.ErrorLogLevel.Error, "Error: FileIO read failed: \r{0}", e);
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


		public void ConsoleCommand(string command)
		{
			if (string.IsNullOrEmpty(command))
			{
				Debug.Console(0, this, "CustomValue Path [NewValue] command requires an argument for Path and optionally a NewValue");
				return;
			}
			var commandArray = command.Split(' ');
			var path = commandArray[0];

			if (commandArray.Length == 1)
			{
				Debug.Console(0, this, "CustomValue Path:{0} Value:{1}", path, Properties.Data.SelectToken(path));
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
					Debug.Console(0, this, "CustomValue command requires an argument for Path and optionally a NewValue");
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
				Debug.Console(0, this, "Custom Joins not found!!!");
				return;
			}

			Debug.Console(1, this, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
			Debug.Console(0, this, "Linking to Bridge Type {0}", GetType().Name);

			foreach (var j in customJoins)
			{
				var path = j.Key;
				var index = j.Value.JoinNumber;
				var join = (ushort)(index + joinStart - 1);
				Debug.Console(0, this, "Attempting to map join:{0} to path:{1}", join, path);

				var value = UseFile ? FileData.SelectToken(path) : Properties.Data.SelectToken(path);
				Debug.Console(0, this, "Mapping to:{0}", value.ToString());

				// Validity Checks
				if (index == 0)
				{
					Debug.Console(0, this, "Missing Join number for path:{0}", path);
					continue;
				}

				if (value == null)
				{
					Debug.Console(0, this, "Missing value in config for path:{0}", path);
					continue;
				}

				//var path = map.Path;
				Debug.Console(0, this, "Mapping join:{0} to value:{1} with path:{2}", join, value.Type.ToString());

				if (value.Type == Newtonsoft.Json.Linq.JTokenType.Integer)
				{
					Debug.Console(2, this, "I AM INT");
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
					Debug.Console(2, this, "I AM STRING");

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
					Debug.Console(2, this, "I AM OBJECT");

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
					Debug.Console(2, this, "I AM BOOL");

					BoolFeedback newFeedback;
					newFeedback = new BoolFeedback(() => { return (bool)FileData.SelectToken(path); });
					Feedbacks.Add(path, newFeedback);
					newFeedback.LinkInputSig(trilist.BooleanInput[join]);
					newFeedback.FireUpdate();
				}
			}

		}

		void Eisc_OnlineStatusChange(Crestron.SimplSharpPro.GenericBase currentDevice, Crestron.SimplSharpPro.OnlineOfflineEventArgs args)
		{
			if (args.DeviceOnLine)
			{
				Debug.Console(2, this, "EISC ONLINE");
				var init = new CTimer((o) => { Debug.Console(2, this, "INITIALIZED"); _initialized = true; }, 10000);
			}
			else if (!args.DeviceOnLine)
			{
				_initialized = false;
				Debug.Console(2, this, "EISC OFFLINE");
			}
		}
	}
}

