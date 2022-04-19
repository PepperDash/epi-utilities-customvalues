using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core.Devices;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Config;
using Crestron.SimplSharpPro.EthernetCommunication;
using System;
using Newtonsoft.Json;

using Newtonsoft.Json.Linq;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp;


namespace Essentials.Plugin.CustomValues
{
	/// <summary>
	/// Plugin device template for logic devices that don't communicate outside the program
	/// </summary>
	/// <remarks>
	/// Rename the class to match the device plugin being developed.
	/// </remarks>
	/// <example>
    /// "EssentialsPluginTemplateLogicDevice" renamed to "SamsungMdcDevice"
	/// </example>
	public class CustomValuesDevice : ReconfigurableBridgableDevice, IBridgeAdvanced
    {
        /// <summary>
        /// It is often desirable to store the config
        /// </summary>
		private DeviceConfig _Config;
		private CustomValuesConfigObject _Properties
		{
			get
			{
				try
				{
					return _Config.Properties.ToObject<CustomValuesConfigObject>();
				}
				catch (Exception)
				{
					throw new FormatException(string.Format("ERROR:Unable to convert CustomValuesConfigObject\n}"));
				}
			}
		}

		private JObject FileData; 

		private bool UseFile
		{
			get
			{
				if (string.IsNullOrEmpty(_Properties.FilePath))
				{
					return false;
				}
				else
				{
					return true;
				}
			}
		}

		public CustomValuesDevice(string key, string name, DeviceConfig config)
            : base(config)
        {
            Debug.Console(0, this, "Constructing new {0} instance", name);
            _Config = config;
			CrestronConsole.AddNewConsoleCommand(ConsoleCommand, "CustomValue", "gets/sets a CustomValue path [NewValue]", ConsoleAccessLevelEnum.AccessOperator);

			if (UseFile)
			{
				AddPostActivationAction(() =>
				{
					PostActivationMethodObject();
				});
			}
        }



		object PostActivationMethodObject()
		{
			try
			{
				if (File.Exists(_Properties.FilePath))
				{
					FileData =  JObject.Parse(FileIO.ReadDataFromFile(FileIO.GetFile(_Properties.FilePath)));
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

			return null;
		}

		void CreateFile()
		{

			FileIO.WriteDataToFile("{}", _Properties.FilePath);
			FileData = JObject.Parse(FileIO.ReadDataFromFile(FileIO.GetFile(_Properties.FilePath)));
		}

		private void WriteValue(string path, ushort value)
		{
			Debug.Console(2, "Writing data {0} {1}", path, value);
			JToken tokenToReplace;
			if(UseFile) 
			{
				tokenToReplace = FileData.SelectToken(path);
				tokenToReplace.Replace(value);
				WriteFile();
			}
			else
			{
				tokenToReplace = _Config.Properties.SelectToken(path);
				tokenToReplace.Replace(value);
				SetConfig(_Config);
			}
			
		}

		private void WriteValue(string path, string value)
		{
			Debug.Console(2, "Writing data {0} {1}", path, value);
			JToken tokenToReplace;
			if (UseFile)
			{
				tokenToReplace = FileData.SelectToken(path);
				tokenToReplace.Replace(value);
				WriteFile();
			}
			else
			{
				tokenToReplace = _Config.Properties.SelectToken(path);
				tokenToReplace.Replace(value);
				SetConfig(_Config);
			}
		}

		private void WriteValue(string path, bool value)
		{
			Debug.Console(2, "Writing data {0} {1}", path, value);
			JToken tokenToReplace;
			if (UseFile)
			{
				tokenToReplace = FileData.SelectToken(path);
				tokenToReplace.Replace(value);
				WriteFile();
			}
			else
			{
				tokenToReplace = _Config.Properties.SelectToken(path);
				tokenToReplace.Replace(value);
				SetConfig(_Config);
			}
		}


		private void WriteFile()
		{
			FileIO.WriteDataToFile(JsonConvert.SerializeObject(FileData) , _Properties.FilePath);
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
				Debug.Console(0, this, "CustomValue Path:{0} Value:{1}", path, _Properties.Data.SelectToken(path));
			}
			else if (commandArray.Length == 2)
			{
				var value = commandArray[1];
				try
				{
					var Number = ushort.Parse(value);
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
					return;
				}
				else 
				{
					WriteValue(path, value);
					return;
				}

			}

		}

