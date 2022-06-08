using System.Collections.Generic;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using Crestron.SimplSharpPro.UI;

namespace Essentials.Plugin.CustomValues
{

    /// <summary>
    /// Plugin device factory for logic devices that don't communicate
    /// </summary>
    /// <remarks>
    /// Rename the class to match the device plugin being developed
    /// </remarks>
    /// <example>
    /// "EssentialsPluginFactoryTemplate" renamed to "MyLogicDeviceFactory"
    /// </example>
	public class EssentialsPluginFactoryLogicDeviceTemplate : EssentialsPluginDeviceFactory<CustomValuesDevice>
    {
		/// <summary>
		/// Plugin device factory constructor
		/// </summary>
		/// <remarks>
		/// Update the MinimumEssentialsFrameworkVersion & TypeNames as needed when creating a plugin
		/// </remarks>
		/// <example>
 		/// Set the minimum Essentials Framework Version
		/// <code>
		/// MinimumEssentialsFrameworkVersion = "1.6.4;
        /// </code>
		/// In the constructor we initialize the list with the typenames that will build an instance of this device
        /// <code>
		/// TypeNames = new List<string>() { "SamsungMdc", "SamsungMdcDisplay" };
        /// </code>
		/// </example>
        public EssentialsPluginFactoryLogicDeviceTemplate()
        {
            // Set the minimum Essentials Framework Version
			// TODO [ ] Update the Essentials minimum framework version which this plugin has been tested against
			MinimumEssentialsFrameworkVersion = "1.10.0";

            // In the constructor we initialize the list with the typenames that will build an instance of this device
			// TODO [ ] Update the TypeNames for the plugin being developed
            TypeNames = new List<string>() { "CustomValues" };
        }
        
		/// <summary>
        /// Builds and returns an instance of EssentialsPluginTemplateLogicDevice
		/// </summary>
		/// <param name="dc">device configuration</param>
		/// <returns>plugin device or null</returns>
		/// <remarks>		
		/// The example provided below takes the device key, name, properties config and the comms device created.
		/// Modify the EssetnialsPlugingDeviceTemplate constructor as needed to meet the requirements of the plugin device.
		/// </remarks>
		/// <seealso cref="PepperDash.Core.eControlMethod"/>
        public override EssentialsDevice BuildDevice(PepperDash.Essentials.Core.Config.DeviceConfig dc)
        {

            Debug.Console(1, "[{0}] Factory Attempting to create new device from type: {1}", dc.Key, dc.Type);

			return new CustomValuesDevice(dc.Key, dc.Name, dc);

        }
    }



}

          