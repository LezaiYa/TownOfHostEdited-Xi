using AmongUs.GameOptions;
using HarmonyLib;
using MS.Internal.Xml.XPath;
using System;
using System.Collections.Generic;
using System.Linq;
using TOHEXI.Modules;
using System.Text;
using TOHEXI.Roles.Crewmate;
using TOHEXI.Roles.Impostor;
using TOHEXI.Roles.Neutral;
using UnityEngine;
using static TOHEXI.Translator;
using Hazel;
using InnerNet;
using System.Threading.Tasks;
using TOHEXI.Roles.AddOns.Crewmate;
using UnityEngine.Profiling;
using System.Runtime.Intrinsics.X86;
using static UnityEngine.GraphicsBuffer;
using UnityEngine.UI;
using UnityEngine.Networking.Types;
using TOHEXI.Roles.Double;
using Microsoft.Extensions.Logging;
using Sentry;
using UnityEngine.SocialPlatforms;
using static UnityEngine.ParticleSystem.PlaybackState;
using Cpp2IL.Core.Extensions;

namespace TOHEXI;

/*
 * HUGE THANKS TO
 * ImaMapleTree / 단풍잎 / Tealeaf
 * FOR THE CODE
 */


[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.TryPet))]
class LocalPetPatch
{
    private static readonly Dictionary<byte, long> LastProcess = new();

    public static bool Prefix(PlayerControl __instance)
    {
        if (!Options.UsePets.GetBool()) return true;
        if (!(AmongUsClient.Instance.AmHost && AmongUsClient.Instance.AmClient)) return true;
        if (GameStates.IsLobby) return true;

        if (__instance.petting) return true;
        __instance.petting = true;

        if (!LastProcess.ContainsKey(__instance.PlayerId)) LastProcess.TryAdd(__instance.PlayerId, Utils.GetTimeStamp() - 2);
        if (LastProcess[__instance.PlayerId] + 1 >= Utils.GetTimeStamp()) return true;

        ExternalRpcPetPatch.Prefix(__instance.MyPhysics, (byte)RpcCalls.Pet);
        LastProcess[__instance.PlayerId] = Utils.GetTimeStamp();
        return !__instance.GetCustomRole().PetActivatedAbility();
    }

    public static void Postfix(PlayerControl __instance)
    {
        if (!Options.UsePets.GetBool()) return;
        if (!(AmongUsClient.Instance.AmHost && AmongUsClient.Instance.AmClient)) return;
        __instance.petting = false;
    }
}

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleRpc))]
class ExternalRpcPetPatch
{
    private static long LastFixedUpdate = new();
    public static Dictionary<byte, float> PetCooldown = new();
    public static Dictionary<byte, bool> SkillReady = new();
    public static void Init()
    {
        PetCooldown = new();
        SkillReady = new();
    }
    
