﻿namespace TrainEditor2.Simulation.TrainManager
{
	/// <summary>The TrainManager is the root class containing functions to load and manage trains within the simulation world.</summary>
	public static partial class TrainManager
	{
		internal struct CarSpecs
		{
			/// current data
			internal double CurrentSpeed;
			internal double CurrentPerceivedSpeed;
			/// <summary>The acceleration generated by the motor. Is positive for power and negative for brake, regardless of the train's direction.</summary>
			internal double CurrentAccelerationOutput;
		}
	}
}
