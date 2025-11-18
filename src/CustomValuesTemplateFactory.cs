using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace UtilitiesCustomValues
{

	/// <summary>
	/// Plugin device factory for logic devices that don't communicate
	/// </summary>
	public class EssentialsPluginFactoryLogicDeviceTemplate : EssentialsPluginDeviceFactory<CustomValuesDevice>
	{
		/// <summary>
		/// Plugin device factory constructor
		/// </summary>
		public EssentialsPluginFactoryLogicDeviceTemplate()
		{
			// Set the minimum Essentials Framework Version			
			MinimumEssentialsFrameworkVersion = "2.15.0";

			// In the constructor we initialize the list with the typenames that will build an instance of this device
			TypeNames = new List<string>() { "CustomValues" };
		}

		/// <summary>
		/// Builds and returns an instance of EssentialsPluginTemplateLogicDevice
		/// </summary>
		/// <inheritdoc />
		public override EssentialsDevice BuildDevice(PepperDash.Essentials.Core.Config.DeviceConfig dc)
		{
			Debug.LogDebug("CustomValues", "[{0}] Factory Attempting to create new device from type: {1}", dc.Key, dc.Type);

			return new CustomValuesController(dc);
		}
	}
}

