// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System.IO;
using System.IO.Compression;
using System.Xml;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Globalization;

namespace LEGOModelImporter
{

    public static class PartUtility
    {
        public static readonly string designIdMappingPath = "Packages/com.unity.lego.modelimporter/Data/designid.xml";
        public static readonly string legacyPartsPath = "Packages/com.unity.lego.modelimporter/Data/LegacyParts.zip";
        public static readonly string newPartsPath = "Packages/com.unity.lego.modelimporter/Data/NewParts.zip";
        public static readonly string commonPartsPath = "Packages/com.unity.lego.modelimporter/Data/CommonParts.zip";
        public static readonly string geometryPath = "Assets/LEGO Data/Geometry";
        public static readonly string collidersPath = "Assets/LEGO Data/Colliders";
        public static readonly string connectivityPath = "Assets/LEGO Data/Connectivity";
        public static readonly string dataRootPath = "Assets/LEGO Data";
        public static readonly string newDir = "New";
        public static readonly string legacyDir = "Legacy";
        public static readonly string commonPartsDir = "CommonParts";
        public static readonly string lightmappedDir = "Lightmapped";
        public static readonly string lod0Dir = "LOD0";
        public static readonly string lod1Dir = "LOD1";
        public static readonly string lod2Dir = "LOD2";
        static Dictionary<string, List<string>> designIdMapping;
        static ZipArchive legacyPartsZipArchive;
        static ZipArchive newPartsZipArchive;
        static ZipArchive commonPartsZipArchive;

        public enum PartExistence
        {
            None,
            Legacy,
            New
        }

        public class PartExistenceResult
        {
            public PartExistence existence;
            public string designID;
        };

        public static void RefreshDB()
        {
            if (legacyPartsZipArchive != null)
            {
                legacyPartsZipArchive.Dispose();
                legacyPartsZipArchive = null;
            }

            if (newPartsZipArchive != null)
            {
                newPartsZipArchive.Dispose();
                newPartsZipArchive = null;
            }

            if (commonPartsZipArchive != null)
            {
                commonPartsZipArchive.Dispose();
                commonPartsZipArchive = null;
            }

            designIdMapping = null;

            OpenDB();
        }

        static void OpenDB()
        {
            if (legacyPartsZipArchive == null)
            {
                legacyPartsZipArchive = ZipFile.OpenRead(legacyPartsPath);
            }

            if (newPartsZipArchive == null)
            {
                newPartsZipArchive = ZipFile.OpenRead(newPartsPath);
            }

            if (commonPartsZipArchive == null)
            {
                commonPartsZipArchive = ZipFile.OpenRead(commonPartsPath);
            }

            if (designIdMapping == null)
            {
                designIdMapping = new Dictionary<string, List<string>>();

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(File.ReadAllText(designIdMappingPath));

                var root = xmlDoc.DocumentElement;
                var partNodes = root.SelectNodes("Part");

                foreach (XmlNode partNode in partNodes)
                {
                    var designID = partNode.Attributes["designID"].Value;
                    var alternateDesignIDs = partNode.Attributes["alternateDesignIDs"].Value.Split(',');

                    designIdMapping.Add(designID, new List<string>());

                    foreach (var alternateDesignID in alternateDesignIDs)
                    {
                        designIdMapping[designID].Add(alternateDesignID.Trim());
                    }
                }
            }
        }

        public static PartExistenceResult CheckIfPartExists(string designID)
        {
            if (CheckIfNewPartExists(designID))
            {
                return new PartExistenceResult()
                {
                    existence = PartExistence.New,
                    designID = designID
                };
            }

            OpenDB();

            // Look for alternate design ids.
            if (designIdMapping.ContainsKey(designID))
            {
                var alternateDesignIDs = designIdMapping[designID];
                foreach (var alternateDesignID in alternateDesignIDs)
                {
                    if (CheckIfNewPartExists(alternateDesignID))
                    {
                        return new PartExistenceResult()
                        {
                            existence = PartExistence.New,
                            designID = alternateDesignID
                        };
                    }
                }
            }

            if (CheckIfLegacyPartExists(designID))
            {
                return new PartExistenceResult()
                {
                    existence = PartExistence.Legacy,
                    designID = designID
                };
            }

            // Look for alternate design ids.
            if (designIdMapping.ContainsKey(designID))
            {
                var alternateDesignIDs = designIdMapping[designID];
                foreach (var alternateDesignID in alternateDesignIDs)
                {
                    if (CheckIfLegacyPartExists(alternateDesignID))
                    {
                        return new PartExistenceResult()
                        {
                            existence = PartExistence.Legacy,
                            designID = alternateDesignID
                        };
                    }
                }
            }

            return new PartExistenceResult()
            {
                existence = PartExistence.None,
                designID = designID
            };
        }

        static bool CheckIfNewPartExists(string designID)
        {
            if (File.Exists(Path.Combine(geometryPath, designID + ".fbx")) || File.Exists(Path.Combine(geometryPath, lightmappedDir, designID + ".fbx")))
            {
                return true;
            }

            OpenDB();

            var entry = newPartsZipArchive.GetEntry("Geometry/VX" + designID.PadLeft(7, '0') + "/m" + designID + ".fbx");
            if (entry != null)
            {
                return true;
            }

            return false;
        }

        static bool CheckIfLegacyPartExists(string designID)
        {
            if (File.Exists(Path.Combine(geometryPath, legacyDir, designID + ".fbx")) || File.Exists(Path.Combine(geometryPath, legacyDir, lightmappedDir, designID + ".fbx")))
            {
                return true;
            }

            OpenDB();

            var entry = legacyPartsZipArchive.GetEntry(designID + ".fbx");
            if (entry != null)
            {
                return true;
            }

            return false;
        }

        public static PartExistenceResult UnpackPart(string designID, bool lightmapped, int lod, bool forceUnpack = false)
        {
            // Look at new parts first.
            var newPartExistenceResult = UnpackNewPart(designID, lightmapped, lod, forceUnpack);
            if (newPartExistenceResult.existence != PartExistence.None)
            {
                return newPartExistenceResult;
            }

            // Then look at legacy parts.
            var legacyPartExistenceResult = UnpackLegacyPart(designID, lightmapped, lod);
            if (legacyPartExistenceResult.existence != PartExistence.None)
            {
                return legacyPartExistenceResult;
            }

            // We did not find neither new nor legacy parts.
            return new PartExistenceResult()
            {
                existence = PartExistence.None,
                designID = designID
            };
        }

