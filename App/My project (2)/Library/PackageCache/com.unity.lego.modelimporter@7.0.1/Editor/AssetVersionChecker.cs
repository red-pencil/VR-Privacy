// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using LEGOMaterials;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LEGOModelImporter
{
    public class AssetVersionChecker
    {
        public static readonly int currentVersion = 2;
        static readonly string versionFile = "Version.asset";
        static readonly string dialogTitle = "Updating assets";
        static readonly string dontAskUpdateToThisVersionPrefsKey = "com.unity.lego.modelimporter.dontAskUpdateForVersion";
        private static bool updating = false;

        public static event Action updateStarted;
        public static event Action updateFinished;

        internal static void CheckForUpdates()
        {
            if (CheckVersion())
            {
                UpdateAssets();
            }
        }

        static bool UpdateDialog()
        {
            return EditorUtility.DisplayDialog("LEGO® asset data outdated",
            "You need to update to continue using Brick Building and connectivity.\n\nUpdating the LEGO® asset data is non-reversible and will be done across all scenes and prefabs.\n\nThe update may take a long time depending on the number of prefabs.\n\nYou can always force an update at a later time in the LEGO Tools menu.\n\nAll unsaved changes will be lost.\n\nUpdate data?", "Yes", "No");
        }

        static void UpToDateDialog()
        {
            EditorUtility.DisplayDialog("LEGO® asset data up to date", $"The asset data is up to date on version {currentVersion}.", "Ok");
        }

        [MenuItem("LEGO Tools/Check Asset Version", priority = 100)]
        static void CheckAssetVersion()
        {
            EditorApplication.delayCall += () => CheckAndUpdate(true, true);
        }

        [MenuItem("LEGO Tools/Dev/Force Update")]
        static void ForceUpdate()
        {
            UpdateAssets();
        }

        [InitializeOnLoadMethod]
        private static void InitializeVersionChecker()
        {
            EditorApplication.delayCall += () => CheckAndUpdate();
        }

        private static void CheckAndUpdate(bool forceCheck = false, bool notifyUpToDate = false)
        {
            if(updating)
            {
                return;
            }

            if(CheckVersion(forceCheck, notifyUpToDate))
            {
                UpdateAssets();
            }            
        }

        private static void UpdateAssets()
        {
            updateStarted?.Invoke();

            EditorUtility.DisplayProgressBar(dialogTitle, "Getting ready", 0.0f);
            updating = true;

            var activeScene = SceneManager.GetActiveScene();
            var activeScenePath = activeScene.path;

            CheckConnectivityFeatureLayer.CreateReceptorLayer();
            CheckConnectivityFeatureLayer.CreateConnectorLayer();

            // 1a. Update connectivity prefabs.
            if (AssetDatabase.IsValidFolder(PartUtility.connectivityPath))
            {
                string[] guids = AssetDatabase.FindAssets("", new string[] { PartUtility.connectivityPath });

                for (int i = 0; i < guids.Length; i++)
                {
                    EditorUtility.DisplayProgressBar(dialogTitle, "Updating connectivity prefabs", 0.15f * i / guids.Length);

                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (!File.GetAttributes(assetPath).HasFlag(FileAttributes.Directory))
                    {
                        Connectivity connectivity = AssetDatabase.LoadAssetAtPath<Connectivity>(assetPath);
                        if (connectivity)
                        {
                            if (connectivity.version < currentVersion)
                            {
                                UpdateConnectivity(connectivity);
                            }
                        }
                    }
                }
            }

            EditorUtility.DisplayProgressBar(dialogTitle, "Getting ready", 0.2f);

            // 1b. Update colliders prefabs.
            if (AssetDatabase.IsValidFolder(PartUtility.collidersPath))
            {
                string[] guids = AssetDatabase.FindAssets("", new string[] { PartUtility.collidersPath });

                for (int i = 0; i < guids.Length; i++)
                {
                    EditorUtility.DisplayProgressBar(dialogTitle, "Updating collider prefabs", 0.15f + 0.15f * i / guids.Length);

                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (!File.GetAttributes(assetPath).HasFlag(FileAttributes.Directory))
                    {
                        Colliders colliders = AssetDatabase.LoadAssetAtPath<Colliders>(assetPath);
                        if (colliders)
                        {
                            if (colliders.version < currentVersion)
                            {
                                UpdateColliders(colliders);
                            }
                        }
                        else
                        {
                            GameObject collidersGO = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                            colliders = collidersGO.AddComponent<Colliders>();
                            UpdateColliders(colliders);
                        }
                    }
                }
            }

            // 1c. Update geometry.
            if (AssetDatabase.IsValidFolder(Path.Combine(PartUtility.geometryPath, PartUtility.newDir)))
            {
                string[] guids = AssetDatabase.FindAssets("", new string[] { Path.Combine(PartUtility.geometryPath, PartUtility.newDir) });

                for (int i = 0; i < guids.Length; i++)
                {
                    EditorUtility.DisplayProgressBar(dialogTitle, "Updating geometry files", 0.3f + 0.05f * i / guids.Length);

                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (!File.GetAttributes(assetPath).HasFlag(FileAttributes.Directory))
                    {
                        UpdateGeometry(assetPath);
                    }
                }
            }
            if (AssetDatabase.IsValidFolder(Path.Combine(PartUtility.geometryPath, PartUtility.legacyDir)))
            {
                string[] guids = AssetDatabase.FindAssets("", new string[] { Path.Combine(PartUtility.geometryPath, PartUtility.legacyDir) });

                for (int i = 0; i < guids.Length; i++)
                {
                    EditorUtility.DisplayProgressBar(dialogTitle, "Updating geometry files", 0.35f + 0.05f * i / guids.Length);

                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (!File.GetAttributes(assetPath).HasFlag(FileAttributes.Directory))
                    {
                        UpdateGeometry(assetPath);
                    }
                }
            }
            if (AssetDatabase.IsValidFolder(Path.Combine(PartUtility.geometryPath, PartUtility.commonPartsDir)))
            {
                string[] guids = AssetDatabase.FindAssets("", new string[] { Path.Combine(PartUtility.geometryPath, PartUtility.commonPartsDir) });

                for (int i = 0; i < guids.Length; i++)
                {
                    EditorUtility.DisplayProgressBar(dialogTitle, "Updating geometry files", 0.4f + 0.05f * i / guids.Length);

                    string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    if (!File.GetAttributes(assetPath).HasFlag(FileAttributes.Directory))
                    {
                        UpdateGeometry(assetPath);
                    }
                }
            }

            // 2. Run through all prefabs and determine dependency graph based on nesting. Also collect all scene paths for later processing.
            string[] allAssetPaths = AssetDatabase.GetAllAssetPaths();
            List<string> scenes = new List<string>();
            var index = 0;

            var dependencyGraph = new DirectedGraph<string>();
            var existingDependencyNodes = new Dictionary<string, DirectedGraph<string>.Node>();

            foreach (var path in allAssetPaths)
            {
                EditorUtility.DisplayProgressBar(dialogTitle, "Detecting prefab dependencies", 0.45f + 0.15f * index++ / allAssetPaths.Length);

                int pos = path.IndexOf("/", StringComparison.Ordinal);
                if (pos < 0 || path.Substring(0, pos) == "Packages")
                    continue;   // Skip assets in packages.

                if (path.StartsWith(PartUtility.connectivityPath))
                    continue;   // Skip connectivity assets.

                if (path.StartsWith(PartUtility.collidersPath))
                    continue;   // Skip collider assets.

                if (path.StartsWith(PartUtility.geometryPath))
                    continue;   // Skip geometry assets.

                pos = path.LastIndexOf(".", StringComparison.Ordinal) + 1;
                string type = path.Substring(pos, path.Length - pos);

                switch (type)
                {
                    case "prefab":
                        {
                            var contents = PrefabUtility.LoadPrefabContents(path);

                            var bricks = contents.GetComponentsInChildren<Brick>(true);

                            if (bricks.Length > 0)
                            {
                                FindDependencies(path, bricks, existingDependencyNodes, dependencyGraph);
                            }

                            PrefabUtility.UnloadPrefabContents(contents);
                        }
                        break;
                    case "unity":
                        {
                            scenes.Add(path);
                        }
                        break;
                }
            }

            // 3. Run through dependency sorted prefabs and update+connect connectivity on non-prefab instances.
            var sortedNodes = dependencyGraph.TopologicalSort();
            index = 0;
            foreach (var node in sortedNodes)
            {
                EditorUtility.DisplayProgressBar(dialogTitle, "Updating prefabs", 0.6f + 0.2f * index++ / sortedNodes.Count);

                var contents = PrefabUtility.LoadPrefabContents(node.data);

                var bricks = contents.GetComponentsInChildren<Brick>(true);

                if (bricks.Length > 0)
                {
                    if (UpdateAsset(bricks))
                    {
                        Debug.Log(node.data + " Updated");
                    }
                    FixConnections(bricks);

                    PrefabUtility.SaveAsPrefabAsset(contents, node.data);
                }

                PrefabUtility.UnloadPrefabContents(contents);
            }

            // 4. Run through all scenes and update+connect connectivity on non-prefab instances.
            index = 0;
            foreach (var path in scenes)
            {
                EditorUtility.DisplayProgressBar(dialogTitle, "Updating scenes", 0.8f + 0.2f * index++ / scenes.Count);

                Scene scene = EditorSceneManager.OpenScene(path);

                var gameObjectsInScene = scene.GetRootGameObjects();

                foreach (var gameObject in gameObjectsInScene)
                {
                    if (PrefabUtility.IsPartOfAnyPrefab(gameObject))
                    {
                        continue;
                    }

                    var bricks = gameObject.GetComponentsInChildren<Brick>(true);

                    if (bricks.Length > 0)
                    {
                        if (UpdateAsset(bricks))
                        {
                            Debug.Log($" -------- {gameObject.name} Updated");
                        }
                        FixConnections(bricks);
                    }
                }
                EditorSceneManager.SaveScene(scene);
            }

            if (activeScenePath.Length != 0)
            {
                EditorSceneManager.OpenScene(activeScenePath);
            }

            updating = false;
            EditorUtility.ClearProgressBar();

            updateFinished?.Invoke();
        }

        static bool CheckVersion(bool forceCheck = false, bool notifyUpToDate = false)
        {
            var dontAskVersion = SessionState.GetInt(dontAskUpdateToThisVersionPrefsKey, 0);
            if(!forceCheck && currentVersion == dontAskVersion)
            {
                return false;
            }
            
            var updateFrom = currentVersion;
            var versionFilePath = Path.Combine(PartUtility.dataRootPath, versionFile);
            if (File.Exists(versionFilePath))
            {
                TextAsset versionAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(versionFilePath); 
                if(int.TryParse(versionAsset.text, out int version))
                {
                    if(currentVersion > version)
                    {
                        if(UpdateDialog())
                        {
                            AssetDatabase.CreateAsset(new TextAsset(currentVersion.ToString()), versionFilePath);
                            updateFrom = version;
                        }
                        else
                        {
                            SessionState.SetInt(dontAskUpdateToThisVersionPrefsKey, currentVersion);
                        }
                    }
                    else
                    {
                        if (notifyUpToDate)
                        {

                            UpToDateDialog();
                        }
                    }
                }
            }
            else
            {
                if(!Directory.Exists(PartUtility.dataRootPath))
                {
                    Directory.CreateDirectory(PartUtility.dataRootPath);

                    AssetDatabase.CreateAsset(new TextAsset(currentVersion.ToString()), versionFilePath);
                    if (notifyUpToDate)
                    {
                        UpToDateDialog();
                    }
                }
                else
                {
                    if (UpdateDialog())
                    {
                        AssetDatabase.CreateAsset(new TextAsset(currentVersion.ToString()), versionFilePath);
                        updateFrom = 0;
                    }
                    else
                    {
                        SessionState.SetInt(dontAskUpdateToThisVersionPrefsKey, currentVersion);
                    }
                }
            }
            return updateFrom < currentVersion;
        }

        private static void FixConnections(Brick[] bricks)
        {
            Physics.SyncTransforms();
            foreach(var brick in bricks)
            {
                foreach (var part in brick.parts)
                {
                    var connectivity = part.connectivity;
                    if (!connectivity)
                    {
                        // Unsupported or legacy.
                        continue;
                    }

                    foreach(var field in connectivity)
                    {
                        var connections = field.QueryConnections(out bool reject);
                        if(reject)
                        {
                            continue;
                        }

                        foreach(var pair in connections)
                        {
                            if(pair.Item1 is PlanarFeature p1 && pair.Item2 is PlanarFeature p2)
                            {
                                if (p1.HasConnection() || p2.HasConnection())
                                {
                                    continue;
                                }

                                if (p1.CheckConnectionTransformationValid(p2, out Connection.ConnectionInteraction match))
                                {
                                    if (match == Connection.ConnectionInteraction.Ignore)
                                    {
                                        continue;
                                    }

                                    p1.Field.Connect(p1, p2);
                                    Connection.RegisterPrefabChanges(p1.field);
                                    Connection.RegisterPrefabChanges(p2.field);
                                }
                            }
                            else if(pair.Item1 is AxleFeature a1 && pair.Item2 is AxleFeature a2)
                            {
                                if (a1.CheckConnectionTransformationValid(a2, out Connection.ConnectionInteraction match))
                                {
                                    if (match == Connection.ConnectionInteraction.Reject)
                                    {
                                        continue;
                                    }

                                    a1.Field.Connect(a2);
                                    Connection.RegisterPrefabChanges(a1.field);
                                    Connection.RegisterPrefabChanges(a2.field);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static bool UpdateCommonPart<T>(Part part, Transform parentToInstantiate, List<T> partsList, string name, bool lightmapped, int lod, Material material) where T : CommonPart
        {
            partsList.Clear();
            var oldCommonParts = part.transform.Find(name);
            if (oldCommonParts)
            {
                UnityEngine.Object.DestroyImmediate(oldCommonParts.gameObject);
            }

            var newParent = UnityEngine.Object.Instantiate(parentToInstantiate);
            newParent.name = name;
            newParent.transform.parent = part.transform;
            newParent.transform.position = part.transform.position;
            newParent.transform.rotation = part.transform.rotation;

            ModelImporter.InstantiateCommonParts(part, partsList, newParent.transform, lightmapped, lod);

            foreach (Transform commonPart in newParent)
            {
                commonPart.GetComponent<MeshRenderer>().sharedMaterial = material;
            }

            return true;
        }

        private static bool UpdatePartGeometry(Part part)
        {
            if(part.legacy)
            {
                return false;
            }

            int lod = PartUtility.GetLOD(part);
            bool lightmapped = PartUtility.IsLightmapped(part);

            var partExistenceResult = PartUtility.UnpackPart(part.designID.ToString(), lightmapped, lod);

            GameObject partToInstantiate;
            if (partExistenceResult.existence != PartUtility.PartExistence.None)
            {
                partToInstantiate = PartUtility.LoadPart(partExistenceResult.designID, lightmapped, false, lod);
            }
            else
            {
                return false;
            }

            if (partToInstantiate)
            {
                var material = Part.GetMaterial(part.materialIDs[0]);

                var tubeLoc = partToInstantiate.transform.Find("Tubes_loc");
                if(tubeLoc)
                {
                    UpdateCommonPart(part, tubeLoc, part.tubes, "Tubes", lightmapped, lod, material);
                }

                var knobLoc = partToInstantiate.transform.Find("Knobs_loc");
                if (knobLoc)
                {
                    UpdateCommonPart(part, knobLoc, part.knobs, "Knobs", lightmapped, lod, material);
                }

                var decorationSurfaces = partToInstantiate.transform.Find("DecorationSurfaces");
                if(decorationSurfaces)
                {
                    Transform oldDecorationSurfaces = part.transform.Find("DecorationSurfaces");

                    var decorationTransform = UnityEngine.Object.Instantiate(decorationSurfaces);
                    decorationTransform.name = "DecorationSurfaces";
                    decorationTransform.parent = part.transform;
                    decorationTransform.localPosition = part.transform.localPosition;
                    decorationTransform.localRotation = part.transform.localRotation;

                    var anyActive = false;

                    if(oldDecorationSurfaces)
                    {
                        var renderers = decorationTransform.GetComponentsInChildren<MeshRenderer>(true);
                        var oldRenderers = oldDecorationSurfaces.GetComponentsInChildren<MeshRenderer>(true);
                        var length = Mathf.Min(renderers.Length, oldRenderers.Length);

                        for (var i = 0; i < length; i++)
                        {
                            renderers[i].sharedMaterial = oldRenderers[i].sharedMaterial;
                            if(oldDecorationSurfaces.gameObject.activeInHierarchy)
                            {
                                var isActive = oldDecorationSurfaces.GetChild(i).gameObject.activeInHierarchy;
                                anyActive |= isActive;
                                decorationTransform.GetChild(i).gameObject.SetActive(isActive);
                            }
                        }

                        UnityEngine.Object.DestroyImmediate(oldDecorationSurfaces.gameObject);
                    }

                    decorationTransform.gameObject.SetActive(anyActive);
                }

                var colourChangeSurfaces = partToInstantiate.transform.Find("ColourChangeSurfaces");
                if(colourChangeSurfaces)
                {
                    Transform oldColourChangeSurfaces = part.transform.Find("ColourChangeSurfaces");

                    if (oldColourChangeSurfaces)
                    {
                        if (oldColourChangeSurfaces.childCount < colourChangeSurfaces.childCount)
                        {
                            Debug.LogWarning("New colour change surfaces were added and are given the default material.");
                        }

                        UnityEngine.Object.DestroyImmediate(oldColourChangeSurfaces.gameObject);
                    }

                    var colourChangeTransform = UnityEngine.Object.Instantiate(colourChangeSurfaces);
                    colourChangeTransform.name = "ColourChangeSurfaces";
                    colourChangeTransform.parent = part.transform;
                    colourChangeTransform.localPosition = part.transform.localPosition;
                    colourChangeTransform.localRotation = part.transform.localRotation;

                    var renderers = colourChangeTransform.GetComponentsInChildren<MeshRenderer>();

                    for (var i = 1; i < part.materialIDs.Count; i++)
                    {
                        renderers[i - 1].sharedMaterial = Part.GetMaterial(part.materialIDs[i]);
                    }
                }

                var shell = partToInstantiate.transform.Find("Shell");
                if (shell)
                {
                    Transform oldShell = part.transform.Find("Shell");

                    if (oldShell)
                    {
                        UnityEngine.Object.DestroyImmediate(oldShell.gameObject);
                    }

                    var shellTransform = UnityEngine.Object.Instantiate(shell);
                    shellTransform.name = "Shell";
                    shellTransform.parent = part.transform;
                    shellTransform.localPosition = shell.localPosition;
                    shellTransform.localRotation = shell.localRotation;

                    var mr = shellTransform.GetComponent<MeshRenderer>();
                    if (mr)
                    {
                        mr.sharedMaterial = material;
                    }
                }

            }
            return true;
        }

        private static bool UpdateAsset(Brick[] bricks)
        {
            var updated = false;

            foreach (var brick in bricks)
            {
                if(PrefabUtility.IsPartOfPrefabInstance(brick))
                {
                    continue;
                }

                foreach(var part in brick.parts)
                {
                    var designID = part.designID.ToString();

                    UpdatePartGeometry(part);

                    var connectivity = part.connectivity;
                    if(connectivity && connectivity.version != currentVersion)
                    {
                        updated = true;

                        // Clear out old connectivity.
                        GameObject.DestroyImmediate(connectivity.gameObject, true);
                        part.connectivity = null;

                        foreach (var tube in part.tubes)
                        {
                            tube.connections.Clear();
                            tube.field = null;

                            tube.gameObject.SetActive(true);
                        }

                        foreach (var knob in part.knobs)
                        {
                            knob.field = null;
                            knob.connectionIndex = -1;

                            knob.gameObject.SetActive(true);
                        }

                        // Try to create updated connectivity.
                        PartUtility.UnpackConnectivityForPart(designID);

                        var connectivityToInstantiate = PartUtility.LoadConnectivityPrefab(designID);
                        if (connectivityToInstantiate)
                        {
                            var connectivityGO = UnityEngine.Object.Instantiate(connectivityToInstantiate);
                            connectivityGO.name = "Connectivity";
                            connectivityGO.transform.SetParent(part.transform, false);
                            var connectivityComp = connectivityGO.GetComponent<Connectivity>();
                            part.connectivity = connectivityComp;
                            part.brick.totalBounds.Encapsulate(connectivityComp.extents);
                            connectivityComp.part = part;

                            foreach (var field in connectivityComp.planarFields)
                            {
                                foreach (var connection in field.connections)
                                {
                                    ModelImporter.MatchConnectionWithKnob(connection, part.knobs);
                                    ModelImporter.MatchConnectionWithTubes(connection, part.tubes);
                                }
                            }
                        }
                        else
                        {
                            Debug.LogError("Failed to load updated connectivity for " + designID + ". Please try reimporting the model to fix the issue.");
                        }
                    }


                    var colliders = part.GetComponentInChildren<Colliders>();
                    if(colliders && colliders.version == currentVersion)
                    {
                        // Already up to date.
                        continue;
                    }

                    // In case we are at version 0 or 1, the collider parent object will not yet have a Colliders component
                    if (colliders || part.transform.Find("Colliders"))
                    {
                        updated = true;

                        // Clear out old colliders.
                        if (!colliders)
                        {
                            GameObject.DestroyImmediate(part.transform.Find("Colliders").gameObject, true);
                        }
                        else
                        {
                            GameObject.DestroyImmediate(colliders.gameObject, true);
                        }

                        // Try to create updated colliders.
                        PartUtility.UnpackCollidersForPart(designID);

                        var colliderToInstantiate = PartUtility.LoadCollidersPrefab(designID);
                        if (colliderToInstantiate)
                        {
                            var collidersGO = UnityEngine.Object.Instantiate(colliderToInstantiate);
                            collidersGO.name = "Colliders";
                            collidersGO.transform.SetParent(part.transform, false);
                            var collidersComp = collidersGO.GetComponent<Colliders>();
                            part.colliders = collidersComp;
                            collidersComp.part = part;
                        }
                        else
                        {
                            Debug.LogError("Failed to load updated colliders for " + designID + ". Please try reimporting the model to fix the issue.");
                        }
                    }
                }
            }
            return updated;
        }

        private static void FindDependencies(string assetPath, Brick[] bricks, Dictionary<string, DirectedGraph<string>.Node> existingNodes, DirectedGraph<string> dependencyGraph)
        {
            DirectedGraph<string>.Node node;
            if (!existingNodes.ContainsKey(assetPath))
            {
                node = new DirectedGraph<string>.Node() { data = assetPath };
                existingNodes.Add(assetPath, node);
                dependencyGraph.AddNode(node);
            }
            else
            {
                node = existingNodes[assetPath];
            }

            foreach (var brick in bricks)
            {
                if (PrefabUtility.IsPartOfPrefabInstance(brick))
                {
                    var dependencyAssetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(brick);

                    DirectedGraph<string>.Node dependencyNode;
                    if (!existingNodes.ContainsKey(dependencyAssetPath))
                    {
                        dependencyNode = new DirectedGraph<string>.Node() { data = dependencyAssetPath };
                        existingNodes.Add(dependencyAssetPath, dependencyNode);
                        dependencyGraph.AddNode(dependencyNode);
                    }
                    else
                    {
                        dependencyNode = existingNodes[dependencyAssetPath];
                    }

                    dependencyGraph.AddEdge(dependencyNode, node);
                }
            }
        }

        private static void UpdateConnectivity(Connectivity connectivity)
        {
            Debug.Log($"Updating Connectivity {connectivity.version} -> {currentVersion} on {connectivity.name}");
            PartUtility.UnpackConnectivityForPart(connectivity.name, true);
        }

        private static void UpdateColliders(Colliders colliders)
        {
            Debug.Log($"Updating Colliders {colliders.version} -> {currentVersion} on {colliders.name}");
            PartUtility.UnpackCollidersForPart(colliders.name, true);
        }

        private static void UpdateGeometry(string assetPath)
        {
            string name = Path.GetFileNameWithoutExtension(assetPath);
            bool newPart = assetPath.Contains(PartUtility.newDir);
            bool legacyPart = assetPath.Contains(PartUtility.legacyDir);
            bool commonPart = assetPath.Contains(PartUtility.commonPartsDir);
            bool lightMapped = assetPath.Contains(PartUtility.lightmappedDir);
            int lod = assetPath.Contains(PartUtility.lod0Dir) ? 0 : assetPath.Contains(PartUtility.lod1Dir) ? 1 : 2;

            if (newPart || legacyPart)
            {
                PartUtility.UnpackPart(name, lightMapped, lod, true);
            }
            else if (commonPart)
            {
                PartUtility.UnpackCommonPart(name, lightMapped, lod, true);
            }
        }
    }
}
