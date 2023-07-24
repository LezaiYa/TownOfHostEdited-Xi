﻿using HarmonyLib;
using Hazel;
using MS.Internal.Xml.XPath;
using Sentry;
using System.Collections.Generic;
using System.Linq;
using TOHE.Roles.Neutral;
using UnityEngine;
using static TOHE.RandomSpawn;
using static UnityEngine.GraphicsBuffer;

namespace TOHE.Roles.Crewmate;
public class NiceMini
{
    private static readonly int Id = 7565376;
    private static List<byte> playerIdList = new();
    public static int GrowUpTime = new();
    public static int GrowUp = new(); 
    public static int Age = new();
    public static int Up = new(); 
    public static OptionItem GrowUpDuration;
    public static OptionItem EveryoneCanKnowMini;
    public static OptionItem OnMeetingStopCountdown;
    public static OptionItem EvilMiniSpawnChances;
    public static OptionItem CanBeEvil;
    public static OptionItem MinorCD;
    public static OptionItem MajorCD;
    public static void SetupCustomOption()
    {
        Options.SetupSingleRoleOptions(Id, TabGroup.CrewmateRoles, CustomRoles.NiceMini, 1, zeroOne: false);
        GrowUpDuration = IntegerOptionItem.Create(Id + 100, "GrowUpDuration", new(200, 400, 50), 300, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.NiceMini])
            .SetValueFormat(OptionFormat.Seconds);
        EveryoneCanKnowMini = BooleanOptionItem.Create(Id + 102, "EveryoneCanKnowMini", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.NiceMini]);
        CanBeEvil = BooleanOptionItem.Create(Id + 106, "CanBeEvil", true, TabGroup.CrewmateRoles, false).SetParent(Options.CustomRoleSpawnChances[CustomRoles.NiceMini]);
        EvilMiniSpawnChances = IntegerOptionItem.Create(Id + 108, "LoverSpawnChances", new(0, 100, 5), 50, TabGroup.CrewmateRoles, false).SetParent(CanBeEvil)
            .SetValueFormat(OptionFormat.Percent);
        MinorCD = FloatOptionItem.Create(Id + 110, "KillCooldown", new(0f, 180f, 2.5f), 45f, TabGroup.CrewmateRoles, false).SetParent(CanBeEvil)
            .SetValueFormat(OptionFormat.Seconds);
        MajorCD = FloatOptionItem.Create(Id + 112, "MajorCooldown", new(0f, 180f, 2.5f), 15f, TabGroup.CrewmateRoles, false).SetParent(CanBeEvil)
           .SetValueFormat(OptionFormat.Seconds);
    }
    public static void Init()
    {
        GrowUpTime = GrowUpDuration.GetInt();
        playerIdList = new();
        GrowUp = GrowUpDuration.GetInt() / 18;
        Age = 0;
        Up = GrowUp;
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);

        if (Options.CurrentGameMode != CustomGameMode.TOEX || Options.AllModMode.GetBool()) if (!AmongUsClient.Instance.AmHost) return;
        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;

    public static string GetAge(byte playerId) => Utils.ColorString(Color.yellow, Age != 18 ? $"({Age})" : "");
}
