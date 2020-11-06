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
	class Aligner
	{
		public bool Enabled { get; private set; }

		IMyShipController controller;
		List<IMyGyro> gyros;
		//IMyTextSurface screen;

		bool _gyroOverride = false;
		bool GyroOverride
		{
			get
			{
				return _gyroOverride;
			}
			set
			{
				_gyroOverride = value;
				for (int i = 0; i < gyros.Count; i++)
				{
					gyros[i].GyroOverride = value;
				}
			}
		}

		bool startedInNaturalGravity = false;
		public bool DisableOnNaturalGravityExit { get; private set; } = true;

		public Aligner(IMyShipController controller, List<IMyGyro> gyros)
		{
			this.controller = controller;
			this.gyros = gyros;

			//screen = (controller as IMyTextSurfaceProvider).GetSurface(0);
			//screen.ContentType = ContentType.TEXT_AND_IMAGE;
			
			//GyroOverride = true;

		}

		//public void Update(ProjectorVisualization proj)
		public void Update()
		{
			if (!Enabled) return;

			Vector3D down = controller.GetNaturalGravity();
			Vector3D shipDown = controller.WorldMatrix.Down;

			if (Vector3D.IsZero(down))
			{
				//gravity is zero, means we're not in gravity well.
				if (DisableOnNaturalGravityExit && startedInNaturalGravity)
				{
					Stop();
					return;
				}
				else
				{
					//Align to velocity vector
					down = controller.GetShipVelocities().LinearVelocity;
				}
			}

			Vector3D locationInGrid = Vector3D.Transform(controller.GetPosition() + Vector3.Normalize(down), MatrixD.Invert(controller.WorldMatrix)) / controller.CubeGrid.GridSize;
			Vector3D locationInGrid2 = Vector3D.Transform(controller.GetPosition() + Vector3.Normalize(shipDown), MatrixD.Invert(controller.WorldMatrix)) / controller.CubeGrid.GridSize;

			//screen.WriteText(locationInGrid.ToString("n3"));
			//screen.WriteText("\n" + locationInGrid2.ToString("n3"), true);

			Vector3D striveForZero = locationInGrid - locationInGrid2;

			striveForZero *= 5; //Increase speed in aligning

			//screen.WriteText("\n" + striveForZero.ToString("n3"), true);
			//screen.WriteText("\n" + controller.RotationIndicator.Y.ToString("n3"), true);

			ApplyGyroOverride(-striveForZero.Z, controller.RotationIndicator.Y, -striveForZero.X, gyros, controller as IMyTerminalBlock);
			

			//proj.UpdatePosition(controller.GetPosition() + (Vector3.Normalize(controller.GetNaturalGravity()) * 10));
			//proj.UpdatePosition(controller.GetPosition() + (Vector3.Normalize(controller.WorldMatrix.Down) * 10));
		}

		//By Whiplash141
		void ApplyGyroOverride(double pitch_speed, double yaw_speed, double roll_speed, List<IMyGyro> gyro_list, IMyTerminalBlock reference)
		{
			var rotationVec = new Vector3D(-pitch_speed, yaw_speed, roll_speed); //because keen does some weird stuff with signs
			var shipMatrix = reference.WorldMatrix;
			var relativeRotationVec = Vector3D.TransformNormal(rotationVec, shipMatrix);
			foreach (var thisGyro in gyro_list)
			{
				var gyroMatrix = thisGyro.WorldMatrix;
				var transformedRotationVec = Vector3D.TransformNormal(relativeRotationVec, Matrix.Transpose(gyroMatrix));
				thisGyro.Pitch = (float)transformedRotationVec.X;
				thisGyro.Yaw = (float)transformedRotationVec.Y;
				thisGyro.Roll = (float)transformedRotationVec.Z;
			}
		}

		public void Start(bool disableOnNaturalGravityExit = true)
		{
			this.DisableOnNaturalGravityExit = disableOnNaturalGravityExit;
			startedInNaturalGravity = !Vector3D.IsZero(controller.GetNaturalGravity());
			Enabled = true;
			GyroOverride = true;
		}

		public void Stop()
		{
			Enabled = false;
			GyroOverride = false;
		}
	}
	#endregion
}
