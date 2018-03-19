﻿using System;
using System.ServiceModel;
using System.Threading;
using OpenBveApi.Runtime;

namespace OpenBveApi.Interop
{
	public static class Shared
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

	[ServiceContract(CallbackContract = typeof(IAtsPluginCallback))]
	public interface IAtsPluginProxy
	{
		[OperationContract]
		void SetPluginFile(string fileName);

		[OperationContract]
		bool Load(VehicleSpecs specs, InitializationModes mode);

		[OperationContract]
		void Unload();

		[OperationContract]
		void BeginJump(InitializationModes mode);

		[OperationContract]
		ElapseProxy Elapse(ElapseProxy proxyData);

		[OperationContract]
		void SetReverser(int reverser);

		[OperationContract]
		void SetPowerNotch(int powerNotch);

		[OperationContract]
		void SetBrake(int brakeNotch);

		[OperationContract]
		void KeyDown(int key);

		[OperationContract]
		void KeyUp(int key);

		[OperationContract]
		void HornBlow(int type);

		[OperationContract]
		void DoorChange(int oldState, int newState);

		[OperationContract]
		void SetSignal(int aspect);

		[OperationContract]
		void SetBeacon(BeaconData beacon);
	};

	public interface IAtsPluginCallback
	{
		[OperationContract(IsOneWay = true)]
		void ReportError(string Error);
	}
}
