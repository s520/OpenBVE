﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using OpenBveApi.Colors;
using OpenBveApi.Graphics;
using OpenBveApi.Math;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using SoundManager;
using TrainEditor2.Extensions;
using TrainEditor2.Models.Dialogs;
using TrainEditor2.Models.Others;
using TrainEditor2.Simulation.TrainManager;
using TrainEditor2.Systems;
using Vector2 = OpenBveApi.Math.Vector2;
using Vector3 = OpenBveApi.Math.Vector3;

namespace TrainEditor2.Models.Trains
{
	internal partial class Motor
	{
		internal partial class Track
		{
			private double XtoVelocity(double x)
			{
				double factorVelocity = GlControlWidth / (MaxVelocity - MinVelocity);
				return MinVelocity + x / factorVelocity;
			}

			private double YtoPitch(double y)
			{
				double factorPitch = -GlControlHeight / (MaxPitch - MinPitch);
				return MinPitch + (y - GlControlHeight) / factorPitch;
			}

			private double YtoVolume(double y)
			{
				double factorVolume = -GlControlHeight / (MaxVolume - MinVolume);
				return MinVolume + (y - GlControlHeight) / factorVolume;
			}

			private double VelocityToX(double v)
			{
				double factorVelocity = GlControlWidth / (MaxVelocity - MinVelocity);
				return (v - MinVelocity) * factorVelocity;
			}

			private double PitchToY(double p)
			{
				double factorPitch = -GlControlHeight / (MaxPitch - MinPitch);
				return GlControlHeight + (p - MinPitch) * factorPitch;
			}

			private double VolumeToY(double v)
			{
				double factorVolume = -GlControlHeight / (MaxVolume - MinVolume);
				return GlControlHeight + (v - MinVolume) * factorVolume;
			}

			internal void ZoomIn()
			{
				Utilities.ZoomIn(ref minVelocity, ref maxVelocity);

				OnPropertyChanged(new PropertyChangedEventArgs(nameof(MinVelocity)));
				OnPropertyChanged(new PropertyChangedEventArgs(nameof(MaxVelocity)));

				switch (CurrentInputMode)
				{
					case InputMode.Pitch:
						Utilities.ZoomIn(ref minPitch, ref maxPitch);

						OnPropertyChanged(new PropertyChangedEventArgs(nameof(MinPitch)));
						OnPropertyChanged(new PropertyChangedEventArgs(nameof(MaxPitch)));
						break;
					case InputMode.Volume:
						Utilities.ZoomIn(ref minVolume, ref maxVolume);

						OnPropertyChanged(new PropertyChangedEventArgs(nameof(MinVolume)));
						OnPropertyChanged(new PropertyChangedEventArgs(nameof(MaxVolume)));
						break;
				}
			}

			internal void ZoomOut()
			{
				Utilities.ZoomOut(ref minVelocity, ref maxVelocity);

				OnPropertyChanged(new PropertyChangedEventArgs(nameof(MinVelocity)));
				OnPropertyChanged(new PropertyChangedEventArgs(nameof(MaxVelocity)));

				switch (CurrentInputMode)
				{
					case InputMode.Pitch:
						Utilities.ZoomOut(ref minPitch, ref maxPitch);

						OnPropertyChanged(new PropertyChangedEventArgs(nameof(MinPitch)));
						OnPropertyChanged(new PropertyChangedEventArgs(nameof(MaxPitch)));
						break;
					case InputMode.Volume:
						Utilities.ZoomOut(ref minVolume, ref maxVolume);

						OnPropertyChanged(new PropertyChangedEventArgs(nameof(MinVolume)));
						OnPropertyChanged(new PropertyChangedEventArgs(nameof(MaxVolume)));
						break;
				}
			}

			internal void Reset()
			{
				Utilities.Reset(0.5 * 40.0, ref minVelocity, ref maxVelocity);

				OnPropertyChanged(new PropertyChangedEventArgs(nameof(MinVelocity)));
				OnPropertyChanged(new PropertyChangedEventArgs(nameof(MaxVelocity)));

				switch (CurrentInputMode)
				{
					case InputMode.Pitch:
						Utilities.Reset(0.5 * 400.0, ref minPitch, ref maxPitch);

						OnPropertyChanged(new PropertyChangedEventArgs(nameof(MinPitch)));
						OnPropertyChanged(new PropertyChangedEventArgs(nameof(MaxPitch)));
						break;
					case InputMode.Volume:
						Utilities.Reset(0.5 * 256.0, ref minVolume, ref maxVolume);

						OnPropertyChanged(new PropertyChangedEventArgs(nameof(MinVolume)));
						OnPropertyChanged(new PropertyChangedEventArgs(nameof(MaxVolume)));
						break;
				}
			}

			internal void MoveLeft()
			{
				Utilities.MoveNegative(ref minVelocity, ref maxVelocity);

				OnPropertyChanged(new PropertyChangedEventArgs(nameof(MinVelocity)));
				OnPropertyChanged(new PropertyChangedEventArgs(nameof(MaxVelocity)));
			}

			internal void MoveRight()
			{
				Utilities.MovePositive(ref minVelocity, ref maxVelocity);

				OnPropertyChanged(new PropertyChangedEventArgs(nameof(MinVelocity)));
				OnPropertyChanged(new PropertyChangedEventArgs(nameof(MaxVelocity)));
			}

			internal void MoveBottom()
			{
				switch (CurrentInputMode)
				{
					case InputMode.Pitch:
						Utilities.MoveNegative(ref minPitch, ref maxPitch);

						OnPropertyChanged(new PropertyChangedEventArgs(nameof(MinPitch)));
						OnPropertyChanged(new PropertyChangedEventArgs(nameof(MaxPitch)));
						break;
					case InputMode.Volume:
						Utilities.MoveNegative(ref minVolume, ref maxVolume);

						OnPropertyChanged(new PropertyChangedEventArgs(nameof(MinVolume)));
						OnPropertyChanged(new PropertyChangedEventArgs(nameof(MaxVolume)));
						break;
				}
			}

			internal void MoveTop()
			{
				switch (CurrentInputMode)
				{
					case InputMode.Pitch:
						Utilities.MovePositive(ref minPitch, ref maxPitch);

						OnPropertyChanged(new PropertyChangedEventArgs(nameof(MinPitch)));
						OnPropertyChanged(new PropertyChangedEventArgs(nameof(MaxPitch)));
						break;
					case InputMode.Volume:
						Utilities.MovePositive(ref minVolume, ref maxVolume);

						OnPropertyChanged(new PropertyChangedEventArgs(nameof(MinVolume)));
						OnPropertyChanged(new PropertyChangedEventArgs(nameof(MaxVolume)));
						break;
				}
			}

			internal void Undo()
			{
				TrackState prev = PrevStates.Last();
				NextStates.Add(new TrackState(this));
				prev.Apply(this);
				PrevStates.Remove(prev);

				IsRefreshGlControl = true;
			}

			internal void Redo()
			{
				TrackState next = NextStates.Last();
				PrevStates.Add(new TrackState(this));
				next.Apply(this);
				NextStates.Remove(next);

				IsRefreshGlControl = true;
			}

