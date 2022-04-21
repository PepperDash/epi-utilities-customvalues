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


		[JsonProperty("filePath")]
		public string FilePath { get; set; }

		[JsonProperty("data")]
		public JObject Data { get; set; }

		[JsonProperty("paths")]
		public Dictionary<string, string> Paths { get; set; }

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




	// May not be used 
	public class SimplBridge
	{
		[JsonProperty("ipid")]
		public string Ipid { get; set; }

		[JsonProperty("mappings")]
		public List<PathToJoinMapping> Mappings { get; set; }

	}

	public class PathToJoinMapping
	{
		[JsonProperty("join")]
		public ushort Join { get; set; }

		[JsonProperty("path")]
		public string Path { get; set; }

	}

}