using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ServiceModel;
using System.Threading;
using OpenBveApi.Runtime;
using OpenBveApi.Interop;

namespace OpenBve
{
    public static class InteropShared
    {
        // Host signals it's ready and listening.
        public static readonly EventWaitHandle eventHostReady = new EventWaitHandle(false, EventResetMode.AutoReset, @"eventHostReady");

        // Client asks the host to quit.
        public static readonly EventWaitHandle eventHostShouldStop = new EventWaitHandle(false, EventResetMode.AutoReset, @"eventHostShouldStop");

        public const string pipeBaseAddress = @"net.pipe://localhost";

        /// <summary>Pipe name</summary>
        public const string pipeName = @"pipename";

        /// <summary>Base addresses for the hosted service.</summary>
        public static Uri baseAddress { get { return new Uri(pipeBaseAddress); } }

        /// <summary>Complete address of the named pipe endpoint.</summary>
        public static Uri endpointAddress { get { return new Uri(pipeBaseAddress + '/' + pipeName); } }

    }
    

    [Guid("1388460c-fc46-46f0-9a3a-98624f6304bd")]
    public interface IAtsPlugin
    {
        int getStatus();

	    void setPluginFile(string fileName);

	    bool load(VehicleSpecs specs, InitializationModes mode);

	    void unload();

	    void beginJump(InitializationModes mode);

	    ElapseProxy elapse(ElapseProxy proxyData);

	    void setReverser(int reverser);

	    void setPowerNotch(int powerNotch);

	    void setBrake(int brakeNotch);

	    void keyDown(int key);

	    void keyUp(int key);

	    void hornBlow(int type);

	    void doorChange(int oldState, int newState);

	    void setSignal(int aspect);

	    void setBeacon(BeaconData beacon);
    };

	/// <summary>Represents a Win32 plugin proxied through WinPluginProxy</summary>
    [ClassInterface(ClassInterfaceType.None)]
    [Guid("c570f27c-0a86-4d9b-a568-4d4b217caf7b")]
    public class Win32ProxyPlugin : IAtsPlugin
    {

        private static int win32Dll_instances = 0;
        private static Process hostProcess;
        private static IAtsPluginProxy pipeProxy;
        private static object syncLock = new object();

        public Win32ProxyPlugin()
        {
            lock (syncLock)
            {
                if (win32Dll_instances == 0)
                {
                    hostProcess = new Process();
                    hostProcess.StartInfo.FileName = @"WinPluginProxy.exe";
                    hostProcess.Start();
                    InteropShared.eventHostReady.WaitOne();
                    pipeProxy = getPipeProxy();
                }
                win32Dll_instances++;
            }
        }
        ~Win32ProxyPlugin()
        {
            lock (syncLock)
            {
                win32Dll_instances--;
                if (win32Dll_instances == 0)
                {
                    InteropShared.eventHostShouldStop.Set();
                    hostProcess.WaitForExit();
                }
            }
        }
        public static IAtsPluginProxy getPipeProxy()
        {
            ChannelFactory<IAtsPluginProxy> pipeFactory =
              new ChannelFactory<IAtsPluginProxy>(
                new NetNamedPipeBinding(),
                new EndpointAddress(
                    InteropShared.endpointAddress));

            IAtsPluginProxy pipeProxy = pipeFactory.CreateChannel();
            return pipeProxy;
        }
        public int getStatus()
        {
            return (int)pipeProxy.WCFGetStatus();
        }

	    public void setPluginFile(string fileName)
	    {
		    pipeProxy.SetPluginFile(fileName);
	    }

	    public bool load(VehicleSpecs specs, InitializationModes mode)
	    {
		    return pipeProxy.Load(specs, mode);
	    }

	    public void unload()
	    {
		    pipeProxy.Unload();
	    }

	    public void beginJump(InitializationModes mode)
	    {
		    pipeProxy.BeginJump(mode);
	    }

	    public ElapseProxy elapse(ElapseProxy proxyData)
	    {
		    return pipeProxy.Elapse(proxyData);
	    }

	    public void setReverser(int reverser)
	    {
		    pipeProxy.SetReverser(reverser);
	    }

	    public void setPowerNotch(int powerNotch)
	    {
		    pipeProxy.SetPowerNotch(powerNotch);
	    }

	    public void setBrake(int brakeNotch)
	    {
		    pipeProxy.SetBrake(brakeNotch);
	    }

	    public void keyDown(int key)
	    {
		    pipeProxy.KeyDown(key);
	    }

	    public void keyUp(int key)
	    {
		    pipeProxy.KeyUp(key);
	    }

	    public void hornBlow(int type)
	    {
		    pipeProxy.HornBlow(type);
	    }

	    public void doorChange(int oldState, int newState)
	    {
		    pipeProxy.DoorChange(oldState, newState);
	    }

	    public void setSignal(int aspect)
	    {
		    pipeProxy.SetSignal(aspect);
	    }

	    public void setBeacon(BeaconData beacon)
	    {
		    pipeProxy.SetBeacon(beacon);
	    }
    }
}
