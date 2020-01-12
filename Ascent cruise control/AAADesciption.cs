#region pre-script
using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using VRageMath;
using VRage.Game;
using VRage.Collections;
using Sandbox.ModAPI.Ingame;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
#endregion
namespace IngameScript
{
	
	class AAADesciption
	{
		#region untouched
		/*
		Blargmode's Ascent Cruise Control
		Version 2.0 (2020-01-13)

		Tired of wasting fuel when leaving a gravity well? What you need is cruise control!

		This script adjusts the thrust of your rear thrusters to the lowest possible without losing speed.


		___/ Setup \\__________

		1. Install script.
		2. Sit in a flight seat for a few seconds.
			You can add the programmable block to the toolbar with the argument 'cruise'.

		The script looks for an occupied flight on startup to determine what is forward. 
		Once one is found, it stops looking and saves the seat internally. If the Wrong seat is stored, you can hop into
		the correct one, access the Programmable block, type 'reset' as the argument, and press run.


		___/ Usage \\__________

		Press the button you set up in step two to turn the cruise control on or off.


		___/ Usage (detailed) \\__________

		These commands will move your ship, so don't trigger them accidentally.

		You trigger it by sending commands, either via a toolbar or directly in the programmable block. 
		Set up the former by adding the programmable block to a toolbar and selecting Run, then type in your trigger command.

		The trigger commands are 'cruise' and 'align'. To toggle cruise or align on or off. 
		if you add the argument 'on' or 'off' if you don't want the button to toggle. Example: 'cruise off'
		Cruise control also accepts a number, target speed, which can be supplied like this: 'cruise 95'. 
		It can also be set in the settings. 
		You can send several commands at once as well, separate them with a comma: 'cruise on 95, align on'.


		___/ Optional extras \\__________

		The script can show status on LCDs. 

		Just add the tag #ACC to the name of the LCD.

		If you want it on a specific cockpit screen, write #ACC@3 to put it on the third display.

		You can have more than one LCD. 

		Cruise Control can also be engaged via a button, a sensor, or any other action. 


		___/ Settings \\__________

		Theres a list with commands and what they do in the sidebar of the programmable block.
		Each command is used with it's prefix and value. Target speed for example is ts. 
		You can set it to 50 by typing 'ts 50'.

		If type the prefix without a value you'll get an error message telling you what values it accepts.

		You can type for example 'mx' (max altitude) and it will tell you:
		'mx' requires a number (set to -1 for unlimited) or 'this' for current altitude.


		___/ Align \\__________

		You can turn on align in order for the ship to align with gravity. This only works in the up direction. 
		This is intended for use in space elevators. You can make a type of space elevator with this script.
		Read more below in the section "Controlled descent".


		___/ Controlled descent \\__________

		In version 1 of this script it was an unintended feature. Now it has been fleshed out. 
		If you set the target speed to a negative value, it will fly in the reverse direction.
		Not only that, you can set a target altitude and it will slow down in order to stop there. 
		This works for both ascending and descending. 

		Together with Align this makes for a pretty nice space elevator. 
		Set the minimum altitude when at the bottom 'mn this', turn on align and fly straight up.
		When you reach the edge of gravity, stop WITHIN it and set the max altitude 'mx this'.
		Now you can run 'cruise -99' for it to go down and 'cruise 99' to go up, and it will slow down and stop
		at each end. 
		You can even build stations there.
		I recommend using connectors to align it at each station. So that it can't drift sideways if you mess
		around up there. I also recommend not using a flight seat that can steer the ship. Use a button panel instead.
		You can leave cruise control and align turned on even when docked. 

		A word of warning though. 
		The controlled descent does not take ship weight and thrust into consideration.
		If your ship is too weak, it might leave a crater.

		*/
		#endregion
	}

}
