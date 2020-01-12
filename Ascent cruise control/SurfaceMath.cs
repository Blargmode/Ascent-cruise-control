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

	enum Anchor
	{
		Center,
		Left,
		Right
	}

	class SurfaceMath
	{
		public Vector2 Size;
		public Vector2 TopLeft;
		public Vector2 BGSize;
		public Vector2 Center;
		public float SmallestSize;
		
		public SurfaceMath(IMyTextSurface surface)
		{
			if (surface.SurfaceSize.X > surface.SurfaceSize.Y)
			{
				BGSize = new Vector2(surface.SurfaceSize.X, surface.SurfaceSize.X);
				SmallestSize = surface.SurfaceSize.Y;
			}
			else
			{
				BGSize = new Vector2(surface.SurfaceSize.Y, surface.SurfaceSize.Y);
				SmallestSize = surface.SurfaceSize.X;
			}

			Size = surface.SurfaceSize;

			TopLeft = (surface.TextureSize - surface.SurfaceSize) * 0.5f;

			Center = surface.TextureSize * 0.5f;
		}

		public Vector2 VW_VH(float x, float y)
		{
			return new Vector2(VW(x), VH(y));
		}

		public Vector2 VW_Vmin(float x, float y)
		{
			return new Vector2(VW(x), VMin(y));
		}

		public Vector2 Vmin_VH(float x, float y)
		{
			return new Vector2(VMin(x), VH(y));
		}

		public Vector2 Vmin_Vmin(float x, float y)
		{
			return new Vector2(VMin(x), VMin(y));
		}
		
		public float VMin(float x)
		{
			return SmallestSize * x * 0.01f;
		}

		public float VW(float x)
		{
			return Size.X * x * 0.01f;
		}

		public float VH(float y)
		{
			return Size.Y * y * 0.01f;
		}

		public Vector2 VCenterText(Vector2 pos, float fontSize)
		{
			return new Vector2(pos.X, pos.Y - TextHeight(fontSize) * 0.5f);
		}

		//public void PostionInterpreter(MeterDefinition def)
		//{


		//	//Position
		//	//vw vh, and vmin adjustments.
		//	switch (def.posType.X)
		//	{
		//		case VectorType.ViewWidth:
		//			def.position.X = Size.X * def.position.X * 0.01f;
		//			break;
		//		case VectorType.ViewHeight:
		//			def.position.X = Size.Y * def.position.X * 0.01f;
		//			break;
		//		case VectorType.ViewMin:
		//			def.position.X = SmallestSize * def.position.X * 0.01f;
		//			break;
		//	}
		//	switch (def.posType.Y)
		//	{
		//		case VectorType.ViewWidth:
		//			def.position.Y = Size.X * def.position.Y * 0.01f;
		//			break;
		//		case VectorType.ViewHeight:
		//			def.position.Y = Size.Y * def.position.Y * 0.01f;
		//			break;
		//		case VectorType.ViewMin:
		//			def.position.Y = SmallestSize * def.position.Y * 0.01f;
		//			break;
		//	}
		//	def.position.Y = def.position.Y * -1; //Invert Y

		//	//size
		//	if (def.type == Meter.Text || def.type == Meter.Value)
		//	{
		//		switch (def.sizeType.X)
		//		{
		//			case VectorType.ViewWidth:
		//				def.size.X = Size.X / 30.6f * def.size.X * 0.01f;
		//				break;
		//			case VectorType.ViewHeight:
		//				def.size.X = Size.Y / 30.6f * def.size.X * 0.01f;
		//				break;
		//			case VectorType.ViewMin:
		//				def.size.X = SmallestSize / 30.6f * def.size.X * 0.01f;
		//				break;
		//		}
		//	}
		//	else
		//	{
		//		switch (def.sizeType.X)
		//		{
		//			case VectorType.ViewWidth:
		//				def.size.X = Size.X * def.size.X * 0.01f;
		//				break;
		//			case VectorType.ViewHeight:
		//				def.size.X = Size.Y * def.size.X * 0.01f;
		//				break;
		//			case VectorType.ViewMin:
		//				def.size.X = SmallestSize * def.size.X * 0.01f;
		//				break;
		//		}
		//		switch (def.sizeType.Y)
		//		{
		//			case VectorType.ViewWidth:
		//				def.size.Y = Size.X * def.size.Y * 0.01f;
		//				break;
		//			case VectorType.ViewHeight:
		//				def.size.Y = Size.Y * def.size.Y * 0.01f;
		//				break;
		//			case VectorType.ViewMin:
		//				def.size.Y = SmallestSize * def.size.Y * 0.01f;
		//				break;
		//		}
		//	}
		//}

		//text var is for multiline text, not tested
		public float TextHeight(float scale, string text)
		{
			//Only for Debug font.
			//Got 28.8f from Surface.MeasureStringInPixels(new StringBuilder("Text"), "Debug", 1f).Y;
			//But that didn't look right, even if it techniclay might be.
			//So trial and error using the UVChecker texure and aligning a 0 to it.
			int count = 1;
			foreach (char c in text)
				if (c == '\n') count++;
			return count * scale * 30.6f;
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
