using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace UtilitiesCustomValues
{
	/// <summary>
	/// Plugin device configuration object
	/// </summary>
	public class CustomValuesConfigObject
	{
		[JsonProperty("seed")]
		public JObject Seed { get; set; }

		[JsonProperty("filePath")]
		public string FilePath { get; set; }

		[JsonProperty("data")]
		public JObject Data { get; set; }
	}
}