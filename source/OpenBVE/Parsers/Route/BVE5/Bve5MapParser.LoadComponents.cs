﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bve5Parser.MapGrammar;
using CsvHelper;
using OpenBveApi.Interface;
using OpenBveApi.Math;
using OpenBveApi.Objects;
using OpenBveApi.Runtime;
using Path = OpenBveApi.Path;

namespace OpenBve
{
	static partial class Bve5ScenarioParser
	{
		private static void LoadStationList(string FileName, MapData ParseData, RouteData RouteData)
		{
			RouteData.StationList = new List<Station>();

			if (string.IsNullOrEmpty(ParseData.StationListPath))
			{
				return;
			}

			if (!File.Exists(ParseData.StationListPath))
			{
				ParseData.StationListPath = Path.CombineFile(System.IO.Path.GetDirectoryName(FileName), ParseData.StationListPath);

				if (!File.Exists(ParseData.StationListPath))
				{
					Interface.AddMessage(MessageType.Error, false, ParseData.StationListPath + "is not found.");
					return;
				}
			}

			System.Text.Encoding Encoding = DetermineFileEncoding(ParseData.StationListPath);
			string[] Lines = File.ReadAllLines(ParseData.StationListPath, Encoding).Select(Line => Line.Trim('"').Trim()).ToArray();

			using (CsvReader Csv = new CsvReader(new StringReader(string.Join(Environment.NewLine, Lines))))
			{
				Csv.Configuration.AllowComments = true;
				while (Csv.Read())
				{
					Station NewStation = new Station();

					for (int i = 0; i < Csv.CurrentRecord.Length; i++)
					{
						string Value = string.Empty;
						if (Csv.CurrentRecord[i] != null)
						{
							Value = Csv.CurrentRecord[i].Split('#')[0].Trim();
						}

						switch (i)
						{
							case 0:
								NewStation.Key = Value;
								break;
							case 1:
								NewStation.Name = Value;
								break;
							case 2:
								if (Value.Equals("p", StringComparison.InvariantCultureIgnoreCase))
								{
									NewStation.StopMode = StationStopMode.AllPass;
								}

								if (!TryParseBve5Time(Value, out NewStation.ArrivalTime))
								{
									NewStation.ArrivalTime = -1.0;
								}
								break;
							case 3:
								if (Value.Equals("t", StringComparison.InvariantCultureIgnoreCase))
								{
									NewStation.StationType = StationType.Terminal;
								}

								if (!TryParseBve5Time(Value, out NewStation.DepartureTime))
								{
									NewStation.DepartureTime = -1.0;
								}
								break;
							case 4:
								{
									double StopTime;
									if (!NumberFormats.TryParseDoubleVb6(Value, out StopTime))
									{
										StopTime = 15.0;
									}
									else if (StopTime < 5.0)
									{
										StopTime = 5.0;
									}

									NewStation.StopTime = StopTime;
								}
								break;
							case 5:
								if (!TryParseBve5Time(Value, out NewStation.DefaultTime))
								{
									NewStation.DefaultTime = -1.0;
								}
								break;
							case 6:
								{
									int SignalFlag;
									if (!NumberFormats.TryParseIntVb6(Value, out SignalFlag))
									{
										SignalFlag = 0;
									}

									NewStation.ForceStopSignal = SignalFlag == 1;
								}
								break;
							case 7:
								{
									double AlightingTime;
									if (!NumberFormats.TryParseDoubleVb6(Value, out AlightingTime))
									{
										AlightingTime = 0.0;
									}

									NewStation.AlightingTime = AlightingTime;
								}
								break;
							case 8:
								{
									double PassengerRatio;
									if (!NumberFormats.TryParseDoubleVb6(Value, out PassengerRatio))
									{
										PassengerRatio = 100.0;
									}

									NewStation.PassengerRatio = PassengerRatio / 100.0;
								}
								break;
							case 9:
								NewStation.ArrivalSoundKey = Value;
								break;
							case 10:
								NewStation.DepartureSoundKey = Value;
								break;
							case 11:
								{
									double ReopenDoor;
									if (!NumberFormats.TryParseDoubleVb6(Value, out ReopenDoor) || ReopenDoor < 0.0)
									{
										ReopenDoor = 0.0;
									}

									NewStation.ReopenDoor = ReopenDoor / 100.0;
								}
								break;
							case 12:
								{
									double InterferenceInDoor;
									if (!NumberFormats.TryParseDoubleVb6(Value, out InterferenceInDoor) || InterferenceInDoor < 0.0)
									{
										InterferenceInDoor = 0.0;
									}

									NewStation.InterferenceInDoor = InterferenceInDoor;
								}
								break;
						}
					}

					RouteData.StationList.Add(NewStation);
				}
			}
		}

