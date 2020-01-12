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
	struct ScreenData
	{
		public double maxAlt;
		public double minAlt;
		public double speed;
		public double maxSpeed;
		public double targetSpeed;
		public float thrustOverrideH2;
		public float thrustOverridePW;
		public bool cruiseEnabled;
		public bool alignEnabled;
	}

	class Screens
	{
		List<IMyTextSurface> surfaces;

		public Screens(List<IMyTextSurface> surfaces)
		{
			this.surfaces = surfaces;
		}

		public void Update(ScreenData data)
		{
			foreach (var surface in surfaces)
			{
				if(surface.SurfaceSize.Y / surface.SurfaceSize.X >= 0.5f)
				{
					DrawRegular(surface, data);
				}
				else
				{
					DrawSlim(surface);
				}
			}
		}

		void DrawRegular(IMyTextSurface surface, ScreenData data)
		{
			Color background = surface.ScriptBackgroundColor;
			Color foreground = surface.ScriptForegroundColor;
			Color white = new Color(230, 230, 230);
			//Color gray0 = new Color(200, 200, 200);
			Color gray1 = new Color(130, 130, 130);
			Color gray2 = new Color(15, 15, 15);
			Color darken = new Color(50, 50, 50, 200);
			Color darken2 = new Color(0, 0, 0, 150);

			white = foreground;
			var hsv = ColorExtensions.ColorToHSV(foreground);
			hsv.Y *= 0.6f;
			hsv.Z *= 0.6f;
			gray1 = ColorExtensions.HSVtoColor(hsv);

			//Canvas canvas = new Canvas((surface.TextureSize - surface.TextureSize * 1f) * 0.5f, surface.TextureSize * 1f);
			Canvas canvas = new Canvas((surface.TextureSize - Vec(surface.SurfaceSize.Y * 1f)) * 0.5f, Vec(surface.SurfaceSize.Y * 1f));

			//if (background.ColorToHSV().Z < 0.01)
			//{
			//	white = new Color(230, 230, 230);
			//	darken = new Color(200, 200, 200, 10);
			//	darken2 = new Color(200, 200, 200, 20);
			//	gray0 = new Color(50, 50, 50);
			//	gray1 = new Color(25, 25, 25);
			//}
			
			using (var frame = surface.DrawFrame())
			{
				//Inner circle
				frame.Add(canvas.Circle(Vec(0, 0), Vec(0.95f), darken));
				frame.Add(canvas.Rect(Vec(0, -0.136f), Vec(0.95f, 0.9f), background));
				frame.Add(canvas.CircleHollow(Vec(0, 0), Vec(1f), gray2));
				frame.Add(canvas.CircleHollow(Vec(0, 0), Vec(0.95f), gray1));



				//Right progress bg
				frame.Add(canvas.Rect(Vec(0.3f, 0), Vec(0.055f, 0.3f), darken));
				frame.Add(canvas.Progress(Vec(0.3f, 0), Vec(0.3f, 0.055f), Color.Orange, data.thrustOverrideH2, rotation: 4.71239f));
				frame.Add(canvas.TriangleRight(Vec(0.3f, -0.13f), Vec(0.06f, 0.04f), background, rotation: MathHelper.ToRadians(180)));
				frame.Add(canvas.TriangleRight(Vec(0.3f, 0.13f), Vec(0.04f, 0.06f), background, rotation: MathHelper.ToRadians(-90)));
				frame.Add(canvas.Rect(Vec(0.3f, 0), Vec(0.06f, 0.008f), background));
				


				//Left progress bg
				frame.Add(canvas.Rect(Vec(-0.3f, 0), Vec(0.055f, 0.3f), darken));
				frame.Add(canvas.Progress(Vec(-0.3f, 0), Vec(0.3f, 0.055f), Color.Blue, data.thrustOverridePW, rotation: 4.71239f));
				frame.Add(canvas.TriangleRight(Vec(-0.3f, -0.13f), Vec(0.04f, 0.06f), background, rotation: MathHelper.ToRadians(90)));
				frame.Add(canvas.TriangleRight(Vec(-0.3f, 0.13f), Vec(0.06f, 0.04f), background));
				frame.Add(canvas.Rect(Vec(0-.3f, 0), Vec(0.06f, 0.008f), background));
				

				//Icons
				frame.Add(canvas.Sprite("IconHydrogen", Vec(0.3f, -0.2f), Vec(0.08f, 0.08f), white, Anchor.Center));
				frame.Add(canvas.Sprite("IconEnergy", Vec(-0.3f, -0.2f), Vec(0.1f, 0.1f), white, Anchor.Center));

				//Thrust text
				frame.Add(canvas.Text((data.cruiseEnabled ? string.Format("{0:0%}", data.thrustOverrideH2) : ""), Vec(0.30f, 0.185f), 0.08f, white));
				frame.Add(canvas.Text((data.cruiseEnabled ? string.Format("{0:0%}", data.thrustOverridePW) : ""), Vec(-0.30f, 0.185f), 0.08f, white));



				//Speedometer
				double speedAngle = ((Math.Abs(data.targetSpeed) / data.maxSpeed) * 4.71238898) + 0.785398163;
				frame.Add(canvas.Triangle(Vec(0, 0), Vec(0.04f), gray2, rotation: (float)speedAngle, offset: Vec(0, 0.46f)));

				speedAngle = ((Math.Abs(data.speed) / data.maxSpeed) * 4.71238898) + 0.785398163; //percent * 270 degrees + 45 degrees offset.
				frame.Add(canvas.Rect(Vec(0, 0), Vec(0.01f, 0.1f), white, rotation: (float) speedAngle, offset: Vec(0, 0.435f)));

				

				//Speedometer ticks
				frame.Add(canvas.Triangle(Vec(0, 0), Vec(0.01f, 0.06f), white, rotation: (float)Math.PI, offset: Vec(0, 0.468f)));
				frame.Add(canvas.Triangle(Vec(0, 0), Vec(0.01f, 0.06f), white, rotation: (float)Math.PI * 0.625f, offset: Vec(0, 0.468f)));
				frame.Add(canvas.Triangle(Vec(0, 0), Vec(0.01f, 0.06f), white, rotation: (float)Math.PI * 0.25f, offset: Vec(0, 0.468f)));

				frame.Add(canvas.Triangle(Vec(0, 0), Vec(0.01f, 0.06f), white, rotation: (float)Math.PI * -0.625f, offset: Vec(0, 0.468f)));
				frame.Add(canvas.Triangle(Vec(0, -0), Vec(0.01f, 0.06f), white, rotation: (float)Math.PI * -0.25f, offset: Vec(0, 0.468f)));



				frame.Add(canvas.Text(data.speed.ToString("n0"), Vec(0, -0.20f), 0.16f, white));
				frame.Add(canvas.Text($"[{data.targetSpeed.ToString("n0")}]", Vec(0, -0.09f), 0.14f, white));
				frame.Add(canvas.Text("m/s", Vec(0, 0), 0.1f, gray1));

				//frame.Add(canvas.Rect(Vec(-0.15f, 0.20f), Vec(0.01f, 0.16f), darken2));
				//frame.Add(canvas.Rect(Vec(0.15f, 0.20f), Vec(0.01f, 0.16f), darken2));
				

				frame.Add(canvas.Text("Cruise", Vec(0, 0.15f), 0.1f, gray1));
				frame.Add(canvas.Text("Align", Vec(0, 0.25f), 0.1f, gray1));

				if (data.cruiseEnabled)
				{
					frame.Add(canvas.Rect(Vec(-0.15f, 0.15f), Vec(0.02f, 0.06f), Color.Green));
				}
				else
				{
					frame.Add(canvas.Rect(Vec(-0.15f, 0.15f), Vec(0.02f, 0.06f), Color.Red));
				}

				if (data.alignEnabled)
				{
					frame.Add(canvas.Rect(Vec(-0.15f, 0.25f), Vec(0.02f, 0.06f), Color.Green));
				}
				else
				{
					frame.Add(canvas.Rect(Vec(-0.15f, 0.25f), Vec(0.02f, 0.06f), Color.Red));
				}
				
				//Create angle on indicators
				frame.Add(canvas.Triangle(Vec(-0.13f, 0.10f), Vec(0.08f), background, rotation: 1.57079633f));
				frame.Add(canvas.Triangle(Vec(-0.13f, 0.20f), Vec(0.08f), background, rotation: 1.57079633f));
				frame.Add(canvas.TriangleRight(Vec(-0.13f, 0.284f), Vec(0.08f, 0.04f), background));

				

				//Arrow max
				//frame.Add(canvas.Rect(Vec(0, -0.392f), Vec(0.02f), gray1, Anchor.Center));
				frame.Add(canvas.Triangle(Vec(0, -0.41f), Vec(0.04f, 0.06f), gray1, Anchor.Center));
				frame.Add(canvas.Rect(Vec(0, -0.426f), Vec(0.04f, 0.01f), gray1, Anchor.Center));

				//Arrow min
				//frame.Add(canvas.Rect(Vec(0, 0.392f), Vec(0.02f), gray1, Anchor.Center)); 
				frame.Add(canvas.Triangle(Vec(0, 0.41f), Vec(0.04f, 0.06f), gray1, Anchor.Center, rotation: (float)Math.PI));
				frame.Add(canvas.Rect(Vec(0, 0.426f), Vec(0.04f, 0.01f), gray1, Anchor.Center));

				//Max alt text
				if(data.maxAlt == double.MaxValue)
				{
					frame.Add(canvas.Text("--", Vec(0, -0.35f), 0.07f, gray1));
				}
				else
				{
					frame.Add(canvas.Text(data.maxAlt.ToString("n0") + "m", Vec(0, -0.35f), 0.07f, gray1));
				}
				frame.Add(canvas.Text(data.minAlt.ToString("n1") + "m", Vec(0, 0.35f), 0.07f, gray1));
			}
		}

		void DrawRegular_Copy(IMyTextSurface surface)
		{
			Color background = surface.ScriptBackgroundColor;
			//Color background = Color.Red;
			Color foreground = surface.ScriptForegroundColor;
			Color white = new Color(230, 230, 230);
			Color gray1 = new Color(50, 50, 50);
			Color gray2 = new Color(25, 25, 25);
			Color darken = new Color(0, 0, 0, 100);
			Color darken2 = new Color(0, 0, 0, 150);
			//Canvas canvas = new Canvas((surface.TextureSize - surface.TextureSize * 0.8f) * 0.5f, surface.TextureSize * 0.8f);
			Canvas canvas = new Canvas(Vec(0, surface.TextureSize.Y - surface.SurfaceSize.Y), Vec(surface.SurfaceSize.Y));

			if (background.ColorToHSV().Z < 0.4)
			{
				white = new Color(230, 230, 230);
				darken = new Color(255, 255, 255, 5);
				darken2 = new Color(255, 255, 255, 15);
			}

			using (var frame = surface.DrawFrame())
			{


				//Right progress bg
				frame.Add(canvas.Rect(Vec(0.325f, 0f), Vec(0.3f, 0.38f), darken));
				//frame.Add(canvas.Progress(Vec(0.325f, 0f), Vec(0.38f, 0.3f), Color.Orange, 0.75f, rotation: 4.71239f));
				frame.Add(canvas.Rect(Vec(0.325f, 0.19f), Vec(0.3f, 0.01f), Color.Orange, offset: Vec(0, -(0.38f * 0.25f))));

				//Left progress bg
				frame.Add(canvas.Rect(Vec(-0.325f, 0f), Vec(0.3f, 0.38f), darken, Anchor.Center));
				//frame.Add(canvas.Progress(Vec(-0.325f, 0f), Vec(0.38f, 0.3f), Color.Blue, 0.75f, rotation: 4.71239f));
				frame.Add(canvas.Rect(Vec(-0.325f, 0.19f), Vec(0.3f, 0.01f), Color.Blue, offset: Vec(0, -(0.38f * 0.71f))));

				//Thrust background
				frame.Add(canvas.Rect(Vec(0, 0.2325f), Vec(0.86f, 0.085f), darken2));

				//Outer circle
				frame.Add(canvas.CircleHollow(Vector2.Zero, Vector2.One, gray1));
				frame.Add(canvas.CircleHollow(Vector2.Zero, Vector2.One * 1.03f, gray2));

				//Cover top
				frame.Add(canvas.Rect(Vec(0, -0.19f - 0.25f), Vec(1.1f, 0.5f), background));



				//Inner circle
				frame.Add(canvas.Circle(Vec(0, -0.05f), Vec(0.62f), background));
				frame.Add(canvas.CircleHollow(Vec(0, -0.05f), Vec(0.62f), gray1));
				frame.Add(canvas.CircleHollow(Vec(0, -0.05f), Vec(0.65f), gray2));

				//Cover Bottom
				frame.Add(canvas.Rect(Vec(0, 0.525f), Vec(1.1f, 0.5f), background));

				frame.Add(canvas.Triangle(Vec(0, -0.05f), Vec(0.01f, 0.06f), gray1, rotation: (float)Math.PI, offset: Vec(0, 0.26f)));
				frame.Add(canvas.Triangle(Vec(0, -0.05f), Vec(0.01f, 0.06f), gray1, rotation: (float)Math.PI * 0.625f, offset: Vec(0, 0.26f)));
				frame.Add(canvas.Triangle(Vec(0, -0.05f), Vec(0.01f, 0.06f), gray1, rotation: (float)Math.PI * 0.25f, offset: Vec(0, 0.26f)));

				frame.Add(canvas.Triangle(Vec(0, -0.05f), Vec(0.01f, 0.06f), gray1, rotation: (float)Math.PI * -0.625f, offset: Vec(0, 0.26f)));
				frame.Add(canvas.Triangle(Vec(0, -0.05f), Vec(0.01f, 0.06f), gray1, rotation: (float)Math.PI * -0.25f, offset: Vec(0, 0.26f)));

				frame.Add(canvas.Rect(Vec(0, -0.05f), Vec(0.01f, 0.1f), white, rotation: (float)Math.PI * 0.785f, offset: Vec(0, 0.23f)));

				//Icons
				frame.Add(canvas.Sprite("IconHydrogen", Vec(-0.35f, -0.25f), Vec(0.08f, 0.08f), white, Anchor.Center));
				frame.Add(canvas.Sprite("IconEnergy", Vec(0.35f, -0.25f), Vec(0.1f, 0.1f), white, Anchor.Center));



				//Thrust text
				frame.Add(canvas.Text("25%", Vec(0.30f, 0.235f), 0.08f, white));
				frame.Add(canvas.Text("71%", Vec(-0.30f, 0.235f), 0.08f, white));

				frame.Add(canvas.Text("46", Vec(0, -0.1f), 0.16f, white));
				frame.Add(canvas.Text("[99]", Vec(0, 0.02f), 0.14f, white));
				frame.Add(canvas.Text("m/s", Vec(0, 0.15f), 0.1f, gray1));

			}
		}

		void DrawSlim(IMyTextSurface surface)
		{

		}

		Vector2 Vec(float x, float y)
		{
			return new Vector2(x, y);
		}

		Vector2 Vec(float xy)
		{
			return new Vector2(xy, xy);
		}

	}
	

	class Canvas
	{
		Vector2 canvasPosition;
		Vector2 canvasSize;
		Vector2 center;


		public Canvas(Vector2 position, Vector2 size)
		{
			canvasPosition = position;
			canvasSize = size;
			center = Vector2.One * 0.5f;
		}

		public MySprite Rect(Vector2 position, Vector2 size, Color color, Anchor anchor = Anchor.Center, float rotation = 0, Vector2? offset = null)
		{
			if (offset == null) offset = Vector2.Zero;
			return Sprite("SquareSimple", position, size, color, anchor, rotation, offset);
		}

		public MySprite Circle(Vector2 position, Vector2 size, Color color, Anchor anchor = Anchor.Center, float rotation = 0)
		{
			return Sprite("Circle", position, size, color, anchor, rotation);
		}

		public MySprite CircleHollow(Vector2 position, Vector2 size, Color color, Anchor anchor = Anchor.Center, float rotation = 0)
		{
			return Sprite("CircleHollow", position, size, color, anchor, rotation);
		}

		public MySprite Triangle(Vector2 position, Vector2 size, Color color, Anchor anchor = Anchor.Center, float rotation = 0, Vector2? offset = null)
		{
			if (offset == null) offset = Vector2.Zero;
			return Sprite("Triangle", position, size, color, anchor, rotation, offset);
		}

		public MySprite TriangleRight(Vector2 position, Vector2 size, Color color, Anchor anchor = Anchor.Center, float rotation = 0, Vector2? offset = null)
		{
			if (offset == null) offset = Vector2.Zero;
			return Sprite("RightTriangle", position, size, color, anchor, rotation, offset);
		}

		public MySprite SemiCircle(Vector2 position, Vector2 size, Color color, Anchor anchor = Anchor.Center, float rotation = 0, Vector2? offset = null)
		{
			if (offset == null) offset = Vector2.Zero;
			return Sprite("SemiCircle", position, size, color, anchor, rotation, offset);
		}

		//percent between 0 and 1.
		public MySprite Progress(Vector2 position, Vector2 size, Color color, float percent, Anchor anchor = Anchor.Center, float rotation = 0)
		{
			size = size * canvasSize;
			Vector2 spriteSize = size;
			spriteSize.X = MathHelper.Clamp(percent, 0, 1) * size.X;
			Vector2 rotateAround = ((center + position) * canvasSize) + canvasPosition;
			position = rotateAround - (Vector2.UnitX * size.X * 0.5f) + (Vector2.UnitX * spriteSize.X * 0.5f);
			return new MySprite(SpriteType.TEXTURE, "SquareSimple", position: AdjustToRotation(AdjustToAnchor(anchor, position, size), rotateAround, rotation), size: spriteSize, color: color, rotation: rotation);
		}


		public MySprite Sprite(string sprite, Vector2 position, Vector2 size, Color color, Anchor anchor, float rotation = 0, Vector2? offset = null)
		{
			if (offset == null) offset = Vector2.Zero;
			size = size * canvasSize;
			Vector2 rotateAround = ((center + position) * canvasSize) + canvasPosition;
			position = rotateAround + (Vector2)offset * canvasSize;
			return new MySprite(SpriteType.TEXTURE, sprite, position: AdjustToRotation(AdjustToAnchor(anchor, position, size), rotateAround, rotation), size: size, color: color, rotation: rotation);
		}

		public MySprite Text(string text, Vector2 position, float size, Color color, TextAlignment align = TextAlignment.CENTER)
		{
			position.Y -= size * 0.5f;
			size = canvasSize.Y / 30.6f * size;
			position = (center + position) * canvasSize + canvasPosition;
			return new MySprite(SpriteType.TEXT, text, position, rotation: size, color: color, alignment: align);
		}

		public float TextHeight(float scale, int lines = 1)
		{
			//Only for Debug font.
			//Got 28.8f from Surface.MeasureStringInPixels(new StringBuilder("Text"), "Debug", 1f).Y;
			//But that didn't look right, even if it techniclay might be.
			//So trial and error using the UVChecker texure and aligning a 0 to it.
			return lines * scale * 30.6f;
		}

		public Vector2 AdjustToAnchor(Anchor anchor, Vector2 position, Vector2 size)
		{
			switch (anchor)
			{
				case Anchor.Left:
					position.X = position.X + size.X * 0.5f;
					break;
				case Anchor.Right:
					position.X = position.X - size.X * 0.5f;
					break;
			}
			return position;
		}

		public Vector2 AdjustToRotation(Vector2 position, Vector2 rotateAround, float rotation)
		{
			var vec = Vector2.Zero;
			vec.X = (float)(Math.Cos(rotation) * (position.X - rotateAround.X) - Math.Sin(rotation) * (position.Y - rotateAround.Y) + rotateAround.X);
			vec.Y = (float)(Math.Sin(rotation) * (position.X - rotateAround.X) + Math.Cos(rotation) * (position.Y - rotateAround.Y) + rotateAround.Y);
			return vec;
		}
	}
	#endregion
}
