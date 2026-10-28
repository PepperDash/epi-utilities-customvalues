using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.Plugins
{

	/// <summary>
	/// Plugin device factory for the CustomValues logic device
	/// </summary>
	public class CustomValuesDeviceFactory : EssentialsPluginDeviceFactory<CustomValuesController>
	{
		/// <summary>
		/// Plugin device factory constructor
		/// </summary>
		public CustomValuesDeviceFactory()
		{
			// Set the minimum Essentials Framework Version (numeric core only; a prerelease suffix fails the loader's version parse)
			MinimumEssentialsFrameworkVersion = "3.0.0";

			// In the constructor we initialize the list with the typenames that will build an instance of this device
			TypeNames = new List<string>() { "CustomValues" };
		}

		/// <summary>
		/// Builds and returns an instance of CustomValuesController
		/// </summary>
		/// <inheritdoc />
		public override EssentialsDevice BuildDevice(PepperDash.Essentials.Core.Config.DeviceConfig dc)
		{
			Debug.LogDebug("CustomValues: factory attempting to create device {Key} from type {Type}", dc.Key, dc.Type);

			return new CustomValuesController(dc);
		}
	}
}

