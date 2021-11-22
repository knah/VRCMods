using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using VRC;
using VRC.Core;

namespace LagFreeScreenshots
{
    public class Metadata
    {
        public int ImageRotation;
        public APIUser ApiUser;
        public ApiWorldInstance WorldInstance;
        public Vector3 Position;
        public List<Tuple<Player, Vector3>> PlayerList;

        public Metadata(int imageRotation, APIUser apiUser, ApiWorldInstance apiWorldInstance, Vector3 position, List<Tuple<Player, Vector3>> playerList)
        {
            ImageRotation = imageRotation;
            ApiUser = apiUser;
            WorldInstance = apiWorldInstance;
            Position = position;
            PlayerList = playerList;
        }

        public string ConvertToString()
        {
            var worldString = "null,0,Not in any world";
            if (WorldInstance != null && WorldInstance.world != null)
                worldString = WorldInstance.world.id + "," + WorldInstance.name + "," + WorldInstance.world.name;

            var positionString = Position.x.ToString(CultureInfo.InvariantCulture) + "," + Position.y.ToString(CultureInfo.InvariantCulture) + "," + Position.z.ToString(CultureInfo.InvariantCulture);

            return "lfs|2|author:"
                + ApiUser.id + "," + ApiUser.displayName
                + "|world:" + worldString
                + "|pos:" + positionString
                + (ImageRotation != -1 ? "|rq:" + ImageRotation : "")
                + "|players:" + string.Join(";", PlayerList.ConvertAll(new Converter<Tuple<Player, Vector3>, string>(PlayerListToString)));
        }

        private static string PlayerListToString(Tuple<Player, Vector3> playerData)
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