		private static void LoadStructureList(string FileName, bool PreviewOnly, MapData ParseData, RouteData RouteData)
		{
			RouteData.Objects = new ObjectDictionary();

			if (PreviewOnly || string.IsNullOrEmpty(ParseData.StructureListPath))
			{
				return;
			}

			if (!File.Exists(ParseData.StructureListPath))
			{
				ParseData.StructureListPath = Path.CombineFile(System.IO.Path.GetDirectoryName(FileName), ParseData.StructureListPath);

				if (!File.Exists(ParseData.StructureListPath))
				{
					Interface.AddMessage(MessageType.Error, false, ParseData.StructureListPath + "is not found.");
					return;
				}
			}

			string BaseDirectory = System.IO.Path.GetDirectoryName(ParseData.StructureListPath);

			System.Text.Encoding Encoding = DetermineFileEncoding(ParseData.StructureListPath);
			string[] Lines = File.ReadAllLines(ParseData.StructureListPath, Encoding).Select(Line => Line.Trim('"').Trim()).ToArray();

			using (CsvReader Csv = new CsvReader(new StringReader(string.Join(Environment.NewLine, Lines))))
			{
				Csv.Configuration.AllowComments = true;
				while (Csv.Read())
				{
					string Key = string.Empty;
					string ObjectFileName = string.Empty;

					for (int i = 0; i < Csv.CurrentRecord.Length; i++)
					{
						string Value = string.Empty;
						if (Csv.CurrentRecord[i] != null)
						{
							Value = Csv.CurrentRecord[i].Split('#')[0].Trim();
						}

						switch (i)
						{
							case 0:
								Key = Value;
								break;
							case 1:
								ObjectFileName = Value;
								break;
						}
					}

					try
					{
						ObjectFileName = Path.CombineFile(BaseDirectory, ObjectFileName);
					}
					catch
					{
						// ignored
					}

					if (!File.Exists(ObjectFileName))
					{
						Interface.AddMessage(MessageType.Error, false, ObjectFileName + "is not found.");
						continue;
					}

					System.Text.Encoding ObjectEncoding = TextEncoding.GetSystemEncodingFromFile(ObjectFileName);

					RouteData.Objects.Add(Key, ObjectManager.LoadObject(ObjectFileName, ObjectEncoding, false, false, false));
				}
			}
		}

