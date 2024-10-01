using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Config;

namespace Essentials.Plugin.CustomValues
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

		/// <summary>
		/// Constuctor
		/// </summary>
		/// <remarks>
		/// If using a collection you must instantiate the collection in the constructor
		/// to avoid exceptions when reading the configuration file 
		/// </remarks>
		public CustomValuesConfigObject()
		{
			
		}
	}

	public class CustomValuesProps
	{


	}


}