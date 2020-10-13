using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using VRage.Game;
using VRageMath;

namespace avaness.GridSpawner
{
    public static class Constants
    {
        public static bool IsServer => MyAPIGateway.Session.IsServer || MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE;
        public static bool IsDedicated => IsServer && MyAPIGateway.Utilities.IsDedicated;
        public static bool IsPlayer => !IsDedicated;
        public static bool IsClient => !IsServer;
        public const string msgNoSpace = "There is not enough room to spawn that.";
        public const string msgDifferentSpace = "Area occupied, projection will be spawned nearby.";
        public const string msgTime = "Projection will be spawned in ";
        public const string msgMissingComp = " components are needed to build that.";
        public const string msgNoGrid = "No projection.";
        public const string msgBuilding = "Projector is busy building a projection.";
        public const string msgWaiting = "Projector is busy waiting to build a projection.";
        public const string msgError = "An unknown error occurred while spawning the projection. Code: ";
        public const string msgSubgrids = "An error occurred while spawning the subgrids. Code: ";
        public const string msgGridSmall = "Projection is too small to be built.";
        public const string msgGridLarge = "Projection is too large to be built.";
        public const string msgUnknownBlock = "Projection contains an unknown block.";
        public const string msgCanceled = "Projection spawn was canceled.";
        public static readonly Random rand = new Random();
        public static readonly Guid Storage = new Guid("9AF39300-CC9E-47C1-A7E1-5DC47DF97A1E");
        public const int checkProjectionRate = 3600; // 60 seconds * 60 ticks
        public const double maxNewDist2 = 1000000;

        public const int minSpeed = 1;
        public const int maxSpeed = 100;
        public const float speedEnergyScale = 6; // Energy in MW = minPower + scale * (speed - 1);

        internal const string mapFile = "InstantProjector-Settings.xml";

        public static void Notify(string msg, ulong steamId, int seconds = 5)
        {
            if(steamId != 0)
            {
                long id2 = MyAPIGateway.Players.TryGetIdentityId(steamId);
                if (id2 != 0)
                    MyVisualScriptLogicProvider.ShowNotification(msg, seconds * 1000, "White", id2);
            }
        }

        public static long RandomLong ()
        {
            byte [] bytes = new byte [8];
            rand.NextBytes(bytes);
            return BitConverter.ToInt64(bytes, 0);
        }

        public static bool RandomEntityId (out long id)
        {
            for (int i = 0; i < 100; i++)
            {
                id = RandomLong();
                if (!MyAPIGateway.Entities.EntityExists(id))
                    return true;
            }
            id = 0;
            return false;
        }

        public static MatrixD LocalToWorld(MatrixD local, MatrixD reference)
        {
            return local * reference;
        }

        public static MatrixD WorldToLocalNI(MatrixD world, MatrixD referenceInverted)
        {
            return world * referenceInverted;
        }

        public static MatrixD WorldToLocal(MatrixD world, MatrixD referenceInverted)
        {
            return world * MatrixD.Normalize(MatrixD.Invert(referenceInverted));
        }

        /*public static Quaternion LocalToWorld(Quaternion local, Quaternion reference)
        {
            return reference * local;
        }

        public static Quaternion WorldToLocal(Quaternion world, Quaternion reference)
        {
            return Quaternion.Inverse(reference) * world;
        }

        public static Quaternion WorldToLocalNI(Quaternion world, Quaternion referenceInverted)
        {
            return referenceInverted * world;
        }*/

    }
}