        static PartExistenceResult UnpackNewPart(string designID, bool lightmapped, int lod, bool forceUnpack = false)
        {
            if (UnpackExactNewPart(designID, lightmapped, lod, forceUnpack))
            {
                return new PartExistenceResult()
                {
                    existence = PartExistence.New,
                    designID = designID
                };
            }

            OpenDB();

            // Look for alternate design ids.
            if (designIdMapping.ContainsKey(designID))
            {
                var alternateDesignIDs = designIdMapping[designID];
                foreach (var alternateDesignID in alternateDesignIDs)
                {
                    if (UnpackExactNewPart(alternateDesignID, lightmapped, lod, forceUnpack))
                    {
                        return new PartExistenceResult()
                        {
                            existence = PartExistence.New,
                            designID = alternateDesignID
                        };
                    }
                }
            }

            return new PartExistenceResult()
            {
                existence = PartExistence.None,
                designID = designID
            };
        }

        static bool UnpackExactNewPart(string designID, bool lightmapped, int lod, bool forceUnpack)
        {
            string lodDir = lod == 0 ? lod0Dir : lod1Dir; // Currently, there is no LOD2 for parts.

            if (!forceUnpack && File.Exists(Path.Combine(geometryPath, newDir, lightmapped ? lightmappedDir : "", lodDir, designID + ".fbx")))
            {
                return true;
            }

            OpenDB();

            var path = "Geometry/VX" + designID.PadLeft(7, '0') + "/m" + designID + ".fbx";
            var entry = newPartsZipArchive.GetEntry(path);
            if (entry != null)
            {
                var fileName = Path.GetFileName(entry.Name).Substring(1);
                var filePath = Path.Combine(geometryPath, newDir, lightmapped ? lightmappedDir : "", lodDir, fileName);

                var directoryName = Path.GetDirectoryName(filePath);
                if (directoryName.Length > 0)
                {
                    Directory.CreateDirectory(directoryName);
                }

                var zipStream = entry.Open();
                var fileStream = File.Create(filePath);
                zipStream.CopyTo(fileStream);
                fileStream.Dispose();
                zipStream.Dispose();
#if UNITY_EDITOR
                AssetDatabase.ImportAsset(filePath);
#endif
                return true;
            }

            return false;
        }

        static PartExistenceResult UnpackLegacyPart(string designID, bool lightmapped, int lod)
        {
            if (UnpackExactLegacyPart(designID, lightmapped, lod))
            {
                return new PartExistenceResult()
                {
                    existence = PartExistence.Legacy,
                    designID = designID
                };
            }

            OpenDB();

            // Look for alternate design ids.
            if (designIdMapping.ContainsKey(designID))
            {
                var alternateDesignIDs = designIdMapping[designID];
                foreach (var alternateDesignID in alternateDesignIDs)
                {
                    if (UnpackExactLegacyPart(alternateDesignID, lightmapped, lod))
                    {
                        return new PartExistenceResult()
                        {
                            existence = PartExistence.Legacy,
                            designID = alternateDesignID
                        };
                    }
                }
            }

            return new PartExistenceResult()
            {
                existence = PartExistence.None,
                designID = designID
            };
        }

        static bool UnpackExactLegacyPart(string designID, bool lightmapped, int lod)
        {
            string lodDir = lod == 0 ? lod0Dir : lod1Dir; // Currently, there is no LOD2 for parts.

            if (File.Exists(Path.Combine(geometryPath, legacyDir, lightmapped ? lightmappedDir : "", lodDir, designID + ".fbx")))
            {
                return true;
            }

            OpenDB();

            var entry = legacyPartsZipArchive.GetEntry(designID + ".fbx");
            if (entry != null)
            {
                var fileName = entry.Name;
                var filePath = Path.Combine(geometryPath, legacyDir, lightmapped ? lightmappedDir : "", lodDir, fileName);

                var directoryName = Path.GetDirectoryName(filePath);
                if (directoryName.Length > 0)
                {
                    Directory.CreateDirectory(directoryName);
                }

                var zipStream = entry.Open();
                var fileStream = File.Create(filePath);
                zipStream.CopyTo(fileStream);
                fileStream.Dispose();
                zipStream.Dispose();
#if UNITY_EDITOR
                AssetDatabase.ImportAsset(filePath);
#endif
                return true;
            }

            return false;
        }

        public static bool UnpackCommonPart(string name, bool lightmapped, int lod, bool forceUnpack = false)
        {
            string lodDir = lod == 0 ? lod0Dir : lod == 1 ? lod1Dir : lod2Dir;
            string extension = lod <= 1 ? ".fbx" : ".prefab";

            if (!forceUnpack && File.Exists(Path.Combine(geometryPath, commonPartsDir, lightmapped ? lightmappedDir : "", lodDir, name + extension)))
            {
                return true;
            }

            if (lod <= 1)
            {
                OpenDB();

                var entry = commonPartsZipArchive.GetEntry(name + ".fbx");
                if (entry != null)
                {
                    var fileName = entry.Name;
                    var filePath = Path.Combine(geometryPath, commonPartsDir, lightmapped ? lightmappedDir : "", lodDir, fileName);

                    var directoryName = Path.GetDirectoryName(filePath);
                    if (directoryName.Length > 0)
                    {
                        Directory.CreateDirectory(directoryName);
                    }

                    var zipStream = entry.Open();
                    var fileStream = File.Create(filePath);
                    zipStream.CopyTo(fileStream);
                    fileStream.Dispose();
                    zipStream.Dispose();
#if UNITY_EDITOR
                    AssetDatabase.ImportAsset(filePath);
#endif
                    return true;
                }
            }
            else
            {
                // Generate LOD 2.
                var knob = name.StartsWith("knob");
                var tube = name.StartsWith("tube");
                var hollow = knob ? name.EndsWith("C") : tube;
                var tubeOrPinHeight = name.Contains("_01_") ? 0.21f : name.Contains("_02_") ? 0.85f : 2.73f;
                var height = knob ? 0.178f : tubeOrPinHeight;
                var radius = knob ? 0.25f : tube ? 0.3377f : 0.1575f;
                CreateCommonPartLod2(name, height, radius, knob, hollow, radius - 0.08f, lightmapped);

                return true;
            }

            return false;
        }

