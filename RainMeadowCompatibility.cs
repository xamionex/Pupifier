using System;
using System.Collections.Generic;
using System.Linq;
using RainMeadow;

#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS8602 // Dereference of a possibly null reference.

namespace Pupifier
{
    public class PlayerData : OnlineEntity.EntityData
    {
        public bool Grabbable;

        public PlayerData() { }

        public override EntityDataState MakeState(OnlineEntity entity, OnlineResource inResource)
        {
            return new PlayerState(this);
        }

        private class PlayerState : OnlineEntity.EntityData.EntityDataState
        {
            [OnlineField]
            public bool Grabbable;

            public PlayerState() { }
            public PlayerState(PlayerData playerData)
            {
                Grabbable = playerData.Grabbable;
            }

            public override Type GetDataType() => typeof(PlayerData);

            public override void ReadTo(OnlineEntity.EntityData data, OnlineEntity resource)
            {
                var playerData = (PlayerData)data;
                playerData.Grabbable = Grabbable;
            }
        }
    }

    public partial class Pupifier
    {
        public bool Player_CheckGrababilityMeadow(Player player)
        {
            var playerOnlineObject = player.abstractPhysicalObject.GetOnlineObject();
            if (playerOnlineObject.TryGetData<PlayerData>(out var data))
            {
                return data.Grabbable;
            }
            return false;
        }

        public static bool PlayerIsLocal(Player player)
        {
            return player.IsLocal();
        }

        public static bool GameIsMeadow()
        {
            return OnlineManager.lobby != null;
        }

        private void ToggleGrabbable(Player player)
        {
            var playerOnlineObject = player.abstractPhysicalObject.GetOnlineObject();
            if (playerOnlineObject.TryGetData<PlayerData>(out var data))
            {
                data.Grabbable = !Options.DisableBeingGrabbed.Value;
            }
        }

        public void InitPlayerData(Player player)
        {
            var playerOnlineObject = player.abstractPhysicalObject.GetOnlineObject();
            if (!playerOnlineObject.TryGetData<PlayerData>(out var data))
            {
                playerOnlineObject.AddData(new PlayerData());
            }
        }
    }
}