			internal void Cleanup()
			{
				Func<int, ObservableCollection<Line>, bool> condition = (i, ls) => ls.Any(l => l.LeftID == i || l.RightID == i);

				int[] pitchTargetIDs = new int[0];
				int[] volumeTargetIDs = new int[0];

				if (CurrentInputMode != InputMode.Volume)
				{
					pitchTargetIDs = PitchVertices.Keys.Where(i => !condition(i, PitchLines)).ToArray();
				}

				if (CurrentInputMode != InputMode.Pitch)
				{
					volumeTargetIDs = VolumeVertices.Keys.Where(i => !condition(i, VolumeLines)).ToArray();
				}

				if (!pitchTargetIDs.Any() && !volumeTargetIDs.Any())
				{
					return;
				}

				PrevStates.Add(new TrackState(this));
				NextStates.Clear();

				foreach (int targetID in pitchTargetIDs)
				{
					PitchVertices.Remove(targetID);
				}

				foreach (int targetID in volumeTargetIDs)
				{
					VolumeVertices.Remove(targetID);
				}

				IsRefreshGlControl = true;
			}

			private static void DeleteDotLine(VertexLibrary vertices, ObservableCollection<Line> lines)
			{
				if (vertices.Any(v => v.Value.Selected) || lines.Any(l => l.Selected))
				{
					KeyValuePair<int, Vertex>[] selectVertices = vertices.Where(v => v.Value.Selected).ToArray();

					foreach (KeyValuePair<int, Vertex> vertex in selectVertices)
					{
						lines.RemoveAll(l => l.LeftID == vertex.Key || l.RightID == vertex.Key);
						vertices.Remove(vertex.Key);
					}

					lines.RemoveAll(l => l.Selected);
				}
				else
				{
					vertices.Clear();
					lines.Clear();
				}
			}

			internal void Delete()
			{
				PrevStates.Add(new TrackState(this));
				NextStates.Clear();

				switch (CurrentInputMode)
				{
					case InputMode.Pitch:
						DeleteDotLine(PitchVertices, PitchLines);
						break;
					case InputMode.Volume:
						DeleteDotLine(VolumeVertices, VolumeLines);
						break;
					case InputMode.SoundIndex:
						SoundIndices.Clear();
						break;
				}

				IsRefreshGlControl = true;
			}

			internal void DirectDot(double x, double y)
			{
				x = 0.01 * Math.Round(100.0 * x);
				y = 0.01 * Math.Round(100.0 * y);

				bool exist = false;

				switch (CurrentInputMode)
				{
					case InputMode.Pitch:
						exist = PitchVertices.Any(v => v.Value.X == x);
						break;
					case InputMode.Volume:
						exist = VolumeVertices.Any(v => v.Value.X == x);
						break;
				}

				if (exist)
				{
					MessageBox = new MessageBox
					{
						Title = @"Dot",
						Icon = BaseDialog.DialogIcon.Question,
						Button = BaseDialog.DialogButton.YesNo,
						Text = @"A point already exists at the same x coordinate, do you want to overwrite it?",
						IsOpen = true
					};

					if (MessageBox.DialogResult != true)
					{
						return;
					}
				}

				switch (CurrentInputMode)
				{
					case InputMode.Pitch:
						DrawDot(PitchVertices, x, y);
						break;
					case InputMode.Volume:
						DrawDot(VolumeVertices, x, y);
						break;
				}
			}

			internal void MouseDown(InputEventModel.EventArgs e)
			{
				if (e.LeftButton == InputEventModel.ButtonState.Pressed)
				{
					lastMousePosX = e.X;
					lastMousePosY = e.Y;

					if (BaseMotor.CurrentSimState == SimulationState.Disable || BaseMotor.CurrentSimState == SimulationState.Stopped)
					{
						if (CurrentInputMode != InputMode.SoundIndex)
						{
							switch (CurrentToolMode)
							{
								case ToolMode.Select:
									SelectDotLine(e);
									break;
								case ToolMode.Dot:
									DrawDot(e);
									break;
								case ToolMode.Line:
									DrawLine(e);
									break;
							}
						}
					}
				}
			}

			internal void MouseMove(InputEventModel.EventArgs e)
			{
				NowVelocity = 0.01 * Math.Round(100.0 * XtoVelocity(e.X));
				NowPitch = 0.01 * Math.Round(100.0 * YtoPitch(e.Y));
				NowVolume = 0.01 * Math.Round(100.0 * YtoVolume(e.Y));

				if (CurrentInputMode != InputMode.Volume)
				{
					ShowToolTipVertex(InputMode.Pitch, PitchVertices, ref hoveredVertexPitch, toolTipVertexPitch, NowVelocity, NowPitch);
				}

				if (CurrentInputMode != InputMode.Pitch)
				{
					ShowToolTipVertex(InputMode.Pitch, VolumeVertices, ref hoveredVertexVolume, toolTipVertexVolume, NowVelocity, NowVolume);
				}

				if (BaseMotor.CurrentSimState == SimulationState.Disable || BaseMotor.CurrentSimState == SimulationState.Stopped)
				{
					if (CurrentInputMode != InputMode.SoundIndex)
					{
						switch (CurrentToolMode)
						{
							case ToolMode.Select:
							case ToolMode.Line:
								switch (CurrentInputMode)
								{
									case InputMode.Pitch:
										ChangeCursor(PitchVertices, PitchLines, NowVelocity, NowPitch);
										break;
									case InputMode.Volume:
										ChangeCursor(VolumeVertices, VolumeLines, NowVelocity, NowVolume);
										break;
								}
								break;
							case ToolMode.Move:
								CurrentCursorType = InputEventModel.CursorType.ScrollAll;
								break;
							case ToolMode.Dot:
								CurrentCursorType = InputEventModel.CursorType.Cross;
								break;
						}
					}
					else
					{
						CurrentCursorType = InputEventModel.CursorType.Cross;
					}
				}
				else
				{
					CurrentCursorType = InputEventModel.CursorType.Arrow;
				}

				if (e.LeftButton == InputEventModel.ButtonState.Pressed)
				{
					if (BaseMotor.CurrentSimState == SimulationState.Disable || BaseMotor.CurrentSimState == SimulationState.Stopped)
					{
						double deltaX = e.X - lastMousePosX;
						double deltaY = e.Y - lastMousePosY;

						double factorVelocity = GlControlWidth / (maxVelocity - minVelocity);
						double factorPitch = -GlControlHeight / (maxPitch - minPitch);
						double factorVolume = -GlControlHeight / (maxVolume - minVolume);

						double deltaVelocity = 0.01 * Math.Round(100.0 * deltaX / factorVelocity);
						double deltaPitch = 0.01 * Math.Round(100.0 * deltaY / factorPitch);
						double deltaVolume = 0.01 * Math.Round(100.0 * deltaY / factorVolume);

						switch (CurrentInputMode)
						{
							case InputMode.Pitch:
								MouseDrag(PitchVertices, PitchLines, NowVelocity, NowPitch, deltaVelocity, deltaPitch);
								break;
							case InputMode.Volume:
								MouseDrag(VolumeVertices, VolumeLines, NowVelocity, NowVolume, deltaVelocity, deltaVolume);
								break;
							case InputMode.SoundIndex:
								if (deltaVelocity != 0.0)
								{
									previewArea = new Area(Math.Min(NowVelocity - deltaVelocity, NowVelocity), Math.Max(NowVelocity - deltaVelocity, NowVelocity), SelectedSoundIndex);
								}
								else
								{
									previewArea = null;
								}
								break;
						}

						if (CurrentInputMode != InputMode.SoundIndex && CurrentToolMode != ToolMode.Select)
						{
							lastMousePosX = e.X;
							lastMousePosY = e.Y;
						}

						IsRefreshGlControl = true;
					}
				}
			}