        public static bool CheckIfConnectivityForPartIsUnpacked(string designID)
        {
            return File.Exists(Path.Combine(connectivityPath, designID + ".prefab"));
        }

        public static bool CheckIfColliderForPartIsUnpacked(string designID)
        {
            return File.Exists(Path.Combine(collidersPath, designID + ".prefab"));
        }

        public static bool UnpackConnectivityForPart(string designID, bool forceUpdate = false)
        {
            if (File.Exists(Path.Combine(connectivityPath, designID + ".prefab")) && !forceUpdate)
            {
                return true;
            }

            OpenDB();

            var partConnectivityPath = "CollisionBox_Connectivity_Info/" + designID + ".xml";
            var entry = newPartsZipArchive.GetEntry(partConnectivityPath);
            if (entry != null)
            {
                var zipStream = entry.Open();

                var doc = new XmlDocument();
                doc.Load(zipStream);

                var primitiveNode = doc.SelectSingleNode("LEGOPrimitive");
                var connectivityNode = primitiveNode.SelectSingleNode("Connectivity");
                if (connectivityNode != null)
                {
                    var planarFields = connectivityNode.SelectNodes("Custom2DField");
                    var axleFields = connectivityNode.SelectNodes("Axel"); // Typo in Connectivity description files
                    var fixedFields = connectivityNode.SelectNodes("Fixed");

                    var connectivityGO = new GameObject("Connectivity");
                    var connectivityComponent = connectivityGO.AddComponent<Connectivity>();

                    var bounding = primitiveNode.SelectSingleNode("Bounding");
                    var aabb = bounding.SelectSingleNode("AABB");

                    var extents = new Bounds
                    {
                        min = new Vector3
                        {
                            x = -float.Parse(aabb.Attributes["minX"].Value, CultureInfo.InvariantCulture),
                            y = float.Parse(aabb.Attributes["minY"].Value, CultureInfo.InvariantCulture),
                            z = float.Parse(aabb.Attributes["minZ"].Value, CultureInfo.InvariantCulture)
                        },

                        max = new Vector3
                        {
                            x = -float.Parse(aabb.Attributes["maxX"].Value, CultureInfo.InvariantCulture),
                            y = float.Parse(aabb.Attributes["maxY"].Value, CultureInfo.InvariantCulture),
                            z = float.Parse(aabb.Attributes["maxZ"].Value, CultureInfo.InvariantCulture)
                        }
                    };

                    connectivityComponent.extents = extents;
                    connectivityComponent.version = AssetVersionChecker.currentVersion;

                    foreach (XmlNode connectionField in planarFields)
                    {
                        var value = connectionField.InnerText;

                        var fieldGO = new GameObject("Custom2DField");
                        var fieldComponent = fieldGO.AddComponent<PlanarField>();

                        fieldComponent.connectivity = connectivityComponent;

                        fieldComponent.kind = int.Parse(connectionField.Attributes["type"].Value, CultureInfo.InvariantCulture) % 2 == 0 ? ConnectionField.FieldKind.receptor : ConnectionField.FieldKind.connector;

                        fieldComponent.gridSize = new Vector2Int
                        {
                            x = int.Parse(connectionField.Attributes["width"].Value, CultureInfo.InvariantCulture),
                            y = int.Parse(connectionField.Attributes["height"].Value, CultureInfo.InvariantCulture)
                        };

                        var position = new Vector3
                        {
                            x = -float.Parse(connectionField.Attributes["tx"].Value, CultureInfo.InvariantCulture),
                            y = float.Parse(connectionField.Attributes["ty"].Value, CultureInfo.InvariantCulture),
                            z = float.Parse(connectionField.Attributes["tz"].Value, CultureInfo.InvariantCulture)
                        };
                        fieldComponent.transform.localPosition = position;

                        var angle = float.Parse(connectionField.Attributes["angle"].Value, CultureInfo.InvariantCulture);

                        var rotation = Quaternion.AngleAxis(
                            -angle,
                            new Vector3(
                                -float.Parse(connectionField.Attributes["ax"].Value, CultureInfo.InvariantCulture),
                                float.Parse(connectionField.Attributes["ay"].Value, CultureInfo.InvariantCulture),
                                float.Parse(connectionField.Attributes["az"].Value, CultureInfo.InvariantCulture)
                            )
                        );
                        
                        fieldComponent.transform.localRotation = rotation;
                        var connections = new List<PlanarFeature>();

                        var connectionDescriptions = value.Split(',');

                        var connectionPositionX = 0.0f;
                        var connectionPositionZ = 0.0f;

                        var featuresPerRow = fieldComponent.gridSize.x + 1;
                        var features = 0;
                        
                        var layer = LayerMask.NameToLayer(ConnectionField.GetLayer(fieldComponent.kind));

                        if(layer != -1)
                        {
                            fieldGO.layer = layer;
                        }

                        var collider = fieldGO.AddComponent<BoxCollider>();
                        collider.isTrigger = true;
                        collider.size = new Vector3((fieldComponent.gridSize.x + 1) * BrickBuildingUtility.LU_5, 0.0f, (fieldComponent.gridSize.y + 1) * BrickBuildingUtility.LU_5);
                        collider.center = new Vector3((collider.size.x - BrickBuildingUtility.LU_5) * -0.5f, 0.0f, (collider.size.z - BrickBuildingUtility.LU_5) * 0.5f);

                        // Build the connection field grid
                        foreach (var description in connectionDescriptions)
                        {
                            var descs = description.Split(':');
                            if (descs.Length > 3 || descs.Length < 2)
                            {
                                Debug.LogError("Connectivity description has wrong format -> " + designID);
                                continue;
                            }

                            var connectionType = (PlanarFeature.PlanarConnectionType)int.Parse(descs[0], CultureInfo.InvariantCulture);
                            var connection = new PlanarFeature();

                            connection.quadrants = int.Parse(descs[1], CultureInfo.InvariantCulture);

                            connection.connectionType = connectionType;

                            connection.flags = descs.Length == 3 ? (PlanarFeature.Flags)int.Parse(descs[2], CultureInfo.InvariantCulture) : 0;
                            connection.field = fieldComponent;

                            connections.Add(connection);
                            connection.index = connections.Count - 1;

                            if(connection.IsConnectableType())
                            {
                                fieldComponent.connectableConnections++;
                            }
                            
                            var x = features % featuresPerRow;
                            var y = features / featuresPerRow;

                            connectionPositionX -= BrickBuildingUtility.LU_5;
                            features++;

                            if (features % featuresPerRow == 0)
                            {
                                connectionPositionZ += BrickBuildingUtility.LU_5;
                                connectionPositionX = 0.0f;
                            }
                        }

                        // Only add the field if it has any connections.
                        if (connections.Count > 0)
                        {
                            connectivityComponent.planarFields.Add(fieldComponent);
                            fieldGO.transform.parent = connectivityGO.transform;
                            fieldComponent.connections = connections.ToArray();
                            fieldComponent.connectedTo = new PlanarField.ConnectionTuple[connections.Count];
                        }
                        else
                        {
                            Object.DestroyImmediate(fieldGO);
                        }
                    }

                    foreach(XmlNode axleField in axleFields)
                    {
                        var value = axleField.InnerText;

                        var fieldGO = new GameObject("AxleField");
                        var fieldComponent = fieldGO.AddComponent<AxleField>();

                        fieldComponent.connectivity = connectivityComponent;

                        var type = int.Parse(axleField.Attributes["type"].Value, CultureInfo.InvariantCulture);
                        fieldComponent.kind = type % 2 == 0 ? ConnectionField.FieldKind.receptor : ConnectionField.FieldKind.connector;

                        var feature = new AxleFeature();
                        feature.axleType = (AxleFeature.AxelType)type;
                        feature.field = fieldComponent;
                        fieldComponent.feature = feature;

                        fieldComponent.name = "AxleField " + ((AxleFeature.AxelType)type).ToString();

                        var length = float.Parse(axleField.Attributes["length"].Value, CultureInfo.InvariantCulture);
                        fieldComponent.length = length;

                        var requireGrabbingNode = axleField.Attributes["requireGrabbing"];
                        if(requireGrabbingNode != null)
                        {
                            fieldComponent.requireGrabbing = int.Parse(requireGrabbingNode.Value, CultureInfo.InvariantCulture) == 1;
                        }

                        var startCappedNode = axleField.Attributes["startCapped"];
                        if(startCappedNode != null)
                        {
                            fieldComponent.startCapped = int.Parse(startCappedNode.Value, CultureInfo.InvariantCulture) == 1;
                        }

                        var endCappedNode = axleField.Attributes["endCapped"];
                        if(endCappedNode != null)
                        {
                            fieldComponent.endCapped = int.Parse(endCappedNode.Value, CultureInfo.InvariantCulture) == 1;
                        }

                        var grabbingNode = axleField.Attributes["grabbing"];
                        if(grabbingNode != null)
                        {
                            fieldComponent.grabbing = int.Parse(grabbingNode.Value, CultureInfo.InvariantCulture) == 1;
                        }

                        var angle = float.Parse(axleField.Attributes["angle"].Value, CultureInfo.InvariantCulture);

                            var position = new Vector3
                        {
                            x = -float.Parse(axleField.Attributes["tx"].Value, CultureInfo.InvariantCulture),
                            y = float.Parse(axleField.Attributes["ty"].Value, CultureInfo.InvariantCulture),
                            z = float.Parse(axleField.Attributes["tz"].Value, CultureInfo.InvariantCulture)
                        };
                        fieldComponent.transform.localPosition = position;

                        var rotation = Quaternion.AngleAxis(
                            -angle,
                            new Vector3(
                                -float.Parse(axleField.Attributes["ax"].Value, CultureInfo.InvariantCulture),
                                float.Parse(axleField.Attributes["ay"].Value, CultureInfo.InvariantCulture),
                                float.Parse(axleField.Attributes["az"].Value, CultureInfo.InvariantCulture)
                            )
                        );
                        fieldComponent.transform.localRotation = rotation;

                        var layer = LayerMask.NameToLayer(ConnectionField.GetLayer(fieldComponent.kind));

                        if(layer != -1)
                        {
                            fieldGO.layer = layer;
                        }

                        var collider = fieldGO.AddComponent<CapsuleCollider>();
                        collider.isTrigger = true;
                        collider.center = Vector3.up * fieldComponent.length * 0.5f;
                        collider.height = fieldComponent.length;
                        collider.radius = BrickBuildingUtility.LU_1;

                        connectivityComponent.axleFields.Add(fieldComponent);
                        fieldGO.transform.parent = connectivityGO.transform;
                    }

                    foreach(XmlNode fixedField in fixedFields)
                    {
                        var value = fixedField.InnerText;

                        var fieldGO = new GameObject("FixedField");
                        var fieldComponent = fieldGO.AddComponent<FixedField>();

                        fieldComponent.connectivity = connectivityComponent;

                        var type = int.Parse(fixedField.Attributes["type"].Value, CultureInfo.InvariantCulture);
                        fieldComponent.kind = type % 2 == 0 ? ConnectionField.FieldKind.receptor : ConnectionField.FieldKind.connector;

                        var feature = new FixedFeature();
                        feature.typeId = type % 2 == 0 ? type / 2 : (type - 1) / 2;
                        feature.field = fieldComponent;
                        fieldComponent.feature = feature;

                        fieldComponent.name = "FixedField " + feature.typeId + "_" + fieldComponent.kind.ToString();

                        var angle = float.Parse(fixedField.Attributes["angle"].Value, CultureInfo.InvariantCulture);

                        var position = new Vector3
                        {
                            x = -float.Parse(fixedField.Attributes["tx"].Value, CultureInfo.InvariantCulture),
                            y = float.Parse(fixedField.Attributes["ty"].Value, CultureInfo.InvariantCulture),
                            z = float.Parse(fixedField.Attributes["tz"].Value, CultureInfo.InvariantCulture)
                        };
                        fieldComponent.transform.localPosition = position;

                        var rotation = Quaternion.AngleAxis(
                            -angle,
                            new Vector3(
                                -float.Parse(fixedField.Attributes["ax"].Value, CultureInfo.InvariantCulture),
                                float.Parse(fixedField.Attributes["ay"].Value, CultureInfo.InvariantCulture),
                                float.Parse(fixedField.Attributes["az"].Value, CultureInfo.InvariantCulture)
                            )
                        );
                        fieldComponent.transform.localRotation = rotation;

                        var axis = int.Parse(fixedField.Attributes["axes"].Value, CultureInfo.InvariantCulture);
                        fieldComponent.feature.axisType = (FixedFeature.AxisType)(axis - 1); // Axes == 1 -> Mono = 0 and Axes == 2 -> Dual = 1

                        var layer = LayerMask.NameToLayer(ConnectionField.GetLayer(fieldComponent.kind));

                        if (layer != -1)
                        {
                            fieldGO.layer = layer;
                        }

                        connectivityComponent.fixedFields.Add(fieldComponent);
                        fieldGO.transform.parent = connectivityGO.transform;

                        var collider = fieldGO.AddComponent<BoxCollider>();
                        collider.isTrigger = true;
                        collider.size = new Vector3(BrickBuildingUtility.LU_1, 0.0f, BrickBuildingUtility.LU_1);
                        collider.center = new Vector3(0.0f, 0.0f, 0.0f);
                    }

                    var fileName = designID + ".prefab";
                    var filePath = Path.Combine(connectivityPath, fileName);

                    var directoryName = Path.GetDirectoryName(filePath);
                    if (directoryName.Length > 0)
                    {
                        Directory.CreateDirectory(directoryName);
                    }

#if UNITY_EDITOR
                    PrefabUtility.SaveAsPrefabAsset(connectivityGO, filePath);
#endif
                    Object.DestroyImmediate(connectivityGO);

                    return true;
                }
            }
            return false;
        }

