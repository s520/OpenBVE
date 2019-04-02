using System;
using System.Linq;
using OpenBveApi.Colors;
using OpenBveApi.FunctionScripting;
using OpenBveApi.Interface;
using OpenBveApi.Math;
using OpenBveApi.Objects;
using OpenBveApi.Runtime;
using OpenBveApi.World;

namespace OpenBve
{
	static partial class Bve5ScenarioParser
	{
		private static void ApplyRouteData(string FileName, bool PreviewOnly, RouteData Data)
		{
			Game.UnitOfSpeed = "km/h";
			Game.SpeedConversionFactor = 0.0;
			Game.RouteInformation.RouteBriefing = null;

			Game.Sections = new Game.Section[1];
			Game.Sections[0].Aspects = new Game.SectionAspect[] { new Game.SectionAspect(0, 0.0), new Game.SectionAspect(4, double.PositiveInfinity) };
			Game.Sections[0].CurrentAspect = 0;
			Game.Sections[0].NextSection = -1;
			Game.Sections[0].PreviousSection = -1;
			Game.Sections[0].StationIndex = -1;
			Game.Sections[0].TrackPosition = 0;
			Game.Sections[0].Trains = new TrainManager.Train[] { };

			System.Globalization.CultureInfo Culture = System.Globalization.CultureInfo.InvariantCulture;

			// background
			if (!PreviewOnly)
			{
				if (Data.Blocks[0].Background >= 0 & Data.Blocks[0].Background < Data.Backgrounds.Count)
				{
					BackgroundManager.CurrentBackground = Data.Backgrounds[Data.Blocks[0].Background].Handle;
				}
				else
				{
					BackgroundManager.CurrentBackground = new BackgroundManager.StaticBackground(null, 6, false);
				}
				BackgroundManager.TargetBackground = BackgroundManager.CurrentBackground;
			}

			// brightness
			int CurrentBrightnessElement = -1;
			int CurrentBrightnessEvent = -1;
			float CurrentBrightnessValue = 1.0f;
			double CurrentBrightnessTrackPosition = 0.0;
			if (!PreviewOnly)
			{
				for (int i = 0; i < Data.Blocks.Count; i++)
				{
					if (Data.Blocks[i].BrightnessChanges != null && Data.Blocks[i].BrightnessChanges.Any())
					{
						CurrentBrightnessValue = Data.Blocks[i].BrightnessChanges[0].Value;
						CurrentBrightnessTrackPosition = Data.Blocks[i].BrightnessChanges[0].Value;
						break;
					}
				}
			}

			// create objects and track
			Vector3 Position = Vector3.Zero;
			Vector2 Direction = new Vector2(0.0, 1.0);
			double CurrentSpeedLimit = double.PositiveInfinity;
			int CurrentRunIndex = 0;
			int CurrentFlangeIndex = 0;
			int CurrentTrackLength = 0;
			int PreviousFogElement = -1;
			int PreviousFogEvent = -1;
			Game.Fog PreviousFog = new Game.Fog(Game.NoFogStart, Game.NoFogEnd, Color24.Grey, -InterpolateInterval);
			Array.Resize(ref TrackManager.Tracks, Data.TrackKeyList.Count);
			for (int j = 0; j < TrackManager.Tracks.Length; j++)
			{
				if (TrackManager.Tracks[j].Elements == null)
				{
					TrackManager.Tracks[j].Elements = new TrackManager.TrackElement[256];
				}
			}

			// process blocks
			double progressFactor = Data.Blocks.Count == 0 ? 0.5 : 0.5 / Data.Blocks.Count;
			for (int i = 0; i < Data.Blocks.Count; i++)
			{
				Loading.RouteProgress = 0.6667 + i * progressFactor;
				if ((i & 15) == 0)
				{
					System.Threading.Thread.Sleep(1);
					if (Loading.Cancel) return;
				}

				double StartingDistance = Data.Blocks[i].StartingDistance;
				double EndingDistance = i < Data.Blocks.Count - 1 ? Data.Blocks[i + 1].StartingDistance : StartingDistance + InterpolateInterval;
				double BlockInterval = EndingDistance - StartingDistance;

				// normalize
				Direction.Normalize();

				TrackManager.TrackElement WorldTrackElement = Data.Blocks[i].CurrentTrackState;
				int n = CurrentTrackLength;
				for (int j = 0; j < TrackManager.Tracks.Length; j++)
				{
					if (n >= TrackManager.Tracks[j].Elements.Length)
					{
						Array.Resize(ref TrackManager.Tracks[j].Elements, TrackManager.Tracks[j].Elements.Length << 1);
					}
				}
				CurrentTrackLength++;
				TrackManager.Tracks[0].Elements[n] = WorldTrackElement;
				TrackManager.Tracks[0].Elements[n].WorldPosition = Position;
				TrackManager.Tracks[0].Elements[n].WorldDirection = Vector3.GetVector3(Direction, Data.Blocks[i].Pitch);
				TrackManager.Tracks[0].Elements[n].WorldSide = new Vector3(Direction.Y, 0.0, -Direction.X);
				TrackManager.Tracks[0].Elements[n].WorldUp = Vector3.Cross(TrackManager.Tracks[0].Elements[n].WorldDirection, TrackManager.Tracks[0].Elements[n].WorldSide);
				TrackManager.Tracks[0].Elements[n].StartingTrackPosition = StartingDistance;
				TrackManager.Tracks[0].Elements[n].AdhesionMultiplier = Data.Blocks[i].AdhesionMultiplier;
				TrackManager.Tracks[0].Elements[n].CsvRwAccuracyLevel = Data.Blocks[i].Accuracy;
				for (int j = 0; j < TrackManager.Tracks.Length; j++)
				{
					TrackManager.Tracks[j].Elements[n].Events = new TrackManager.GeneralEvent[] { };
				}

				// background
				if (!PreviewOnly)
				{
					if (Data.Blocks[i].Background >= 0)
					{
						int typ;
						if (i == 0)
						{
							typ = Data.Blocks[i].Background;
						}
						else
						{
							typ = Data.Backgrounds.Count > 0 ? 0 : -1;
							for (int j = i - 1; j >= 0; j--)
							{
								if (Data.Blocks[j].Background >= 0)
								{
									typ = Data.Blocks[j].Background;
									break;
								}
							}
						}
						if (typ >= 0 & typ < Data.Backgrounds.Count)
						{
							int m = TrackManager.Tracks[0].Elements[n].Events.Length;
							Array.Resize(ref TrackManager.Tracks[0].Elements[n].Events, m + 1);
							TrackManager.Tracks[0].Elements[n].Events[m] = new TrackManager.BackgroundChangeEvent(0.0, Data.Backgrounds[typ].Handle, Data.Backgrounds[Data.Blocks[i].Background].Handle);
						}
					}
				}

				// brightness
				if (!PreviewOnly)
				{
					for (int j = 0; j < Data.Blocks[i].BrightnessChanges.Count; j++)
					{
						int m = TrackManager.Tracks[0].Elements[n].Events.Length;
						for (int k = 0; k < TrackManager.Tracks.Length; k++)
						{
							Array.Resize(ref TrackManager.Tracks[k].Elements[n].Events, m + 1);
							double d = Data.Blocks[i].BrightnessChanges[j].TrackPosition - StartingDistance;
							TrackManager.Tracks[k].Elements[n].Events[m] = new TrackManager.BrightnessChangeEvent(d, Data.Blocks[i].BrightnessChanges[j].Value, CurrentBrightnessValue, Data.Blocks[i].BrightnessChanges[j].TrackPosition - CurrentBrightnessTrackPosition);
							if (CurrentBrightnessElement >= 0 & CurrentBrightnessEvent >= 0)
							{
								TrackManager.BrightnessChangeEvent bce = (TrackManager.BrightnessChangeEvent)TrackManager.Tracks[k].Elements[CurrentBrightnessElement].Events[CurrentBrightnessEvent];
								bce.NextBrightness = Data.Blocks[i].BrightnessChanges[j].Value;
								bce.NextDistance = Data.Blocks[i].BrightnessChanges[j].TrackPosition - CurrentBrightnessTrackPosition;
							}
						}
						CurrentBrightnessElement = n;
						CurrentBrightnessEvent = m;
						CurrentBrightnessValue = Data.Blocks[i].BrightnessChanges[j].Value;
						CurrentBrightnessTrackPosition = Data.Blocks[i].BrightnessChanges[j].TrackPosition;
					}
				}

				// fog
				if (!PreviewOnly)
				{
					if (Data.Blocks[i].FogDefined)
					{
						if (i == 0 && StartingDistance == 0)
						{
							//Fog starts at zero position
							PreviousFog = Data.Blocks[i].Fog;
						}
						Data.Blocks[i].Fog.TrackPosition = StartingDistance;
						int m = TrackManager.Tracks[0].Elements[n].Events.Length;
						Array.Resize(ref TrackManager.Tracks[0].Elements[n].Events, m + 1);
						TrackManager.Tracks[0].Elements[n].Events[m] = new TrackManager.FogChangeEvent(0.0, PreviousFog, Data.Blocks[i].Fog, Data.Blocks[i].Fog);
						if (PreviousFogElement >= 0 & PreviousFogEvent >= 0)
						{
							TrackManager.FogChangeEvent e = (TrackManager.FogChangeEvent)TrackManager.Tracks[0].Elements[PreviousFogElement].Events[PreviousFogEvent];
							e.NextFog = Data.Blocks[i].Fog;
						}
						else
						{
							Game.PreviousFog = PreviousFog;
							Game.CurrentFog = PreviousFog;
							Game.NextFog = Data.Blocks[i].Fog;
						}
						PreviousFog = Data.Blocks[i].Fog;
						PreviousFogElement = n;
						PreviousFogEvent = m;
					}
				}

				// rail sounds
				if (!PreviewOnly)
				{
					for (int k = 0; k < Data.Blocks[i].RunSounds.Count; k++)
					{
						int r = Data.Blocks[i].RunSounds[k].SoundIndex;
						if (r != CurrentRunIndex)
						{
							int m = TrackManager.Tracks[0].Elements[n].Events.Length;
							Array.Resize(ref TrackManager.Tracks[0].Elements[n].Events, m + 1);
							double d = Data.Blocks[i].RunSounds[k].TrackPosition - StartingDistance;
							if (d > 0.0)
							{
								d = 0.0;
							}
							TrackManager.Tracks[0].Elements[n].Events[m] = new TrackManager.RailSoundsChangeEvent(d, CurrentRunIndex, CurrentFlangeIndex, r, CurrentFlangeIndex);
							CurrentRunIndex = r;
						}
					}

					for (int k = 0; k < Data.Blocks[i].FlangeSounds.Count; k++)
					{
						int f = Data.Blocks[i].FlangeSounds[k].SoundIndex;
						if (f != CurrentFlangeIndex)
						{
							int m = TrackManager.Tracks[0].Elements[n].Events.Length;
							Array.Resize(ref TrackManager.Tracks[0].Elements[n].Events, m + 1);
							double d = Data.Blocks[i].FlangeSounds[k].TrackPosition - StartingDistance;
							if (d > 0.0)
							{
								d = 0.0;
							}
							TrackManager.Tracks[0].Elements[n].Events[m] = new TrackManager.RailSoundsChangeEvent(d, CurrentRunIndex, CurrentFlangeIndex, CurrentRunIndex, f);
							CurrentFlangeIndex = f;
						}
					}

					if (Data.Blocks[i].JointSound)
					{
						int m = TrackManager.Tracks[0].Elements[n].Events.Length;
						Array.Resize(ref TrackManager.Tracks[0].Elements[n].Events, m + 1);
						TrackManager.Tracks[0].Elements[n].Events[m] = new TrackManager.PointSoundEvent(12.5);
					}
				}

				// station
				if (Data.Blocks[i].Station >= 0)
				{
					int s = Data.Blocks[i].Station;
					int m = TrackManager.Tracks[0].Elements[n].Events.Length;
					Array.Resize(ref TrackManager.Tracks[0].Elements[n].Events, m + 1);
					TrackManager.Tracks[0].Elements[n].Events[m] = new TrackManager.StationStartEvent(0.0, s);
					double dx, dy = 3.0;
					if (Game.Stations[s].OpenLeftDoors & !Game.Stations[s].OpenRightDoors)
					{
						dx = -5.0;
					}
					else if (!Game.Stations[s].OpenLeftDoors & Game.Stations[s].OpenRightDoors)
					{
						dx = 5.0;
					}
					else
					{
						dx = 0.0;
					}
					Game.Stations[s].SoundOrigin = Position + dx * TrackManager.Tracks[0].Elements[n].WorldSide + dy * TrackManager.Tracks[0].Elements[n].WorldUp;
				}

				// stop
				if (Data.Blocks[i].Stop >= 0)
				{
					int s = Data.Blocks[i].Stop;
					double dx, dy = 3.0;
					if (Game.Stations[s].OpenLeftDoors & !Game.Stations[s].OpenRightDoors)
					{
						dx = -5.0;
					}
					else if (!Game.Stations[s].OpenLeftDoors & Game.Stations[s].OpenRightDoors)
					{
						dx = 5.0;
					}
					else
					{
						dx = 0.0;
					}
					Game.Stations[s].SoundOrigin = Position + dx * TrackManager.Tracks[0].Elements[n].WorldSide + dy * TrackManager.Tracks[0].Elements[n].WorldUp;
				}

				// limit
				if (!PreviewOnly)
				{
					for (int k = 0; k < Data.Blocks[i].Limits.Count; k++)
					{
						int m = TrackManager.Tracks[0].Elements[n].Events.Length;
						Array.Resize(ref TrackManager.Tracks[0].Elements[n].Events, m + 1);
						double d = Data.Blocks[i].Limits[k].TrackPosition - StartingDistance;
						TrackManager.Tracks[0].Elements[n].Events[m] = new TrackManager.LimitChangeEvent(d, CurrentSpeedLimit, Data.Blocks[i].Limits[k].Speed);
						CurrentSpeedLimit = Data.Blocks[i].Limits[k].Speed;
					}
				}

				// sound
				if (!PreviewOnly)
				{
					for (int k = 0; k < Data.Blocks[i].SoundEvents.Count; k++)
					{
						if (Data.Blocks[i].SoundEvents[k].Type == SoundType.TrainStatic)
						{
							Sounds.SoundBuffer buffer;
							Data.Sounds.TryGetValue(Data.Blocks[i].SoundEvents[k].Key, out buffer);

							if (buffer != null)
							{
								int m = TrackManager.Tracks[0].Elements[n].Events.Length;
								Array.Resize(ref TrackManager.Tracks[0].Elements[n].Events, m + 1);
								double d = Data.Blocks[i].SoundEvents[k].TrackPosition - StartingDistance;
								TrackManager.Tracks[0].Elements[n].Events[m] = new TrackManager.SoundEvent(d, buffer, true, true, false, Vector3.Zero, 0.0);
							}
						}
					}
				}

				// sections
				if (!PreviewOnly)
				{
					// sections
					for (int k = 0; k < Data.Blocks[i].Sections.Count; k++)
					{
						int m = Game.Sections.Length;
						Array.Resize(ref Game.Sections, m + 1);

						// create section
						Game.Sections[m].TrackPosition = Data.Blocks[i].Sections[k].TrackPosition;
						Game.Sections[m].Aspects = new Game.SectionAspect[Data.Blocks[i].Sections[k].Aspects.Length];
						for (int l = 0; l < Data.Blocks[i].Sections[k].Aspects.Length; l++)
						{
							Game.Sections[m].Aspects[l].Number = Data.Blocks[i].Sections[k].Aspects[l];
							if (Data.Blocks[i].Sections[k].Aspects[l] >= 0 & Data.Blocks[i].Sections[k].Aspects[l] < Data.SignalSpeeds.Length)
							{
								Game.Sections[m].Aspects[l].Speed = Data.SignalSpeeds[Data.Blocks[i].Sections[k].Aspects[l]];
							}
							else
							{
								Game.Sections[m].Aspects[l].Speed = double.PositiveInfinity;
							}
						}
						Game.Sections[m].Type = Game.SectionType.IndexBased;
						Game.Sections[m].CurrentAspect = -1;
						if (m > 0)
						{
							Game.Sections[m].PreviousSection = m - 1;
							Game.Sections[m - 1].NextSection = m;
						}
						else
						{
							Game.Sections[m].PreviousSection = -1;
						}
						Game.Sections[m].NextSection = -1;
						Game.Sections[m].StationIndex = Data.Blocks[i].Sections[k].DepartureStationIndex;
						Game.Sections[m].Invisible = false;
						Game.Sections[m].Trains = new TrainManager.Train[] { };

						// create section change event
						double d = Data.Blocks[i].Sections[k].TrackPosition - StartingDistance;
						int p = TrackManager.Tracks[0].Elements[n].Events.Length;
						Array.Resize(ref TrackManager.Tracks[0].Elements[n].Events, p + 1);
						TrackManager.Tracks[0].Elements[n].Events[p] = new TrackManager.SectionChangeEvent(d, m - 1, m);
					}
				}

				// rail-aligned objects
				if (!PreviewOnly)
				{
					for (int j = 0; j < Data.Blocks[i].Rails.Length; j++)
					{
						// free objects
						if (Data.Blocks[i].FreeObj.Length > j && Data.Blocks[i].FreeObj[j] != null)
						{
							double turn = Data.Blocks[i].Turn;
							double curveRadius = Data.Blocks[i].CurrentTrackState.CurveRadius;
							double curveCant = j == 0 ? Data.Blocks[i].CurrentTrackState.CurveCant : Data.Blocks[i].Rails[j].CurveCant;
							double pitch = Data.Blocks[i].Pitch;
							double x = Data.Blocks[i].Rails[j].RailX;
							double y = Data.Blocks[i].Rails[j].RailY;
							double radiusH = Data.Blocks[i].Rails[j].RadiusH;
							double radiusV = Data.Blocks[i].Rails[j].RadiusV;
							double nextStartingDistance = StartingDistance + BlockInterval;
							double nextX = i < Data.Blocks.Count - 1 ? Data.Blocks[i + 1].Rails[j].RailX : x;
							double nextY = i < Data.Blocks.Count - 1 ? Data.Blocks[i + 1].Rails[j].RailY : y;

							for (int k = 0; k < Data.Blocks[i].FreeObj[j].Count; k++)
							{
								string key = Data.Blocks[i].FreeObj[j][k].Key;
								double span = Data.Blocks[i].FreeObj[j][k].Span;
								int type = Data.Blocks[i].FreeObj[j][k].Type;
								double dx = Data.Blocks[i].FreeObj[j][k].X;
								double dy = Data.Blocks[i].FreeObj[j][k].Y;
								double dz = Data.Blocks[i].FreeObj[j][k].Z;
								double tpos = Data.Blocks[i].FreeObj[j][k].TrackPosition;
								Vector3 wpos;
								Transformation Transformation;
								if (j == 0)
								{
									GetTransformation(Position, StartingDistance, turn, curveRadius, curveCant, pitch, tpos, type, span, Direction, out wpos, out Transformation);
								}
								else
								{
									GetTransformation(Position, StartingDistance, turn, curveRadius, pitch, x, y, radiusH, radiusV, curveCant, nextStartingDistance, nextX, nextY, tpos, type, span, Direction, out wpos, out Transformation);
								}
								wpos += dx * Transformation.X + dy * Transformation.Y + dz * Transformation.Z;
								UnifiedObject obj;
								Data.Objects.TryGetValue(key, out obj);
								if (obj != null)
								{
									obj.CreateObject(wpos, Transformation, new Transformation(Data.Blocks[i].FreeObj[j][k].Yaw, Data.Blocks[i].FreeObj[j][k].Pitch, Data.Blocks[i].FreeObj[j][k].Roll), -1, Data.AccurateObjectDisposal, StartingDistance, EndingDistance, BlockInterval, tpos, 1.0, false);
								}
							}
						}

						// cracks
						for (int k = 0; k < Data.Blocks[i].Cracks.Count; k++)
						{
							if (Data.Blocks[i].Cracks[k].PrimaryRail == j)
							{
								double turn = Data.Blocks[i].Turn;
								double curveRadius = Data.Blocks[i].CurrentTrackState.CurveRadius;
								double pitch = Data.Blocks[i].Pitch;
								double nextStartingDistance = StartingDistance + BlockInterval;

								int p = Data.Blocks[i].Cracks[k].PrimaryRail;
								double px0 = Data.Blocks[i].Rails[p].RailX;
								double py0 = Data.Blocks[i].Rails[p].RailY;
								double pRadiusH = Data.Blocks[i].Rails[p].RadiusH;
								double pRadiusV = Data.Blocks[i].Rails[p].RadiusV;
								double py1 = i < Data.Blocks.Count - 1 ? Data.Blocks[i + 1].Rails[p].RailY : py0;
								double px1 = i < Data.Blocks.Count - 1 ? Data.Blocks[i + 1].Rails[p].RailX : px0;

								int s = Data.Blocks[i].Cracks[k].SecondaryRail;
								double sx0 = Data.Blocks[i].Rails[s].RailX;
								double sRadiusH = Data.Blocks[i].Rails[s].RadiusH;
								double sx1 = i < Data.Blocks.Count - 1 ? Data.Blocks[i + 1].Rails[s].RailX : sx0;

								string key = Data.Blocks[i].Cracks[k].Key;
								double tpos = Data.Blocks[i].Cracks[k].TrackPosition;
								Vector3 wpos;
								Transformation Transformation;
								if (j == 0)
								{
									GetTransformation(Position, StartingDistance, turn, curveRadius, 0.0, pitch, tpos, 1, InterpolateInterval, Direction, out wpos, out Transformation);
								}
								else
								{
									GetTransformation(Position, StartingDistance, turn, curveRadius, pitch, px0, py0, pRadiusH, pRadiusV, 0.0, nextStartingDistance, px1, py1, tpos, 1, InterpolateInterval, Direction, out wpos, out Transformation);
								}

								double pInterpolateX0 = GetTrackCoordinate(StartingDistance, px0, nextStartingDistance, px1, pRadiusH, tpos);
								double pInterpolateX1 = GetTrackCoordinate(StartingDistance, px0, nextStartingDistance, px1, pRadiusH, tpos + InterpolateInterval);
								double sInterpolateX0 = GetTrackCoordinate(StartingDistance, sx0, nextStartingDistance, sx1, sRadiusH, tpos);
								double sInterpolateX1 = GetTrackCoordinate(StartingDistance, sx0, nextStartingDistance, sx1, sRadiusH, tpos + InterpolateInterval);
								double d0 = sInterpolateX0 - pInterpolateX0;
								double d1 = sInterpolateX1 - pInterpolateX1;

								UnifiedObject obj;
								Data.Objects.TryGetValue(key, out obj);
								if (obj != null)
								{
									ObjectManager.StaticObject crack = GetTransformedStaticObject((ObjectManager.StaticObject)obj, d0, d1);
									ObjectManager.CreateStaticObject(crack, wpos, Transformation, new Transformation(0.0, 0.0, 0.0), Data.AccurateObjectDisposal, StartingDistance, EndingDistance, BlockInterval, tpos);
								}
							}
						}

						// signals
						if (Data.Blocks[i].Signals.Length > j && Data.Blocks[i].Signals[j] != null)
						{
							double turn = Data.Blocks[i].Turn;
							double curveRadius = Data.Blocks[i].CurrentTrackState.CurveRadius;
							double curveCant = j == 0 ? Data.Blocks[i].CurrentTrackState.CurveCant : Data.Blocks[i].Rails[j].CurveCant;
							double pitch = Data.Blocks[i].Pitch;
							double x = Data.Blocks[i].Rails[j].RailX;
							double y = Data.Blocks[i].Rails[j].RailY;
							double radiusH = Data.Blocks[i].Rails[j].RadiusH;
							double radiusV = Data.Blocks[i].Rails[j].RadiusV;
							double nextStartingDistance = StartingDistance + BlockInterval;
							double nextX = i < Data.Blocks.Count - 1 ? Data.Blocks[i + 1].Rails[j].RailX : x;
							double nextY = i < Data.Blocks.Count - 1 ? Data.Blocks[i + 1].Rails[j].RailY : y;

							for (int k = 0; k < Data.Blocks[i].Signals[j].Count; k++)
							{
								string key = Data.Blocks[i].Signals[j][k].SignalObjectKey;
								double span = Data.Blocks[i].Signals[j][k].Span;
								int type = Data.Blocks[i].Signals[j][k].Type;
								double dx = Data.Blocks[i].Signals[j][k].X;
								double dy = Data.Blocks[i].Signals[j][k].Y;
								double dz = Data.Blocks[i].Signals[j][k].Z;
								double tpos = Data.Blocks[i].Signals[j][k].TrackPosition;
								Vector3 wpos;
								Transformation Transformation;
								if (j == 0)
								{
									GetTransformation(Position, StartingDistance, turn, curveRadius, curveCant, pitch, tpos, type, span, Direction, out wpos, out Transformation);
								}
								else
								{
									GetTransformation(Position, StartingDistance, turn, curveRadius, pitch, x, y, radiusH, radiusV, curveCant, nextStartingDistance, nextX, nextY, tpos, type, span, Direction, out wpos, out Transformation);
								}
								wpos += dx * Transformation.X + dy * Transformation.Y + dz * Transformation.Z;

								SignalData sd = Data.SignalObjects.Find(data => data.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase));
								if (sd != null)
								{
									if (sd.Numbers.Any())
									{
										//double brightness = 0.25 + 0.75 * GetBrightness(ref Data, tpos);
										ObjectManager.AnimatedObjectCollection aoc = new ObjectManager.AnimatedObjectCollection();
										aoc.Objects = new ObjectManager.AnimatedObject[2];
										for (int m = 0; m < aoc.Objects.Length; m++)
										{
											aoc.Objects[m] = new ObjectManager.AnimatedObject();
											aoc.Objects[m].States = new ObjectManager.AnimatedObjectState[sd.Numbers.Length];
										}
										for (int m = 0; m < sd.Numbers.Length; m++)
										{
											aoc.Objects[0].States[m].Object = sd.BaseObjects[m].Clone();
											if (sd.GlowObjects != null && m < sd.GlowObjects.Length)
											{
												aoc.Objects[1].States[m].Object = sd.GlowObjects[m].Clone();
											}
											else
											{
												aoc.Objects[1].States[m].Object = new ObjectManager.StaticObject();
											}
										}
										string expr = "";
										for (int m = 0; m < sd.Numbers.Length - 1; m++)
										{
											expr += "section " + sd.Numbers[m].ToString(Culture) + " <= " + m.ToString(Culture) + " ";
										}
										expr += (sd.Numbers.Length - 1).ToString(Culture);
										for (int m = 0; m < sd.Numbers.Length - 1; m++)
										{
											expr += " ?";
										}

										double refreshRate = 1.0 + 0.01 * Program.RandomNumberGenerator.NextDouble();
										for (int m = 0; m < aoc.Objects.Length; m++)
										{
											aoc.Objects[m].StateFunction = new FunctionScript(Program.CurrentHost, expr, false);
											aoc.Objects[m].RefreshRate = refreshRate;
										}
										//aoc.CreateObject(wpos, Transformation, new Transformation(Data.Blocks[i].Signals[j][k].Yaw, Data.Blocks[i].Signals[j][k].Pitch, Data.Blocks[i].Signals[j][k].Roll), Data.Blocks[i].Signals[j][k].SectionIndex, Data.AccurateObjectDisposal, StartingDistance, EndingDistance, BlockInterval, tpos, brightness, false);
										aoc.CreateObject(wpos, Transformation, new Transformation(Data.Blocks[i].Signals[j][k].Yaw, Data.Blocks[i].Signals[j][k].Pitch, Data.Blocks[i].Signals[j][k].Roll), Data.Blocks[i].Signals[j][k].SectionIndex, Data.AccurateObjectDisposal, StartingDistance, EndingDistance, BlockInterval, tpos, 1.0, false);
									}
								}
							}
						}
					}
				}

				// turn
				if (Data.Blocks[i].Turn != 0.0)
				{
					double ag = -Math.Atan(Data.Blocks[i].Turn);
					double cosag = Math.Cos(ag);
					double sinag = Math.Sin(ag);
					Direction.Rotate(cosag, sinag);
					TrackManager.Tracks[0].Elements[n].WorldDirection.RotatePlane(cosag, sinag);
					TrackManager.Tracks[0].Elements[n].WorldSide.RotatePlane(cosag, sinag);
					TrackManager.Tracks[0].Elements[n].WorldUp = Vector3.Cross(TrackManager.Tracks[0].Elements[n].WorldDirection, TrackManager.Tracks[0].Elements[n].WorldSide);
				}

				//Pitch
				if (Data.Blocks[i].Pitch != 0.0)
				{
					TrackManager.Tracks[0].Elements[n].Pitch = Data.Blocks[i].Pitch;
				}
				else
				{
					TrackManager.Tracks[0].Elements[n].Pitch = 0.0;
				}

				// curves
				double a, c, h;
				CalcTransformation(WorldTrackElement.CurveRadius, Data.Blocks[i].Pitch, BlockInterval, ref Direction, out a, out c, out h);

				if (!PreviewOnly)
				{
					for (int j = 1; j < Data.Blocks[i].Rails.Length; j++)
					{
						double x = Data.Blocks[i].Rails[j].RailX;
						double y = Data.Blocks[i].Rails[j].RailY;
						Vector3 offset = new Vector3(Direction.Y * x, y, -Direction.X * x);
						Vector3 pos = Position + offset;

						// take orientation of upcoming block into account
						Vector2 Direction2 = Direction;
						Vector3 Position2 = Position;
						Position2.X += Direction.X * c;
						Position2.Y += h;
						Position2.Z += Direction.Y * c;
						if (a != 0.0)
						{
							Direction2.Rotate(Math.Cos(-a), Math.Sin(-a));
						}

						double StartingDistance2 = i < Data.Blocks.Count - 1 ? Data.Blocks[i + 1].StartingDistance : StartingDistance + InterpolateInterval;
						double EndingDistance2 = i < Data.Blocks.Count - 2 ? Data.Blocks[i + 2].StartingDistance : StartingDistance2 + InterpolateInterval;
						double BlockInterval2 = EndingDistance2 - StartingDistance2;
						double Turn2 = i < Data.Blocks.Count - 1 ? Data.Blocks[i + 1].Turn : 0.0;
						double CurveRadius2 = i < Data.Blocks.Count - 1 ? Data.Blocks[i + 1].CurrentTrackState.CurveRadius : 0.0;
						double Pitch2 = i < Data.Blocks.Count - 1 ? Data.Blocks[i + 1].Pitch : 0.0;

						if (Turn2 != 0.0)
						{
							double ag = -Math.Atan(Turn2);
							double cosag = Math.Cos(ag);
							double sinag = Math.Sin(ag);
							Direction2.Rotate(cosag, sinag);
						}

						double a2, c2, h2;
						CalcTransformation(CurveRadius2, Pitch2, BlockInterval2, ref Direction2, out a2, out c2, out h2);

						double x2 = i < Data.Blocks.Count - 1 ? Data.Blocks[i + 1].Rails[j].RailX : x;
						double y2 = i < Data.Blocks.Count - 1 ? Data.Blocks[i + 1].Rails[j].RailY : y;
						Vector3 offset2 = new Vector3(Direction2.Y * x2, y2, -Direction2.X * x2);
						Vector3 pos2 = Position2 + offset2;
						Vector3 r = new Vector3(pos2.X - pos.X, pos2.Y - pos.Y, pos2.Z - pos.Z);
						r.Normalize();

						Transformation RailTransformation;
						RailTransformation.Z = r;
						RailTransformation.X = new Vector3(r.Z, 0.0, -r.X);
						World.Normalize(ref RailTransformation.X.X, ref RailTransformation.X.Z);
						RailTransformation.Y = Vector3.Cross(RailTransformation.Z, RailTransformation.X);
						RailTransformation = new Transformation(RailTransformation, 0.0, 0.0, Math.Atan(Data.Blocks[i].Rails[j].CurveCant));

						TrackManager.Tracks[j].Elements[n].StartingTrackPosition = StartingDistance;
						TrackManager.Tracks[j].Elements[n].WorldPosition = pos;
						TrackManager.Tracks[j].Elements[n].WorldDirection = RailTransformation.Z;
						TrackManager.Tracks[j].Elements[n].WorldSide = RailTransformation.X;
						TrackManager.Tracks[j].Elements[n].WorldUp = RailTransformation.Y;
						TrackManager.Tracks[j].Elements[n].CurveCant = Data.Blocks[i].Rails[j].CurveCant;
					}
				}

				// world sounds
				for (int k = 0; k < Data.Blocks[i].SoundEvents.Count; k++)
				{
					if (Data.Blocks[i].SoundEvents[k].Type == SoundType.World)
					{
						var SoundEvent = Data.Blocks[i].SoundEvents[k];
						Sounds.SoundBuffer buffer;
						Data.Sound3Ds.TryGetValue(SoundEvent.Key, out buffer);
						double d = SoundEvent.TrackPosition - StartingDistance;
						double dx = SoundEvent.X;
						double dy = SoundEvent.Y;
						double wa = Math.Atan2(Direction.Y, Direction.X);
						Vector3 w = new Vector3(Math.Cos(wa), Math.Tan(0.0), Math.Sin(wa));
						w.Normalize();
						Vector3 s = new Vector3(Direction.Y, 0.0, -Direction.X);
						Vector3 u = Vector3.Cross(w, s);
						Vector3 wpos = Position + new Vector3(s.X * dx + u.X * dy + w.X * d, s.Y * dx + u.Y * dy + w.Y * d, s.Z * dx + u.Z * dy + w.Z * d);
						if (buffer != null)
						{
							Sounds.PlaySound(buffer, 1.0, 1.0, wpos, true);
						}
					}
				}

				// finalize block
				Position.X += Direction.X * c;
				Position.Y += h;
				Position.Z += Direction.Y * c;
				if (a != 0.0)
				{
					Direction.Rotate(Math.Cos(-a), Math.Sin(-a));
				}
			}