			private void ShowToolTipVertex(InputMode inputMode, VertexLibrary vertices, ref Vertex hoveredVertex, ToolTipModel toolTipVertex, double x, double y)
			{
				Func<Vertex, bool> conditionVertex = v => v.X - 0.01 < x && x < v.X + 0.01 && v.Y - 2.0 < y && y < v.Y + 2.0;

				Vertex newHoveredVertex = vertices.Values.FirstOrDefault(v => conditionVertex(v));

				if (newHoveredVertex != hoveredVertex)
				{
					if (newHoveredVertex != null)
					{
						Area area = SoundIndices.FirstOrDefault(a => a.LeftX <= newHoveredVertex.X && a.RightX >= newHoveredVertex.X);

						StringBuilder builder = new StringBuilder();
						builder.AppendLine($"{Utilities.GetInterfaceString("motor_sound_settings", "vertex_info", "velocity")}: {newHoveredVertex.X.ToString("0.00", Culture)} km/h");

						switch (inputMode)
						{
							case InputMode.Pitch:
								builder.AppendLine($"{Utilities.GetInterfaceString("motor_sound_settings", "vertex_info", "pitch")}: {newHoveredVertex.Y.ToString("0.00", Culture)}");
								break;
							case InputMode.Volume:
								builder.AppendLine($"{Utilities.GetInterfaceString("motor_sound_settings", "vertex_info", "volume")}: {newHoveredVertex.Y.ToString("0.00", Culture)}");
								break;
						}

						builder.AppendLine($"{Utilities.GetInterfaceString("motor_sound_settings", "vertex_info", "sound_index")}: {area?.Index ?? -1}");

						toolTipVertex.Title = Utilities.GetInterfaceString("motor_sound_settings", "vertex_info", "name");
						toolTipVertex.Icon = ToolTipModel.ToolTipIcon.Information;
						toolTipVertex.Text = builder.ToString();
						toolTipVertex.X = VelocityToX(newHoveredVertex.X) + 10.0;

						switch (inputMode)
						{
							case InputMode.Pitch:
								toolTipVertex.Y = PitchToY(newHoveredVertex.Y) + 10.0;
								break;
							case InputMode.Volume:
								toolTipVertex.Y = VolumeToY(newHoveredVertex.Y) + 10.0;
								break;
						}

						toolTipVertex.IsOpen = true;
					}
					else
					{
						toolTipVertex.IsOpen = false;
					}

					hoveredVertex = newHoveredVertex;
				}
			}

			private void ChangeCursor(VertexLibrary vertices, ObservableCollection<Line> lines, double x, double y)
			{
				if (IsSelectDotLine(vertices, lines, x, y))
				{
					if (CurrentToolMode == ToolMode.Select || IsDrawLine(vertices, lines, x, y))
					{
						CurrentCursorType = InputEventModel.CursorType.Hand;
					}
					else
					{
						CurrentCursorType = InputEventModel.CursorType.No;
					}
				}
				else
				{
					CurrentCursorType = InputEventModel.CursorType.Arrow;
				}
			}

			private void MouseDrag(VertexLibrary vertices, ObservableCollection<Line> lines, double x, double y, double deltaX, double deltaY)
			{
				switch (CurrentToolMode)
				{
					case ToolMode.Select:
						{
							double leftX = Math.Min(x - deltaX, x);
							double rightX = Math.Max(x - deltaX, x);

							double topY = Math.Max(y - deltaY, y);
							double bottomY = Math.Min(y - deltaY, y);

							if (deltaX != 0.0 && deltaY != 0.0)
							{
								selectedRange = SelectedRange.CreateSelectedRange(vertices, lines, leftX, rightX, topY, bottomY);
							}
							else
							{
								selectedRange = null;
							}
						}
						break;
					case ToolMode.Move:
						MoveDot(vertices, deltaX, deltaY);
						break;
				}
			}

			internal void MouseUp()
			{
				if (CurrentInputMode != InputMode.SoundIndex)
				{
					isMoving = false;

					if (CurrentToolMode == ToolMode.Select)
					{
						if (selectedRange != null)
						{
							foreach (Vertex vertex in selectedRange.SelectedVertices)
							{
								vertex.Selected = !vertex.Selected;
							}

							foreach (Line line in selectedRange.SelectedLines)
							{
								line.Selected = !line.Selected;
							}

							selectedRange = null;
						}
					}
				}
				else
				{
					if (previewArea != null)
					{
						PrevStates.Add(new TrackState(this));

						List<Area> addAreas = new List<Area>();

						foreach (Area area in SoundIndices)
						{
							if (area.RightX < previewArea.LeftX || area.LeftX > previewArea.RightX)
							{
								continue;
							}

							if (area.LeftX < previewArea.LeftX && area.RightX > previewArea.RightX)
							{
								if (area.Index != previewArea.Index)
								{
									addAreas.Add(new Area(area.LeftX, previewArea.LeftX - 0.01, area.Index));
									addAreas.Add(new Area(previewArea.RightX + 0.01, area.RightX, area.Index));
									area.TBD = true;
								}
								else
								{
									previewArea.TBD = true;
								}

								break;
							}

							if (area.LeftX < previewArea.LeftX)
							{
								if (area.Index != previewArea.Index)
								{
									area.RightX = previewArea.LeftX - 0.01;
								}
								else
								{
									previewArea.LeftX = area.LeftX;
									area.TBD = true;
								}
							}
							else if (area.RightX > previewArea.RightX)
							{
								if (area.Index != previewArea.Index)
								{
									area.LeftX = previewArea.RightX + 0.01;
								}
								else
								{
									previewArea.RightX = area.RightX;
									area.TBD = true;
								}
							}
							else
							{
								area.TBD = true;
							}
						}

						SoundIndices.Add(previewArea);
						SoundIndices.AddRange(addAreas);
						SoundIndices.RemoveAll(a => a.TBD);
						SoundIndices = new ObservableCollection<Area>(SoundIndices.OrderBy(a => a.LeftX));

						if (previewArea.TBD)
						{
							PrevStates.Remove(PrevStates.Last());
						}
						else
						{
							NextStates.Clear();
						}

						previewArea = null;
					}
				}

				IsRefreshGlControl = true;
			}

			internal void DirectMove(double x, double y)
			{
				x = 0.01 * Math.Round(100.0 * x);
				y = 0.01 * Math.Round(100.0 * y);

				switch (CurrentInputMode)
				{
					case InputMode.Pitch:
						MoveDot(PitchVertices, x, y);
						break;
					case InputMode.Volume:
						MoveDot(VolumeVertices, x, y);
						break;
				}
			}

			internal void ResetSelect()
			{
				if (CurrentInputMode != InputMode.Volume)
				{
					ResetSelect(PitchVertices, PitchLines);
				}

				if (CurrentInputMode != InputMode.Pitch)
				{
					ResetSelect(VolumeVertices, VolumeLines);
				}
			}

			private void ResetSelect(VertexLibrary vertices, ObservableCollection<Line> lines)
			{
				foreach (Vertex vertex in vertices.Values)
				{
					if (CurrentInputMode == InputMode.SoundIndex || CurrentToolMode != ToolMode.Select && CurrentToolMode != ToolMode.Move)
					{
						vertex.Selected = false;
					}

					if (CurrentInputMode == InputMode.SoundIndex || CurrentToolMode != ToolMode.Line)
					{
						vertex.IsOrigin = false;
					}
				}

				foreach (Line line in lines)
				{
					if (CurrentInputMode == InputMode.SoundIndex || CurrentToolMode != ToolMode.Select && CurrentToolMode != ToolMode.Move)
					{
						line.Selected = false;
					}
				}
			}

