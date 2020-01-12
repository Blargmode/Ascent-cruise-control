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
#endregion
namespace IngameScript
{
	#region in-game
	
	class CruiseControl
	{
		public bool Enabled { get; private set; }

		IMyShipController controller;
		IMyTextSurface screen;

		//These are sorted according to which way they push.
		Dictionary<Base6Directions.Direction, List<IMyThrust>> electricThrusters;
		Dictionary<Base6Directions.Direction, List<IMyThrust>> hydrogenThrusters;

		//Set these to decide what thrusters to use.
		public Base6Directions.Direction Forward
		{
			get
			{
				return forward;
			}
			private set
			{
				forward = value;
				reverse = Base6Directions.GetOppositeDirection(value);
			}
		}
		Base6Directions.Direction forward = Base6Directions.Direction.Up; 
		Base6Directions.Direction reverse = Base6Directions.Direction.Down;
		Base6Directions.Direction autoForward; 

		public double TargetSpeed { get; private set; }
		public double AdaptiveTargetSpeed { get; private set; }
		public double Speed { get; private set; }
		bool descending = false;
		float cutoff = 0.05f;
		double deceleration = 5; //5

		public float ThrustOverrideElectric { get; private set; } //For display purposes
		public float ThrustOverrideHydrogen { get; private set; } //For display purposes

		//double targetAltAcending = double.MaxValue;
		//double targetAltDescending;
		public double TargetAltAcending { get; private set; } = double.MaxValue;
		public double TargetAltDescending { get; private set; }
		MyPlanetElevation elevationType;
		bool startedInNaturalGravity = false;
		public bool DisableOnNaturalGravityExit { get; private set; } = true;

		public CruiseControl(IMyShipController controller, List<IMyThrust> allThrusters)
		{
			this.controller = controller;

			screen = (controller as IMyTextSurfaceProvider).GetSurface(0);
			screen.ContentType = ContentType.TEXT_AND_IMAGE;

			electricThrusters = new Dictionary<Base6Directions.Direction, List<IMyThrust>>()
			{
				{Base6Directions.Direction.Forward, new List<IMyThrust>() },
				{Base6Directions.Direction.Backward, new List<IMyThrust>() },
				{Base6Directions.Direction.Up, new List<IMyThrust>() },
				{Base6Directions.Direction.Down, new List<IMyThrust>() },
			};

			hydrogenThrusters = new Dictionary<Base6Directions.Direction, List<IMyThrust>>()
			{
				{Base6Directions.Direction.Forward, new List<IMyThrust>() },
				{Base6Directions.Direction.Backward, new List<IMyThrust>() },
				{Base6Directions.Direction.Up, new List<IMyThrust>() },
				{Base6Directions.Direction.Down, new List<IMyThrust>() },
			};

			var forward = controller.Orientation.Forward;
			var back = Base6Directions.GetOppositeDirection(forward);
			var up = controller.Orientation.Up;
			var down = Base6Directions.GetOppositeDirection(up);

			//For automatic thruster selection.
			var maxThrust = new Dictionary<Base6Directions.Direction, float>()
			{
				{ Base6Directions.Direction.Forward, 0f },
				{ Base6Directions.Direction.Backward, 0f },
				{ Base6Directions.Direction.Up, 0f },
				{ Base6Directions.Direction.Down, 0f }
			};

			
			//These are all inverted to make the Dictuionary reflect which way they push, not which way they point.
			foreach (var item in allThrusters)
			{
				if (item.Orientation.Forward == forward)
				{
					if(item.BlockDefinition.SubtypeId.Contains("Hydrogen")) hydrogenThrusters[Base6Directions.Direction.Backward].Add(item);
					else electricThrusters[Base6Directions.Direction.Backward].Add(item);
					maxThrust[Base6Directions.Direction.Backward] += item.MaxThrust;
				}
				else if (item.Orientation.Forward == back)
				{
					if (item.BlockDefinition.SubtypeId.Contains("Hydrogen")) hydrogenThrusters[Base6Directions.Direction.Forward].Add(item);
					else electricThrusters[Base6Directions.Direction.Forward].Add(item);
					maxThrust[Base6Directions.Direction.Forward] += item.MaxThrust;
				}
				if (item.Orientation.Forward == up)
				{
					if (item.BlockDefinition.SubtypeId.Contains("Hydrogen")) hydrogenThrusters[Base6Directions.Direction.Down].Add(item);
					else electricThrusters[Base6Directions.Direction.Down].Add(item);
					maxThrust[Base6Directions.Direction.Down] += item.MaxThrust;
				}
				else if (item.Orientation.Forward == down)
				{
					if (item.BlockDefinition.SubtypeId.Contains("Hydrogen")) hydrogenThrusters[Base6Directions.Direction.Up].Add(item);
					else electricThrusters[Base6Directions.Direction.Up].Add(item);
					maxThrust[Base6Directions.Direction.Up] += item.MaxThrust;
				}
			}
			
			Forward = maxThrust.Aggregate((left, right) => left.Value > right.Value ? left : right).Key;
			autoForward = Forward;
		}