    private static readonly Dictionary<byte, long> LastProcess = new();
    public static void Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] byte callID)
    {
        if (!Options.UsePets.GetBool() || !AmongUsClient.Instance.AmHost || (RpcCalls)callID != RpcCalls.Pet) return;

        var pc = __instance.myPlayer;
        var physics = __instance;

        if (pc == null || physics == null) return;

        if (pc != null
            && !pc.inVent
            && !pc.inMovingPlat
            && !pc.walkingToVent
            && !pc.onLadder
            && !physics.Animations.IsPlayingEnterVentAnimation()
            && !physics.Animations.IsPlayingClimbAnimation()
            && !physics.Animations.IsPlayingAnyLadderAnimation()
            && !Pelican.IsEaten(pc.PlayerId)
            && GameStates.IsInTask
            && pc.GetCustomRole().PetActivatedAbility())
            physics.CancelPet();

        if (!LastProcess.ContainsKey(pc.PlayerId)) LastProcess.TryAdd(pc.PlayerId, Utils.GetTimeStamp() - 2);
        if (LastProcess[pc.PlayerId] + 1 >= Utils.GetTimeStamp()) return;
        LastProcess[pc.PlayerId] = Utils.GetTimeStamp();
        __instance.CancelPet();
        physics.RpcCancelPet();
        physics.RpcCancelPet();
        physics.RpcCancelPet();
        physics.RpcCancelPet();
        Logger.Info($"Player {pc.GetNameWithRole().RemoveHtmlTags()} petted their pet", "PetActionTrigger");

        _ = new LateTask(() => { OnPetUse(pc); }, 0.2f, $"OnPetUse: {pc.GetNameWithRole().RemoveHtmlTags()}");
    }
    public static void OnPetUse(PlayerControl pc)
    {
        
        if (pc == null ||
            pc.inVent ||
            pc.inMovingPlat ||
            pc.onLadder ||
            pc.walkingToVent ||
            pc.MyPhysics.Animations.IsPlayingEnterVentAnimation() ||
            pc.MyPhysics.Animations.IsPlayingClimbAnimation() ||
            pc.MyPhysics.Animations.IsPlayingAnyLadderAnimation() ||
            Pelican.IsEaten(pc.PlayerId))
            return;
        Rudepeople.OnUsePet(pc);
        switch (pc.GetCustomRole())
        {
            case CustomRoles.Mayor:
                if (Main.MayorUsedButtonCount[pc.PlayerId] < Options.MayorNumOfUseButton.GetInt() && !Main.MayorStartMeetCooldown.ContainsKey(pc.PlayerId))
                {
                    Main.MayorStartMeetCooldown.Add(pc.PlayerId, Utils.GetTimeStamp());
                    pc?.ReportDeadBody(null);
                }
                break;
            case CustomRoles.Veteran:
                if(!Main.VeteranProtectCooldown.ContainsKey(pc.PlayerId) && Main.VeteranNumOfUsed[pc.PlayerId] >= 1)
                {
                    Logger.Info($"Playerawa", "PetActionTrigger");
                    Main.VeteranInProtect.Remove(pc.PlayerId);
                    Main.VeteranInProtect.Add(pc.PlayerId, Utils.GetTimeStamp());
                    Main.VeteranNumOfUsed[pc.PlayerId]--;
                    if (!pc.IsModClient()) pc.RpcGuardAndKill(pc);
                    pc.RPCPlayCustomSound("Gunload");
                    pc.Notify(GetString("VeteranOnGuard"), Options.VeteranSkillDuration.GetFloat());
                    Main.VeteranProtectCooldown.Add(pc.PlayerId, Utils.GetTimeStamp());
                }
                break;
            case CustomRoles.TimeMaster:
                if (!Main.TimeMasterCooldown.ContainsKey(pc.PlayerId))
                {
                    Main.TimeMasterCooldown.Add(pc.PlayerId, Utils.GetTimeStamp());
                    Main.TimeMasterInProtect.Remove(pc.PlayerId);
                  Main.TimeMasterInProtect.Add(pc.PlayerId, Utils.GetTimeStamp());
                if (!pc.IsModClient()) pc.RpcGuardAndKill(pc);
                pc.Notify(GetString("TimeMasterOnGuard"), Options.TimeMasterSkillDuration.GetFloat());
                foreach (var player in Main.AllPlayerControls)
                {
                    if (Main.TimeMasterbacktrack.ContainsKey(player.PlayerId))
                    {
                        var position = Main.TimeMasterbacktrack[player.PlayerId];
                        Utils.TP(player.NetTransform, position);
                        Main.TimeMasterbacktrack.Remove(player.PlayerId);
                    }
                    else
                    {
                        Main.TimeMasterbacktrack.Add(player.PlayerId, player.GetTruePosition());
                    }
                }
                }
                break;
            case CustomRoles. Grenadier:
                if(!Main.GrenadierCooldown.ContainsKey(pc.PlayerId) || !Main.MadGrenadierCooldown.ContainsKey(pc.PlayerId))
                {
         if (pc.Is(CustomRoles.Madmate))
                {
                        Main.MadGrenadierCooldown.Add(pc.PlayerId, Utils.GetTimeStamp());
                        Main.MadGrenadierBlinding.Remove(pc.PlayerId);
                    Main.MadGrenadierBlinding.Add(pc.PlayerId, Utils.GetTimeStamp());
                    Main.AllPlayerControls.Where(x => x.IsModClient()).Where(x => !x.GetCustomRole().IsImpostorTeam() && !x.Is(CustomRoles.Madmate)).Do(x => x.RPCPlayCustomSound("FlashBang"));
                }
                else
                {
                        Main.GrenadierCooldown.Add(pc.PlayerId, Utils.GetTimeStamp());
                        Main.GrenadierBlinding.Remove(pc.PlayerId);
                    Main.GrenadierBlinding.Add(pc.PlayerId, Utils.GetTimeStamp());
                    Main.AllPlayerControls.Where(x => x.IsModClient()).Where(x => x.GetCustomRole().IsImpostor() || (x.GetCustomRole().IsNeutral() && Options.GrenadierCanAffectNeutral.GetBool())).Do(x => x.RPCPlayCustomSound("FlashBang"));
                }
                if (!pc.IsModClient()) pc.RpcGuardAndKill(pc);
                pc.RPCPlayCustomSound("FlashBang");
                pc.Notify(GetString("GrenadierSkillInUse"), Options.GrenadierSkillDuration.GetFloat());
                Utils.MarkEveryoneDirtySettings();
                }
           
                break;
            case CustomRoles.TimeStops:
                if(!Main.TimeStopsCooldown.ContainsKey(pc.PlayerId))
                {
                    Main.TimeStopsCooldown.Add(pc.PlayerId, Utils.GetTimeStamp());
                    CustomSoundsManager.RPCPlayCustomSoundAll("THEWORLD");
                Main.TimeStopsInProtect.Remove(pc.PlayerId);
                Main.TimeStopsInProtect.Add(pc.PlayerId, Utils.GetTimeStamp());
                if (!pc.IsModClient()) pc.RpcGuardAndKill(pc);
                pc.RPCPlayCustomSound("THEWORLD");
                pc.Notify(GetString("TimeStopsOnGuard"), Options.TimeStopsSkillDuration.GetFloat());
                foreach (var player in Main.AllAlivePlayerControls)
                {
                    if (pc == player) continue;
                    if (!player.IsAlive() || Pelican.IsEaten(player.PlayerId)) continue;
                    NameNotifyManager.Notify(player, Utils.ColorString(Utils.GetRoleColor(CustomRoles.TimeStops), GetString("ForTimeStops")));
                    var tmpSpeed1 = Main.AllPlayerSpeed[player.PlayerId];
                    Main.TimeStopsstop.Add(player.PlayerId);
                    Main.AllPlayerSpeed[player.PlayerId] = Main.MinSpeed;
                    ReportDeadBodyPatch.CanReport[player.PlayerId] = false;
                    player.MarkDirtySettings();
                    new LateTask(() =>
                    {
                        Main.AllPlayerSpeed[player.PlayerId] = Main.AllPlayerSpeed[player.PlayerId] - Main.MinSpeed + tmpSpeed1;
                        ReportDeadBodyPatch.CanReport[player.PlayerId] = true;
                        player.MarkDirtySettings();
                        Main.TimeStopsstop.Remove(player.PlayerId);
                        RPC.PlaySoundRPC(player.PlayerId, Sounds.TaskComplete);
                    }, Options.TimeStopsSkillDuration.GetFloat(), "Time Stop");
                }
                }
                break;
            case CustomRoles.GlennQuagmire:
                if(!Main.GlennQuagmireCooldown.ContainsKey(pc.PlayerId))
                {
        List<PlayerControl> list = Main.AllAlivePlayerControls.Where(x => x.PlayerId != pc.PlayerId).ToList();
                if (list.Count < 1)
                {
                    Logger.Info($"Q哥没有目标", "GlennQuagmire");
                }
                else
                {
                    list = list.OrderBy(x => Vector2.Distance(pc.GetTruePosition(), x.GetTruePosition())).ToList();
                    var target = list[0];
                    if (target.GetCustomRole().IsImpostor())
                    {
                        pc.RPCPlayCustomSound("giggity");
                        target.SetRealKiller(pc);
                        target.RpcCheckAndMurder(target);
                        pc.RpcGuardAndKill();
                        Main.PlayerStates[target.PlayerId].deathReason = PlayerState.DeathReason.Creation;
                    }
                    else
                    {
                        if (Main.ForSourcePlague.Contains(pc.PlayerId))
                        {
                                Utils.TP(pc.NetTransform, target.GetTruePosition());
                                Main.ForSourcePlague.Add(target.PlayerId);
                                Utils.NotifyRoles();
                        }
                            Utils.TP(pc.NetTransform, target.GetTruePosition());
                    }
                }
                }

                break;
            case CustomRoles.SoulSeeker:
                if(!Main.SoulSeekerCooldown.ContainsKey(pc.PlayerId))
                {
                if (!pc.IsModClient()) pc.RpcGuardAndKill(pc);
                foreach (var player in Main.AllPlayerControls)
                {
                    if (Pelican.IsEaten(player.PlayerId) && Options.SoulSeekerCanSeeEat.GetBool())
                    {
                        Main.SoulSeekerCanEat[pc.PlayerId]++;
                    }
                    if (!player.IsAlive())
                    {
                        Main.SoulSeekerDead[pc.PlayerId]++;
                        if (player.CanUseKillButton())
                        {
                            Main.SoulSeekerCanKill[pc.PlayerId]++;
                        }
                        else
                        {
                            Main.SoulSeekerNotCanKill[pc.PlayerId]++;
                        }
                    }
                }
                pc.Notify(string.Format(GetString("SoulSeekerOffGuard"), Main.SoulSeekerDead[pc.PlayerId], Main.SoulSeekerNotCanKill[pc.PlayerId], Main.SoulSeekerCanKill[pc.PlayerId], Main.SoulSeekerCanEat[pc.PlayerId]));
                if (Main.SoulSeekerCanEat[pc.PlayerId] > 0)
                {
                    Main.SoulSeekerCanEat[pc.PlayerId] = 0;
                }
                if (Main.SoulSeekerCanKill[pc.PlayerId] > 0)
                {
                    Main.SoulSeekerCanKill[pc.PlayerId] = 0;
                }
                if (Main.SoulSeekerNotCanKill[pc.PlayerId] > 0)
                {
                    Main.SoulSeekerNotCanKill[pc.PlayerId] = 0;
                }
                if (Main.SoulSeekerDead[pc.PlayerId] > 0)
                {
                    Main.SoulSeekerDead[pc.PlayerId] = 0;
                }
                }

                break;
            case CustomRoles.Plumber:
                if(!Main.PlumberCooldown.ContainsKey(pc.PlayerId))
                {
                    foreach (var player in Main.AllAlivePlayerControls)
                    {
                        if (player.PlayerId == pc.PlayerId || !player.inVent) continue;
                        player?.MyPhysics?.RpcBootFromVent(pc.PlayerId);
                    }
                }
                break;
        }
    }
}