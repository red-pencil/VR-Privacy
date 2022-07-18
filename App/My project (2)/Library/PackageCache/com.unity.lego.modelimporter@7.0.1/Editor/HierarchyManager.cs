// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
#if UNITY_2021_2_OR_NEWER
using UnityEditor.SceneManagement;
#else
using UnityEditor.Experimental. SceneManagement;
using UnityEditor.SceneManagement;
#endif

namespace LEGOModelImporter
{
    /// <summary>
    /// Handles all book-keeping for updating the hierarchy
    /// </summary>
    public class HierarchyManager
    {
        private HashSet<Brick> enabledBricks = new HashSet<Brick>();
        private HashSet<Brick> bricksRelatedToDeletedBricks = new HashSet<Brick>();
        private HashSet<Brick> bricksRelatedToMovingBricks = new HashSet<Brick>();
        private HashSet<Brick> changedBricks = new HashSet<Brick>();

        private HashSet<Model> movedBrickModels = new HashSet<Model>();
        private HashSet<Model> deletedBrickModels = new HashSet<Model>();
        private HashSet<Model> modelsToRecompute = new HashSet<Model>();

        private HashSet<Brick> brickParentChanged = new HashSet<Brick>();
        private HashSet<ModelGroup> modelGroupParentChanged = new HashSet<ModelGroup>();

        private HashSet<ModelGroup> movedBrickModelGroups = new HashSet<ModelGroup>();
        private HashSet<ModelGroup> deletedBrickModelGroups = new HashSet<ModelGroup>();
        private HashSet<ModelGroup> modelGroupsToRecompute = new HashSet<ModelGroup>();

        private HashSet<Transform> transformsWithNoChildren = new HashSet<Transform>();
        private List<GameObject> destroyList = new List<GameObject>();

        private bool suspendUpdate = false;
        private bool undoQueued = false;

        // For undo collapse on moving bricks and destroying empty transforms
        private int undoGroupIndex = 0;
        private bool collapseUndo = false;
        private bool brickMoveStarted = false;

        private HashSet<Transform> checkedThisFrame = new HashSet<Transform>();

        public HierarchyManager()
        {
            Brick.onParentChange += OnBrickParentChanged;
            Brick.onEnable += OnBrickEnabled;
            Brick.onDestroy += OnBrickDestroyed;
            Model.onChildrenChange += OnModelChildrenChanged;
            ModelGroup.onParentChange += OnModelGroupParentChanged;
            ModelGroup.onChildrenChange += OnModelGroupChildrenChanged;
            SceneBrickBuilder.bricksMoved += OnBricksMoved;
            SceneBrickBuilder.bricksPlaced += OnBricksPlaced;
            SceneBrickBuilder.startBrickMove += OnStartBrickMove;
            ModelImporter.modelImportStarted += OnModelImportStarted;
            ModelImporter.modelImportEnded += OnModelImportEnded;

            EditorApplication.playModeStateChanged -= PlayModeStateChanged;
            EditorApplication.playModeStateChanged += PlayModeStateChanged;

            Selection.selectionChanged += SelectionChanged;

            PrefabStage.prefabStageClosing += PrefabStageClosing;
            PrefabStage.prefabStageOpened += PrefabStageOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;

            Undo.undoRedoPerformed += UndoRedoPerformed;
        }

        public void Destroy()
        {
            Brick.onParentChange -= OnBrickParentChanged;
            Brick.onEnable -= OnBrickEnabled;
            Brick.onDestroy -= OnBrickDestroyed;
            Model.onChildrenChange -= OnModelChildrenChanged;
            ModelGroup.onParentChange -= OnModelGroupParentChanged;
            ModelGroup.onChildrenChange -= OnModelGroupChildrenChanged;
            SceneBrickBuilder.bricksMoved -= OnBricksMoved;
            SceneBrickBuilder.bricksPlaced -= OnBricksPlaced;
            SceneBrickBuilder.startBrickMove -= OnStartBrickMove;
            ModelImporter.modelImportStarted -= OnModelImportStarted;
            ModelImporter.modelImportEnded -= OnModelImportEnded;

            EditorApplication.playModeStateChanged -= PlayModeStateChanged;

            Selection.selectionChanged -= SelectionChanged;

            PrefabStage.prefabStageClosing -= PrefabStageClosing;
            PrefabStage.prefabStageOpened -= PrefabStageOpened;
            EditorSceneManager.sceneOpened -= OnSceneOpened;

            Undo.undoRedoPerformed -= UndoRedoPerformed;
        }