		public void Update(double deltaTime)
		{
			if (!Enabled) return;

			//Reduces the target speed in relation to altitude.
			double height;
			double newTarget;
			if (descending)
			{
				if (controller.TryGetPlanetElevation(MyPlanetElevation.Surface, out height))
				{
					if (height > TargetAltDescending)
					{
						newTarget = -((height - TargetAltDescending) / deceleration);
					}
					else
					{
						newTarget = ((TargetAltDescending - height) / deceleration);
					}
					if (newTarget < TargetSpeed) AdaptiveTargetSpeed = TargetSpeed;
					else AdaptiveTargetSpeed = newTarget;
					screen.WriteText("\nGnd alt: " + height.ToString("n3"), true);
				}
			}
			else
			{
				if (controller.TryGetPlanetElevation(elevationType, out height))
				{
					if (height > TargetAltAcending)
					{
						newTarget = -((height - TargetAltAcending) / deceleration);
					}
					else
					{
						newTarget = ((TargetAltAcending - height) / deceleration);
					}
					if (newTarget > TargetSpeed) AdaptiveTargetSpeed = TargetSpeed;
					else AdaptiveTargetSpeed = newTarget;
					screen.WriteText("\nSea alt: " + height.ToString("n3"), true);
				}
				else
				{
					//Failed at getting planet elevation, means we're not in gravity well.
					if (DisableOnNaturalGravityExit && startedInNaturalGravity)
					{
						Stop();
						return;
					}
				}
			}



			//Mass/Gravity
			double mass = controller.CalculateShipMass().PhysicalMass;
			double gravity = Vector3D.Dot(controller.GetNaturalGravity(), controller.WorldMatrix.GetOrientation().Down); //TODO: Is this supposed to be down or dependent on selected thrusters?
			//mass *= 9.80665f; //Convert to newton.
			double weight = mass * gravity;

			//Speed
			var vel = controller.GetShipVelocities().LinearVelocity;
			if (forward == Base6Directions.Direction.Up)
			{
				Speed = (Vector3D.TransformNormal(vel, MatrixD.Transpose(controller.WorldMatrix))).Y;
			}
			else if(forward == Base6Directions.Direction.Down)
			{
				Speed = -(Vector3D.TransformNormal(vel, MatrixD.Transpose(controller.WorldMatrix))).Y;
			}
			else if (forward == Base6Directions.Direction.Forward)
			{
				Speed = -(Vector3D.TransformNormal(vel, MatrixD.Transpose(controller.WorldMatrix))).Z;
			}
			else if (forward == Base6Directions.Direction.Backward)
			{
				Speed = (Vector3D.TransformNormal(vel, MatrixD.Transpose(controller.WorldMatrix))).Z;
			}


			double difference = AdaptiveTargetSpeed - Speed;
			double errorMagnitude = MathHelper.Clamp(difference / 5, -1, 1); //More than 5 away from target = full thrust.


			float electricThrust = GetElectricThrust(forward);
			float electricThrustRev = GetElectricThrust(reverse);

			float hydroThrust = GetHydrogenThrust(forward);
			float hydroThrustRev = GetHydrogenThrust(reverse);




			//TODO: Calulate thrust needed, tanking speed into account. MAss isnt just kg * gravity when hurling towards earth. Need how many newtons it takes to reach a specific speed, 0 in this case.
			//That should work for liftoff as well.

			//a = (v1 - v0) / t
			//F = m * a

			//Time is 1.
			//F = m * (v1 - v0)

			//double thrustNeeded = mass * difference;
			//This is not more stable than what I did before.

			
			screen.WriteText("\nTS: " + AdaptiveTargetSpeed.ToString("n3") + ", S: " + Speed.ToString("n3"), true);
			screen.WriteText("\nerr " + errorMagnitude.ToString("n3"), true);
			//screen.WriteText("\nneed " + thrustNeeded.ToString("n3"), true);
			screen.WriteText("\nthr " + (electricThrust + hydroThrust).ToString("n3"), true);

			//Dividing by zero will happen here, but since it's float it will result in infinity instead of exception, which is fine.
			float thrustOverride = MathHelper.Clamp((float)weight / electricThrust, 0, 1); //Convert to percent 
			float hydroThrustOverride = MathHelper.Clamp(((float)weight - electricThrust) / hydroThrust, 0, 1);

			float thrustOverrideRev = 0;
			float hydroThrustOverrideRev = 0;

			float thrustExcess = 0;
			float hydroThrustExcess = 0;

			thrustExcess = 1 - thrustOverride;
			hydroThrustExcess = 1 - hydroThrustOverride;

			if (Speed < AdaptiveTargetSpeed)
			{
				thrustOverride += thrustExcess * (float)errorMagnitude;
				hydroThrustOverride += hydroThrustExcess * (float)errorMagnitude;
			}
			else
			{
				//thrustOverride *= 1 - (float)errorMagnitude;
				//hydroThrustOverride *= 1 - (float)errorMagnitude;
				thrustOverride *= 1 + (float)errorMagnitude;
				hydroThrustOverride *= 1 + (float)errorMagnitude;
			}

			

			thrustOverrideRev = -MathHelper.Clamp((electricThrustRev * (float)errorMagnitude) / electricThrustRev, -1, 0);
			hydroThrustOverrideRev = -MathHelper.Clamp((hydroThrustRev * (float)errorMagnitude) / hydroThrustRev, -1, 0);
			
			//Not using h2 thruster if setting says so
			//if (electricThrust > mass) hydroThrustOverride = 0.000001f;




			//DEBUGLCD?.WritePublicText($"mass kg {mass2}\nmass n {mass.ToString("n3")}\ne thrust {thrust.ToString("n3")}\nh thrust {hydroThrust.ToString("n3")}\ne override {thrustOverride.ToString("n3")}\nh override {hydroThrustOverride.ToString("n3")}\ne excess {thrustExcess.ToString("n3")}\nh excess {hydroThrustExcess.ToString("n3")}\ndiff {difference.ToString("n3")}\nerr {errorMagnitude.ToString("n3")}\ngrav {gravity.ToString("n3")}\nelevation {elevation.ToString("n3")}");

			//Prevent setting thrust override to 0, as that turns it off.
			if (thrustOverride <= 0 || double.IsNaN(thrustOverride)) thrustOverride = 0.000001f;
			if (hydroThrustOverride <= 0 || double.IsNaN(hydroThrustOverride)) hydroThrustOverride = 0.000001f;

			if (thrustOverrideRev <= 0 || double.IsNaN(thrustOverrideRev)) thrustOverrideRev = 0.000001f;
			if (hydroThrustOverrideRev <= 0 || double.IsNaN(hydroThrustOverrideRev)) hydroThrustOverrideRev = 0.000001f;

			ThrustOverrideElectric = 0;

			for (int i = 0; i < electricThrusters[forward].Count; i++)
			{
				if (electricThrusters[forward][i].MaxEffectiveThrust / electricThrusters[forward][i].MaxThrust <= cutoff) electricThrusters[forward][i].ThrustOverridePercentage = 0.000001f; //If max effective trhust is less than 5%, don't use the thruster
				else
				{
					electricThrusters[forward][i].ThrustOverridePercentage = thrustOverride;
					ThrustOverrideElectric += thrustOverride;
				}
			}

			for (int i = 0; i < hydrogenThrusters[forward].Count; i++)
			{
				hydrogenThrusters[forward][i].ThrustOverridePercentage = hydroThrustOverride;
			}

			screen.WriteText("\novr-rev " + thrustOverrideRev.ToString("n3"), true);
			screen.WriteText("\novr-hrev " + hydroThrustOverrideRev.ToString("n3"), true);

			for (int i = 0; i < electricThrusters[reverse].Count; i++)
			{
				if (electricThrusters[reverse][i].MaxEffectiveThrust / electricThrusters[reverse][i].MaxThrust <= cutoff) electricThrusters[reverse][i].ThrustOverridePercentage = 0.000001f; //If max effective trhust is less than 5%, don't use the thruster
				else
				{
					electricThrusters[reverse][i].ThrustOverridePercentage = thrustOverrideRev;
					ThrustOverrideElectric += thrustOverrideRev;
				}
			}

			for (int i = 0; i < hydrogenThrusters[reverse].Count; i++)
			{
				hydrogenThrusters[reverse][i].ThrustOverridePercentage = hydroThrustOverrideRev;
			}

			ThrustOverrideElectric = ThrustOverrideElectric / (electricThrusters[forward].Count + electricThrusters[reverse].Count);
			ThrustOverrideHydrogen = hydroThrustOverride + hydroThrustOverrideRev;

			//screen.WriteText("\nTS: " + AdaptiveTargetSpeed.ToString("n3") + ", S: " + speed.ToString("n3"), true);
			//screen.WriteText("\nE: " + electricThrusters[forward].Count + ", ovr " + thrustOverride.ToString("n3") + ", thr " + electricThrust.ToString("n3"), true);
			//screen.WriteText("\nH " + hydrogenThrusters[forward].Count + ", ovr " + hydroThrustOverride.ToString("n3") + ", thr " + hydroThrust.ToString("n3"), true);
			//screen.WriteText("\nerr " + errorMagnitude.ToString("n3"), true);

		}

