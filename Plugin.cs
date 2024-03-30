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
using Assets.Scripts.Atmospherics;

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
        public void writeToJson(JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("SlotName");
            writer.WriteValue(SlotName);
            writer.WritePropertyName("SlotType");
            writer.WriteValue(SlotType);
            writer.WritePropertyName("SlotIndex");
            writer.WriteValue(SlotIndex);
            writer.WriteEnd();
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
        public void writeToJson(JsonWriter writer)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("LogicName");
            writer.WriteValue(LogicName);
            writer.WritePropertyName("LogicAccessTypes");
            writer.WriteValue(LogicAccessTypes);
            writer.WriteEnd();
        }
    }

    struct OutputStationpediaPage
    {
        public string Key;
        public string Title;
        public string Description;
        public string PrefabName;
        public int PrefabHash;
        public List<OutputSlotsInset> SlotInserts;
        public List<OutputLogicInsert> LogicInsert;
        public List<OutputLogicInsert> LogicSlotInsert;
        public List<OutputLogicInsert> ModeInsert;
        public List<OutputLogicInsert> ConnectionInsert;
        public void setFromPage(StationpediaPage page)
        {
            Key = page.Key;
            Title = page.Title;
            Description = page.Description;
            PrefabName = page.PrefabName;
            PrefabHash = page.PrefabHash;
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
        }
        public void writeToJson(JsonWriter writer)
        {
            writer.WriteStartObject();

            writer.WritePropertyName("Key");
            writer.WriteValue(Key);
            writer.WritePropertyName("Title");
            writer.WriteValue(Title);
            writer.WritePropertyName("Description");
            writer.WriteValue(Description);
            writer.WritePropertyName("PrefabName");
            writer.WriteValue(PrefabName);
            writer.WritePropertyName("PrefabHash");
            writer.WriteValue(PrefabHash);
            writer.WritePropertyName("SlotInserts");
            writer.WriteStartArray();
            foreach (var si in SlotInserts)
            {
                si.writeToJson(writer);
            }
            writer.WriteEnd();
            writer.WritePropertyName("LogicInsert");
            writer.WriteStartArray();
            foreach (var li in LogicInsert)
            {
                li.writeToJson(writer);
            }
            writer.WriteEnd();
            writer.WritePropertyName("LogicSlotInsert");
            writer.WriteStartArray();
            foreach (var sli in LogicSlotInsert)
            {
                sli.writeToJson(writer);
            }
            writer.WriteEnd();
            writer.WritePropertyName("ModeInsert");
            writer.WriteStartArray();
            foreach (var mi in ModeInsert)
            {
                mi.writeToJson(writer);
            }
            writer.WriteEnd();
            writer.WritePropertyName("ConnectionInsert");
            writer.WriteStartArray();
            foreach (var ci in ConnectionInsert)
            {
                ci.writeToJson(writer);
            }
            writer.WriteEnd();

            writer.WritePropertyName("ConnectionList");
            writer.WriteStartArray();
            {
                Thing thing = Prefab.Find(PrefabName);
                Device device = thing as Device;
                if (device)
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

            }
            writer.WriteEnd();

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
                    foreach (var i in Enum.GetValues(typeof(LogicType)))
                    {
                        var name = Enum.GetName(typeof(LogicType), i);
                        writer.WritePropertyName(name);
                        writer.WriteValue(i);
                    }
                    writer.WriteEnd();

                    writer.WritePropertyName("LogicSlotType");
                    writer.WriteStartObject();
                    foreach (var i in Enum.GetValues(typeof(LogicSlotType)))
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

                    foreach (var i in Enum.GetValues(typeof(LogicType)))
                    {
                        writer.WritePropertyName("LogicType." + Enum.GetName(typeof(LogicType), i));
                        writer.WriteValue(i);
                    }
                    foreach (var i in Enum.GetValues(typeof(LogicSlotType)))
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

                    writer.WriteEnd();

                    writer.WriteEnd();


                }
            }

            return String.Join("\n", msgs) + "\nFiles saved to " + out_path;
        }
    }
}