			private bool IsSelectDotLine(VertexLibrary vertices, ObservableCollection<Line> lines, double x, double y)
			{
				if (vertices.Any(v => v.Value.X - 0.01 < x && x < v.Value.X + 0.01 && v.Value.Y - 2.0 < y && y < v.Value.Y + 2.0))
				{
					return true;
				}

				if (lines.Any(l => vertices[l.LeftID].X + 0.01 < x && x < vertices[l.RightID].X - 0.01 && Math.Min(vertices[l.LeftID].Y, vertices[l.RightID].Y) - 2.0 < y && y < Math.Max(vertices[l.LeftID].Y, vertices[l.RightID].Y) + 2.0))
				{
					return true;
				}

				return false;
			}

			private void SelectDotLine(InputEventModel.EventArgs e)
			{
				double velocity = XtoVelocity(e.X);
				double pitch = YtoPitch(e.Y);
				double volume = YtoVolume(e.Y);

				switch (CurrentInputMode)
				{
					case InputMode.Pitch:
						SelectDotLine(PitchVertices, PitchLines, velocity, pitch);
						break;
					case InputMode.Volume:
						SelectDotLine(VolumeVertices, VolumeLines, velocity, volume);
						break;
				}
			}

			private void SelectDotLine(VertexLibrary vertices, ObservableCollection<Line> lines, double x, double y)
			{
				Func<Vertex, bool> conditionVertex = v => v.X - 0.01 < x && x < v.X + 0.01 && v.Y - 2.0 < y && y < v.Y + 2.0;

				if (vertices.Any(v => conditionVertex(v.Value)))
				{
					KeyValuePair<int, Vertex> selectVertex = vertices.First(v => conditionVertex(v.Value));

					if (!CurrentModifierKeys.HasFlag(InputEventModel.ModifierKeys.Control))
					{
						foreach (Vertex vertex in vertices.Values.Where(v => v != selectVertex.Value))
						{
							vertex.Selected = false;
						}
					}

					selectVertex.Value.Selected = !selectVertex.Value.Selected;
				}
				else
				{
					if (!CurrentModifierKeys.HasFlag(InputEventModel.ModifierKeys.Control))
					{
						foreach (Vertex vertex in vertices.Values)
						{
							vertex.Selected = false;
						}
					}
				}

				Line selectLine = lines.FirstOrDefault(l => vertices[l.LeftID].X + 0.01 < x && x < vertices[l.RightID].X - 0.01 && Math.Min(vertices[l.LeftID].Y, vertices[l.RightID].Y) - 2.0 < y && y < Math.Max(vertices[l.LeftID].Y, vertices[l.RightID].Y) + 2.0);

				if (selectLine != null)
				{
					if (!CurrentModifierKeys.HasFlag(InputEventModel.ModifierKeys.Control))
					{
						foreach (Line line in lines.Where(l => l != selectLine))
						{
							line.Selected = false;
						}
					}

					selectLine.Selected = !selectLine.Selected;
				}
				else
				{
					foreach (Line line in lines)
					{
						line.Selected = false;
					}
				}

				IsRefreshGlControl = true;
			}

			private void MoveDot(VertexLibrary vertices, double deltaX, double deltaY)
			{
				if (vertices.Values.Any(v => v.Selected))
				{
					if (!isMoving)
					{
						PrevStates.Add(new TrackState(this));
						NextStates.Clear();
						isMoving = true;
					}

					foreach (Vertex select in vertices.Values.Where(v => v.Selected).OrderBy(v => v.X))
					{
						if (deltaX >= 0.0)
						{
							Vertex unselectLeft = vertices.Values.OrderBy(v => v.X).FirstOrDefault(v => v.X > select.X);

							if (unselectLeft != null)
							{
								if (select.X + deltaX + 0.01 >= unselectLeft.X)
								{
									deltaX = 0.0;
								}
							}
						}
						else
						{
							Vertex unselectRight = vertices.Values.OrderBy(v => v.X).LastOrDefault(v => v.X < select.X);

							if (unselectRight != null)
							{
								if (select.X + deltaX - 0.01 <= unselectRight.X)
								{
									deltaX = 0.0;
								}
							}

							if (select.X + deltaX < 0.0)
							{
								deltaX = 0.0;
							}
						}

						if (deltaY < 0.0)
						{
							if (select.Y + deltaY < 0.0)
							{
								deltaY = 0.0;
							}
						}
					}

					foreach (Vertex vertex in vertices.Values.Where(v => v.Selected))
					{
						vertex.X += deltaX;
						vertex.Y += deltaY;
					}

					IsRefreshGlControl = true;
				}
			}

			private void DrawDot(InputEventModel.EventArgs e)
			{
				double velocity = 0.01 * Math.Round(100.0 * XtoVelocity(e.X));
				double pitch = 0.01 * Math.Round(100.0 * YtoPitch(e.Y));
				double volume = 0.01 * Math.Round(100.0 * YtoVolume(e.Y));

				switch (CurrentInputMode)
				{
					case InputMode.Pitch:
						DrawDot(PitchVertices, velocity, pitch);
						break;
					case InputMode.Volume:
						DrawDot(VolumeVertices, velocity, volume);
						break;
				}
			}

			private void DrawDot(VertexLibrary vertices, double x, double y)
			{
				PrevStates.Add(new TrackState(this));
				NextStates.Clear();
				vertices.Add(new Vertex(x, y));

				IsRefreshGlControl = true;
			}

			private bool IsDrawLine(VertexLibrary vertices, ObservableCollection<Line> lines, double x, double y)
			{
				Func<Vertex, bool> conditionVertex = v => v.X - 0.01 < x && x < v.X + 0.01 && v.Y - 2.0 < y && y < v.Y + 2.0;

				if (vertices.Any(v => conditionVertex(v.Value)))
				{
					KeyValuePair<int, Vertex> selectVertex = vertices.First(v => conditionVertex(v.Value));

					if (selectVertex.Value.IsOrigin)
					{
						return true;
					}

					if (vertices.Any(v => v.Value.IsOrigin))
					{
						KeyValuePair<int, Vertex> origin = vertices.First(v => v.Value.IsOrigin);
						KeyValuePair<int, Vertex>[] selectVertices = new[] { origin, selectVertex }.OrderBy(v => v.Value.X).ToArray();

						Func<Line, bool> conditionLineLeft = l => vertices[l.LeftID].X <= selectVertices[0].Value.X && selectVertices[0].Value.X < vertices[l.RightID].X;
						Func<Line, bool> conditionLineRight = l => vertices[l.LeftID].X < selectVertices[1].Value.X && selectVertices[1].Value.X <= vertices[l.RightID].X;

						if (!lines.Any(l => conditionLineLeft(l)) && !lines.Any(l => conditionLineRight(l)))
						{
							return true;
						}

						return false;
					}

					return true;
				}

				return false;
			}

			private void DrawLine(InputEventModel.EventArgs e)
			{
				double velocity = XtoVelocity(e.X);
				double pitch = YtoPitch(e.Y);
				double volume = YtoVolume(e.Y);

				switch (CurrentInputMode)
				{
					case InputMode.Pitch:
						DrawLine(PitchVertices, PitchLines, velocity, pitch);
						break;
					case InputMode.Volume:
						DrawLine(VolumeVertices, VolumeLines, velocity, volume);
						break;
				}
			}

