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
                                    if (recipeToken.Type == JTokenType.Object)
                                    {
                                        foreach (var prop in recipeToken.Children<JProperty>())
                                        {
                                            writer.WritePropertyName(prop.Name);
                                            if (
                                                prop.Value.Type == JTokenType.Object
                                                || prop.Value.Type == JTokenType.Array
                                            )
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
                            if (!enumsOutput.scriptEnums.ContainsKey(typeName)) {
                                enumsOutput.scriptEnums.Add(typeName, listing);
                            } else {
                                msgs.Add("[Warning] Duplicate script enum key: " + typeName);
                                enumsOutput.scriptEnums.Add(typeName + "_" + enumTyp.Name, listing);
                            }
                        }
                        else if (seTyp.Name.Contains("BasicEnum"))
                        {
                            if (!enumsOutput.basicEnums.ContainsKey(typeName)) {
                                enumsOutput.basicEnums.Add(typeName, listing);
                            } else {
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
