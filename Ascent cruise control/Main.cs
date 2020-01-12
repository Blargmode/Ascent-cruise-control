#region pre-script
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;
namespace IngameScript
{
	public class Program : MyGridProgram
	{
		#endregion
		#region in-game
		//To put your code in a PB copy from this comment...

		IMyShipController MainController;
		bool Initialized = false;
		
		Aligner Align;
		CruiseControl Cruise;

		Screens LCDs;
		
		MyIni Settings;
		const string SettingsHeader = "ACC Settings";
		const string SettingsController = "controller position";
		const string SettingsTargetSpeed = "target speed";
		const string SettingsSelectedThrusters = "selected thrusters";
		const string SettingsTargetAltAscending = "target altitude ascending";
		const string SettingsTargetAltDescending = "target altitude descending";
		const string SettingsDisableCruiseExitingGravity = "disable cruise exiting gravity";
		const string SettingsDisableAlignExitingGravity = "disable align exiting gravity";
		const string SettingsUseSeaLevel = "use sealevel";
		const string SettingsCruiseEnabled = "cruise enabled";
		const string SettingsAlignEnabled = "align enabled";
		const string SettingsWorldTopSpeed = "world top speed";


		float targetSpeed = 95;
		Base6Directions.Direction thrustDirection = Base6Directions.Direction.Left; //Using left as auto
		double targetAltAscending = double.MaxValue;
		double targetAltDescending = 0;
		bool disableCruiseExitingGravity = true;
		bool disableAlignExitingGravity = false;
		bool useSeaLevel = false;
		float worldTopSpeed = 100;

		Dictionary<string, string> Errors = new Dictionary<string, string>();

		List<string> InputErrors = new List<string>();
		TimeSpan InputErrorTimeout = TimeSpan.MinValue;
		TimeSpan time;

		//ProjectorVisualization proj;

		int detailedInfoTextWith = 40;

		IMyTextSurface screen; //For debuygging
		

		public Program()
		{
			Runtime.UpdateFrequency = UpdateFrequency.Update10;
		}

		public void Main(string argument, UpdateType updateType)
		{
			time += Runtime.TimeSinceLastRun;

			if (!Initialized && time.Seconds % 5 == 0)
			{
				//Run Init every 5 seconds if not initialized.
				Initialized = Init();
			}

			if (Initialized)
			{
				if ((updateType & (UpdateType.Terminal | UpdateType.Trigger)) != 0)
				{
					Input(argument);
				}

				if ((updateType & UpdateType.Update10) != 0)
				{

					screen.WriteText($"Align: {(Align != null && Align.Enabled ? "on" : "off")}, Cruise: {(Cruise != null && Cruise.Enabled ? "on" : "off")}");
					if (Align != null && Align.Enabled)
					{
						Align.Update();
					}

					if (Cruise != null && Cruise.Enabled)
					{
						Cruise.Update(Runtime.TimeSinceLastRun.TotalSeconds);
					}

					if(LCDs != null)
					{
						ScreenData data = new ScreenData();

						data.maxAlt = targetAltAscending;
						data.minAlt = targetAltDescending;
						data.maxSpeed = worldTopSpeed;
						if (Cruise != null)
						{
							
							if (Cruise.Enabled)
							{
								data.speed = Cruise.Speed;
								data.targetSpeed = Cruise.AdaptiveTargetSpeed;
							}
							else if (MainController != null)
							{
								data.speed = MainController.GetShipSpeed();
								data.targetSpeed = targetSpeed;
							}
							data.thrustOverrideH2 = Cruise.ThrustOverrideHydrogen;
							data.thrustOverridePW = Cruise.ThrustOverrideElectric;
							data.cruiseEnabled = Cruise.Enabled;
						}
						else
						{
							if(MainController != null)
							{
								data.speed = MainController.GetShipSpeed();
							}
							data.targetSpeed = targetSpeed;
						}

						if (Align != null)
						{
							data.alignEnabled = Align.Enabled;
						}



						LCDs.Update(data);
					}
				}
			}

			DetailedInfo();
			BroadcastStatus();
			//DrawScreens();
		}

		void ResetController()
		{
			Settings.Set(SettingsHeader, SettingsController, "");
			SaveSettings();
			Initialized = false;
			MainController = null;
		}

		void ToggleCruiseControl()
		{
			if(Cruise != null)
			{
				if (Cruise.Enabled)
				{
					StartCruiseControl(false);
				}
				else
				{
					StartCruiseControl(true);
				}
			}
		}

