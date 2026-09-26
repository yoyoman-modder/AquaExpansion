using Jakaria.API;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace AquaExpansion.Core.Animals
{
    public enum SeaAnimalType
    {
        Shark,
        Fish,
        Mammal,
        Ray,
        Cephalopod,
        Jellyfish,
        Crustacean,
        Reptile
    }
    public enum SeaAnimalBehavior { Predator,Passive }
    public enum SeaAnimalFoodSystemStage
    { Idle,Processing,Finished,Full }
    /// <summary>
    /// Animal Utility class
    /// </summary>
    public static class AnimalUtils
    {
        /// <summary>
        /// Debug box
        /// </summary>
        /// <param name="position"></param>
        /// <param name="color"></param>
        /// <param name="size"></param>
        public static void DebugBox(Vector3D position, Color color, double size = 0.02)
        {
            Color c = color * 10f;
            MatrixD world = MatrixD.CreateTranslation(position);
            BoundingBoxD box = new BoundingBoxD(new Vector3D(-size), new Vector3D(size));
            MySimpleObjectDraw.DrawTransparentBox(
            ref world,
            ref box,
            ref c,
            MySimpleObjectRasterizer.Solid,
            1,
            0.02f,
            MyStringId.GetOrCompute("Square"),
            MyStringId.GetOrCompute("Square"),
            false,
            -1,
            MyBillboard.BlendTypeEnum.PostPP,
            1f);
        }
        /// <summary>
        /// Debug Sphere
        /// </summary>
        /// <param name="position"></param>
        /// <param name="color"></param>
        /// <param name="radius"></param>
        public static void DebugSphere(Vector3D position,Color color,float radius = 1f)
        {
            Color c = color * 10f;
            MatrixD world = MatrixD.CreateTranslation(position);
            MySimpleObjectDraw.DrawTransparentSphere(
                ref world,
                radius,
                ref c,
                MySimpleObjectRasterizer.Solid,
                30,
                MyStringId.GetOrCompute("Square"),
                MyStringId.GetOrCompute("Square"),
                0.01f
                );
        }
        /// <summary>
        /// Character Axis Debug
        /// </summary>
        /// <param name="character"></param>
        /// <param name="movement"></param>
        /// <param name="show"></param>
        public static void AxisDebug(IMyCharacter character, SeadCreatureMovementData movement, bool show)
        {
            if (!show)
                return;
            if (character == null || movement == null)
                return;
            Vector3D start = character.GetPosition();
            Vector3D end = start + movement.CurrentDirection * 2;
            var lineColor = Color.PaleGreen.ToVector4();
            //Vector4 lineColorVec = lineColor.ToVector4() * 2;
            MySimpleObjectDraw.DrawLine(
             start,
             end,
             MyStringId.GetOrCompute("Square"),
            ref lineColor,
            0.01f,
            MyBillboard.BlendTypeEnum.Standard);
            //axis debug
            var FC = Color.Blue.ToVector4();
            var RC = Color.Red.ToVector4();
            var UC = Color.Green.ToVector4();
            Vector3D forward = start + character.WorldMatrix.Forward * 10;
            Vector3D right = start + character.WorldMatrix.Right * 10;
            Vector3D up = start + character.WorldMatrix.Up * 10;
            MySimpleObjectDraw.DrawLine(
                start,
                forward,
                MyStringId.GetOrCompute("Square"),
               ref FC,
               0.01f,
               MyBillboard.BlendTypeEnum.Standard);
            MySimpleObjectDraw.DrawLine(
               start,
               right,
               MyStringId.GetOrCompute("Square"),
              ref RC,
              0.01f,
              MyBillboard.BlendTypeEnum.Standard);
            MySimpleObjectDraw.DrawLine(
               start,
               up,
               MyStringId.GetOrCompute("Square"),
              ref UC,
              0.01f,
              MyBillboard.BlendTypeEnum.Standard);
        }
        /// <summary>
        /// Debug line from start to end with specified color and thinkness
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="color"></param>
        /// <param name="thickness"></param>
        public static void DebugLine(Vector3D start, Vector3D end, Color color, float thickness = 0.15f)
        {
            Vector4 lineColor = color.ToVector4() * 10f;

            MySimpleObjectDraw.DrawLine(
                start,
                end,
                MyStringId.GetOrCompute("Square"),
                ref lineColor,
                thickness, MyBillboard.BlendTypeEnum.Standard);
        }
        /// <summary>
        /// Log Animal phydsics data to the log for debugging purposes
        /// </summary>
        /// <param name="subtype"></param>
        /// <param name="speed"></param>
        /// <param name="speederror"></param>
        /// <param name="acceleration"></param>
        /// <param name="boost"></param>
        /// <param name="angle"></param>
        /// <param name="yawinput"></param>
        /// <param name="swimforce"></param>
        public static void LogAnimalPhysics(string subtype, float speed,float speederror,float acceleration, float boost,float angle,float yawinput,float swimforce)
        {
            AquaExpansionSession.Insance.Log(true,
           string.Format(
            "Animal Data\n" +
            "Subtype       : {0}\n" +
            "Speed         : {1:0.00} m/s\n" +
            "Speed Error   : {2:0.00}\n" +
            "Acceleration  : {3:0.00} m/s²\n" +
            "Boost         : {4:0.00}\n" +
            "Angle         : {5:0.00}*\n" +
            "Yaw Input     : {6:0.00}\n" +
            "Swim Force    : {7:0.00}",
            subtype,
            speed,
            speederror,
            acceleration,
            boost,
            angle,
            yawinput,
            swimforce));
        }
        /// <summary>
        /// Get float from chat input
        /// </summary>
        /// <param name="args"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool TryGetFloat(string[] args, int index, out float value)
        {
            value = 0f;
            if (args.Length <= index)
                return false;
            return float.TryParse(args[index], out value);
        }
        /// <summary>
        /// Get string input from chat input
        /// </summary>
        /// <param name="args"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool TryGetString(string[] args, int index, out string value)
        {
            value = null;
            if (args == null || args.Length <= index)
                return false;
            value = args[index];
            return true;
        }
        /// <summary>
        /// Get int from chat input
        /// </summary>
        /// <param name="args"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static bool TryGetInt(string[] args, int index, out int value)
        {
            value = 0;
            if (args == null || args.Length <= index)
                return false;
            return int.TryParse(args[index], out value);
        }
        /// <summary>
        /// Show chat commands
        /// </summary>
        public static void ShowAnimalHelp()
        {
            MyAPIGateway.Utilities.ShowMissionScreen(
                "Animal Runtime Commands",
                "",
                "",
                @"GLOBAL
                /animal help
                /animal clear
                /animal log
                /animal list
                /animal vis
                /animal sensor

                ANIMAL DATA
                /animal <SubtypeId> show
                /animal <SubtypeId> save
                /animal <SubtypeId> reset

                MOVEMENT DATA
                /animal <SubtypeId> desiredspeed <value>
                /animal <SubtypeId> maxspeed <value>
                /animal <SubtypeId> desiredepth <value>
                /animal <SubtypeId> turnspeed <value>
                /animal <SubtypeId> forwardforce <value>
                /animal <SubtypeId> depthgain <value>
                /animal <SubtypeId> maxbuoyancyforce <value>
                /animal <SubtypeId> verticaldamping <value>
                /animal <SubtypeId> acceleration <value>
                
                EXAMPLES
                /animal DefaultAnimal save
                /animal DefaultAnimal reset
                /animal clear",
                null,
                "Close");
        }
        /// <summary>
        /// Show all registered animal data
        /// </summary>
        public static void ListAllAnimalData()
        {
            StringBuilder text = new StringBuilder();
            foreach (var profile in SeaAnimalDatabase.GetAllAnimals())
            {
                if (profile == null ||
                    string.IsNullOrWhiteSpace(profile.Subtypeid))
                    continue;
                text.AppendLine(profile.Subtypeid);
            }
            if (text.Length == 0)
                text.AppendLine("No registered animal data.");
            MyAPIGateway.Utilities.ShowMissionScreen(
                "Registered Animal data",
                "",
                "",
                text.ToString(),
                null,
                "Close");
        }
        /// <summary>
        /// Show  Animal Data by subtype
        /// </summary>
        /// <param name="p"></param>
        private static void ShowAnimalProfile(SeaCreatureDefinition p)
        {
            MyAPIGateway.Utilities.ShowMessage(
                AquaExpansionSession.Insance.AquaAPI,
                string.Format(@"{0}
                DesiredSpeed={1}
                MaxSpeed={2}
                DesiredDepth={3}
                TurnSpeed={4}
                ForwardForce={5}
                DepthGain={6}
                MaxBuoyancyForce={7}
                VerticalDamping={8}
                Acceleration={9}",
                p.Subtypeid,
                p.DesiredSpeed,
                p.MaxSpeed,
                p.DesiredDepth,
                p.TurnSpeed,
                p.ForwardForce,
                p.DepthGain,
                p.MaxBuoyancyForce,
                p.VerticalDamping,
                p.Acceleration));
        }
        /// <summary>
        /// Save  runtime profile
        /// </summary>
        /// <param name="p"></param>
        private static void SaveAnimalProfile(SeaCreatureDefinition p)
        {
            string text = string.Format(
            @"new SeaCreatureDefinition
            {{
                Subtypeid = ""{0}"",
                DesiredSpeed = {1}f,
                MaxSpeed = {2}f,
                DesiredDepth = {3}f,
                TurnSpeed = {4}f,
                ForwardForce = {5}f,
                DepthGain = {6}f,
                MaxBuoyancyForce = {7}f,
                VerticalDamping = {8}f,
                Acceleration = {9}f,
            }};",
            p.Subtypeid,
            p.DesiredSpeed,
            p.MaxSpeed,
            p.DesiredDepth,
            p.TurnSpeed,
            p.ForwardForce,
            p.DepthGain,
            p.MaxBuoyancyForce,
            p.VerticalDamping,
            p.Acceleration);
            MyAPIGateway.Utilities.ShowMissionScreen(
                "Animal data",
                "",
                "",
                text,
                null,
                "Close");
        }
        /// <summary>
        /// Run  runtime Animal command
        /// </summary>
        /// <param name="args"></param>
        public static void ExecuteAnimalCommand(string[] args)
        {
            string subtype = args[1];
            string command = args[2].ToLower();
            SeaCreatureDefinition profile;
            float value;
            //string strvalue;
            //int intvalue;
            switch (command)
            {
                case "show":
                    profile = SeaAnimalDatabase.Get(subtype);
                    if (profile == null)
                    {
                        MyAPIGateway.Utilities.ShowMessage(
                            AquaExpansionSession.Insance.AquaAPI,
                            AquaModdingNamesDatabase.GetModCommandByID(33) + subtype);
                        return;
                    }
                    ShowAnimalProfile(profile);
                    return;
                case "reset":
                    SeaAnimalDatabase.Runtimeanimals.Remove(subtype);
                    MyAPIGateway.Utilities.ShowMessage(
                        AquaExpansionSession.Insance.AquaAPI,
                        subtype + AquaModdingNamesDatabase.GetModCommandByID(4));
                    return;
                case "save":
                    profile = SeaAnimalDatabase.Get(subtype);
                    if (profile == null)
                    {
                        MyAPIGateway.Utilities.ShowMessage(
                            AquaExpansionSession.Insance.AquaAPI,
                            AquaModdingNamesDatabase.GetModCommandByID(33) + subtype);
                        return;
                    }
                    SaveAnimalProfile(profile);
                    return;
            }
            // Editing commands use runtime profile
            profile = SeaAnimalDatabase.GetRuntime(subtype);
            if (profile == null)
            {
                MyAPIGateway.Utilities.ShowMessage(
                    AquaExpansionSession.Insance.AquaAPI,
                    AquaModdingNamesDatabase.GetModCommandByID(33) + subtype);
                return;
            }
            switch (command)
            {
                case "desiredspeed":
                    if (TryGetFloat(args, 3, out value))
                        profile.DesiredSpeed = value;
                    break;
                case "maxspeed":
                    if (TryGetFloat(args, 3, out value))
                        profile.MaxSpeed = value;
                    break;
                case "desireddepth":
                    if (TryGetFloat(args, 3, out value))
                        profile.DesiredDepth = value;
                    break;
                case "turnspeed":
                    if (TryGetFloat(args, 3, out value))
                        profile.TurnSpeed = value;
                    break;
                case "forwardforce":
                    if (TryGetFloat(args, 3, out value))
                        profile.ForwardForce = value;
                    break;
                case "depthgain":
                    if (TryGetFloat(args, 3, out value))
                        profile.DepthGain = value;
                    break;
                case "maxbuoyancyforce":
                    if (TryGetFloat(args, 3, out value))
                        profile.MaxBuoyancyForce = value;
                    break;
                case "verticaldamping":
                    if (TryGetFloat(args, 3, out value))
                        profile.VerticalDamping = value;
                    break;
                case "acceleration":
                    if (TryGetFloat(args, 3, out value))
                        profile.Acceleration = value;
                    break;
                default:
                    ShowAnimalHelp();
                    return;
            }
            MyAPIGateway.Utilities.ShowMessage(
                AquaExpansionSession.Insance.AquaAPI,
                string.Format("{0}: {1} = updated.", subtype, command));
        }
        /// <summary>
        /// Get ClosestPoint on AABB
        /// </summary>
        /// <param name="box"></param>
        /// <param name="point"></param>
        /// <returns></returns>
        public static Vector3D GetClosestPointOnAABB(BoundingBoxD box,Vector3D point)
        {
            return new Vector3D(
                MathHelperD.Clamp(
                    point.X,
                    box.Min.X,
                    box.Max.X),
                MathHelperD.Clamp(
                    point.Y,
                    box.Min.Y,
                    box.Max.Y),
                MathHelperD.Clamp(
                    point.Z,
                    box.Min.Z,
                    box.Max.Z));
        }
        /// <summary>
        /// Check if player in cocpit/seat
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool isPlayerCovered(IMyPlayer player)
        {
            if (player == null ||
                player.Controller == null)
                return false;
            IMyEntity entity = player.Controller.ControlledEntity?.Entity;
            if (entity == null)
                return false;
            IMyCockpit cockpit = entity as IMyCockpit;
            if (cockpit != null)
                return true;
            IMyShipController seat = entity as IMyShipController;
            if (seat != null)
                return true;
            return false;
        }
        public static bool isPlayersGridPressurated(IMyCharacter character)
        {
            if (character == null || character.IsDead || character.Closed || character.MarkedForClose)
                return false;
            float ox;
            AquaExpansionSession.Insance.GetInAirtightGrid(character,out ox);
            if (ox >= AquaExpansionSession.Insance.MIN_ENVOXYGENLEVEL)
                return true;
            return false;
        }

    }
    /// <summary>
    /// Animal Movement Data
    /// </summary>
    public class SeadCreatureMovementData
    {
        public bool IsMoving = true;
        public Vector3D DesiredDirection;
        public Vector3D CurrentDirection;
        public float DesiredSpeed;
        public float DesiredDepth;
        public float TurnSpeed;
        public bool UseDepthControl = true;
        public float ForwardForce;
        public float DepthGain;
        public float MaxBuoyancyForce;
        public float VerticalDamping;
        public float Acceleration;
        public float CurrentYaw;
        public float MaxSpeed;
    }
    /// <summary>
    /// Animal Attack Data
    /// </summary>
    public class SeaCreatureAttackData
    {
        public float AttackRadius;
        public int AttackInterval;
        public float HealhDamage;
        public float AttackCenter;
        public float Agression;
    }
    /// <summary>
    /// Animal Definition
    /// </summary>
    public class SeaCreatureDefinition
    {
        public SeaAnimalType Type;
        public SeaAnimalBehavior Behavior;
        public string Subtypeid;
        public float DesiredSpeed;
        public float MaxSpeed;
        public float DesiredDepth;
        public float TurnSpeed;
        public float ForwardForce;
        public float DepthGain;
        public float MaxBuoyancyForce;
        public float VerticalDamping;
        public float Acceleration;
        public string Food;
        public float AttackRadius;
        public int AttackInterval;
        public float HealthDamage;
        public float AttackCenter;
        public float Agression;
        public SeaCreatureDefinition Clone()
        {
            return new SeaCreatureDefinition
            {
                Type = this.Type,
                Behavior = this.Behavior,
                Subtypeid = this.Subtypeid,
                DesiredSpeed = this.DesiredSpeed,
                MaxSpeed = this.MaxSpeed,
                DesiredDepth = this.DesiredDepth,
                TurnSpeed = this.TurnSpeed,
                ForwardForce = this.ForwardForce,
                DepthGain = this.DepthGain,
                MaxBuoyancyForce = this.MaxBuoyancyForce,
                VerticalDamping = this.VerticalDamping,
                Acceleration = this.Acceleration,
                Food = this.Food,
                AttackRadius = this.AttackRadius,
                AttackInterval = this.AttackInterval,
                HealthDamage = this.HealthDamage,
                AttackCenter = this.AttackCenter,
                Agression = this.Agression
            };
        }
    }
    /// <summary>
    /// Animal database
    /// </summary>
    public static class SeaAnimalDatabase
    {
        private static readonly Dictionary<string, SeaCreatureDefinition> animals = new Dictionary<string, SeaCreatureDefinition>();
        public static readonly Dictionary<string, SeaCreatureDefinition> Runtimeanimals = new Dictionary<string, SeaCreatureDefinition>();
        /// <summary>
        /// Init
        /// </summary>
        public static void Init()
        {
            RegisterAnimal(new SeaCreatureDefinition
            {
                Type = SeaAnimalType.Shark,
                Behavior = SeaAnimalBehavior.Predator,
                Subtypeid = "AquaWhiteShark",
                DesiredSpeed = 4f,
                MaxSpeed = 6f,
                DesiredDepth = 10f,
                TurnSpeed = 2f,
                ForwardForce = 50f,
                DepthGain = 50f,
                MaxBuoyancyForce = 500f,
                VerticalDamping = 45f,
                Acceleration = 10f,
                Food = SeaAnimalFoodItemsDatabase.GetFoodByID(1),
                AttackRadius = 0.5f,
                AttackInterval = 90,
                HealthDamage = 20f,
                AttackCenter = 1.5f,
                Agression = 50
            });
        }
        /// <summary>
        /// Register AnimalData
        /// </summary>
        /// <param name="animal"></param>
        private static void RegisterAnimal(SeaCreatureDefinition animal)
        {
            animals[animal.Subtypeid] = animal;
        }
        /// <summary>
        /// Get AnimalData by subtype
        /// </summary>
        /// <param name="subtype"></param>
        /// <returns></returns>
        public static SeaCreatureDefinition Get(string subtype)
        {
            SeaCreatureDefinition profile;
            if (Runtimeanimals.TryGetValue(subtype, out profile))
                return profile;
            if (animals.TryGetValue(subtype, out profile))
                return profile;
            return null;
        }
        /// <summary>
        /// Get origial AnimalData by subtype
        /// </summary>
        /// <param name="subtype"></param>
        /// <returns></returns>
        public static SeaCreatureDefinition GetOriginal(string subtype)
        {
            if (string.IsNullOrWhiteSpace(subtype))
                return null;
            subtype = subtype.Trim();
            SeaCreatureDefinition profile;
            if (animals.TryGetValue(subtype, out profile))
            {
                return profile;
            }
            return null;
        }
        /// <summary>
        /// Get Runtime AnimalData by Subtype
        /// </summary>
        /// <param name="subtype"></param>
        /// <returns></returns>
        public static SeaCreatureDefinition GetRuntime(string subtype)
        {
            if (string.IsNullOrWhiteSpace(subtype))
                return null;
            subtype = subtype.Trim();
            // Already has a runtime override.
            SeaCreatureDefinition profile;
            if (Runtimeanimals.TryGetValue(subtype, out profile))
            {
                return profile;
            }
            // Find registered/original profile.
            SeaCreatureDefinition original = GetOriginal(subtype);
            if (original == null)
                return null;
            // Create an independent runtime copy.
            profile = original.Clone();
            Runtimeanimals[subtype] = profile;
            return profile;
        }
        /// <summary>
        /// Default Animal Data
        /// </summary>
        /// <returns></returns>
        public static SeaCreatureDefinition DefaultAnimal()
        {
            return new SeaCreatureDefinition
            {
                Subtypeid = "Default",
                DesiredSpeed = 2f,
                MaxSpeed = 4f,
                DesiredDepth = 10f,
                TurnSpeed = 2f,
                ForwardForce = 40f,
                DepthGain = 50f,
                MaxBuoyancyForce = 500f,
                VerticalDamping = 20f,
                Acceleration = 10f
            };
        }
        /// <summary>
        /// Get All Animal Data
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<SeaCreatureDefinition> GetAllAnimals()
        {
            return animals.Values;
        }
        /// <summary>
        /// Clear runtime AnimalData changes
        /// </summary>
        public static void ClearRuntime()
        {
            Runtimeanimals.Clear();
        }
    }
    /// <summary>
    /// Sea Animal Food Item DataBase
    /// </summary>
    public static class SeaAnimalFoodItemsDatabase
    {
        private static readonly Dictionary<int, string> AnimalFoodItemsbyID = new Dictionary<int, string>();
        private static readonly Dictionary<string, int> AnimalFoodItemsbyName = new Dictionary<string, int>();
        private static readonly Dictionary<int, string> SeaAnimalWasteEffectsbyID = new Dictionary<int, string>();
        /// <summary>
        /// Init
        /// </summary>
        public static void Init()
        {
            //Food
            RegisterFood(1, "AquaAnimalMeatRaw");
            //Waste

        }
        /// <summary>
        /// Register Food
        /// </summary>
        /// <param name="id"></param>
        /// <param name="subtype"></param>
        private static void RegisterFood(int id, string subtype)
        {
            AnimalFoodItemsbyID[id] = subtype;
            AnimalFoodItemsbyName[subtype] = id;
        }
        /// <summary>
        /// Register Waste
        /// </summary>
        /// <param name="id"></param>
        /// <param name="subtype"></param>
        private static void RegisterWaste(int id, string subtype)
        {
            SeaAnimalWasteEffectsbyID[id] = subtype;
        }
        /// <summary>
        /// Get animal food by ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static string GetFoodByID(int id)
        {
            string subtype;
            if (AnimalFoodItemsbyID.TryGetValue(id, out subtype))
                return subtype;
            AquaExpansionSession.Insance.Log(true, $"Animal Food NOT FOUND (id): {id}");
            return null;
        }
        /// <summary>
        /// Get animal waste by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static string GetWasteByID(int id)
        {
            string subtype;
            if (SeaAnimalWasteEffectsbyID.TryGetValue(id, out subtype))
                return subtype;
            AquaExpansionSession.Insance.Log(true, $"Animal Waste NOT FOUND (id): {id}");
            return null;
        }
        /// <summary>
        /// Get animal food by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static int GetFoodIDbyName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return -1;
            int id;
            name = name.Trim();
            if (AnimalFoodItemsbyName.TryGetValue(name, out id))
                return id;
            foreach (var key in AnimalFoodItemsbyName.Keys)
            {
                if (string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                {
                    AquaExpansionSession.Insance.Log(true, $"Case mismatch: '{name}' should be '{key}'");
                    break;
                }
            }
            AquaExpansionSession.Insance.Log(true, $"Animal Food NOT FOUND: '{name}'");
            return -1;
        }
        /// <summary>
        /// Get all animal food items
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> GetAllAnimalFoodItems()
        {
            return AnimalFoodItemsbyID.Values;
        }
        /// <summary>
        /// Get All animal wastes
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> GetAllAnimalWaste()
        {
            return SeaAnimalWasteEffectsbyID.Values;
        }
        /// <summary>
        /// is this subtype are Animal Food?
        /// </summary>
        /// <param name="subtype"></param>
        /// <returns></returns>
        public static bool IsAnimalFood(string subtype) => AnimalFoodItemsbyName.ContainsKey(subtype);
        /// <summary>
        /// Fill Animal Food Items into a HashSet for quick lookup
        /// </summary>
        /// <returns></returns>
        public static HashSet<string> FillAnimalFoodItems()
        {
            return new HashSet<string>(AnimalFoodItemsbyID.Values);
        }
        /// <summary>
        /// Fill Animal Waste effect subtypes into a HashSet for quick lookup
        /// </summary>
        /// <returns></returns>
        public static HashSet<string> FillAnimalWasteData()
        {
            return new HashSet<string>(SeaAnimalWasteEffectsbyID.Values);
        }
    }
    /// <summary>
    /// Sea Animal Component Database
    /// </summary>
    public static class SeaAnimalComponentDatabase
    {
        private static readonly Dictionary<string, Func<MyGameLogicComponent>> SeaAnimalsLogicData = new Dictionary<string, Func<MyGameLogicComponent>>();
        /// <summary>
        /// Init
        /// </summary>
        public static void Init()
        {
            RegisterAnimalComponent("AquaWhiteShark", () => new SeaCreatureWhiteShark());
        }
        /// <summary>
        /// Register AnimalComponent by subtype and factory function to create the component
        /// </summary>
        /// <param name="subtype"></param>
        /// <param name="factory"></param>
        private static void RegisterAnimalComponent(string subtype, Func<MyGameLogicComponent> factory)
        {
            if (string.IsNullOrEmpty(subtype))
                return;
            if (factory == null)
                return;
            SeaAnimalsLogicData[subtype] = factory;
        }
        /// <summary>
        /// Get AnimalComponent factory function by subtype
        /// </summary>
        /// <param name="subtype"></param>
        /// <returns></returns>
        public static Func<MyGameLogicComponent> Get(string subtype)
        {
            if (string.IsNullOrEmpty(subtype))
                return null;
            Func<MyGameLogicComponent> factory;
            if (SeaAnimalsLogicData.TryGetValue(subtype, out factory))
                return factory;
            AquaExpansionSession.Insance.Log(true, $"Animal Component NOT FOUND: '{subtype}'");
            return null;
        }
        /// <summary>
        /// Get all animal components subtype keys
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> GetAllAnimalComponents()
        {
            return SeaAnimalsLogicData.Keys;
        }
        /// <summary>
        /// Get all animal component factory functions
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Func<MyGameLogicComponent>> GetAllAnimalComponentsFactories()
        {
            return SeaAnimalsLogicData.Values;
        }
        /// <summary>
        /// Fill Anmal Components into a Dictionary for quick lookup by subtype
        /// </summary>
        /// <returns></returns>
        public static Dictionary<string,Func<MyGameLogicComponent>> FillAnimalComponents()
        {
            return new Dictionary<string, Func<MyGameLogicComponent>>(SeaAnimalsLogicData);
        }
    }
    /// <summary>
    /// Animal Navigator for moving toward a target position
    /// </summary>
    public class SeaCreatureNavigator
    {
        public Vector3D TargetPosition;
        public bool HasTarget;
        public double ArriveDistance = 2.0;
        /// <summary>
        /// Update
        /// </summary>
        /// <param name="character"></param>
        /// <param name="movement"></param>
        /// <returns></returns>
        public bool Update(IMyCharacter character, SeadCreatureMovementData movement)
        {
            if (!HasTarget)
                return false;
            Vector3D toTarget = TargetPosition - character.GetPosition();
            double distance = toTarget.Length();
            if (distance <= ArriveDistance)
            {
                movement.IsMoving = false;
                HasTarget = false;
                return true;
            }
            toTarget.Normalize();
            movement.IsMoving = true;
            movement.DesiredDirection = toTarget;
            return false;
        }
    }
    /// <summary>
    /// Animal Movement Controller
    /// </summary>
    public static class SeaCreatureMovement
    {
        /// <summary>
        /// Update Movement
        /// </summary>
        /// <param name="character"></param>
        /// <param name="movement"></param>
        ///  <param name="sensor"></param>
        /// <param name="run"></param>
        public static void Update(IMyCharacter character,SeadCreatureMovementData movement,AnimalSensor sensor,bool run,int dt)
        {
            if (!run)
                return;
            if (character == null ||
                character.Physics == null)
                return;
            if (movement == null ||
                !movement.IsMoving)
                return;
            Vector3D desiredDirection = movement.DesiredDirection;
            bool voxelRecovery = UpdateObstacleAvoidance(character,movement,sensor,ref desiredDirection);
            if (voxelRecovery)
            {
                movement.UseDepthControl = true;
            }
            else
            {
                movement.UseDepthControl = true;
            }
            movement.DesiredDirection = desiredDirection;
            UpdateSteering(character, movement, desiredDirection);
            UpdatePropulsion(character,movement,dt);
            UpdateDepth(character,movement);
        }
        /// <summary>
        /// Update Obstacle Avoidance
        /// </summary>
        /// <param name="character"></param>
        /// <param name="movement"></param>
        /// <param name="sensor"></param>
        /// <param name="desiredDirection"></param>
        /// <returns></returns>
        private static bool UpdateObstacleAvoidance(IMyCharacter character,SeadCreatureMovementData movement,AnimalSensor sensor,ref Vector3D desiredDirection)
        {
            if (character == null ||
                character.Physics == null ||
                movement == null ||
                sensor == null)
                return false;
            Vector3D forward = character.WorldMatrix.Forward;
            Vector3D right = character.WorldMatrix.Right;
            forward.Y = 0.0;
            right.Y = 0.0;
            if (forward.LengthSquared() > 0.001)
                forward.Normalize();
            if (right.LengthSquared() > 0.001)
                right.Normalize();
            Vector3D avoidance = Vector3D.Zero;
            bool frontBlocked = sensor.FrontObstacleDetected;
            bool leftBlocked = sensor.LeftObstacleDetected;
            bool rightBlocked = sensor.RightObstacleDetected;
            bool upBlocked = sensor.UpObstacleDetected;
            bool downBlocked = sensor.DownObstacleDetected;
            bool voxelRecovery = UpdateVoxelRecovery(character,sensor);
            if (frontBlocked)
            {
                float strength = GetObstacleStrength(sensor.FrontObstacleDistance);
                if (leftBlocked && rightBlocked)
                {
                    if (!upBlocked && downBlocked)
                    {
                        movement.DesiredDepth += strength;
                    }
                    else if (upBlocked && !downBlocked)
                    {
                        movement.DesiredDepth -= strength;
                    }
                    else if (!upBlocked && !downBlocked)
                    {
                        if (sensor.UpObstacleDistance >
                            sensor.DownObstacleDistance)
                        {
                            movement.DesiredDepth += strength;
                        }
                        else
                        {
                            movement.DesiredDepth -= strength;
                        }
                    }
                    else
                    {
                        avoidance -= forward * strength;
                    }
                }
                else if (leftBlocked && !rightBlocked)
                {
                    avoidance += right * strength;
                }
                else if (rightBlocked && !leftBlocked)
                {
                    avoidance -= right * strength;
                }
                else
                {
                    if (sensor.LeftObstacleDistance >
                        sensor.RightObstacleDistance)
                    {
                        avoidance -= right * strength;
                    }
                    else
                    {
                        avoidance += right * strength;
                    }
                }
            }
            if (leftBlocked)
            {
                float strength = GetSideObstacleStrength(sensor.LeftObstacleDistance);
                avoidance += right * strength;
            }
            if (rightBlocked)
            {
                float strength = GetSideObstacleStrength(sensor.RightObstacleDistance);
                avoidance -= right * strength;
            }
            if (sensor.UpGridObstacleDetected)
            {
                float strength = GetVerticalObstacleStrength(sensor.UpGridObstacleDistance);
                movement.DesiredDepth -= strength;
            }
            else if (upBlocked)
            {
                float strength = GetVerticalObstacleStrength(sensor.UpObstacleDistance);
                movement.DesiredDepth -= strength;
            }
            if (sensor.DownGridObstacleDetected)
            {
                float strength = GetVerticalObstacleStrength(sensor.DownGridObstacleDistance);
                movement.DesiredDepth += strength;
            }
            else if (downBlocked)
            {
                float strength = GetVerticalObstacleStrength(sensor.DownObstacleDistance);

                movement.DesiredDepth += strength;
            }
            if (sensor.IsGroundDetected &&
                !voxelRecovery &&
                sensor.GroundDistance < 4.0f)
            {
                float groundOffset = 4.0f - sensor.GroundDistance;

                if (groundOffset > 3.0f)
                    groundOffset = 3.0f;
                movement.DesiredDepth += groundOffset;
            }
            if (movement.DesiredDepth < 0.5f)
                movement.DesiredDepth = 0.5f;
            if (avoidance.LengthSquared() > 0.001)
            {
                desiredDirection += avoidance;
            }
            if (desiredDirection.LengthSquared() > 0.001)
                desiredDirection.Normalize();
            movement.UseDepthControl = !voxelRecovery;
            return voxelRecovery;
        }
        /// <summary>
        /// Update Animal Stereeng
        /// </summary>
        /// <param name="character"></param>
        /// <param name="movement"></param>
        /// <param name="desiredDirection"></param>
        private static void UpdateSteering(IMyCharacter character,SeadCreatureMovementData movement,Vector3D desiredDirection)
        {
            if (character == null ||
                character.Physics == null ||
                movement == null)
                return;
            desiredDirection.Y = 0.0;
            if (desiredDirection.LengthSquared() <= 0.001)
                return;
            desiredDirection.Normalize();
            Vector3D forward = character.WorldMatrix.Forward;
            forward.Y = 0.0;
            if (forward.LengthSquared() <= 0.001)
                return;
            forward.Normalize();
            double dot = Vector3D.Dot(forward,desiredDirection);
            dot = MathHelper.Clamp((float)dot,-1.0f,1.0f);
            double angle = Math.Acos(dot);
            if (angle <= 0.001)
                return;
            Vector3D cross = Vector3D.Cross(forward,desiredDirection);
            if (cross.Y < 0.0)
                angle = -angle;
            float maxTurn = movement.TurnSpeed * (float)MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
            float turn = MathHelper.Clamp((float)angle,-maxTurn,maxTurn);
            if (Math.Abs(turn) <= 0.0001f)
                return;
            MatrixD rotation = MatrixD.CreateFromAxisAngle(Vector3D.Up,turn);
            Vector3D newForward = Vector3D.TransformNormal(forward,rotation);
            newForward.Normalize();
            MatrixD world = character.WorldMatrix;
            world.Forward = newForward;
            character.WorldMatrix = world;
        }
        /// <summary>
        /// Update Propulsion
        /// </summary>
        /// <param name="character"></param>
        /// <param name="movement"></param>
        private static void UpdatePropulsion(IMyCharacter character,SeadCreatureMovementData movement,float dt)
        {
            if (character == null ||
                character.Physics == null ||
                movement == null ||
                dt <= 0f)
                return;
            Vector3 forward = character.WorldMatrix.Forward;
            forward.Normalize();
            Vector3 velocity = character.Physics.LinearVelocity;
            float forwardSpeed = Vector3.Dot(velocity, forward);
            float targetSpeed = MathHelper.Clamp(movement.DesiredSpeed,0f,movement.MaxSpeed);
            float speedChange = movement.Acceleration * dt;
            if (forwardSpeed < targetSpeed)
            {
                forwardSpeed = Math.Min(forwardSpeed + speedChange,targetSpeed);
            }
            else if (forwardSpeed > targetSpeed)
            {
                forwardSpeed = Math.Max(forwardSpeed - speedChange,targetSpeed);
            }
            Vector3 verticalVelocity = Vector3.Up * Vector3.Dot(velocity,Vector3.Up);
            Vector3 newVelocity = forward * forwardSpeed + verticalVelocity;
            character.Physics.LinearVelocity = newVelocity;
        }
        /// <summary>
        /// Update Depth
        /// </summary>
        /// <param name="character"></param>
        /// <param name="movement"></param>
        private static void UpdateDepth(IMyCharacter character,SeadCreatureMovementData movement)
        {
            if (character == null ||
                character.Physics == null ||
                movement == null ||
                !movement.UseDepthControl)
                return;
            float depth = Math.Abs(AquaExpansionSession.Insance.GetWaterDepthbyCharacter(character));
            float error = movement.DesiredDepth - depth;
            if (Math.Abs(error) < 0.5f)
                error = 0.0f;
            float buoyancyForce = -error * movement.DepthGain;
            buoyancyForce = MathHelper.Clamp(buoyancyForce,-movement.MaxBuoyancyForce,movement.MaxBuoyancyForce);
            Vector3 worldUp = character.WorldMatrix.Up;
            worldUp.Normalize();
            float verticalSpeed = Vector3.Dot(character.Physics.LinearVelocity,worldUp);
            buoyancyForce -= verticalSpeed * movement.VerticalDamping;
            buoyancyForce = MathHelper.Clamp(buoyancyForce,-movement.MaxBuoyancyForce,movement.MaxBuoyancyForce);
            Vector3 impulse = worldUp * buoyancyForce;
            character.Physics.AddForce(
                MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE,
                impulse,
                null,
                null);
        }
        /// <summary>
        /// Get Obstacle Strength
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        private static float GetObstacleStrength(float distance)
        {
            if (float.IsNaN(distance) || float.IsInfinity(distance) || distance < 0f)
                return 0f;
            if (distance <= 1.5f)
                return 3.0f;
            if (distance <= 3.0f)
                return 2.0f;
            return 1.0f;
        }
        /// <summary>
        /// Get Side Obstacle Strength
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        private static float GetSideObstacleStrength(float distance)
        {
            if (float.IsNaN(distance) || float.IsInfinity(distance) || distance < 0f)
                return 0f;
            if (distance <= 1.5f)
                return 1.5f;
            if (distance <= 3.0f)
                return 1.0f;
            return 0.5f;
        }
        /// <summary>
        /// Get Vertical Obstacle Strength
        /// </summary>
        /// <param name="distance"></param>
        /// <returns></returns>
        private static float GetVerticalObstacleStrength(float distance)
        {
            if (float.IsNaN(distance) || float.IsInfinity(distance) || distance < 0f)
                return 0f;
            if (distance <= 1.5f)
                return 2.0f;
            if (distance <= 3.0f)
                return 1.0f;
            return 0.5f;
        }
        /// <summary>
        /// Update voxel recovery
        /// </summary>
        /// <param name="character"></param>
        /// <param name="sensor"></param>
        /// <returns></returns>
        private static bool UpdateVoxelRecovery(IMyCharacter character,AnimalSensor sensor)
        {
            if (character == null ||
                character.Physics == null ||
                sensor == null)
                return false;

            if (!sensor.IsGroundDetected ||
                sensor.GroundDistance > 2f ||
                sensor.GroundDistance < 0f)
                return false;

            IMyVoxelBase voxel =
                sensor.GroundHitEntity as IMyVoxelBase;

            if (voxel == null)
                return false;

            Vector3D up =
                character.WorldMatrix.Up;

            if (up.LengthSquared() < 0.001)
                return true;

            up.Normalize();

            Vector3 velocity =
                character.Physics.LinearVelocity;

            float upwardSpeed =
                (float)Vector3D.Dot(
                    velocity,
                    up);

            const float maximumRecoverySpeed = 4.0f;
            const float maximumSpeedChange = 0.35f;

            if (upwardSpeed >= maximumRecoverySpeed)
                return true;

            float speedChange =
                maximumRecoverySpeed -
                upwardSpeed;

            if (speedChange >
                maximumSpeedChange)
            {
                speedChange =
                    maximumSpeedChange;
            }

            Vector3D impulse =
                up *
                character.Physics.Mass *
                speedChange;

            character.Physics.AddForce(
                MyPhysicsForceType.APPLY_WORLD_IMPULSE_AND_WORLD_ANGULAR_IMPULSE,
                impulse,
                null,
                null);

            return true;
        }
    }
    /// <summary>
    /// Animal Sensor Array Component
    /// </summary>
    public class AnimalSensor
    {
        public float SenseDistance = 5f;
        public float AngleOffset = 0.35f;
        public bool Render = false;
        private enum RaySide { Front, Back, Left, Right, Up, Down }
        // -------------------------------------------------
        // General obstacle
        // -------------------------------------------------
        public bool IsObstacleAhead { get; private set; }
        public float ObstacleDistance { get; private set; }
        public Vector3D HitPosition { get; private set; }
        public IMyEntity HitEntity { get; private set; }
        // -------------------------------------------------
        // Horizontal obstacles
        // -------------------------------------------------
        public bool FrontObstacleDetected { get; private set; }
        public bool LeftObstacleDetected { get; private set; }
        public bool RightObstacleDetected { get; private set; }
        public float FrontObstacleDistance { get; private set; }
        public float LeftObstacleDistance { get; private set; }
        public float RightObstacleDistance { get; private set; }
        public IMyEntity FrontObstacle { get; private set; }
        public IMyEntity LeftObstacle { get; private set; }
        public IMyEntity RightObstacle { get; private set; }
        // -------------------------------------------------
        // Vertical obstacles
        // -------------------------------------------------
        public bool UpObstacleDetected { get; private set; }
        public bool DownObstacleDetected { get; private set; }
        public float UpObstacleDistance { get; private set; }
        public float DownObstacleDistance { get; private set; }
        public IMyEntity UpObstacle { get; private set; }
        public IMyEntity DownObstacle { get; private set; }
        // -------------------------------------------------
        // Ground / voxel
        // -------------------------------------------------
        public bool IsGroundDetected { get; private set; }
        public float GroundDistance { get; private set; }
        public Vector3D GroundHitPosition { get; private set; }
        public IMyEntity GroundHitEntity { get; private set; }
        // -------------------------------------------------
        // Grid
        // -------------------------------------------------
        public bool IsGridDetected { get; private set; }
        public float GridDistance { get; private set; }
        public Vector3D GridHitPosition { get; private set; }
        public IMyCubeGrid GridHitEntity { get; private set; }
        // -------------------------------------------------
        // Directional grid obstacles
        // -------------------------------------------------
        public bool UpGridObstacleDetected { get; private set; }
        public bool DownGridObstacleDetected { get; private set; }
        public float UpGridObstacleDistance { get; private set; }
        public float DownGridObstacleDistance { get; private set; }
        public Vector3D UpGridHitPosition { get; private set; }
        public Vector3D DownGridHitPosition { get; private set; }
        public IMyCubeGrid UpGridObstacle { get; private set; }
        public IMyCubeGrid DownGridObstacle { get; private set; }
        // -------------------------------------------------
        // Update
        // -------------------------------------------------
        public void Update(IMyCharacter character)
        {
            if (character == null)
                return;
            Reset();
            Vector3D start = character.WorldMatrix.Translation;
            Vector3D forward = character.WorldMatrix.Forward;
            Vector3D right = character.WorldMatrix.Right;
            Vector3D up = character.WorldMatrix.Up;
            forward.Normalize();
            right.Normalize();
            up.Normalize();
            // -------------------------------------------------
            // FRONT
            // -------------------------------------------------
            CheckRay(
                start,
                forward + up * AngleOffset,
                RaySide.Front,
                false);
            CheckRay(
                start,
                forward,
                RaySide.Front,
                false);
            CheckRay(
                start,
                forward - up * AngleOffset,
                RaySide.Front,
                false);
            // -------------------------------------------------
            // BACK
            // -------------------------------------------------
            CheckRay(
                start,
                -forward + up * AngleOffset,
                RaySide.Back,
                false);
            CheckRay(
                start,
                -forward,
                RaySide.Back,
                false);
            CheckRay(
                start,
                -forward - up * AngleOffset,
                RaySide.Back,
                false);
            // -------------------------------------------------
            // LEFT
            // -------------------------------------------------
            CheckRay(
                start,
                -right + up * AngleOffset,
                RaySide.Left,
                false);
            CheckRay(
                start,
                -right,
                RaySide.Left,
                false);
            CheckRay(
                start,
                -right - up * AngleOffset,
                RaySide.Left,
                false);
            // -------------------------------------------------
            // RIGHT
            // -------------------------------------------------
            CheckRay(
                start,
                right + up * AngleOffset,
                RaySide.Right,
                false);
            CheckRay(
                start,
                right,
                RaySide.Right,
                false);
            CheckRay(
                start,
                right - up * AngleOffset,
                RaySide.Right,
                false);
            // -------------------------------------------------
            // UP
            // -------------------------------------------------
            CheckRay(
                start,
                up + forward * AngleOffset,
                RaySide.Up,
                false);
            CheckRay(
                start,
                up,
                RaySide.Up,
                false);
            CheckRay(
                start,
                up - forward * AngleOffset,
                RaySide.Up,
                false);
            // -------------------------------------------------
            // DOWN
            // -------------------------------------------------
            CheckRay(
                start,
                -up + forward * AngleOffset,
                RaySide.Down,
                true);
            CheckRay(
                start,
                -up,
                RaySide.Down,
                true);
            CheckRay(
                start,
                -up - forward * AngleOffset,
                RaySide.Down,
                true);
        }
        // -------------------------------------------------
        // Reset
        // -------------------------------------------------
        private void Reset()
        {
            IsObstacleAhead = false;
            ObstacleDistance = float.MaxValue;
            HitPosition = Vector3D.Zero;
            HitEntity = null;
            FrontObstacleDetected = false;
            LeftObstacleDetected = false;
            RightObstacleDetected = false;
            FrontObstacleDistance = float.MaxValue;
            LeftObstacleDistance = float.MaxValue;
            RightObstacleDistance = float.MaxValue;
            FrontObstacle = null;
            LeftObstacle = null;
            RightObstacle = null;
            UpObstacleDetected = false;
            DownObstacleDetected = false;
            UpObstacleDistance = float.MaxValue;
            DownObstacleDistance = float.MaxValue;
            UpObstacle = null;
            DownObstacle = null;
            IsGroundDetected = false;
            GroundDistance = float.MaxValue;
            GroundHitPosition = Vector3D.Zero;
            GroundHitEntity = null;
            IsGridDetected = false;
            GridDistance = float.MaxValue;
            GridHitPosition = Vector3D.Zero;
            GridHitEntity = null;
            UpGridObstacleDetected = false;
            DownGridObstacleDetected = false;
            UpGridObstacleDistance = float.MaxValue;
            DownGridObstacleDistance = float.MaxValue;
            UpGridHitPosition = Vector3D.Zero;
            DownGridHitPosition = Vector3D.Zero;
            UpGridObstacle = null;
            DownGridObstacle = null;
        }
        // -------------------------------------------------
        // Raycast
        // -------------------------------------------------
        private void CheckRay(Vector3D start, Vector3D direction, RaySide side, bool groundRay)
        {
            if (direction.LengthSquared() < 0.0001)
                return;
            direction.Normalize();
            Vector3D end = start + direction * SenseDistance;
            IHitInfo hitInfo = null;
            MyAPIGateway.Physics.CastRay(start, end, out hitInfo);
            if (hitInfo == null)
            {
                if (Render)
                    DrawRay(start, end, false);
                return;
            }
            IMyEntity entity = hitInfo.HitEntity;
            if (entity == null)
                return;
            // -------------------------------------------------
            // Ignore characters
            // -------------------------------------------------
            if (entity is IMyCharacter)
            {
                if (Render)
                    DrawRay(start, end, false);
                return;
            }
            float distance = (float)Vector3D.Distance(start, hitInfo.Position);
            // -------------------------------------------------
            // GRID
            // -------------------------------------------------
            IMyCubeGrid grid = entity as IMyCubeGrid;
            if (grid != null)
            {
                RegisterObstacle(
                    entity,
                    hitInfo.Position,
                    distance);
                RegisterDirectionalObstacle(
                    side,
                    entity,
                    distance);
                // General grid detection
                if (!IsGridDetected ||
                    distance < GridDistance)
                {
                    IsGridDetected = true;
                    GridDistance = distance;
                    GridHitPosition = hitInfo.Position;
                    GridHitEntity = grid;
                }
                // Directional grid detection
                if (side == RaySide.Up)
                {
                    if (!UpGridObstacleDetected ||
                        distance < UpGridObstacleDistance)
                    {
                        UpGridObstacleDetected = true;
                        UpGridObstacleDistance = distance;
                        UpGridHitPosition = hitInfo.Position;
                        UpGridObstacle = grid;
                    }
                }
                else if (side == RaySide.Down)
                {
                    if (!DownGridObstacleDetected ||
                        distance < DownGridObstacleDistance)
                    {
                        DownGridObstacleDetected = true;
                        DownGridObstacleDistance = distance;
                        DownGridHitPosition = hitInfo.Position;
                        DownGridObstacle = grid;
                    }
                }
                if (Render)
                    DrawRay(start, end, true);
                return;
            }
            // -------------------------------------------------
            // VOXEL
            // -------------------------------------------------
            IMyVoxelBase voxel = entity as IMyVoxelBase;
            if (voxel != null)
            {
                RegisterObstacle(entity, hitInfo.Position, distance);
                if (groundRay)
                {
                    if (!IsGroundDetected ||
                        distance < GroundDistance)
                    {
                        IsGroundDetected = true;
                        GroundDistance = distance;
                        GroundHitPosition = hitInfo.Position;
                        GroundHitEntity = voxel;
                    }
                }
                else
                {
                    RegisterDirectionalObstacle(side, entity, distance);
                }
                if (Render)
                    DrawRay(start, end, true);
                return;
            }
            // -------------------------------------------------
            // OTHER PHYSICS OBJECT
            // -------------------------------------------------
            RegisterObstacle(entity, hitInfo.Position, distance);
            if (!groundRay)
            {
                RegisterDirectionalObstacle(side, entity, distance);
            }
            if (Render)
                DrawRay(start, end, true);
        }
        // -------------------------------------------------
        // General obstacle
        // -------------------------------------------------
        private void RegisterObstacle(IMyEntity entity, Vector3D position, float distance)
        {
            if (!IsObstacleAhead ||
                distance < ObstacleDistance)
            {
                IsObstacleAhead = true;
                ObstacleDistance = distance;
                HitPosition = position;
                HitEntity = entity;
            }
        }
        // -------------------------------------------------
        // Directional obstacle
        // -------------------------------------------------
        private void RegisterDirectionalObstacle(RaySide side, IMyEntity entity, float distance)
        {
            if (side == RaySide.Front)
            {
                if (!FrontObstacleDetected ||
                    distance < FrontObstacleDistance)
                {
                    FrontObstacleDetected = true;
                    FrontObstacleDistance = distance;
                    FrontObstacle = entity;
                }
            }
            else if (side == RaySide.Left)
            {
                if (!LeftObstacleDetected ||
                    distance < LeftObstacleDistance)
                {
                    LeftObstacleDetected = true;
                    LeftObstacleDistance = distance;
                    LeftObstacle = entity;
                }
            }
            else if (side == RaySide.Right)
            {
                if (!RightObstacleDetected ||
                    distance < RightObstacleDistance)
                {
                    RightObstacleDetected = true;
                    RightObstacleDistance = distance;
                    RightObstacle = entity;
                }
            }
            else if (side == RaySide.Up)
            {
                if (!UpObstacleDetected ||
                    distance < UpObstacleDistance)
                {
                    UpObstacleDetected = true;
                    UpObstacleDistance = distance;
                    UpObstacle = entity;
                }
            }
            else if (side == RaySide.Down)
            {
                if (!DownObstacleDetected ||
                    distance < DownObstacleDistance)
                {
                    DownObstacleDetected = true;
                    DownObstacleDistance = distance;
                    DownObstacle = entity;
                }
            }
        }
        /// <summary>
        /// Draw Debug
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="hit"></param>
        private void DrawRay(Vector3D start, Vector3D end, bool hit)
        {
            if (!hit)
                return;
            Vector3D direction = end - start;
            if (direction.LengthSquared() < 0.0001)
                return;
            direction.Normalize();
            Vector3D drawFrom = start + direction * 0.5;
            Vector3D drawTo = start + direction * SenseDistance;
            Vector4 color;
            if (hit)
                color = Color.Red.ToVector4() * 10f;
            else
                color = Color.LimeGreen.ToVector4() * 10f;
            MySimpleObjectDraw.DrawLine(
                drawFrom,
                drawTo,
                MyStringId.GetOrCompute("Square"),
                ref color,
                0.02f,
                MyBillboard.BlendTypeEnum.Standard);
        }
        /// <summary>
        /// Check if animal in dangerous depth
        /// </summary>
        /// <param name="character"></param>
        /// <param name="minDepth"></param>
        /// <returns></returns>
        public bool IsDangerousDepth(IMyCharacter character,float minDepth)
        {
            if (character == null ||
                character.MarkedForClose)
                return true;
            if (!WaterModAPI.IsUnderwater(character.GetPosition()))
                return true;
            float depth = Math.Abs(AquaExpansionSession.Insance.GetWaterDepthbyCharacter(character));
            if (depth > minDepth)
                return false;
            if (IsGroundDetected ||
                DownGridObstacleDetected)
                return true;
            return false;
        }
    }
    public class AnimalDigestiveOrder
    {
        public Dictionary<MyDefinitionId, MyFixedPoint> FoodPrerequisites = new Dictionary<MyDefinitionId, MyFixedPoint>();
        public Dictionary<string,int> CloudWasteResults = new Dictionary<string, int>();
        public int Id;
        public string Displayname;
        public AnimalDigestiveOrder(int id,string displayname)
        {
            Id = id;
            Displayname = displayname;
        }

    }
    /// <summary>
    /// Animal Spawn Deffinition
    /// </summary>
    public class SeaAnimalSpawnDefinition
    {
        public string SubtypeId;
        public string BotSubtype;
        public bool Enabled;
        public int MaxPopulation;
        public float MinSpawnDepth;
        public float MaxSpawnDepth;
        public float MinSpawnDistance;
        public float MaxSpawnDistance;
        public int SpawnInterval;
        public float DespawnDistance;
        public string Name;
        public SeaAnimalSpawnDefinition(
            string subtypeId,
            string botsubtype,
            int maxPopulation,
            float minSpawnDepth,
            float maxSpawnDepth,
            float minSpawnDistance,
            float maxSpawnDistance,
            int spawnInterval,
            float despawnDistance,
            string name)
        {
            SubtypeId = subtypeId;
            BotSubtype = botsubtype;
            Enabled = true;
            MaxPopulation = maxPopulation;
            MinSpawnDepth = minSpawnDepth;
            MaxSpawnDepth = maxSpawnDepth;
            MinSpawnDistance = minSpawnDistance;
            MaxSpawnDistance = maxSpawnDistance;
            SpawnInterval = spawnInterval;
            DespawnDistance = despawnDistance;
            Name = name;
        }
    }
    public class SeaAnimalSpawnRecord
    {
        public readonly long EntityId;
        public readonly string SubtypeId;
        public readonly Vector3D SpawnPosition;
        public SeaAnimalSpawnRecord(
            long entityId,
            string subtypeId,
            Vector3D spawnPosition)
        {
            EntityId = entityId;
            SubtypeId = subtypeId;
            SpawnPosition = spawnPosition;
        }
    }
    /// <summary>
    /// Sea Animal Spawn Deffinition Database
    /// </summary>
    public static class SeaAnimalSpawnDatabase
    {
        private static readonly Dictionary<string, SeaAnimalSpawnDefinition> definitions = new Dictionary<string, SeaAnimalSpawnDefinition>();
        public static void Init()
        {
            Register(new SeaAnimalSpawnDefinition(
                 "AquaWhiteShark",
                 "AquaShark_Bot",//subtypeId
                  8,// max population
                10f, // min spawn depth
                20f, // max spawn depth
                100f, // min spawn distance
                500f, // max spawn distance
                5, //spawn interval
                1000f,
                "White Shark") // despawn distance
                );
        }
        private static void Register(SeaAnimalSpawnDefinition definition)
        {
            if (definition == null)
                return;
            if (string.IsNullOrWhiteSpace(definition.SubtypeId))
                return;
            if (definitions.ContainsKey(definition.SubtypeId))
            {
                AquaExpansionSession.Insance.Log(true,"Duplicate SeaAnimal Spawn definition: " + definition.SubtypeId);
                return;
            }
            definitions.Add(definition.SubtypeId,definition);
        }
        public static SeaAnimalSpawnDefinition Get(string subtypeid)
        {
            SeaAnimalSpawnDefinition def;
            if (definitions.TryGetValue(subtypeid, out def))
                return def;
            return null;
        }
        public static IEnumerable<SeaAnimalSpawnDefinition> GetAllSpawnDeffinitions()
        {
            return definitions.Values;
        }
    }
    public static class SeaAnimalGuidDatabase
    {
        private static readonly Dictionary<string, Guid> animalguids = new Dictionary<string, Guid>();
        public static void Init()
        {
            RegisterGuid("AquaWhiteShark",new Guid("D33FD442-888A-4BF4-A41E-8F016FF10484"));
        }
        private static void RegisterGuid(string subtype,Guid guid)
        {
            if (string.IsNullOrWhiteSpace(subtype))
                return;
            if (guid == Guid.Empty)
                return;
            animalguids[subtype] = guid;
        }
        public static Guid Get(string subtype)
        {
            if (string.IsNullOrWhiteSpace(subtype))
                return Guid.Empty;
            Guid guid;
            if (animalguids.TryGetValue(subtype, out guid))
                return guid;
            return Guid.Empty;
        }
        public static string GetSubtype(Guid guid)
        {
            if (guid == Guid.Empty)
                return null;
            foreach (KeyValuePair<string, Guid> pair in animalguids)
            {
                if (pair.Value == guid)
                    return pair.Key;
            }
            return null;
        }
        public static IEnumerable<Guid> GetAllAnimalGuids()
        {
            return animalguids.Values;
        }
        public static IEnumerable<KeyValuePair<string, Guid>> GetAllAnimalGuidsFull()
        {
            return animalguids;
        }
    }
}