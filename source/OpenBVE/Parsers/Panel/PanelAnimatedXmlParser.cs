using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using OpenBveApi;
using OpenBveApi.Colors;
using OpenBveApi.FunctionScripting;
using OpenBveApi.Interface;
using OpenBveApi.Math;
using OpenBveApi.Objects;

namespace OpenBve.Parsers.Panel
{
	class PanelAnimatedXmlParser
	{
		/// <summary>Parses a openBVE panel.animated.xml file</summary>
		/// <param name="PanelFile">The relative path of the panel configuration file from the train</param>
		/// <param name="Encoding">The encoding the file is saved in. If the file uses a byte order mark, the encoding indicated by the byte order mark is used and the Encoding parameter is ignored.</param>
		/// <param name="TrainPath">The on-disk path to the train</param>
		/// <param name="Train">The train</param>
		/// <param name="Car">The car index to add the panel to</param>
		internal static void ParsePanelAnimatedXml(string PanelFile, System.Text.Encoding Encoding, string TrainPath, TrainManager.Train Train, int Car)
		{
			// The current XML file to load
			string FileName = Path.CombineFile(TrainPath, PanelFile);
			XDocument CurrentXML = XDocument.Load(FileName, LoadOptions.SetLineInfo);

			// Check for null
			if (CurrentXML.Root == null)
			{
				// We couldn't find any valid XML, so return false
				throw new System.IO.InvalidDataException();
			}

			IEnumerable<XElement> DocumentElements = CurrentXML.Root.Elements("PanelAnimated");

			// Check this file actually contains OpenBVE panel definition elements
			if (DocumentElements == null || !DocumentElements.Any())
			{
				// We couldn't find any valid XML, so return false
				throw new System.IO.InvalidDataException();
			}

			foreach (XElement element in DocumentElements)
			{
				ParsePanelAnimatedNode(element, FileName, Encoding, TrainPath, Train, Car, Train.Cars[Car].CarSections[0], 0);
			}
		}

