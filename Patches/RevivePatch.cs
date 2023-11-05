/*using HarmonyLib;
using TOHEXI;
using System;
using UnityEngine;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Revive))] // Modded
class RevivePatch
{
	public static bool Prefix(PlayerControl target)
	{
		if (!AmongUsClient.Instance.AmHost) return false;
		TOHEXI.Logger.Info($"Revive{target.GetNameWithRole()}", "Revive");

		if (!AmongUsClient.Instance.AmHost) return true;
		return RevivePatch.Prefix(target);
	}
}*/