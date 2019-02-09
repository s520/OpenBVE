using System;
using System.Reflection;
using System.Threading;
using OpenBveApi.Colors;
using OpenBveApi.Interface;
using OpenBveApi.Math;
using OpenBveApi.Runtime;

namespace OpenBve
{
	internal class NetPlugin2 : PluginManager.Plugin
	{
		private readonly string TrainPluginFolder;
		private readonly string TrainFolder;
		private readonly IRuntimeTrain TrainApi;
		private IRuntimeRoute RouteApi;
		private NetPlugin.SoundHandleEx[] SoundHandles;
		private int SoundHandlesCount;
		private readonly Section[] OriginalSections;
		private readonly int SectionsLength;

		internal NetPlugin2(string pluginFile, string trainFolder, IRuntimeTrain trainApi, TrainManager.Train train)
		{
			PluginTitle = System.IO.Path.GetFileName(pluginFile);
			PluginValid = true;
			PluginMessage = null;
			Train = train;
			Panel = null;
			SupportsAI = false;
			LastTime = 0.0;
			LastReverser = -2;
			LastPowerNotch = -1;
			LastBrakeNotch = -1;
			LastAspects = new int[] { };
			LastSection = -1;
			LastException = null;
			TrainPluginFolder = System.IO.Path.GetDirectoryName(pluginFile);
			TrainFolder = trainFolder;
			TrainApi = trainApi;
			SoundHandles = new NetPlugin.SoundHandleEx[16];
			SoundHandlesCount = 0;
			SectionsLength = Game.Sections.Length;
			OriginalSections = new Section[SectionsLength];
			for (int i = 0; i < SectionsLength; i++)
			{
				OriginalSections[i].Aspects = new SectionAspect[Game.Sections[i].Aspects.Length];
				Array.Copy(Game.Sections[i].Aspects, OriginalSections[i].Aspects, Game.Sections[i].Aspects.Length);
			}
		}

		internal override bool Load(VehicleSpecs specs, InitializationModes mode)
		{
			Program.FileSystem.AppendToLogFile("Loading route plugin: " + Game.RoutePlugin);
			if (!string.IsNullOrEmpty(Game.RoutePlugin))
			{
				if (!LoadRoutePlugin(Game.RoutePlugin))
				{
					LoadDefaultRoutePlugin();
				}
			}
			else
			{
				LoadDefaultRoutePlugin();
			}
			RouteApi.Initialize();
			TrainApi.Transmitter += TrainPluginTx;

			LoadProperties properties = new LoadProperties(TrainPluginFolder, TrainFolder, PlaySound, PlaySound, AddInterfaceMessage, AddScore);
			bool success;
			try
			{
				success = TrainApi.Load(properties);
				SupportsAI = properties.AISupport == AISupport.Basic;
			}
			catch (Exception ex)
			{
				if (ex is ThreadStateException)
				{
					//TTC plugin, broken when multi-threading is used
					success = false;
					properties.FailureReason = "This plugin does not function correctly with current versions of openBVE. Please ask the plugin developer to fix this.";
				}
				else
				{
					success = false;
					properties.FailureReason = ex.Message;
				}
			}
			if (success)
			{
				Panel = properties.Panel ?? new int[] { };
#if !DEBUG
				try
				{
#endif
					TrainApi.SetVehicleSpecs(specs);
					TrainApi.Initialize(mode);
#if !DEBUG
				}
				catch (Exception ex)
				{
					base.LastException = ex;
					throw;
				}
#endif
				UpdatePower();
				UpdateBrake();
				UpdateReverser();
				return true;
			}
			else if (properties.FailureReason != null)
			{
				Interface.AddMessage(MessageType.Error, false, "The train plugin " + PluginTitle + " failed to load for the following reason: " + properties.FailureReason);
				return false;
			}
			else
			{
				Interface.AddMessage(MessageType.Error, false, "The train plugin " + PluginTitle + " failed to load for an unspecified reason.");
				return false;
			}
		}

		internal override void Unload()
		{
#if !DEBUG
			try
			{
#endif
				TrainApi.Unload();
				UnloadRoutePlugin();
#if !DEBUG
			}
			catch (Exception ex)
			{
				base.LastException = ex;
				throw;
			}
#endif
		}