			private void DrawLine(VertexLibrary vertices, ObservableCollection<Line> lines, double x, double y)
			{
				Func<Vertex, bool> conditionVertex = v => v.X - 0.01 < x && x < v.X + 0.01 && v.Y - 2.0 < y && y < v.Y + 2.0;

				if (vertices.Any(v => conditionVertex(v.Value)))
				{
					KeyValuePair<int, Vertex> selectVertex = vertices.First(v => conditionVertex(v.Value));

					if (selectVertex.Value.IsOrigin)
					{
						selectVertex.Value.IsOrigin = false;
					}
					else if (vertices.Any(v => v.Value.IsOrigin))
					{
						KeyValuePair<int, Vertex> origin = vertices.First(v => v.Value.IsOrigin);
						KeyValuePair<int, Vertex>[] selectVertices = new[] { origin, selectVertex }.OrderBy(v => v.Value.X).ToArray();

						Func<Line, bool> conditionLineLeft = l => vertices[l.LeftID].X <= selectVertices[0].Value.X && selectVertices[0].Value.X < vertices[l.RightID].X;
						Func<Line, bool> conditionLineRight = l => vertices[l.LeftID].X < selectVertices[1].Value.X && selectVertices[1].Value.X <= vertices[l.RightID].X;

						if (!lines.Any(l => conditionLineLeft(l)) && !lines.Any(l => conditionLineRight(l)))
						{
							PrevStates.Add(new TrackState(this));
							NextStates.Clear();
							lines.Add(new Line(selectVertices[0].Key, selectVertices[1].Key));

							origin.Value.IsOrigin = false;
							selectVertex.Value.IsOrigin = true;
						}
					}
					else
					{
						selectVertex.Value.IsOrigin = true;
					}

					IsRefreshGlControl = true;
				}
			}

			private static TrainManager.MotorSound.Vertex<float>[] LineToMotorSoundVertices(VertexLibrary library, IEnumerable<Line> lines, Func<double, double> xConverter, Func<double, double> yConverter, double _default)
			{
				List<TrainManager.MotorSound.Vertex<float>> vertices = new List<TrainManager.MotorSound.Vertex<float>>();
				lines = lines.OrderBy(x => library[x.LeftID].X).ToArray();

				for (int i = 0; i < lines.Count(); i++)
				{
					Vertex left = library[lines.ElementAt(i).LeftID];
					Vertex right = library[lines.ElementAt(i).RightID];

					if (i > 1)
					{
						Vertex prevRight = library[lines.ElementAt(i - 1).RightID];

						if (prevRight != left)
						{
							if (left.X - 0.01 > prevRight.X)
							{
								vertices.Add(new TrainManager.MotorSound.Vertex<float> { X = (float)xConverter(left.X - 0.001), Y = (float)yConverter(_default) });
							}
						}
					}

					TrainManager.MotorSound.Vertex<float> existLeft = vertices.FirstOrDefault(v => v.X == (float)xConverter(left.X));

					if (existLeft != null)
					{
						existLeft.Y = (float)yConverter(left.Y);
					}
					else
					{
						vertices.Add(new TrainManager.MotorSound.Vertex<float> { X = (float)xConverter(left.X), Y = (float)yConverter(left.Y) });
					}

					TrainManager.MotorSound.Vertex<float> existRight = vertices.FirstOrDefault(v => v.X == (float)xConverter(right.X));

					if (existRight != null)
					{
						existRight.Y = (float)yConverter(right.Y);
					}
					else
					{
						vertices.Add(new TrainManager.MotorSound.Vertex<float> { X = (float)xConverter(right.X), Y = (float)yConverter(right.Y) });
					}

					if (i < lines.Count() - 1)
					{
						Vertex nextLeft = library[lines.ElementAt(i + 1).LeftID];

						if (nextLeft != right)
						{
							if (right.X + 0.01 < nextLeft.X)
							{
								vertices.Add(new TrainManager.MotorSound.Vertex<float> { X = (float)xConverter(right.X + 0.001), Y = (float)yConverter(_default) });
							}
						}
					}
				}

				return vertices.ToArray();
			}

			private static TrainManager.MotorSound.Vertex<int, SoundBuffer>[] IndexToMotorSoundVertices(IEnumerable<Area> areas, Func<double, double> xConverter, int _default)
			{
				List<TrainManager.MotorSound.Vertex<int, SoundBuffer>> vertices = new List<TrainManager.MotorSound.Vertex<int, SoundBuffer>>();
				areas = areas.OrderBy(x => x.LeftX).ToArray();

				for (int i = 0; i < areas.Count(); i++)
				{
					Area area = areas.ElementAt(i);

					if (i > 1)
					{
						if (area.LeftX - 0.01 > areas.ElementAt(i - 1).RightX)
						{
							vertices.Add(new TrainManager.MotorSound.Vertex<int, SoundBuffer> { X = (float)xConverter(area.LeftX - 0.001), Y = _default });
						}
					}

					TrainManager.MotorSound.Vertex<int, SoundBuffer> existLeft = vertices.FirstOrDefault(v => v.X == (float)xConverter(area.LeftX));

					if (existLeft != null)
					{
						existLeft.Y = area.Index;
					}
					else
					{
						vertices.Add(new TrainManager.MotorSound.Vertex<int, SoundBuffer> { X = (float)xConverter(area.LeftX), Y = area.Index });
					}

					TrainManager.MotorSound.Vertex<int, SoundBuffer> existRight = vertices.FirstOrDefault(v => v.X == (float)xConverter(area.RightX));

					if (existRight != null)
					{
						existRight.Y = area.Index;
					}
					else
					{
						vertices.Add(new TrainManager.MotorSound.Vertex<int, SoundBuffer> { X = (float)xConverter(area.RightX), Y = area.Index });
					}

					if (i < areas.Count() - 1)
					{
						if (area.RightX + 0.01 < areas.ElementAt(i + 1).LeftX)
						{
							vertices.Add(new TrainManager.MotorSound.Vertex<int, SoundBuffer> { X = (float)xConverter(area.RightX + 0.001), Y = _default });
						}
					}
				}

				return vertices.ToArray();
			}

			internal static TrainManager.MotorSound.Table TrackToMotorSoundTable(Track track, Func<double, double> speedConverter, Func<double, double> pitchConverter, Func<double, double> volumeConverter)
			{
				return new TrainManager.MotorSound.Table
				{
					PitchVertices = LineToMotorSoundVertices(track.PitchVertices, track.PitchLines, speedConverter, pitchConverter, 100.0),
					GainVertices = LineToMotorSoundVertices(track.VolumeVertices, track.VolumeLines, speedConverter, volumeConverter, 128),
					BufferVertices = IndexToMotorSoundVertices(track.SoundIndices, speedConverter, -1)
				};
			}