		private static void LoadSignalList(string FileName, bool PreviewOnly, MapData ParseData, RouteData RouteData)
		{
			RouteData.SignalObjects = new List<SignalData>();

			if (PreviewOnly || string.IsNullOrEmpty(ParseData.SignalListPath))
			{
				return;
			}

			if (!File.Exists(ParseData.SignalListPath))
			{
				ParseData.SignalListPath = Path.CombineFile(System.IO.Path.GetDirectoryName(FileName), ParseData.SignalListPath);

				if (!File.Exists(ParseData.SignalListPath))
				{
					Interface.AddMessage(MessageType.Error, false, ParseData.SignalListPath + "is not found.");
					return;
				}
			}

			System.Text.Encoding Encoding = DetermineFileEncoding(ParseData.SignalListPath);
			string[] Lines = File.ReadAllLines(ParseData.SignalListPath, Encoding).Select(Line => Line.Trim('"').Trim()).ToArray();

			using (CsvReader Csv = new CsvReader(new StringReader(string.Join(Environment.NewLine, Lines))))
			{
				Csv.Configuration.AllowComments = true;
				while (Csv.Read())
				{
					string Key = string.Empty;
					List<int> Numbers = new List<int>();
					List<string> ObjectKeys = new List<string>();

					for (int i = 0; i < Csv.CurrentRecord.Length; i++)
					{
						string Value = string.Empty;
						if (Csv.CurrentRecord[i] != null)
						{
							Value = Csv.CurrentRecord[i].Split('#')[0].Trim();
						}

						switch (i)
						{
							case 0:
								Key = Value;
								break;
							default:
								if (!string.IsNullOrEmpty(Value))
								{
									Numbers.Add(i - 1);
									ObjectKeys.Add(Value);
								}
								break;
						}
					}

					List<ObjectManager.StaticObject> Objects = new List<ObjectManager.StaticObject>();
					foreach (var ObjectKey in ObjectKeys)
					{
						UnifiedObject Object;
						RouteData.Objects.TryGetValue(ObjectKey, out Object);
						if (Object != null)
						{
							Objects.Add((ObjectManager.StaticObject)Object);
						}
						else
						{
							Objects.Add(new ObjectManager.StaticObject());
						}
					}

					if (!string.IsNullOrEmpty(Key))
					{
						RouteData.SignalObjects.Add(new SignalData
						{
							Key = Key,
							Numbers = Numbers.ToArray(),
							BaseObjects = Objects.ToArray()
						});
					}
					else
					{
						if (RouteData.SignalObjects.Any())
						{
							RouteData.SignalObjects.Last().GlowObjects = Objects.ToArray();
						}
					}
				}
			}
		}

		private static void LoadSoundList(string FileName, bool PreviewOnly, MapData ParseData, RouteData RouteData)
		{
			RouteData.Sounds = new SoundDictionary();

			if (PreviewOnly || string.IsNullOrEmpty(ParseData.SoundListPath))
			{
				return;
			}

			if (!File.Exists(ParseData.SoundListPath))
			{
				ParseData.SoundListPath = Path.CombineFile(System.IO.Path.GetDirectoryName(FileName), ParseData.SoundListPath);

				if (!File.Exists(ParseData.SoundListPath))
				{
					Interface.AddMessage(MessageType.Error, false, ParseData.SoundListPath + "is not found.");
					return;
				}
			}

			string BaseDirectory = System.IO.Path.GetDirectoryName(ParseData.SoundListPath);

			System.Text.Encoding Encoding = DetermineFileEncoding(ParseData.SoundListPath);
			string[] Lines = File.ReadAllLines(ParseData.SoundListPath, Encoding).Select(Line => Line.Trim('"').Trim()).ToArray();

			using (CsvReader Csv = new CsvReader(new StringReader(string.Join(Environment.NewLine, Lines))))
			{
				Csv.Configuration.AllowComments = true;
				while (Csv.Read())
				{
					string Key = string.Empty;
					string SoundFileName = string.Empty;

					for (int i = 0; i < Csv.CurrentRecord.Length; i++)
					{
						string Value = string.Empty;
						if (Csv.CurrentRecord[i] != null)
						{
							Value = Csv.CurrentRecord[i].Split('#')[0].Trim();
						}

						switch (i)
						{
							case 0:
								Key = Value;
								break;
							case 1:
								SoundFileName = Value;
								break;
						}
					}

					try
					{
						SoundFileName = Path.CombineFile(BaseDirectory, SoundFileName);
					}
					catch
					{
						// ignored
					}

					if (!File.Exists(SoundFileName))
					{
						Interface.AddMessage(MessageType.Error, false, SoundFileName + "is not found.");
						continue;
					}

					RouteData.Sounds.Add(Key, Sounds.RegisterBuffer(SoundFileName, 15.0));
				}
			}
		}

