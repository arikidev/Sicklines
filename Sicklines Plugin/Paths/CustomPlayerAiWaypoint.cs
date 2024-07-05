using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reptile;
using HarmonyLib;
using BepInEx.Logging;

namespace Sicklines.AiPaths
{
    public class CustomPlayerAiWaypoint: PlayerAIWaypoint
    {
		public int TrickId = 1;
		public bool TrickBoosted = false;
		public MoveStyle movestyle;
		public bool fromJump = false;
		public int dance = -1;
		public bool end;
		public bool noEncounter;
		public WaypointType waypointType = WaypointType.basic;

		//Non saved properties
		public bool inAir = false; //Was the player not grounded at time of creation.
    }

	public enum TrickType
	{
		Normal,
		// Token: 0x04002815 RID: 10261
		Boosted
	}

	public enum WaypointType
	{ 
		basic,
		landing,
		jump,
		trick,
		trickBoosted,
		airtrick,
		airtrickBoosted,
		airdash,
		grind,
		grindRight,
		grindLeft,
		grindTrick,
		grindTrickBoosted,
		wallrun,
		wallrunRight,
		wallrunLeft,
		boost,
		boostEnd,
		slide,
		slideEnd,
		movestyleSwitch,
		dance,
		end,
		hidden,
		max
	}
}