		void StartCruiseControl(bool enable)
		{
			if (Initialized && Cruise != null)
			{
				if (enable)
				{
					Cruise.Start(targetSpeed, thrustDirection, targetAltDescending, targetAltAscending, useSeaLevel, disableCruiseExitingGravity);
				}
				else
				{
					Cruise.Stop();
				}
			}
		}

		void ToggleAlign()
		{
			if (Initialized && Align != null)
			{
				if (Align.Enabled)
				{
					StartAlign(false);
				}
				else
				{
					StartAlign(true);
				}
			}
		}

		void StartAlign(bool enable)
		{
			if (Align != null)
			{
				if (enable)
				{
					Align.Start(disableAlignExitingGravity);
				}
				else
				{
					Align.Stop();
				}
			}
		}
		

		void Input(string argument)
		{
			InputErrors.Clear();

			//Legacy commands
			argument = argument.ToLower().Trim();
			if(argument.Length == 0)
			{
				//Toggle cruise control
				ToggleCruiseControl();
			}
			else if(argument == "on")
			{
				StartCruiseControl(true);
			}
			else if (argument == "off")
			{
				StartCruiseControl(true);
			}
			else if (argument == "swap")
			{
				if (thrustDirection == Base6Directions.Direction.Forward) thrustDirection = Base6Directions.Direction.Up;
				else if (thrustDirection == Base6Directions.Direction.Up) thrustDirection = Base6Directions.Direction.Forward;

				if (Cruise != null && Cruise.Enabled)
				{
					//Stop then start to reset used thrusters
					StartCruiseControl(false);
					StartCruiseControl(true);
				}
			}
			else if (argument == "reset")
			{
				ResetController();
				return;
			}
			else
			{
				float newSpeed = 0;
				if(float.TryParse(argument, out newSpeed))
				{
					targetSpeed = newSpeed;
					StartCruiseControl(true);
				}
				else
				{
					string[] commands = argument.ToLower().Split(',');
					foreach (var command in commands)
					{
						string[] parts = command.Trim(new char[] { ' ', '\'' }).Split(' ');

						for (int i = 0; i < parts.Length; i++)
						{
							parts[i] = parts[i].Trim();
						}

						if(parts.Length > 0)
						{
							if (parts[0] == "?")
							{
								InputErrors.Add("Errors shows up here.");
							}
							else if (parts[0] == "cruise" || parts[0] == "cc")
							{
								if (parts.Length > 1)
								{
									float num = 0;
									bool on = true;
									for (int i = 1; i < parts.Length; i++)
									{
										if (float.TryParse(parts[i], out num))
										{
											if (targetSpeed == num && Cruise != null && Cruise.Enabled)
											{
												on = false;
											}
											targetSpeed = num;
										}
										else if (parts[i] == "on")
										{
											on = true;
										}
										else if (parts[i] == "off")
										{
											on = false;
										}
										else
										{
											InputErrors.Add("'" + parts[1] + "' not recognozed for 'cruise' use: on|off (or omitt for toggle).");
										}
									}
									StartCruiseControl(on);
								}
								else
								{
									ToggleCruiseControl();
								}
							}
							else if (parts[0] == "align" || parts[0] == "al")
							{
								if (parts.Length > 1)
								{
									bool on = true;
									for (int i = 1; i < parts.Length; i++)
									{
										if (parts[i] == "on")
										{
											on = true;
										}
										else if (parts[i] == "off")
										{
											on = false;
										}
										else
										{
											InputErrors.Add("'" + parts[1] + "' not recognozed for 'align' use: on|off (or omitt for toggle).");
										}
									}
									StartAlign(on);
								}
								else
								{
									ToggleAlign();
								}
							}
							else if (parts[0] == "ts")
							{
								if (parts.Length > 1)
								{
									float num = 0;
									if (float.TryParse(parts[1], out num))
									{
										targetSpeed = num;

										if (Cruise != null && Cruise.Enabled) StartCruiseControl(true);
									}
									else
									{
										InputErrors.Add("'" + parts[1] + "' not recognized as a number. 'ts' requires one (negative for descent).");
									}
								}
								else
								{
									InputErrors.Add("'ts' requires a number (negative for descent).");
								}
							}
							else if (parts[0] == "td")
							{
								if (parts.Length > 1)
								{
									switch (parts[1])
									{
										case "up": thrustDirection = Base6Directions.Direction.Up; break;
										case "down": thrustDirection = Base6Directions.Direction.Down; break;
										case "forward": thrustDirection = Base6Directions.Direction.Forward; break;
										case "backward": thrustDirection = Base6Directions.Direction.Backward; break;
										case "auto": thrustDirection = Base6Directions.Direction.Left; break;
										default:
											InputErrors.Add("'" + parts[1] + "' not recognized for 'td', use: up | down | forward | backward | auto.");
										break;
									}

									if (Cruise != null && Cruise.Enabled)
									{
										StartCruiseControl(false);
										StartCruiseControl(true);
									}

									if (Align != null && Align.Enabled)
									{
										StartAlign(true);
									}
								}
								else
								{
									InputErrors.Add("'td' requires a direction: up | down | forward | backward | auto.");
								}
							}
							else if (parts[0] == "mx")
							{
								if (parts.Length > 1)
								{
									double num = 0;
									if (double.TryParse(parts[1], out num))
									{
										if(num <= 0)
										{
											targetAltAscending = double.MaxValue;
										}
										else
										{
											targetAltAscending = num;
										}
										

										if (Cruise != null && Cruise.Enabled) StartCruiseControl(true);
									}
									else if(parts[1] == "this") 
									{
										if(MainController != null)
										{
											double elev = 0;
											if(MainController.TryGetPlanetElevation((useSeaLevel ? MyPlanetElevation.Sealevel : MyPlanetElevation.Surface), out elev))
											{
												targetAltAscending = elev;
											}
										}
										else
										{
											InputErrors.Add("No controller, can set 'mx' to 'this'.");
										}
									}
									else
									{
										InputErrors.Add("'mx' requires a number or 'this', couldn't parse '" + parts[1] + "'.");
									}
								}
								else
								{
									InputErrors.Add("'mx' requires a number (set to -1 for unlimited) or 'this' for current altitude.");
								}
							}
							else if (parts[0] == "mn")
							{
								if (parts.Length > 1)
								{
									double num = 0;
									if (double.TryParse(parts[1], out num))
									{
										if (num < 0)
										{
											targetAltDescending = 0;
										}
										else
										{
											targetAltDescending = num;
										}
										if (Cruise != null && Cruise.Enabled) StartCruiseControl(true);
									}
									else if (parts[1] == "this")
									{
										if (MainController != null)
										{
											double elev = 0;
											if (MainController.TryGetPlanetElevation(MyPlanetElevation.Surface, out elev))
											{
												targetAltDescending = elev;
											}
										}
										else
										{
											InputErrors.Add("No controller, can set 'mn' to 'this'.");
										}
									}
									else
									{
										InputErrors.Add("'mn' requires a number or 'this', couldn't parse '" + parts[1] + "'.");
									}
								}
								else
								{
									InputErrors.Add("'mn' requires a number or 'this' for current altitude.");
								}
							}
							else if (parts[0] == "rf")
							{
								if (parts.Length > 1)
								{
									if(parts[1] == "sea" || parts[1] == "sealevel")
									{
										useSeaLevel = true;
									}
									else
									{
										useSeaLevel = false;
									}
									if (Cruise != null && Cruise.Enabled) StartCruiseControl(true);
								}
								else
								{
									InputErrors.Add("'rf' requires a reference point: sea | ground");
								}
							}
							else if (parts[0] == "ag")
							{
								if (parts.Length > 1)
								{
									if (parts[1] == "yes")
									{
										disableAlignExitingGravity = true;
									}
									else
									{
										disableAlignExitingGravity = false;
									}
									if (Cruise != null && Cruise.Enabled) StartCruiseControl(true);
								}
								else
								{
									InputErrors.Add("'ag' requires a state: on | off.");
								}
							}
							else if (parts[0] == "cg")
							{
								if (parts.Length > 1)
								{
									if (parts[1] == "yes")
									{
										disableCruiseExitingGravity = true;
									}
									else
									{
										disableCruiseExitingGravity = false;
									}
									if (Cruise != null && Cruise.Enabled) StartCruiseControl(true);
								}
								else
								{
									InputErrors.Add("'cg' requires a state: on | off.");
								}
							}
							else if (parts[0] == "ws")
							{
								if (parts.Length > 1)
								{
									float num = 0;
									if (float.TryParse(parts[1], out num))
									{
										worldTopSpeed = num;
									}
									else
									{
										InputErrors.Add("'" + parts[1] + "' not recognized as a number. 'ws' requires one.");
									}
								}
								else
								{
									InputErrors.Add("'ws' requires a number.");
								}
							}
							else
							{
								InputErrors.Add("Command not recognized '" + parts[0] + "'");
							}
						}
						else
						{
							InputErrors.Add("Input not recognized '" + command + "'");
						}
					}
				}
			}

			if(InputErrors.Count > 0)
			{
				InputErrorTimeout = TimeSpan.FromSeconds(60) + time;
			}
			else
			{
				InputErrorTimeout = TimeSpan.MinValue;
			}

			SaveSettings();
		}

