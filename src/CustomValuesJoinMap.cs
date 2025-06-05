using PepperDash.Essentials.Core;

namespace UtilitiesCustomValues
{
	/// <summary>
	/// Plugin device Bridge Join Map
	/// </summary>
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