			internal static Track MotorSoundTableToTrack(Motor baseMotor, TrackType type, TrainManager.MotorSound.Table table, Func<double, double> speedConverter, Func<double, double> pitchConverter, Func<double, double> volumeConverter)
			{
				Track track = new Track(baseMotor) { Type = type };

				foreach (TrainManager.MotorSound.Vertex<float> vertex in table.PitchVertices)
				{
					double nextX = 0.01 * Math.Round(100.0 * speedConverter(vertex.X));
					double nextY = 0.01 * Math.Round(100.0 * pitchConverter(vertex.Y));

					if (track.PitchVertices.Count >= 2)
					{
						KeyValuePair<int, Vertex>[] leftVertices = { track.PitchVertices.ElementAt(track.PitchVertices.Count - 2), track.PitchVertices.Last() };
						Func<double, double> f = x => leftVertices[0].Value.Y + (leftVertices[1].Value.Y - leftVertices[0].Value.Y) / (leftVertices[1].Value.X - leftVertices[0].Value.X) * (x - leftVertices[0].Value.X);

						if ((float)f(nextX) == nextY)
						{
							track.PitchVertices.Remove(leftVertices[1].Key);
						}
					}

					track.PitchVertices.Add(new Vertex(nextX, nextY));
				}

				foreach (TrainManager.MotorSound.Vertex<float> vertex in table.GainVertices)
				{
					double nextX = 0.01 * Math.Round(100.0 * speedConverter(vertex.X));
					double nextY = 0.01 * Math.Round(100.0 * volumeConverter(vertex.Y));

					if (track.VolumeVertices.Count >= 2)
					{
						KeyValuePair<int, Vertex>[] leftVertices = { track.VolumeVertices.ElementAt(track.VolumeVertices.Count - 2), track.VolumeVertices.Last() };
						Func<double, double> f = x => leftVertices[0].Value.Y + (leftVertices[1].Value.Y - leftVertices[0].Value.Y) / (leftVertices[1].Value.X - leftVertices[0].Value.X) * (x - leftVertices[0].Value.X);

						if ((float)f(nextX) == nextY)
						{
							track.VolumeVertices.Remove(leftVertices[1].Key);
						}

						track.VolumeVertices.Add(new Vertex(nextX, nextY));
					}
				}

				foreach (TrainManager.MotorSound.Vertex<int, SoundBuffer> vertex in table.BufferVertices)
				{
					double nextX = 0.01 * Math.Round(100.0 * speedConverter(vertex.X));

					if (track.SoundIndices.Any())
					{
						Area leftArea = track.SoundIndices.Last();

						if (vertex.Y != leftArea.Index)
						{
							leftArea.RightX = nextX - 0.01;
							track.SoundIndices.Add(new Area(nextX, nextX, vertex.Y));
						}
						else
						{
							leftArea.RightX = nextX;
						}
					}
					else
					{
						track.SoundIndices.Add(new Area(nextX, nextX, vertex.Y));
					}
				}

				for (int j = 0; j < track.PitchVertices.Count - 1; j++)
				{
					track.PitchLines.Add(new Line(track.PitchVertices.ElementAt(j).Key, track.PitchVertices.ElementAt(j + 1).Key));
				}

				for (int j = 0; j < track.VolumeVertices.Count - 1; j++)
				{
					track.VolumeLines.Add(new Line(track.VolumeVertices.ElementAt(j).Key, track.VolumeVertices.ElementAt(j + 1).Key));
				}

				if (track.SoundIndices.Any())
				{
					Area lastArea = track.SoundIndices.Last();

					if (lastArea.LeftX == lastArea.RightX)
					{
						lastArea.RightX += 0.01;
					}
				}

				return track;
			}

			internal void DrawSimulation(double startSpeed, double endSpeed)
			{
				double rangeVelocity = MaxVelocity - MinVelocity;

				if (startSpeed <= endSpeed)
				{
					if (CurrentSimSpeed < MinVelocity || CurrentSimSpeed > MaxVelocity)
					{
						minVelocity = 10.0 * Math.Round(0.1 * CurrentSimSpeed);

						if (MinVelocity < 0.0)
						{
							minVelocity = 0.0;
						}

						maxVelocity = MinVelocity + rangeVelocity;

						OnPropertyChanged(new PropertyChangedEventArgs(nameof(MinVelocity)));
						OnPropertyChanged(new PropertyChangedEventArgs(nameof(MaxVelocity)));

						return;
					}
				}
				else
				{
					if (CurrentSimSpeed < MinVelocity || CurrentSimSpeed > MaxVelocity)
					{
						maxVelocity = 10.0 * Math.Round(0.1 * CurrentSimSpeed);

						if (MaxVelocity < rangeVelocity)
						{
							maxVelocity = rangeVelocity;
						}

						minVelocity = MaxVelocity - rangeVelocity;

						OnPropertyChanged(new PropertyChangedEventArgs(nameof(MinVelocity)));
						OnPropertyChanged(new PropertyChangedEventArgs(nameof(MaxVelocity)));

						return;
					}
				}

				IsRefreshGlControl = true;
			}

			private void DrawPolyLine(Matrix4D proj, Matrix4D look, Vector2 p1, Vector2 p2, double lineWidth, Color color)
			{
				Matrix4D inv = Matrix4D.Invert(look) * Matrix4D.Invert(proj);
				Vector2 line = new Vector2((inv.Row0.X + inv.Row0.Y) * lineWidth / 2.0, (inv.Row1.X + inv.Row1.Y) * lineWidth / 2.0) / 100.0;

				double rad = Math.Atan2(p2.Y - p1.Y, p2.X - p1.X);

				double p1x1 = p1.X + Math.Cos(rad + Math.PI / 2.0) * line.X;
				double p1y1 = p1.Y + Math.Sin(rad + Math.PI / 2.0) * line.Y;

				double p1x2 = p1.X + Math.Cos(rad - Math.PI / 2.0) * line.X;
				double p1y2 = p1.Y + Math.Sin(rad - Math.PI / 2.0) * line.Y;

				double p2x1 = p2.X + Math.Cos(rad + Math.PI / 2.0) * line.X;
				double p2y1 = p2.Y + Math.Sin(rad + Math.PI / 2.0) * line.Y;

				double p2x2 = p2.X + Math.Cos(rad - Math.PI / 2.0) * line.X;
				double p2y2 = p2.Y + Math.Sin(rad - Math.PI / 2.0) * line.Y;

				GL.Begin(PrimitiveType.TriangleStrip);
				GL.Color4(color);
				GL.Vertex2(p1x1, p1y1);
				GL.Vertex2(p1x2, p1y2);
				GL.Vertex2(p2x1, p2y1);
				GL.Vertex2(p2x2, p2y2);
				GL.End();
			}

			private void DrawPolyDashLine(Matrix4D proj, Matrix4D look, Box2d box, double lineWidth, double dashLength, Color color)
			{
				Matrix4D inv = Matrix4D.Invert(look) * Matrix4D.Invert(proj);
				Vector2 dash = new Vector2((inv.Row0.X + inv.Row0.Y) * dashLength, (inv.Row1.X + inv.Row1.Y) * dashLength) / 100.0;

				for (double i = box.Left; i + dash.X < box.Right; i += dash.X * 2)
				{
					DrawPolyLine(proj, look, new Vector2(i, box.Bottom), new Vector2(i + dash.X, box.Bottom), lineWidth, color);
				}

				for (double i = box.Bottom; i + dash.Y < box.Top; i += dash.Y * 2)
				{
					DrawPolyLine(proj, look, new Vector2(box.Right, i), new Vector2(box.Right, i + dash.Y), lineWidth, color);
				}

				for (double i = box.Left; i + dash.X < box.Right; i += dash.X * 2)
				{
					DrawPolyLine(proj, look, new Vector2(i, box.Top), new Vector2(i + dash.X, box.Top), lineWidth, color);
				}

				for (double i = box.Bottom; i + dash.Y < box.Top; i += dash.Y * 2)
				{
					DrawPolyLine(proj, look, new Vector2(box.Left, i), new Vector2(box.Left, i + dash.Y), lineWidth, color);
				}
			}

