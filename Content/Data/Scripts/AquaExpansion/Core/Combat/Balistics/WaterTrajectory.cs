using Jakaria.API;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace AquaExpansion.Core.Combat.Balistics
{
    public static class WaterTrajectory
    {
       public static WaterTrajectoryResult Calculate(MyPlanet planet, Vector3D origin, Vector3D direction, double maxDistance)
       {
            WaterTrajectoryResult result = new WaterTrajectoryResult();
            Vector3D end = origin + direction * maxDistance;
            // Limit trajectory by first blocking obstacle
            FindFirstBlockingHit(origin, end, out end);
            result.TotalDistance = Vector3D.Distance(origin, end);
            if (planet == null || !WaterModAPI.HasWater(planet))
            {
                result.Type = WaterTrajectoryType.Air;
                result.AirDistance = result.TotalDistance;
                return result;
            }
            LineD path = new LineD(origin, end);
            switch (WaterModAPI.LineIntersectsWater(path, planet))
            {
                case 0:
                    CalculateAir(ref result);
                    break;
                case 1:
                    CalculateExitWater(planet, origin, end, ref result);
                    break;
                case 2:
                    CalculateEnterWater(planet, origin, end, ref result);
                    break;
                case 3:
                    CalculateUnderwater(ref result);
                    break;
            }
            CalculateAverageDepth(planet, origin, end, ref result);
            return result;
        }
        private static void CalculateAir(ref WaterTrajectoryResult result)
        {
            result.Type = WaterTrajectoryType.Air;
            result.AirDistance = result.TotalDistance;
            result.WaterDistance = 0;
        }
        private static void CalculateUnderwater(ref WaterTrajectoryResult result)
        {
            result.Type = WaterTrajectoryType.Underwater;
            result.AirDistance = 0;
            result.WaterDistance = result.TotalDistance;
        }
        private static void CalculateEnterWater(MyPlanet planet, Vector3D origin, Vector3D hitPosition, ref WaterTrajectoryResult result)
        {
            result.Type = WaterTrajectoryType.EnteredWater;
            var physical = WaterModAPI.GetPhysical(planet);
            Vector3D center = physical.Item1;
            double radius = physical.Item2;
            Vector3D entry;
            TryIntersectSphere(origin,hitPosition,center,radius,WaterIntersectionType.Entry,out entry);
            result.EntryPoint = entry;
            result.AirDistance = Vector3D.Distance(origin, entry);
            result.WaterDistance = Vector3D.Distance(entry, hitPosition);
        }
        private static void CalculateExitWater(MyPlanet planet,Vector3D origin,Vector3D hitPosition,ref WaterTrajectoryResult result)
        {
            result.Type = WaterTrajectoryType.ExitedWater;
            var physical = WaterModAPI.GetPhysical(planet);
            Vector3D center = physical.Item1;
            double radius = physical.Item2;
            Vector3D exit;
            TryIntersectSphere(origin,hitPosition,center,radius,WaterIntersectionType.Exit,out exit);
            result.ExitPoint = exit;
            result.WaterDistance = Vector3D.Distance(origin, exit);
            result.AirDistance = Vector3D.Distance(exit, hitPosition);
        }
        private static bool TryIntersectSphere(Vector3D start, Vector3D end, Vector3D center, double radius, WaterIntersectionType type, out Vector3D intersection)
        {
            intersection = Vector3D.Zero;
            Vector3D direction = end - start;
            double length = direction.Length();
            if (length <= 1e-6)
                return false;
            direction /= length;
            Vector3D m = start - center;
            double b = Vector3D.Dot(m, direction);
            double c = Vector3D.Dot(m, m) - radius * radius;
            double discriminant = b * b - c;
            if (discriminant < 0.0)
                return false;
            double sqrt = Math.Sqrt(discriminant);
            double tNear = -b - sqrt;
            double tFar = -b + sqrt;
            double t = (type == WaterIntersectionType.Entry) ? tNear : tFar;
            if (t < 0.0 || t > length)
                return false;
            intersection = start + direction * t;
            return true;
        }
        private static void CalculateAverageDepth(MyPlanet planet,Vector3D origin,Vector3D hitPosition,ref WaterTrajectoryResult result)
        {
            if (result.WaterDistance <= 0)
                return;
            Vector3D sample;
            switch (result.Type)
            {
                case WaterTrajectoryType.EnteredWater:
                    sample = Vector3D.Lerp(result.EntryPoint, hitPosition, 0.5);
                    break;
                case WaterTrajectoryType.ExitedWater:
                    sample = Vector3D.Lerp(origin, result.ExitPoint, 0.5);
                    break;
                case WaterTrajectoryType.Underwater:
                    sample = Vector3D.Lerp(origin, hitPosition, 0.5);
                    break;
                default:
                    return;
            }
            float? depth = WaterModAPI.GetDepth(sample, planet);
            result.AverageDepth = depth ?? 0f;
        }
        private static bool FindFirstBlockingHit(Vector3D origin, Vector3D end, out Vector3D hitPosition)
        {
            hitPosition = end;
            IHitInfo hitInfo;
            if (!MyAPIGateway.Physics.CastRay(origin, end, out hitInfo))
                return false;
            IMyEntity entity = hitInfo.HitEntity;
            // Ignore anything that isn't a real obstacle
            if (entity != null)
            {
                if (!(entity is IMyCubeGrid) &&
                    !(entity is MyVoxelBase) &&
                    !(entity is IMyCharacter) &&
                    !(entity is MyFloatingObject))
                {
                    return false;
                }
            }
            hitPosition = hitInfo.Position;
            return true;
        }
    }
}
   
