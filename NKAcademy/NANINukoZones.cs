using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace ChiirinAcademyNormalZone
{
    public class Zone_chiirin_academy_normal : Zone_Civilized
    {
        public Zone_chiirin_academy_normal()
        {
            try
            {
                var field = typeof(Zone_Civilized)
                    .GetField("isMainZone", BindingFlags.Instance | BindingFlags.NonPublic);

                field?.SetValue(this, true);
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to set isMainZone: " + ex);
            }
        }

        public override bool ShouldRegenerate => false;
        public override bool isClaimable => false;
        public override bool HiddenInRegionMap => true;
        public override bool AllowCriminal => true;
        public override bool CanSpawnAdv => false;
        public override bool CanFastTravel => false;
        public override bool CanBeDeliverDestination => false;
        public override bool IsReturnLocation => false;

        public override bool RestrictBuild => false;
        public override bool LockExit => false;
        public override bool BlockBorderExit => false;

        public override bool IsFestival
        {
            get
            {
                var flags = EClass.player?.dialogFlags;
                return flags != null
                    && flags.TryGetValue("naninu.academy.festival", out int value)
                    && value != 0;
            }
        }

        public override string IDBaseLandFeat => "bfHill,bfCoal,bfRuin";

        public override ZoneTransition.EnterState RegionEnterState =>
            ZoneTransition.EnterState.Return;

        public override string pathExport =>
            Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "Maps",
                this.idExport + ".z"
            );
    }

    public class Zone_chiirin_academy_dorm : Zone_Civilized
    {
        public Zone_chiirin_academy_dorm()
        {
            try
            {
                var field = typeof(Zone_Civilized)
                    .GetField("isMainZone", BindingFlags.Instance | BindingFlags.NonPublic);

                field?.SetValue(this, true);
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to set isMainZone: " + ex);
            }
        }

        public override bool ShouldRegenerate => false;
        public override bool isClaimable => false;
        public override bool HiddenInRegionMap => true;
        public override bool AllowCriminal => true;
        public override bool CanSpawnAdv => false;
        public override bool CanFastTravel => false;
        public override bool CanBeDeliverDestination => false;
        public override bool IsReturnLocation => false;

        public override bool RestrictBuild => false;
        public override bool LockExit => true;
        public override bool BlockBorderExit => true;

        public override string IDBaseLandFeat => "bfHill,bfCoal,bfRuin";

        public override ZoneTransition.EnterState RegionEnterState =>
            ZoneTransition.EnterState.Return;

        public override string pathExport =>
            Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "Maps",
                this.idExport + ".z"
            );
    }
    public class Zone_chiirin_academy_riverban : Zone_Civilized
    {
        public Zone_chiirin_academy_riverban()
        {
            try
            {
                var field = typeof(Zone_Civilized)
                    .GetField("isMainZone", BindingFlags.Instance | BindingFlags.NonPublic);

                field?.SetValue(this, true);
            }
            catch (Exception ex)
            {
                Debug.LogError("Failed to set isMainZone: " + ex);
            }
        }

        public override bool ShouldRegenerate => false;
        public override bool isClaimable => false;
        public override bool HiddenInRegionMap => true;
        public override bool AllowCriminal => true;
        public override bool CanSpawnAdv => false;
        public override bool CanFastTravel => false;
        public override bool CanBeDeliverDestination => false;
        public override bool IsReturnLocation => false;

        public override bool RestrictBuild => false;
        public override bool LockExit => true;
        public override bool BlockBorderExit => true;

        public override string IDBaseLandFeat => "bfHill,bfCoal,bfRuin";

        public override ZoneTransition.EnterState RegionEnterState =>
            ZoneTransition.EnterState.Return;

        public override string pathExport =>
            Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "Maps",
                this.idExport + ".z"
            );
    }
}