        public void Update()
        {
            if(suspendUpdate) return; 
            if(SceneBrickBuilder.IsUndoRedoEvent()) return;
            
            if(undoQueued)
            {
                EditorUtility.DisplayDialog("Auto Update Hierarchy Error", "You can't split model groups in the prefab stage", "Ok");
                Undo.PerformUndo();
                undoQueued = false;
                return;
            }

            HandleDeletedBricks();
            BrickParentsChanged();
            ModelGroupParentChanged();
            
            if(enabledBricks.Count > 0)
            {
                ModelGroupUtility.RecomputeHierarchy(enabledBricks);
                enabledBricks.Clear();
            }

            if(SceneBrickBuilder.CurrentSelectionState != SceneBrickBuilder.SelectionState.Moving)
            {
                if (IsModelGroupPrefabRoot() && !IsSceneSingleCluster())
                {
                    // NOTE(Niels): In case we have moved bricks with the transform tools, we might end up here and need to undo.
                    undoQueued = true;
                }
                else
                {
                    // NOTE(Niels): Cache (transform) changed bricks and disconnect all invalid connections.
                    foreach (var changed in SceneBrickBuilder.TransformTracker.Changed)
                    {
                        var brick = SceneBrickBuilder.TransformTracker.GetComponent<Brick>(changed);
                        if (!brick) continue;
                        brick.DisconnectAllInvalid();
                        changedBricks.Add(brick);
                    }

                    // NOTE(Niels): Recompute hierarchy when we are no longer moving bricks, so we don't do it constantly.
                    if (changedBricks.Count > 0 && SceneBrickBuilder.TransformTracker.Changed.Count == 0)
                    {
                        ModelGroupUtility.RecomputeHierarchy(changedBricks);
                        changedBricks.Clear();
                    }
                }
            }


            // NOTE(Niels): Cache models and modelgroups for later for recomputing pivot.
            var activeTransform = Selection.activeTransform;
            if(SceneBrickBuilder.CurrentSelectionState != SceneBrickBuilder.SelectionState.Moving && activeTransform && SceneBrickBuilder.TransformTracker.HasChanged(activeTransform))
            {
                foreach(var transform in Selection.transforms)
                {
                    if(SceneBrickBuilder.TransformTracker.HasChanged(transform))
                    {
                        var modelGroup = transform.GetComponentInParent<ModelGroup>();
                        if(modelGroup)
                        {
                            modelGroupsToRecompute.Add(modelGroup);
                        }

                        var model = transform.GetComponentInParent<Model>();
                        if(model)
                        {
                            modelsToRecompute.Add(model);
                        }
                    }
                }                
            }

            DestroyLoop();

            checkedThisFrame.Clear();

            if(collapseUndo)
            {
                Undo.CollapseUndoOperations(undoGroupIndex);
                collapseUndo = false;
            }
        }

        void DestroyLoop()
        {
            //NOTE(Niels): Keep checking if there are any transforms to destroy, since destroying an object can lead to more child-less objects.
            while(transformsWithNoChildren.Count > 0)
            {
                destroyList.Clear();

                foreach (var transform in transformsWithNoChildren)
                {
                    if (!transform || transform.childCount > 0)
                    {
                        continue;
                    }
                    destroyList.Add(transform.gameObject);
                }
                transformsWithNoChildren.Clear();
                ModelGroupUtility.DestroyObjects(destroyList);
            }
        }

        private void UndoRedoPerformed()
        {
            changedBricks.Clear();
            checkedThisFrame.Clear();
        }

        private void HandleDeletedBricks()
        {
            if(bricksRelatedToDeletedBricks.Count > 0)
            {
                var stillLivingBricks = new HashSet<Brick>();
                foreach(var brick in bricksRelatedToDeletedBricks)
                {
                    if(!brick)
                    {
                        continue;
                    }
                    stillLivingBricks.Add(brick);
                }
                ModelGroupUtility.RecomputeHierarchy(stillLivingBricks);
                bricksRelatedToDeletedBricks.Clear();
            }
            RecomputePivot(deletedBrickModels, deletedBrickModelGroups);
        }