			// transponders
			if (!PreviewOnly)
			{
				for (int i = 0; i < Data.Blocks.Count; i++)
				{
					for (int k = 0; k < Data.Blocks[i].Transponders.Count; k++)
					{
						if (Data.Blocks[i].Transponders[k].Type != -1)
						{
							int n = i;
							int m = TrackManager.Tracks[0].Elements[n].Events.Length;
							Array.Resize(ref TrackManager.Tracks[0].Elements[n].Events, m + 1);
							double d = Data.Blocks[i].Transponders[k].TrackPosition - TrackManager.Tracks[0].Elements[n].StartingTrackPosition;
							int s = Data.Blocks[i].Transponders[k].SectionIndex;
							if (s < 0 || s >= Game.Sections.Length)
							{
								s = -1;
							}
							TrackManager.Tracks[0].Elements[n].Events[m] = new TrackManager.TransponderEvent(d, Data.Blocks[i].Transponders[k].Type, Data.Blocks[i].Transponders[k].Data, s, false);
							Data.Blocks[i].Transponders[k].Type = -1;
						}
					}
				}
			}

			// insert station end events
			for (int i = 0; i < Game.Stations.Length; i++)
			{
				int j = Game.Stations[i].Stops.Length - 1;
				if (j >= 0)
				{
					double p = Game.Stations[i].Stops[j].TrackPosition + Game.Stations[i].Stops[j].ForwardTolerance;
					int k = Data.Blocks.FindLastIndex(Block => Block.StartingDistance <= p);
					if (k != -1)
					{
						double d = p - Data.Blocks[k].StartingDistance;
						int m = TrackManager.Tracks[0].Elements[k].Events.Length;
						Array.Resize(ref TrackManager.Tracks[0].Elements[k].Events, m + 1);
						TrackManager.Tracks[0].Elements[k].Events[m] = new TrackManager.StationEndEvent(d, i);
					}
				}
			}

