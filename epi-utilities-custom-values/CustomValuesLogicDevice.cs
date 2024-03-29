﻿using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core.Devices;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Config;
using Crestron.SimplSharpPro.EthernetCommunication;
using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using PepperDash.Essentials.Core.Feedbacks;
using Crestron.SimplSharp.CrestronIO;
using Crestron.SimplSharp;


namespace Essentials.Plugin.CustomValues
{
	/// <summary>
	/// Plugin device template for logic devices that don't communicate outside the program
	/// </summary>
	/// <remarks>
	/// Rename the class to match the device plugin being developed.
	/// </remarks>
	/// <example>
    /// "EssentialsPluginTemplateLogicDevice" renamed to "SamsungMdcDevice"
	/// </example>
	public class CustomValuesDevice : ReconfigurableBridgableDevice, IBridgeAdvanced
    {
        /// <summary>
        /// It is often desirable to store the config
        /// </summary>
		private DeviceConfig _Config;
        public static CTimer _WriteTimer;
        public const long WriteTimeout = 15000;
		public Dictionary<string, PepperDash.Essentials.Core.Feedback> Feedbacks;
        bool Initialized = false;
        static CCriticalSection fileLock = new CCriticalSection();

		private CustomValuesConfigObject _Properties
		{
			get
			{
				try
				{
					return _Config.Properties.ToObject<CustomValuesConfigObject>();
				}
				catch (Exception)
				{
					throw new FormatException(string.Format("ERROR:Unable to convert CustomValuesConfigObject\n}"));
				}
			}
		}


		private JObject _FileData;
		private JObject FileData
		{
			get
			{
				if(UseFile)
				{
					return _FileData;
				}
				else 
				{
					return _Properties.Data;
				}
			}
			set 
			{
				if (UseFile)
				{
					_FileData = value;
				}
				else
				{
					_Config.Properties.ToObject<CustomValuesConfigObject>().Data = value;
					
				}
			}

		}



		private bool UseFile
		{
			get
			{
				if (string.IsNullOrEmpty(_Properties.FilePath))
				{
					return false;
				}
				else
				{
					return true;
				}	
			}
		}

		public CustomValuesDevice(string key, string name, DeviceConfig config)
            : base(config)
        {
            Debug.Console(0, this, "Constructing new {0} instance", name);
            _Config = config;
			CrestronConsole.AddNewConsoleCommand(ConsoleCommand, "CustomValues", "gets/sets a CustomValue [path] ([NewValue])", ConsoleAccessLevelEnum.AccessOperator);
			Feedbacks = new Dictionary<string, PepperDash.Essentials.Core.Feedback>();
            
            //if (UseFile)
            //{
            //    AddPostActivationAction(() =>
            //    {
            //        PostActivationMethodObject();
            //    });
            //}
           
        }

        public override bool CustomActivate()
        {
            if (UseFile)
            {
                try
                {

                    if (File.Exists(Global.FilePathPrefix + _Properties.FilePath))
                    {
                        Debug.Console(2, this, "Reading exsisting file");
                        FileData = JObject.Parse(FileIO.ReadDataFromFile(_Properties.FilePath));
                    }
                    else
                    {
                        CreateFile();
                    }
                }
                catch (Exception e)
                {
                    Debug.Console(0, this, "Error Processing File: {0}", e);
                }
            }
            return true;
        }


        //object PostActivationMethodObject()
        //{
        //    try
        //    {
                
        //        if (File.Exists(Global.FilePathPrefix + _Properties.FilePath))
        //        {
        //            FileData =  JObject.Parse(FileIO.ReadDataFromFile(_Properties.FilePath));
        //        }
        //        else
        //        {
        //            CreateFile();
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        Debug.Console(0, this, "Error Processing File: {0}", e);
        //    }

        //    return null;
        //}

		void CreateFile()
		{
            Debug.Console(2, this, "Creating new file");
			FileIO.WriteDataToFile("{}", _Properties.FilePath);
			FileData = JObject.Parse(FileIO.ReadDataFromFile(FileIO.GetFile(_Properties.FilePath)));
		}

