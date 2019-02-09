namespace OpenBveApi.Runtime
{
	/// <summary>A signalling aspect attached to a track section</summary>
	public struct SectionAspect
	{
		/// <summary>The aspect number</summary>
		public int Number;
		/// <summary>The speed limit associated with this aspect number</summary>
		public double Speed;

		/// <summary>Creates a new signalling aspect</summary>
		/// <param name="Number">The aspect number</param>
		/// <param name="Speed">The speed limit</param>
		public SectionAspect(int Number, double Speed)
		{
			this.Number = Number;
			this.Speed = Speed;
		}
	}

	public struct Section
	{
		public SectionAspect[] Aspects;
	}

	public class ElapseDataRoute
	{
		public readonly Section[] Sections;

		public ElapseDataRoute(Section[] sections)
		{
			Sections = sections;
		}
	}

	public class BeaconDataEx : BeaconData
	{
		public BeaconDataEx(int type, int optional, SignalData signal) : base(type, optional, signal) { }

		public new int Type
		{
			get
			{
				return MyType;
			}
			set
			{
				MyType = value;
			}
		}

		public new int Optional
		{
			get
			{
				return MyOptional;
			}
			set
			{
				MyOptional = value;
			}
		}
	}

	public interface IRuntimeRoute
	{
		bool Load();
		void Unload();
		void Initialize();
		void Elapse(ElapseDataRoute data, out byte[] sendData);
		void SetBeacon(BeaconDataEx data);
		void Receiver(byte[] receiveData);
	}
}