        private bool IsSceneSingleCluster()
        {
            var connectedBricks = SceneBrickBuilder.Bricks[0].GetConnectedBricks();
            connectedBricks.Add(SceneBrickBuilder.Bricks[0]);
            return connectedBricks.Count == SceneBrickBuilder.Bricks.Length;
        }

        private void RecomputePivot(HashSet<Model> models, HashSet<ModelGroup> groups)
        {
            foreach (var modelGroup in groups)
            {
                if (!modelGroup) continue;
                ModelGroupUtility.RecomputePivot(modelGroup);
            }

            foreach (var model in models)
            {
                if (!model) continue;
                ModelGroupUtility.RecomputePivot(model);
            }
            models.Clear();
            groups.Clear();
        }

        private void PrefabStageClosing(PrefabStage stage)
        {
            changedBricks.Clear();
        }

        private void PrefabStageOpened(PrefabStage stage)
        {
            changedBricks.Clear();
        }

        private void OnSceneOpened(Scene scene, OpenSceneMode openMode)
        {
            changedBricks.Clear();
        }

        private HashSet<Brick> GetRelatedBricks(Brick toCheck, bool includeSelf = true)
        {
            var relatedBricks = toCheck.GetConnectedBricks(false);
            if(includeSelf)
            {
                relatedBricks.Add(toCheck);
            }
            return relatedBricks;
        }

        private HashSet<Brick> GetRelatedBricks(IEnumerable<Brick> toCheck, bool includeSelf = true)
        {
            var relatedBricks = new HashSet<Brick>();
            foreach(var b in toCheck)
            {
                relatedBricks.UnionWith(b.GetConnectedBricks(false));
                if(includeSelf)
                {
                    relatedBricks.Add(b);    
                }
            }
            return relatedBricks;
        }

#region Events
        private void SelectionChanged()
        {
            if(suspendUpdate) return;
            if(SceneBrickBuilder.IsUndoRedoEvent()) return;
            RecomputePivot(modelsToRecompute, modelGroupsToRecompute);
        }

        private void PlayModeStateChanged(PlayModeStateChange state)
        {
            if(state == PlayModeStateChange.EnteredPlayMode)
            {
                enabledBricks.Clear();
                suspendUpdate = true;
            }
            else if(state == PlayModeStateChange.EnteredEditMode)
            {
                enabledBricks.Clear();
                suspendUpdate = false;
            }
        }

        private void OnModelImportStarted()
        {
            suspendUpdate = true;
        }

        private void OnModelImportEnded()
        {
            suspendUpdate = false;
        }

        private void OnStartBrickMove(IEnumerable<Brick> bricks)
        {
            if(suspendUpdate) return;
            bricksRelatedToMovingBricks = GetRelatedBricks(bricks);
            undoGroupIndex = Undo.GetCurrentGroup();
            brickMoveStarted = true;
            foreach (var brick in bricks)
            {
                var modelGroup = brick.GetComponentInParent<ModelGroup>();
                if(modelGroup)
                {
                    movedBrickModelGroups.Add(modelGroup);
                }
                var model = brick.GetComponentInParent<Model>();
                if(model && model.pivot != Model.Pivot.Original)
                {
                    movedBrickModels.Add(model);
                }                
            }
        }

        private void OnBricksMoved(IEnumerable<Brick> bricks)
        {
            if(suspendUpdate) return;            
            ModelGroupUtility.RecomputeHierarchy(bricks);
        }

        private bool IsModelGroupPrefabRoot()
        {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                var rootObjects = prefabStage.scene.GetRootGameObjects();
                var rootObject = rootObjects[0];
                if (prefabStage.mode == PrefabStage.Mode.InContext && !prefabStage.IsPartOfPrefabContents(rootObject))
                {
                    var child = rootObjects[0].transform.GetChild(0);
                    if (child)
                    {
                        return child.GetComponent<ModelGroup>() != null;
                    }
                }
                else
                {
                    return rootObject.GetComponent<ModelGroup>() != null;
                }
            }
            return false;
        }