		public override bool CustomActivate()
		{
			return true; 
		}

        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = new EssentialsPluginBridgeJoinMapTemplate(joinStart);

            // This adds the join map to the collection on the bridge
            if (bridge != null)
            {
                bridge.AddJoinMap(Key, joinMap);
            }
			// bridge.CommunicationMonitor.StatusChange(
            var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);

			
            if (customJoins == null)
            {
				Debug.Console(0, "Custom Joins not found!!!");
            }
			

            Debug.Console(1, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
            Debug.Console(0, "Linking to Bridge Type {0}", GetType().Name);

			//Debug.Console(0, "TEST {0}", customJoins.);

			foreach (var j in customJoins)
			{
				
				//var parts = path.Key.Split('-');
				//var type = parts[0];
				var path = j.Key;
				uint? index = j.Value.JoinNumber;
				JToken value;
				if (UseFile)
				{
					value = FileData.SelectToken(path);
				}
				else
				{
					value = _Properties.Data.SelectToken(path);
				}
				if (index == 0)
				{
					Debug.Console(0, "Missing Join number for Key {0}", path);
					continue;
				}
				if (value == null)
				{
					Debug.Console(0, "Cannot find value for Key {0}", path);
					continue;
				}

				
				//var path = map.Path;
				ushort join = (ushort)(index + joinStart - 1); 

				Debug.Console(2, "Read and mapped data {0} {1} {2}",  value, join, value.Type.ToString());
				if (value.Type == Newtonsoft.Json.Linq.JTokenType.Integer)
				{
					Debug.Console(2, "I AM INT");
					
					trilist.SetUShortSigAction(join, (x) => 
						{
							WriteValue(path, x);
							if (UseFile) { trilist.UShortInput[join].UShortValue = (ushort)FileData.SelectToken(path); }
							else { trilist.UShortInput[join].UShortValue = (ushort)_Properties.Data.SelectToken(path); }
						});

					trilist.UShortInput[join].UShortValue = (ushort)value;
				}
				else if (value.Type == Newtonsoft.Json.Linq.JTokenType.String) 
				{
					Debug.Console(2, "I AM STRING");
					
					trilist.SetStringSigAction(join, (x) =>
							{
								WriteValue(path, x);
								if (UseFile) { trilist.StringInput[join].StringValue = (string)FileData.SelectToken(path); }
								else { trilist.StringInput[join].StringValue = ((string)_Properties.Data.SelectToken(path)); }
							});
					trilist.StringInput[join].StringValue = (string)value;
				}
				else if (value.Type == Newtonsoft.Json.Linq.JTokenType.Object)
				{
					Debug.Console(2, "I AM STRING");
					
					trilist.SetStringSigAction(join, (x) =>
					{
						WriteValue(path, x);
						if (UseFile) { trilist.StringInput[join].StringValue = (string)FileData.SelectToken(path); }
						else { trilist.StringInput[join].StringValue = ((string)_Properties.Data.SelectToken(path)); } 
						
					});
					trilist.StringInput[join].StringValue = value.ToString(Newtonsoft.Json.Formatting.None);
				}
				else if (value.Type == Newtonsoft.Json.Linq.JTokenType.Boolean)
				{
					Debug.Console(2, "I AM BOOL");
					
					trilist.SetStringSigAction(join, (x) =>
					{
						WriteValue(path, x);
						if (UseFile) { trilist.BooleanInput[join].BoolValue = (bool)FileData.SelectToken(path); }
						else { trilist.BooleanInput[join].BoolValue = ((bool)_Properties.Data.SelectToken(path)); }
					});
					trilist.StringInput[join].StringValue = value.ToString(Newtonsoft.Json.Formatting.None);
				}
			}
            trilist.SetString(joinMap.DeviceName.JoinNumber, Name);

            trilist.OnlineStatusChange += (o, a) =>
            {
                if (!a.DeviceOnLine) return;

                trilist.SetString(joinMap.DeviceName.JoinNumber, Name);
            };
        }


    }
}