		private static void LoadSound3DList(string FileName, bool PreviewOnly, MapData ParseData, RouteData RouteData)
		{
			RouteData.Sound3Ds = new SoundDictionary();

			if (PreviewOnly || string.IsNullOrEmpty(ParseData.Sound3DListPath))
			{
				return;
			}

			if (!File.Exists(ParseData.Sound3DListPath))
			{
				ParseData.Sound3DListPath = Path.CombineFile(System.IO.Path.GetDirectoryName(FileName), ParseData.Sound3DListPath);

				if (!File.Exists(ParseData.Sound3DListPath))
				{
					Interface.AddMessage(MessageType.Error, false, ParseData.Sound3DListPath + "is not found.");
					return;
				}
			}

			string BaseDirectory = System.IO.Path.GetDirectoryName(ParseData.Sound3DListPath);

			System.Text.Encoding Encoding = DetermineFileEncoding(ParseData.Sound3DListPath);
			string[] Lines = File.ReadAllLines(ParseData.Sound3DListPath, Encoding).Select(Line => Line.Trim('"').Trim()).ToArray();

			using (CsvReader Csv = new CsvReader(new StringReader(string.Join(Environment.NewLine, Lines))))
			{
				Csv.Configuration.AllowComments = true;
				while (Csv.Read())
				{
					string Key = string.Empty;
					string SoundFileName = string.Empty;

					for (int i = 0; i < Csv.CurrentRecord.Length; i++)
					{
						string Value = string.Empty;
						if (Csv.CurrentRecord[i] != null)
						{
							Value = Csv.CurrentRecord[i].Split('#')[0].Trim();
						}

						switch (i)
						{
							case 0:
								Key = Value;
								break;
							case 1:
								SoundFileName = Value;
								break;
						}
					}

					try
					{
						SoundFileName = Path.CombineFile(BaseDirectory, SoundFileName);
					}
					catch
					{
						// ignored
					}

					if (!File.Exists(SoundFileName))
					{
						Interface.AddMessage(MessageType.Error, false, SoundFileName + "is not found.");
						continue;
					}

					RouteData.Sound3Ds.Add(Key, Sounds.RegisterBuffer(SoundFileName, 15.0));
				}
			}
		}

