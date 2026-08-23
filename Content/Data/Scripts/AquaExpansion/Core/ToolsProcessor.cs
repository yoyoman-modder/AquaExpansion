using Jakaria.API;
using Sandbox.ModAPI.Weapons;
using System.Collections.Generic;
using VRage.Game;
using VRageMath;

namespace AquaExpansion.Core
{
    public static class ToolsProcessor
    {
        private static bool isShoot(IMyWelder welder)
        {
            return welder != null &&
              welder.IsShooting;
        }
        public static void UpdateHandWelders(HashSet<IMyWelder> handwelders, Dictionary<long, MyParticleEffect> effects)
        {
            if (handwelders == null)
                return;
            foreach (IMyWelder welder in handwelders)
            {
                if (welder == null ||
                    welder.Closed ||
                    welder.MarkedForClose)
                    continue;
                Update(welder,effects);
            }
        }
        private static void Update(IMyWelder welder, Dictionary<long, MyParticleEffect> effects)
        {
            if (welder == null ||
            welder.Closed ||
            welder.MarkedForClose)
                return;
            AquaHantToolWelderHandler handler = welder.GameLogic.GetAs<AquaHantToolWelderHandler>();
            if (handler == null)
                return;
            Vector3D muzzlePosition = welder.GetMuzzlePosition();
            // Welder muzzle is no longer underwater.
            if (!WaterModAPI.IsUnderwater(muzzlePosition))
            {
                GlobalEffects.StopToolEffect(
                    welder.EntityId,
                    effects);
                return;
            }
            // Stop the looping effect when the player
            // releases the welder.
            if (!isShoot(welder))
            {
                GlobalEffects.StopToolEffect(
                    welder.EntityId,
                    effects);
                return;
            }
            // Create only once. The particle follows the
            // welder render object while shooting.
            GlobalEffects.CreateToolEffect(
                welder,
                GlobalEffects.GetWelderEffect("WelderSmall"),
                1f,
                effects);
        }
    }
}