		float GetElectricThrust(Base6Directions.Direction direction)
		{
			float electricThrust = 0;
			for (int i = 0; i < electricThrusters[direction].Count; i++)
			{
				if (electricThrusters[direction][i].MaxEffectiveThrust / electricThrusters[direction][i].MaxThrust > cutoff)
				{
					electricThrust += electricThrusters[direction][i].MaxEffectiveThrust;
				}
			}
			return electricThrust;
		}

		float GetHydrogenThrust(Base6Directions.Direction direction)
		{
			float hydroThrust = 0;
			for (int i = 0; i < hydrogenThrusters[direction].Count; i++)
			{
				hydroThrust += hydrogenThrusters[direction][i].MaxEffectiveThrust;
			}
			return hydroThrust;
		}


		public void UpdateCopy()
		{
			if (!Enabled) return;

			//Reduces the target speed in relation to altitude.
			double height;
			double newTarget;
			if (descending)
			{
				if (controller.TryGetPlanetElevation(MyPlanetElevation.Sealevel, out height))
				{
					if (height > TargetAltDescending)
					{
						newTarget = -((height - TargetAltDescending) / deceleration);
					}
					else
					{
						newTarget = ((TargetAltDescending - height) / deceleration);
					}
					if (newTarget < TargetSpeed) AdaptiveTargetSpeed = TargetSpeed;
					else AdaptiveTargetSpeed = newTarget;
				}
			}
			else
			{
				if (controller.TryGetPlanetElevation(elevationType, out height))
				{ 
					if (height > TargetAltAcending)
					{
						newTarget = -((height - TargetAltAcending) / deceleration);
					}
					else
					{
						newTarget = ((TargetAltAcending - height) / deceleration);
					}
					if (newTarget > TargetSpeed) AdaptiveTargetSpeed = TargetSpeed;
					else AdaptiveTargetSpeed = newTarget;
					screen.WriteText("\nSea alt: " + height.ToString("n3"), true);
				}
				else
				{
					//Failed at getting planet elevation, means we're not in gravity well.
					if (DisableOnNaturalGravityExit && startedInNaturalGravity)
					{
						Stop();
						return;
					}
				}
			}
			


			//Mass/Gravity
			double mass = controller.CalculateShipMass().PhysicalMass;
			double gravity = Vector3D.Dot(controller.GetNaturalGravity(), controller.WorldMatrix.GetOrientation().Down);

			//Speed
			var vel = controller.GetShipVelocities().LinearVelocity;
			double speed = 0;
			if(forward == Base6Directions.Direction.Up)
			{
				speed = (Vector3D.TransformNormal(vel, MatrixD.Transpose(controller.WorldMatrix))).Y;
			}
			else
			{
				speed = -(Vector3D.TransformNormal(vel, MatrixD.Transpose(controller.WorldMatrix))).Z;
			}
			double difference = AdaptiveTargetSpeed - speed;
			double errorMagnitude = MathHelper.Clamp(Math.Abs(difference / 5), 0, 1); //More than 5 away from target = full thrust.

			
			float electricThrust = 0;
			for (int i = 0; i < electricThrusters[forward].Count; i++)
			{
				if (electricThrusters[forward][i].MaxEffectiveThrust / electricThrusters[forward][i].MaxThrust > cutoff)
				{
					electricThrust += electricThrusters[forward][i].MaxEffectiveThrust;
				}
			}
			
			float hydroThrust = 0;
			for (int i = 0; i < hydrogenThrusters[forward].Count; i++)
			{
				hydroThrust += hydrogenThrusters[forward][i].MaxEffectiveThrust;
			}

			//mass *= 9.80665f; //Convert to newton.
			mass *= gravity;


			//TODO: Calulate thrust needed, tanking speed into account. MAss isnt just kg * gravity when hurling towards earth. Need how many newtons it takes to reach a specific speed, 0 in this case.
			//That should work for liftoff as well.


			//Dividing by zero will happen here, but since it's float it will result in infinity instead of exception, which is fine.
			float thrustOverride = MathHelper.Clamp((float)mass / electricThrust, 0, 1); //Convert to percent 
			float hydroThrustOverride = MathHelper.Clamp(((float)mass - electricThrust) / hydroThrust, 0, 1);
			
			float thrustExcess = 0;
			float hydroThrustExcess = 0;

			thrustExcess = 1 - thrustOverride;
			hydroThrustExcess = 1 - hydroThrustOverride;
			

			if (AdaptiveTargetSpeed > speed)
			{
				thrustOverride += thrustExcess * (float)errorMagnitude;
				hydroThrustOverride += hydroThrustExcess * (float)errorMagnitude;
			}
			else
			{
				thrustOverride *= 1 - (float)errorMagnitude;
				hydroThrustOverride *= 1 - (float)errorMagnitude;
			}

			//Not using h2 thruster if setting says so
			//if (electricThrust > mass) hydroThrustOverride = 0.000001f;


			//DEBUGLCD?.WritePublicText($"mass kg {mass2}\nmass n {mass.ToString("n3")}\ne thrust {thrust.ToString("n3")}\nh thrust {hydroThrust.ToString("n3")}\ne override {thrustOverride.ToString("n3")}\nh override {hydroThrustOverride.ToString("n3")}\ne excess {thrustExcess.ToString("n3")}\nh excess {hydroThrustExcess.ToString("n3")}\ndiff {difference.ToString("n3")}\nerr {errorMagnitude.ToString("n3")}\ngrav {gravity.ToString("n3")}\nelevation {elevation.ToString("n3")}");

			//Prevent setting thrust override to 0, as that turns it off.
			if (thrustOverride <= 0 || double.IsNaN(thrustOverride)) thrustOverride = 0.000001f;
			if (hydroThrustOverride <= 0 || double.IsNaN(thrustOverride)) hydroThrustOverride = 0.000001f;

			for (int i = 0; i < electricThrusters[forward].Count; i++)
			{
				if (electricThrusters[forward][i].MaxEffectiveThrust / electricThrusters[forward][i].MaxThrust <= cutoff) electricThrusters[forward][i].ThrustOverridePercentage = 0.000001f; //If max effective trhust is less than 5%, don't use the thruster
				else electricThrusters[forward][i].ThrustOverridePercentage = thrustOverride;
			}

			for (int i = 0; i < hydrogenThrusters[forward].Count; i++)
			{
				hydrogenThrusters[forward][i].ThrustOverridePercentage = hydroThrustOverride;
			}

			screen.WriteText("\nTS: " + AdaptiveTargetSpeed.ToString("n3") + ", S: " + speed.ToString("n3"), true);
			screen.WriteText("\nE: " + electricThrusters[forward].Count + ", ovr " + thrustOverride.ToString("p3") + ", thr " + electricThrust.ToString("n3"), true);
			screen.WriteText("\nH " + hydrogenThrusters[forward].Count + ", ovr " + hydroThrustOverride.ToString("p3") + ", thr " + hydroThrust.ToString("n3"), true);
			
		}

