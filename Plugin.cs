using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Assets.Scripts;
using Assets.Scripts.Atmospherics;
using Assets.Scripts.Inventory;
using Assets.Scripts.Objects;
using Assets.Scripts.Objects.Appliances;
using Assets.Scripts.Objects.Clothing;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Items;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.UI;
using BepInEx;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Objects.Items;
using Objects.Rockets;
using Reagents;
using UnityEngine;
using Util.Commands;

namespace StationeersTest
{
    [BepInPlugin(pluginGuid, pluginName, pluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        private const string pluginGuid = "io.inp.stationeers.stationpediaextractor";
        private const string pluginName = "Stationpedia Extractor";
        private const string pluginVersion = "1.0.0";
        private static Plugin instance;

        public static void Log(object line)
        {
            instance.Logger.LogInfo(line);
        }

        private void Awake()
        {
            instance = this;
            // Plugin startup logic
            Logger.LogInfo($"Plugin {pluginName} is loaded!");

            CommandLine.AddCommand("stationpedia_export", new StationpediaExportCommand());
        }
    }

    struct OutputSlotsInset
    {
        public string SlotName;
        public string SlotType;
        public string SlotIndex;

        public void setFromStationSlotInsert(StationSlotsInsert insert)
        {
            SlotName = insert.SlotName;
            SlotType = insert.SlotType;
            SlotIndex = insert.SlotIndex;
        }
    }

    struct OutputLogicInsert
    {
        public string LogicName;
        public string LogicAccessTypes;

        public void setFromStationLogicInsert(StationLogicInsert insert)
        {
            LogicName = insert.LogicName;
            LogicAccessTypes = insert.LogicAccessTypes;
        }
    }

    struct OutputCategoryInsert
    {
        public string NameOfThing;
        public int PrefabHash;
        public string PageLink;

        public void setFromStationCategoryInsert(StationCategoryInsert insert)
        {
            NameOfThing = insert.NameOfThing;
            PrefabHash = insert.PrefabHash;
            PageLink = insert.PageLink;
        }
    }

    struct OutputPrefab
    {
        public OutputPrefab(Thing prefab)
        {
            OutputThing = prefab;
        }

        public Thing OutputThing;

        public void writeToJson(JsonWriter writer)
        {
            Thing thing = OutputThing;
            Device device = thing as Device;
            DynamicThing dynamicthing = thing as DynamicThing;

            if (thing is Human human)
            {
                writer.WritePropertyName("Human");
                writer.WriteStartObject();

                writer.WritePropertyName("Slots");
                writer.WriteStartArray();
                for (int i = 0; i < thing.Slots.Count; i++)
                {
                    if (thing.Slots[i] != null)
                    {
                        writer.WriteStartObject();
                        writer.WritePropertyName("SlotClass");
                        writer.WriteValue(Enum.GetName(typeof(Slot.Class), thing.Slots[i].Type));
                        writer.WritePropertyName("StringHash");
                        writer.WriteValue(thing.Slots[i].StringHash);
                        writer.WritePropertyName("StringKey");
                        writer.WriteValue(thing.Slots[i].StringKey);
                        writer.WritePropertyName("SlotName");
                        String slotStr;
                        int hash = thing.Slots[i].StringHash;
                        if (hash == Slot.LeftHandHash)
                        {
                            slotStr = "LeftHand";
                        }
                        else if (hash == Slot.RightHandHash)
                        {
                            slotStr = "RightHand";
                        }
                        else if (hash == Slot.UniformHash)
                        {
                            slotStr = "Uniform";
                        }
                        else if (hash == Slot.LungsHash)
                        {
                            slotStr = "Lungs";
                        }
                        else if (hash == Slot.BrainHash)
                        {
                            slotStr = "Brain";
                        }
                        else if (hash == Slot.ProgrammableChipHash)
                        {
                            slotStr = "ProgrammableChip";
                        }
                        else
                        {
                            slotStr = "";
                        }
                        writer.WriteValue(slotStr);
                        writer.WriteEndObject();
                    }
                }
                writer.WriteEndArray();

                writer.WritePropertyName("Hydration");
                writer.WriteValue(human.Hydration);
                writer.WritePropertyName("MaxHydration");
                writer.WriteValue(8.75f); // hardcoded with Mathf.Clamp
                writer.WritePropertyName("Nutrition");
                writer.WriteValue(human.Nutrition);
                writer.WritePropertyName("BaseNutritionStorage");
                writer.WriteValue(human.BaseNutritionStorage);
                writer.WritePropertyName("FoodQuality");
                writer.WriteValue(human.FoodQuality);
                writer.WritePropertyName("MaxFoodQuality");
                writer.WriteValue(1f); // hardcoded with Mathf.Clamp
                writer.WritePropertyName("Oxygenation");
                writer.WriteValue(human.Oxygenation);
                writer.WritePropertyName("MaxOxygenStorage");
                writer.WriteValue(0.024f); // MaxOxygenStorage is protected
                writer.WritePropertyName("Mood");
                writer.WriteValue(human.Mood);
                writer.WritePropertyName("MaxMood");
                writer.WriteValue(1f); // hardcoded with Mathf.Clamp
                writer.WritePropertyName("Hygiene");
                writer.WriteValue(human.Hygiene);
                writer.WritePropertyName("MaxHygiene");
                writer.WriteValue(1.25f); // hardcoded with Mathf.Clamp

                writer.WritePropertyName("NutritionDamageRateAwake");
                writer.WriteValue(human.NutritionDamageRate);
                writer.WritePropertyName("DehydrationDamageRateAwake");
                writer.WriteValue(human.DehydrationDamageRate);
                // TODO: Entity.SetRagdoll fails here, we need to patch the method
                // var lastState = human.State;
                // human.State = EntityState.Unconscious;
                // writer.WritePropertyName("NutritionDamageRateSleeping");
                // writer.WriteValue(human.NutritionDamageRate);
                // writer.WritePropertyName("DehydrationDamageRateSleeping");
                // writer.WriteValue(human.DehydrationDamageRate);
                // human.State = lastState;

                writer.WritePropertyName("WarningOxygen");
                writer.WriteValue(human.WarningOxygen);
                writer.WritePropertyName("CriticalOxygen");
                writer.WriteValue(human.CriticalOxygen);
                writer.WritePropertyName("WarningNutrition");
                writer.WriteValue(human.WarningNutrition);
                writer.WritePropertyName("CriticalNutrition");
                writer.WriteValue(human.CriticalNutrition);
                writer.WritePropertyName("WarningHydration");
                writer.WriteValue(human.WarningHydration);
                writer.WritePropertyName("CriticalHydration");
                writer.WriteValue(human.CriticalHydration);
                writer.WritePropertyName("FullNutrition");
                writer.WriteValue(human.FullNutrition);
                writer.WritePropertyName("WarningHealth");
                writer.WriteValue(human.WarningHealth);
                writer.WritePropertyName("CriticalHealth");
                writer.WriteValue(human.CriticalHealth);
                writer.WritePropertyName("WarningMood");
                writer.WriteValue(human.WarningMood);
                writer.WritePropertyName("CriticalMood");
                writer.WriteValue(human.CriticalMood);
                writer.WritePropertyName("WarningHygiene");
                writer.WriteValue(human.WarningHygiene);
                writer.WritePropertyName("CriticalHygiene");
                writer.WriteValue(human.CriticalHygiene);
                writer.WritePropertyName("ToxicPartialPressureDamage");
                writer.WriteValue(Human.ToxicPartialPressureForDamage.ToDouble());
                writer.WritePropertyName("ToxicPartialPressureWarning");
                writer.WriteValue(Human.ToxicPartialPressureForWarning.ToDouble());

                writer.WriteEndObject();
            }

            if (thing is Organ organ)
            {
                writer.WritePropertyName("Organ");
                writer.WriteStartObject();
                writer.WriteEndObject();
            }

            if (thing is Brain brain)
            {
                writer.WritePropertyName("Brain");
                writer.WriteStartObject();
                writer.WriteEndObject();
            }

            if (thing is Lungs lungs)
            {
                writer.WritePropertyName("Lungs");
                writer.WriteStartObject();
                writer.WritePropertyName("TemperatureMin");
                writer.WriteValue(
                    ((TemperatureKelvin)lungs
                        .GetType()
                        .GetProperty(
                            "TemperatureMin",
                            BindingFlags.NonPublic | BindingFlags.Instance
                        )
                        .GetValue(lungs)).ToDouble()
                );
                writer.WritePropertyName("TemperatureMax");
                writer.WriteValue(
                    ((TemperatureKelvin)lungs
                        .GetType()
                        .GetProperty(
                            "TemperatureMax",
                            BindingFlags.NonPublic | BindingFlags.Instance
                        )
                        .GetValue(lungs)).ToDouble()
                );
                writer.WritePropertyName("Volume");
                writer.WriteValue(lungs.Volume.ToDouble());

                writer.WritePropertyName("BreathableType");
                writer.WriteValue(Enum.GetName(typeof(Chemistry.GasType), lungs.BreathedType));
                writer.WritePropertyName("ToxicTypes");
                writer.WriteStartArray();
                foreach (Chemistry.GasType gasType in EnumCollections.GasTypes.Values)
                {
                    if ((lungs.ToxicTypes & gasType) != Chemistry.GasType.Undefined)
                    {
                        writer.WriteValue(Enum.GetName(typeof(Chemistry.GasType), gasType));
                    }
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            ILogicable logicable = thing as ILogicable;
            if (logicable != null)
            {
                writer.WritePropertyName("LogicInfo");
                writer.WriteStartObject();

                writer.WritePropertyName("LogicSlotTypes");
                writer.WriteStartObject();
                for (int i = 0; i < thing.Slots.Count; i++)
                {
                    if (thing.Slots[i] != null)
                    {
                        writer.WritePropertyName(i.ToString());
                        writer.WriteStartObject();
                        foreach (LogicSlotType logicSlotType in Logicable.LogicSlotTypes)
                        {
                            bool read = logicable.CanLogicRead(logicSlotType, i);
                            bool write = false;
                            if (device != null)
                            {
                                write = device.CanLogicWrite(logicSlotType, i);
                            }
                            if (read || write)
                            {
                                writer.WritePropertyName(
                                    Enum.GetName(typeof(LogicSlotType), logicSlotType)
                                );
                                if (read && !write)
                                {
                                    writer.WriteValue("Read");
                                }
                                else if (!read && write)
                                {
                                    writer.WriteValue("Write");
                                }
                                else
                                {
                                    writer.WriteValue("ReadWrite");
                                }
                            }
                        }
                        writer.WriteEndObject();
                    }
                }
                writer.WriteEndObject();

                writer.WritePropertyName("LogicTypes");
                writer.WriteStartObject();
                foreach (LogicType logicType in EnumCollections.LogicTypes.Values)
                {
                    bool read = logicable.CanLogicRead(logicType);
                    bool write = logicable.CanLogicWrite(logicType);
                    if (read || write)
                    {
                        writer.WritePropertyName(Enum.GetName(typeof(LogicType), logicType));
                        if (read && !write)
                        {
                            writer.WriteValue("Read");
                        }
                        else if (!read && write)
                        {
                            writer.WriteValue("Write");
                        }
                        else
                        {
                            writer.WriteValue("ReadWrite");
                        }
                    }
                }
                writer.WriteEndObject();

                writer.WriteEndObject();

                IMemory memory = thing as IMemory;
                if (memory != null)
                {
                    writer.WritePropertyName("Memory");
                    writer.WriteStartObject();

                    int memorySize = memory.GetStackSize();
                    writer.WritePropertyName("MemorySize");
                    writer.WriteValue(memorySize);
                    writer.WritePropertyName("MemorySizeReadable");
                    writer.WriteValue(
                        NetworkHelper.GetBytesReadable((long)(memory.GetStackSize() * 8))
                    );

                    IMemoryWritable memoryWritable = memory as IMemoryWritable;
                    IMemoryReadable memoryReadable = memory as IMemoryReadable;
                    writer.WritePropertyName("MemoryAccess");
                    if (memoryWritable != null && memoryReadable != null)
                    {
                        writer.WriteValue("ReadWrite");
                    }
                    else if (memoryReadable != null)
                    {
                        writer.WriteValue("Read");
                    }
                    else if (memoryWritable != null)
                    {
                        writer.WriteValue("Write");
                    }
                    else
                    {
                        writer.WriteValue("None");
                    }

                    IInstructable instructable = memory as IInstructable;
                    if (instructable != null)
                    {
                        writer.WritePropertyName("Instructions");
                        writer.WriteStartObject();
                        IEnumCollection instructions = instructable.GetInstructions();
                        for (int i = 1; i < instructions.Length; i++)
                        {
                            writer.WritePropertyName(instructions.GetNameFromIndex(i, false));
                            writer.WriteStartObject();
                            writer.WritePropertyName("Type");
                            writer.WriteValue(instructions.GetEnumTypeName());
                            int intFromIndex = instructions.GetIntFromIndex(i);
                            writer.WritePropertyName("Value");
                            writer.WriteValue(intFromIndex);
                            writer.WritePropertyName("Description");
                            writer.WriteValue(instructable.GetInstructionDescription(i));
                            writer.WriteEndObject();
                        }
                        writer.WriteEndObject();
                    }

                    writer.WriteEndObject();
                }
            }

            if (thing is ITransmitable)
            {
                writer.WritePropertyName("WirelessLogic");
                writer.WriteValue(true);
            }

            if (thing is ITransmissionReceiver)
            {
                writer.WritePropertyName("TransmissionReceiver");
                writer.WriteValue(true);
            }

            if (thing is ICircuitHolder)
            {
                writer.WritePropertyName("CircuitHolder");
                writer.WriteValue(true);
            }

            if (thing is ISourceCode)
            {
                writer.WritePropertyName("SourceCode");
                writer.WriteValue(true);
            }

            if (thing is IChargable chargeable)
            {
                writer.WritePropertyName("Chargeable");
                writer.WriteStartObject();

                writer.WritePropertyName("PowerMaximum");
                writer.WriteValue(chargeable.GetPowerMaximum());

                writer.WriteEndObject();
            }

            if (thing is IResourceConsumer consumer)
            {
                writer.WritePropertyName("ResourceConsumer");
                writer.WriteStartObject();

                writer.WritePropertyName("ConsumedResources");
                writer.WriteStartArray();
                foreach (var item in consumer.GetResourcesUsed())
                {
                    writer.WriteValue(item.GetPrefabName());
                }
                writer.WriteEndArray();

                writer.WritePropertyName("ProcessedReagents");
                writer.WriteStartArray();
                foreach (var reagent in Reagent.AllReagentsSorted)
                {
                    if (consumer.CanProcess(reagent))
                    {
                        writer.WriteValue(reagent.Hash);
                    }
                }
                writer.WriteEndArray();

                writer.WriteEndObject();
            }

            if (device != null)
            {
                writer.WritePropertyName("Device");
                writer.WriteStartObject();

                writer.WritePropertyName("ConnectionList");
                writer.WriteStartArray();
                {
                    for (int j = 0; j < device.OpenEnds.Count; j++)
                    {
                        writer.WriteStartArray();
                        Connection connection = device.OpenEnds[j];
                        NetworkType typ = connection.ConnectionType;
                        ConnectionRole role = connection.ConnectionRole;
                        var typ_name = Enum.GetName(typeof(NetworkType), typ);
                        writer.WriteValue(typ_name);
                        var role_name = Enum.GetName(typeof(ConnectionRole), role);
                        writer.WriteValue(role_name);
                        writer.WriteEnd();
                    }
                }
                writer.WriteEnd();

                CircuitHousing circuitHousing = device as CircuitHousing;
                DeviceInputOutputCircuit deviceCircuitIO = device as DeviceInputOutputCircuit;
                DeviceInputOutputImportExportCircuit deviceCircuitIOIE =
                    device as DeviceInputOutputImportExportCircuit;

                if (circuitHousing || deviceCircuitIO || deviceCircuitIOIE)
                {
                    writer.WritePropertyName("DevicesLength");
                    if (circuitHousing)
                    {
                        writer.WriteValue(circuitHousing.Devices.Length);
                    }
                    else if (deviceCircuitIO)
                    {
                        writer.WriteValue(deviceCircuitIO.Devices.Length);
                    }
                    else if (deviceCircuitIOIE)
                    {
                        writer.WriteValue(deviceCircuitIOIE.Devices.Length);
                    }
                }

                writer.WritePropertyName("HasReagents");
                writer.WriteValue(device.HasReadableReagentMixture);
                writer.WritePropertyName("HasAtmosphere");
                writer.WriteValue(device.HasReadableAtmosphere);
                writer.WritePropertyName("HasLockState");
                writer.WriteValue(device.HasLockState);
                writer.WritePropertyName("HasOpenState");
                writer.WriteValue(device.HasOpenState);
                writer.WritePropertyName("HasOnOffState");
                writer.WriteValue(device.HasOnOffState);
                writer.WritePropertyName("HasActivateState");
                writer.WriteValue(device.HasActivateState);
                writer.WritePropertyName("HasModeState");
                writer.WriteValue(device.HasModeState);
                writer.WritePropertyName("HasColorState");
                writer.WriteValue(device.HasColorState);

                if (device is SimpleFabricatorBase fabricator)
                {
                    writer.WritePropertyName("Fabricator");
                    writer.WriteStartObject();

                    writer.WritePropertyName("Tier");
                    writer.WriteValue(fabricator.CurrentTier);
                    writer.WritePropertyName("TierName");
                    writer.WriteValue(fabricator.CurrentTier.ToString());

                    writer.WritePropertyName("Recipes");
                    writer.WriteStartObject();
                    foreach (var recipePair in fabricator.Recipes)
                    {
                        writer.WritePropertyName(recipePair.Key.GetPrefabName());
                        writer.WriteStartObject();
                        writer.WritePropertyName("CreatorPrefabName");
                        writer.WriteValue(fabricator.PrefabName);
                        if (
                            recipePair.Key.RecipeTier != MachineTier.Undefined
                            && recipePair.Key.RecipeTier != MachineTier.Max
                        )
                        {
                            writer.WritePropertyName("TierName");
                            writer.WriteValue(recipePair.Key.RecipeTier.ToString());
                        }

                        try
                        {
                            JToken recipeToken = JToken.FromObject(recipePair.Value);
                            WriteNonZeroNonNullProperties(recipeToken, writer, ["Pressure", "Rule", "Temperature"]);
                        }
                        catch (FormatException ex)
                        {
                            Debug.LogError(
                                (object)(
                                    "There was an error with text "
                                    + Stationpedia.CreatorItem.Parsed
                                    + " "
                                    + ex.Message
                                )
                            );
                        }
                        writer.WriteEndObject();
                    }
                    writer.WriteEndObject();

                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
            }

            if (thing is Structure structure)
            {
                writer.WritePropertyName("Structure");
                writer.WriteStartObject();

                writer.WritePropertyName("SmallGrid");
                writer.WriteValue(thing is SmallGrid);

                if (structure.BuildStates.Count > 0)
                {
                    writer.WritePropertyName("BuildStates");
                    writer.WriteStartArray();
                    foreach (BuildState buildState in structure.BuildStates)
                    {
                        writer.WriteStartObject();
                        if (
                            buildState.Tool.ToolEntry is not null
                            || buildState.Tool.ToolEntry2 is not null
                        )
                        {
                            writer.WritePropertyName("Tool");
                            writer.WriteStartArray();
                            if (buildState.Tool.ToolEntry is { } toolEntry)
                            {
                                writer.WriteStartObject();
                                writer.WritePropertyName("PrefabName");
                                writer.WriteValue(toolEntry.PrefabName);
                                if (toolEntry is IQuantity)
                                {
                                    writer.WritePropertyName("Quantity");
                                    writer.WriteValue(buildState.Tool.EntryQuantity);
                                }
                                writer.WritePropertyName("IsTool");
                                writer.WriteValue(toolEntry is Tool);
                                writer.WriteEndObject();
                            }
                            if (buildState.Tool.ToolEntry2 is { } toolEntry2)
                            {
                                writer.WriteStartObject();
                                writer.WritePropertyName("PrefabName");
                                writer.WriteValue(toolEntry2.PrefabName);
                                if (toolEntry2 is IQuantity)
                                {
                                    writer.WritePropertyName("Quantity");
                                    writer.WriteValue(buildState.Tool.EntryQuantity2);
                                }
                                writer.WritePropertyName("IsTool");
                                writer.WriteValue(toolEntry2 is Tool);
                                writer.WriteEndObject();
                            }
                            writer.WriteEndArray();
                        }
                        if (buildState.Tool.ToolExit is not null || buildState.Tool is not null)
                        {
                            writer.WritePropertyName("ToolExit");
                            writer.WriteStartArray();
                            if (buildState.Tool.ToolExit is { } toolExit)
                            {
                                writer.WriteStartObject();
                                writer.WritePropertyName("PrefabName");
                                writer.WriteValue(toolExit.PrefabName);
                                writer.WriteEndObject();
                            }
                            writer.WriteEndArray();
                        }
                        if (buildState.CanManufacture)
                        {
                            writer.WritePropertyName("CanManufacture");
                            writer.WriteValue(buildState.CanManufacture);
                            writer.WritePropertyName("MachineTier");
                            writer.WriteValue(buildState.ManufactureDat.MachinesTier.ToString());
                        }
                        writer.WriteEndObject();
                    }
                    writer.WriteEnd();
                }

                writer.WriteEndObject();
            }

            if (dynamicthing)
            {
                writer.WritePropertyName("Item");
                writer.WriteStartObject();

                writer.WritePropertyName("SlotClass");
                writer.WriteValue(Enum.GetName(typeof(Slot.Class), dynamicthing.SlotType));
                writer.WritePropertyName("SortingClass");
                writer.WriteValue(Enum.GetName(typeof(SortingClass), dynamicthing.SortingClass));
                if (dynamicthing is IQuantity)
                {
                    IQuantity quantity = dynamicthing as IQuantity;
                    double maxQuantity;
                    float? num = (quantity != null) ? new float?(quantity.GetMaxQuantity) : null;
                    if (num == null)
                    {
                        maxQuantity = 1.0;
                    }
                    else
                    {
                        maxQuantity = num.GetValueOrDefault();
                    }

                    writer.WritePropertyName("MaxQuantity");
                    writer.WriteValue(maxQuantity);
                }
                if (dynamicthing is GasFilter)
                {
                    GasFilter gasFilter = dynamicthing as GasFilter;
                    writer.WritePropertyName("FilterType");
                    writer.WriteValue(
                        Enum.GetName(typeof(Chemistry.GasType), gasFilter.FilterType)
                    );
                }

                if (
                    !(
                        !(bool)(UnityEngine.Object)dynamicthing
                        || Stationpedia.CreatorItem == null
                        || dynamicthing is Plant
                    )
                )
                {
                    List<RecipeReference> allMyCreators = ElectronicReader.GetAllMyCreators(
                        dynamicthing
                    );
                    if (!(allMyCreators == null))
                    {
                        List<int> existingCreators = new List<int>();
                        writer.WritePropertyName("Recipes");
                        writer.WriteStartArray();

                        for (int index = 0; index < allMyCreators.Count; ++index)
                        {
                            RecipeReference reference = allMyCreators[index];
                            if (!(reference.Creator is Fabricator))
                            {
                                writer.WriteStartObject();
                                writer.WritePropertyName("CreatorPrefabName");
                                writer.WriteValue(reference.Creator.PrefabName);
                                if (
                                    dynamicthing.RecipeTier != MachineTier.Undefined
                                    && dynamicthing.RecipeTier != MachineTier.Max
                                )
                                {
                                    writer.WritePropertyName("TierName");
                                    writer.WriteValue(dynamicthing.RecipeTier.ToString());
                                }

                                try
                                {
                                    JToken recipeToken = JToken.FromObject(reference.Recipe);
                                    WriteNonZeroNonNullProperties(recipeToken, writer, ["Pressure", "Rule", "Temperature"]);
                                }
                                catch (FormatException ex)
                                {
                                    Debug.LogError(
                                        (object)(
                                            "There was an error with text "
                                            + Stationpedia.CreatorItem.Parsed
                                            + " "
                                            + ex.Message
                                        )
                                    );
                                }
                                writer.WriteEndObject();
                            }
                        }
                        writer.WriteEndArray();
                    }
                }

                if (dynamicthing is INutrition nutrition)
                {
                    writer.WritePropertyName("Food");
                    writer.WriteStartObject();

                    writer.WritePropertyName("NutritionQuality");
                    writer.WriteValue(nutrition.GetFoodQuality());

                    if (nutrition is Food food)
                    {
                        writer.WritePropertyName("NutritionValue");
                        writer.WriteValue(food.GetNutritionalValue());
                        // MoodBonus
                        writer.WritePropertyName("MoodBonus");
                        writer.WriteValue(food.MoodBonus);
                    }
                    else if (nutrition is StackableFood stackable)
                    {
                        writer.WritePropertyName("NutritionValue");
                        writer.WriteValue(stackable.GetNutritionalValue());
                        writer.WritePropertyName("MoodBonus");
                        writer.WriteValue(stackable.MoodBonus);
                    }
                    else if (nutrition is Plant plant)
                    {
                        writer.WritePropertyName("NutritionValue");
                        writer.WriteValue(plant.GetNutritionalValue());
                        writer.WritePropertyName("MoodBonus");
                        writer.WriteValue(plant.MoodBonus);
                    }

                    writer.WritePropertyName("NutritionQualityReadable");
                    writer.WriteValue(
                        (string)Food.GetFoodQualityStationpediaDescription(nutrition)
                    );

                    writer.WriteEndObject();
                }

                Consumable consumable = thing as Consumable;
                IIngredient ingredient = thing as IIngredient;

                if (consumable != null || ingredient != null)
                {
                    ReagentMixture mixture;
                    if (ingredient != null)
                    {
                        mixture = ingredient.AddMixture;
                    }
                    else
                    {
                        mixture = consumable.CreatedReagentMixture;
                    }

                    if (mixture.TotalReagents > 0.0)
                    {
                        if (consumable != null)
                        {
                            writer.WritePropertyName("Consumable");
                            writer.WriteValue(true);
                        }
                        if (ingredient != null)
                        {
                            writer.WritePropertyName("Ingredient");
                            writer.WriteValue(true);
                        }
                        writer.WritePropertyName("Reagents");
                        writer.WriteStartObject();

                        foreach (var reagent in Reagent.AllReagentsSorted)
                        {
                            double val;

                            if (consumable != null)
                            {
                                val = consumable.CreatedReagentMixture.Get(reagent);
                            }
                            else
                            {
                                val = ingredient.AddMixture.Get(reagent);
                            }

                            if (val > 0.0)
                            {
                                writer.WritePropertyName(reagent.TypeNameShort);
                                writer.WriteValue(val);
                            }
                        }
                        writer.WriteEnd();
                    }
                }

                if (thing is IWearable)
                {
                    writer.WritePropertyName("Wearable");
                    writer.WriteValue(true);
                }

                if (thing is ISuit suit)
                {
                    writer.WritePropertyName("Suit");
                    writer.WriteStartObject();

                    writer.WritePropertyName("HygieneReductionMultiplier");
                    writer.WriteValue(suit.HygieneReductionMultiplier);
                    writer.WritePropertyName("WasteMaxPressure");
                    writer.WriteValue(suit.WasteMaxPressure.ToDouble());

                    writer.WriteEndObject();
                }

                writer.WriteEnd();
            }

            if (thing is IInternalAtmosphere internalAtmosphere)
            {
                // setup internal atmo if it has one so we can collect data

                writer.WritePropertyName("InternalAtmosphere");
                writer.WriteStartObject();

                IVolume volume = thing as IVolume;
                writer.WritePropertyName("Volume");
                writer.WriteValue(volume != null ? volume.GetVolume.ToDouble() : 0.0);

                writer.WriteEndObject();
            }

            if (thing is IThermal thermal)
            {
                writer.WritePropertyName("Thermal");
                writer.WriteStartObject();

                writer.WritePropertyName("Convection");
                writer.WriteValue(thermal.ConvectionFactor);

                writer.WritePropertyName("Radiation");
                writer.WriteValue(thermal.RadiationFactor);

                writer.WriteEndObject();
            }
        }
        private void WriteNonZeroNonNullProperties(JToken props, JsonWriter writer, string[] ignore = null)
        {
            if (props.Type == JTokenType.Object)
            {
                foreach (var prop in props.Children<JProperty>())
                {
                    if (!ignore.Contains(prop.Name) && (prop.Value.ToString() == "0" || prop.Value.ToString() == "0.0")) continue;
                    writer.WritePropertyName(prop.Name);
                    if (
                        prop.Value.Type == JTokenType.Object
                    )
                    {
                        if (ignore.Contains(prop.Name))
                        {
                            prop.Value.WriteTo(writer);
                        }
                        else
                        {
                            writer.WriteStartObject();
                            WriteNonZeroNonNullProperties(prop.Value, writer, ignore);
                            writer.WriteEndObject();
                        }
                    }
                    else if (prop.Value.Type == JTokenType.Array)
                    {
                        prop.Value.WriteTo(writer);
                    }
                    else
                    {
                        writer.WriteValue(prop.Value);
                    }
                }
            }
        }
    }

    struct OutputStationpediaPage
    {
        public string Key;
        public string Title;
        public string Description;
        public string PrefabName;
        public int PrefabHash;
        public string BasePowerDraw;
        public string MaxPressure;
        public string GrowthTime;
        public List<OutputSlotsInset> SlotInserts;
        public List<OutputLogicInsert> LogicInsert;
        public List<OutputLogicInsert> LogicSlotInsert;
        public List<OutputLogicInsert> ModeInsert;
        public List<OutputLogicInsert> ConnectionInsert;
        public List<OutputCategoryInsert> ConstructedByKits;

        public void setFromPage(StationpediaPage page)
        {
            Key = page.Key;
            Title = page.Title;
            Description = page.Description;
            PrefabName = page.PrefabName;
            PrefabHash = page.PrefabHash;
            BasePowerDraw = page.BasePowerDraw;
            MaxPressure = page.MaxPressure;
            GrowthTime = page.GrowthTime;
            SlotInserts = page.SlotInserts.ConvertAll(i =>
            {
                OutputSlotsInset oi = new();
                oi.setFromStationSlotInsert(i);
                return oi;
            });
            LogicInsert = page.LogicInsert.ConvertAll(i =>
            {
                OutputLogicInsert oi = new();
                oi.setFromStationLogicInsert(i);
                return oi;
            });
            LogicSlotInsert = page.LogicSlotInsert.ConvertAll(i =>
            {
                OutputLogicInsert oi = new();
                oi.setFromStationLogicInsert(i);
                return oi;
            });
            ModeInsert = page.ModeInsert.ConvertAll(i =>
            {
                OutputLogicInsert oi = new();
                oi.setFromStationLogicInsert(i);
                return oi;
            });
            ConnectionInsert = page.ConnectionInsert.ConvertAll(i =>
            {
                OutputLogicInsert oi = new();
                oi.setFromStationLogicInsert(i);
                return oi;
            });
            ConstructedByKits = page.ConstructedByKits.ConvertAll(i =>
            {
                OutputCategoryInsert oi = new();
                oi.setFromStationCategoryInsert(i);
                return oi;
            });
        }

        public void writeToJson(JsonWriter writer)
        {
            writer.WriteStartObject();

            JObject obj = JObject.FromObject(this);
            foreach (var property in obj.Properties())
            {
                property.WriteTo(writer);
            }

            OutputPrefab p = new(Prefab.Find(PrefabName));
            p.writeToJson(writer);

            writer.WriteEnd();
        }
    }

    struct EnumEntry
    {
        public int value;
        public bool deprecated;
        public string description;
    }

    struct EnumListing
    {
        public string enumName;
        public Dictionary<string, EnumEntry> values;
    }

    struct EnumsOutput
    {
        public Dictionary<string, EnumListing> scriptEnums;
        public Dictionary<string, EnumListing> basicEnums;
    }

    class StationpediaExportCommand : CommandBase
    {
        public override string HelpText => "Export Stationpedia";

        public override string[] Arguments { get; } = new string[] { };

        public override bool IsLaunchCmd { get; }

        public override string Execute(string[] args)
        {
            string out_path = Path.Combine(
                Path.Combine(Application.dataPath, ".."),
                "Stationpedia"
            );
            Directory.CreateDirectory(out_path);
            List<string> msgs = new();

            {
                msgs.Add("Writing Stationpedia...");
                string path = Path.Combine(out_path, "Stationpedia.json");
                StreamWriter sw = new(path);
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    writer.Formatting = Formatting.Indented;

                    writer.WriteStartObject();
                    writer.WritePropertyName("version");
                    writer.WriteValue(typeof(Stationpedia).Assembly.GetName().Version.ToString());
                    writer.WritePropertyName("pages");
                    writer.WriteStartArray();
                    foreach (var page in Stationpedia.StationpediaPages)
                    {
                        page.ParsePage();
                        if (page.PrefabHashString != null && page.PrefabHashString != "")
                        {
                            OutputStationpediaPage p = new();
                            p.setFromPage(page);
                            p.writeToJson(writer);
                        }
                    }

                    writer.WriteEndArray();

                    writer.WritePropertyName("core_prefabs");
                    writer.WriteStartArray();

                    List<Thing> corePrefabs =
                    [
                        Prefab.Find<Human>("Character"),
                        Prefab.Find<Brain>("OrganBrain"),
                        Prefab.Find<Lungs>("OrganLungs"),
                        Prefab.Find<Lungs>("OrganLungsZrilian"),
                        Prefab.Find<Lungs>("OrganLungsChicken")
                    ];

                    foreach (var prefab in corePrefabs)
                    {
                        writer.WriteStartObject();
                        writer.WritePropertyName("Name");
                        writer.WriteValue(prefab.PrefabName);

                        OutputPrefab p = new(prefab);
                        p.writeToJson(writer);

                        writer.WriteEndObject();
                    }
                    writer.WriteEndArray();

                    writer.WritePropertyName("reagents");
                    writer.WriteStartObject();
                    foreach (var reagent in Reagent.AllReagentsSorted)
                    {
                        writer.WritePropertyName(reagent.TypeNameShort);
                        writer.WriteStartObject();
                        writer.WritePropertyName("Id");
                        writer.WriteValue(reagent.ReagentId);
                        writer.WritePropertyName("Hash");
                        writer.WriteValue(reagent.Hash);
                        writer.WritePropertyName("Unit");
                        writer.WriteValue(reagent.Unit);
                        writer.WritePropertyName("IsOrganic");
                        writer.WriteValue(reagent is OrganicReagent);

                        List<Item> allSources = ElectronicReader.GetAllSources(reagent);
                        if (allSources != null)
                        {
                            writer.WritePropertyName("Sources");
                            writer.WriteStartObject();
                            for (int i = 0; i < allSources.Count; i++)
                            {
                                Item item = allSources[i];
                                writer.WritePropertyName(item.PrefabName);
                                writer.WriteValue(item.QuantityPerUse);
                            }
                            writer.WriteEndObject();
                        }
                        writer.WriteEndObject();
                    }
                    writer.WriteEndObject();

                    writer.WritePropertyName("scriptCommands");
                    writer.WriteStartObject();
                    foreach (ScriptCommand cmd in EnumCollections.ScriptCommands.Values)
                    {
                        writer.WritePropertyName(Enum.GetName(typeof(ScriptCommand), cmd));
                        writer.WriteStartObject();
                        writer.WritePropertyName("desc");
                        writer.WriteValue(ProgrammableChip.GetCommandDescription(cmd));
                        writer.WritePropertyName("example");
                        writer.WriteValue(ProgrammableChip.GetCommandExample(cmd));
                        writer.WriteEndObject();
                    }
                    writer.WriteEndObject();

                    writer.WriteEndObject();
                }
            }

            {
                msgs.Add("Writing Enums...");
                string path = Path.Combine(out_path, "Enums.json");
                StreamWriter sw = new(path);
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    writer.Formatting = Formatting.Indented;
                    // writer.WriteStartObject();

                    List<IScriptEnum> scriptEnums = ProgrammableChip.InternalEnums;
                    EnumsOutput enumsOutput = new();
                    enumsOutput.scriptEnums = new();
                    enumsOutput.basicEnums = new();
                    foreach (IScriptEnum e in scriptEnums)
                    {
                        EnumListing listing = new();
                        listing.values = new();
                        Type seTyp = e.GetType();
                        FieldInfo ftypTypes = seTyp.GetField(
                            "_types",
                            System.Reflection.BindingFlags.NonPublic
                                | System.Reflection.BindingFlags.Instance
                        );
                        FieldInfo ftypNames = seTyp.GetField(
                            "_names",
                            System.Reflection.BindingFlags.NonPublic
                                | System.Reflection.BindingFlags.Instance
                        );
                        FieldInfo ftypTypeString = seTyp.GetField(
                            "_typeString",
                            System.Reflection.BindingFlags.NonPublic
                                | System.Reflection.BindingFlags.Instance
                        );
                        var types = (Array)ftypTypes.GetValue(e);
                        var names = (string[])ftypNames.GetValue(e);
                        Type enumTyp = types.GetType().GetElementType();
                        string typeName;
                        if (ftypTypeString == null)
                        {
                            typeName = enumTyp.Name;
                        }
                        else
                        {
                            typeName = (string)ftypTypeString.GetValue(e);
                        }
                        if (string.IsNullOrEmpty(typeName))
                        {
                            typeName = "_unnamed";
                        }

                        listing.enumName = enumTyp.Name;

                        // writer.WritePropertyName(typeName);
                        // writer.WriteStartObject();
                        //
                        // writer.WritePropertyName("enum");
                        // writer.WriteValue(enumTyp.Name);
                        // writer.WritePropertyName("values");
                        // writer.WriteStartObject();

                        for (int i = 0; i < names.Length; i++)
                        {
                            EnumEntry entry = new();
                            var enm = types.GetValue(i);
                            var name = names[i];
                            entry.value = Convert.ToInt32(enm);
                            entry.deprecated = e.IsDeprecated(i);
                            entry.description = string.Empty;
                            if (enm.GetType().Name == "LogicType")
                            {
                                entry.description = LogicBase.GetLogicDescription((LogicType)enm);
                            }
                            else if (enm.GetType().Name == "LogicSlotType")
                            {
                                entry.description = LogicBase.GetLogicDescription(
                                    (LogicSlotType)enm
                                );
                            }
                            string key = name;
                            string[] parts = name.Split(new Char[] { '.' }, 2);
                            if (parts.Length > 1)
                            {
                                key = parts[1];
                            }
                            listing.values[key] = entry;
                        }

                        msgs.Add("Adding Enum " + seTyp.Name + "<" + enumTyp.Name + "> ...");
                        if (seTyp.Name.Contains("ScriptEnum"))
                        {
                            if (!enumsOutput.scriptEnums.ContainsKey(typeName))
                            {
                                enumsOutput.scriptEnums.Add(typeName, listing);
                            }
                            else
                            {
                                msgs.Add("[Warning] Duplicate script enum key: " + typeName);
                                enumsOutput.scriptEnums.Add(typeName + "_" + enumTyp.Name, listing);
                            }
                        }
                        else if (seTyp.Name.Contains("BasicEnum"))
                        {
                            if (!enumsOutput.basicEnums.ContainsKey(typeName))
                            {
                                enumsOutput.basicEnums.Add(typeName, listing);
                            }
                            else
                            {
                                msgs.Add("[Warning] Duplicate basic enum key: " + typeName);
                                enumsOutput.basicEnums.Add(typeName + "_" + enumTyp.Name, listing);
                            }
                        }
                        else
                        {
                            Debug.LogError((object)("Unknown ScriptEnum Type " + seTyp.Name));
                        }
                    }
                    JObject enumsObj = JObject.FromObject(enumsOutput);
                    enumsObj.WriteTo(writer);

                    // writer.WriteEnd();
                }
            }

            return String.Join("\n", msgs) + "\nFiles saved to " + out_path;
        }

    }
}