		void DetailedInfo()
		{
			StringBuilder sb = new StringBuilder();

			//if (false || (InputErrorTimeout > time && InputErrors.Count > 0))
			//{
				//sb.AppendLine("____Input Errors_________");
				//foreach (var item in InputErrors)
				//{
				//	sb.AppendLine(AdjustTextToWidth("- " + item, detailedInfoTextWith));
				//}

				//sb.AppendLine();
				//sb.AppendLine("____Input Options_________");
				//sb.AppendLine("Options within [brackets] are optional.");
				//sb.AppendLine("Several comma separated");
				//sb.AppendLine("commands are allowed.");
				//sb.AppendLine();
				//sb.AppendLine("?");
				//sb.AppendLine("Prints this message.");
				//sb.AppendLine();
				//sb.AppendLine("cruise [on | off] [#]");
				//sb.AppendLine("Toggle or set state and target speed");
				//sb.AppendLine("of cruise control.");
				//sb.AppendLine();
				//sb.AppendLine("align [on | off]");
				//sb.AppendLine("Toggle or set state of alignment.");
				//sb.AppendLine();
				//sb.AppendLine("speed #");
				//sb.AppendLine("Set target speed. Negative values");
				//sb.AppendLine("for descent. (Save before attepting,");
				//sb.AppendLine("there's no guarantee you'll stop in time.)");
				//sb.AppendLine();
				//sb.AppendLine("travel up | down | forward | backward | auto");
				//sb.AppendLine("Direction of travel in relation to");
				//sb.AppendLine("your cockpit/flight seat.");
				//sb.AppendLine();
				//sb.AppendLine("alt-max #");
				//sb.AppendLine("Target altitude to reach when ascending. ");
				//sb.AppendLine("Set to -1 for infinity, space.");
				//sb.AppendLine();
				//sb.AppendLine("alt-min #");
				//sb.AppendLine("Target altitude to reach when decending.");
				//sb.AppendLine("Altitude is measured at center of");
				//sb.AppendLine("mass. There's no guarantee you'll");
				//sb.AppendLine("stop in time.");
				//sb.AppendLine();
				//sb.AppendLine("reference sea | ground");
				//sb.AppendLine("When ascending to a max altitude.");
				//sb.AppendLine("Ground is good for keeping a");
				//sb.AppendLine("distance from the ground.");
				//sb.AppendLine("Sea is good for keeping a");
				//sb.AppendLine("consistent altitude, i.e. an orbit.");
				//sb.AppendLine();
				//sb.AppendLine("0g-align on | off");
				//sb.AppendLine("If align should stay engaged after");
				//sb.AppendLine("leaving the planets gravity.");
				//sb.AppendLine();
				//sb.AppendLine("0g-cruise on | off");
				//sb.AppendLine("If cruise control should stay engaged");
				//sb.AppendLine("after leaving the planets gravity.");


			//}
			//else
			//{

				sb.AppendLine("Blarg's Ascent Cruise Control");
				sb.AppendLine($"Cruise: {(Cruise != null && Cruise.Enabled ? "on" : "off")}   |   Align: {(Align != null && Align.Enabled ? "on" : "off")} ");

				if (Errors.Count > 0)
				{
					sb.AppendLine();
					sb.AppendLine("____Errors_________");
					foreach (var item in Errors)
					{
						sb.AppendLine(AdjustTextToWidth("- " + item.Value, detailedInfoTextWith));
					}
				}

				if (InputErrorTimeout > time && InputErrors.Count > 0)
				{
					sb.AppendLine();
					sb.AppendLine("____Input Errors_________");
					foreach (var item in InputErrors)
					{
						sb.AppendLine(AdjustTextToWidth("- " + item, detailedInfoTextWith));
					}
				}



				sb.AppendLine();
				sb.AppendLine("____Start/stop_________");
				sb.AppendLine("Commands:");
				sb.AppendLine("- cruise [on|off][#]");
				sb.AppendLine("- align [on|off]");
				sb.AppendLine("[optional], will toggle if");
				sb.AppendLine("omitted. # is target speed.");


				sb.AppendLine();
				sb.AppendLine("____Settings_________");
				sb.AppendLine("Type 'prefix value' and press");
				sb.AppendLine("run to set, for example 'ts 100'");
				sb.AppendLine("Run just the prefix for details.");

			if (Cruise != null)
				{
					sb.AppendLine();
					sb.AppendLine("____Cruise Control_________");
					sb.AppendLine($"ts    Target speed: {targetSpeed.ToString("n0")} ");
					sb.AppendLine($"cg   Disable at zero-G: {(disableCruiseExitingGravity ? "Yes" : "No")}");
					sb.AppendLine($"td    Travel direction: {(thrustDirection == Base6Directions.Direction.Left ? "Auto (" + Cruise.Forward.ToString() + ")" : thrustDirection.ToString())}");
					sb.AppendLine($"mx  Target altiude max: {(targetAltAscending == double.MaxValue ? "••" : targetAltAscending.ToString("n0") + "m")}");
					sb.AppendLine($"mn  Target altiude min: {targetAltDescending.ToString("n0") + "m"}");
					sb.AppendLine($"rf     Max altiude reference: {(useSeaLevel ? "Sealevel" : "Ground")}");
					sb.AppendLine($"ws  World top speed: {worldTopSpeed.ToString("n0")}");
			}
				if (Align != null)
				{
					sb.AppendLine();
					sb.AppendLine("____Aligner_________");
					sb.AppendLine($"ag   Disable at zero-G: {(disableAlignExitingGravity ? "yes" : "no")}");
				}
			//}

			Echo(sb.ToString());
		}