		private static void ParsePanelAnimatedNode(XElement Element, string FileName, System.Text.Encoding Encoding, string TrainPath, TrainManager.Train Train, int Car, TrainManager.CarSection CarSection, int GroupIndex)
		{
			System.Globalization.CultureInfo Culture = System.Globalization.CultureInfo.InvariantCulture;

			int currentSectionElement = 0;
			int numberOfSectionElements = Element.Elements().Count();
			double invfac = numberOfSectionElements == 0 ? Loading.TrainProgressCurrentWeight : Loading.TrainProgressCurrentWeight / (double)numberOfSectionElements;

			foreach (XElement SectionElement in Element.Elements())
			{
				Loading.TrainProgress = Loading.TrainProgressCurrentSum + invfac * (double) currentSectionElement;
				if ((currentSectionElement & 4) == 0)
				{
					System.Threading.Thread.Sleep(1);
					if (Loading.Cancel) return;
				}

				string Section = SectionElement.Name.LocalName;

				switch (SectionElement.Name.LocalName.ToLowerInvariant())
				{
					case "group":
						if (GroupIndex == 0)
						{
							int n = 0;

							foreach (XElement KeyNode in SectionElement.Elements())
							{
								string Key = KeyNode.Name.LocalName;
								string Value = KeyNode.Value;
								int LineNumber = ((IXmlLineInfo) KeyNode).LineNumber;

								switch (Key.ToLowerInvariant())
								{
									case "number":
										if (Value.Length != 0 && !NumberFormats.TryParseIntVb6(Value, out n))
										{
											Interface.AddMessage(MessageType.Error, false, "Value is invalid in " + Key + " in " + Section + " at line " + LineNumber.ToString(Culture) + " in " + FileName);
										}
										break;
								}
							}

							if (n + 1 >= CarSection.Groups.Length)
							{
								Array.Resize(ref CarSection.Groups, n + 2);
								CarSection.Groups[n + 1] = new TrainManager.ElementsGroup
								{
									Elements = new ObjectManager.AnimatedObject[] { },
									Overlay = true
								};
							}

							ParsePanelAnimatedNode(SectionElement, FileName, Encoding, TrainPath, Train, Car, CarSection, n + 1);
						}
						break;
					case "touch":
						if (GroupIndex > 0)
						{
							Vector3 Position = Vector3.Zero;
							Vector3 Size = Vector3.Zero;
							int JumpScreen = GroupIndex - 1;
							int SoundIndex = -1;
							Translations.Command Command = Translations.Command.None;
							int CommandOption = 0;

							foreach (XElement KeyNode in SectionElement.Elements())
							{
								string Key = KeyNode.Name.LocalName;
								string Value = KeyNode.Value;
								int LineNumber = ((IXmlLineInfo) KeyNode).LineNumber;

								switch (Key.ToLowerInvariant())
								{
									case "position":
										{
											string[] s = Value.Split(',');
											if (s.Length == 3)
											{
												if (s[0].Length != 0 && !NumberFormats.TryParseDoubleVb6(s[0], out Position.X))
												{
													Interface.AddMessage(MessageType.Error, false, "X is invalid in " + Key + " in " + Section + " at line " + LineNumber.ToString(Culture) + " in " + FileName);
												}
												if (s[1].Length != 0 && !NumberFormats.TryParseDoubleVb6(s[1], out Position.Y))
												{
													Interface.AddMessage(MessageType.Error, false, "Y is invalid in " + Key + " in " + Section + " at line " + LineNumber.ToString(Culture) + " in " + FileName);
												}
												if (s[2].Length != 0 && !NumberFormats.TryParseDoubleVb6(s[2], out Position.Z))
												{
													Interface.AddMessage(MessageType.Error, false, "Z is invalid in " + Key + " in " + Section + " at line " + LineNumber.ToString(Culture) + " in " + FileName);
												}
											}
											else
											{
												Interface.AddMessage(MessageType.Error, false, "Three arguments are expected in " + Key + " in " + Section + " at line " + LineNumber.ToString(Culture) + " in " + FileName);
											}
										}
										break;
									case "size":
										{
											string[] s = Value.Split(',');
											if (s.Length == 3)
											{
												if (s[0].Length != 0 && !NumberFormats.TryParseDoubleVb6(s[0], out Size.X))
												{
													Interface.AddMessage(MessageType.Error, false, "X is invalid in " + Key + " in " + Section + " at line " + LineNumber.ToString(Culture) + " in " + FileName);
												}
												if (s[1].Length != 0 && !NumberFormats.TryParseDoubleVb6(s[1], out Size.Y))
												{
													Interface.AddMessage(MessageType.Error, false, "Y is invalid in " + Key + " in " + Section + " at line " + LineNumber.ToString(Culture) + " in " + FileName);
												}
												if (s[2].Length != 0 && !NumberFormats.TryParseDoubleVb6(s[2], out Size.Z))
												{
													Interface.AddMessage(MessageType.Error, false, "Z is invalid in " + Key + " in " + Section + " at line " + LineNumber.ToString(Culture) + " in " + FileName);
												}
											}
											else
											{
												Interface.AddMessage(MessageType.Error, false, "Three arguments are expected in " + Key + " in " + Section + " at line " + LineNumber.ToString(Culture) + " in " + FileName);
											}
										}
										break;
									case "jumpscreen":
										if (Value.Length != 0 && !NumberFormats.TryParseIntVb6(Value, out JumpScreen))
										{
											Interface.AddMessage(MessageType.Error, false, "Value is invalid in " + Key + " in " + Section + " at line " + LineNumber.ToString(Culture) + " in " + FileName);
										}
										break;
									case "soundindex":
										if (Value.Length != 0 && !NumberFormats.TryParseIntVb6(Value, out SoundIndex))
										{
											Interface.AddMessage(MessageType.Error, false, "Value is invalid in " + Key + " in " + Section + " at line " + LineNumber.ToString(Culture) + " in " + FileName);
										}
										break;
									case "command":
										{
											int i;
											for (i = 0; i < Translations.CommandInfos.Length; i++)
											{
												if (string.Compare(Value, Translations.CommandInfos[i].Name, StringComparison.OrdinalIgnoreCase) == 0)
												{
													break;
												}
											}
											if (i == Translations.CommandInfos.Length || Translations.CommandInfos[i].Type != Translations.CommandType.Digital)
											{
												Interface.AddMessage(MessageType.Error, false, "Value is invalid in " + Key + " in " + Section + " at line " + LineNumber.ToString(Culture) + " in " + FileName);
											}
											else
											{
												Command = Translations.CommandInfos[i].Command;
											}
										}
										break;
									case "commandoption":
										if (Value.Length != 0 && !NumberFormats.TryParseIntVb6(Value, out CommandOption))
										{
											Interface.AddMessage(MessageType.Error, false, "Value is invalid in " + Key + " in " + Section + " at line " + LineNumber.ToString(Culture) + " in " + FileName);
										}
										break;
								}
							}
							CreateTouchElement(CarSection.Groups[GroupIndex], Position, Size, JumpScreen, SoundIndex, Command, CommandOption);
						}
						break;
					case "include":
						{
							foreach (XElement KeyNode in SectionElement.Elements())
							{
								string Key = KeyNode.Name.LocalName;
								string Value = KeyNode.Value;
								int LineNumber = ((IXmlLineInfo)KeyNode).LineNumber;

								switch (Key.ToLowerInvariant())
								{
									case "filename":
										{
											string File = OpenBveApi.Path.CombineFile(TrainPath, Value);
											if (System.IO.File.Exists(File))
											{
												ObjectManager.AnimatedObjectCollection a = AnimatedObjectParser.ReadObject(File, Encoding);
												if (a != null)
												{
													for (int i = 0; i < a.Objects.Length; i++)
													{
														a.Objects[i].ObjectIndex = ObjectManager.CreateDynamicObject();
													}
													CarSection.Groups[GroupIndex].Elements = a.Objects;
												}
												else
												{
													Interface.AddMessage(MessageType.Error, false, "Value is invalid in " + Key + " in " + Section + " at line " + LineNumber.ToString(Culture) + " in " + FileName);
												}
											}
										}
										break;
								}
							}
						}
						break;
				}
			}
		}