        public static bool UnpackCollidersForPart(string designID, bool forceUpdate = false)
        {
            if (File.Exists(Path.Combine(collidersPath, designID + ".prefab")) && !forceUpdate)
            {
                return true;
            }

            OpenDB();

            var partCollisionPath = "CollisionBox_Connectivity_Info/" + designID + ".xml";
            var entry = newPartsZipArchive.GetEntry(partCollisionPath);
            if (entry != null)
            {
                var zipStream = entry.Open();

                var doc = new XmlDocument();
                doc.Load(zipStream);

                var primitiveNode = doc.SelectSingleNode("LEGOPrimitive");
                var collisionNode = primitiveNode.SelectSingleNode("Collision");

                var collidersGO = new GameObject("Colliders");
                var colliders = collidersGO.AddComponent<Colliders>();
                colliders.version = AssetVersionChecker.currentVersion;

                if (collisionNode != null)
                {
                    var boxNodes = collisionNode.SelectNodes("Box");

                    foreach (XmlNode boxNode in boxNodes)
                    {
                        var attributes = boxNode.Attributes;
                        var size = new Vector3
                        {
                            x = float.Parse(attributes["sX"].Value, CultureInfo.InvariantCulture) * 2.0f,
                            y = float.Parse(attributes["sY"].Value, CultureInfo.InvariantCulture) * 2.0f,
                            z = float.Parse(attributes["sZ"].Value, CultureInfo.InvariantCulture) * 2.0f
                        };

                        var angle = float.Parse(attributes["angle"].Value, CultureInfo.InvariantCulture);

                        var rotation = Quaternion.AngleAxis
                        (
                            -angle,
                            new Vector3(
                                -float.Parse(attributes["ax"].Value, CultureInfo.InvariantCulture),
                                float.Parse(attributes["ay"].Value, CultureInfo.InvariantCulture),
                                float.Parse(attributes["az"].Value, CultureInfo.InvariantCulture)
                            )
                        );

                        var translation = new Vector3
                        {
                            x = -float.Parse(attributes["tx"].Value, CultureInfo.InvariantCulture),
                            y = float.Parse(attributes["ty"].Value, CultureInfo.InvariantCulture),
                            z = float.Parse(attributes["tz"].Value, CultureInfo.InvariantCulture)
                        };

                        var boxGO = new GameObject("Collision Box");
                        boxGO.transform.parent = collidersGO.transform;
                        var boxCollider = boxGO.AddComponent<BoxCollider>();
                        boxCollider.size = size;

                        boxGO.transform.localPosition = translation;
                        boxGO.transform.localRotation = rotation;

                        colliders.colliders.Add(boxCollider);
                    }

                    var sphereNodes = collisionNode.SelectNodes("Sphere");

                    foreach (XmlNode sphereNode in sphereNodes)
                    {
                        var attributes = sphereNode.Attributes;
                        var radius = float.Parse(attributes["radius"].Value, CultureInfo.InvariantCulture);

                        var translation = new Vector3
                        {
                            x = -float.Parse(attributes["tx"].Value, CultureInfo.InvariantCulture),
                            y = float.Parse(attributes["ty"].Value, CultureInfo.InvariantCulture),
                            z = float.Parse(attributes["tz"].Value, CultureInfo.InvariantCulture)
                        };

                        var sphereGO = new GameObject("Collision Sphere");
                        sphereGO.transform.parent = collidersGO.transform;
                        var sphereCollider = sphereGO.AddComponent<SphereCollider>();
                        sphereCollider.radius = radius;

                        sphereGO.transform.localPosition = translation;

                        colliders.colliders.Add(sphereCollider);
                    }
                }

                var collisionAddNode = primitiveNode.SelectSingleNode("CollisionAdd");

                if (collisionAddNode != null)
                {
                    var cylinderNodes = collisionAddNode.SelectNodes("Cylinder");

                    foreach (XmlNode cylinderNode in cylinderNodes)
                    {
                        var attributes = cylinderNode.Attributes;
                        var radius = float.Parse(attributes["radius"].Value, CultureInfo.InvariantCulture);
                        var length = float.Parse(attributes["length"].Value, CultureInfo.InvariantCulture) * 2.0f;

                        var translation = new Vector3
                        {
                            x = -float.Parse(attributes["tx"].Value, CultureInfo.InvariantCulture),
                            y = float.Parse(attributes["ty"].Value, CultureInfo.InvariantCulture),
                            z = float.Parse(attributes["tz"].Value, CultureInfo.InvariantCulture)
                        };

                        var boxCount = 4;
                        var anglePerBox = 180.0f / boxCount;
                        var baseAngle = (180.0f - anglePerBox) * 0.5f;

                        var width = (radius / Mathf.Sin(Mathf.Deg2Rad * baseAngle)) * Mathf.Sin(Mathf.Deg2Rad * anglePerBox);
                        var height = 2.0f * radius * Mathf.Cos(Mathf.Deg2Rad * anglePerBox * 0.5f);
                        var size = new Vector3(height, width, length);

                        var angle = float.Parse(attributes["angle"].Value, CultureInfo.InvariantCulture);
                        var rotation = Quaternion.AngleAxis
                        (
                            -angle,
                            new Vector3(
                                -float.Parse(attributes["ax"].Value, CultureInfo.InvariantCulture),
                                float.Parse(attributes["ay"].Value, CultureInfo.InvariantCulture),
                                float.Parse(attributes["az"].Value, CultureInfo.InvariantCulture)
                            )
                        );

                        for(var i = 0; i < boxCount; i++)
                        {
                            var boxColliderGO = new GameObject("Cylinder Box " + (i + 1));
                            boxColliderGO.transform.parent = collidersGO.transform;
                            var boxCollider = boxColliderGO.AddComponent<BoxCollider>();
                            
                            boxCollider.size = size;
                            boxColliderGO.transform.localPosition = translation;

                            var rot = Quaternion.AngleAxis(i * anglePerBox, Vector3.forward);
                            boxColliderGO.transform.localRotation = rotation * rot;

                            colliders.colliders.Add(boxCollider);
                        }
                    }

                    var tubeNodes = collisionAddNode.SelectNodes("Tube");

                    foreach (XmlNode tubeNode in tubeNodes)
                    {
                        var attributes = tubeNode.Attributes;
                        var outerRadius = float.Parse(attributes["outerRadius"].Value, CultureInfo.InvariantCulture);
                        var innerRadius = float.Parse(attributes["innerRadius"].Value, CultureInfo.InvariantCulture);
                        var length = float.Parse(attributes["length"].Value, CultureInfo.InvariantCulture) * 2.0f;

                        var translation = new Vector3
                        {
                            x = -float.Parse(attributes["tx"].Value, CultureInfo.InvariantCulture),
                            y = float.Parse(attributes["ty"].Value, CultureInfo.InvariantCulture),
                            z = float.Parse(attributes["tz"].Value, CultureInfo.InvariantCulture),
                        };

                        var angle = float.Parse(attributes["angle"].Value, CultureInfo.InvariantCulture);
                        var rotation = Quaternion.AngleAxis
                        (
                            -angle,
                            new Vector3(
                                -float.Parse(attributes["ax"].Value, CultureInfo.InvariantCulture),
                                float.Parse(attributes["ay"].Value, CultureInfo.InvariantCulture),
                                float.Parse(attributes["az"].Value, CultureInfo.InvariantCulture)
                            )
                        );

                        var boxCount = 6;
                        var anglePerBox = 180.0f / boxCount;
                        var baseAngle = (180.0f - anglePerBox) * 0.5f;

                        var width = (innerRadius / Mathf.Sin(Mathf.Deg2Rad * baseAngle)) * Mathf.Sin(Mathf.Deg2Rad * anglePerBox);
                        var height = (outerRadius - innerRadius) * Mathf.Cos(Mathf.Deg2Rad * anglePerBox * 0.5f);
                        var size = new Vector3(height, width, length);

                        for (var i = 0; i < boxCount; i++)
                        {
                            var rot = Quaternion.AngleAxis(i * anglePerBox, Vector3.forward);

                            var boxAGO = new GameObject("TubeBoxA " + (i + 1));
                            boxAGO.transform.parent = collidersGO.transform;

                            var boxA = boxAGO.AddComponent<BoxCollider>();
                            boxA.size = size;
                            boxAGO.transform.localPosition = translation;
                            boxAGO.transform.localRotation = rotation * rot;
                            boxAGO.transform.position += boxAGO.transform.right * (innerRadius + height * 0.5f);

                            var boxBGO = new GameObject("TubeBoxB " + (i + 1));
                            boxBGO.transform.parent = collidersGO.transform;

                            var boxB = boxBGO.AddComponent<BoxCollider>();
                            boxB.size = size;
                            boxBGO.transform.localPosition = translation;
                            boxBGO.transform.localRotation = rotation * rot;
                            boxBGO.transform.position -= boxBGO.transform.right * (innerRadius + height * 0.5f);

                            colliders.colliders.Add(boxA);
                            colliders.colliders.Add(boxB);
                        }
                    }
                }

                var fileName = designID + ".prefab";
                var filePath = Path.Combine(collidersPath, fileName);

                var directoryName = Path.GetDirectoryName(filePath);
                if (directoryName.Length > 0)
                {
                    Directory.CreateDirectory(directoryName);
                }

#if UNITY_EDITOR
                PrefabUtility.SaveAsPrefabAsset(collidersGO, filePath);
#endif
                Object.DestroyImmediate(collidersGO);

                return true;
            }
            return false;
        }

