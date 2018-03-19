using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.ServiceModel;
using OpenBveApi.Runtime;

namespace OpenBve {
	/// <summary>Represents a legacy Win32 plugin.</summary>
	internal class ProxyPlugin : PluginManager.Plugin {
		
		private static class SoundInstructions {
			internal const int Stop = -10000;
			internal const int PlayLooping = 0;
			internal const int PlayOnce = 1;
			internal const int Continue = 2;
		}
		
		// --- members ---
		private readonly string PluginFile;
		private int[] Sound;
		private readonly int[] LastSound;
		private GCHandle PanelHandle;
		private GCHandle SoundHandle;

		private readonly Win32ProxyPlugin pluginProxy = new Win32ProxyPlugin();

		// --- constructors ---
		internal ProxyPlugin(string pluginFile, TrainManager.Train train) {
			base.PluginTitle = System.IO.Path.GetFileName(pluginFile);
			//Load the plugin via the proxy callback
			pluginProxy.setPluginFile(pluginFile);
			base.PluginValid = true;
			base.PluginMessage = null;
			base.Train = train;
			base.Panel = new int[256];
			base.SupportsAI = false;
			base.LastTime = 0.0;
			base.LastReverser = -2;
			base.LastPowerNotch = -1;
			base.LastBrakeNotch = -1;
			base.LastAspects = new int[] { };
			base.LastSection = -1;
			base.LastException = null;
			this.PluginFile = pluginFile;		
			this.Sound = new int[256];
			this.LastSound = new int[256];
			this.PanelHandle = new GCHandle();
			this.SoundHandle = new GCHandle();
		}

		
		
		// --- functions ---
		internal override bool Load(VehicleSpecs specs, InitializationModes mode)
		{
			return pluginProxy.load(specs, mode);
		}
		internal override void Unload() {
			if (PanelHandle.IsAllocated) {
				PanelHandle.Free();
			}
			if (SoundHandle.IsAllocated) {
				SoundHandle.Free();
			}
			pluginProxy.unload();
		}
		internal override void BeginJump(InitializationModes mode) {
			pluginProxy.beginJump(mode);
		}

		internal override void EndJump()
		{
			//EndJump is not relevant to legacy plugins, but we must implement it as an API member
		}
		internal override void Elapse(ref ElapseData data)
		{
			if (pluginProxy.callback.lastError != string.Empty)
			{
				Program.AppendToLogFile("ERROR: The proxy plugin " + PluginFile + " generated the following error:");
				Program.AppendToLogFile(pluginProxy.callback.lastError);
				pluginProxy.callback.lastError = string.Empty;
			}

			if (pluginProxy.callback.Unload == true)
			{
				this.Unload();
				return;
			}
			ElapseProxy e = new ElapseProxy(data);
			ElapseProxy proxyData = pluginProxy.elapse(e);
			base.Panel = proxyData.Panel;
			this.Sound = proxyData.Sound;
			for (int i = 0; i < this.Sound.Length; i++) {
					if (this.Sound[i] != this.LastSound[i]) {
						if (this.Sound[i] == SoundInstructions.Stop) {
							if (i < base.Train.Cars[base.Train.DriverCar].Sounds.Plugin.Length) {
								Sounds.StopSound(base.Train.Cars[base.Train.DriverCar].Sounds.Plugin[i].Source);
							}
						} else if (this.Sound[i] > SoundInstructions.Stop & this.Sound[i] <= SoundInstructions.PlayLooping) {
							if (i < base.Train.Cars[base.Train.DriverCar].Sounds.Plugin.Length) {
								Sounds.SoundBuffer buffer = base.Train.Cars[base.Train.DriverCar].Sounds.Plugin[i].Buffer;
								if (buffer != null) {
									double volume = (double)(this.Sound[i] - SoundInstructions.Stop) / (double)(SoundInstructions.PlayLooping - SoundInstructions.Stop);
									if (Sounds.IsPlaying(base.Train.Cars[base.Train.DriverCar].Sounds.Plugin[i].Source)) {
										base.Train.Cars[base.Train.DriverCar].Sounds.Plugin[i].Source.Volume = volume;
									} else {
										base.Train.Cars[base.Train.DriverCar].Sounds.Plugin[i].Source = Sounds.PlaySound(buffer, 1.0, volume, base.Train.Cars[base.Train.DriverCar].Sounds.Plugin[i].Position, base.Train, base.Train.DriverCar, true);
									}
								}
							}
						} else if (this.Sound[i] == SoundInstructions.PlayOnce) {
							if (i < base.Train.Cars[base.Train.DriverCar].Sounds.Plugin.Length) {
								Sounds.SoundBuffer buffer = base.Train.Cars[base.Train.DriverCar].Sounds.Plugin[i].Buffer;
								if (buffer != null) {
									base.Train.Cars[base.Train.DriverCar].Sounds.Plugin[i].Source = Sounds.PlaySound(buffer, 1.0, 1.0, base.Train.Cars[base.Train.DriverCar].Sounds.Plugin[i].Position, base.Train, base.Train.DriverCar, false);
								}
							}
							this.Sound[i] = SoundInstructions.Continue;
						} else if (this.Sound[i] != SoundInstructions.Continue) {
							this.PluginValid = false;
						}
						this.LastSound[i] = this.Sound[i];
					} else {
						if ((this.Sound[i] < SoundInstructions.Stop | this.Sound[i] > SoundInstructions.PlayLooping) && this.Sound[i] != SoundInstructions.PlayOnce & this.Sound[i] != SoundInstructions.Continue) {
							this.PluginValid = false;
						}
					}
				}

			data = proxyData.Data;
		}
		internal override void SetReverser(int reverser) {
			pluginProxy.setReverser(reverser);
		}
		internal override void SetPower(int powerNotch) {
			pluginProxy.setPowerNotch(powerNotch);
		}
		internal override void SetBrake(int brakeNotch) {
			pluginProxy.setBrake(brakeNotch);
		}
		internal override void KeyDown(VirtualKeys key) {
			pluginProxy.keyDown((int)key);
		}
		internal override void KeyUp(VirtualKeys key) {
			pluginProxy.keyUp((int)key);
		}
		internal override void HornBlow(HornTypes type) {
			pluginProxy.hornBlow((int)type);
		}
		internal override void DoorChange(DoorStates oldState, DoorStates newState) {
			pluginProxy.doorChange((int)oldState, (int)newState);
		}
		internal override void SetSignal(SignalData[] signal) {
			if (base.LastAspects.Length == 0 || signal[0].Aspect != base.LastAspects[0])
			{
				pluginProxy.setSignal(signal[0].Aspect);
			}
		}
		internal override void SetBeacon(BeaconData beacon) {
			pluginProxy.setBeacon(beacon);
		}

		internal override void PerformAI(AIData data)
		{
			//PerformAI is not relevant to legacy plugins, but we must implement it as an API member
		}
		
	}
}
