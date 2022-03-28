using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using PepperDash.Essentials.Core;

namespace Essentials.Plugin.CustomValues
{
	/// <summary>
	/// Plugin device configuration object
	/// </summary>
	public class EssentialsPluginConfigObject
	{
		[JsonProperty("filePath")]
		public uint FilePath { get; set; }
		
		[JsonProperty("data")]
		public JObject Data { get; set; }
		

		/// <summary>
		/// Constuctor
		/// </summary>
		/// <remarks>
		/// If using a collection you must instantiate the collection in the constructor
		/// to avoid exceptions when reading the configuration file 
		/// </remarks>
		public EssentialsPluginConfigObject()
		{

		}
	}

}