using AmongUs.GameOptions;
using HarmonyLib;
using MS.Internal.Xml.XPath;
using Sentry.Internal.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TOHEXI.Roles.Crewmate;
using TOHEXI.Roles.Impostor;
using TOHEXI.Roles.Neutral;
using UnityEngine;
using static TOHEXI.Translator;
using Hazel;
using InnerNet;
using System.Threading.Tasks;
using TOHEXI.Modules;
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

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
class PlayerStartPatch
{
    public static void Postfix(PlayerControl __instance)
    {
        var roleText = UnityEngine.Object.Instantiate(__instance.cosmetics.nameText);
        roleText.transform.SetParent(__instance.cosmetics.nameText.transform);
        roleText.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        roleText.fontSize -= 1.2f;
        roleText.text = "RoleText";
        roleText.gameObject.name = "RoleText";
        roleText.enabled = false;
    }
}

public enum RoleActionType
{
    /// <summary>
    /// Represents no action
    /// </summary>
    None,
    /// <summary>
    /// Any action specifically taken by a player
    /// Parameters: (PlayerControl source, RoleAction action, object[] parameters)
    /// </summary>
    AnyPlayerAction,
    /// <summary>
    /// Triggers when any player pets
    /// </summary>
    AnyPet,
    OnPet,
    /// <summary>
    /// Triggers when the pet button is held down. This gets sent every 0.4 seconds if the button is held down. The
    /// times parameter indicates how many times the button has been held down during the current span.
    /// <br/>
    /// Example: if times = 3 then the button has been held down for 1.2 seconds because 3 x 0.4 = 1.2
    /// </summary>
    /// <param name="times">the number of times the button has been detected in the down state (+1 every 0.4 seconds)</param>
    OnHoldPet,
    /// <summary>
    /// Triggers when the pet button has been held then released. Similar to <see cref="OnHoldPet"/>, the
    /// times parameter indicates how many times the button has been held down during the current span.
    /// </summary>
    /// <param name="times">the number of times the button has been detected in the down state (+1 every 0.4 seconds)</param>
    OnPetRelease,
    /// <summary>
    /// Triggers whenever the player enters a vent (this INCLUDES vent activation)
    /// Parameters: (Vent vent)
    /// </summary>
    MyEnterVent,
    /// <summary>
    /// Triggered when a player ACTUALLY enters a vent (not just Vent activation)
    /// Parameters: (Vent vent, PlayerControl venter)
    /// </summary>
    AnyEnterVent,
    VentExit,
    SuccessfulAngelProtect,
    SabotageStarted,
    /// <summary>
    /// Triggered when any one player fixes any part of a sabotage (I.E MiraHQ Comms) <br></br>
    /// Parameters: (SabotageType type, PlayerControl fixer, byte fixBit)
    /// </summary>
    SabotagePartialFix,
    SabotageFixed,
    AnyShapeshift,
    Shapeshift,
    AnyUnshapeshift,
    Unshapeshift,
    /// <summary>
    /// Triggered when my player attacks another player<br/>
    /// Parameters: (PlayerControl target)
    /// </summary>
    Attack,
    /// <summary>
    /// Triggered when my player dies. This action <b>CANNOT</b> be canceled. <br/>
    /// </summary>
    /// <param name="killer"><see cref="PlayerControl"/> the killer</param>
    /// <param name="realKiller"><see cref="Optional{T}"/> the OPTIONAl real killer (exists if killed indirectly)</param>
    MyDeath,
    SelfExiled,
    /// <summary>
    /// Triggers when any player gets exiled (by being voted out)
    /// </summary>
    /// <param name="exiled"><see cref="GameData.PlayerInfo"/> the exiled player</param>
    AnyExiled,
    /// <summary>
    /// Triggers on Round Start (end of meetings, and start of game)
    /// Parameters: (bool isRoundOne)
    /// </summary>
    RoundStart,
    RoundEnd,
    SelfReportBody,
    /// <summary>
    /// Triggers when any player reports a body. <br></br>Parameters: (PlayerControl reporter, PlayerInfo reported)
    /// </summary>
    AnyReportedBody,
    /// <summary>
    /// Triggers when any player completes a task. This cannot be canceled (Currently)
    /// </summary>
    /// <param name="player"><see cref="PlayerControl"/> the player completing the task</param>
    /// <param name="task"><see cref="Optional"/> an optional of <see cref="PlayerTask"/>, containing the task that was done</param>
    /// <param name="taskLength"><see cref="NormalPlayerTask.TaskLength"/> the length of the completed task</param>
    TaskComplete,
    FixedUpdate,
    /// <summary>
    /// Triggers when any player dies. This cannot be canceled
    /// </summary>
    /// <param name="victim"><see cref="PlayerControl"/> the dead player</param>
    /// <param name="killer"><see cref="PlayerControl"/> the killing player</param>
    /// <param name="deathEvent"><see cref="Lotus.Managers.History.Events.IDeathEvent"/> the related death event </param>
    AnyDeath,
    /// <summary>
    /// Triggers when my player votes for someone (or skips)
    /// </summary>
    /// <param name="voted"><see cref="PlayerControl"/> the player voted for, or null if skipped</param>
    /// <param name="delegate"><see cref="MeetingDelegate"/> the meeting delegate for the current meeting</param>
    MyVote,
    /// <summary>
    /// Triggers when any player votes for someone (or skips)
    /// </summary>
    /// <param name="voter"><see cref="PlayerControl"/> the player voting</param>
    /// <param name="voted"><see cref="PlayerControl"/> the player voted for, or null if skipped</param>
    /// <param name="delegate"><see cref="MeetingDelegate"/> the meeting delegate for the current meeting</param>
    AnyVote,
    /// <summary>
    /// Triggers whenever another player interacts with THIS role
    /// </summary>
    /// <param name="interactor"><see cref="PlayerControl"/> the player starting the interaction</param>
    /// <param name="interaction"><see cref="Interaction"/> the interaction</param>
    Interaction,
    /// <summary>
    /// Triggers whenever another player interacts with any other player
    /// </summary>
    /// <param name="interactor"><see cref="PlayerControl"/> the player starting the interaction</param>
    /// <param name="target"><see cref="PlayerControl"/> the player being interacted with</param>
    /// <param name="interaction"><see cref="Interaction"/> the interaction</param>
    AnyInteraction,
    /// <summary>
    /// Triggers whenever a player sends a chat message. This action cannot be canceled.
    /// </summary>
    /// <param name="sender"><see cref="PlayerControl"/> the player who sent the chat message</param>
    /// <param name="message"><see cref="string"/> the message sent</param>
    /// <param name="state"><see cref="GameState"/> the current state of the game (for checking in meeting)</param>
    /// <param name="isAlive"><see cref="bool"/> if the chatting player is alive</param>
    Chat,
    /// <summary>
    /// Triggers whenever a player leaves the game. This action cannot be canceled
    /// </summary>
    /// <param name="player"><see cref="PlayerControl"/> the player who disconnected</param>
    Disconnect,
    /// <summary>
    /// Triggers when voting session ends. This action cannot be canceled.
    /// <b>IMPORTANT</b><br/>
    /// You CAN modify the meeting delegate at this time to change the results of the meeting. HOWEVER,
    /// modifying the votes will only change what is displayed during the meeting. You MUST also update the exiled player to change
    /// the exiled player, as the votes WILL NOT be recalculated automatically at this point. <see cref="MeetingDelegate.CalculateExiledPlayer"/>
    /// </summary>
    /// <param name="meetingDelegate"><see cref="MeetingDelegate"/> the meeting delegate for the current meeting</param>
    VotingComplete,
    /// <summary>
    /// Triggers when the meeting ends, this does not pass the meeting delegate as at this point everything has been finalized.
    /// <param name="Exiled Player">><see cref="Optional{T}"/> the optional exiled player</param>
    /// <param name="isTie"><see cref="bool"/> a boolean representing if the meeting tied</param>
    /// <param name="player vote counts"><see cref="Dictionary{TKey,TValue}"/> a dictionary containing (byte, int) representing the amount of votes a player got</param>
    /// <param name="playerVoteStatus"><see cref="Dictionary{TKey,TValue}"/> a dictionary containing (byte, List[Optional[byte]] containing the voting statuses of all players)</param>
    /// </summary>
    MeetingEnd,
    /// <summary>
    /// Triggers when a meeting is called
    /// </summary>
    /// <param name="player"><see cref="PlayerControl"/> the player who called the meeting</param>
    /// <param name="deadBody"><see cref="Optional{T}"/> optional <see cref="GameData.PlayerInfo"/> which exists if the meeting was called byt reporting a body</param>
    MeetingCalled
}
[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetColor))]
class SetColorPatch
{
    public static bool IsAntiGlitchDisabled = false;
    public static bool Prefix(PlayerControl __instance, int bodyColor)
    {
        //色変更バグ対策
        if (!AmongUsClient.Instance.AmHost || __instance.CurrentOutfit.ColorId == bodyColor || IsAntiGlitchDisabled) return true;
        return true;
    }
}