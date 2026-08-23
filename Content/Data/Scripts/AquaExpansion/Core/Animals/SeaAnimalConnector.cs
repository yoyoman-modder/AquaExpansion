using Sandbox.Game;
using Sandbox.Game.Entities;
using System;
using System.Collections.Generic;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;

namespace AquaExpansionExperimental.Core.Animals
{
    [MyStatLogicDescriptor("SeaAnimalConnector")]
    public class SeaAnimalConnector : MyStatLogic
    {
        private static MyStringHash SeaAnimalConnectorId = MyStringHash.GetOrCompute("SeaAnimalBridge");
        private bool ready = false;
        private BioLatentScheduler Biobuffer;
        private Dictionary<string, Func<MyGameLogicComponent>> SeaAnimalsLogicData = new Dictionary<string, Func<MyGameLogicComponent>>();
        public MyEntityStat SeaAnimalBridge
        {
            get 
            {
                MyEntityStat animalbridge;
                if (this.m_stats.TryGetValue(SeaAnimalConnectorId, out animalbridge))
                {
                    return animalbridge;
                }
                return animalbridge;
            }
        }

        public override void Init(IMyCharacter character, Dictionary<MyStringHash, MyEntityStat> stats, string scriptName)
        {
            base.Init(character, stats, scriptName);
            MyEntityStat animalbridge = this.SeaAnimalBridge;
            if (animalbridge != null)
            {
                if (animalbridge.MaxValue == 1)
                {
                    ConstructSeaAnimalData();
                }
                if (animalbridge.MaxValue == 2)
                {
                    ConstructModSeaAnimalData();
                }
            }
            Biobuffer = new BioLatentScheduler();
        }

        private void ConnectToAnimal()
        {
            if (ready)
                return;
            ready = true;
            Biobuffer.Schedule(GetSeaAnimal, 2);

        }

        private void GetSeaAnimal()
        {
            if (base.Character != null && !base.Character.Closed && !base.Character.IsDead)
            {
                //AquaExpansionSession.Insance.Log(true, $"SeaAnimalBridge initialized for {base.Character.DisplayName}");
                var bot = base.Character;
                if (bot != null)
                {
                    AddAnimalComponent(bot, bot.Definition.Id.SubtypeId.ToString());
                }
            }
        }

        private void ConstructSeaAnimalData()
        {
            SeaAnimalsLogicData.Add("AquaWhiteShark", () => new SeaCreatureWhiteShark());
        }

        private void ConstructModSeaAnimalData()
        {
            
        }

        private void AddAnimalComponent(IMyCharacter animal, string typekey)
        {
            if (animal == null || animal.MarkedForClose || animal.IsDead)
                return;
            if (string.IsNullOrEmpty(typekey))
                return;
            Func<MyGameLogicComponent> Seafactory;
            if (!SeaAnimalsLogicData.TryGetValue(typekey, out Seafactory))
            {
                //AquaExpansionSession.Insance.Log(true, $"No component factory for key '{typekey}'");
                return;
            }
            var existing = animal.GameLogic.GetAs<SeaCreatureBase>();
            if (existing != null)
            {
                //AquaExpansionSession.Insance.Log(true, $"Already has {existing.GetType().Name}");
                return;
            }
            var newComp = Seafactory();
            if (newComp != null)
            {
                animal.Components.Add(newComp);
                var builder = new MyObjectBuilder_EntityBase();
                newComp.Init(builder);
                //AquaExpansionSession.Insance.Log(true, $"Attached {newComp.GetType().Name} to {animal.Definition.Id.SubtypeName}");
            }
        }

        public override void Update()
        {
            ConnectToAnimal();
            Biobuffer.Update();
            base.Update();
        }

        public override void Close()
        {
            MyEntityStat animalbridge = this.SeaAnimalBridge;
            if (animalbridge != null)
            {
                
            }
            Biobuffer = null;
            base.Close();
        }
    }
}
