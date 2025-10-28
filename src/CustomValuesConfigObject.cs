using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace UtilitiesCustomValues
{
	/// <summary>
	/// Plugin device configuration object
	/// </summary>
	public class CustomValuesConfigObject
	{
        /// <summary>
        /// Optional seed object written to disk if a file path is provided and the file does not yet exist.
        /// Ignored after initial creation; subsequent loads use file contents.
        /// </summary>
		[JsonProperty("seed")]
		public JObject Seed { get; set; }

        /// <summary>
        /// Relative path (under the Essentials Global.FilePathPrefix) used for persistence. When omitted,
        /// data operates purely in-memory using the <see cref="Data"/> object.
        /// </summary>
		[JsonProperty("filePath")]
		public string FilePath { get; set; }

        /// <summary>
        /// In-memory JSON data structure consumed when no <see cref="FilePath"/> is provided, or seed for
        /// runtime state prior to any persisted modifications.
        /// </summary>
		[JsonProperty("data")]
		public JObject Data { get; set; }

		/// <summary>
		/// When true, preserves legacy digital join numbering (no offset). Default TRUE now (legacy compatibility) => digitals start at 101 only when this is false.
		/// Optional in config; missing property will assume TRUE for backward compatibility with older Essentials projects that did not include this property.
		/// </summary>
        [JsonProperty("legacyDigitalJoinBehavior", DefaultValueHandling = DefaultValueHandling.Populate)]
		[System.ComponentModel.DefaultValue(true)]
		public bool LegacyDigitalJoinBehavior { get; set; }

		/// <summary>
		/// When true, bridge-originated value changes while EnableSaving is LOW will update in-memory JSON (but still not persist to file). Default TRUE now (legacy compatibility) — previously default was false. When false, changes while disabled are ignored.
		/// Optional in config; missing property will assume TRUE for backward compatibility with older deployments that expect legacy behavior of tracking updates in memory even while saving is disabled.
		/// </summary>
        [JsonProperty("trackChangesWhileSavingDisabled", DefaultValueHandling = DefaultValueHandling.Populate)]
		[System.ComponentModel.DefaultValue(true)]
		public bool TrackChangesWhileSavingDisabled { get; set; }
	}
}