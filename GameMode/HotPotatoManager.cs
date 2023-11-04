﻿using AmongUs.GameOptions;
using HarmonyLib;
using System.Linq;
using UnityEngine;
using static TOHEXI.Translator;

namespace TOHEXI;

internal static class HotPotatoManager
{
    public static int RoundTime = new();
    public static int BoomTimes = new();
    public static int HotPotatoMax = new();
    public static int  IsAliveHot = new();
    public static int IsAliveCold = new();
    //设置

    public static OptionItem HotQuan;//热土豆数量
    public static OptionItem Boom; //爆炸时间;Remaining time of explosion
    public static OptionItem TD;//总时长;Totalduration


    public static void SetupCustomOption()
    {

        Boom = IntegerOptionItem.Create(62_293_008, "BoomTime", new(10, 60, 5), 15, TabGroup.GameSettings, false)
           .SetGameMode(CustomGameMode.HotPotato)
           .SetColor(new Color32(245, 82, 82, byte.MaxValue))
           .SetValueFormat(OptionFormat.Seconds);

    }
    public static void Init()
    {
        if (Options.CurrentGameMode != CustomGameMode.HotPotato) return;
        BoomTimes = Boom.GetInt() + 8;
        HotPotatoMax = Main.RealOptionsData.GetInt(Int32OptionNames.NumImpostors);
        IsAliveCold = 0;
        
    }
    public static string GetHudText()
    {
        return string.Format(Translator.GetString("HotPotatoTimeRemain"), BoomTimes.ToString());
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    class FixedUpdatePatch
    {
        private static long LastFixedUpdate = new();
        public static void Postfix(PlayerControl __instance)
        {
            if (!GameStates.IsInTask || Options.CurrentGameMode != CustomGameMode.HotPotato) return;

            if (AmongUsClient.Instance.AmHost)
            {
                foreach (var player in Main.AllPlayerControls)
                {
                    if (!player.IsAlive()) continue;
                    //一些巴拉巴拉的东西
                    var playerList = Main.AllAlivePlayerControls.ToList();
                    if (playerList.Count == 1)
                    {
                        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.CP);
                        CustomWinnerHolder.WinnerIds.Add(player.PlayerId);
                    }
                        //土豆数量检测
                        if (playerList.Count == 9 || playerList.Count == 10 || playerList.Count == 11)
                    {
                        if (HotPotatoMax == 3)
                        HotPotatoMax = 2;
                        }
                        else if(playerList.Count == 6 || playerList.Count == 7 || playerList.Count == 5)
                    {
                        if (HotPotatoMax == 2)
                            HotPotatoMax = 1;
                    }
                    
                        
                        //爆炸时间为0时
                    if (BoomTimes <= 0)
                    {
                        BoomTimes = Boom.GetInt();
                        foreach (var pc in Main.AllAlivePlayerControls)
                        {
                            if (pc.Is(CustomRoles.Hotpotato)) pc.RpcMurderPlayerV3(pc);
                            Logger.Info($"炸死一群","awa");
                        }
                     for(int i=0;i<HotPotatoMax;i++)
                        {
                            IsAliveCold++;
                            var pcList = Main.AllAlivePlayerControls.Where(x => x.GetCustomRole() != CustomRoles.Hotpotato).ToList();
                            var Ho = pcList[IRandom.Instance.Next(0, pcList.Count)];
                            Ho.RpcSetCustomRole(CustomRoles.Hotpotato);
                           Ho.Notify(GetString("GetHotPotato"));
                            Logger.Info($"分配热土豆", "awa");
                        }
                        IsAliveCold = 0;
                        break;
                    }                                           
                    
                }

                if (LastFixedUpdate == Utils.GetTimeStamp()) return;
                LastFixedUpdate = Utils.GetTimeStamp();
                //减少爆炸冷却
                BoomTimes--;
            }
        }
    }
}