		void BroadcastStatus()
		{
			if (Initialized)
			{
				IGC.SendBroadcastMessage("ACC", Cruise.AdaptiveTargetSpeed, TransmissionDistance.CurrentConstruct);
			}
		}

		bool GetController(List<IMyShipController> controllers, Vector3I position)
		{
			foreach (var item in controllers)
			{
				if(item.Position == position)
				{
					MainController = item;
					return true;
				}
			}
			return false;
		}

		bool GetController(List<IMyShipController> controllers)
		{
			foreach (var item in controllers)
			{
				if (item.CanControlShip)
				{
					if (item.IsUnderControl)
					{
						MainController = item;
						return true;
					}
				}
			}
			return false;
		}

		bool Init()
		{
			#region controller and settings

			var controllers = new List<IMyShipController>();
			GridTerminalSystem.GetBlocksOfType(controllers, x => x.IsSameConstructAs(Me));

			bool cruiseEnabledInSettings = false;
			bool alignEnabledInSettings = false;

			Settings = new MyIni();
			if (Settings.TryParse(Storage))
			{
				if (Settings.ContainsSection(SettingsHeader))
				{
					targetSpeed = Settings.Get(SettingsHeader, SettingsTargetSpeed).ToSingle(targetSpeed);
					thrustDirection = (Base6Directions.Direction)Settings.Get(SettingsHeader, SettingsSelectedThrusters).ToInt32((int)thrustDirection);
					targetAltAscending = Settings.Get(SettingsHeader, SettingsTargetAltAscending).ToDouble(targetAltAscending);
					targetAltDescending = Settings.Get(SettingsHeader, SettingsTargetAltDescending).ToDouble(targetAltDescending);
					disableCruiseExitingGravity = Settings.Get(SettingsHeader, SettingsDisableCruiseExitingGravity).ToBoolean(disableCruiseExitingGravity);
					disableAlignExitingGravity = Settings.Get(SettingsHeader, SettingsDisableAlignExitingGravity).ToBoolean(disableAlignExitingGravity);
					useSeaLevel = Settings.Get(SettingsHeader, SettingsUseSeaLevel).ToBoolean(useSeaLevel);
					worldTopSpeed = Settings.Get(SettingsHeader, SettingsWorldTopSpeed).ToSingle(worldTopSpeed);

					cruiseEnabledInSettings = Settings.Get(SettingsHeader, SettingsCruiseEnabled).ToBoolean();
					alignEnabledInSettings = Settings.Get(SettingsHeader, SettingsCruiseEnabled).ToBoolean();

					var parts = Settings.Get(SettingsHeader, SettingsController).ToString().Split(';');
					if (parts.Length == 3)
					{
						GetController(controllers, new Vector3I(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2])));
					}
				}
			}
			
			