		private void WriteValue(string path, ushort value)
		{
            if (Initialized)
            {
                try
                {
                    Debug.Console(2, this, "Writing data {0} {1}", path, value);

                    JToken tokenToReplace;
                    if (UseFile)
                    {
                        tokenToReplace = FileData.SelectToken(path);
                        tokenToReplace.Replace(value);
                    }
                    else
                    {
                        tokenToReplace = _Config.Properties["Data"].SelectToken(path);
                        tokenToReplace.Replace(value);
                    }
                    WriteFile();

                    Feedbacks[path].FireUpdate();
                }
                catch (Exception e)
                {
                    Debug.Console(0, this, "Error WriteValue: {0}", e);
                }
            }

		}

		private void WriteValue(string path, string value)
		{
			Debug.Console(2, this, "Writing data {0} {1}", path, value);
			if (!String.IsNullOrEmpty(value))
			{
				JToken tokenToReplace;
				if (UseFile)
				{
					tokenToReplace = FileData.SelectToken(path);
					tokenToReplace.Replace(value);
				}
				else
				{
					tokenToReplace = _Config.Properties["Data"].SelectToken(path);
					tokenToReplace.Replace(value);
				}
				Feedbacks[path].FireUpdate();
				WriteFile();
			}
			
		}

		private void WriteValue(string path, bool value)
		{
			Debug.Console(2, this, "Writing data {0} {1}", path, value);
			JToken tokenToReplace;
			if (UseFile)
			{
				tokenToReplace = FileData.SelectToken(path);
				tokenToReplace.Replace(value);
			}
			else
			{
				tokenToReplace = _Config.Properties["Data"].SelectToken(path);
				tokenToReplace.Replace(value);
			}
			WriteFile();
			Feedbacks[path].FireUpdate();
		}

        private void WriteFile()
        {
            try
			{
				if (fileLock.TryEnter())
				{
                    if (_WriteTimer == null)
                        _WriteTimer = new CTimer(WriteFileNow, WriteTimeout);

                    _WriteTimer.Reset(WriteTimeout);
                    Debug.Console(1, "Config File write timer has been reset.");
                }
                else
                {
                    Debug.Console(0, Debug.ErrorLogLevel.Error, "FileIO Unable to enter FileLock");
                    return;
                }

            }
            catch (Exception e)
            {
                Debug.Console(0, Debug.ErrorLogLevel.Error, "Error: FileIO read failed: \r{0}", e);
                return;
            }
            finally
            {
                if (fileLock != null && !fileLock.Disposed)
                    fileLock.Leave();

            }
        }

        private void WriteFileNow(object o)
        {
            WriteFileNow();
        }
		private void WriteFileNow()
		{
			if (UseFile)
			{
				FileIO.WriteDataToFile(JsonConvert.SerializeObject(FileData), _Properties.FilePath);
			}
			else
			{
				SetConfig(_Config);
			}
		}


		public void ConsoleCommand(string command)
		{
			if (string.IsNullOrEmpty(command))
			{
				Debug.Console(0, this, "CustomValue Path [NewValue] command requires an argument for Path and optionally a NewValue");
				return;
			}
			var commandArray = command.Split(' ');
			var path = commandArray[0];
			
			if (commandArray.Length == 1)
			{
				Debug.Console(0, this, "CustomValue Path:{0} Value:{1}", path, _Properties.Data.SelectToken(path));
			}
			else if (commandArray.Length == 2)
			{
				var value = commandArray[1];
				try
				{
					var Number = ushort.Parse(value);
					WriteValue(path, value);
					return;
				}
				catch
				{
					Debug.Console(0, this, "CustomValue command requires an argument for Path and optionally a NewValue");
				}
				if (value.ToLower() == "true" || value.ToLower() == "false")
				{
					WriteValue(path, bool.Parse(value));
					return;
				}
				else 
				{
					WriteValue(path, value);
					return;
				}

			}

		}


        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = new EssentialsPluginBridgeJoinMapTemplate(joinStart);