		internal override void BeginJump(InitializationModes mode)
		{
#if !DEBUG
			try
			{
#endif
				for (int i = 0; i < SectionsLength; i++)
				{
					Array.Copy(OriginalSections[i].Aspects, Game.Sections[i].Aspects, Game.Sections[i].Aspects.Length);
				}
				RouteApi.Initialize();
				TrainApi.Initialize(mode);
#if !DEBUG
			}
			catch (Exception ex)
			{
				base.LastException = ex;
				throw;
			}
#endif
		}

		internal override void EndJump() { }

		protected override void Elapse(ElapseData trainApiData)
		{
#if !DEBUG
			try
			{
#endif
				int start = Train.CurrentSectionIndex >= 0 ? Train.CurrentSectionIndex : 0;
				Section[] sections = new Section[SectionsLength - start];
				for (int i = 0; i < SectionsLength - start; i++)
				{
					sections[i].Aspects = new SectionAspect[Game.Sections[i + start].Aspects.Length];
					Array.Copy(Game.Sections[i + start].Aspects, sections[i].Aspects, Game.Sections[i + start].Aspects.Length);
				}
				ElapseDataRoute routeApiData = new ElapseDataRoute(sections);
				byte[] sendData;
				RouteApi.Elapse(routeApiData, out sendData);
				for (int i = 0; i < SectionsLength - start; i++)
				{
					Array.Copy(sections[i].Aspects, Game.Sections[i + start].Aspects, Game.Sections[i + start].Aspects.Length);
				}
				TrainApi.Elapse(trainApiData, sendData);
				for (int i = 0; i < SoundHandlesCount; i++)
				{
					if (SoundHandles[i].Stopped | SoundHandles[i].Source.State == Sounds.SoundSourceState.Stopped)
					{
						SoundHandles[i].Stop();
						SoundHandles[i].Source.Stop();
						SoundHandles[i] = SoundHandles[SoundHandlesCount - 1];
						SoundHandlesCount--;
						i--;
					}
					else
					{
						SoundHandles[i].Source.Pitch = Math.Max(0.01, SoundHandles[i].Pitch);
						SoundHandles[i].Source.Volume = Math.Max(0.0, SoundHandles[i].Volume);
					}
				}
#if !DEBUG
			}
			catch (Exception ex)
			{
				base.LastException = ex;
				throw;
			}
#endif
		}

		protected override void SetReverser(int reverser)
		{
#if !DEBUG
			try
			{
#endif
				TrainApi.SetReverser(reverser);
#if !DEBUG
			}
			catch (Exception ex)
			{
				base.LastException = ex;
				throw;
			}
#endif
		}

		protected override void SetPower(int powerNotch)
		{
#if !DEBUG
			try
			{
#endif
				TrainApi.SetPower(powerNotch);
#if !DEBUG
			}
			catch (Exception ex)
			{
				base.LastException = ex;
				throw;
			}
#endif
		}

		protected override void SetBrake(int brakeNotch)
		{
#if !DEBUG
			try
			{
#endif
				TrainApi.SetBrake(brakeNotch);
#if !DEBUG
			}
			catch (Exception ex)
			{
				base.LastException = ex;
				throw;
			}
#endif
		}

		internal override void KeyDown(VirtualKeys key)
		{
#if !DEBUG
			try
			{
#endif
				TrainApi.KeyDown(key);
#if !DEBUG
			}
			catch (Exception ex)
			{
				base.LastException = ex;
				throw;
			}
#endif
		}

		internal override void KeyUp(VirtualKeys key)
		{
#if !DEBUG
			try
			{
#endif
				TrainApi.KeyUp(key);
#if !DEBUG
			}
			catch (Exception ex)
			{
				base.LastException = ex;
				throw;
			}
#endif
		}

		internal override void HornBlow(HornTypes type)
		{
#if !DEBUG
			try
			{
#endif
				TrainApi.HornBlow(type);
#if !DEBUG
			}
			catch (Exception ex)
			{
				base.LastException = ex;
				throw;
			}
#endif
		}

