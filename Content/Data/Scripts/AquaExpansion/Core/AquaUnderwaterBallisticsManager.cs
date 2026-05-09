using VRage.Game;
using VRage.Game.Components;

namespace AquaExpansion.Core
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class AquaUnderwaterBallisticsManager :MySessionComponentBase
    {
        public static AquaUnderwaterBallisticsManager Instance;
        public override void LoadData()
        {
            Instance = this;
            base.LoadData();
        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);
        }

        public override void UpdateBeforeSimulation()
        {
            
            base.UpdateBeforeSimulation();
        }

        protected override void UnloadData()
        {
            Instance = null;
            base.UnloadData();
        }
    }
}
