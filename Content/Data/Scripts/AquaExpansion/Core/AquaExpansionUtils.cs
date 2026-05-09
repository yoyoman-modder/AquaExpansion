using ProtoBuf;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using VRage;
using VRage.Game;
using VRage.Game.Entity;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace AquaExpansion.Core
{
    public enum AquaFarmBlockStage { Iddle, Planting, Growing, Harvestable, Full }

    public enum AquaFarmingBlockType { UnderwaterFarmPlot, FishingBlock, FishingBlockAdvance }

    public enum AquaFishBlockStage { Idlle, Atracting, Catching, CatchResult, Full };

    public enum AquaOceanLayer { Surface, Shallow, Mid, Deep, Abyss};

    public enum AquaDefinitionOB { Component, ConsumableItem };

    /// <summary>
    /// Main utilite class
    /// </summary>
    public class AquaExpansionUtils
    {
        public List<AquaFarmingRecipe> Recipes = new List<AquaFarmingRecipe>();
        private HashSet<string> SporeSubtypes = new HashSet<string>();
        private HashSet<string> SeaweedSubtypes = new HashSet<string>();
        private string ErrorText = "Error";
        private string EmptyplantText = "No Plant";
        private string EmptyfishText = "No Fish";
        private int zeroed = 0;
        public event Action<int> ModelStageChanged;
        public event Action<int> FishModelStageChanged;
        public event Action<int> InventoryFishShow;
        private HashSet<string> BaitSubtypes = new HashSet<string>();
        private HashSet<string> FishSubtypes = new HashSet<string>();
        private float blocksubmodelLod1distSq = 20f * 20f;
        private readonly HashSet<string> inventorymodelpaths = new HashSet<string>();
        public readonly List<string> inventorymodelpathsList = new List<string>();
        private bool onetimereloadbait = true;
        /// <summary>
        /// Change model event
        /// </summary>
        /// <param name="stage"></param>
        public void OnModelStageChanged(int stage)
        {
            ModelStageChanged?.Invoke(stage);
        }

        /// <summary>
        /// Change fish model event
        /// </summary>
        /// <param name="stage"></param>
        public void OnFishModelStageChanged(int stage)
        {
            FishModelStageChanged?.Invoke(stage);
        }

        /// <summary>
        /// Change inventory fish model event
        /// </summary>
        /// <param name="stage"></param>
        public void OnInventoryFishShowStageChanged(int stage)
        {
            InventoryFishShow?.Invoke(stage);
        }

        /// <summary>
        /// Model optimization
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="block"></param>
        public void ModelOptimization(MyEntity entity, IMyFunctionalBlock block)
        {
            if (block == null || entity == null)
                return;
            if (block.Closed || block.MarkedForClose || entity.MarkedForClose)
                return;
            if (!block.Enabled || !block.IsFunctional || !entity.InScene)
            {
                entity.Render.Visible = false;
                return;
            }
            var player = MyAPIGateway.Session?.Player;
            if (player == null)
                return;
            double distanceSq = Vector3D.DistanceSquared(player.GetPosition(), block.GetPosition());
            bool shouldBeVisible = distanceSq <= blocksubmodelLod1distSq;
            if (entity.Render.Visible != shouldBeVisible)
                entity.Render.Visible = shouldBeVisible;
        }

        /// <summary>
        /// Show inventory fish model
        /// </summary>
        /// <param name="count"></param>
        /// <param name="time"></param>
        public void ShowInventoryFish(int count, ref float time)
        {
            if (count == 0 || inventorymodelpathsList == null || inventorymodelpathsList.Count == 0)
            { OnInventoryFishShowStageChanged(-1); return; }
            float timer = 10f;
            float delta = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            float SpeedMult = delta * 10f; // Update10() sync speeed
            time += SpeedMult / timer;
            Math.Min(time, 1f);
            if (time >= 1f)
            {
                time = 0f;
                int randIndex = MyUtils.GetRandomInt(inventorymodelpathsList.Count);
                string selectedModel = inventorymodelpathsList[randIndex];
                //AquaExpansionSession.Insance.Log(true, $"model {selectedModel}");
                OnInventoryFishShowStageChanged(randIndex);
            }
        }

        /// <summary>
        /// Add model to list and hashset
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private bool AddModel(string model)
        {
            if (string.IsNullOrEmpty(model))
                return false;
            if (!inventorymodelpaths.Add(model))
                return false;
            inventorymodelpathsList.Add(model);
            return true;
        }

        private string GetModel(int index)
        {
            if (index == -1)
                return null;
            if ((uint)index >= (uint)inventorymodelpathsList.Count)
                return null;

            return inventorymodelpathsList[index];
        }

        /// <summary>
        /// Get Color by Status
        /// </summary>
        /// <param name="stage"></param>
        /// <returns></returns>
        private Color GeStatusColor(AquaFarmBlockStage stage)
        {
            Color statusColor = Color.White;
            switch (stage)
            {
                case AquaFarmBlockStage.Iddle:
                    statusColor = Color.White;
                    break;
                case AquaFarmBlockStage.Growing:
                    statusColor = Color.Yellow;
                    break;
                case AquaFarmBlockStage.Harvestable:
                    statusColor = Color.Green;
                    break;
                case AquaFarmBlockStage.Planting:
                    statusColor = Color.Blue;
                    break;
                case AquaFarmBlockStage.Full:
                    statusColor = Color.HotPink;
                    break;
            }
            return statusColor;
        }

        /// <summary>
        /// Get Color by Fishing block status
        /// </summary>
        /// <param name="stage"></param>
        /// <returns></returns>
        private Color GetFishingStatusColor(AquaFishBlockStage stage)
        {
            Color statusColor = Color.White;
            switch (stage)
            {
                case AquaFishBlockStage.Idlle:
                    statusColor = Color.White;
                    break;
                case AquaFishBlockStage.Atracting:
                    statusColor = Color.Blue;
                    break;
                case AquaFishBlockStage.Catching:
                    statusColor = Color.Yellow;
                    break;
                case AquaFishBlockStage.CatchResult:
                    statusColor = Color.Green;
                    break;
                case AquaFishBlockStage.Full:
                    statusColor = Color.HotPink;
                    break;
            }
            return statusColor;
        }

        /// <summary>
        /// Set Emissive by Status
        /// </summary>
        /// <param name="block"></param>
        /// <param name="stage"></param>
        /// <param name="emissivity"></param>
        private void SetEmissivebyStatus(IMyFunctionalBlock block, AquaFarmBlockStage stage, float emissivity)
        {
            if (block == null || !block.Enabled || !block.IsFunctional || block.Closed || block.MarkedForClose)
                return;
            block.SetEmissiveParts("Emissive", GeStatusColor(stage), emissivity);
        }

        /// <summary>
        /// Get emissive depth factor
        /// </summary>
        /// <param name="depth"></param>
        /// <returns></returns>
        private float GetDepthFactor(float depth)
        {
            return MathHelper.Clamp(1f - depth * 0.08f, 0.05f, 1f);
        }

        /// <summary>
        /// Get wave emissive factor
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="time"></param>
        /// <returns></returns>
        private float GetWaveFactor(Vector3 pos, float time)
        {
            float w =
                (float)Math.Sin(pos.X * 0.06f + time * 1.1f) +
                (float)Math.Sin(pos.Z * 0.05f + time * 0.9f);

            return (w * 0.5f + 0.5f);
        }

        /// <summary>
        /// Get eff emissive power
        /// </summary>
        /// <param name="eff"></param>
        /// <returns></returns>
        private float GetEfficiencyPower(float eff)
        {
            eff = MathHelper.Clamp(eff, 0f, 1f);
            return 0.2f + eff * 1.6f + eff * eff * 0.6f;
        }

        /// <summary>
        /// get emissive pulse
        /// </summary>
        /// <param name="time"></param>
        /// <param name="stageFactor"></param>
        /// <returns></returns>
        private float GetPulse(float time, float stageFactor)
        {
            // early stage = chaotic
            // late stage = smooth

            float speed = 1f + (1f - stageFactor) * 2f;

            float p =
                (float)Math.Sin(time * speed) +
                (float)Math.Sin(time * 0.7f);

            return (p * 0.5f + 0.5f);
        }

        /// <summary>
        /// Set emissiive enviromental color
        /// </summary>
        /// <param name="stageColor"></param>
        /// <param name="eff"></param>
        /// <param name="depth"></param>
        /// <param name="wave"></param>
        /// <param name="stageFactor"></param>
        /// <returns></returns>
        private Color ApplyStageEnvironmentColor(Color stageColor, float eff, float depth, float wave, float stageFactor)
        {
            Vector3 col = stageColor.ToVector3();
            //depth blue bias
            float depthFactor = MathHelper.Clamp(1f - depth * 0.08f, 0f, 1f);
            col *= new Vector3(depthFactor, depthFactor, 1f);

            //wave subtle brightness motion
            col *= (0.85f + wave * 0.3f);

            //efficiency brightness boost
            float effBoost = 0.4f + eff * 1.4f + eff * eff * 0.5f;
            col *= effBoost;

            //early stages slight noisy tint
            float instability = 1f - stageFactor;
            col += new Vector3(instability * 0.05f);
            return new Color(col);
        }

        /// <summary>
        /// Set emissive by enviroment
        /// </summary>
        /// <param name="time"></param>
        /// <param name="block"></param>
        /// <param name="stage"></param>
        /// <param name="eff"></param>
        /// <param name="depth"></param>
        /// <param name="plant"></param>
        /// <param name="growth"></param>
        /// <param name="harvest"></param>
        public void SetEnviromentalEmissive(float time, IMyFunctionalBlock block, AquaFarmBlockStage stage, float eff, float depth, float plant, float growth, float harvest)
        {
            if (block == null || !block.Enabled || !block.IsFunctional || block.Closed || block.MarkedForClose || !block.IsWorking)
                return;
            Vector3 pos = block.GetPosition();
            int s = (int)stage;
            int maxStage = 5;
            float stageFactor = s / (float)maxStage;
            Color stageColor = GeStatusColor(stage);
            float effi = MathHelper.Clamp(eff, 0f, 1f);
            float wave = GetWaveFactor(pos, time);
            float depthFactor = GetDepthFactor(depth);
            float pulse = GetPulse(time, stageFactor);
            float intensity = GetEfficiencyPower(eff) * (0.3f + plant * growth * harvest) * pulse * depthFactor * (0.7f + wave * 0.6f);
            Color finalColor = ApplyStageEnvironmentColor(stageColor, eff, depth, wave, stageFactor);
            block.SetEmissiveParts("Emissive", finalColor, intensity);
        }

        /// <summary>
        /// Set emissive by fish block enviroment
        /// </summary>
        /// <param name="time"></param>
        /// <param name="block"></param>
        /// <param name="stage"></param>
        /// <param name="depth"></param>
        /// <param name="atractc"></param>
        /// <param name="cath"></param>
        /// <param name="catchresult"></param>
        public void SetEnviromentalFishingEmissive(float time, IMyFunctionalBlock block, AquaFishBlockStage stage, float depth, float atractc, float cath, float catchresult)
        {
            if (block == null || !block.Enabled || !block.IsFunctional || block.Closed || block.MarkedForClose || !block.IsWorking)
                return;
            Vector3 pos = block.GetPosition();
            int s = (int)stage;
            int maxStage = 5;
            float stageFactor = s / (float)maxStage;
            Color stageColor = GetFishingStatusColor(stage);
            float effi = 1.0f;
            float wave = GetWaveFactor(pos, time);
            float depthFactor = GetDepthFactor(depth);
            float pulse = GetPulse(time, stageFactor);
            float intensity = effi * (0.3f + atractc * cath * catchresult) * pulse * depthFactor * (0.7f + wave * 0.6f);
            Color finalColor = ApplyStageEnvironmentColor(stageColor, effi, depth, wave, stageFactor);
            block.SetEmissiveParts("Emissive", finalColor, intensity);
        }
        /// Get Status info
        /// </summary>
        /// <param name="info"></param>
        /// <param name="block"></param>
        /// <param name="inv"></param>
        /// <param name="Stage"></param>
        public void StatusInfo(StringBuilder info, IMyFunctionalBlock block, MyInventory inv, AquaFarmBlockStage Stage)
        {
            if (block == null || block.Closed || block.MarkedForClose || inv == null)
                return;
            string status = "";
            if (!block.Enabled)
            { status = "ERROR"; }

            else
            {
                switch (Stage)
                {
                    case AquaFarmBlockStage.Iddle:
                        status = "Idle";
                        break;
                    case AquaFarmBlockStage.Planting:
                        status = "Planting";
                        break;
                    case AquaFarmBlockStage.Growing:
                        status = "Growing";
                        break;
                    case AquaFarmBlockStage.Harvestable:
                        status = "Harvestable";
                        break;
                    case AquaFarmBlockStage.Full:
                        status = "Container is Full";
                        break;
                }
            }
            info.AppendLine($"Status:{status}");
        }

        /// <summary>
        /// Get fish block status info
        /// </summary>
        /// <param name="info"></param>
        /// <param name="block"></param>
        /// <param name="inv"></param>
        /// <param name="stage"></param>
        public void FishingStatusInfo(StringBuilder info, IMyFunctionalBlock block, MyInventory inv, AquaFishBlockStage stage)
        {

            if (block == null || block.Closed || block.MarkedForClose || inv == null)
                return;
            string status = "";
            if (!block.Enabled)
            { status = "ERROR"; }
            else
            {
                switch (stage)
                {
                    case AquaFishBlockStage.Idlle:
                        status = "Idle";
                        break;
                    case AquaFishBlockStage.Atracting:
                        status = "Atracting";
                        break;
                    case AquaFishBlockStage.Catching:
                        status = "Catching";
                        break;
                    case AquaFishBlockStage.CatchResult:
                        status = "Finishing";
                        break;
                    case AquaFishBlockStage.Full:
                        status = "Container is Full";
                        break;
                }
            }
            info.AppendLine($"Status:{status}");
        }

        /// <summary>
        /// Get Current Plant
        /// </summary>
        /// <param name="info"></param>
        /// <param name="block"></param>
        /// <param name="inv"></param>
        /// <param name="recipe"></param>
        public void CurrentPlant(StringBuilder info, IMyFunctionalBlock block, MyInventory inv, AquaFarmingRecipe recipe)
        {
            if (block != null && inv != null)
            {
                if (block.Enabled)
                {
                    if (recipe != null)
                    {
                        info.AppendLine($"Plant:{recipe.Displayname}");
                    }
                    else
                    {
                        info.AppendLine($"Plant: {EmptyplantText}");
                    }
                }
                else
                {
                    info.AppendLine($"Plant: {ErrorText}");
                }
            }
        }

        /// <summary>
        /// Get Current Catching Fish
        /// </summary>
        public void CurrentFish(StringBuilder info, IMyFunctionalBlock block, MyInventory inv, AquaFishingRecipe recipe)
        {
            if (block != null && inv != null)
            {
                if (block.Enabled)
                {
                    if (recipe != null)
                    {
                        info.AppendLine($"Fish:{recipe.Displayname}");
                    }
                    else
                    {
                        info.AppendLine($"Fish: {EmptyfishText}");
                    }
                }
                else
                {
                    info.AppendLine($"Fish: {ErrorText}");
                }
            }
        }

        /// <summary>
        /// Grow Line
        /// </summary>
        /// <param name="info"></param>
        /// <param name="block"></param>
        /// <param name="inv"></param>
        /// <param name="stage"></param>
        /// <param name="grow"></param>
        /// <param name="plant"></param>
        /// <param name="harv"></param>
        public void GrowLine(StringBuilder info, IMyFunctionalBlock block, MyInventory inv, AquaFarmBlockStage stage, int grow, int plant, int harv, float time)
        {
            if (block != null && inv != null)
            {
                if (block.Enabled && stage == AquaFarmBlockStage.Planting && time > 0f)
                { info.AppendLine($"Progress: {plant} %  {GetHMS(time)}"); }
                else if (block.Enabled && stage == AquaFarmBlockStage.Growing && time > 0f)
                { info.AppendLine($"Progress: {grow} % {GetHMS(time)}"); }
                else if (block.Enabled && stage == AquaFarmBlockStage.Harvestable && time > 0f)
                { info.AppendLine($"Progress: {harv} % {GetHMS(time)}"); }
                else { info.AppendLine($"Progress: {zeroed} %"); }
            }
        }

        /// <summary>
        /// ProgressLine
        /// </summary>
        /// <param name="info"></param>
        /// <param name="block"></param>
        /// <param name="inv"></param>
        /// <param name="stage"></param>
        /// <param name="atracting"></param>
        /// <param name="catching"></param>
        /// <param name="result"></param>
        /// <param name="time"></param>
        public void ProgressLine(StringBuilder info, IMyFunctionalBlock block, MyInventory inv, AquaFishBlockStage stage, int atracting, int catching, int result, float time)
        {
            if (block != null && inv != null)
            {
                if (block.Enabled && stage == AquaFishBlockStage.Atracting && time > 0f)
                { info.AppendLine($"Progress: {atracting} %  {GetHMS(time)}"); }
                else if (block.Enabled && stage == AquaFishBlockStage.Catching && time > 0f)
                { info.AppendLine($"Progress: {catching} % {GetHMS(time)}"); }
                else if (block.Enabled && stage == AquaFishBlockStage.CatchResult && time > 0f)
                { info.AppendLine($"Progress: {result} % {GetHMS(time)}"); }
                else { info.AppendLine($"Progress: {zeroed} %"); }
            }
        }

        /// <summary>
        /// Get Storage
        /// </summary>
        /// <param name="info"></param>
        /// <param name="block"></param>
        /// <param name="inv"></param>
        /// <param name="spores"></param>
        /// <param name="storage"></param>
        public void GetStorage(StringBuilder info, IMyFunctionalBlock block, MyInventory inv, int spores, int storage)
        {
            if (block != null && inv != null)
            {
                if (block.Enabled)
                {
                    info.AppendLine($"Spore Storage: {spores} Crop Storage: {storage}");
                }
                else
                {
                    info.AppendLine($"Spore Storage: {zeroed}  Crop Storage:  {zeroed}");
                }
            }
        }

        /// <summary>
        /// Get fish block storage
        /// </summary>
        /// <param name="info"></param>
        /// <param name="block"></param>
        /// <param name="inv"></param>
        /// <param name="baits"></param>
        /// <param name="fishes"></param>
        public void GetFishingStorage(StringBuilder info, IMyFunctionalBlock block, MyInventory inv, int baits, int fishes)
        {
             if (block != null && inv != null)
            {
                if (block.Enabled)
                {
                    info.AppendLine($"Bait Storage: {baits} Fish Storage: {fishes}");
                }
            }
        }

        /// <summary>
        /// Count Storage
        /// </summary>
        /// <param name="block"></param>
        /// <param name="inv"></param>
        /// <param name="spores"></param>
        /// <param name="crops"></param>
        public void CountCargo(IMyFunctionalBlock block, MyInventory inv, out int spores, out int crops)
        {
            spores = 0;
            crops = 0;
            if (block == null || block.Closed || block.MarkedForClose || !block.IsFunctional)
                return;
            if (inv == null)
                return;
            foreach (var item in inv.GetItems())
            {
                var subtype = item.Content.SubtypeId.String;
                int amount = (int)item.Amount;

                if (SporeSubtypes.Contains(subtype))
                {
                    spores += amount;
                }
                else if (SeaweedSubtypes.Contains(subtype))
                {
                    crops += amount;
                }
            }
        }

        /// <summary>
        /// Count fish block storage
        /// </summary>
        /// <param name="block"></param>
        /// <param name="inv"></param>
        /// <param name="baits"></param>
        /// <param name="fishes"></param>
        public void CountFishingCargo(IMyFunctionalBlock block, MyInventory inv, out int baits, out int fishes, AquaFarmingBlockType type)
        {
            baits = 0;
            fishes = 0;
            inventorymodelpaths.Clear();
            inventorymodelpathsList.Clear();
            if (block == null || block.Closed || block.MarkedForClose || !block.IsFunctional || !block.Enabled)
                return;
            if (inv == null)
                return;
            foreach (var item in inv.GetItems())
            {
                var subtype = item.Content.SubtypeId.String;
                int amount = (int)item.Amount;
                if (BaitSubtypes.Contains(subtype))
                {
                    baits += amount;
                }
                else if (FishSubtypes.Contains(subtype))
                {
                    fishes += amount;
                    AddModel(AquaModpathUtils.GetDirectModelPath(subtype, type));
                    //AquaExpansionSession.Insance.Log(true, $"model {AquaModpathUtils.GetDirectModelPath(subtype, type)}");
                }
            }
        }

        /// <summary>
        /// Get Growth % Line
        /// </summary>
        /// <param name="plant"></param>
        /// <returns></returns>
        public int GetGrowthPercent(AquaPlantInstance plant)
        {
            if (plant == null)
                return 0;
            return (int)MathHelper.Clamp(plant.Growth * 100f, 0f, 100f);
        }

        /// <summary>
        /// Get Attract % line
        /// </summary>
        /// <param name="fish"></param>
        /// <returns></returns>
        public int GetAttractingpercent(AquaFishInstance fish)
        {
            if (fish == null)
                return 0;
            return (int)MathHelper.Clamp(fish.Attract * 100f, 0f, 100f);
        }

        /// <summary>
        /// Get Catch % line
        /// </summary>
        /// <param name="fish"></param>
        /// <returns></returns>
        public int GetCatchingngpercent(AquaFishInstance fish)
        {
            if (fish == null)
                return 0;
            return (int)MathHelper.Clamp(fish.Catch * 100f, 0f, 100f);
        }

        /// <summary>
        /// Get catchResult % line
        /// </summary>
        /// <param name="fish"></param>
        /// <returns></returns>
        public int GetCatchresultngpercent(AquaFishInstance fish)
        {
            if (fish == null)
                return 0;
            return (int)MathHelper.Clamp(fish.CatchResult * 100f, 0f, 100f);
        }
        /// <summary>
        /// Get Planting % Line
        /// </summary>
        /// <param name="plant"></param>
        /// <returns></returns>
        public int GetPlantingPercent(AquaPlantInstance plant)
        {
            if (plant == null)
                return 0;
            return (int)MathHelper.Clamp(plant.Planting * 100f, 0f, 100f);
        }

        /// <summary>
        /// Get Harvesting % Line
        /// </summary>
        /// <param name="plant"></param>
        /// <returns></returns>
        public int GetHarvestingPercent(AquaPlantInstance plant)
        {
            if (plant == null)
                return 0;
            return (int)MathHelper.Clamp(plant.Harvesting * 100f, 0f, 100f);
        }

        /// <summary>
        /// Get Efficiency % Line
        /// </summary>
        /// <param name="eff"></param>
        /// <param name="plant"></param>
        /// <returns></returns>
        public int GetEfficiencyPercent(float eff, AquaPlantInstance plant)
        {
            if (plant == null)
                return 0;
            return (int)MathHelper.Clamp(eff * 100f, 0f, 100f);
        }

        /// <summary>
        /// Fish catch probability
        /// </summary>
        /// <param name="info"></param>
        /// <param name="block"></param>
        /// <param name="inv"></param>
        /// <param name="fish"></param>
        /// <param name="chance"></param>
        public void CurrentProbability(StringBuilder info, IMyFunctionalBlock block, MyInventory inv, AquaFishInstance fish, float chance)
        {
            if (block != null && inv != null)
            {
                if (block.Enabled && fish != null)
                {
                    float percent = chance * 100f;
                    info.AppendLine($"Catch Chance: {(int)MathHelper.Clamp(percent, 0f, 100f)} %");
                }
                else
                {
                    info.AppendLine($"Catch Chance: {zeroed} %");
                }
            }
        }

        /// <summary>
        /// Get Efficiency with % New
        /// </summary>
        /// <param name="info"></param>
        /// <param name="block"></param>
        /// <param name="inv"></param>
        /// <param name="plant"></param>
        /// <param name="eff"></param>
        public void CurrentEfficiency(StringBuilder info, IMyFunctionalBlock block, MyInventory inv, AquaPlantInstance plant, float eff)
        {
            if (block != null && inv != null)
            {
                if (block.Enabled && plant != null)
                {
                    info.AppendLine($"Efficiency: {(int)MathHelper.Clamp(eff * 100f, 0f, 100f)} %");
                }
                else
                {
                    info.AppendLine($"Efficiency: {zeroed} %");
                }
            }
        }
        /// <summary>
        /// Get total growth percent
        /// </summary>
        /// <param name="plant"></param>
        /// <returns></returns>
        public int GetTotalGrowthPercent(AquaPlantInstance plant)
        {
            if (plant == null)
                return 0;

            int stageIndex = (int)plant.Stage; // planting=0, Growing=1, MHarvstable=2
            float total = stageIndex + plant.Growth;

            return (int)MathHelper.Clamp((total / 2f) * 100f, 0f, 100f);
        }

        /// <summary>
        /// Counts items in inventory
        /// </summary>
        /// <param name="inventory"></param>
        /// <param name="hashes"></param>
        /// <returns></returns>
        private Dictionary<string, int> GetItemCounts(MyInventory inventory, HashSet<string> hashes)
        {
            var result = new Dictionary<string, int>();
            foreach (var item in inventory.GetItems())
            {
                var subtype = item.Content.SubtypeId.String;

                if (!hashes.Contains(subtype))
                    continue;

                if (!result.ContainsKey(subtype))
                    result[subtype] = 0;

                result[subtype] += (int)item.Amount;
            }

            return result;
        }

        /// <summary>
        /// Setup Definitions
        /// </summary>
        /// <param name="type"></param>
        public void SetupFarmingDefinitions(AquaFarmingBlockType type)
        {
            switch (type)
            {
                case AquaFarmingBlockType.UnderwaterFarmPlot:
                    AquaFarmItemsDatabase.Init();
                    AquaFarmItemsDatabase.Validate();
                    FillHashSubtypes();
                    AquaPlantDatabase.Init();
                    AquaPlantDatabase.Validate();
                    MapPlantData();
                    AquaRecipeDatabase.Init();
                    AquaRecipeDatabase.Validate();
                    break;
                case AquaFarmingBlockType.FishingBlock:
                    AquaFishItemsDatabase.Init();
                    AquaFishItemsDatabase.Validate();
                    FillFishingSubtypes();
                    AquaFishDatabase.Init();
                    AquaFishDatabase.Validate();
                    AquaFishingRecipeDatabase.Init();
                    AquaFishingRecipeDatabase.Validate();
                    break;
                case AquaFarmingBlockType.FishingBlockAdvance:
                    AquaFishItemsDatabase.Init();
                    AquaFishItemsDatabase.Validate();
                    FillFishingSubtypes();
                    AquaFishDatabase.Init();
                    AquaFishDatabase.Validate();
                    AquaFishingRecipeDatabase.Init();
                    AquaFishingRecipeDatabase.Validate();
                    break;
            }
        }

        /// <summary>
        /// Map Plant Data
        /// </summary>
        private void MapPlantData()
        {
            AquaPlantDatabase.MapComponent(AquaFarmItemsDatabase.GetSporebyID(1), 0);
            AquaPlantDatabase.MapComponent(AquaFarmItemsDatabase.GetSporebyID(2), 1);
            AquaPlantDatabase.MapComponent(AquaFarmItemsDatabase.GetSporebyID(3), 2);
        }

        /// <summary>
        /// Fill DataHashes
        /// </summary>
        private void FillHashSubtypes()
        {
            SporeSubtypes.Add(AquaFarmItemsDatabase.GetSporebyID(1));
            SporeSubtypes.Add(AquaFarmItemsDatabase.GetSporebyID(2));
            SporeSubtypes.Add(AquaFarmItemsDatabase.GetSporebyID(3));
            SeaweedSubtypes.Add(AquaFarmItemsDatabase.GetCropbyID(1));
            SeaweedSubtypes.Add(AquaFarmItemsDatabase.GetCropbyID(2));
            SeaweedSubtypes.Add(AquaFarmItemsDatabase.GetCropbyID(3));
        }

        /// <summary>
        /// Fill Fish Datahashes
        /// </summary>
        private void FillFishingSubtypes()
        {
            BaitSubtypes.Add(AquaFishItemsDatabase.GetBaitbyID(1));
            BaitSubtypes.Add(AquaFishItemsDatabase.GetBaitbyID(2));
            BaitSubtypes.Add(AquaFishItemsDatabase.GetBaitbyID(3));
            FishSubtypes.Add(AquaFishItemsDatabase.GetFishbyID(1));
            FishSubtypes.Add(AquaFishItemsDatabase.GetFishbyID(2));
            FishSubtypes.Add(AquaFishItemsDatabase.GetFishbyID(3));
            FishSubtypes.Add(AquaFishItemsDatabase.GetFishbyID(4));
            FishSubtypes.Add(AquaFishItemsDatabase.GetFishbyID(5));
            FishSubtypes.Add(AquaFishItemsDatabase.GetFishbyID(6));
            FishSubtypes.Add(AquaFishItemsDatabase.GetFishbyID(7));
            FishSubtypes.Add(AquaFishItemsDatabase.GetFishbyID(8));
            FishSubtypes.Add(AquaFishItemsDatabase.GetFishbyID(9));
            FishSubtypes.Add(AquaFishItemsDatabase.GetFishbyID(10));
            FishSubtypes.Add(AquaFishItemsDatabase.GetFishbyID(11));
            FishSubtypes.Add(AquaFishItemsDatabase.GetFishbyID(12));
            FishSubtypes.Add(AquaFishItemsDatabase.GetFishbyID(13));
            FishSubtypes.Add(AquaFishItemsDatabase.GetFishbyID(14));
            FishSubtypes.Add(AquaFishItemsDatabase.GetFishbyID(15));
            FishSubtypes.Add(AquaFishItemsDatabase.GetFishbyID(16));
            FishSubtypes.Add(AquaFishItemsDatabase.GetFishbyID(17));
            FishSubtypes.Add(AquaFishItemsDatabase.GetFishbyID(18));
            FishSubtypes.Add(AquaFishItemsDatabase.GetFishbyID(19));
            FishSubtypes.Add(AquaFishItemsDatabase.GetFishbyID(20));
            FishSubtypes.Add(AquaFishItemsDatabase.GetFishbyID(21));
            FishSubtypes.Add(AquaFishItemsDatabase.GetFishbyID(22));
        }

        /// <summary>
        /// Check Recipe Prerequisites
        /// </summary>
        /// <param name="inv"></param>
        /// <param name="requirements"></param>
        /// <returns></returns>
        private bool HasItems(MyInventory inv, Dictionary<MyDefinitionId, MyFixedPoint> requirements)
        {
            foreach (var req in requirements)
            {
                if (inv.GetItemAmount(req.Key) < req.Value)
                    return false;
            }
            return true;
        }
        /// <summary>
        /// Consume Recipe Prerequisites
        /// </summary>
        /// <param name="inv"></param>
        /// <param name="requirements"></param>
        private void ConsumeItems(MyInventory inv, Dictionary<MyDefinitionId, MyFixedPoint> requirements)
        {
            foreach (var req in requirements)
            {
                inv.RemoveItemsOfType(req.Value, req.Key);
            }
        }

        /// <summary>
        /// Check invertory volume
        /// </summary>
        /// <param name="inv"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        private bool CanAddItems(MyInventory inv, Dictionary<MyDefinitionId, MyFixedPoint> items)
        {
            MyFixedPoint totalVolume = 0;

            foreach (var item in items)
            {
                if (!inv.CanItemsBeAdded(item.Value, item.Key))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Get Recipe subtype
        /// </summary>
        /// <param name="recipe"></param>
        /// <returns></returns>
        private string GetRecipeSporeSubtype(AquaFarmingRecipe recipe)
        {
            // each recipe should have 1 spore input
            foreach (var pre in recipe.SporeToGrowPrerequisites)
            {
                return pre.Key.SubtypeId.ToString();
            }

            return null;
        }

        /// <summary>
        /// Get fish recipe subtype
        /// </summary>
        /// <param name="recipe"></param>
        /// <returns></returns>
        private string GetRecipeBaitSubtype(AquaFishingRecipe recipe)
        {
            // each recipe should have 1 bait input
            foreach (var pre in recipe.BaitPrerequisites)
            {
                return pre.Key.SubtypeId.ToString();
            }

            return null;
        }

        /// <summary>
        /// Autoselect Plant Recipe
        /// </summary>
        /// <param name="inventory"></param>
        /// <returns></returns>
        private AquaFarmingRecipe SelectBestRecipe(MyInventory inventory)
        {
            if (inventory == null)
                return null;
            var sporeCounts = GetItemCounts(inventory, SporeSubtypes);

            AquaFarmingRecipe bestRecipe = null;
            int bestCount = 0;
            foreach (var recipe in AquaRecipeDatabase.GetAll())
            {
                var sporeSubtype = GetRecipeSporeSubtype(recipe);
                int count;
                if (sporeSubtype == null)
                    continue;
                if (!sporeCounts.TryGetValue(sporeSubtype, out count))
                    continue;
                if (count <= 0)
                    continue;
                // ensure we have enough for recipe
                if (!HasItems(inventory, recipe.SporeToGrowPrerequisites))
                    continue;
                // pick recipe with MOST spores available
                if (count > bestCount)
                {
                    bestCount = count;
                    bestRecipe = recipe;
                    //AquaExpansionSession.Insance.Log(true, $"Recipe ID {bestRecipe.Id}");
                }
            }
            return bestRecipe;
        }

        /// <summary>
        /// Autoselect old fish recipe
        /// </summary>
        /// <param name="inventory"></param>
        /// <param name="type"></param>
        /// <param name="depth"></param>
        /// <returns></returns>
        private AquaFishingRecipe SelectBestFishRecipe(MyInventory inventory, AquaFarmingBlockType type, float depth)
        {
            if (inventory == null)
                return null;
            var baitCounts = GetItemCounts(inventory, BaitSubtypes);
            AquaFishingRecipe bestrecipe = null;
            int bestCount = 0;
            IEnumerable<AquaFishingRecipe> enumerator = null;
            //var Layer = AquaEnviromentUtils.GetOceanLayer(depth * (-1f));
            //AquaExpansionSession.Insance.Log(true, $"layer {depth * (-1f)}");
            switch (type)
            {
                case AquaFarmingBlockType.FishingBlock:
                    enumerator = AquaFishingRecipeDatabase.GetAll();
                    break;
                case AquaFarmingBlockType.FishingBlockAdvance:
                    enumerator = AquaFishingRecipeDatabase.GetAllBigFish();
                    break;
            }
            foreach (var recipe in enumerator)
            {
                var baitSubtype = GetRecipeBaitSubtype(recipe);
                int count;
                if (baitSubtype == null)
                    continue;
                if (!baitCounts.TryGetValue(baitSubtype, out count))
                    continue;
                if (count <= 0)
                    continue;
                // ensure we have enough for recipe
                if (!HasItems(inventory, recipe.BaitPrerequisites))
                    continue;
                // pick recipe with MOST baites available
                if (count > bestCount)
                {
                    bestCount = count;
                    bestrecipe = recipe;
                    AquaExpansionSession.Insance.Log(true, $"Recipe ID {bestrecipe.Id}, layer {bestrecipe.Oceanlayer}");
                }
            }
            return bestrecipe;
        }

        /// <summary>
        /// Autoselect random fish recipe
        /// </summary>
        /// <param name="inventory"></param>
        /// <param name="type"></param>
        /// <param name="currentdepth"></param>
        /// <param name="currentsalt"></param>
        /// <returns></returns>
        private AquaFishingRecipe SelectFishRecipeinLayer(MyInventory inventory, AquaFarmingBlockType type, float currentdepth, float currentsalt)
        {
            if (inventory == null)
                return null;
            var baitCounts = GetItemCounts(inventory, BaitSubtypes);
            AquaFishingRecipe randomlayerrecipe = null;
            int bestCount = 0;
            IEnumerable<AquaFishingRecipe> enumerator = null;
            var Layer = AquaEnviromentUtils.GetOceanLayer(currentdepth * (-1f));
            List<AquaFishingRecipe> validRecipes = new List<AquaFishingRecipe>();
            switch (type)
            {
                case AquaFarmingBlockType.FishingBlock:
                    enumerator = AquaFishingRecipeDatabase.GetAll();
                    break;
                case AquaFarmingBlockType.FishingBlockAdvance:
                    enumerator = AquaFishingRecipeDatabase.GetAllBigFish();
                    break;
            }
            foreach (var recipe in enumerator)
            {
                //layer
                if (recipe.Oceanlayer != Layer)
                    continue;
                //bait
                var baitSubtype = GetRecipeBaitSubtype(recipe);
                if (baitSubtype == null)
                    continue;
                int count;
                if (!baitCounts.TryGetValue(baitSubtype, out count))
                    continue;
                if (count <= 0)
                   continue;
                if (!HasItems(inventory, recipe.BaitPrerequisites))
                    continue;
                if (count > bestCount)
                {
                    validRecipes.Add(recipe);
                }
            }
            if (validRecipes.Count == 0)
                return null;
            //AquaExpansionSession.Insance.Log(true, $"validrecipes {validRecipes.Count}");
            randomlayerrecipe = validRecipes[MyUtils.GetRandomInt(validRecipes.Count)];
            //AquaExpansionSession.Insance.Log(true, $"Recipe ID {randomlayerrecipe.Id}, Recipe Displayname {randomlayerrecipe.Displayname}, in OceanLayer {Layer}");
            return randomlayerrecipe;
        }

        /// <summary>
        /// Update fishing global
        /// </summary>
        /// <param name="block"></param>
        /// <param name="inv"></param>
        /// <param name="type"></param>
        /// <param name="stage"></param>
        /// <param name="activeRecipe"></param>
        /// <param name="currentfish"></param>
        /// <param name="chance"></param>
        /// <param name="depth"></param>
        /// <param name="salt"></param>
        /// <param name="remtime"></param>
        public void UpdateFishing(IMyFunctionalBlock block, MyInventory inv, AquaFarmingBlockType type ,ref AquaFishBlockStage stage, ref AquaFishingRecipe activeRecipe, 
            ref AquaFishInstance currentfish, ref float chance, float depth, float salt, ref float remtime)
        {
            if (block == null || block.Closed || !block.IsFunctional || inv == null || block.CubeGrid == null || block.CubeGrid.Closed)
            {
                stage = AquaFishBlockStage.Idlle;
                activeRecipe = null;
                currentfish = null;
                chance = 0f;
                OnModelStageChanged(0);
                OnFishModelStageChanged(0);
                return;
            }
            //try start fishing
            if (currentfish == null)
            {
                TryStartFishing(block, inv, type ,ref stage, ref activeRecipe, ref currentfish, depth, salt);
                return;
            }
            //has fish target update fishing
            if (!block.Enabled && (stage == AquaFishBlockStage.Idlle || stage == AquaFishBlockStage.Atracting))
            { onetimereloadbait = false; return; }
            if (!block.Enabled && (stage == AquaFishBlockStage.Catching || stage == AquaFishBlockStage.CatchResult || stage == AquaFishBlockStage.Full))
            {
                    stage = AquaFishBlockStage.Idlle;
                    activeRecipe = null;
                    currentfish = null;
                    chance = 0f;
                    OnModelStageChanged(0);
                    OnFishModelStageChanged(0);
                    return;
            }
            UpdateFishingProcess(inv, ref stage, ref activeRecipe, ref currentfish, ref chance, depth, salt, ref remtime);
        }

        /// <summary>
        /// Try start Fishing
        /// </summary>
        /// <param name="block"></param>
        /// <param name="inv"></param>
        /// <param name="type"></param>
        /// <param name="Stage"></param>
        /// <param name="ActiveRecipe"></param>
        /// <param name="CurrentFish"></param>
        /// <param name="depth"></param>
        /// <param name="salt"></param>
        private void TryStartFishing(IMyFunctionalBlock block, MyInventory inv, AquaFarmingBlockType type,  ref AquaFishBlockStage Stage, ref AquaFishingRecipe ActiveRecipe, 
            ref AquaFishInstance CurrentFish, float depth, float salt)
        {
            if (!block.Enabled || !block.IsFunctional)
                return;
            if (inv == null || CurrentFish != null || ActiveRecipe != null)
                return;
            //var recipe = SelectBestFishRecipe(inv, type, depth);
            var recipe = SelectFishRecipeinLayer(inv, type, depth, salt);
            if (recipe == null)
            {
                Stage = AquaFishBlockStage.Idlle;
                return;
            }
            if (!HasItems(inv, recipe.BaitPrerequisites))
            {
                Stage = AquaFishBlockStage.Idlle;
                return;
            }
            ConsumeItems(inv, recipe.BaitPrerequisites);
            CurrentFish = new AquaFishInstance
            {
                DefID = recipe.FishResult.NumericId,
                DefId = recipe.FishResult.Id,
                fishDef = recipe.FishResult,
                Attract = 0f,
                Catch = 0f,
                CatchResult = 0f,
                Stage = AquaFishBlockStage.Atracting
            };
            ActiveRecipe = recipe;
            Stage = AquaFishBlockStage.Atracting;
            OnModelStageChanged(1);
        }

        /// <summary>
        /// Update fishing process
        /// </summary>
        /// <param name="inv"></param>
        /// <param name="stage"></param>
        /// <param name="activeRecipe"></param>
        /// <param name="currenfish"></param>
        /// <param name="chance"></param>
        /// <param name="indepth"></param>
        /// <param name="insalt"></param>
        /// <param name="remantime"></param>
        private void UpdateFishingProcess(MyInventory inv, ref AquaFishBlockStage stage, ref AquaFishingRecipe activeRecipe, ref AquaFishInstance currenfish, 
            ref float chance, float indepth, float insalt, ref float remantime)
        {

            if (currenfish == null || activeRecipe == null)
                return;
            var def = currenfish.fishDef;
            chance = Math.Max(0.01f, CalculateFishCatchChance(def , indepth, insalt));
            float AttractTime = def.BaseAttractTime;
            float CatchTime = def.BaseCatchTime;
            float CatchResultTime = def.BaseCatchResultTime;
            float delta = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            float SpeedMult = delta * 10f; // Update10() sync speeed
            switch (stage)
            {
                //1. Baiting Stage
                case AquaFishBlockStage.Atracting:
                    currenfish.Attract += SpeedMult / AttractTime;
                    currenfish.Attract = Math.Min(currenfish.Attract, 1f);
                    remantime = ((1f - currenfish.Attract) * AttractTime);
                    OneTimeReloadBait();
                    if (currenfish.Attract >= 1f)
                    {
                        AdvanceFishingStage(ref currenfish);
                        currenfish.Attract = 0f;
                        //AquaExpansionSession.Insance.Log(true, $" Acttracting end");
                        if (currenfish.Stage == AquaFishBlockStage.Catching)
                            stage = AquaFishBlockStage.Catching;
                        OnModelStageChanged(1);
                        OnFishModelStageChanged(1);
                    }
                    break;
                // 2. Catching stage
                case AquaFishBlockStage.Catching:
                    currenfish.Catch += SpeedMult / CatchTime;
                    currenfish.Catch = Math.Min(currenfish.Catch, 1f);
                    remantime = ((1f - currenfish.Catch) * CatchTime);
                    if (currenfish.Catch >= 1f)
                    {
                        AdvanceFishingStage(ref currenfish);
                        currenfish.Catch = 0f;
                        //AquaExpansionSession.Insance.Log(true, $" Catching end");
                        if (currenfish.Stage == AquaFishBlockStage.CatchResult)
                            stage = AquaFishBlockStage.CatchResult;
                        OnModelStageChanged(1);
                        OnFishModelStageChanged(1);
                    }
                    break;
                // 3. Resulting stage
                case AquaFishBlockStage.CatchResult:
                    currenfish.CatchResult += SpeedMult / CatchResultTime;
                    currenfish.CatchResult = Math.Min(currenfish.CatchResult, 1f);
                    remantime = ((1f - currenfish.CatchResult) * CatchResultTime);
                    if (currenfish.CatchResult >= 1f)
                    {
                        currenfish.CatchResult = 0f;
                        //AquaExpansionSession.Insance.Log(true, $" CatchResult end");
                        if (currenfish.Stage == AquaFishBlockStage.CatchResult)
                            stage = AquaFishBlockStage.CatchResult;
                        TryCatchFish(inv, ref activeRecipe, ref currenfish, ref stage, ref chance);
                        OnModelStageChanged(0);
                        OnFishModelStageChanged(0);
                    }
                    break;
                // if container full
                case AquaFishBlockStage.Full:
                    if (currenfish.Stage == AquaFishBlockStage.Full)
                        stage = AquaFishBlockStage.Full;
                    //TryCatchFish(inv, ref activeRecipe, ref currenfish, ref stage, ref chance);
                    MissCatch(ref activeRecipe, ref currenfish, ref stage);
                    OnModelStageChanged(0);
                    OnFishModelStageChanged(0);
                    break;
            }
        }

        /// <summary>
        /// OneTime reload bait after block was disabled
        /// </summary>
        private void OneTimeReloadBait()
        {
            if (onetimereloadbait)
                return;
            onetimereloadbait = true;
            OnModelStageChanged(1);
        }

        /// <summary>
        /// Try Catch fish
        /// </summary>
        /// <param name="inv"></param>
        /// <param name="activeRecipe"></param>
        /// <param name="currentFish"></param>
        /// <param name="stage"></param>
        /// <param name="chance"></param>
        private void TryCatchFish(MyInventory inv, ref AquaFishingRecipe activeRecipe, ref AquaFishInstance currentFish, ref AquaFishBlockStage stage, ref float chance)
        {
            if (currentFish == null || activeRecipe == null || inv == null)
                return;
            if (stage != AquaFishBlockStage.CatchResult)
                return;
            float finalchance = chance;
            if (RollCatchChance(finalchance))
            {
                foreach (var res in activeRecipe.BaitToFishResults)
                {
                    if (!inv.CanItemsBeAdded(res.Value, res.Key))
                    {
                        currentFish.Stage = AquaFishBlockStage.Full;
                        stage = AquaFishBlockStage.Full;
                        //AquaExpansionSession.Insance.Log(true, $"Item {res.Key.SubtypeName} cant be added");
                        return;
                    }
                }
                foreach (var res in activeRecipe.BaitToFishResults)
                {
                    var ob = MyObjectBuilderSerializer.CreateNewObject(res.Key) as MyObjectBuilder_PhysicalObject;
                    if (ob == null)
                        continue;

                    inv.AddItems(res.Value, ob);
                    //AquaExpansionSession.Insance.Log(true, $"Item created");
                }
                ResetCatch(ref activeRecipe, ref currentFish, ref stage);
            }
            else
            {
                MissCatch(ref activeRecipe, ref currentFish, ref stage);
            }
        }

        /// <summary>
        /// Miss catch
        /// </summary>
        /// <param name="activeRecipe"></param>
        /// <param name="currentFish"></param>
        /// <param name="stage"></param>
        private void MissCatch(ref AquaFishingRecipe activeRecipe, ref AquaFishInstance currentFish, ref AquaFishBlockStage stage)
        {
            if (currentFish == null || activeRecipe == null)
                return;
            currentFish = null;
            activeRecipe = null;
            //AquaExpansionSession.Insance.Log(true, $"Failed");
            stage = AquaFishBlockStage.Idlle;
        }

        /// <summary>
        /// Rest catch
        /// </summary>
        /// <param name="activeRecipe"></param>
        /// <param name="currentFish"></param>
        /// <param name="stage"></param>
        private void ResetCatch(ref AquaFishingRecipe activeRecipe, ref AquaFishInstance currentFish, ref AquaFishBlockStage stage)
        {
            if (currentFish == null || activeRecipe == null)
                return;
            currentFish = null;
            activeRecipe = null;
            //AquaExpansionSession.Insance.Log(true, $"Success");
            stage = AquaFishBlockStage.Idlle;
        }

        /// <summary>
        /// Roll catch chance
        /// </summary>
        /// <param name="chance"></param>
        /// <returns></returns>
        private bool RollCatchChance(float chance)
        {
            chance = MathHelper.Clamp(chance, 0f, 1f);
            float roll = MyUtils.GetRandomFloat();
            bool success = roll < chance;
            //AquaExpansionSession.Insance.Log(true,$"Fish Roll={roll:0.000} Chance={chance:0.000} Success={success}");
            return success;
        }

        /// <summary>
        /// Update Farm
        /// </summary>
        /// <param name="block"></param>
        /// <param name="inv"></param>
        /// <param name="stage"></param>
        /// <param name="activeRecipe"></param>
        /// <param name="currentPlant"></param>
        /// <param name="eff"></param>
        /// <param name="depth"></param>
        /// <param name="salt"></param>
        public void UpdateFarm(IMyFunctionalBlock block, MyInventory inv, ref AquaFarmBlockStage stage, ref AquaFarmingRecipe activeRecipe, ref AquaPlantInstance currentPlant, ref float eff, float depth, float salt, ref float remtime)
        {
            if (block == null || block.Closed || !block.IsFunctional || inv == null || block.CubeGrid == null || block.CubeGrid.Closed)
            {
                stage = AquaFarmBlockStage.Iddle;
                activeRecipe = null;
                currentPlant = null;
                eff = 0f;
                OnModelStageChanged(0);
                //AquaExpansionSession.Insance.Log(true, "block not functional, Plant terminated ");
                return;
            }
            //try start
            if (currentPlant == null)
            {
                TryStartPlant(block, inv, ref stage, ref activeRecipe, ref currentPlant);
                return;
            }
            //  Has plant update growth
            if (!block.Enabled)
                return;
            UpdatePlant(inv, ref stage, ref activeRecipe, ref currentPlant, ref eff, depth, salt, ref remtime);
        }

        /// <summary>
        /// Start Plant
        /// </summary>
        /// <param name="block"></param>
        /// <param name="inv"></param>
        /// <param name="Stage"></param>
        /// <param name="ActiveRecipe"></param>
        /// <param name="CurrentPlant"></param>
        private void TryStartPlant(IMyFunctionalBlock block, MyInventory inv, ref AquaFarmBlockStage Stage, ref AquaFarmingRecipe ActiveRecipe, ref AquaPlantInstance CurrentPlant)
        {
            if (!block.Enabled || !block.IsFunctional)
                return;
            if (inv == null || CurrentPlant != null || ActiveRecipe != null)
                return;
            var recipe = SelectBestRecipe(inv);
            if (recipe == null)
            {
                Stage = AquaFarmBlockStage.Iddle;
                return;
            }
            if (!HasItems(inv, recipe.SporeToGrowPrerequisites))
            {
                Stage = AquaFarmBlockStage.Iddle;
                return;
            }
            ConsumeItems(inv, recipe.SporeToGrowPrerequisites);
            CurrentPlant = new AquaPlantInstance
            {
                DefID = recipe.PlantResult.NumericId,
                DefId = recipe.PlantResult.Id,
                Def = recipe.PlantResult,
                Growth = 0f,
                Planting = 0f,
                Harvesting = 0f,
                Stage = AquaFarmBlockStage.Planting
            };
            ActiveRecipe = recipe;
            Stage = AquaFarmBlockStage.Planting;
            OnModelStageChanged(1);
        }

        /// <summary>
        /// Change Plant Stage
        /// </summary>
        /// <param name="plant"></param>
        private void AdvanceStage(ref AquaPlantInstance plant)
        {
            switch (plant.Stage)
            {
                case AquaFarmBlockStage.Planting:
                    plant.Stage = AquaFarmBlockStage.Growing;
                    break;

                case AquaFarmBlockStage.Growing:
                    plant.Stage = AquaFarmBlockStage.Harvestable;
                    break;
            }
        }

        /// <summary>
        /// Change Fishing Stage
        /// </summary>
        /// <param name="fish"></param>
        private void AdvanceFishingStage(ref AquaFishInstance fish)
        {
            switch (fish.Stage)
            {
                case AquaFishBlockStage.Atracting:
                    fish.Stage = AquaFishBlockStage.Catching;
                    break;
                case AquaFishBlockStage.Catching:
                    fish.Stage = AquaFishBlockStage.CatchResult;
                    break;
            }
        }

        /// <summary>
        /// Update Plant Progress
        /// </summary>
        /// <param name="inv"></param>
        /// <param name="stage"></param>
        /// <param name="activeRecipe"></param>
        /// <param name="currentPlant"></param>
        /// <param name="indepth"></param>
        /// <param name="insalt"></param>
        private void UpdatePlant(MyInventory inv, ref AquaFarmBlockStage stage, ref AquaFarmingRecipe activeRecipe, ref AquaPlantInstance currentPlant, ref float eff, float indepth, float insalt, ref float remantime)
        {
            if (currentPlant == null || activeRecipe == null)
                return;
            var def = currentPlant.Def;
            eff = Math.Max(0.01f, CalculateEfficiency(def, indepth, insalt));
            float plantTime = def.PlantTime;
            float growTime = def.BaseGrowTime;
            float harvestTime = def.HarvestTime;
            float delta = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            float SpeedMult = delta * 10f; // Update10() sync speeed
            switch (stage)
            {
                // 1. PLANTING STAGE
                case AquaFarmBlockStage.Planting:
                    currentPlant.Planting += SpeedMult / plantTime;
                    currentPlant.Planting = Math.Min(currentPlant.Planting, 1f);
                    remantime = ((1f - currentPlant.Planting) * plantTime);
                    if (currentPlant.Planting >= 1f)
                    {
                        AdvanceStage(ref currentPlant);
                        currentPlant.Planting = 0f;
                        //AquaExpansionSession.Insance.Log(true, $" Planting end");
                        if (currentPlant.Stage == AquaFarmBlockStage.Growing)
                            stage = AquaFarmBlockStage.Growing;
                        OnModelStageChanged(2);
                    }
                    break;

                // 2. GROWING STAGE
                case AquaFarmBlockStage.Growing:
                    currentPlant.Growth += SpeedMult / growTime * eff;
                    currentPlant.Growth = Math.Min(currentPlant.Growth, 1f);
                    remantime = ((1f - currentPlant.Growth) * growTime / eff);
                    if (currentPlant.Growth >= 1f)
                    {
                        AdvanceStage(ref currentPlant);
                        currentPlant.Growth = 0f;
                        //AquaExpansionSession.Insance.Log(true, $" Growing end");
                        if (currentPlant.Stage == AquaFarmBlockStage.Harvestable)
                            stage = AquaFarmBlockStage.Harvestable;
                        OnModelStageChanged(3);
                    }
                    //AquaExpansionSession.Insance.Log(true, $"basetime {def.BaseGrowTime} new {growTime}");
                    break;

                // 3. HARVEST STAGE
                case AquaFarmBlockStage.Harvestable:
                    currentPlant.Harvesting += SpeedMult / harvestTime;
                    currentPlant.Harvesting = Math.Min(currentPlant.Harvesting, 1f);
                    remantime = ((1f - currentPlant.Harvesting) * harvestTime);
                    if (currentPlant.Harvesting >= 1f)
                    {
                        currentPlant.Harvesting = 0f;
                        //AquaExpansionSession.Insance.Log(true, $" Harvesting end");
                        if (currentPlant.Stage == AquaFarmBlockStage.Harvestable)
                            stage = AquaFarmBlockStage.Harvestable;
                        TryHarvest(inv, ref activeRecipe, ref currentPlant, ref stage);
                    }
                    break;
                case AquaFarmBlockStage.Full:
                    // keep trying until space is available
                    if (currentPlant.Stage == AquaFarmBlockStage.Full)
                        stage = AquaFarmBlockStage.Full;
                        TryHarvest(inv, ref activeRecipe, ref currentPlant, ref stage);
                    break;
            }

        }

        /// <summary>
        /// Autoharvert
        /// </summary>
        /// <param name="inv"></param>
        /// <param name="activeRecipe"></param>
        /// <param name="currentPlant"></param>
        /// <param name="stage"></param>
        private void TryHarvest(MyInventory inv, ref AquaFarmingRecipe activeRecipe, ref AquaPlantInstance currentPlant, ref AquaFarmBlockStage stage)
        {
            if (currentPlant == null || activeRecipe == null || inv == null)
                return;
            //check
            foreach (var res in activeRecipe.PlantToCropResults)
            {
                if (!inv.CanItemsBeAdded(res.Value, res.Key))
                {
                    currentPlant.Stage = AquaFarmBlockStage.Full;
                    stage = AquaFarmBlockStage.Full;
                    //AquaExpansionSession.Insance.Log(true, $"Item {res.Key.SubtypeName} cant be added");
                    return;
                }
            }
            foreach (var res in activeRecipe.PlantToCropResults)
            {
                var ob = MyObjectBuilderSerializer.CreateNewObject(res.Key) as MyObjectBuilder_PhysicalObject;
                if (ob == null)
                    continue;

                inv.AddItems(res.Value, ob);
                //AquaExpansionSession.Insance.Log(true, $"Item created");
            }
            ResetPlant(ref activeRecipe, ref currentPlant, ref stage);
            //currentPlant = null;
            //activeRecipe = null;
            //reset
            //AquaExpansionSession.Insance.Log(true, $"Harvested");
            //stage = AquaFarmBlockStage.Iddle;
            //OnModelStageChanged(0);
        }

        /// <summary>
        /// Rest plant
        /// </summary>
        /// <param name="activeRecipe"></param>
        /// <param name="currentPlant"></param>
        /// <param name="stage"></param>
        private void ResetPlant(ref AquaFarmingRecipe activeRecipe, ref AquaPlantInstance currentPlant, ref AquaFarmBlockStage stage)
        {
            if (activeRecipe == null || currentPlant == null)
                return;
            currentPlant = null;
            activeRecipe = null;
            stage = AquaFarmBlockStage.Iddle;
            OnModelStageChanged(0);
        }

        /// <summary>
        /// Farm effiency
        /// </summary>
        /// <param name="def"></param>
        /// <param name="depth"></param>
        /// <param name="salt"></param>
        /// <returns></returns>
        private float CalculateEfficiency(AquaPlantDefinition def, float depth, float salt)
        {
            float depthFactor = DepthEff(depth, def.OptimalDepth, def.ToUp, def.ToDown, def.FalloffUp, def.FalloffDown);
            depthFactor = MathHelper.Clamp(depthFactor, 0.1f, 1f);
            //float saltFactor = SaltEff(salt, def.OptimalSalt, def.SaltTolerance, def.SaltFallof);
            float saltFactor = MathHelper.Clamp(salt / 3f, 0f, 1f);
            saltFactor = MathHelper.Clamp(saltFactor, 0.1f, 1f);
            float eff = (float)Math.Sqrt(depthFactor * saltFactor);
            eff = Math.Max(0.01f, eff);
            //AquaExpansionSession.Insance.Log(true, $" depthFactor {depthFactor}, salt {salt} saltfactor {saltFactor} eff {eff}");
            return eff;
        }

        /// <summary>
        /// Fish catch chance
        /// </summary>
        /// <param name="def"></param>
        /// <param name="depth"></param>
        /// <param name="salt"></param>
        /// <returns></returns>
        private float CalculateFishCatchChance(AquaFishDefinition def, float depth, float salt)
        {
            // Depth preference (0–1)
            float depthFactor = FishDepthEff(depth, def.OptimalDepth, def.ToUp, def.ToDown);
            float chance = depthFactor * def.BaseCatchChance;
            //AquaExpansionSession.Insance.Log(true, $"depthfactor {depthFactor} def chance {def.BaseCatchChance}, chance {chance}");
            return MathHelper.Clamp(chance, 0f, 1f);
        }

        /// <summary>
        /// Fish depth  catch eff
        /// </summary>
        /// <param name="depth"></param>
        /// <param name="optimal"></param>
        /// <param name="rangeUp"></param>
        /// <param name="rangeDown"></param>
        /// <returns></returns>
        private float FishDepthEff(float depth, float optimal, float rangeUp, float rangeDown)
        {
            float d = Math.Abs(depth);
            float delta = d - optimal;
            float maxUp = rangeUp;
            float maxDown = rangeDown;
            bool deeper = delta > 0f;
            float limit = deeper ? maxDown : maxUp;
            float absDelta = Math.Abs(delta);
            if (absDelta <= limit)
                return 1f;
            float x = (absDelta - limit) / limit;
            float t = (float)Math.Exp(-(x * x) * 2.5f);
            if (t < 0.02f)
                return 0f;
            return t;
        }

        /// <summary>
        /// Get fish salt factor
        /// </summary>
        /// <param name="def"></param>
        /// <param name="salt"></param>
        /// <returns></returns>
        private float GetSalinityFactor(AquaFishDefinition def, float salt)
        {
            float delta = Math.Abs(salt - def.OptimalSalt);
            float t = 1f - (delta / def.SaltTolerance);

            return MathHelper.Clamp(t, 0f, 1f);
        }

        /// <summary>
        /// Salt Tolerance Efficiency
        /// </summary>
        /// <param name="salt"></param>
        /// <param name="optimal"></param>
        /// <param name="tolerance"></param>
        /// <param name="falloff"></param>
        /// <returns></returns>
        private float SaltEff(float salt, float optimal, float tolerance, float falloff)
        {
            /*float delta = Math.Abs(salt - optimal);
            if (delta <= tolerance)
                return 1f;
            float x = (delta - tolerance) / falloff;
            return (float)Math.Exp(-x * x);*/
            float delta = Math.Abs(salt - optimal);
            return (float)Math.Exp(-(delta * delta) / (tolerance * tolerance));
        }

        /// <summary>
        /// Depth Tolerance Efficiency
        /// </summary>
        /// <param name="depth"></param>
        /// <param name="optimal"></param>
        /// <param name="tolUp"></param>
        /// <param name="tolDown"></param>
        /// <param name="falloffUp"></param>
        /// <param name="falloffDown"></param>
        /// <returns></returns>
        private float DepthEff(float depth, float optimal, float tolUp, float tolDown, float falloffUp, float falloffDown)
        {
            float d = Math.Abs(depth); 
            float delta = d - optimal;
            float tolerance = delta < 0 ? tolUp : tolDown;
            float falloff = delta < 0 ? falloffUp : falloffDown;
            float absDelta = Math.Abs(delta);
            // dead zone
            if (absDelta <= tolerance)
                return 1f;
            float x = (absDelta - tolerance) / falloff;
            return (float)Math.Exp(-x * x);
        }

        /// <summary>
        /// Get virtual recipe data
        /// </summary>
        /// <param name="recipe"></param>
        /// <returns></returns>
        public string GetVirtualPlantRecipe(AquaFarmingRecipe recipe)
        {
            if (recipe == null || recipe.SporeToGrowPrerequisites == null || recipe.SporeToGrowPrerequisites.Count == 0)
                return "No valid recipe data";
            var parts = new HashSet<string>();
            var odepth = $"Optimal depth: -{recipe.PlantResult.OptimalDepth} m";
            var time = $"Base Time: {GetHMS(recipe.PlantResult.PlantTime+recipe.PlantResult.BaseGrowTime+recipe.PlantResult.HarvestTime)}";
            var crops = new HashSet<string>();
            foreach (var pre in recipe.SporeToGrowPrerequisites)
            {
                var def = MyDefinitionManager.Static.GetPhysicalItemDefinition(pre.Key);
                var name = def?.DisplayNameText ?? pre.Key.SubtypeName;
                parts.Add($"{pre.Value}x {name}");
            }
            foreach (var crop in recipe.PlantToCropResults)
            {
                var def = MyDefinitionManager.Static.GetPhysicalItemDefinition(crop.Key);
                var name = def?.DisplayNameText ?? crop.Key.SubtypeName;
                crops.Add($"{crop.Value}x {name}");
            }
            string preinfo = string.Join(" \n ", parts);
            string cropinfo = string.Join(" \n", crops);
            return $"{preinfo}\n{odepth}\n{time}\n{cropinfo}";
        }

        /// <summary>
        /// Get virtual Fish recipe data
        /// </summary>
        /// <param name="recipe"></param>
        /// <returns></returns>
        public string GetVirtualFishRecipe(AquaFishingRecipe recipe)
        {
            if (recipe == null || recipe.BaitPrerequisites == null || recipe.BaitPrerequisites.Count == 0)
                return "No valid recipe data";
            var parts = new HashSet<string>();
            var odepth = $"Live in {recipe.Oceanlayer} waters";
            var time = $"Base Time: {GetHMS(recipe.FishResult.BaseAttractTime + recipe.FishResult.BaseCatchTime + recipe.FishResult.BaseCatchResultTime)}";
            var chance = $"Base Catch Chance: {(int)MathHelper.Clamp(recipe.FishResult.BaseCatchChance * 100f, 0f, 100f)} %";
            var fishes = new HashSet<string>();
            foreach (var pre in recipe.BaitPrerequisites)
            {
                var def = MyDefinitionManager.Static.GetPhysicalItemDefinition(pre.Key);
                var name = def?.DisplayNameText ?? pre.Key.SubtypeName;
                parts.Add($"{pre.Value}x {name}");
            }
            foreach (var fish in recipe.BaitToFishResults)
            {
                var def = MyDefinitionManager.Static.GetPhysicalItemDefinition(fish.Key);
                var name = def?.DisplayNameText ?? fish.Key.SubtypeName;
                fishes.Add($"{fish.Value}x {name}");
            }
            string preinfo = string.Join(" \n ", parts);
            string fishinfo = string.Join(" \n", fishes);
            return $"{preinfo}\n{odepth}\n{time}\n{chance}\n{fishinfo}";
        }

        /// <summary>
        /// Time Conversion to 24H format
        /// </summary>
        /// <param name="seconds"></param>
        /// <returns></returns>
        public static string GetHMS(float seconds) => $"{(int)seconds / 3600:00}:{((int)seconds % 3600) / 60:00}:{(int)seconds % 60:00}";

        /// <summary>
        /// Get Mod path in utils bound
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public string GetModPaths(string model)
        {
            if (string.IsNullOrEmpty(model))
                return null;
            string modpath = "";
            modpath = AquaExpansionSession.Insance.ModContext.ModPath;
            return Path.Combine(modpath, "Models\\", model);
        }
    }

    /// <summary>
    /// Enviromental helpers
    /// </summary>
    public static class AquaEnviromentUtils
    {
        /// <summary>
        /// Get Ocean Layer by depth
        /// </summary>
        /// <param name="depth"></param>
        /// <returns></returns>
        public static AquaOceanLayer GetOceanLayer(float depth)
        {
            if (depth < 10f)
                return AquaOceanLayer.Surface;
            if (depth < 30f)
                return AquaOceanLayer.Shallow;
            if (depth < 80f)
                return AquaOceanLayer.Mid;
            if (depth < 180f)
                return AquaOceanLayer.Deep;
            return AquaOceanLayer.Abyss;
        }
    }

    /// <summary>
    /// Class for Process virtual blueprints
    /// </summary>
    public class AquaFarmingRecipe
    {
        public Dictionary<MyDefinitionId, MyFixedPoint> SporeToGrowPrerequisites = new Dictionary<MyDefinitionId, MyFixedPoint>();
        public AquaPlantDefinition PlantResult;
        public Dictionary<MyDefinitionId, MyFixedPoint> PlantToCropResults = new Dictionary<MyDefinitionId, MyFixedPoint>();
        public int Id;
        public int Amount;
        public string Displayname;

        public AquaFarmingRecipe(int id, string displayname, int amout  = 1)
        {
            Id = id;
            Displayname = displayname;
            Amount = amout;
        }
        /// <summary>
        /// Add prerequisites Old
        /// </summary>
        public void AddPlantPrerequisites(string subtypeid, MyFixedPoint amount)
        {
            SporeToGrowPrerequisites.Add(MyDefinitionId.Parse($"MyObjectBuilder_Component/{subtypeid}"), amount);
        }

        /// <summary>
        /// Add Plant results Old
        /// </summary>
        public void SetPlantResult(AquaPlantDefinition plant)
        {
            PlantResult = plant;
        }

        /// <summary>
        /// Add Crop Result Old
        /// </summary>
        /// <param name="subtypeid"></param>
        /// <param name="amount"></param>
        public void AddCropResult(string subtypeid, MyFixedPoint amount)
        {
            PlantToCropResults.Add(
                MyDefinitionId.Parse($"MyObjectBuilder_ConsumableItem/{subtypeid}"),
                amount
            );
        }
    }

    /// <summary>
    /// Class for process virual fishing blueprins
    /// </summary>
    public class AquaFishingRecipe
    {
        public Dictionary<MyDefinitionId, MyFixedPoint> BaitPrerequisites = new Dictionary<MyDefinitionId, MyFixedPoint>();
        public AquaFishDefinition FishResult;
        public Dictionary<MyDefinitionId, MyFixedPoint> BaitToFishResults = new Dictionary<MyDefinitionId, MyFixedPoint>();
        public int Id;
        public int Amount;
        public string Displayname;
        public AquaOceanLayer Oceanlayer;

        public AquaFishingRecipe(int id, string displayname, int amout = 1)
        {
            Id = id;
            Displayname = displayname;
            Amount = amout;
        }
    }

    /// <summary>
    /// Plant definition class for defining the plant properies and requirements to grow
    /// </summary>
    public class AquaPlantDefinition
    {
        // identity
        public int NumericId;     // used for save/load
        public string Id;
        // timing
        public float BaseGrowTime;
        public float PlantTime;
        public float HarvestTime;
        // environment
        public float OptimalDepth;
        public float ToUp;
        public float ToDown;
        public float FalloffUp;
        public float FalloffDown;
        public float OptimalSalt;
        public float SaltTolerance;
        public float SaltFallof;
        // power
        public float PowerIdle;
        public float PowerWork;
        // visuals
        public string[] StageModels;
    }

    /// <summary>
    /// Fish definition class for defining the fish properies and requirements to catch
    /// </summary>
    public class AquaFishDefinition
    {
        // identity
        public int NumericId;     // used for save/load
        public string Id;
        // timing
        public float BaseAttractTime;
        public float BaseCatchTime;
        public float BaseCatchResultTime;
        // environment
        public float OptimalDepth;
        public float ToUp;
        public float ToDown;
        public float FalloffUp;
        public float FalloffDown;
        public float OptimalSalt;
        public float SaltTolerance;
        public float SaltFalloff;
        public float BaseCatchChance;
        public string Model;
    }

    /// <summary>
    /// Running instance of a plant with all the necessary data to track its growth progress
    /// </summary>
    [ProtoContract]
    public class AquaPlantInstance
    {
        [ProtoIgnore] public AquaPlantDefinition Def;
        [ProtoIgnore] public string DefId;
        [ProtoMember(1)] public int DefID;
        [ProtoMember(2)] public AquaFarmBlockStage Stage;
        [ProtoMember(3)] public float Growth; // 0..1 per stage
        [ProtoMember(4)] public float Planting; // 0..1
        [ProtoMember(5)] public float Harvesting;
        // cached environment
        [ProtoIgnore] public float CurrentDepth;
        [ProtoIgnore] public float CurrentSalt;
    }

    /// <summary>
    /// Running instance of a fish with all the necessary data to track catch progress
    /// </summary>
    [ProtoContract]
    public class AquaFishInstance
    {
        [ProtoIgnore] public AquaFishDefinition fishDef;
        [ProtoIgnore] public string DefId;
        [ProtoMember(1)] public int DefID;
        [ProtoMember(2)] public AquaFishBlockStage Stage;
        [ProtoMember(3)] public float Attract;
        [ProtoMember(4)] public float Catch;
        [ProtoMember(5)] public float CatchResult;
        // cached environment
        [ProtoIgnore] public float CurrentDepth;
        [ProtoIgnore] public float CurrentSalt;
    }

    /// <summary>
    /// Plant Database
    /// </summary>
    public static class AquaPlantDatabase
    {
        private static readonly Dictionary<int, AquaPlantDefinition> plantsById = new Dictionary<int, AquaPlantDefinition>();
        private static readonly  Dictionary<string, AquaPlantDefinition> plantsByName = new Dictionary<string, AquaPlantDefinition>();
        private static readonly Dictionary<string, int> componentToPlant = new Dictionary<string, int>();
        private static readonly Dictionary<string, AquaPlantDefinition> plants = new Dictionary<string, AquaPlantDefinition>();

        /// <summary>
        /// Init Database
        /// </summary>
        public static void Init()
        {
            Register(new AquaPlantDefinition
            {
                NumericId = 1,
                Id = "SeaweedGreen",
                BaseGrowTime = 180f,
                PlantTime = 300f,
                HarvestTime = 420f,
                OptimalDepth = 25f,
                ToUp = 7.5f,
                ToDown = 7.5f,
                FalloffUp = 5f,
                FalloffDown = 5f,
                OptimalSalt = 0.8f,
                SaltTolerance = 0.3f,
                SaltFallof = 0.1f,
                PowerIdle = 0.01f,
                PowerWork = 0.05f,
                StageModels  = new string[]
                {
                  AquaModpathUtils.GetModPaths(AquaModpathUtils.GetDetailedModelPath("SeaweedGreen_S1.mwm")),
                  AquaModpathUtils.GetModPaths(AquaModpathUtils.GetDetailedModelPath("SeaweedGreen_S2.mwm")),
                  AquaModpathUtils.GetModPaths(AquaModpathUtils.GetDetailedModelPath("SeaweedGreen_S3.mwm"))
                }
            });
            Register(new AquaPlantDefinition
            {
                NumericId = 2,
                Id = "SeaweedBrown",
                BaseGrowTime = 300f,
                PlantTime = 540f,
                HarvestTime = 660f,
                OptimalDepth = 50f,
                ToUp = 12.5f,
                ToDown = 12.5f,
                FalloffUp = 5f,
                FalloffDown = 5f,
                OptimalSalt = 0.8f,
                SaltTolerance = 0.3f,
                SaltFallof = 0.1f,
                PowerIdle = 0.01f,
                PowerWork = 0.05f,
                StageModels = new string[]
                {
                  AquaModpathUtils.GetModPaths(AquaModpathUtils.GetDetailedModelPath("SeaweedBrown_S1.mwm")),
                  AquaModpathUtils.GetModPaths(AquaModpathUtils.GetDetailedModelPath("SeaweedBrown_S2.mwm")),
                  AquaModpathUtils.GetModPaths(AquaModpathUtils.GetDetailedModelPath("SeaweedBrown_S3.mwm"))
                }
            });
            Register(new AquaPlantDefinition
            {
                NumericId = 3,
                Id = "SeaweedRed",
                BaseGrowTime = 480f,
                PlantTime = 840f,
                HarvestTime = 1080f,
                OptimalDepth = 90f,
                ToUp = 20f,
                ToDown = 20f,
                FalloffUp = 5f,
                FalloffDown = 5f,
                OptimalSalt = 0.8f,
                SaltTolerance = 0.3f,
                SaltFallof = 0.1f,
                PowerIdle = 0.01f,
                PowerWork = 0.05f,
                StageModels = new string[]
                {
                  AquaModpathUtils.GetModPaths(AquaModpathUtils.GetDetailedModelPath("SeaweedRed_S1.mwm")),
                  AquaModpathUtils.GetModPaths(AquaModpathUtils.GetDetailedModelPath("SeaweedRed_S2.mwm")),
                  AquaModpathUtils.GetModPaths(AquaModpathUtils.GetDetailedModelPath("SeaweedRed_S3.mwm"))
                }
            });
        }

        /// <summary>
        /// Register Plant
        /// </summary>
        /// <param name="def"></param>
        private static void Register(AquaPlantDefinition def)
        {
            plantsById[def.NumericId] = def;
            plantsByName[def.Id] = def;
        }

        /// <summary>
        /// Get Plant by ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static AquaPlantDefinition Get(int id)
        {
            AquaPlantDefinition def;
            if (plantsById.TryGetValue(id, out def))
                return def;
            AquaExpansionSession.Insance.Log(true, $"Plant NOT FOUND (id): {id}");
            return null;
        }

        /// <summary>
        /// Get Plant by String
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static AquaPlantDefinition Get(string id)
        {
            AquaPlantDefinition def;
            if (plants.TryGetValue(id, out def))
                return def;
            AquaExpansionSession.Insance.Log(true, $"PlantDefinition NOT FOUND: {id}");
            return null;
        }

        /// <summary>
        /// Map Component by ID
        /// </summary>
        /// <param name="componentSubtype"></param>
        /// <param name="plantId"></param>
        public static void MapComponent(string componentSubtype, int plantId)
        {
            componentToPlant[componentSubtype] = plantId;
        }

        /// <summary>
        /// Get Component
        /// </summary>
        /// <param name="subtype"></param>
        /// <returns></returns>
        public static AquaPlantDefinition GetByComponent(string subtype)
        {
            int id;
            if (componentToPlant.TryGetValue(subtype, out id))
                return Get(id);

            return null;
        }

        /// <summary>
        /// Validate Database
        /// </summary>
        /// <exception cref="Exception"></exception>
        public static void Validate()
        {
            foreach (var p in plantsByName.Values)
            {
                if (p.NumericId <= 0)
                    throw new Exception($"Invalid NumericId for plant: {p.Id}");
                if (string.IsNullOrEmpty(p.Id))
                    throw new Exception($"Plant missing string Id");
            }
        }
    }

    /// <summary>
    /// Fish Database
    /// </summary>
    public static class AquaFishDatabase
    {
        private static readonly Dictionary<int, AquaFishDefinition> fishsById = new Dictionary<int, AquaFishDefinition>();
        private static readonly Dictionary<string, AquaFishDefinition> fishesByName = new Dictionary<string, AquaFishDefinition>();
        private static readonly Dictionary<string, AquaFishDefinition> fishes = new Dictionary<string, AquaFishDefinition>();

        /// <summary>
        /// Init
        /// </summary>
        public static void Init()
        {
            Register(new AquaFishDefinition
            {
                NumericId = 1,
                Id = "Barracuda",
                BaseAttractTime = 25f,
                BaseCatchTime = 28f,
                BaseCatchResultTime = 20f,
                OptimalDepth = 20f,
                ToUp = 8f,
                ToDown = 12f,
                FalloffUp = 0.6f,
                FalloffDown = 0.6f,
                BaseCatchChance = 0.65f,
                OptimalSalt = 0.8f,
                SaltTolerance = 0.15f,
                SaltFalloff = 0.7f,
                //Model = AquaModpathUtils.GetWaterModPath(AquaModpathUtils.GetDetailedModelPath("Fish_Barracuda.mwm"))
                Model = AquaModpathUtils.GetWaterModPath(GetFishModelbysubtype(AquaFishItemsDatabase.GetFishbyID(1)))

            });
            Register(new AquaFishDefinition
            {
                NumericId = 2,
                Id = "Blue Tang",
                BaseAttractTime = 15f,
                BaseCatchTime = 12f,
                BaseCatchResultTime = 10f,
                OptimalDepth = 12f,
                ToUp = 3f,
                ToDown = 6f,
                FalloffUp = 0.5f,
                FalloffDown = 0.5f,
                BaseCatchChance = 0.90f,
                OptimalSalt = 0.6f,
                SaltTolerance = 0.25f,
                SaltFalloff = 0.5f,
                //Model = AquaModpathUtils.GetWaterModPath(AquaModpathUtils.GetDetailedModelPath("Fish_BlueTang.mwm"))
                Model = AquaModpathUtils.GetWaterModPath(GetFishModelbysubtype(AquaFishItemsDatabase.GetFishbyID(2)))
            });
            Register(new AquaFishDefinition
            {
                NumericId = 3,
                Id = "Bottlenose Dolphin",
                BaseAttractTime = 80f,
                BaseCatchTime = 60f,
                BaseCatchResultTime = 50f,
                OptimalDepth = 20f,
                ToUp = 8f,
                ToDown = 15f,
                FalloffUp = 0.7f,
                FalloffDown = 0.8f,
                BaseCatchChance = 0.25f,
                OptimalSalt = 0.8f,
                SaltTolerance = 0.2f,
                SaltFalloff = 0.6f,
                //Model = AquaModpathUtils.GetWaterModPath(AquaModpathUtils.GetDetailedModelPath("Fish_BottlenoseDolphin.mwm"))
                Model = AquaModpathUtils.GetWaterModPath(GetFishModelbysubtype(AquaFishItemsDatabase.GetFishbyID(3)))
            });
            Register(new AquaFishDefinition
            {
                NumericId = 4,
                Id = "Clown Fish",
                BaseAttractTime = 12f,
                BaseCatchTime = 10f,
                BaseCatchResultTime = 8f,
                OptimalDepth = 10f,
                ToUp = 2f,
                ToDown = 5f,
                FalloffUp = 0.4f,
                FalloffDown = 0.4f,
                BaseCatchChance = 0.92f,
                OptimalSalt = 0.5f,
                SaltTolerance = 0.3f,
                SaltFalloff = 0.4f,
                //Model = AquaModpathUtils.GetWaterModPath(AquaModpathUtils.GetDetailedModelPath("Fish_ClownFish.mwm"))
                Model = AquaModpathUtils.GetWaterModPath(GetFishModelbysubtype(AquaFishItemsDatabase.GetFishbyID(4)))
            });
            Register(new AquaFishDefinition
            {
                NumericId = 5,
                Id = "Giant Grouper",
                BaseAttractTime = 35f,
                BaseCatchTime = 40f,
                BaseCatchResultTime = 30f,
                OptimalDepth = 30f,
                ToUp = 10f,
                ToDown = 15f,
                FalloffUp = 0.6f,
                FalloffDown = 0.7f,
                BaseCatchChance = 0.55f,
                OptimalSalt = 0.6f,
                SaltTolerance = 0.25f,
                SaltFalloff = 0.5f,
                //Model = AquaModpathUtils.GetWaterModPath(AquaModpathUtils.GetDetailedModelPath("Fish_GiantGrouper.mwm"))
                Model = AquaModpathUtils.GetWaterModPath(GetFishModelbysubtype(AquaFishItemsDatabase.GetFishbyID(5)))
            });
            Register(new AquaFishDefinition
            {
                NumericId = 6,
                Id = "Lion Fish",
                BaseAttractTime = 20f,
                BaseCatchTime = 18f,
                BaseCatchResultTime = 15f,
                OptimalDepth = 18f,
                ToUp = 6f,
                ToDown = 10f,
                FalloffUp = 0.7f,
                FalloffDown = 0.7f,
                BaseCatchChance = 0.70f,
                OptimalSalt = 0.7f,
                SaltTolerance = 0.2f,
                SaltFalloff = 0.6f,
                //Model = AquaModpathUtils.GetWaterModPath(AquaModpathUtils.GetDetailedModelPath("Fish_LionFish.mwm"))
                Model = AquaModpathUtils.GetWaterModPath(GetFishModelbysubtype(AquaFishItemsDatabase.GetFishbyID(6)))
            });
            Register(new AquaFishDefinition
            {
                NumericId = 7,
                Id = "Nurse Shark",
                BaseAttractTime = 40f,
                BaseCatchTime = 50f,
                BaseCatchResultTime = 35f,
                OptimalDepth = 35f,
                ToUp = 12f,
                ToDown = 20f,
                FalloffUp = 0.7f,
                FalloffDown = 0.8f,
                BaseCatchChance = 0.45f,
                OptimalSalt = 0.9f,
                SaltTolerance = 0.1f,
                SaltFalloff = 0.8f,
                //Model = AquaModpathUtils.GetWaterModPath(AquaModpathUtils.GetDetailedModelPath("Fish_NurseShark.mwm"))
                Model = AquaModpathUtils.GetWaterModPath(GetFishModelbysubtype(AquaFishItemsDatabase.GetFishbyID(7)))
            });
            Register(new AquaFishDefinition
            {
                NumericId = 8,
                Id = "Reef Fish A",
                BaseAttractTime = 18f,
                BaseCatchTime = 15f,
                BaseCatchResultTime = 12f,
                OptimalDepth = 15f,
                ToUp = 5f,
                ToDown = 8f,
                FalloffUp = 0.5f,
                FalloffDown = 0.6f,
                BaseCatchChance = 0.85f,
                OptimalSalt = 0.5f,
                SaltTolerance = 0.3f,
                SaltFalloff = 0.4f,
                //Model = AquaModpathUtils.GetWaterModPath(AquaModpathUtils.GetDetailedModelPath("Fish_ReefFish0.mwm"))
                Model = AquaModpathUtils.GetWaterModPath(GetFishModelbysubtype(AquaFishItemsDatabase.GetFishbyID(8)))
            });
            Register(new AquaFishDefinition
            {
                NumericId = 9,
                Id = "Reef Fish B",
                BaseAttractTime = 18f,
                BaseCatchTime = 15f,
                BaseCatchResultTime = 12f,
                OptimalDepth = 15f,
                ToUp = 5f,
                ToDown = 8f,
                FalloffUp = 0.5f,
                FalloffDown = 0.6f,
                BaseCatchChance = 0.85f,
                OptimalSalt = 0.5f,
                SaltTolerance = 0.3f,
                SaltFalloff = 0.4f,
                //Model = AquaModpathUtils.GetWaterModPath(AquaModpathUtils.GetDetailedModelPath("Fish_ReefFish3.mwm"))
                Model = AquaModpathUtils.GetWaterModPath(GetFishModelbysubtype(AquaFishItemsDatabase.GetFishbyID(9)))
            });
            Register(new AquaFishDefinition
            {
                NumericId = 10,
                Id = "Reef Fish C",
                BaseAttractTime = 18f,
                BaseCatchTime = 15f,
                BaseCatchResultTime = 12f,
                OptimalDepth = 15f,
                ToUp = 5f,
                ToDown = 8f,
                FalloffUp = 0.5f,
                FalloffDown = 0.6f,
                BaseCatchChance = 0.85f,
                OptimalSalt = 0.5f,
                SaltTolerance = 0.3f,
                SaltFalloff = 0.4f,
                //Model = AquaModpathUtils.GetWaterModPath(AquaModpathUtils.GetDetailedModelPath("Fish_ReefFish4.mwm"))
                Model = AquaModpathUtils.GetWaterModPath(GetFishModelbysubtype(AquaFishItemsDatabase.GetFishbyID(10)))
            });
            Register(new AquaFishDefinition
            {
                NumericId = 11,
                Id = "Reef Fish D",
                BaseAttractTime = 18f,
                BaseCatchTime = 15f,
                BaseCatchResultTime = 12f,
                OptimalDepth = 15f,
                ToUp = 5f,
                ToDown = 8f,
                FalloffUp = 0.5f,
                FalloffDown = 0.6f,
                BaseCatchChance = 0.85f,
                OptimalSalt = 0.5f,
                SaltTolerance = 0.3f,
                SaltFalloff = 0.4f,
                //Model = AquaModpathUtils.GetWaterModPath(AquaModpathUtils.GetDetailedModelPath("Fish_ReefFish5.mwm"))
                Model = AquaModpathUtils.GetWaterModPath(GetFishModelbysubtype(AquaFishItemsDatabase.GetFishbyID(11)))
            });
            Register(new AquaFishDefinition
            {
                NumericId = 12,
                Id = "Reef Fish E",
                BaseAttractTime = 18f,
                BaseCatchTime = 15f,
                BaseCatchResultTime = 12f,
                OptimalDepth = 15f,
                ToUp = 5f,
                ToDown = 8f,
                FalloffUp = 0.5f,
                FalloffDown = 0.6f,
                BaseCatchChance = 0.85f,
                OptimalSalt = 0.5f,
                SaltTolerance = 0.3f,
                SaltFalloff = 0.4f,
                //Model = AquaModpathUtils.GetWaterModPath(AquaModpathUtils.GetDetailedModelPath("Fish_ReefFish7.mwm"))
                Model = AquaModpathUtils.GetWaterModPath(GetFishModelbysubtype(AquaFishItemsDatabase.GetFishbyID(12)))
            });
            Register(new AquaFishDefinition
            {
                NumericId = 13,
                Id = "Reef Fish F",
                BaseAttractTime = 18f,
                BaseCatchTime = 15f,
                BaseCatchResultTime = 12f,
                OptimalDepth = 15f,
                ToUp = 5f,
                ToDown = 8f,
                FalloffUp = 0.5f,
                FalloffDown = 0.6f,
                BaseCatchChance = 0.85f,
                OptimalSalt = 0.5f,
                SaltTolerance = 0.3f,
                SaltFalloff = 0.4f,
                //Model = AquaModpathUtils.GetWaterModPath(AquaModpathUtils.GetDetailedModelPath("Fish_ReefFish8.mwm"))
                Model = AquaModpathUtils.GetWaterModPath(GetFishModelbysubtype(AquaFishItemsDatabase.GetFishbyID(13)))
            });
            Register(new AquaFishDefinition
            {
                NumericId = 14,
                Id = "Reef Fish G",
                BaseAttractTime = 18f,
                BaseCatchTime = 15f,
                BaseCatchResultTime = 12f,
                OptimalDepth = 15f,
                ToUp = 5f,
                ToDown = 8f,
                FalloffUp = 0.5f,
                FalloffDown = 0.6f,
                BaseCatchChance = 0.85f,
                OptimalSalt = 0.5f,
                SaltTolerance = 0.3f,
                SaltFalloff = 0.4f,
                //Model = AquaModpathUtils.GetWaterModPath(AquaModpathUtils.GetDetailedModelPath("Fish_ReefFish12.mwm"))
                Model = AquaModpathUtils.GetWaterModPath(GetFishModelbysubtype(AquaFishItemsDatabase.GetFishbyID(14)))
            });
            Register(new AquaFishDefinition
            {
                NumericId = 15,
                Id = "Reef Fish H",
                BaseAttractTime = 18f,
                BaseCatchTime = 15f,
                BaseCatchResultTime = 12f,
                OptimalDepth = 15f,
                ToUp = 5f,
                ToDown = 8f,
                FalloffUp = 0.5f,
                FalloffDown = 0.6f,
                BaseCatchChance = 0.85f,
                OptimalSalt = 0.5f,
                SaltTolerance = 0.3f,
                SaltFalloff = 0.4f,
                //Model = AquaModpathUtils.GetWaterModPath(AquaModpathUtils.GetDetailedModelPath("Fish_ReefFish14.mwm"))
                Model = AquaModpathUtils.GetWaterModPath(GetFishModelbysubtype(AquaFishItemsDatabase.GetFishbyID(15)))
            });
            Register(new AquaFishDefinition
            {
                NumericId = 16,
                Id = "Reef Fish I",
                BaseAttractTime = 18f,
                BaseCatchTime = 15f,
                BaseCatchResultTime = 12f,
                OptimalDepth = 15f,
                ToUp = 5f,
                ToDown = 8f,
                FalloffUp = 0.5f,
                FalloffDown = 0.6f,
                BaseCatchChance = 0.85f,
                OptimalSalt = 0.5f,
                SaltTolerance = 0.3f,
                SaltFalloff = 0.4f,
                //Model = AquaModpathUtils.GetWaterModPath(AquaModpathUtils.GetDetailedModelPath("Fish_ReefFish16.mwm"))
                Model = AquaModpathUtils.GetWaterModPath(GetFishModelbysubtype(AquaFishItemsDatabase.GetFishbyID(16)))
            });
            Register(new AquaFishDefinition
            {
                NumericId = 17,
                Id = "Reef Fish J",
                BaseAttractTime = 18f,
                BaseCatchTime = 15f,
                BaseCatchResultTime = 12f,
                OptimalDepth = 15f,
                ToUp = 5f,
                ToDown = 8f,
                FalloffUp = 0.5f,
                FalloffDown = 0.6f,
                BaseCatchChance = 0.85f,
                OptimalSalt = 0.5f,
                SaltTolerance = 0.3f,
                SaltFalloff = 0.4f,
                //Model = AquaModpathUtils.GetWaterModPath(AquaModpathUtils.GetDetailedModelPath("Fish_ReefFish17.mwm"))
                Model = AquaModpathUtils.GetWaterModPath(GetFishModelbysubtype(AquaFishItemsDatabase.GetFishbyID(17)))
            });
            Register(new AquaFishDefinition
            {
                NumericId = 18,
                Id = "Reef Fish K",
                BaseAttractTime = 18f,
                BaseCatchTime = 15f,
                BaseCatchResultTime = 12f,
                OptimalDepth = 15f,
                ToUp = 5f,
                ToDown = 8f,
                FalloffUp = 0.5f,
                FalloffDown = 0.6f,
                BaseCatchChance = 0.85f,
                OptimalSalt = 0.5f,
                SaltTolerance = 0.3f,
                SaltFalloff = 0.4f,
                //Model = AquaModpathUtils.GetWaterModPath(AquaModpathUtils.GetDetailedModelPath("Fish_ReefFish20.mwm"))
                Model = AquaModpathUtils.GetWaterModPath(GetFishModelbysubtype(AquaFishItemsDatabase.GetFishbyID(18)))
            });
            Register(new AquaFishDefinition
            {
                NumericId = 19,
                Id = "Sea Turtle",
                BaseAttractTime = 60f,
                BaseCatchTime = 45f,
                BaseCatchResultTime = 40f,
                OptimalDepth = 15f,
                ToUp = 5f,
                ToDown = 10f,
                FalloffUp = 0.6f,
                FalloffDown = 0.6f,
                BaseCatchChance = 0.35f,
                OptimalSalt = 0.6f,
                SaltTolerance = 0.3f,
                SaltFalloff = 0.4f,
                //Model = AquaModpathUtils.GetWaterModPath(AquaModpathUtils.GetDetailedModelPath("Fish_SeaTurtle.mwm"))
                Model = AquaModpathUtils.GetWaterModPath(GetFishModelbysubtype(AquaFishItemsDatabase.GetFishbyID(19)))
            });
            Register(new AquaFishDefinition
            {
                NumericId = 20,
                Id = "Tiny Yellow Fish",
                BaseAttractTime = 12f,
                BaseCatchTime = 10f,
                BaseCatchResultTime = 8f,
                OptimalDepth = 10f,
                ToUp = 2f,
                ToDown = 5f,
                FalloffUp = 0.4f,
                FalloffDown = 0.4f,
                BaseCatchChance = 0.95f,
                OptimalSalt = 0.5f,
                SaltTolerance = 0.3f,
                SaltFalloff = 0.4f,
                //Model = AquaModpathUtils.GetWaterModPath(AquaModpathUtils.GetDetailedModelPath("Fish_TinyYellowFish.mwm"))
                Model = AquaModpathUtils.GetWaterModPath(GetFishModelbysubtype(AquaFishItemsDatabase.GetFishbyID(20)))
            });
            Register(new AquaFishDefinition
            {
                NumericId = 21,
                Id = "Yellow Tang",
                BaseAttractTime = 15f,
                BaseCatchTime = 12f,
                BaseCatchResultTime = 10f,
                OptimalDepth = 12f,
                ToUp = 3f,
                ToDown = 6f,
                FalloffUp = 0.5f,
                FalloffDown = 0.5f,
                BaseCatchChance = 0.90f,
                OptimalSalt = 0.6f,
                SaltTolerance = 0.25f,
                SaltFalloff = 0.5f,
                //Model = AquaModpathUtils.GetWaterModPath(AquaModpathUtils.GetDetailedModelPath("Fish_YellowTang.mwm"))
                Model = AquaModpathUtils.GetWaterModPath(GetFishModelbysubtype(AquaFishItemsDatabase.GetFishbyID(21)))
            });
            Register(new AquaFishDefinition
            {
                NumericId = 22,
                Id = "Whale Shark",
                BaseAttractTime = 120f,
                BaseCatchTime = 180f,
                BaseCatchResultTime = 60f,
                OptimalDepth = 50f,
                ToUp = 20f,
                ToDown = 30f,
                FalloffUp = 0.9f,
                FalloffDown = 0.9f,
                BaseCatchChance = 0.10f,
                OptimalSalt = 0.9f,
                SaltTolerance = 0.05f,
                SaltFalloff = 0.9f,
                //Model = AquaModpathUtils.GetWaterModPath(AquaModpathUtils.GetDetailedModelPath("Fish_WhaleShark.mwm"))
                Model = AquaModpathUtils.GetWaterModPath(GetFishModelbysubtype(AquaFishItemsDatabase.GetFishbyID(22)))
            });
        }

        /// <summary>
        /// Register Fish
        /// </summary>
        /// <param name="def"></param>
        private static void Register(AquaFishDefinition def)
        {
            fishsById[def.NumericId] = def;
            fishesByName[def.Id] = def;
        }

        /// <summary>
        /// Get Fish by ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static AquaFishDefinition Get(int id)
        {
            AquaFishDefinition def;
            if (fishsById.TryGetValue(id, out def))
                return def;
            AquaExpansionSession.Insance.Log(true, $"Fish NOT FOUND (id): {id}");
            return null;
        }

        /// <summary>
        /// Get Fish by string
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static AquaFishDefinition Get(string id)
        {
            AquaFishDefinition def;
            if (fishes.TryGetValue(id, out def))
                return def;
            AquaExpansionSession.Insance.Log(true, $"FishDefinition NOT FOUND: {id}");
            return null;
        }

        /// <summary>
        /// Get model from subtype
        /// </summary>
        /// <param name="subtype"></param>
        /// <returns></returns>
        public static string GetFishModelbysubtype(string subtype)
        {
            if (string.IsNullOrEmpty(subtype))
                return null;
            string model = "";
            var def = MyDefinitionManager.Static.GetPhysicalItemDefinition(AquaModpathUtils.GetSubtypebyObjectBuilder(subtype, AquaDefinitionOB.ConsumableItem));
            if (def == null || string.IsNullOrEmpty(def.Model))
                return null;
            model = def.Model;
            int index = model.IndexOf("Cubes\\large\\", StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
                model = model.Substring(index);
            return model;
        }

        /// <summary>
        /// Validate
        /// </summary>
        /// <exception cref="Exception"></exception>
        public static void Validate()
        {
            foreach (var p in fishesByName.Values)
            {
                if (p.NumericId <= 0)
                    throw new Exception($"Invalid NumericId for fish: {p.Id}");
                if (string.IsNullOrEmpty(p.Id))
                    throw new Exception($"Fish missing string Id");
            }
        }
    }

    /// <summary>
    /// SavePlantData
    /// </summary>
    [ProtoContract]
    public class AquaSavePlantData
    {
        [ProtoMember(1)] public int DefId;
        [ProtoMember(2)] public int Stage;
        [ProtoMember(3)] public float Growth;
        [ProtoMember(4)] public float Planting;
        [ProtoMember(5)] public float Harvesting;
    }

    /// <summary>
    /// Save FishData
    /// </summary>
    [ProtoContract]
    public class AquaSaveFishData
    {
        [ProtoMember(1)] public int DefId;
        [ProtoMember(2)] public int Stage;
        [ProtoMember(3)] public float Attract;
        [ProtoMember(4)] public float Catch;
        [ProtoMember(5)] public float CatchResult;
    }

    /// <summary>
    /// SaveData
    /// </summary>
    [ProtoContract]
    public class AquaFarmSaveData
    {
        [ProtoMember(1)] public AquaSavePlantData Plant;
        [ProtoMember(2)] public int RecipeId;
        [ProtoMember(3)] public int ModelStageId;
    }

    /// <summary>
    /// Save FishingData
    /// </summary>
    [ProtoContract]
    public class AquaFishingSaveData
    {
        [ProtoMember(1)] public AquaSaveFishData Fish;
        [ProtoMember(2)] public int RecipeId;
        [ProtoMember(3)] public int BaitModelStageId;
        [ProtoMember(4)] public int FishModelStageId;
    }

    /// <summary>
    /// Recipe Database
    /// </summary>
    public static class AquaRecipeDatabase
    {
        private static readonly Dictionary<int, AquaFarmingRecipe> recipesById = new Dictionary<int, AquaFarmingRecipe>();
        private static readonly Dictionary<string, AquaFarmingRecipe> recipesByName = new Dictionary<string, AquaFarmingRecipe>();
        private static readonly Dictionary<string, int> plantToRecipe = new Dictionary<string, int>();

        /// <summary>
        /// Init registered Recipes
        /// </summary>
        public static void Init()
        {
            Register(new AquaFarmingRecipe(1, "Seaweed (Green)", 1)
            {
                SporeToGrowPrerequisites = new Dictionary<MyDefinitionId, MyFixedPoint>
               {
                   {
                      SetSource(AquaFarmItemsDatabase.GetSporebyID(1)),
                      1
                   }
               },
                PlantResult = AquaPlantDatabase.Get(1),
                PlantToCropResults = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetRaw(AquaFarmItemsDatabase.GetCropbyID(1)),
                        8
                    }
                }
            });
            Register(new AquaFarmingRecipe(2, "Seaweed (Brown)", 1)
            {
                SporeToGrowPrerequisites = new Dictionary<MyDefinitionId, MyFixedPoint>
               {
                   {
                       SetSource(AquaFarmItemsDatabase.GetSporebyID(2)),
                      1
                   }
               },
                PlantResult = AquaPlantDatabase.Get(2),
                PlantToCropResults = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetRaw(AquaFarmItemsDatabase.GetCropbyID(2)),
                        14
                    }
                }
            });
            Register(new AquaFarmingRecipe(3, "Seaweed (Red)", 1)
            {
                SporeToGrowPrerequisites = new Dictionary<MyDefinitionId, MyFixedPoint>
               {
                   {
                       SetSource(AquaFarmItemsDatabase.GetSporebyID(3)),
                      1
                   }
               },
                PlantResult = AquaPlantDatabase.Get(3),
                PlantToCropResults = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetRaw(AquaFarmItemsDatabase.GetCropbyID(3)),
                         26
                    }
                }
            });
        }

        /// <summary>
        /// Register Recipe in Database
        /// </summary>
        /// <param name="recipe"></param>
        private static void Register(AquaFarmingRecipe recipe)
        {
            recipesById[recipe.Id] = recipe;
            recipesByName[recipe.Displayname] = recipe;
        }

        /// <summary>
        /// Get recipe by ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static AquaFarmingRecipe Get(int id)
        {
            AquaFarmingRecipe recipe;
            if (recipesById.TryGetValue(id, out recipe))
                return recipe;
            AquaExpansionSession.Insance.Log(true, $"Recipe NOT FOUND (id): {id}");
            return null;
        }

        /// <summary>
        /// Get recipe by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static AquaFarmingRecipe Get(string name)
        {
            AquaFarmingRecipe recipe;
            if (recipesByName.TryGetValue(name, out recipe))
                return recipe;
            AquaExpansionSession.Insance.Log(true, $"Recipe NOT FOUND (name): {name}");
            return null;
        }

        /// <summary>
        /// Map Plant by ID
        /// </summary>
        /// <param name="plantId"></param>
        /// <param name="recipeId"></param>
        public static void MapPlant(string plantId, int recipeId)
        {
            plantToRecipe[plantId] = recipeId;
        }

        /// <summary>
        /// Get by Plant ID
        /// </summary>
        /// <param name="plantId"></param>
        /// <returns></returns>
        public static AquaFarmingRecipe GetByPlant(string plantId)
        {
            int id;
            if (plantToRecipe.TryGetValue(plantId, out id))
                return Get(id);
            return null;
        }

        /// <summary>
        /// Validate Recipe Database
        /// </summary>
        /// <exception cref="Exception"></exception>
        public static void Validate()
        {
            foreach (var r in recipesById.Values)
            {
                if (r.Id <= 0)
                    throw new Exception($"Invalid Recipe Id: {r.Displayname}");
                if (r.Id <= 0)
                    throw new Exception($"Recipe {r.Displayname} missing PlantDefId");
            }
        }

        /// <summary>
        /// Get Recipes
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<AquaFarmingRecipe> GetAll()
        {
            return recipesById.Values;
        }

        /// <summary>
        /// Set Item Input
        /// </summary>
        /// <param name="subtype"></param>
        /// <returns></returns>
        private static MyDefinitionId SetSource(string subtype)
        {
            return MyDefinitionId.Parse($"MyObjectBuilder_Component/{subtype}");
        }

        /// <summary>
        /// Set Item output
        /// </summary>
        /// <param name="subtype"></param>
        /// <returns></returns>
        private static MyDefinitionId SetRaw(string subtype)
        {
            return MyDefinitionId.Parse($"MyObjectBuilder_ConsumableItem/{subtype}");
        }
    }

    /// <summary>
    /// Fishing Recipe database
    /// </summary>
    public static class AquaFishingRecipeDatabase
    {
        private static readonly Dictionary<int, AquaFishingRecipe> recipesById = new Dictionary<int, AquaFishingRecipe>();
        private static readonly Dictionary<string, AquaFishingRecipe> recipesByName = new Dictionary<string, AquaFishingRecipe>();
        private static readonly Dictionary<int, AquaFishingRecipe> BigFishrecipesById = new Dictionary<int, AquaFishingRecipe>();
        private static readonly Dictionary<string, AquaFishingRecipe> BigFishrecipesByName = new Dictionary<string, AquaFishingRecipe>();
        private static readonly Dictionary<string, AquaFishingRecipe> recipebyrawfishsubtype = new Dictionary<string, AquaFishingRecipe>();
        private static readonly Dictionary<string, AquaFishingRecipe> recipebyrawbigfishsubtype = new Dictionary<string, AquaFishingRecipe>();
        /// <summary>
        /// Init
        /// </summary>
        public static void Init()
        {
            //Standart recipes
            Register(new AquaFishingRecipe(1, "Barracuda", 1)
            {
                BaitPrerequisites = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetBait(AquaFishItemsDatabase.GetBaitbyID(2)),
                        1
                    }
                },
                FishResult = AquaFishDatabase.Get(1),
                BaitToFishResults = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetFishRaw(AquaFishItemsDatabase.GetFishbyID(1)),
                        1
                    }
                },
                Oceanlayer = AquaEnviromentUtils.GetOceanLayer(AquaFishDatabase.Get(1).OptimalDepth)
            });
            Register(new AquaFishingRecipe(2, "Blue Tang", 1)
            {
                BaitPrerequisites = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetBait(AquaFishItemsDatabase.GetBaitbyID(1)),
                        1
                    }
                },
                FishResult = AquaFishDatabase.Get(2),
                BaitToFishResults = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetFishRaw(AquaFishItemsDatabase.GetFishbyID(2)),
                        1
                    }
                },
                Oceanlayer = AquaEnviromentUtils.GetOceanLayer(AquaFishDatabase.Get(2).OptimalDepth)
            });
            Register(new AquaFishingRecipe(3, "Clown Fish", 1)
            {
                BaitPrerequisites = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetBait(AquaFishItemsDatabase.GetBaitbyID(1)),
                        1
                    }
                },
                FishResult = AquaFishDatabase.Get(4),
                BaitToFishResults = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetFishRaw(AquaFishItemsDatabase.GetFishbyID(4)),
                        1
                    }
                },
                Oceanlayer = AquaEnviromentUtils.GetOceanLayer(AquaFishDatabase.Get(4).OptimalDepth)
            });
            Register(new AquaFishingRecipe(4, "Giant Grouper", 1)
            {
                BaitPrerequisites = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetBait(AquaFishItemsDatabase.GetBaitbyID(2)),
                        1
                    }
                },
                FishResult = AquaFishDatabase.Get(5),
                BaitToFishResults = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetFishRaw(AquaFishItemsDatabase.GetFishbyID(5)),
                        1
                    }
                },
                Oceanlayer = AquaEnviromentUtils.GetOceanLayer(AquaFishDatabase.Get(5).OptimalDepth)
            });
            Register(new AquaFishingRecipe(5, "Lion Fish", 1)
            {
                BaitPrerequisites = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetBait(AquaFishItemsDatabase.GetBaitbyID(2)),
                        1
                    }
                },
                FishResult = AquaFishDatabase.Get(6),
                BaitToFishResults = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetFishRaw(AquaFishItemsDatabase.GetFishbyID(6)),
                        1
                    }
                },
                Oceanlayer = AquaEnviromentUtils.GetOceanLayer(AquaFishDatabase.Get(6).OptimalDepth)
            });
            Register(new AquaFishingRecipe(6, "Reef Fish A", 1)
            {
                BaitPrerequisites = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetBait(AquaFishItemsDatabase.GetBaitbyID(1)),
                        1
                    }
                },
                FishResult = AquaFishDatabase.Get(8),
                BaitToFishResults = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetFishRaw(AquaFishItemsDatabase.GetFishbyID(8)),
                        1
                    }
                },
                Oceanlayer = AquaEnviromentUtils.GetOceanLayer(AquaFishDatabase.Get(8).OptimalDepth)
            });
            Register(new AquaFishingRecipe(7, "Reef Fish B", 1)
            {
                BaitPrerequisites = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetBait(AquaFishItemsDatabase.GetBaitbyID(1)),
                        1
                    }
                },
                FishResult = AquaFishDatabase.Get(9),
                BaitToFishResults = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetFishRaw(AquaFishItemsDatabase.GetFishbyID(9)),
                        1
                    }
                },
                Oceanlayer = AquaEnviromentUtils.GetOceanLayer(AquaFishDatabase.Get(9).OptimalDepth)
            });
            Register(new AquaFishingRecipe(8, "Reef Fish C", 1)
            {
                BaitPrerequisites = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetBait(AquaFishItemsDatabase.GetBaitbyID(1)),
                        1
                    }
                },
                FishResult = AquaFishDatabase.Get(10),
                BaitToFishResults = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetFishRaw(AquaFishItemsDatabase.GetFishbyID(10)),
                        1
                    }
                },
                Oceanlayer = AquaEnviromentUtils.GetOceanLayer(AquaFishDatabase.Get(10).OptimalDepth)
            });
            Register(new AquaFishingRecipe(9, "Reef Fish D", 1)
            {
                BaitPrerequisites = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetBait(AquaFishItemsDatabase.GetBaitbyID(1)),
                        1
                    }
                },
                FishResult = AquaFishDatabase.Get(11),
                BaitToFishResults = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetFishRaw(AquaFishItemsDatabase.GetFishbyID(11)),
                        1
                    }
                },
                Oceanlayer = AquaEnviromentUtils.GetOceanLayer(AquaFishDatabase.Get(11).OptimalDepth)
            });
            Register(new AquaFishingRecipe(10, "Reef Fish E", 1)
            {
                BaitPrerequisites = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetBait(AquaFishItemsDatabase.GetBaitbyID(1)),
                        1
                    }
                },
                FishResult = AquaFishDatabase.Get(12),
                BaitToFishResults = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetFishRaw(AquaFishItemsDatabase.GetFishbyID(12)),
                        1
                    }
                },
                Oceanlayer = AquaEnviromentUtils.GetOceanLayer(AquaFishDatabase.Get(12).OptimalDepth)
            });
            Register(new AquaFishingRecipe(11, "Reef Fish F", 1)
            {
                BaitPrerequisites = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetBait(AquaFishItemsDatabase.GetBaitbyID(1)),
                        1
                    }
                },
                FishResult = AquaFishDatabase.Get(13),
                BaitToFishResults = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetFishRaw(AquaFishItemsDatabase.GetFishbyID(13)),
                        1
                    }
                },
                Oceanlayer = AquaEnviromentUtils.GetOceanLayer(AquaFishDatabase.Get(13).OptimalDepth)
            });
            Register(new AquaFishingRecipe(12, "Reef Fish G", 1)
            {
                BaitPrerequisites = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetBait(AquaFishItemsDatabase.GetBaitbyID(1)),
                        1
                    }
                },
                FishResult = AquaFishDatabase.Get(14),
                BaitToFishResults = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetFishRaw(AquaFishItemsDatabase.GetFishbyID(14)),
                        1
                    }
                },
                Oceanlayer = AquaEnviromentUtils.GetOceanLayer(AquaFishDatabase.Get(14).OptimalDepth)
            });
            Register(new AquaFishingRecipe(13, "Reef Fish H", 1)
            {
                BaitPrerequisites = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetBait(AquaFishItemsDatabase.GetBaitbyID(1)),
                        1
                    }
                },
                FishResult = AquaFishDatabase.Get(15),
                BaitToFishResults = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetFishRaw(AquaFishItemsDatabase.GetFishbyID(15)),
                        1
                    }
                },
                Oceanlayer = AquaEnviromentUtils.GetOceanLayer(AquaFishDatabase.Get(15).OptimalDepth)
            });
            Register(new AquaFishingRecipe(14, "Reef Fish I", 1)
            {
                BaitPrerequisites = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetBait(AquaFishItemsDatabase.GetBaitbyID(1)),
                        1
                    }
                },
                FishResult = AquaFishDatabase.Get(16),
                BaitToFishResults = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetFishRaw(AquaFishItemsDatabase.GetFishbyID(16)),
                        1
                    }
                },
                Oceanlayer = AquaEnviromentUtils.GetOceanLayer(AquaFishDatabase.Get(16).OptimalDepth)
            });
            Register(new AquaFishingRecipe(15, "Reef Fish J", 1)
            {
                BaitPrerequisites = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetBait(AquaFishItemsDatabase.GetBaitbyID(1)),
                        1
                    }
                },
                FishResult = AquaFishDatabase.Get(17),
                BaitToFishResults = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetFishRaw(AquaFishItemsDatabase.GetFishbyID(17)),
                        1
                    }
                },
                Oceanlayer = AquaEnviromentUtils.GetOceanLayer(AquaFishDatabase.Get(17).OptimalDepth)
            });
            Register(new AquaFishingRecipe(16, "Reef Fish K", 1)
            {
                BaitPrerequisites = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetBait(AquaFishItemsDatabase.GetBaitbyID(1)),
                        1
                    }
                },
                FishResult = AquaFishDatabase.Get(18),
                BaitToFishResults = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetFishRaw(AquaFishItemsDatabase.GetFishbyID(18)),
                        1
                    }
                },
                Oceanlayer = AquaEnviromentUtils.GetOceanLayer(AquaFishDatabase.Get(18).OptimalDepth)
            });
            Register(new AquaFishingRecipe(17, "Tiny Yellow Fish", 1)
            {
                BaitPrerequisites = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetBait(AquaFishItemsDatabase.GetBaitbyID(1)),
                        1
                    }
                },
                FishResult = AquaFishDatabase.Get(20),
                BaitToFishResults = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetFishRaw(AquaFishItemsDatabase.GetFishbyID(20)),
                        1
                    }
                },
                Oceanlayer = AquaEnviromentUtils.GetOceanLayer(AquaFishDatabase.Get(20).OptimalDepth)
            });
            Register(new AquaFishingRecipe(18, "Yellow Tang", 1)
            {
                BaitPrerequisites = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetBait(AquaFishItemsDatabase.GetBaitbyID(1)),
                        1
                    }
                },
                FishResult = AquaFishDatabase.Get(21),
                BaitToFishResults = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetFishRaw(AquaFishItemsDatabase.GetFishbyID(21)),
                        1
                    }
                },
                Oceanlayer = AquaEnviromentUtils.GetOceanLayer(AquaFishDatabase.Get(21).OptimalDepth)
            });

            //Big Fish recipes
            RegisterBigFish(new AquaFishingRecipe(1, "Nurse Shark", 1)
            {
                BaitPrerequisites = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetBait(AquaFishItemsDatabase.GetBaitbyID(2)),
                        1
                    }
                },
                FishResult = AquaFishDatabase.Get(7),
                BaitToFishResults = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetFishRaw(AquaFishItemsDatabase.GetFishbyID(7)),
                        1
                    }
                },
                Oceanlayer = AquaEnviromentUtils.GetOceanLayer(AquaFishDatabase.Get(7).OptimalDepth)
            });
            RegisterBigFish(new AquaFishingRecipe(2, "Sea Turtle", 1)
            {
                BaitPrerequisites = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetBait(AquaFishItemsDatabase.GetBaitbyID(3)),
                        1
                    }
                },
                FishResult = AquaFishDatabase.Get(19),
                BaitToFishResults = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetFishRaw(AquaFishItemsDatabase.GetFishbyID(19)),
                        1
                    }
                },
                Oceanlayer = AquaEnviromentUtils.GetOceanLayer(AquaFishDatabase.Get(19).OptimalDepth)
            });
            RegisterBigFish(new AquaFishingRecipe(3, "Bottlenose Dolphin", 1)
            {
                BaitPrerequisites = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetBait(AquaFishItemsDatabase.GetBaitbyID(3)),
                        1
                    }
                },
                FishResult = AquaFishDatabase.Get(3),
                BaitToFishResults = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetFishRaw(AquaFishItemsDatabase.GetFishbyID(3)),
                        1
                    }
                },
                Oceanlayer = AquaEnviromentUtils.GetOceanLayer(AquaFishDatabase.Get(3).OptimalDepth)
            });
            RegisterBigFish(new AquaFishingRecipe(4, "Whale Shark", 1)
            {
                BaitPrerequisites = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetBait(AquaFishItemsDatabase.GetBaitbyID(3)),
                        1
                    }
                },
                FishResult = AquaFishDatabase.Get(22),
                BaitToFishResults = new Dictionary<MyDefinitionId, MyFixedPoint>
                {
                    {
                        SetFishRaw(AquaFishItemsDatabase.GetFishbyID(22)),
                        1
                    }
                },
                Oceanlayer = AquaEnviromentUtils.GetOceanLayer(AquaFishDatabase.Get(22).OptimalDepth)
            });
        }

        /// <summary>
        /// Register standart recipe
        /// </summary>
        /// <param name="recipe"></param>
        private static void Register(AquaFishingRecipe recipe)
        {
            recipesById[recipe.Id] = recipe;
            recipesByName[recipe.Displayname] = recipe;
            foreach (var fishDef in recipe.BaitToFishResults.Keys)
            {
                string subtype = fishDef.SubtypeId.String;
                if (!string.IsNullOrEmpty(subtype))
                    recipebyrawfishsubtype[subtype] = recipe;
            }
        }

        /// <summary>
        /// Register Big Fish recipe
        /// </summary>
        /// <param name="recipe"></param>
        private static void RegisterBigFish(AquaFishingRecipe recipe)
        {
            BigFishrecipesById[recipe.Id] = recipe;
            BigFishrecipesByName[recipe.Displayname] = recipe;
            foreach (var fishDef in recipe.BaitToFishResults.Keys)
            {
                string subtype = fishDef.SubtypeId.String;
                if (!string.IsNullOrEmpty(subtype))
                    recipebyrawbigfishsubtype[subtype] = recipe;
            }
        }

        /// <summary>
        /// Get recipe by ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static AquaFishingRecipe Get(int id)
        {
            AquaFishingRecipe recipe;
            if (recipesById.TryGetValue(id, out recipe))
                return recipe;
            AquaExpansionSession.Insance.Log(true, $"Recipe NOT FOUND (id): {id}");
            return null;
        }

        /// <summary>
        /// Get BigFish recipe by ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static AquaFishingRecipe GetBigFish(int id)
        {
            AquaFishingRecipe recipe;
            if (BigFishrecipesById.TryGetValue(id, out recipe))
                return recipe;
            AquaExpansionSession.Insance.Log(true, $"Big Fish Recipe NOT FOUND (id): {id}");
            return null;
        }
        /// <summary>
        /// Get recipe by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static AquaFishingRecipe Get(string name)
        {
            AquaFishingRecipe recipe;
            if (recipesByName.TryGetValue(name, out recipe))
                return recipe;
            AquaExpansionSession.Insance.Log(true, $"Recipe NOT FOUND (name): {name}");
            return null;
        }

        /// <summary>
        /// Get Big Fish recipe by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static AquaFishingRecipe GetBigFish(string name)
        {
            AquaFishingRecipe recipe;
            if (BigFishrecipesByName.TryGetValue(name, out recipe))
                return recipe;
            AquaExpansionSession.Insance.Log(true, $"Big Fish Recipe NOT FOUND (name): {name}");
            return null;
        }

        /// <summary>
        /// Get recipe by subtype and block type
        /// </summary>
        /// <param name="subtype"></param>
        /// <returns></returns>
        public static AquaFishingRecipe GetRecipebySubtype(string subtype, AquaFarmingBlockType type)
        {
            AquaFishingRecipe recipe;
            switch (type)
            {
                case AquaFarmingBlockType.FishingBlock:
                    if (recipebyrawfishsubtype.TryGetValue(subtype, out recipe))
                        return recipe;
                    break;
                case AquaFarmingBlockType.FishingBlockAdvance:
                    if (recipebyrawbigfishsubtype.TryGetValue(subtype, out recipe))
                        return recipe;
                    break;
            }
            AquaExpansionSession.Insance.Log(true, $"Fish Recipe NOT FOUND (subtype): {subtype}");
            return null;
        }

        /// <summary>
        /// Get baitmodel from recipe
        /// </summary>
        /// <param name="recipe"></param>
        /// <returns></returns>
        public static string GetBaitModelfromRecipe(AquaFishingRecipe recipe)
        {
            if (recipe == null)
                return null;
            string model = "";
            string subtype;
            foreach (var key in recipe.BaitPrerequisites)
            {
                subtype = key.Key.SubtypeId.String;
                if (string.IsNullOrEmpty(subtype))
                    continue;
                var def = MyDefinitionManager.Static.GetPhysicalItemDefinition(SetBait(subtype));
                if (def == null || string.IsNullOrEmpty(def.Model))
                    continue;
                model = def.Model;
                int index = model.IndexOf("Items\\", StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                    model = model.Substring(index);
                return model;
            }
            return null;
        }

        /// <summary>
        /// Validate
        /// </summary>
        /// <exception cref="Exception"></exception>
        public static void Validate()
        {
            foreach (var r in recipesById.Values)
            {
                if (r.Id <= 0)
                    throw new Exception($"Invalid Recipe Id: {r.Displayname}");
                if (r.Id <= 0)
                    throw new Exception($"Recipe {r.Displayname} missing FishDefId");
            }
            foreach (var bf in BigFishrecipesById.Values)
            {
                if (bf.Id <= 0)
                    throw new Exception($"Invalid Big Fish Recipe Id: {bf.Displayname}");
                if (bf.Id <= 0)
                    throw new Exception($"Big Fish Recipe {bf.Displayname} missing FishDefId");
            }
        }

        /// <summary>
        /// Get All standart recipes
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<AquaFishingRecipe> GetAll()
        {
            return recipesById.Values;
        }
        /// <summary>
        /// Get All Big Fish recipes
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<AquaFishingRecipe> GetAllBigFish()
        {
            return BigFishrecipesById.Values;
        }

        /// <summary>
        /// Set Bait input
        /// </summary>
        /// <param name="subtype"></param>
        /// <returns></returns>
        private static MyDefinitionId SetBait(string subtype)
        {
            return MyDefinitionId.Parse($"MyObjectBuilder_Component/{subtype}");
        }

        /// <summary>
        /// Set fish raw output
        /// </summary>
        /// <param name="subtype"></param>
        /// <returns></returns>
        private static MyDefinitionId SetFishRaw(string subtype)
        {
            return MyDefinitionId.Parse($"MyObjectBuilder_ConsumableItem/{subtype}");
        }
    }

    /// <summary>
    /// Get Modpath anywhere
    /// </summary>
    public static class AquaModpathUtils
    {
        /// <summary>
        /// Get my mod path to models
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static string GetModPaths(string model)
        {
            if (string.IsNullOrEmpty(model))
                return null;
            string modpath = "";
            modpath = AquaExpansionSession.Insance.ModContext.ModPath;
            return Path.Combine(modpath, "Models\\", model);
        }

        /// <summary>
        /// Get detailed my mod path
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static string GetDetailedModelPath(string model)
        {
            if (string.IsNullOrEmpty(model))
                return null;
            string detailpath = "";
            detailpath = $"Cubes\\large\\{model}";
            return detailpath;
        }

        /// <summary>
        /// Get detailed my mod item path
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static string GetDetailedItemModelPath(string model)
        {
            if (string.IsNullOrEmpty(model))
                return null;
            string detailpath = "";
            detailpath = $"{model}";
            return detailpath;
        }

        /// <summary>
        /// Get watermod path to models
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public static string GetWaterModPath(string model)
        {
            if (string.IsNullOrEmpty(model))
                return null;
            string watermodpath = "";
            watermodpath = AquaExpansionSession.Insance.WatermodLink;
            return Path.Combine(watermodpath, "Models\\", model);
        }

        /// <summary>
        /// Get direct fish model path from recipe
        /// </summary>
        /// <param name="subtype"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetDirectModelPath(string subtype, AquaFarmingBlockType type)
        {
            if (string.IsNullOrEmpty(subtype))
                return null;
            string directpath = "";
            directpath = AquaFishingRecipeDatabase.GetRecipebySubtype(subtype, type).FishResult.Model;
            return directpath;
        }

        /// <summary>
        /// Get subtype by OB
        /// </summary>
        /// <param name="subtype"></param>
        /// <param name="definition"></param>
        /// <returns></returns>
        public static MyDefinitionId GetSubtypebyObjectBuilder(string subtype, AquaDefinitionOB definition)
        {
            return MyDefinitionId.Parse($"MyObjectBuilder_{definition}/{subtype}");
        }
    }

    /// <summary>
    /// Farm Items Database
    /// </summary>
    public static class AquaFarmItemsDatabase
    {
        private static readonly Dictionary<int, string> SporeItemsbyID = new Dictionary<int, string>();
        private static readonly Dictionary<string, int> SporeItemsbyName = new Dictionary<string, int>();
        private static readonly Dictionary<int, string> CropItemsbyID = new Dictionary<int, string>();
        private static readonly Dictionary<string, int> CropItemsbyName = new Dictionary<string, int>();

        /// <summary>
        /// Init Farm Items Database
        /// </summary>
        public static void Init()
        {
            RegisterSpore(1, "AquaSeaweedSpores_Green");
            RegisterSpore(2, "AquaSeaweedSpores_Brown");
            RegisterSpore(3, "AquaSeaweedSpores_Red");
            RegisterCrop(1, "AquaSeaweedRaw_Green");
            RegisterCrop(2, "AquaSeaweedRaw_Brown");
            RegisterCrop(3, "AquaSeaweedRaw_Red");
        }

        /// <summary>
        /// Register Spore Item
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        private static void RegisterSpore(int id, string subtype)
        {
            SporeItemsbyID[id] = subtype;
            SporeItemsbyName[subtype] = id;
        }

        /// <summary>
        /// Register Crop Item
        /// </summary>
        /// <param name="id"></param>
        /// <param name="subtype"></param>
        private static void RegisterCrop(int id, string subtype)
        {
            CropItemsbyID[id] = subtype;
            CropItemsbyName[subtype] = id;
        }

        /// <summary>
        /// Get Spore subtype by ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static string GetSporebyID(int id)
        {
            string subtype;
            if (SporeItemsbyID.TryGetValue(id, out subtype))
                return subtype;
            AquaExpansionSession.Insance.Log(true, $"Spore NOT FOUND (id): {id}");
            return null;
        }

        /// <summary>
        /// Get Spore ID by Name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static int GetSporeIDbyName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return -1;
            int id;
            name = name.Trim();
            if (SporeItemsbyName.TryGetValue(name, out id))
                return id;
            foreach (var key in SporeItemsbyName.Keys)
            {
                if (string.Equals(key, name, System.StringComparison.OrdinalIgnoreCase))
                {
                    AquaExpansionSession.Insance.Log(true,$"Case mismatch: '{name}' should be '{key}'");
                    break;
                }
            }
            AquaExpansionSession.Insance.Log(true, $"Spore NOT FOUND: '{name}'");
            return -1;
        }

        /// <summary>
        /// Get Crop subtype by ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static string GetCropbyID(int id)
        {
            string subtype;
            if (CropItemsbyID.TryGetValue(id, out subtype))
                return subtype;
            AquaExpansionSession.Insance.Log(true, $"Crop NOT FOUND (id): {id}");
            return null;
        }

        /// <summary>
        /// Get Crop ID by subtype
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static int GetCropIDbyName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return -1;
            int id;
            name = name.Trim();
            if (CropItemsbyName.TryGetValue(name, out id))
                return id;
            foreach (var key in SporeItemsbyName.Keys)
            {
                if (string.Equals(key, name, System.StringComparison.OrdinalIgnoreCase))
                {
                    AquaExpansionSession.Insance.Log(true, $"Case mismatch: '{name}' should be '{key}'");
                    break;
                }
            }
            AquaExpansionSession.Insance.Log(true, $"Crop NOT FOUND: '{name}'");
            return -1;
        }

        /// <summary>
        /// Get All Spores
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> GetAllSpores()
        {
            return SporeItemsbyID.Values;
        }

        /// <summary>
        /// Get All Crops
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> GetAllCrops()
        {
            return CropItemsbyID.Values;
        }

        public static bool IsSpore(string subtype) => SporeItemsbyName.ContainsKey(subtype);

        public static bool IsCrop(string subtype) => CropItemsbyName.ContainsKey(subtype);

        /// <summary>
        /// Validate Farm Items database
        /// </summary>
        /// <exception cref="Exception"></exception>
        public static void Validate()
        {
            // --- SPORES ---
            var usedSporeNames = new HashSet<string>();
            foreach (var pair in SporeItemsbyID)
            {
                int id = pair.Key;
                int backId;
                string subtype = pair.Value;
                if (string.IsNullOrWhiteSpace(subtype))
                    throw new Exception($"Spore ID {id} has null/empty subtype");
                if (!usedSporeNames.Add(subtype))
                    throw new Exception($"Spore Duplicate subtype: {subtype}");
                if (!SporeItemsbyName.TryGetValue(subtype, out backId))
                    throw new Exception($"Spore Missing reverse mapping for '{subtype}'");
                if (backId != id)
                    throw new Exception($"Spore Mismatch: '{subtype}' → {backId}, expected {id}");
            }
            // --- CROPS ---
            var usedCropNames = new HashSet<string>();
            foreach (var pair in CropItemsbyID)
            {
                int id = pair.Key;
                int backId;
                string subtype = pair.Value;
                if (string.IsNullOrWhiteSpace(subtype))
                    throw new Exception($"Crop ID {id} has null/empty subtype");
                if (!usedCropNames.Add(subtype))
                    throw new Exception($"Crop Duplicate subtype: {subtype}");
                if (!CropItemsbyName.TryGetValue(subtype, out backId))
                    throw new Exception($"Crop Missing reverse mapping for '{subtype}'");
                if (backId != id)
                    throw new Exception($"Crop Mismatch: '{subtype}' → {backId}, expected {id}");
            }
        }
    }

    /// <summary>
    /// FishItems Database
    /// </summary>
    public static class AquaFishItemsDatabase
    {
        private static readonly Dictionary<int, string> BaitItemsbyID = new Dictionary<int, string>();
        private static readonly Dictionary<string, int> BaitItemsbyName = new Dictionary<string, int>();
        private static readonly Dictionary<int, string> FishItemsbyID = new Dictionary<int, string>();
        private static readonly Dictionary<string, int> FishItemsbyName = new Dictionary<string, int>();

        /// <summary>
        /// Init
        /// </summary>
        public static void Init()
        {
            RegisterBait(1, "AquaFishBait_HydroFeed");
            RegisterBait(2, "AquaFishBait_ProteinLure");
            RegisterBait(3, "AquaFishBait_XenoLure");
            RegisterFish(1, "AquaFishdRaw_barracuda");
            RegisterFish(2, "AquaFishdRaw_blueTang");
            RegisterFish(3, "AquaFishdRaw_bottlenoseDolphin");
            RegisterFish(4, "AquaFishdRaw_clownFish");
            RegisterFish(5, "AquaFishdRaw_giantGrouper");
            RegisterFish(6, "AquaFishdRaw_lionFish");
            RegisterFish(7, "AquaFishdRaw_nurseShark");
            RegisterFish(8, "AquaFishdRaw_reefFish0");
            RegisterFish(9, "AquaFishdRaw_reefFish3");
            RegisterFish(10, "AquaFishdRaw_reefFish4");
            RegisterFish(11, "AquaFishdRaw_reefFish5");
            RegisterFish(12, "AquaFishdRaw_reefFish7");
            RegisterFish(13, "AquaFishdRaw_reefFish8");
            RegisterFish(14, "AquaFishdRaw_reefFish12");
            RegisterFish(15, "AquaFishdRaw_reefFish14");
            RegisterFish(16, "AquaFishdRaw_reefFish16");
            RegisterFish(17, "AquaFishdRaw_reefFish17");
            RegisterFish(18, "AquaFishdRaw_reefFish20");
            RegisterFish(19, "AquaFishdRaw_seaTurtle");
            RegisterFish(20, "AquaFishdRaw_tinyYellowFish");
            RegisterFish(21, "AquaFishdRaw_yellowTang");
            RegisterFish(22, "AquaFishdRaw_whaleShark");
        }

        /// <summary>
        /// Register bait
        /// </summary>
        /// <param name="id"></param>
        /// <param name="subtype"></param>
        private static void RegisterBait(int id, string subtype)
        {
            BaitItemsbyID[id] = subtype;
            BaitItemsbyName[subtype] = id;
        }

        /// <summary>
        /// Register Fish
        /// </summary>
        /// <param name="id"></param>
        /// <param name="subtype"></param>
        private static void RegisterFish(int id, string subtype)
        {
            FishItemsbyID[id] = subtype;
            FishItemsbyName[subtype] = id;
        }

        /// <summary>
        /// Get bait by ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static string GetBaitbyID(int id)
        {
            string subtype;
            if (BaitItemsbyID.TryGetValue(id, out subtype))
                return subtype;
            AquaExpansionSession.Insance.Log(true, $"Bait NOT FOUND (id): {id}");
            return null;
        }

        /// <summary>
        /// Get bait by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static int GetBaitIDbyName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return -1;
            int id;
            name = name.Trim();
            if (BaitItemsbyName.TryGetValue(name, out id))
                return id;
            foreach (var key in BaitItemsbyName.Keys)
            {
                if (string.Equals(key, name, System.StringComparison.OrdinalIgnoreCase))
                {
                    AquaExpansionSession.Insance.Log(true, $"Case mismatch: '{name}' should be '{key}'");
                    break;
                }
            }
            AquaExpansionSession.Insance.Log(true, $"Bait NOT FOUND: '{name}'");
            return -1;
        }

        /// <summary>
        /// Get fish by ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static string GetFishbyID(int id)
        {
            string subtype;
            if (FishItemsbyID.TryGetValue(id, out subtype))
                return subtype;
            AquaExpansionSession.Insance.Log(true, $"Fish NOT FOUND (id): {id}");
            return null;
        }

        /// <summary>
        /// Get fish by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static int GetFishIDbyName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return -1;
            int id;
            name = name.Trim();
            if (FishItemsbyName.TryGetValue(name, out id))
                return id;
            foreach (var key in FishItemsbyName.Keys)
            {
                if (string.Equals(key, name, System.StringComparison.OrdinalIgnoreCase))
                {
                    AquaExpansionSession.Insance.Log(true, $"Case mismatch: '{name}' should be '{key}'");
                    break;
                }
            }
            AquaExpansionSession.Insance.Log(true, $"Fish NOT FOUND: '{name}'");
            return -1;
        }

        /// <summary>
        /// Get all baits
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> GetAllBaites()
        {
            return BaitItemsbyID.Values;
        }

        /// <summary>
        /// Get all fishes
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> GetAllFishes()
        {
            return FishItemsbyID.Values;
        }

        /// <summary>
        /// is this subtype are Bait?
        /// </summary>
        /// <param name="subtype"></param>
        /// <returns></returns>
        public static bool IsBait(string subtype) => BaitItemsbyName.ContainsKey(subtype);

        /// <summary>
        /// is this subtype are Fish?
        /// </summary>
        /// <param name="subtype"></param>
        /// <returns></returns>
        public static bool IsFish(string subtype) => FishItemsbyName.ContainsKey(subtype);

        /// <summary>
        /// Validate
        /// </summary>
        /// <exception cref="Exception"></exception>
        public static void Validate()
        {
            // --- BAITS ---
            var usedBaitNames = new HashSet<string>();
            foreach (var pair in BaitItemsbyID)
            {
                int id = pair.Key;
                int backId;
                string subtype = pair.Value;
                if (string.IsNullOrWhiteSpace(subtype))
                    throw new Exception($"Bait ID {id} has null/empty subtype");
                if (!usedBaitNames.Add(subtype))
                    throw new Exception($"Bait Duplicate subtype: {subtype}");
                if (!BaitItemsbyName.TryGetValue(subtype, out backId))
                    throw new Exception($"Bait Missing reverse mapping for '{subtype}'");
                if (backId != id)
                    throw new Exception($"Bait Mismatch: '{subtype}' → {backId}, expected {id}");
            }
            // --- FISHES ---
            var usedFishNames = new HashSet<string>();
            foreach (var pair in FishItemsbyID)
            {
                int id = pair.Key;
                int backId;
                string subtype = pair.Value;
                if (string.IsNullOrWhiteSpace(subtype))
                    throw new Exception($"Fish ID {id} has null/empty subtype");
                if (!usedFishNames.Add(subtype))
                    throw new Exception($"Fish Duplicate subtype: {subtype}");
                if (!FishItemsbyName.TryGetValue(subtype, out backId))
                    throw new Exception($"Fish Missing reverse mapping for '{subtype}'");
                if (backId != id)
                    throw new Exception($"Fish Mismatch: '{subtype}' → {backId}, expected {id}");
            }
        }
    }
}