		internal override void DoorChange(DoorStates oldState, DoorStates newState)
		{
#if !DEBUG
			try
			{
#endif
				TrainApi.DoorChange(oldState, newState);
#if !DEBUG
			}
			catch (Exception ex)
			{
				base.LastException = ex;
				throw;
			}
#endif
		}

		protected override void SetSignal(SignalData[] signal)
		{
#if !DEBUG
			try
			{
#endif
				TrainApi.SetSignal(signal);
#if !DEBUG
			}
			catch (Exception ex)
			{
				base.LastException = ex;
				throw;
			}
#endif
		}

		protected override void SetBeacon(BeaconData trainApiBeacon)
		{
#if !DEBUG
			try
			{
#endif
				BeaconDataEx routeApiBeacon = new BeaconDataEx(trainApiBeacon.Type, trainApiBeacon.Optional, trainApiBeacon.Signal);
				RouteApi.SetBeacon(routeApiBeacon);
				TrainApi.SetBeacon(routeApiBeacon);
#if !DEBUG
			}
			catch (Exception ex)
			{
				base.LastException = ex;
				throw;
			}
#endif
		}

		protected override void PerformAI(AIData data)
		{
#if !DEBUG
			try
			{
#endif
				TrainApi.PerformAI(data);
#if !DEBUG
			}
			catch (Exception ex)
			{
				base.LastException = ex;
				throw;
			}
#endif
		}

		/// <summary>May be called from a .Net plugin, in order to add a message to the in-game display</summary>
		/// <param name="Message">The message to display</param>
		/// <param name="Color">The color in which to display the message</param>
		/// <param name="Time">The time in seconds for which to display the message</param>
		internal void AddInterfaceMessage(string Message, MessageColor Color, double Time)
		{
			Game.AddMessage(Message, MessageManager.MessageDependency.Plugin, Interface.GameMode.Expert, Color, Game.SecondsSinceMidnight + Time, null);
		}

		/// <summary>May be called from a .Net plugin, in order to add a score to the post-game log</summary>
		/// <param name="Score">The score to add</param>
		/// <param name="Message">The message to display in the post-game log</param>
		/// <param name="Color">The color of the in-game message</param>
		/// <param name="Timeout">The time in seconds for which to display the in-game message</param>
		internal void AddScore(int Score, string Message, MessageColor Color, double Timeout)
		{
			Game.CurrentScore.CurrentValue += Score;

			int n = Game.ScoreMessages.Length;
			Array.Resize(ref Game.ScoreMessages, n + 1);
			Game.ScoreMessages[n] = new Game.ScoreMessage
			{
				Value = Score,
				Color = Color,
				RendererPosition = new Vector2(0, 0),
				RendererAlpha = 0.0,
				Text = Message,
				Timeout = Timeout
			};
		}

		/// <summary>May be called from a .Net plugin, in order to play a sound from the driver's car of a train</summary>
		/// <param name="index">The plugin-based of the sound to play</param>
		/// <param name="volume">The volume of the sound- A volume of 1.0 represents nominal volume</param>
		/// <param name="pitch">The pitch of the sound- A pitch of 1.0 represents nominal pitch</param>
		/// <param name="looped">Whether the sound is looped</param>
		/// <returns>The sound handle, or null if not successful</returns>
		internal NetPlugin.SoundHandleEx PlaySound(int index, double volume, double pitch, bool looped)
		{
			if (index >= 0 && index < Train.Cars[Train.DriverCar].Sounds.Plugin.Length && Train.Cars[Train.DriverCar].Sounds.Plugin[index].Buffer != null)
			{
				Sounds.SoundBuffer buffer = Train.Cars[Train.DriverCar].Sounds.Plugin[index].Buffer;
				Vector3 position = Train.Cars[Train.DriverCar].Sounds.Plugin[index].Position;
				Sounds.SoundSource source = Sounds.PlaySound(buffer, pitch, volume, position, Train, Train.DriverCar, looped);
				if (SoundHandlesCount == SoundHandles.Length)
				{
					Array.Resize(ref SoundHandles, SoundHandles.Length << 1);
				}
				SoundHandles[SoundHandlesCount] = new NetPlugin.SoundHandleEx(volume, pitch, source);
				SoundHandlesCount++;
				return SoundHandles[SoundHandlesCount - 1];
			}
			return null;
		}

