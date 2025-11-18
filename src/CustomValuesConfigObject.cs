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

		// NOTE: legacyDigitalJoinBehavior and trackChangesWhileSavingDisabled removed.
		// Behavior is now fixed: boolean data joins always offset (start at 101) and
		// changes while saving disabled are always tracked in memory and flushed when saving is re-enabled.
	}
}