			//If fetching controller from storage failed, try to set up a new.
			if (MainController == null)
			{
				if (!GetController(controllers))
				{
					Errors["no controller"] = "No ship controller found. Can't resume. Sit in one for a few seconds.";
					return false;
				}
			}
			if (MainController != null)
			{
				Errors.Remove("no controller");
			}

			Settings.Set(SettingsHeader, SettingsController, $"{MainController.Position.X};{MainController.Position.Y};{MainController.Position.Z}");
			SaveSettings();

			screen = (MainController as IMyTextSurfaceProvider).GetSurface(0);
			screen.ContentType = ContentType.TEXT_AND_IMAGE;

			var sprites = new List<string>();
			screen.GetSprites(sprites);
			Me.CustomData = "";
			foreach (var item in sprites)
			{
				Me.CustomData += item + "\n";
			}

			#endregion




			var allBlocks = new List<IMyTerminalBlock>();
			GridTerminalSystem.GetBlocksOfType(allBlocks, x => x.IsSameConstructAs(Me));

			List<IMyGyro> gyros = new List<IMyGyro>();
			List<IMyThrust> thrusters = new List<IMyThrust>();
			List<IMyTextSurface> screens = new List<IMyTextSurface>();

			foreach (var item in allBlocks)
			{
				//if(item is IMyProjector)
				//{
				//	proj = new ProjectorVisualization(item as IMyProjector, Vector3I.Zero);
				//}

				if(item is IMyGyro)
				{
					gyros.Add(item as IMyGyro);
				}

				if (item is IMyThrust)
				{
					thrusters.Add(item as IMyThrust);
				}
				if(item as IMyTextSurfaceProvider != null)
				{
					if (item.CustomName.Contains("#ACC"))
					{
						int screennr = 0;
						var parts = item.CustomName.Split('@');
						if(parts.Length > 1)
						{
							for (int i = 0; i < parts.Length; i++)
							{
								if (parts[i].EndsWith("#ACC") && parts.Length > i+1)
								{
									int.TryParse(new string(parts[i+1].TakeWhile(char.IsDigit).ToArray()), out screennr);
								}
							}
						}
						screens.Add((item as IMyTextSurfaceProvider).GetSurface(screennr));
						screens[screens.Count-1].ContentType = ContentType.SCRIPT;
						screens[screens.Count-1].Script = "";
					}
				}
			}