        private static void CreateCommonPartLod2(string name, float height, float radius, bool knob, bool hollow, float innerRadius, bool lightmapped, int numVerticesAroundEdge = 5)
        {
            // FIXME Do we want lightmapped and normalmapped (logo) versions?

            Debug.Assert(numVerticesAroundEdge > 2);

            var meshPath = Path.Combine(geometryPath, commonPartsDir, lightmapped ? lightmappedDir : "", lod2Dir, name + ".asset");
            if (File.Exists(meshPath))
            {
                return;
            }

            int verticesOnSides = 2 * numVerticesAroundEdge;
            int verticesOnCap = numVerticesAroundEdge;
            int verticesOnHollowCap = 2 * numVerticesAroundEdge;

            int trianglesOnSides = 2 * numVerticesAroundEdge;
            int trianglesOnCap = numVerticesAroundEdge;
            int trianglesOnHollowCap = 2 * numVerticesAroundEdge;

            var mesh = new Mesh();
            var vertices = new Vector3[verticesOnSides + (hollow ? verticesOnSides + verticesOnHollowCap : verticesOnCap + 1)];
            var normals = new Vector3[verticesOnSides + (hollow ? verticesOnSides + verticesOnHollowCap : verticesOnCap + 1)];
            var triangles = new int[3 * (trianglesOnSides + (hollow ? trianglesOnSides + trianglesOnHollowCap : trianglesOnCap))];

            var capNormal = knob ? Vector3.up : Vector3.down;

            // Build the outer sides - they are always the same.
            for(var i = 0; i < numVerticesAroundEdge; ++i)
            {
                var angle = 2.0f * i / numVerticesAroundEdge * Mathf.PI;
                var direction = new Vector3(Mathf.Cos(angle), 0.0f, Mathf.Sin(angle));
                vertices[2 * i + 0] = direction * radius;
                vertices[2 * i + 1] = direction * radius + capNormal * height;

                normals[2 * i + 0] = direction;
                normals[2 * i + 1] = direction;
            }

            for(var i = 0; i < trianglesOnSides; i+= 2)
            {
                triangles[3 * i + 0] = i;
                triangles[3 * i + 1] = (i + 1) % verticesOnSides;
                triangles[3 * i + 2] = (i + 2) % verticesOnSides;
                triangles[3 * i + 3] = (i + 2) % verticesOnSides;
                triangles[3 * i + 4] = (i + 1) % verticesOnSides;
                triangles[3 * i + 5] = (i + 3) % verticesOnSides;
            }

            // Build the outer cap vertices - they are also always the same.
            for(var i = 0; i < verticesOnCap; ++i)
            {
                vertices[verticesOnSides + i] = vertices[2 * i + 1];
                normals[verticesOnSides + i] = capNormal;
            }

            if (!hollow)
            {
                // Add central vertex to cap.
                vertices[verticesOnSides + verticesOnCap] = capNormal * height;
                normals[verticesOnSides + verticesOnCap] = capNormal;

                // Connect the cap.
                for (var i = 0; i < trianglesOnCap; ++i)
                {
                    triangles[3 * (trianglesOnSides + i) + 0] = verticesOnSides + i;
                    triangles[3 * (trianglesOnSides + i) + 1] = vertices.Length - 1;
                    triangles[3 * (trianglesOnSides + i) + 2] = verticesOnSides + (i + 1) % trianglesOnCap;
                }
            } else
            {
                // Build the inner sides.
                for (var i = 0; i < numVerticesAroundEdge; ++i)
                {
                    var angle = 2.0f * i / numVerticesAroundEdge * Mathf.PI;
                    var direction = new Vector3(Mathf.Cos(angle), 0.0f, Mathf.Sin(angle));
                    vertices[verticesOnSides + verticesOnCap + 2 * i + 0] = direction * innerRadius;
                    vertices[verticesOnSides + verticesOnCap + 2 * i + 1] = direction * innerRadius + capNormal * height;

                    normals[verticesOnSides + verticesOnCap + 2 * i + 0] = -direction;
                    normals[verticesOnSides + verticesOnCap + 2 * i + 1] = -direction;
                }

                for (var i = 0; i < trianglesOnSides; i += 2)
                {
                    triangles[3 * (trianglesOnSides + i) + 0] = verticesOnSides + verticesOnCap + i;
                    triangles[3 * (trianglesOnSides + i) + 1] = verticesOnSides + verticesOnCap + (i + 2) % verticesOnSides;
                    triangles[3 * (trianglesOnSides + i) + 2] = verticesOnSides + verticesOnCap + (i + 1) % verticesOnSides;
                    triangles[3 * (trianglesOnSides + i) + 3] = verticesOnSides + verticesOnCap + (i + 2) % verticesOnSides;
                    triangles[3 * (trianglesOnSides + i) + 4] = verticesOnSides + verticesOnCap + (i + 3) % verticesOnSides;
                    triangles[3 * (trianglesOnSides + i) + 5] = verticesOnSides + verticesOnCap + (i + 1) % verticesOnSides;
                }

                // Build the inner cap vertices.
                for (var i = 0; i < verticesOnCap; ++i)
                {
                    vertices[verticesOnSides + verticesOnCap + verticesOnSides + i] = vertices[verticesOnSides + verticesOnCap + 2 * i + 1];
                    normals[verticesOnSides + verticesOnCap + verticesOnSides + i] = capNormal;
                }

                // Connect the cap.
                for (var i = 0; i < trianglesOnHollowCap; i += 2)
                {
                    triangles[3 * (trianglesOnSides + trianglesOnSides + i) + 0] = verticesOnSides + i / 2;
                    triangles[3 * (trianglesOnSides + trianglesOnSides + i) + 1] = verticesOnSides + verticesOnCap + verticesOnSides + i / 2;
                    triangles[3 * (trianglesOnSides + trianglesOnSides + i) + 2] = verticesOnSides + (i / 2 + 1) % verticesOnCap;
                    triangles[3 * (trianglesOnSides + trianglesOnSides + i) + 3] = verticesOnSides + verticesOnCap + verticesOnSides + i / 2;
                    triangles[3 * (trianglesOnSides + trianglesOnSides + i) + 4] = verticesOnSides + verticesOnCap + verticesOnSides + (i / 2 + 1) % verticesOnCap;
                    triangles[3 * (trianglesOnSides + trianglesOnSides + i) + 5] = verticesOnSides + (i / 2 + 1) % verticesOnCap;
                }
            }

            // Handle orientation flip for non-knobs.
            if (!knob)
            {
                for (var i = 0; i < triangles.Length; i += 3)
                {
                    var tmp = triangles[i + 1];
                    triangles[i + 1] = triangles[i];
                    triangles[i] = tmp;
                }
            }

            // Create mesh and save as asset.
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = triangles;

            var directoryName = Path.GetDirectoryName(meshPath);
            if (directoryName.Length > 0)
            {
                Directory.CreateDirectory(directoryName);
            }

#if UNITY_EDITOR
            AssetDatabase.CreateAsset(mesh, meshPath);
#endif

            // Create mesh prefab.
            var meshPrefab = new GameObject(name);
            meshPrefab.AddComponent<MeshFilter>().sharedMesh = mesh;
            meshPrefab.AddComponent<MeshRenderer>();

            var meshPrefabPath = Path.Combine(geometryPath, commonPartsDir, lightmapped ? lightmappedDir : "", lod2Dir, name + ".prefab");

#if UNITY_EDITOR
            PrefabUtility.SaveAsPrefabAsset(meshPrefab, meshPrefabPath);
#endif
            Object.DestroyImmediate(meshPrefab);
        }