		internal static void CreateTouchElementForExtendedMode(TrainManager.CarSection CarSection, TrainManager.Train Train)
		{
			Array.Resize(ref CarSection.Groups, 2);
			CarSection.Groups[1] = new TrainManager.ElementsGroup
			{
				Elements = new ObjectManager.AnimatedObject[] { },
				Overlay = true
			};
			foreach (var Element in CarSection.Groups[0].Elements)
			{
				FunctionType Type;
				if (CheckTouchFunctions(Element, out Type))
				{
					if (Element.States != null && Element.States.Any())
					{
						Vector3[] Positions = new Vector3[2];
						Positions[0] = Element.States[0].Position + new Vector3(0.0, 0.0, Interface.CurrentOptions.PanelAnimatedExtendedSize / 2.0);
						Positions[1] = Element.States[0].Position - new Vector3(0.0, 0.0, Interface.CurrentOptions.PanelAnimatedExtendedSize / 2.0);
						Vector3 Size = new Vector3(Interface.CurrentOptions.PanelAnimatedExtendedSize, Interface.CurrentOptions.PanelAnimatedExtendedSize, Interface.CurrentOptions.PanelAnimatedExtendedSize / 2.0);
						Translations.Command[] Commands = new Translations.Command[2];
						switch (Type)
						{
							case FunctionType.Power:
								if (Train.Handles.SingleHandle)
								{
									Commands[0] = Translations.Command.SingleBrake;
									Commands[1] = Translations.Command.SinglePower;
								}
								else
								{
									Commands[0] = Translations.Command.PowerDecrease;
									Commands[1] = Translations.Command.PowerIncrease;
								}
								break;
							case FunctionType.Brake:
								if (Train.Handles.SingleHandle)
								{
									Commands[0] = Translations.Command.SingleBrake;
									Commands[1] = Translations.Command.SinglePower;
								}
								else
								{
									Commands[0] = Translations.Command.BrakeIncrease;
									Commands[1] = Translations.Command.BrakeDecrease;
								}
								break;
							case FunctionType.Reverser:
								Commands[0] = Translations.Command.ReverserForward;
								Commands[1] = Translations.Command.ReverserBackward;
								break;
						}
						CreateTouchElement(CarSection.Groups[1], Positions[0], Size, 0, -1, Commands[0], 0);
						CreateTouchElement(CarSection.Groups[1], Positions[1], Size, 0, -1, Commands[1], 0);
					}
				}
			}
		}

		private enum FunctionType
		{
			None,
			Power,
			Brake,
			Reverser
		}

		private static bool CheckTouchFunctions(ObjectManager.AnimatedObject Element, out FunctionType Type)
		{
			if (CheckTouchFunction(Element.StateFunction, out Type))
			{
				return true;
			}
			if (CheckTouchFunction(Element.TranslateXFunction, out Type))
			{
				return true;
			}
			if (CheckTouchFunction(Element.TranslateYFunction, out Type))
			{
				return true;
			}
			if (CheckTouchFunction(Element.TranslateZFunction, out Type))
			{
				return true;
			}
			if (CheckTouchFunction(Element.RotateXFunction, out Type))
			{
				return true;
			}
			if (CheckTouchFunction(Element.RotateYFunction, out Type))
			{
				return true;
			}
			if (CheckTouchFunction(Element.RotateZFunction, out Type))
			{
				return true;
			}
			if (CheckTouchFunction(Element.TextureShiftXFunction, out Type))
			{
				return true;
			}
			if (CheckTouchFunction(Element.TextureShiftYFunction, out Type))
			{
				return true;
			}
			if (CheckTouchFunction(Element.LEDFunction, out Type))
			{
				return true;
			}
			Type = FunctionType.None;
			return false;
		}

