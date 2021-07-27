using Sandbox.ModAPI;
using System;
using VRage.Game;
using VRage.Utils;

namespace avaness.GridSpawner
{
    public static class Constants
    {
        public static bool IsServer => MyAPIGateway.Session.IsServer;
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
        public const string msgScale = "Projection scale must be 100% before spawning.";
        public static readonly Random rand = new Random();
        public static readonly Guid Storage = new Guid("9AF39300-CC9E-47C1-A7E1-5DC47DF97A1E");
        public const int checkProjectionRate = 3600; // 60 seconds * 60 ticks
        public const double maxNewDist2 = 1000000;

        public const int minSpeed = 1;
        public const int maxSpeed = 100;

        internal const string mapFile = "InstantProjector-Settings.xml";
        
        public static readonly MyStringHash DefenseShieldId = MyStringHash.GetOrCompute("DefenseShield");
    }
}