		private static void LoadOtherTrain(string FileName, bool PreviewOnly, MapData ParseData, RouteData RouteData)
		{
			if (PreviewOnly)
			{
				return;
			}

			List<OtherTrain> OtherTrains = new List<OtherTrain>();

			foreach (var Statement in ParseData.Statements)
			{
				if (Statement.MapElement[0] != "train")
				{
					continue;
				}

				switch (Statement.Function)
				{
					case "add":
					case "load":
						object TrainKey, TrainFilePath, TrackKey, Direction;

						if (Statement.Function == "add")
						{
							if (!Statement.Arguments.TryGetValue("trainkey", out TrainKey) || TrainKey == null)
							{
								continue;
							}
						}
						else
						{
							TrainKey = Statement.Key;
						}
						if (!Statement.Arguments.TryGetValue("filepath", out TrainFilePath) || TrainFilePath == null)
						{
							continue;
						}
						if (!Statement.Arguments.TryGetValue("trackkey", out TrackKey) || Convert.ToString(TrackKey) == string.Empty)
						{
							TrackKey = "0";
						}
						if (!Statement.Arguments.TryGetValue("direction", out Direction) || Direction == null)
						{
							Direction = 1;
						}
						TrainFilePath = Path.CombineFile(System.IO.Path.GetDirectoryName(FileName), Convert.ToString(TrainFilePath));
						if (!File.Exists(Convert.ToString(TrainFilePath)))
						{
							Interface.AddMessage(MessageType.Error, false, Convert.ToString(TrainFilePath) + "is not found.");
							continue;
						}

						OtherTrains.Add(new OtherTrain
						{
							Key = Convert.ToString(TrainKey),
							FilePath = Convert.ToString(TrainFilePath),
							TrackKey = Convert.ToString(TrackKey),
							Direction = Convert.ToInt32(Direction)
						});
						break;
					default:
						continue;
				}
			}

			foreach (var Statement in ParseData.Statements)
			{
				if (Statement.MapElement[0] != "train" || Statement.Function != "settrack")
				{
					continue;
				}

				object TrackKey, Direction;
				object TrainKey = Statement.Key;

				if (!Statement.Arguments.TryGetValue("trackkey", out TrackKey) || Convert.ToString(TrackKey) == string.Empty)
				{
					TrackKey = "0";
				}
				if (!Statement.Arguments.TryGetValue("direction", out Direction) || Direction == null)
				{
					Direction = 1;
				}

				int TrainIndex = OtherTrains.FindIndex(Train => Train.Key.Equals(Convert.ToString(TrainKey), StringComparison.InvariantCultureIgnoreCase));
				if (TrainIndex == -1)
				{
					continue;
				}

				OtherTrains[TrainIndex].TrackKey = Convert.ToString(TrackKey);
				OtherTrains[TrainIndex].Direction = Convert.ToInt32(Direction);
			}

			foreach (var OtherTrain in OtherTrains)
			{
				int RailIndex = RouteData.TrackKeyList.IndexOf(OtherTrain.TrackKey);
				if (RailIndex == -1)
				{
					continue;
				}

				ParseOtherTrain(OtherTrain);

				if (!OtherTrain.CarObjects.Any())
				{
					continue;
				}

				OtherTrain.CarObjects = OtherTrain.CarObjects.OrderByDescending(Object => Object.Distance).ToList();

				TrainManager.OtherTrain Train = new TrainManager.OtherTrain(TrainManager.TrainState.Pending);
				Train.Cars = new TrainManager.Car[OtherTrain.CarObjects.Count];
				Train.Couplers = new TrainManager.Coupler[OtherTrain.CarObjects.Count - 1];
				Train.Handles.Reverser = new TrainManager.ReverserHandle();

				for (int i = 0; i < Train.Cars.Length; i++)
				{
					Train.Cars[i] = new TrainManager.Car(Train, i);
					Train.Cars[i].CurrentCarSection = -1;
					Train.Cars[i].ChangeCarSection(TrainManager.CarSectionType.NotVisible);
					Train.Cars[i].FrontBogie.ChangeSection(-1);
					Train.Cars[i].RearBogie.ChangeSection(-1);
					Train.Cars[i].FrontAxle.Follower.TriggerType = i == 0 ? TrackManager.EventTriggerType.FrontCarFrontAxle : TrackManager.EventTriggerType.OtherCarFrontAxle;
					Train.Cars[i].RearAxle.Follower.TriggerType = i == OtherTrain.CarObjects.Count - 1 ? TrackManager.EventTriggerType.RearCarRearAxle : TrackManager.EventTriggerType.OtherCarRearAxle;
					Train.Cars[i].BeaconReceiver.TriggerType = i == 0 ? TrackManager.EventTriggerType.TrainFront : TrackManager.EventTriggerType.None;
					Train.Cars[i].FrontAxle.Follower.CarIndex = i;
					Train.Cars[i].RearAxle.Follower.CarIndex = i;
					Train.Cars[i].FrontAxle.Position = OtherTrain.CarObjects[i].Span - OtherTrain.CarObjects[i].Z;
					Train.Cars[i].RearAxle.Position = -OtherTrain.CarObjects[i].Z;
					Train.Cars[i].Doors[0].Direction = -1;
					Train.Cars[i].Doors[1].Direction = 1;
					Train.Cars[i].Width = 2.6;
					Train.Cars[i].Height = 3.2;
					if (i + 1 < Train.Cars.Length)
					{
						Train.Cars[i].Length = OtherTrain.CarObjects[i].Distance - OtherTrain.CarObjects[i + 1].Distance;
					}
					else if (i > 0)
					{
						Train.Cars[i].Length = Train.Cars[i - 1].Length;
					}
					else
					{
						Train.Cars[i].Length = OtherTrain.CarObjects[i].Span;
					}
					Train.Cars[i].Specs.CenterOfGravityHeight = 1.5;
					Train.Cars[i].Specs.CriticalTopplingAngle = 0.5 * Math.PI - Math.Atan(2 * Train.Cars[i].Specs.CenterOfGravityHeight / Train.Cars[i].Width);

					UnifiedObject CarObject;
					RouteData.Objects.TryGetValue(OtherTrain.CarObjects[i].Key, out CarObject);
					if (CarObject != null)
					{
						Train.Cars[i].LoadCarSections(CarObject);
					}
				}

				List<Game.TravelData> Data = new List<Game.TravelData>();

				foreach (var Statement in ParseData.Statements)
				{
					if (Statement.MapElement[0] != "train" || Statement.Key != OtherTrain.Key)
					{
						continue;
					}

					switch (Statement.Function)
					{
						case "enable":
							{
								object TempTime;
								double Time;
								Statement.Arguments.TryGetValue("time", out TempTime);

								TryParseBve5Time(Convert.ToString(TempTime), out Time);

								Train.AppearanceStartPosition = Statement.Distance;
								Train.AppearanceTime = Time;
							}
							break;
						case "stop":
							{
								object Decelerate, StopTime, Accelerate, Speed;
								Statement.Arguments.TryGetValue("decelerate", out Decelerate);
								Statement.Arguments.TryGetValue("stopTime", out StopTime);
								Statement.Arguments.TryGetValue("accelerate", out Accelerate);
								Statement.Arguments.TryGetValue("speed", out Speed);

								Data.Add(new Game.TravelData
								{
									Decelerate = Convert.ToDouble(Decelerate) / 3.6,
									StopPosition = Statement.Distance,
									StopTime = Convert.ToDouble(StopTime),
									Accelerate = Convert.ToDouble(Accelerate) / 3.6,
									TargetSpeed = Convert.ToDouble(Speed) / 3.6,
									Direction = (Game.TravelDirection)OtherTrain.Direction,
									RailIndex = RailIndex
								});
							}
							break;
					}
				}

				if (OtherTrain.Direction == -1)
				{
					Data = Data.OrderByDescending(d => d.StopPosition).ToList();
				}

				if (!Data.Any())
				{
					continue;
				}

				Train.AI = new Game.OtherTrainAI(Train, Data);

				// For debug
				Interface.AddMessage(MessageType.Information, false, string.Format("[{0}] 走行軌道: {1}, 進行方向: {2}, 有効開始位置: {3}m, 有効開始時刻: {4}s", OtherTrain.Key, OtherTrain.TrackKey, OtherTrain.Direction, Train.AppearanceStartPosition, Train.AppearanceTime));
				foreach (var d in Data)
				{
					Interface.AddMessage(MessageType.Information, false, string.Format("[{0}] 停車位置: {1}m, 減速度: {2}km/h/s, 停車時間: {3}s, 加速度: {4}km/h/s, 加速後の走行速度: {5}km/h", OtherTrain.Key, d.StopPosition, d.Decelerate * 3.6, d.StopTime, d.Accelerate * 3.6, d.TargetSpeed * 3.6));
				}

				foreach (var Car in Train.Cars)
				{
					Car.FrontAxle.Follower.TrackIndex = Data[0].RailIndex;
					Car.RearAxle.Follower.TrackIndex = Data[0].RailIndex;
					Car.FrontBogie.FrontAxle.Follower.TrackIndex = Data[0].RailIndex;
					Car.FrontBogie.RearAxle.Follower.TrackIndex = Data[0].RailIndex;
					Car.RearBogie.FrontAxle.Follower.TrackIndex = Data[0].RailIndex;
					Car.RearBogie.RearAxle.Follower.TrackIndex = Data[0].RailIndex;
				}

				Train.PlaceCars(Data[0].StopPosition);

				int n = TrainManager.OtherTrains.Length;
				Array.Resize(ref TrainManager.OtherTrains, n + 1);
				TrainManager.OtherTrains[n] = Train;
			}
		}

