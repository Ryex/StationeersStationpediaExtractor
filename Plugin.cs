using BepInEx;
using HarmonyLib;
using Assets.Scripts.Objects.Items;
using System.Collections.Generic;
using Assets.Scripts.Objects;
using System.Reflection.Emit;
using System;
using Assets.Scripts.Inventory;
using Util.Commands;
using Assets.Scripts.UI;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using System.Linq;
using Assets.Scripts.Objects.Motherboards;
using Assets.Scripts.Objects.Electrical;
using Assets.Scripts;
using Assets.Scripts.Objects.Entities;
using Assets.Scripts.Objects.Pipes;
using Assets.Scripts.Objects.Appliances;
using Assets.Scripts.Atmospherics;
using Objects.Items;
using Objects.Rockets;
using Reagents;
using Newtonsoft.Json.Linq;

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

    struct OutputStationpediaPage
    {
        public string Key;
        public string Title;
        public string Description;
        public string PrefabName;
        public int PrefabHash;
        public string BasePowerDraw;
        public string MaxPressure;
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

            Thing thing = Prefab.Find(PrefabName);
            Device device = thing as Device;
            DynamicThing dynamicthing = thing as DynamicThing;

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
                                writer.WritePropertyName(Enum.GetName(typeof(LogicSlotType), logicSlotType));
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
                    writer.WriteValue(NetworkHelper.GetBytesReadable((long)(memory.GetStackSize() * 8)));


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

            ITransmitable transmitable = thing as ITransmitable;
            if (transmitable != null)
            {
                writer.WritePropertyName("WirelessLogic");
                writer.WriteValue(true);
            }

            ITransmissionReceiver transmissionReceiver = thing as ITransmissionReceiver;
            if (transmissionReceiver != null)
            {
                writer.WritePropertyName("TransmissionReceiver");
                writer.WriteValue(true);
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
                DeviceInputOutputImportExportCircuit deviceCircuitIOIE = device as DeviceInputOutputImportExportCircuit;

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

                writer.WriteEndObject();
            }

            if (thing is Structure structure)
            {
                writer.WritePropertyName("Structure");
                writer.WriteStartObject();

                if (structure.BuildStates.Count > 0)
                {
                    writer.WritePropertyName("BuildStates");
                    writer.WriteStartArray();
                    foreach (BuildState buildState in structure.BuildStates)
                    {
                        writer.WriteStartObject();
                        if (buildState.Tool.ToolEntry is not null || buildState.Tool.ToolEntry2 is not null)
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
                    writer.WriteValue(Enum.GetName(typeof(Chemistry.GasType), gasFilter.FilterType));
                }

                if (!(!(bool)(UnityEngine.Object)dynamicthing || Stationpedia.CreatorItem == null || dynamicthing is Plant))
                {
                    List<RecipeReference> allMyCreators = ElectronicReader.GetAllMyCreators(dynamicthing);
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
                                if (dynamicthing.RecipeTier != MachineTier.Undefined && dynamicthing.RecipeTier != MachineTier.Max)
                                {
                                    writer.WritePropertyName("TierName");
                                    writer.WriteValue(dynamicthing.RecipeTier.ToString());
                                }

                                try
                                {
                                    JToken recipeToken = JToken.FromObject(reference.Recipe);
                                    if (recipeToken.Type == JTokenType.Object)
                                    {
                                        foreach (var prop in recipeToken.Children<JProperty>())
                                        {
                                            writer.WritePropertyName(prop.Name);
                                            if (prop.Value.Type == JTokenType.Object || prop.Value.Type == JTokenType.Array)
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
                                catch (FormatException ex)
                                {
                                    Debug.LogError((object)("There was an error with text " + Stationpedia.CreatorItem.Parsed + " " + ex.Message));
                                }
                                writer.WriteEndObject();

                            }
                        }
                        writer.WriteEndArray();
                    }
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

                        foreach (var reagent in Reagent.AllReagents)
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

                writer.WriteEnd();

            }

            writer.WriteEnd();
        }
    }

    class StationpediaExportCommand : CommandBase
    {
        public override string HelpText => "Export Stationpedia";

        public override string[] Arguments { get; } = new string[] { };

        public override bool IsLaunchCmd { get; }

        public override string Execute(string[] args)
        {
            string out_path = Path.Combine(Path.Combine(Application.dataPath, ".."), "Stationpedia");
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

                    writer.WritePropertyName("reagents");
                    writer.WriteStartObject();
                    foreach (var reagent in Reagent.AllReagentsSorted)
                    {
                        writer.WritePropertyName(reagent.TypeNameShort);
                        writer.WriteStartObject();
                        writer.WritePropertyName("Hash");
                        writer.WriteValue(reagent.Hash);
                        writer.WritePropertyName("Unit");
                        writer.WriteValue(reagent.Unit);
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
                    writer.WriteStartObject();

                    writer.WritePropertyName("LogicType");
                    writer.WriteStartObject();
                    foreach (LogicType i in EnumCollections.LogicTypes.Values)
                    {
                        var name = Enum.GetName(typeof(LogicType), i);
                        writer.WritePropertyName(name);
                        writer.WriteValue(i);
                    }
                    writer.WriteEnd();

                    writer.WritePropertyName("LogicSlotType");
                    writer.WriteStartObject();
                    foreach (LogicSlotType i in EnumCollections.LogicSlotTypes.Values)
                    {
                        var name = Enum.GetName(typeof(LogicSlotType), i);
                        writer.WritePropertyName(name);
                        writer.WriteValue(i);
                    }
                    writer.WriteEnd();

                    writer.WritePropertyName("LogicBatchMethod");
                    writer.WriteStartObject();
                    foreach (var i in Enum.GetValues(typeof(LogicBatchMethod)))
                    {
                        var name = Enum.GetName(typeof(LogicBatchMethod), i);
                        writer.WritePropertyName(name);
                        writer.WriteValue(i);
                    }
                    writer.WriteEnd();

                    writer.WritePropertyName("LogicReagentMode");
                    writer.WriteStartObject();
                    foreach (var i in Enum.GetValues(typeof(LogicReagentMode)))
                    {
                        var name = Enum.GetName(typeof(LogicReagentMode), i);
                        writer.WritePropertyName(name);
                        writer.WriteValue(i);
                    }
                    writer.WriteEnd();


                    writer.WritePropertyName("Enums");
                    writer.WriteStartObject();

                    foreach (var i in EnumCollections.LogicTypes.Values)
                    {
                        writer.WritePropertyName("LogicType." + Enum.GetName(typeof(LogicType), i));
                        writer.WriteValue(i);
                    }
                    foreach (var i in EnumCollections.LogicSlotTypes.Values)
                    {
                        writer.WritePropertyName("LogicSlotType." + Enum.GetName(typeof(LogicSlotType), i));
                        writer.WriteValue(i);
                    }
                    foreach (var i in Enum.GetValues(typeof(SoundAlert)))
                    {
                        writer.WritePropertyName("Sound." + Enum.GetName(typeof(SoundAlert), i));
                        writer.WriteValue(i);
                    }
                    foreach (var i in Enum.GetValues(typeof(LogicTransmitterMode)))
                    {
                        writer.WritePropertyName("TransmitterMode." + Enum.GetName(typeof(LogicTransmitterMode), i));
                        writer.WriteValue(i);
                    }
                    foreach (var i in Enum.GetValues(typeof(ElevatorMode)))
                    {
                        writer.WritePropertyName("ElevatorMode." + Enum.GetName(typeof(ElevatorMode), i));
                        writer.WriteValue(i);
                    }
                    foreach (var i in Enum.GetValues(typeof(ColorType)))
                    {
                        writer.WritePropertyName("Color." + Enum.GetName(typeof(ColorType), i));
                        writer.WriteValue(i);
                    }
                    foreach (var i in Enum.GetValues(typeof(EntityState)))
                    {
                        writer.WritePropertyName("EntityState." + Enum.GetName(typeof(EntityState), i));
                        writer.WriteValue(i);
                    }
                    foreach (var i in Enum.GetValues(typeof(AirControlMode)))
                    {
                        writer.WritePropertyName("AirControl." + Enum.GetName(typeof(AirControlMode), i));
                        writer.WriteValue(i);
                    }
                    foreach (var i in Enum.GetValues(typeof(DaylightSensor.DaylightSensorMode)))
                    {
                        writer.WritePropertyName("DaylightSensorMode." + Enum.GetName(typeof(DaylightSensor.DaylightSensorMode), i));
                        writer.WriteValue(i);
                    }
                    foreach (var i in Enum.GetValues(typeof(ConditionOperation)))
                    {
                        writer.WritePropertyName(Enum.GetName(typeof(ConditionOperation), i));
                        writer.WriteValue(i);
                    }
                    foreach (var i in Enum.GetValues(typeof(AirConditioningMode)))
                    {
                        writer.WritePropertyName("AirCon." + Enum.GetName(typeof(AirConditioningMode), i));
                        writer.WriteValue(i);
                    }
                    foreach (var i in Enum.GetValues(typeof(VentDirection)))
                    {
                        writer.WritePropertyName("Vent." + Enum.GetName(typeof(VentDirection), i));
                        writer.WriteValue(i);
                    }
                    foreach (var i in Enum.GetValues(typeof(PowerMode)))
                    {
                        writer.WritePropertyName("PowerMode." + Enum.GetName(typeof(PowerMode), i));
                        writer.WriteValue(i);
                    }
                    foreach (var i in Enum.GetValues(typeof(RobotMode)))
                    {
                        writer.WritePropertyName("RobotMode." + Enum.GetName(typeof(RobotMode), i));
                        writer.WriteValue(i);
                    }
                    foreach (var i in Enum.GetValues(typeof(SortingClass)))
                    {
                        writer.WritePropertyName("SortingClass." + Enum.GetName(typeof(SortingClass), i));
                        writer.WriteValue(i);
                    }
                    foreach (var i in Enum.GetValues(typeof(Slot.Class)))
                    {
                        writer.WritePropertyName("SlotClass." + Enum.GetName(typeof(Slot.Class), i));
                        writer.WriteValue(i);
                    }
                    foreach (var i in Enum.GetValues(typeof(Chemistry.GasType)))
                    {
                        writer.WritePropertyName("GasType." + Enum.GetName(typeof(Chemistry.GasType), i));
                        writer.WriteValue(i);
                    }
                    foreach (var i in Enum.GetValues(typeof(RocketMode)))
                    {
                        writer.WritePropertyName("RocketMode." + Enum.GetName(typeof(RocketMode), i));
                        writer.WriteValue(i);
                    }
                    foreach (var i in Enum.GetValues(typeof(ReEntryProfile)))
                    {
                        writer.WritePropertyName("ReEntryProfile." + Enum.GetName(typeof(ReEntryProfile), i));
                        writer.WriteValue(i);
                    }
                    foreach (var i in Enum.GetValues(typeof(SorterInstruction)))
                    {
                        writer.WritePropertyName("SorterInstruction." + Enum.GetName(typeof(SorterInstruction), i));
                        writer.WriteValue(i);
                    }

                    writer.WriteEnd();

                    writer.WriteEnd();


                }
            }

            return String.Join("\n", msgs) + "\nFiles saved to " + out_path;
        }
    }
}
