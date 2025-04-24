using PepperDash.Essentials.Core;

namespace Essentials.Plugin.CustomValues
{
	/// <summary>
	/// Plugin device Bridge Join Map
	/// </summary>
	/// <remarks>
	/// Rename the class to match the device plugin being developed.  Reference Essentials JoinMaps, if one exists for the device plugin being developed
	/// </remarks>
	/// <see cref="PepperDash.Essentials.Core.Bridges"/>
	/// <example>
	/// "EssentialsPluginBridgeJoinMapTemplate" renamed to "SamsungMdcBridgeJoinMap"
	/// </example>
	public class EssentialsPluginBridgeJoinMapTemplate : JoinMapBaseAdvanced
	{
		/// <summary>
		/// Plugin device BridgeJoinMap constructor
		/// </summary>
		/// <param name="joinStart">This will be the join it starts on the EISC bridge</param>
		public EssentialsPluginBridgeJoinMapTemplate(uint joinStart)
			: base(joinStart, typeof(EssentialsPluginBridgeJoinMapTemplate))
		{
		}
	}
}