		/// <summary>May be called from a .Net plugin, in order to play a sound from a specific car of a train</summary>
		/// <param name="index">The plugin-based of the sound to play</param>
		/// <param name="volume">The volume of the sound- A volume of 1.0 represents nominal volume</param>
		/// <param name="pitch">The pitch of the sound- A pitch of 1.0 represents nominal pitch</param>
		/// <param name="looped">Whether the sound is looped</param>
		/// <param name="CarIndex">The index of the car which is to emit the sound</param>
		/// <returns>The sound handle, or null if not successful</returns>
		internal NetPlugin.SoundHandleEx PlaySound(int index, double volume, double pitch, bool looped, int CarIndex)
		{
			if (index >= 0 && index < Train.Cars[Train.DriverCar].Sounds.Plugin.Length && Train.Cars[Train.DriverCar].Sounds.Plugin[index].Buffer != null && CarIndex < Train.Cars.Length && CarIndex >= 0)
			{
				Sounds.SoundBuffer buffer = Train.Cars[Train.DriverCar].Sounds.Plugin[index].Buffer;
				Vector3 position = Train.Cars[Train.DriverCar].Sounds.Plugin[index].Position;
				Sounds.SoundSource source = Sounds.PlaySound(buffer, pitch, volume, position, Train, CarIndex, looped);
				if (SoundHandlesCount == SoundHandles.Length)
				{
					Array.Resize(ref SoundHandles, SoundHandles.Length << 1);
				}
				SoundHandles[SoundHandlesCount] = new NetPlugin.SoundHandleEx(volume, pitch, source);
				SoundHandlesCount++;
				return SoundHandles[SoundHandlesCount - 1];
			}
			return null;
		}

		private bool LoadRoutePlugin(string pluginFile)
		{
			string pluginTitle = System.IO.Path.GetFileName(pluginFile);
			string pluginFolder = System.IO.Path.GetDirectoryName(pluginFile);

			UnloadRoutePlugin();

			Assembly assembly;
			try
			{
				assembly = Assembly.LoadFile(pluginFile);
			}
			catch (BadImageFormatException)
			{
				assembly = null;
			}
			catch (Exception ex)
			{
				Interface.AddMessage(MessageType.Error, false, "The route plugin " + pluginTitle + " could not be loaded due to the following exception: " + ex.Message);
				return false;
			}

			if (assembly == null)
			{
				return false;
			}
			Type[] types;
			try
			{
				types = assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				foreach (Exception e in ex.LoaderExceptions)
				{
					Interface.AddMessage(MessageType.Error, false, "The route plugin " + pluginTitle + " raised an exception on loading: " + e.Message);
				}
				return false;
			}
			foreach (Type type in types)
			{
				if (typeof(IRuntimeRoute).IsAssignableFrom(type))
				{
					if (type.FullName == null)
					{
						//Should never happen, but static code inspection suggests that it's possible....
						throw new InvalidOperationException();
					}
					RouteApi = assembly.CreateInstance(type.FullName) as IRuntimeRoute;
					if (RouteApi != null && RouteApi.Load())
					{
						return true;
					}
					else
					{
						RouteApi = null;
						return false;
					}
				}
			}
			Interface.AddMessage(MessageType.Error, false, "The route plugin " + pluginTitle + " does not export a route interface and therefore cannot be used with openBVE.");
			return false;
		}

		private void LoadDefaultRoutePlugin()
		{
			string file = OpenBveApi.Path.CombineFile(Program.FileSystem.GetDataFolder("Plugins"), "OpenBveRoutePlugin.dll");
			LoadRoutePlugin(file);
		}

		private void UnloadRoutePlugin()
		{
			if (RouteApi == null)
			{
				return;
			}
			RouteApi.Unload();
			RouteApi = null;
		}

		private void TrainPluginTx(object sender, TxEventArgs e)
		{
			RouteApi.Receiver(e.SendData);
		}
	}
}
