using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using VRC;
using VRC.Core;

namespace LagFreeScreenshots.API
{
    public class MetadataV2
    {
        public readonly ScreenshotRotation ImageRotation;
        public readonly APIUser ApiUser;
        public readonly ApiWorldInstance WorldInstance;
        public Vector3 Position;
        public readonly List<(Player, Vector3)> PlayerList;

        public MetadataV2(ScreenshotRotation imageRotation, APIUser apiUser, ApiWorldInstance apiWorldInstance, Vector3 position, List<(Player, Vector3)> playerList)
        {
            ImageRotation = imageRotation;
            ApiUser = apiUser;
            WorldInstance = apiWorldInstance;
            Position = position;
            PlayerList = playerList;
        }

        public override string ToString()
        {
            var worldString = "null,0,Not in any world";
            if (WorldInstance != null && WorldInstance.world != null)
                worldString = WorldInstance.world.id + "," + WorldInstance.name + "," + WorldInstance.world.name;

            var positionString = Position.x.ToString(CultureInfo.InvariantCulture) + "," + Position.y.ToString(CultureInfo.InvariantCulture) + "," + Position.z.ToString(CultureInfo.InvariantCulture);

            return "lfs|2|author:"
                + ApiUser.id + "," + ApiUser.displayName
                + "|world:" + worldString
                + "|pos:" + positionString
                + (ImageRotation != ScreenshotRotation.NoRotation ? "|rq:" + ImageRotation : "")
                + "|players:" + string.Join(";", PlayerList.Select(PlayerListToString));
        }

        private static string PlayerListToString((Player, Vector3) playerData)
        {
            if (playerData.Item1 == null || playerData.Item1.prop_APIUser_0 == null) return "null,0,0,0,null";
            return playerData.Item1.prop_APIUser_0.id + "," +
                       playerData.Item2.x.ToString("0.00", CultureInfo.InvariantCulture) + "," +
                       playerData.Item2.y.ToString("0.00", CultureInfo.InvariantCulture) + "," +
                       playerData.Item2.z.ToString("0.00", CultureInfo.InvariantCulture) + "," +
                       playerData.Item1.prop_APIUser_0.displayName;
        }
    }
}