			// create default point of interests
			if (!PreviewOnly)
			{
				if (Game.PointsOfInterest.Length == 0)
				{
					Game.PointsOfInterest = new Game.PointOfInterest[Game.Stations.Length];
					int n = 0;
					for (int i = 0; i < Game.Stations.Length; i++)
					{
						if (Game.Stations[i].Stops.Length != 0)
						{
							Game.PointsOfInterest[n].Text = Game.Stations[i].Name;
							Game.PointsOfInterest[n].TrackPosition = Game.Stations[i].Stops[0].TrackPosition;
							Game.PointsOfInterest[n].TrackOffset = new Vector3(0.0, 2.8, 0.0);
							if (Game.Stations[i].OpenLeftDoors & !Game.Stations[i].OpenRightDoors)
							{
								Game.PointsOfInterest[n].TrackOffset.X = -2.5;
							}
							else if (!Game.Stations[i].OpenLeftDoors & Game.Stations[i].OpenRightDoors)
							{
								Game.PointsOfInterest[n].TrackOffset.X = 2.5;
							}
							n++;
						}
					}
					Array.Resize(ref Game.PointsOfInterest, n);
				}
			}

			// convert block-based cant into point-based cant
			if (!PreviewOnly)
			{
				for (int i = 0; i < TrackManager.Tracks.Length; i++)
				{
					for (int j = CurrentTrackLength - 1; j >= 1; j--)
					{
						if (TrackManager.Tracks[i].Elements[j].CurveCant == 0.0)
						{
							TrackManager.Tracks[i].Elements[j].CurveCant = TrackManager.Tracks[i].Elements[j - 1].CurveCant;
						}
						else if (TrackManager.Tracks[i].Elements[j - 1].CurveCant != 0.0)
						{
							if (Math.Sign(TrackManager.Tracks[i].Elements[j - 1].CurveCant) == Math.Sign(TrackManager.Tracks[i].Elements[j].CurveCant))
							{
								if (Math.Abs(TrackManager.Tracks[i].Elements[j - 1].CurveCant) > Math.Abs(TrackManager.Tracks[i].Elements[j].CurveCant))
								{
									TrackManager.Tracks[i].Elements[j].CurveCant = TrackManager.Tracks[i].Elements[j - 1].CurveCant;
								}
							}
							else
							{
								TrackManager.Tracks[i].Elements[j].CurveCant = 0.5 * (TrackManager.Tracks[i].Elements[j].CurveCant + TrackManager.Tracks[i].Elements[j - 1].CurveCant);
							}
						}
					}
				}
			}