		public void Start(float targetSpeed, Base6Directions.Direction forward, double targetAltDescending = 0, double targetAltAcending = double.MaxValue, bool useSealevel = false, bool disableOnNaturalGravityExit = true)
		{
			Enabled = true;
			TargetSpeed = targetSpeed;
			AdaptiveTargetSpeed = targetSpeed;
			if (forward == Base6Directions.Direction.Left) Forward = autoForward;
			else Forward = forward;
			//EnableThrusters(reverse, false);
			if (targetSpeed < 0) descending = true;
			else descending = false;
			if (useSealevel) elevationType = MyPlanetElevation.Sealevel;
			else elevationType = MyPlanetElevation.Surface;
			TargetAltAcending = targetAltAcending;
			TargetAltDescending = targetAltDescending;
			DisableOnNaturalGravityExit = disableOnNaturalGravityExit;
			startedInNaturalGravity = controller.GetNaturalGravity().Length() > 0;
		}

		public void Stop()
		{
			Enabled = false;
			EnableThrusters(reverse, true, true);
			EnableThrusters(forward, true, true);
		}

		void EnableThrusters(Base6Directions.Direction direction, bool enable, bool disableThrustOverride = false)
		{
			for (int i = 0; i < electricThrusters[direction].Count; i++)
			{
				electricThrusters[direction][i].Enabled = enable;
				if (disableThrustOverride) electricThrusters[direction][i].ThrustOverride = 0;
			}
			for (int i = 0; i < hydrogenThrusters[direction].Count; i++)
			{
				hydrogenThrusters[direction][i].Enabled = enable;
				if (disableThrustOverride) hydrogenThrusters[direction][i].ThrustOverride = 0;
			}
		}

		void SetThrustOverride(Base6Directions.Direction direction, float amount)
		{
			for (int i = 0; i < electricThrusters[direction].Count; i++)
			{
				electricThrusters[direction][i].ThrustOverride = amount;
			}
			for (int i = 0; i < hydrogenThrusters[direction].Count; i++)
			{
				hydrogenThrusters[direction][i].ThrustOverride = amount;
			}
		}

	}
	#endregion
}
