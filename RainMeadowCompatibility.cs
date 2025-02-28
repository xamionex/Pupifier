using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RainMeadow;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace Pupifier
{
    [HarmonyPatch(typeof(MeadowPlayerController))]
    public static class MeadowPlayerControllerPatches
    {
        [HarmonyPatch("Player_Update")]
        [HarmonyPostfix]
        private static void Postfix_Player_Update(Player self, bool eu)
        {
            if (!Pupifier.Options.EnableInMeadowGamemode.Value) return;
            self.playerState.isPup = Pupifier.Instance.slugpupEnabled;
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

    public partial class Pupifier
    {
        public bool Player_CheckGrababilityMeadow(Player player)
        {
            OnlinePlayer onlinePlayer = player.abstractPhysicalObject.GetOnlineObject().owner;
            if (PlayerData.Ungrabbables.Contains(onlinePlayer)) return false;
            return true;
        }

        public static bool PlayerIsLocal(Player player)
        {
            return player.IsLocal();
        }

        public static bool GameIsMeadow()
        {
            return OnlineManager.lobby != null;
        }

        public static bool GamemodeIsMeadow()
        {
            if (GameIsMeadow())
            {
                if (Options.EnableInMeadowGamemode.Value) return false;
                return OnlineManager.lobby.gameMode is MeadowGameMode;
            }
            return false;
        }

        private void ToggleGrabbable(Player player)
        {
            OnlinePlayer onlinePlayer = player.abstractPhysicalObject.GetOnlineObject().owner;
            if (Options.DisableBeingGrabbed.Value)
            {
                if (PlayerData.Ungrabbables.Contains(onlinePlayer)) return;
                foreach (var participant in OnlineManager.lobby.participants)
                {
                    participant.InvokeRPC(JoinUngrabbable, OnlineManager.mePlayer);
                }
                Log("Joined ungrabbables");
            }
            else
            {
                if (!PlayerData.Ungrabbables.Contains(onlinePlayer)) return;
                foreach (var participant in OnlineManager.lobby.participants)
                {
                    participant.InvokeRPC(LeaveUngrabbable);
                }
                Log("Left ungrabbables");
            }
        }

        [RainMeadow.RPCMethod]
        public static void JoinUngrabbable(RPCEvent rpcEvent, OnlinePlayer newUngrabbable)
        {
            if (!PlayerData.Ungrabbables.Contains(newUngrabbable))
            {
                Log($"Got RPC and added new ungrabbable player: {newUngrabbable}");
                PlayerData.Ungrabbables.Add(newUngrabbable);
                OnlineManager.lobby.NewVersion();
            }
        }

        [RainMeadow.RPCMethod]
        public static void LeaveUngrabbable(RPCEvent rpcEvent)
        {
            Log($"Got RPC event and removed ungrabbable player: {rpcEvent.from}");
            PlayerData.Ungrabbables.Remove(rpcEvent.from);
            OnlineManager.lobby.NewVersion();
        }
    }
}