			// finalize
			for (int j = 0; j < TrackManager.Tracks.Length; j++)
			{
				Array.Resize(ref TrackManager.Tracks[j].Elements, CurrentTrackLength);
			}
			for (int i = 0; i < Game.Stations.Length; i++)
			{
				if (Game.Stations[i].Stops.Length == 0 & Game.Stations[i].StopMode != StationStopMode.AllPass)
				{
					Interface.AddMessage(MessageType.Warning, false, "Station " + Game.Stations[i].Name + " expects trains to stop but does not define stop points at track position " + Game.Stations[i].DefaultTrackPosition.ToString(Culture) + " in file " + FileName);
					Game.Stations[i].StopMode = StationStopMode.AllPass;
				}
				if (Game.Stations[i].Type == StationType.ChangeEnds)
				{
					if (i < Game.Stations.Length - 1)
					{
						if (Game.Stations[i + 1].StopMode != StationStopMode.AllStop)
						{
							Interface.AddMessage(MessageType.Warning, false, "Station " + Game.Stations[i].Name + " is marked as \"change ends\" but the subsequent station does not expect all trains to stop in file " + FileName);
							Game.Stations[i + 1].StopMode = StationStopMode.AllStop;
						}
					}
					else
					{
						Interface.AddMessage(MessageType.Warning, false, "Station " + Game.Stations[i].Name + " is marked as \"change ends\" but there is no subsequent station defined in file " + FileName);
						Game.Stations[i].Type = StationType.Terminal;
					}
				}
			}
			if (Game.Stations.Any())
			{
				Game.Stations.Last().Type = StationType.Terminal;
			}
			if (TrackManager.Tracks[0].Elements.Any())
			{
				int n = TrackManager.Tracks[0].Elements.Length - 1;
				int m = TrackManager.Tracks[0].Elements[n].Events.Length;
				Array.Resize(ref TrackManager.Tracks[0].Elements[n].Events, m + 1);
				TrackManager.Tracks[0].Elements[n].Events[m] = new TrackManager.TrackEndEvent(InterpolateInterval);
			}

			// cant
			if (!PreviewOnly)
			{
				ComputeCantTangents();
			}
		}
	}
}