        private void OnBricksPlaced(IEnumerable<Brick> bricks)
        {
            if(suspendUpdate) return;
            if(IsModelGroupPrefabRoot() && !IsSceneSingleCluster())
            {
                undoQueued = true;
                return;
            }

            if (SceneBrickBuilder.CurrentSelectionState == SceneBrickBuilder.SelectionState.Moving)
            {                
                bricksRelatedToMovingBricks.UnionWith(GetRelatedBricks(bricks));
                ModelGroupUtility.RecomputeHierarchy(bricksRelatedToMovingBricks);
                bricksRelatedToMovingBricks.Clear();
            }

            RecomputePivot(movedBrickModels, movedBrickModelGroups);

            if(brickMoveStarted)
            {
                collapseUndo = true;
            }

            brickMoveStarted = false;
        }        

        private void OnBrickDestroyed(Brick destroyedBrick)
        {
            if(suspendUpdate) return;
            if(SceneBrickBuilder.IsUndoRedoEvent()) return;

            enabledBricks.Remove(destroyedBrick);
            changedBricks.Remove(destroyedBrick);

            bricksRelatedToDeletedBricks.UnionWith(GetRelatedBricks(destroyedBrick, false));
            var modelGroup = destroyedBrick.GetComponentInParent<ModelGroup>();
            if(modelGroup)
            {
                deletedBrickModelGroups.Add(modelGroup);
            }
            var model = destroyedBrick.GetComponentInParent<Model>();
            if(model && model.pivot != Model.Pivot.Original)
            {
                deletedBrickModels.Add(model);
            }
        }

        private void OnBrickEnabled(Brick brick)
        {
            if(suspendUpdate) return;
            if(SceneBrickBuilder.IsOverSceneView()) return;
            if(SceneBrickBuilder.SceneViewHasFocus()) return;
            if(SceneBrickBuilder.IsUndoRedoEvent()) return;

            enabledBricks.Add(brick);
        }

        void BrickParentsChanged()
        {
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null && prefabStage.mode == PrefabStage.Mode.InContext)
            {
                brickParentChanged.Clear();
                return;
            }

            if (suspendUpdate)
            {
                brickParentChanged.Clear();
                return;
            }

            if (SceneBrickBuilder.IsUndoRedoEvent())
            {
                brickParentChanged.Clear();
                return;
            }

