using System.ServiceModel;
using OpenBveApi.Runtime;

namespace OpenBveApi.Interop
{
	[ServiceContract]
	public interface IAtsPluginProxy
	{
		[OperationContract]
		int WCFGetStatus();

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
}