            // This adds the join map to the collection on the bridge
            if (bridge != null)
            {
                bridge.AddJoinMap(Key, joinMap);
            }
            bridge.Eisc.OnlineStatusChange += new Crestron.SimplSharpPro.OnlineStatusChangeEventHandler(Eisc_OnlineStatusChange);
            var customJoins = JoinMapHelper.TryGetJoinMapAdvancedForDevice(joinMapKey);

			
            if (customJoins == null)
            {
				Debug.Console(0, this, "Custom Joins not found!!!");
            }
			
            Debug.Console(1, this, "Linking to Trilist '{0}'", trilist.ID.ToString("X"));
            Debug.Console(0, this, "Linking to Bridge Type {0}", GetType().Name);
			foreach (var j in customJoins)
			{
				
				var path = j.Key;
				uint? index = j.Value.JoinNumber;
				JToken value;
				if (UseFile)
				{
					value = FileData.SelectToken(path);
				}
				else
				{
					value = _Properties.Data.SelectToken(path);
				}

				// Validity Checks
				if (index == 0)
				{
					Debug.Console(0, this, "Missing Join number for Key {0}", path);
					continue;
				}
				if (value == null)
				{
					Debug.Console(0, this, "Cannot find value for Key {0}", path);
					continue;
				}

				
				//var path = map.Path;
				ushort join = (ushort)(index + joinStart - 1); 

				Debug.Console(2, this, "Read and mapped data {0} {1} {2}",  value, join, value.Type.ToString());
				if (value.Type == Newtonsoft.Json.Linq.JTokenType.Integer)
				{
					Debug.Console(2, this, "I AM INT");
					trilist.SetUShortSigAction(join, (x) =>
					{
						WriteValue(path, x);
					});

					var newFeedback = new IntFeedback(() => { return (ushort)FileData.SelectToken(path); }); 
					Feedbacks.Add(path, newFeedback);
					newFeedback.LinkInputSig(trilist.UShortInput[join]);
					newFeedback.FireUpdate();
				}
				else if (value.Type == Newtonsoft.Json.Linq.JTokenType.String) 
				{
					Debug.Console(2, this, "I AM STRING");
					
					StringFeedback newFeedback;
					trilist.SetStringSigAction(join, (x) =>
							{
								WriteValue(path, x);
							});
					newFeedback = new StringFeedback(() => { return (string)FileData.SelectToken(path); }); 
					Feedbacks.Add(path, newFeedback);
					newFeedback.LinkInputSig(trilist.StringInput[join]);
					newFeedback.FireUpdate();
				}
				else if (value.Type == Newtonsoft.Json.Linq.JTokenType.Object)
				{
					Debug.Console(2, this, "I AM OBJECT");

					StringFeedback newFeedback;
					trilist.SetStringSigAction(join, (x) =>
					{
						WriteValue(path, x);
					});
					newFeedback = new StringFeedback(() =>
					{
						return FileData.SelectToken(path).ToString(Formatting.None);
					});

					Feedbacks.Add(path, newFeedback);
					newFeedback.LinkInputSig(trilist.StringInput[join]);
					newFeedback.FireUpdate();
				}
				else if (value.Type == Newtonsoft.Json.Linq.JTokenType.Boolean)
				{
					Debug.Console(2, this, "I AM BOOL");

					BoolFeedback newFeedback;
					newFeedback = new BoolFeedback(() => { return (bool)FileData.SelectToken(path); });
					Feedbacks.Add(path, newFeedback);
					newFeedback.LinkInputSig(trilist.BooleanInput[join]);
					newFeedback.FireUpdate();
				}
			}
           
        }

        void Eisc_OnlineStatusChange(Crestron.SimplSharpPro.GenericBase currentDevice, Crestron.SimplSharpPro.OnlineOfflineEventArgs args)
        {
            if (args.DeviceOnLine)
            {
                Debug.Console(2, this, "EISC ONLINE");
                CTimer init = new CTimer((o) => { Debug.Console(2, this, "INITIALIZED"); Initialized = true; }, 10000);
            }
            else if (!args.DeviceOnLine)
            {
                Initialized = false;
                Debug.Console(2, this, "EISC OFFLINE");
            }
        }




    }
}