			if(screens.Count > 0)
			{
				LCDs = new Screens(screens);
			}

			if(gyros.Count > 0)
			{
				Align = new Aligner(MainController, gyros);
				if (alignEnabledInSettings)
				{
					StartAlign(true);
				}
			}

			if (thrusters.Count > 0)
			{
				Cruise = new CruiseControl(MainController, thrusters);
				if (cruiseEnabledInSettings)
				{
					StartCruiseControl(true);
				}
			}
			return true;
		}


		void SaveSettings()
		{
			if (Settings == null) Settings = new MyIni();

			Settings.Set(SettingsHeader, SettingsTargetSpeed, targetSpeed);
			Settings.Set(SettingsHeader, SettingsSelectedThrusters, (int)thrustDirection);
			Settings.Set(SettingsHeader, SettingsTargetAltAscending, targetAltAscending);
			Settings.Set(SettingsHeader, SettingsTargetAltDescending, targetAltDescending);
			Settings.Set(SettingsHeader, SettingsDisableCruiseExitingGravity, disableCruiseExitingGravity);
			Settings.Set(SettingsHeader, SettingsDisableAlignExitingGravity, disableAlignExitingGravity);
			Settings.Set(SettingsHeader, SettingsUseSeaLevel, useSeaLevel);
			Settings.Set(SettingsHeader, SettingsCruiseEnabled, (Cruise == null ? false : Cruise.Enabled));
			Settings.Set(SettingsHeader, SettingsAlignEnabled, (Align == null ? false : Align.Enabled));
			Settings.Set(SettingsHeader, SettingsWorldTopSpeed, worldTopSpeed);

			Storage = Settings.ToString();
		}


		//void DrawScreens()
		//{
		//	List<IMyTextSurface> screens = new List<IMyTextSurface>(); //TEMP This whole sthing is going to be removed
		//	float percent = 0;
		//	if (Cruise != null && Cruise.Enabled)
		//	{
		//		percent = (float)(Math.Abs(Math.Round(Cruise.Speed)) / Math.Abs(Math.Round(Cruise.AdaptiveTargetSpeed)));
		//		if (float.IsNaN(percent) || float.IsInfinity(percent)) percent = 1f;
		//		percent = MathHelper.Clamp(percent, 0, 1);
		//	}

		//	foreach (var surface in screens)
		//	{
		//		using (var frame = surface.DrawFrame())
		//		{
		//			SurfaceMath sm = new SurfaceMath(surface);

		//			Color background = surface.ScriptBackgroundColor;
		//			Color foreground = surface.ScriptForegroundColor;
		//			//Vector3 hsv = ColorExtensions.ColorToHSV(foreground);
		//			//if(hsv.Z < 0.5f)
		//			//{
		//			//	hsv.Z = MathHelper.Clamp(hsv.Z + 0.5f, 0f, 1f);
		//			//}
		//			//else
		//			//{
		//			//	hsv.Z = MathHelper.Clamp(hsv.Z - 0.5f, 0f, 1f);
		//			//}
		//			//Color foreground2 = ColorExtensions.HSVtoColor(hsv);
		//			Color foreground2 = foreground.Alpha(0.7f);

					
		//			Color red = new Color(170, 0, 0, 150);
		//			Color green = new Color(0, 170, 0, 150);
		//			Color blue = new Color(30, 30, 230, 150);
		//			Color gray = new Color(0, 0, 0, 150);
					
		//			//Main
		//			MySprite box = new MySprite(SpriteType.TEXTURE, "SquareSimple", position: sm.Center + sm.VW_VH(-22, -34), size: sm.VW_VH(40, 8));
		//			if (Cruise != null)
		//			{
		//				if (Cruise.Enabled)
		//				{
		//					box.Color = green;
		//				}
		//				else
		//				{
		//					box.Color = red;
		//				}

		//			}
		//			else
		//			{
		//				box.Color = gray;
		//			}
		//			frame.Add(box);
		//			//frame.Add(tri);

		//			box = new MySprite(SpriteType.TEXTURE, "SquareSimple", position: sm.Center + sm.VW_VH(22, -34), size: sm.VW_VH(40, 8));
		//			if (Align != null)
		//			{
		//				if (Align.Enabled)
		//				{
		//					box.Color = green;
		//				}
		//				else
		//				{
		//					box.Color = red;
		//				}

		//			}
		//			else
		//			{
		//				box.Color = gray;
		//			}
		//			frame.Add(box);

					
		//			MySprite tri = new MySprite(SpriteType.TEXTURE, "RightTriangle", position: sm.Center + sm.VW_VH(-41, -31.5f), size: sm.VW_VH(3, 3), color: background);
		//			frame.Add(tri);
		//			tri = new MySprite(SpriteType.TEXTURE, "RightTriangle", position: sm.Center + sm.VW_VH(3.5f, -31.5f), size: sm.VW_VH(3, 3), color: background);
		//			frame.Add(tri);

		//			tri = new MySprite(SpriteType.TEXTURE, "RightTriangle", position: sm.Center + sm.VW_VH(41, -36.5f), size: sm.VW_VH(3, 3), color: background, rotation: (float)Math.PI);
		//			frame.Add(tri);
		//			tri = new MySprite(SpriteType.TEXTURE, "RightTriangle", position: sm.Center + sm.VW_VH(-3.5f, -36.5f), size: sm.VW_VH(3, 3), color: background, rotation: (float)Math.PI);
		//			frame.Add(tri);


		//			//var sprites = new List<string>();
		//			//surface.GetSprites(sprites);
		//			//foreach (var item in sprites)
		//			//{
		//			//	Echo(item);
		//			//}

		//			// Speed
		//			box = new MySprite(SpriteType.TEXTURE, "SquareSimple", position: sm.Center + sm.VW_VH(-22, -4), size: sm.VW_VH(40, 8), color: blue);
		//			if (Cruise == null || !Cruise.Enabled) box.Color = gray;
		//			frame.Add(box);
					
					
		//			if (Cruise != null)
		//			{
		//				box = new MySprite(SpriteType.TEXTURE, "SquareSimple", position: sm.Center + sm.VW_VH(22, -4), size: sm.VW_VH(40, 8), color: blue);
		//				if (Cruise.Enabled)
		//				{
		//					Vector2 size = sm.VW_VH(40, 8);
		//					Vector2 pos = sm.Center + sm.VW_VH(22, -4);
		//					pos.X += size.X * 0.5f; //Left align
		//					size.X = size.X * (1 - percent);
		//					pos.X -= size.X * 0.5f; //Adjust for groth in both direction
		//					box.Size = size;
		//					box.Position = pos;
		//					frame.Add(box);

		//					box = new MySprite(SpriteType.TEXTURE, "SquareSimple", color: green);
		//					size = sm.VW_VH(40, 8);
		//					pos = sm.Center + sm.VW_VH(22, -4);
		//					pos.X -= size.X * 0.5f; //Left align
		//					size.X = size.X * percent;
		//					pos.X += size.X * 0.5f; //Adjust for groth in both direction
		//					box.Size = size;
		//					box.Position = pos;
		//					frame.Add(box);
		//				}
		//				else
		//				{
							
		//					frame.Add(box);
		//				}
		//			}
		//			else
		//			{
		//				box = new MySprite(SpriteType.TEXTURE, "SquareSimple", position: sm.Center + sm.VW_VH(22, -4), size: sm.VW_VH(40, 8), color: gray);
		//				frame.Add(box);
		//			}
						

		//			tri = new MySprite(SpriteType.TEXTURE, "RightTriangle", position: sm.Center + sm.VW_VH(-41, -1.5f), size: sm.VW_VH(3, 3), color: background);
		//			frame.Add(tri);
		//			tri = new MySprite(SpriteType.TEXTURE, "RightTriangle", position: sm.Center + sm.VW_VH(3.5f, -1.5f), size: sm.VW_VH(3, 3), color: background);
		//			frame.Add(tri);

		//			tri = new MySprite(SpriteType.TEXTURE, "RightTriangle", position: sm.Center + sm.VW_VH(41, -6.5f), size: sm.VW_VH(3, 3), color: background, rotation: (float)Math.PI);
		//			frame.Add(tri);
		//			tri = new MySprite(SpriteType.TEXTURE, "RightTriangle", position: sm.Center + sm.VW_VH(-3.5f, -6.5f), size: sm.VW_VH(3, 3), color: background, rotation: (float)Math.PI);
		//			frame.Add(tri);




		//			MySprite text = new MySprite(SpriteType.TEXT, "Ascent Cruise Control", position: sm.VCenterText(sm.Center + sm.VW_VH(0, -43), sm.Size.X * 0.003f), color: foreground, fontId: "Debug", rotation: sm.Size.X * 0.003f, alignment: TextAlignment.CENTER);
		//			frame.Add(text);

		//			text = new MySprite(SpriteType.TEXT, "Align", position: sm.VCenterText(sm.Center + sm.VW_VH(22, -34), sm.Size.X * 0.0025f), color: foreground2, fontId: "Debug", rotation: sm.Size.X * 0.0025f, alignment: TextAlignment.CENTER);
		//			frame.Add(text);

		//			text = new MySprite(SpriteType.TEXT, "Cruise", position: sm.VCenterText(sm.Center + sm.VW_VH(-22, -34), sm.Size.X * 0.0025f), color: foreground2, fontId: "Debug", rotation: sm.Size.X * 0.0025f, alignment: TextAlignment.CENTER);
		//			frame.Add(text);


		//			text = new MySprite(SpriteType.TEXT, "Speed", position: sm.VCenterText(sm.Center + sm.VW_VH(0, -23), sm.Size.X * 0.003f), color: foreground, fontId: "Debug", rotation: sm.Size.X * 0.003f, alignment: TextAlignment.CENTER);
		//			frame.Add(text);

		//			text = new MySprite(SpriteType.TEXT, "Target", position: sm.VCenterText(sm.Center + sm.VW_VH(22, -14), sm.Size.X * 0.0025f), color: foreground, fontId: "Debug", rotation: sm.Size.X * 0.0025f, alignment: TextAlignment.CENTER);
		//			frame.Add(text);

		//			text = new MySprite(SpriteType.TEXT, "Current", position: sm.VCenterText(sm.Center + sm.VW_VH(-22, -14), sm.Size.X * 0.0025f), color: foreground, fontId: "Debug", rotation: sm.Size.X * 0.0025f, alignment: TextAlignment.CENTER);
		//			frame.Add(text);

		//			if(Cruise != null)
		//			{
		//				//Target
		//				text = new MySprite(SpriteType.TEXT, (Cruise.Enabled ? Cruise.AdaptiveTargetSpeed : targetSpeed).ToString("n1") + " m/s", position: sm.VCenterText(sm.Center + sm.VW_VH(22, -4), sm.Size.X * 0.0025f), color: foreground2, fontId: "Debug", rotation: sm.Size.X * 0.0025f, alignment: TextAlignment.CENTER);
		//				frame.Add(text);

		//				//Current
		//				text = new MySprite(SpriteType.TEXT, (Cruise.Enabled ? Cruise.Speed.ToString("n1") : "--") + " m/s", position: sm.VCenterText(sm.Center + sm.VW_VH(-22, -4), sm.Size.X * 0.0025f), color: foreground2, fontId: "Debug", rotation: sm.Size.X * 0.0025f, alignment: TextAlignment.CENTER);
		//				frame.Add(text);
		//			}

		//		}
		//	}
		//}


		public static string AdjustTextToWidth(string text, int width)
		{
			string rest = text;
			string output = "";

			if (rest.Length > width)
			{
				while (rest.Length > width)
				{
					string part = rest.Substring(0, width);
					rest = rest.Substring(width);
					for (int i = part.Length - 1; i > 0; i--)
					{
						if (part[i] == ' ')
						{
							output += part.Substring(0, i) + "\n";
							rest = part.Substring(i + 1) + rest;
							break;
						}
					}
				}
			}
			output += rest;
			return output;
		}

		//to this comment.
		#endregion
		#region post-script
	}
}
#endregion