using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;

namespace UtilitiesCustomValues
{
	/// <summary>
	/// Plugin device Bridge Join Map
	/// </summary>
	public class EssentialsPluginBridgeJoinMapTemplate : JoinMapBaseAdvanced
	{
		#region Control Joins
        /// <summary>
        /// Digital input join that gates persistence (HIGH allows saves, LOW blocks file writes).
        /// </summary>
		[JoinName("enableSaving")]
		public JoinDataComplete EnableSaving = new JoinDataComplete(
			new JoinData { JoinNumber = 1, JoinSpan = 1 },
			new JoinMetadata
			{
				Description = "Enable saving gate (HIGH allows persistence)",
				JoinCapabilities = eJoinCapabilities.FromSIMPL,
				JoinType = eJoinType.Digital
			});

        /// <summary>
        /// Digital output feedback indicating both join mapping completion and that saving is enabled.
        /// </summary>
		[JoinName("savingReadyFb")]
		public JoinDataComplete SavingReadyFb = new JoinDataComplete(
			new JoinData { JoinNumber = 2, JoinSpan = 1 }, // Distinct output join (separate from input 1)
			new JoinMetadata
			{
				Description = "Feedback: plugin mapped and saving enabled",
				JoinCapabilities = eJoinCapabilities.ToSIMPL,
				JoinType = eJoinType.Digital
			});
		#endregion

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