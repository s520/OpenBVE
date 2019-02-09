using OpenBveApi.Runtime;

namespace OpenBveRoutePlugin
{
	public class RoutePlugin : IRuntimeRoute
	{
		public bool Load()
		{
			return true;
		}

		public void Unload() { }

		public void Initialize() { }

		public void Elapse(ElapseDataRoute data, out byte[] receiveData)
		{
			receiveData = new byte[] { };
		}

		public void SetBeacon(BeaconDataEx beacon) { }

		public void Receiver(byte[] receiveData) { }
	}
}