			internal void DrawGlControl()
			{
				// prepare
				GL.Enable(EnableCap.PointSmooth);
				GL.Enable(EnableCap.PolygonSmooth);
				GL.Hint(HintTarget.PointSmoothHint, HintMode.Nicest);
				GL.Hint(HintTarget.PolygonSmoothHint, HintMode.Nicest);
				GL.Enable(EnableCap.Blend);
				GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

				GL.Viewport(0, 0, GlControlWidth, GlControlHeight);
				GL.ClearColor(Color.Black);
				GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

				Matrix4D projPitch, projVolume, projString;
				Matrix4D.CreateOrthographic(MaxVelocity - MinVelocity, MaxPitch - MinPitch, float.Epsilon, 1.0, out projPitch);
				Matrix4D.CreateOrthographic(MaxVelocity - MinVelocity, MaxVolume - MinVolume, float.Epsilon, 1.0, out projVolume);
				Matrix4D.CreateOrthographicOffCenter(0.0, GlControlWidth, GlControlHeight, 0.0, -1.0, 1.0, out projString);
				Matrix4D lookPitch = Matrix4D.LookAt(new Vector3((MinVelocity + MaxVelocity) / 2.0, (MinPitch + MaxPitch) / 2.0, float.Epsilon), new Vector3((MinVelocity + MaxVelocity) / 2.0, (MinPitch + MaxPitch) / 2.0, 0.0), new Vector3(0, 1, 0));
				Matrix4D lookVolume = Matrix4D.LookAt(new Vector3((MinVelocity + MaxVelocity) / 2.0, (MinVolume + MaxVolume) / 2.0, float.Epsilon), new Vector3((MinVelocity + MaxVelocity) / 2.0, (MinVolume + MaxVolume) / 2.0, 0.0), new Vector3(0, 1, 0));

				// vertical grid
				{

					unsafe
					{
						GL.MatrixMode(MatrixMode.Projection);
						double* matrixPointer = &projPitch.Row0.X;
						GL.LoadMatrix(matrixPointer);
						GL.MatrixMode(MatrixMode.Modelview);
						matrixPointer = &lookPitch.Row0.X;
						GL.LoadMatrix(matrixPointer);
					}
					GL.Begin(PrimitiveType.Lines);

					for (double v = 0.0; v < MaxVelocity; v += 10.0)
					{
						GL.Color4(Color.DimGray);
						GL.Vertex2(v, 0.0);
						GL.Vertex2(v, float.MaxValue);
					}

					GL.End();

					Program.Renderer.CurrentProjectionMatrix = projString;
					Program.Renderer.CurrentViewMatrix = Matrix4D.Identity;

					for (double v = 0.0; v < MaxVelocity; v += 10.0)
					{
						Program.Renderer.OpenGlString.Draw(Fonts.VerySmallFont, v.ToString("0", Culture), new Point((int)VelocityToX(v) + 1, 1), TextAlignment.TopLeft, new Color128(Color24.Grey));
					}

					GL.Disable(EnableCap.Texture2D);
				}

				// horizontal grid
				switch (CurrentInputMode)
				{
					case InputMode.Pitch:
						unsafe
						{
							GL.MatrixMode(MatrixMode.Projection);
							double* matrixPointer = &projPitch.Row0.X;
							GL.LoadMatrix(matrixPointer);
							GL.MatrixMode(MatrixMode.Modelview);
							matrixPointer = &lookPitch.Row0.X;
							GL.LoadMatrix(matrixPointer);
						}

						GL.Begin(PrimitiveType.Lines);

						for (double p = 0.0; p < MaxPitch; p += 100.0)
						{
							GL.Color4(Color.DimGray);
							GL.Vertex2(MinVelocity, p);
							GL.Vertex2(MaxVelocity, p);
						}

						GL.End();

						Program.Renderer.CurrentProjectionMatrix = projString;
						Program.Renderer.CurrentViewMatrix = Matrix4D.Identity;

						for (double p = 0.0; p < MaxPitch; p += 100.0)
						{
							Program.Renderer.OpenGlString.Draw(Fonts.VerySmallFont, p.ToString("0", Culture), new Point(1, (int)PitchToY(p) + 1), TextAlignment.TopLeft, new Color128(Color24.Grey));
						}

						GL.Disable(EnableCap.Texture2D);
						break;
					case InputMode.Volume:
						unsafe
						{
							GL.MatrixMode(MatrixMode.Projection);
							double* matrixPointer = &projVolume.Row0.X;
							GL.LoadMatrix(matrixPointer);
							GL.MatrixMode(MatrixMode.Modelview);
							matrixPointer = &lookVolume.Row0.X;
							GL.LoadMatrix(matrixPointer);
						}

						GL.Begin(PrimitiveType.Lines);

						for (double v = 0.0; v < MaxVolume; v += 128.0)
						{
							GL.Color4(Color.DimGray);
							GL.Vertex2(MinVelocity, v);
							GL.Vertex2(MaxVelocity, v);
						}

						GL.End();

						Program.Renderer.CurrentProjectionMatrix = projString;
						Program.Renderer.CurrentViewMatrix = Matrix4D.Identity;

						for (double v = 0.0; v < MaxVolume; v += 128.0)
						{
							Program.Renderer.OpenGlString.Draw(Fonts.VerySmallFont, v.ToString("0", Culture), new Point(1, (int)VolumeToY(v) + 1), TextAlignment.TopLeft, new Color128(Color24.Grey));
						}

						GL.Disable(EnableCap.Texture2D);
						break;
				}

				// dot
				if (CurrentInputMode != InputMode.Volume)
				{
					unsafe
					{
						GL.MatrixMode(MatrixMode.Projection);
						double* matrixPointer = &projPitch.Row0.X;
						GL.LoadMatrix(matrixPointer);
						GL.MatrixMode(MatrixMode.Modelview);
						matrixPointer = &lookPitch.Row0.X;
						GL.LoadMatrix(matrixPointer);
					}

					GL.PointSize(11.0f);
					GL.Begin(PrimitiveType.Points);

					foreach (Vertex vertex in PitchVertices.Values)
					{
						Area area = SoundIndices.FirstOrDefault(a => a.LeftX <= vertex.X && vertex.X <= a.RightX);
						Color c;

						if (area != null && area.Index >= 0)
						{
							double hue = Utilities.HueFactor * area.Index;
							hue -= Math.Floor(hue);
							c = Utilities.GetColor(hue, vertex.Selected || vertex.IsOrigin);
						}
						else
						{
							if (vertex.Selected || vertex.IsOrigin)
							{
								c = Color.Silver;
							}
							else
							{
								c = Color.FromArgb((int)Math.Round(Color.Silver.R * 0.6), (int)Math.Round(Color.Silver.G * 0.6), (int)Math.Round(Color.Silver.B * 0.6));
							}
						}

						GL.Color4(c);
						GL.Vertex2(vertex.X, vertex.Y);
					}

					GL.End();
				}

				if (CurrentInputMode != InputMode.Pitch)
				{
					unsafe
					{
						GL.MatrixMode(MatrixMode.Projection);
						double* matrixPointer = &projVolume.Row0.X;
						GL.LoadMatrix(matrixPointer);
						GL.MatrixMode(MatrixMode.Modelview);
						matrixPointer = &lookVolume.Row0.X;
						GL.LoadMatrix(matrixPointer);
					}

					GL.PointSize(9.0f);
					GL.Begin(PrimitiveType.Points);

					foreach (Vertex vertex in VolumeVertices.Values)
					{
						Area area = SoundIndices.FirstOrDefault(a => a.LeftX <= vertex.X && vertex.X <= a.RightX);
						Color c;

						if (area != null && area.Index >= 0)
						{
							double hue = Utilities.HueFactor * area.Index;
							hue -= Math.Floor(hue);
							c = Utilities.GetColor(hue, vertex.Selected || vertex.IsOrigin);
						}
						else
						{
							if (vertex.Selected || vertex.IsOrigin)
							{
								c = Color.Silver;
							}
							else
							{
								c = Color.FromArgb((int)Math.Round(Color.Silver.R * 0.6), (int)Math.Round(Color.Silver.G * 0.6), (int)Math.Round(Color.Silver.B * 0.6));
							}
						}

						GL.Color4(c);
						GL.Vertex2(vertex.X, vertex.Y);
					}

					GL.End();
				}

				// line
				if (CurrentInputMode != InputMode.Volume)
				{
					unsafe
					{
						GL.MatrixMode(MatrixMode.Projection);
						double* matrixPointer = &projPitch.Row0.X;
						GL.LoadMatrix(matrixPointer);
						GL.MatrixMode(MatrixMode.Modelview);
						matrixPointer = &lookPitch.Row0.X;
						GL.LoadMatrix(matrixPointer);
					}

					foreach (Line line in PitchLines)
					{
						Vertex left = PitchVertices[line.LeftID];
						Vertex right = PitchVertices[line.RightID];

						Func<double, double> f = x => left.Y + (right.Y - left.Y) / (right.X - left.X) * (x - left.X);

						{
							Color c;

							if (line.Selected)
							{
								c = Color.Silver;
							}
							else
							{
								c = Color.FromArgb((int)Math.Round(Color.Silver.R * 0.6), (int)Math.Round(Color.Silver.G * 0.6), (int)Math.Round(Color.Silver.B * 0.6));
							}

							DrawPolyLine(projPitch, lookPitch, new Vector2(left.X, left.Y), new Vector2(right.X, right.Y), 1.5, c);
						}

						foreach (Area area in SoundIndices)
						{
							if (right.X < area.LeftX || left.X > area.RightX || area.Index < 0)
							{
								continue;
							}

							double hue = Utilities.HueFactor * area.Index;
							hue -= Math.Floor(hue);

							Vector2 p1 = new Vector2(left.X < area.LeftX ? area.LeftX : left.X, left.X < area.LeftX ? f(area.LeftX) : left.Y);
							Vector2 p2 = new Vector2(right.X > area.RightX ? area.RightX : right.X, right.X > area.RightX ? f(area.RightX) : right.Y);

							DrawPolyLine(projPitch, lookPitch, p1, p2, 1.5, Utilities.GetColor(hue, line.Selected));
						}
					}
				}

				if (CurrentInputMode != InputMode.Pitch)
				{
					unsafe
					{
						GL.MatrixMode(MatrixMode.Projection);
						double* matrixPointer = &projVolume.Row0.X;
						GL.LoadMatrix(matrixPointer);
						GL.MatrixMode(MatrixMode.Modelview);
						matrixPointer = &lookVolume.Row0.X;
						GL.LoadMatrix(matrixPointer);
					}

					foreach (Line line in VolumeLines)
					{
						Vertex left = VolumeVertices[line.LeftID];
						Vertex right = VolumeVertices[line.RightID];

						Func<double, double> f = x => left.Y + (right.Y - left.Y) / (right.X - left.X) * (x - left.X);

						{
							Color c;

							if (line.Selected)
							{
								c = Color.Silver;
							}
							else
							{
								c = Color.FromArgb((int)Math.Round(Color.Silver.R * 0.6), (int)Math.Round(Color.Silver.G * 0.6), (int)Math.Round(Color.Silver.B * 0.6));
							}

							DrawPolyLine(projVolume, lookVolume, new Vector2(left.X, left.Y), new Vector2(right.X, right.Y), 1.0, c);
						}

						foreach (Area area in SoundIndices)
						{
							if (right.X < area.LeftX || left.X > area.RightX || area.Index < 0)
							{
								continue;
							}

							double hue = Utilities.HueFactor * area.Index;
							hue -= Math.Floor(hue);

							Vector2 p1 = new Vector2(left.X < area.LeftX ? area.LeftX : left.X, left.X < area.LeftX ? f(area.LeftX) : left.Y);
							Vector2 p2 = new Vector2(right.X > area.RightX ? area.RightX : right.X, right.X > area.RightX ? f(area.RightX) : right.Y);

							DrawPolyLine(projVolume, lookVolume, p1, p2, 1.0, Utilities.GetColor(hue, line.Selected));
						}
					}
				}

				// area
				if (CurrentInputMode == InputMode.SoundIndex)
				{
					IEnumerable<Area> areas;

					if (previewArea != null)
					{
						areas = SoundIndices.Concat(new[] { previewArea });
					}
					else
					{
						areas = SoundIndices;
					}

					unsafe
					{
						GL.MatrixMode(MatrixMode.Projection);
						double* matrixPointer = &projPitch.Row0.X;
						GL.LoadMatrix(matrixPointer);
						GL.MatrixMode(MatrixMode.Modelview);
						matrixPointer = &lookPitch.Row0.X;
						GL.LoadMatrix(matrixPointer);
					}

					foreach (Area area in areas)
					{
						Color c;

						if (area.Index >= 0)
						{
							double hue = Utilities.HueFactor * area.Index;
							hue -= Math.Floor(hue);
							c = Utilities.GetColor(hue, true);
						}
						else
						{
							c = Color.Silver;
						}

						GL.Begin(PrimitiveType.TriangleStrip);

						GL.Color4(Color.FromArgb(64, c));
						GL.Vertex2(area.LeftX, 0.0);
						GL.Vertex2(area.RightX, 0.0);
						GL.Vertex2(area.LeftX, float.MaxValue);
						GL.Vertex2(area.RightX, float.MaxValue);

						GL.End();
					}
				}

				// selected range
				if (selectedRange != null)
				{
					switch (CurrentInputMode)
					{
						case InputMode.Pitch:
							DrawPolyDashLine(projPitch, lookPitch, selectedRange.Range, 2.0, 4.0, Color.DimGray);
							break;
						case InputMode.Volume:
							DrawPolyDashLine(projVolume, lookVolume, selectedRange.Range, 2.0, 4.0, Color.DimGray);
							break;
					}
				}

				// simulation speed
				if (BaseMotor.CurrentSimState == SimulationState.Started || BaseMotor.CurrentSimState == SimulationState.Paused)
				{
					unsafe
					{
						GL.MatrixMode(MatrixMode.Projection);
						double* matrixPointer = &projPitch.Row0.X;
						GL.LoadMatrix(matrixPointer);
						GL.MatrixMode(MatrixMode.Modelview);
						matrixPointer = &lookPitch.Row0.X;
						GL.LoadMatrix(matrixPointer);
					}

					GL.LineWidth(3.0f);
					GL.Begin(PrimitiveType.Lines);

					GL.Color4(Color.White);
					GL.Vertex2((float)CurrentSimSpeed, 0.0f);
					GL.Vertex2((float)CurrentSimSpeed, float.MaxValue);

					GL.End();
				}

				IsRefreshGlControl = false;
			}
		}
	}
}