		private static bool CheckTouchFunction(FunctionScript Function, out FunctionType Type)
		{
			if (Function != null)
			{
				foreach (var Instruction in Function.InstructionSet)
				{
					switch (Instruction)
					{
						case Instructions.PowerNotch:
							Type = FunctionType.Power;
							return true;
						case Instructions.BrakeNotch:
						case Instructions.BrakeNotchLinear:
							Type = FunctionType.Brake;
							return true;
						case Instructions.ReverserNotch:
							Type = FunctionType.Reverser;
							return true;
					}
				}
			}
			Type = FunctionType.None;
			return false;
		}

		private static void CreateTouchElement(TrainManager.ElementsGroup Group, Vector3 Position, Vector3 Size, int ScreenIndex, int SoundIndex, Translations.Command Command, int CommandOption)
		{
			Vertex t0 = new Vertex(Size.X, Size.Y, -Size.Z);
			Vertex t1 = new Vertex(Size.X, -Size.Y, -Size.Z);
			Vertex t2 = new Vertex(-Size.X, -Size.Y, -Size.Z);
			Vertex t3 = new Vertex(-Size.X, Size.Y, -Size.Z);
			Vertex t4 = new Vertex(Size.X, Size.Y, Size.Z);
			Vertex t5 = new Vertex(Size.X, -Size.Y, Size.Z);
			Vertex t6 = new Vertex(-Size.X, -Size.Y, Size.Z);
			Vertex t7 = new Vertex(-Size.X, Size.Y, Size.Z);
			ObjectManager.StaticObject Object = new ObjectManager.StaticObject();
			Object.Mesh.Vertices = new VertexTemplate[] { t0, t1, t2, t3, t4, t5, t6, t7 };
			Object.Mesh.Faces = new MeshFace[] { new MeshFace(new int[] { 0, 1, 2, 3 }), new MeshFace(new int[] { 0, 4, 5, 1 }), new MeshFace(new int[] { 0, 3, 7, 4 }), new MeshFace(new int[] { 6, 5, 4, 7 }), new MeshFace(new int[] { 6, 7, 3, 2 }), new MeshFace(new int[] { 6, 2, 1, 5 }) };
			Object.Mesh.Materials = new MeshMaterial[1];
			Object.Mesh.Materials[0].Flags = 0;
			Object.Mesh.Materials[0].Color = Color32.White;
			Object.Mesh.Materials[0].TransparentColor = Color24.Blue;
			Object.Mesh.Materials[0].DaytimeTexture = null;
			Object.Mesh.Materials[0].NighttimeTexture = null;
			Object.Dynamic = true;
			if (Group.TouchElements == null)
			{
				Group.TouchElements = new TrainManager.TouchElement[0];
			}
			int n = Group.TouchElements.Length;
			Array.Resize(ref Group.TouchElements, n + 1);
			Group.TouchElements[n] = new TrainManager.TouchElement
			{
				Element = new ObjectManager.AnimatedObject(),
				JumpScreenIndex = ScreenIndex,
				SoundIndex = SoundIndex,
				Command = Command,
				CommandOption = CommandOption
			};
			Group.TouchElements[n].Element.States = new ObjectManager.AnimatedObjectState[1];
			Group.TouchElements[n].Element.States[0].Position = Position;
			Group.TouchElements[n].Element.States[0].Object = Object;
			Group.TouchElements[n].Element.CurrentState = 0;
			Group.TouchElements[n].Element.ObjectIndex = ObjectManager.CreateDynamicObject();
			ObjectManager.Objects[Group.TouchElements[n].Element.ObjectIndex] = Object.Clone();
			int m = Interface.CurrentControls.Length;
			Array.Resize(ref Interface.CurrentControls, m + 1);
			Interface.CurrentControls[m].Command = Command;
			Interface.CurrentControls[m].Method = Interface.ControlMethod.Touch;
			Interface.CurrentControls[m].Option = CommandOption;
		}
	}
}
