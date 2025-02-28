using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RainMeadow;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace Pupifier;

[HarmonyPatch(typeof(MeadowPlayerController), "Player_Update")]
public static class MeadowPlayerControllerPatches
{
    private static bool GetIsPupValue()
    {
        return Pupifier.Options.EnableInMeadowGamemode.Value && Pupifier.Instance.slugpupEnabled;
    }
        
    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        // stfld // Set Field
        // ldarg.1 // Player
        // callvirt // PlayerState.isPup
        // ldc.i4.0 // False
        var codes = new List<CodeInstruction>(instructions);
        FieldInfo isPupField = AccessTools.Field(typeof(PlayerState), nameof(PlayerState.isPup));

        for (int i = 0; i < codes.Count; i++)
        {
            // Check if the current instruction is storing to PlayerState.isPup
            if (codes[i].opcode == OpCodes.Stfld && codes[i].operand as FieldInfo == isPupField)
            {
                // Check if the previous instruction is loading 'false' (ldc.i4.0)
                if (i > 0 && codes[i - 1].opcode == OpCodes.Ldc_I4_0)
                {
                    // Replace loading 'false' with a call to GetIsPupValue
                    codes[i - 1] = new CodeInstruction(OpCodes.Call, 
                        AccessTools.Method(typeof(MeadowPlayerControllerPatches), nameof(GetIsPupValue)));
                    break;
                }
            }
        }

        return codes;
    }
}

public class PlayerData : OnlineResource.ResourceData
{
    public List<OnlinePlayer> Ungrabbables = new();

    public PlayerData() { }

    public override ResourceDataState MakeState(OnlineResource resource)
    {
        return new PlayerState(this);
    }

    private class PlayerState : ResourceDataState
    {
        [OnlineField(nullable = true)]
        RainMeadow.Generics.DynamicUnorderedUshorts _ungrabbables;

        public PlayerState() { }
        public PlayerState(PlayerData playerData)
        {
            _ungrabbables = new(playerData.Ungrabbables.Select(p => p.inLobbyId).ToList());
        }

        public override Type GetDataType() => typeof(PlayerData);

        public override void ReadTo(OnlineResource.ResourceData data, OnlineResource resource)
        {
            PlayerData playerData = (PlayerData)data;
            playerData.Ungrabbables = _ungrabbables.list.Select(i => OnlineManager.lobby.PlayerFromId(i)).Where(p => p != null).ToList();
        }
    }
}

public class PupifierMeadowCompat
{
    public static PlayerData PlayerData;
    public static bool Initialized;
    public static void Initialize()
    {
        if (!Initialized)
        {
            Pupifier.Log("Running Compatibility Patches");
            Pupifier.HarmonyInstance.PatchAll(typeof(MeadowPlayerControllerPatches));
            PlayerData = new PlayerData();
            Initialized = true;
        }
    }
    
    public static bool Player_CheckGrababilityMeadow(Player player)
    {
        Initialize();
        OnlinePlayer onlinePlayer = player.abstractPhysicalObject.GetOnlineObject().owner;
        if (PlayerData.Ungrabbables.Contains(onlinePlayer)) return false;
        return true;
    }

    public static bool PlayerIsLocal(Player player)
    {
        Initialize();
        return player.IsLocal();
    }

    public static bool GameIsMeadow()
    {
        Initialize();
        return OnlineManager.lobby != null;
    }

    public static bool GamemodeIsMeadow()
    {
        Initialize();
        if (GameIsMeadow())
        {
            if (Pupifier.Options.EnableInMeadowGamemode.Value) return false;
            return OnlineManager.lobby.gameMode is MeadowGameMode;
        }
        return false;
    }

    public static void ToggleGrabbable(Player player)
    {
        Initialize();
        OnlinePlayer onlinePlayer = player.abstractPhysicalObject.GetOnlineObject().owner;
        if (Pupifier.Options.DisableBeingGrabbed.Value)
        {
            if (PlayerData.Ungrabbables.Contains(onlinePlayer)) return;
            foreach (var participant in OnlineManager.lobby.participants)
            {
                participant.InvokeRPC(JoinUngrabbable, OnlineManager.mePlayer);
            }
            Pupifier.Log("Joined ungrabbables");
        }
        else
        {
            if (!PlayerData.Ungrabbables.Contains(onlinePlayer)) return;
            foreach (var participant in OnlineManager.lobby.participants)
            {
                participant.InvokeRPC(LeaveUngrabbable);
            }
            Pupifier.Log("Left ungrabbables");
        }
    }

    [RPCMethod]
    public static void JoinUngrabbable(RPCEvent rpcEvent, OnlinePlayer newUngrabbable)
    {
        Initialize();
        if (!PlayerData.Ungrabbables.Contains(newUngrabbable))
        {
            Pupifier.Log($"Got RPC and added new ungrabbable player: {newUngrabbable}");
            PlayerData.Ungrabbables.Add(newUngrabbable);
            OnlineManager.lobby.NewVersion();
        }
    }

    [RPCMethod]
    public static void LeaveUngrabbable(RPCEvent rpcEvent)
    {
        Initialize();
        Pupifier.Log($"Got RPC event and removed ungrabbable player: {rpcEvent.from}");
        PlayerData.Ungrabbables.Remove(rpcEvent.from);
        OnlineManager.lobby.NewVersion();
    }
}