﻿using SatisfactorySaveEditor.Model;
using SatisfactorySaveEditor.ViewModel.Property;
using SatisfactorySaveEditor.ViewModel.Struct;
using SatisfactorySaveParser;
using SatisfactorySaveParser.PropertyTypes;
using SatisfactorySaveParser.PropertyTypes.Structs;
using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Windows;

namespace SatisfactorySaveEditor.Cheats
{
    public class DeleteEnemiesCheat : ICheat
    {
        public string Name => "Delete enemies";

        private int currentDoggoID = 0;

        private SaveObjectModel FindOrCreatePath(SaveObjectModel start, string[] path, int index = 0)
        {
            if (index == path.Length)
                return start;
            if (start.FindChild(path[index], false) == null)
                start.Items.Add(new SaveObjectModel(path[index]));
            return FindOrCreatePath(start.FindChild(path[index], false), path, index + 1);
        }
        
        private int GetNextDoggoID(int currentId, SaveObjectModel rootItem)
        {
            while (rootItem.FindChild($"Persistent_Level:PersistentLevel.Char_SpaceRabbit_C_{currentId}", false) != null)
                currentId++;
            return currentId;
        }

        public byte[] PrepareForParse(string itemPath, int itemAmount)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(ms))
                {
                    writer.WriteLengthPrefixedString("mInventoryStacks");
                    writer.WriteLengthPrefixedString("StructProperty");
                    writer.Write("Item".GetSerializedLength() + "StructProperty".GetSerializedLength() + 4 + 4 + "InventoryItem".GetSerializedLength() + 4 + 4 + 4 + 4 + 1 + 4 + itemPath.GetSerializedLength() + "".GetSerializedLength() + "".GetSerializedLength() + "NumItems".GetSerializedLength() + "IntProperty".GetSerializedLength() + 4 + 4 + 1 + 4 + "None".GetSerializedLength()); // TODO
                    writer.Write(0);
                    writer.WriteLengthPrefixedString("InventoryStack");
                    writer.Write(0);
                    writer.Write(0);
                    writer.Write(0);
                    writer.Write(0);
                    writer.Write((byte)0);
                    writer.WriteLengthPrefixedString("Item");
                    writer.WriteLengthPrefixedString("StructProperty");
                    writer.Write(4 + itemPath.GetSerializedLength() + "".GetSerializedLength() + "".GetSerializedLength());
                    writer.Write(0);
                    writer.WriteLengthPrefixedString("InventoryItem"); // ItemType
                    writer.Write(0);
                    writer.Write(0);
                    writer.Write(0);
                    writer.Write(0);
                    writer.Write((byte)0);
                    writer.Write(0); // Unknown1
                    writer.WriteLengthPrefixedString(itemPath); // ItemType
                    writer.WriteLengthPrefixedString(""); // Unknown2
                    writer.WriteLengthPrefixedString(""); // Unknown3
                    writer.WriteLengthPrefixedString("NumItems");
                    writer.WriteLengthPrefixedString("IntProperty");
                    writer.Write(4); // Length
                    writer.Write(0); // Index
                    writer.Write((byte)0);
                    writer.Write(itemAmount); // Value
                    writer.WriteLengthPrefixedString("None");

                }
                return ms.ToArray();
            }
        }

        private void AddDoggo(SaveObjectModel rootItem)
        {
            currentDoggoID = GetNextDoggoID(currentDoggoID, rootItem);
            var player = (SaveEntityModel)rootItem.FindChild("Char_Player.Char_Player_C", false).Items[0];
            SaveComponent healthComponent = new SaveComponent("/Script/FactoryGame.FGHealthComponent", "Persistent_Level", $"Persistent_Level:PersistentLevel.Char_SpaceRabbit_C_{currentDoggoID}.HealthComponent")
            {
                DataFields = new SerializedFields(),
                ParentEntityName = $"Persistent_Level:PersistentLevel.Char_SpaceRabbit_C_{currentDoggoID}"
            };
            byte[] bytes = PrepareForParse("", 0);
            SaveComponent inventoryComponent;
            using (BinaryReader reader = new BinaryReader(new MemoryStream(bytes)))
            {
                inventoryComponent = new SaveComponent("/Script/FactoryGame.FGInventoryComponent", "Persistent_Level", $"Persistent_Level:PersistentLevel.Char_SpaceRabbit_C_{currentDoggoID}.mInventory")
                {
                    DataFields = new SerializedFields()
                    {
                        new ArrayProperty("mInventoryStacks")
                        {
                            Type = StructProperty.TypeName,
                            Elements = new System.Collections.Generic.List<SerializedProperty>()
                            {
                                SerializedProperty.Parse(reader)
                            }
                        },
                        new ArrayProperty("mArbitrarySlotSizes")
                        {
                            Type = IntProperty.TypeName,
                            Elements = new System.Collections.Generic.List<SerializedProperty>()
                            {
                                new IntProperty("Element") { Value = 0 }
                            }
                        },
                        new ArrayProperty("mAllowedItemDescriptors")
                        {
                            Type = ObjectProperty.TypeName,
                            Elements = new System.Collections.Generic.List<SerializedProperty>()
                            {
                                new ObjectProperty("Element") { LevelName = "", PathName = "" }
                            }
                        }
                    },
                    ParentEntityName = $"Persistent_Level:PersistentLevel.Char_SpaceRabbit_C_{currentDoggoID}"
                };
            }
            SaveEntity doggo = new SaveEntity("/Game/FactoryGame/Character/Creature/Wildlife/SpaceRabbit/Char_SpaceRabbit.Char_SpaceRabbit_C", "Persistent_Level", $"Persistent_Level:PersistentLevel.Char_SpaceRabbit_C_{currentDoggoID}")
            {
                NeedTransform = true,
                Rotation = ((SaveEntity)player.Model).Rotation,
                Position = new Vector3()
                {
                    X = ((SaveEntity)player.Model).Position.X,
                    Y = ((SaveEntity)player.Model).Position.Y + 100 + 10 * currentDoggoID, // so they don't glitch one into another like the tractors did
                    Z = ((SaveEntity)player.Model).Position.Z + 10
                },
                Scale = new Vector3() { X = 1, Y = 1, Z = 1 },
                WasPlacedInLevel = false,
                ParentObjectName = "",
                ParentObjectRoot = ""
            };
            doggo.Components = new System.Collections.Generic.List<SatisfactorySaveParser.Structures.ObjectReference>()
            {
                new SatisfactorySaveParser.Structures.ObjectReference() {LevelName = "Persistent_Level", PathName = $"Persistent_Level:PersistentLevel.Char_SpaceRabbit_C_{currentDoggoID}.mInventory"},
                new SatisfactorySaveParser.Structures.ObjectReference() {LevelName = "Persistent_Level", PathName = $"Persistent_Level:PersistentLevel.Char_SpaceRabbit_C_{currentDoggoID}.HealthComponent"}
            };
            byte[] emptyDynamicStructData = { 0x05, 0x00, 0x00, 0x00, 0x4e, 0x6f, 0x6e, 0x65 }; // Length prefixed "None"
            using (BinaryReader binaryReader = new BinaryReader(new MemoryStream(emptyDynamicStructData)))
            {
                doggo.DataFields = new SerializedFields()
                {
                    new ObjectProperty("mFriendActor") { LevelName = "Persistent_Level", PathName = player.Title },
                    new IntProperty("mLootTableIndex") { Value = 0 },
                    new StructProperty("mLootTimerHandle")
                    {
                        Data = new DynamicStructData(binaryReader, "TimerHandle"),
                        Unk1 = 0,
                        Unk2 = 0,
                        Unk3 = 0,
                        Unk4 = 0,
                        Unk5 = 0
                    },
                    new BoolProperty("mIsPersistent") { Value = true },
                    new ObjectProperty("mHealthComponent") { LevelName = "Persistent_Level", PathName = $"Persistent_Level:PersistentLevel.Char_SpaceRabbit_C_{currentDoggoID}.HealthComponent" }
                };
            }
            FindOrCreatePath(rootItem, new string[] { "Character", "Creature", "Wildlife", "SpaceRabbit", "Char_SpaceRabbit.Char_SpaceRabbit_C" }).Items.Add(new SaveEntityModel(doggo));
            rootItem.FindChild("FactoryGame.FGInventoryComponent", false).Items.Add(new SaveComponentModel(inventoryComponent));
            rootItem.FindChild("FactoryGame.FGHealthComponent", false).Items.Add(new SaveComponentModel(healthComponent));
        }

        public bool Apply(SaveObjectModel rootItem)
        {
            var animalSpawners = rootItem.FindChild("BP_CreatureSpawner.BP_CreatureSpawner_C", false);
            if (animalSpawners == null)
            {
                MessageBox.Show("This save does not contain animals or it is corrupt.", "Cannot find animals", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            foreach (SaveObjectModel animalSpawner in animalSpawners.DescendantSelfViewModel)
            {
                // Some crab hatchers are marked as CreatureSpawner instead of EnemySpawner and there is no other trace of the difference between enemy and friendly in the savefile
                //if (animalSpawner.Title.ToLower().Contains("enemy"))
                //{
                animalSpawner.FindField("mSpawnData", (ArrayPropertyViewModel arrayProperty) =>
                {
                    foreach (StructPropertyViewModel elem in arrayProperty.Elements)
                    {
                        // Set WasKilled to true so they don't respawn after deleting them
                        ((BoolPropertyViewModel)((DynamicStructDataViewModel)elem.StructData).Fields[2]).Value = true;
                        // Set KilledOnDayNumber to a huge number (some far away animals respawn if the number is too small)
                        ((IntPropertyViewModel)((DynamicStructDataViewModel)elem.StructData).Fields[3]).Value = 1000000000;
                    }
                });
                //}
            }

            // Delete the already spawned enemies
            var enemies = rootItem.FindChild("Creature", false).FindChild("Enemy", false);
            rootItem.Remove(enemies);


            if (MessageBox.Show($"Deleted all spawned enemies, and all unspawned creatures (enemy & friendly). Would you like 3 tamed Lizzard Doggos as a compensation?", "Success", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                for(int i = 0; i < 3; i++)
                    AddDoggo(rootItem);
            }
            return true;
        }
    }
}