            foreach (var brick in brickParentChanged)
            {
                if (!brick) continue;
                if (!brick.transform.parent) continue;
                if (checkedThisFrame.Contains(brick.transform)) continue;
                checkedThisFrame.Add(brick.transform);

                // NOTE(Niels): In case the brick has connected bricks, check if they have a different parent
                // If yes, then change that brick's parent to the different parent - it has likely been moved.
                HashSet<Brick> connectedBricks = brick.GetConnectedBricks();
                Transform groupParent = null;
                Transform rootModel = null;
                foreach (var connectedBrick in connectedBricks)
                {
                    var parent = connectedBrick.transform.parent;
                    if (parent && parent != brick.transform.parent)
                    {
                        if (parent.childCount > connectedBricks.Count)
                        {
                            groupParent = ModelGroupUtility.CreateNewDefaultModelGroup(parent.name).transform;
                        }
                        else
                        {
                            groupParent = parent;
                        }
                        rootModel = parent.transform.parent;
                        break;
                    }
                }

                if (groupParent)
                {
                    ModelGroupUtility.SetParent(brick.transform, groupParent);
                    if (rootModel)
                    {
                        ModelGroupUtility.SetParent(groupParent, rootModel);
                    }
                }

                //NOTE(Niels): If the brick has no connected bricks, but the parent either has more than 1 child (thus at least one brick should move parents),
                // or it has no parent in which case we also move. Moving means we have to create a new group.
                Transform brickParent = brick.transform.parent;

                if ((connectedBricks.Count == 0 && brickParent && brickParent.childCount > 1) || !brickParent)
                {
                    var newGroup = ModelGroupUtility.CreateNewDefaultModelGroup(brick.name);
                    ModelGroupUtility.SetParent(brick.transform, newGroup.transform);

                    if (brickParent && brickParent.transform.parent)
                    {
                        ModelGroupUtility.SetParent(newGroup.transform, brickParent.transform.parent);
                    }
                }

                //NOTE(Niels): If the brick has no model in its parent hierarchy, create a model.
                Model parentModel = brick.GetComponentInParent<Model>();
                if (!parentModel)
                {
                    parentModel = ModelGroupUtility.CreateNewDefaultModel(brick.transform.parent.name);
                    ModelGroupUtility.SetParent(brick.transform.parent.transform, parentModel.transform);
                }
            }
            brickParentChanged.Clear();
        }

        private void OnBrickParentChanged(Brick brick)
        {
            if (suspendUpdate) return;
            if (SceneBrickBuilder.IsOverSceneView()) return;
            if (SceneBrickBuilder.SceneViewHasFocus()) return;
            if (SceneBrickBuilder.IsUndoRedoEvent()) return;

            brickParentChanged.Add(brick);         
        }

        private void OnModelChildrenChanged(Model model)
        {
            if(suspendUpdate) return;
            if(SceneBrickBuilder.IsUndoRedoEvent()) return;

            // NOTE(Niels): We have to cache models here, because we might accidentally delete one that will have children while doing hierarchy computations.
            if (model && model.transform && model.transform.childCount == 0)
            {
                if(!SceneBrickBuilder.IsOverSceneView() && !collapseUndo)
                {
                    undoGroupIndex = Undo.GetCurrentGroup();
                    collapseUndo = true;
                }
                transformsWithNoChildren.Add(model.transform);
            }
        }

        private void OnModelGroupChildrenChanged(ModelGroup modelGroup)
        {
            if(suspendUpdate) return;
            if(SceneBrickBuilder.IsUndoRedoEvent()) return;

            // NOTE(Niels): We have to cache groups here, because we might accidentally delete one that will have children while doing hierarchy computations.
            if (modelGroup && modelGroup.transform && modelGroup.transform.childCount == 0)
            {
                if (!SceneBrickBuilder.IsOverSceneView() && !collapseUndo)
                {
                    undoGroupIndex = Undo.GetCurrentGroup();
                    collapseUndo = true;
                }
                transformsWithNoChildren.Add(modelGroup.transform);
            }
        }

        void ModelGroupParentChanged()
        {
            if (suspendUpdate) return;
            if (SceneBrickBuilder.IsUndoRedoEvent()) return;
            if (SceneBrickBuilder.IsOverSceneView()) return;
            if (SceneBrickBuilder.SceneViewHasFocus()) return;

            foreach(var modelGroup in modelGroupParentChanged)
            {
                if (!modelGroup) continue;

                // NOTE(Niels): In case the group is now a child of another group, we should flatten by parenting it to the model in the parent.
                var groupsInParent = modelGroup.GetComponentsInParent<ModelGroup>();
                if (groupsInParent.Length > 1)
                {
                    Model modelInParent = modelGroup.GetComponentInParent<Model>();
                    if (modelInParent.transform == modelGroup.transform.parent)
                    {
                        return;
                    }
                    ModelGroupUtility.SetParent(modelGroup.transform, modelInParent.transform, ModelGroupUtility.UndoBehavior.withoutUndo);
                }
                else // NOTE(Niels): Otherwise we check if there is a model parent, and if not then we create it.
                {
                    Model modelInParent = modelGroup.GetComponentInParent<Model>();
                    if (!modelInParent)
                    {
                        Model newModel = ModelGroupUtility.CreateNewDefaultModel(modelGroup.name);
                        ModelGroupUtility.SetParent(modelGroup.transform, newModel.transform, ModelGroupUtility.UndoBehavior.withoutUndo);
                    }
                }
            }
            modelGroupParentChanged.Clear();
        }

        private void OnModelGroupParentChanged(ModelGroup modelGroup)
        {
            if(suspendUpdate) return;
            if(SceneBrickBuilder.IsUndoRedoEvent()) return;
            if(SceneBrickBuilder.IsOverSceneView()) return;
            if(SceneBrickBuilder.SceneViewHasFocus()) return;
            if(checkedThisFrame.Contains(modelGroup.transform)) return;
            checkedThisFrame.Add(modelGroup.transform);

            if (IsModelGroupPrefabRoot())
            {
                return;
            }

            modelGroupParentChanged.Add(modelGroup);
        }
    }
#endregion
}