		private static void ParseOtherTrain(OtherTrain OtherTrain)
		{
			OtherTrain.CarObjects = new List<CarObject>();
			OtherTrain.CarSounds = new List<CarSound>();

			System.Text.Encoding Encoding = DetermineFileEncoding(OtherTrain.FilePath);

			string[] Lines = File.ReadAllLines(OtherTrain.FilePath, Encoding).Skip(1).ToArray();
			List<Dictionary<string, string>> Structures = new List<Dictionary<string, string>>();
			List<Dictionary<string, string>> Sound3ds = new List<Dictionary<string, string>>();
			string Section = string.Empty;

			for (int i = 0; i < Lines.Length; i++)
			{
				Lines[i] = Lines[i].Trim();

				if (!Lines[i].Any() || Lines[i].StartsWith(";", StringComparison.OrdinalIgnoreCase) || Lines[i].StartsWith("#", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				if (Lines[i].StartsWith("[", StringComparison.Ordinal) & Lines[i].EndsWith("]", StringComparison.Ordinal))
				{
					Section = Lines[i].Substring(1, Lines[i].Length - 2).Trim().ToLowerInvariant();

					switch (Section)
					{
						case "structure":
							Structures.Add(new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase));
							break;
						case "sound3d":
							Sound3ds.Add(new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase));
							break;
					}
				}
				else
				{
					int j = Lines[i].IndexOf("=", StringComparison.OrdinalIgnoreCase);
					string Key, Value;
					if (j >= 0)
					{
						Key = Lines[i].Substring(0, j).TrimEnd().ToLowerInvariant();
						Value = Lines[i].Substring(j + 1).TrimStart().ToLowerInvariant();
					}
					else
					{
						Key = string.Empty;
						Value = Lines[i];
					}

					switch (Section)
					{
						case "structure":
							Structures.Last()[Key] = Value;
							break;
						case "sound3d":
							Sound3ds.Last()[Key] = Value;
							break;
					}
				}
			}

			foreach (var Structure in Structures)
			{
				string Key, TempDistance, TempSpan, TempZ;

				Structure.TryGetValue("key", out Key);
				Structure.TryGetValue("distance", out TempDistance);
				Structure.TryGetValue("span", out TempSpan);
				Structure.TryGetValue("z", out TempZ);

				double Distance, Span, Z;
				if (string.IsNullOrEmpty(TempDistance) || !NumberFormats.TryParseDoubleVb6(TempDistance, out Distance))
				{
					Distance = 0.0;
				}
				if (string.IsNullOrEmpty(TempSpan) || !NumberFormats.TryParseDoubleVb6(TempSpan, out Span))
				{
					Span = 0.0;
				}
				if (string.IsNullOrEmpty(TempZ) || !NumberFormats.TryParseDoubleVb6(TempZ, out Z))
				{
					Z = 0.0;
				}

				OtherTrain.CarObjects.Add(new CarObject
				{
					Key = Key,
					Distance = Distance,
					Span = Span,
					Z = Z
				});
			}

			foreach (var Sound3d in Sound3ds)
			{
				string Key, TempDistance1, TempDistance2, Function;

				Sound3d.TryGetValue("key", out Key);
				Sound3d.TryGetValue("distance1", out TempDistance1);
				Sound3d.TryGetValue("distance2", out TempDistance2);
				Sound3d.TryGetValue("function", out Function);

				double Distance1;
				double Distance2;
				if (string.IsNullOrEmpty(TempDistance1) || !NumberFormats.TryParseDoubleVb6(TempDistance1, out Distance1))
				{
					Distance1 = 0.0;
				}
				if (string.IsNullOrEmpty(TempDistance2) || !NumberFormats.TryParseDoubleVb6(TempDistance2, out Distance2))
				{
					Distance2 = 0.0;
				}

				OtherTrain.CarSounds.Add(new CarSound
				{
					Key = Key,
					Distance1 = Distance1,
					Distance2 = Distance2,
					Function = Function
				});
			}
		}
	}
}
