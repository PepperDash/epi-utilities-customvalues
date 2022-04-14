using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core.Devices;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Config;
using Crestron.SimplSharpPro.EthernetCommunication;
using System;
using Newtonsoft.Json.Linq;

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

		public CustomValuesDevice(string key, string name, DeviceConfig config)
            : base(config)
        {
            Debug.Console(0, this, "Constructing new {0} instance", name);
            _Config = config;
        }

		private void WriteValue(string path, ushort value)
		{
			Debug.Console(2, "Writing data {0} {1}", path, value);
			_Config.Properties["Data"][path] = value;
			SetConfig(_Config);
		}

		private void WriteValue(string path, string value)
		{
			Debug.Console(2, "Writing data {0} {1}", path, value);
			_Config.Properties["Data"][path] = value;
			SetConfig(_Config);
			
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

            if (customJoins != null)
            {
                joinMap.SetCustomJoinData(customJoins);
            }

            Debug.Console(1, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
            Debug.Console(0, "Linking to Bridge Type {0}", GetType().Name);

			foreach (var path in _Properties.Paths)
			{
				
				var parts = path.Key.Split('-');
				var type = parts[0];
				var index = ushort.Parse(parts[1]); 

				var value = _Properties.Data.SelectToken(path.Value);
				//var path = map.Path;
				ushort join = (ushort)(index + joinStart - 1); 

				Debug.Console(2, "Read and mapped data {0} {1} {2} {3}", type, value, join, value.Type.ToString());
				if (value.Type == Newtonsoft.Json.Linq.JTokenType.Integer)
				{
					Debug.Console(2, "I AM INT");
					trilist.UShortInput[join].UShortValue = (ushort)value;
					trilist.SetUShortSigAction(join, (x) => 
						{
							WriteValue(path.Value, x);
							trilist.UShortInput[join].UShortValue = (ushort)_Properties.Data.SelectToken(path.Value); 
						});

				}
				else if (value.Type == Newtonsoft.Json.Linq.JTokenType.String) 
				{
					Debug.Console(2, "I AM STRING");
					trilist.StringInput[join].StringValue = (string)value;
					trilist.SetStringSigAction(join, (x) =>
							{
								WriteValue(path.Value, x);
								trilist.StringInput[join].StringValue = (string)_Properties.Data.SelectToken(path.Value);
							});
				}
				else if (value.Type == Newtonsoft.Json.Linq.JTokenType.Object)
				{
					Debug.Console(2, "I AM STRING");
					trilist.StringInput[join].StringValue = value.ToString(Newtonsoft.Json.Formatting.None);
					trilist.SetStringSigAction(join, (x) =>
					{
						WriteValue(path.Value, x);
						trilist.StringInput[join].StringValue = (string)_Properties.Data.SelectToken(path.Value);
					});


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

