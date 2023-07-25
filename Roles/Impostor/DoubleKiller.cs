﻿using System;
using System.Collections.Generic;
using static TOHE.Options;

namespace TOHE;

public static class DoubleKiller
{
    private static readonly int Id = 416574687;
    public static List<byte> playerIdList = new();
    public static OptionItem TwoDoubleKillerKillColldown;
    public static OptionItem DoubleKillerKillColldown;
    private static Dictionary<byte, float> NowCooldown;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id,TabGroup.ImpostorRoles, CustomRoles.DoubleKiller);
        DoubleKillerKillColldown = FloatOptionItem.Create(165467847, "KillCooldown", new(0f, 100f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.DoubleKiller])
           .SetValueFormat(OptionFormat.Seconds);
        TwoDoubleKillerKillColldown = FloatOptionItem.Create(165467847, "DoubleKillerKillCooldown", new(0f, 100f, 2.5f), 25f, TabGroup.ImpostorRoles, false).SetParent(CustomRoleSpawnChances[CustomRoles.DoubleKiller])
           .SetValueFormat(OptionFormat.Seconds);
    }
    public static void Init()
    {
        playerIdList = new();
        NowCooldown = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        NowCooldown.TryAdd(playerId, DoubleKillerKillColldown.GetFloat());
    }
    public static bool IsEnable() => playerIdList.Count > 0;
    public static void SetKillCooldown(byte id) => Main.AllPlayerKillCooldown[id] = NowCooldown[id];
    public static void OnCheckMurder(PlayerControl killer)
    {
        if (Main.DoubleKillerMax.Contains(killer.PlayerId))
        {
            Main.DoubleKillerKillSeacond.Add(killer.PlayerId, Utils.GetTimeStamp());
            Main.DoubleKillerMax.Remove(killer.PlayerId);
            NowCooldown[killer.PlayerId] = Math.Clamp(NowCooldown[killer.PlayerId] - DoubleKillerKillColldown.GetFloat(), 0, DoubleKillerKillColldown.GetFloat());
            killer.ResetKillCooldown();
            killer.SyncSettings();
            Logger.Info($"QAQ", "ReportDeadbody");
        }
        else
        {
            NowCooldown[killer.PlayerId] = Math.Clamp(NowCooldown[killer.PlayerId] = DoubleKillerKillColldown.GetFloat(), 0, DoubleKillerKillColldown.GetFloat());
            killer.ResetKillCooldown();
            killer.SyncSettings();
            Logger.Info($"wwwwwww", "ReportDeadbody");
        }       
    }
}