        public static GameObject LoadPart(string designID, bool lightmapped, bool legacy, int lod)
        {
#if UNITY_EDITOR
            string lodDir = lod == 0 ? lod0Dir : lod1Dir; // Currently, there is no LOD2 for parts.
            return AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(geometryPath, legacy ? legacyDir : newDir, lightmapped ? lightmappedDir : "", lodDir, designID + ".fbx"));
#else
        return null;
#endif
        }

        public static GameObject LoadCommonPart(string name, bool lightmapped, int lod)
        {
#if UNITY_EDITOR
            string lodDir = lod == 0 ? lod0Dir : lod == 1 ? lod1Dir : lod2Dir;
            string extension = lod <= 1 ? ".fbx" : ".prefab";
            return AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(geometryPath, commonPartsDir, lightmapped ? lightmappedDir : "", lodDir, name + extension));
#else
        return null;
#endif
        }

        public static GameObject LoadConnectivityPrefab(string designID)
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(connectivityPath, designID + ".prefab"));
#else
        return null;
#endif
        }

        public static GameObject LoadCollidersPrefab(string designID)
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<GameObject>(Path.Combine(collidersPath, designID + ".prefab"));
#else
        return null;
#endif
        }

        public static void StoreOptimizedMesh(Mesh mesh, string name)
        {
#if UNITY_EDITOR
            AssetDatabase.CreateAsset(mesh, AssetDatabase.GenerateUniqueAssetPath(Path.Combine(geometryPath, name)));
#endif
        }

        public static bool IsLightmapped(Part part)
        {
            var modelGroup = part.transform.GetComponentInParent<ModelGroup>();
            if (modelGroup)
            {
                return modelGroup.importSettings.lightmapped;
            }

            var lightmapped = false;

            if (part.knobs.Count > 0)
            {
                if (IsMeshFilterLightmapped(part.knobs[0].transform, ref lightmapped))
                {
                    return lightmapped;
                }
            }

            if (part.tubes.Count > 0)
            {
                if (IsMeshFilterLightmapped(part.tubes[0].transform, ref lightmapped))
                {
                    return lightmapped;
                }
            }

            var shell = part.transform.Find("Shell");
            if (shell)
            {
                if (IsMeshFilterLightmapped(shell.transform, ref lightmapped))
                {
                    return lightmapped;
                }
            }

            Debug.LogWarning("Could not determine if part " + part + " is lightmapped. Assuming not.");

            return false;
        }

        private static bool IsMeshFilterLightmapped(Transform transform, ref bool result)
        {
            var meshFilter = transform.GetComponent<MeshFilter>();
            if (meshFilter && meshFilter.sharedMesh)
            {
                var assetPath = AssetDatabase.GetAssetPath(meshFilter.sharedMesh);

                result = assetPath.Contains(lightmappedDir);
                return true;
            }

            return false;
        }
        
        public static int GetLOD(Part part)
        {
            var modelGroup = part.transform.GetComponentInParent<ModelGroup>();
            if (modelGroup)
            {
                return modelGroup.importSettings.lod;
            }

            var lod = 0;

            if (part.knobs.Count > 0)
            {
                if (GetLODFromMeshFilter(part.knobs[0].transform, ref lod))
                {
                    return lod;
                }
            }

            if (part.tubes.Count > 0)
            {
                if (GetLODFromMeshFilter(part.tubes[0].transform, ref lod))
                {
                    return lod;
                }
            }

            var shell = part.transform.Find("Shell");
            if (shell)
            {
                if (GetLODFromMeshFilter(shell.transform, ref lod))
                {
                    return lod;
                }
            }

            Debug.LogWarning("Could not determine LOD for part " + part + ". Assuming LOD 0.");

            return 0;
        }

        private static bool GetLODFromMeshFilter(Transform transform, ref int result)
        {
            var meshFilter = transform.GetComponent<MeshFilter>();
            if (meshFilter && meshFilter.sharedMesh)
            {
                var assetPath = AssetDatabase.GetAssetPath(meshFilter.sharedMesh);
                if (GetLODFromAssetPath(assetPath, ref result))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool GetLODFromAssetPath(string path, ref int result)
        {
            if (path.Contains(lod0Dir))
            {
                result = 0;
                return true;
            }
            else if (path.Contains(lod1Dir))
            {
                result = 1;
                return true;
            }
            else if (path.Contains(lod2Dir))
            {
                result = 2;
                return true;
            }

            return false;
        }

        //[MenuItem("LEGO Tools/Dev/Compare Legacy and New Parts DBs")]
        public static void CompareLegcayAndNewPartsDBs()
        {
            var legacyPartsZipArchive = ZipFile.OpenRead(legacyPartsPath);
            var entries = legacyPartsZipArchive.Entries;

            HashSet<string> foundOldIds = new HashSet<string>();

            foreach (var entry in entries)
            {
                var designId = Path.GetFileNameWithoutExtension(entry.Name);

                foundOldIds.Add(designId);
            }
            legacyPartsZipArchive.Dispose();

            var newsPartsZipArchive = ZipFile.OpenRead(newPartsPath);
            entries = newsPartsZipArchive.Entries;

            HashSet<string> foundNewIds = new HashSet<string>();

            foreach (var entry in entries)
            {
                if (entry.Name.StartsWith("Geometry") && entry.Name.EndsWith(".fbx"))
                {
                    var designId = Path.GetFileNameWithoutExtension(entry.Name).Substring(1);

                    foundNewIds.Add(designId);
                }
            }
            newsPartsZipArchive.Dispose();

            Debug.Log($"Legacy part count {foundOldIds.Count}");
            Debug.Log($"New part count {foundNewIds.Count}");

            var legacyNotInNew = new List<string>();
            foreach (var oldId in foundOldIds)
            {
                if (!foundNewIds.Contains(oldId))
                {
                    legacyNotInNew.Add(oldId);
                }
            }

            legacyNotInNew.Sort((a, b) => int.Parse(a) - int.Parse(b));

            var newNotInLegacy = new List<string>();
            foreach (var newId in foundNewIds)
            {
                if (!foundOldIds.Contains(newId))
                {
                    newNotInLegacy.Add(newId);
                }
            }

            newNotInLegacy.Sort((a, b) => int.Parse(a) - int.Parse(b));

            Debug.Log($"Legacy not in new count {legacyNotInNew.Count}");
            Debug.Log($"New not in legacy count {newNotInLegacy.Count}");

            Debug.Log("Legacy parts not in new:");
            foreach (var missingLegacy in legacyNotInNew)
            {
                Debug.Log(missingLegacy);
            }

            Debug.Log("New parts not in legacy:");
            foreach (var missingNew in newNotInLegacy)
            {
                Debug.Log(missingNew);
            }
        }
    }

}