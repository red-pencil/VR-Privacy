// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using LEGOMaterials;
#endif

namespace LEGOModelImporter
{
#if UNITY_EDITOR
    [ExecuteAlways]
#endif
    [SelectionBase]
    public class Brick : MonoBehaviour
    {
        public int designID;
        public string uuid;
        public List<Part> parts = new List<Part>();
        public Bounds totalBounds = new Bounds();
        public bool colliding;
        public static event System.Action<Brick> onEnable;
        public static event System.Action<Brick> onParentChange;
#if UNITY_EDITOR
        public static event System.Action<Brick> onDestroy;        
        private static Material transparentMaterial;

        private Material GetMaterial(int id)
        {
            // FIXME Remove when colour palette experiments are over.
            var useBI = MouldingColour.GetBI();
            var path = MaterialPathUtility.GetPath((MouldingColour.Id)id, false, useBI);
            if(File.Exists(path))
            {
                return AssetDatabase.LoadAssetAtPath<Material>(path);
            }
            else
            {
                path = MaterialPathUtility.GetPath((MouldingColour.Id)id, true, useBI);
                return AssetDatabase.LoadAssetAtPath<Material>(path);
            }                            
        }

        public void SetMaterial(bool ghosted, bool recordUndo = true)
        {
            if(ghosted && transparentMaterial == null)
            {
                transparentMaterial = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.lego.modelimporter/Materials/LEGO_GhostedBrick.mat");;
            }

            foreach(var part in parts)
            {
                if(part.transform.childCount > 0)
                {
                    var colourChangeSurfaces = part.transform.Find("ColourChangeSurfaces");
                    Material material = ghosted ? transparentMaterial : GetMaterial(part.materialIDs[0]);
                    if(!material)
                    {
                        continue;
                    }

                    var renderersToEdit = new List<MeshRenderer>();
                    var colourSurfaceRenderersToEdit = new List<(MeshRenderer, int)>();

                    var shell = part.transform.Find("Shell");
                    if(shell)
                    {
                        var mr = shell.GetComponent<MeshRenderer>();
                        renderersToEdit.Add(mr);
                    }

                    foreach(var knob in part.knobs)
                    {
                        var mr = knob.GetComponent<MeshRenderer>();
                        renderersToEdit.Add(mr);
                    }

                    foreach(var tube in part.tubes)
                    {
                        var mr = tube.GetComponent<MeshRenderer>();
                        renderersToEdit.Add(mr);                            
                    }

                    if(part.materialIDs.Count > 1 && colourChangeSurfaces)
                    {
                        for(var i = 1; i < part.materialIDs.Count; i++)
                        {
                            var surface = colourChangeSurfaces.GetChild(i - 1);
                            if(surface)
                            {
                                var mr = surface.GetComponent<MeshRenderer>();
                                colourSurfaceRenderersToEdit.Add((mr, part.materialIDs[i]));
                            }
                        }
                    }

                    if(recordUndo)
                    {
                        Undo.RegisterCompleteObjectUndo(renderersToEdit.ToArray(), "Recording material changes");
                        Undo.RegisterCompleteObjectUndo(colourSurfaceRenderersToEdit.Select(x => x.Item1).ToArray(), "Recording colour surface material changes");
                    }

                    foreach(var renderer in renderersToEdit)
                    {
                        renderer.sharedMaterial = material;
                    }

                    foreach(var (cs, id) in colourSurfaceRenderersToEdit)
                    {
                        if (ghosted)
                        {
                            cs.sharedMaterial = material;
                        }
                        else
                        {
                            cs.sharedMaterial = GetMaterial(id);
                        }
                    }
                }
            }
        }

        public void UpdateColliding(bool isColliding, bool updateMaterial = true, bool recordUndo = true)
        {
            bool changed = colliding != isColliding;
            if (recordUndo && changed)
            {
                Undo.RegisterCompleteObjectUndo(this, "Changing collision status.");
            }

            colliding = isColliding;

            if (updateMaterial && changed)
            {
                SetMaterial(colliding, recordUndo);
            }
            Connection.RegisterPrefabChanges(this);
        }

        public bool IsColliding(out int hits, HashSet<Brick> ignoredBricks = null, bool earlyOut = true)
        {
            foreach(var part in parts)
            {
                if(Part.IsColliding(part, part.transform.localToWorldMatrix, BrickBuildingUtility.ColliderBuffer, out hits, ignoredBricks, earlyOut))
                {
                    return true;
                }
            }
            hits = 0;
            return false;
        }
#endif

        void OnEnable()
        {
            onEnable?.Invoke(this);
        }

        void OnTransformParentChanged()
        {
            onParentChange?.Invoke(this);
        }

        public HashSet<Brick> GetConnectedBricks(bool recursive = true)
        {
            var connectedBricks = new HashSet<Brick>();
            GetConnectedBricks(this, connectedBricks, recursive);
            return connectedBricks;
        }

        public bool IsLegacy()
        {
            for(int i = 0; i < parts.Count; i++)
            {
                if(parts[i].legacy)
                {
                    return true;
                }
            }
            return false;
        }

        public bool HasConnectivity()
        {
            for(int i = 0; i < parts.Count; i++)
            {
                if(parts[i].connectivity)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Disconnect all fields and their connections on this brick
        /// </summary>
        public void DisconnectAll()
        {
            foreach (var part in parts)
            {
                part.DisconnectAll();                
            }
        }

        /// <summary>
        /// Disconnect all invalid connections for this brick.
        /// </summary>
        public void DisconnectAllInvalid()
        {
            foreach (var part in parts)
            {
                part.DisconnectAllInvalid();  
            }
        }

        /// <summary>
        /// Disconnect from all connections not connected to a list of bricks.
        /// Used to certain cases where you may want to keep connections with a 
        /// selection of bricks.
        /// </summary>
        /// <param name="bricksToKeep">List of bricks to keep connections to</param>
        public void DisconnectInverse(HashSet<Brick> bricksToKeep)
        {
            foreach (var part in parts)
            {
                part.DisconnectInverse(bricksToKeep);
            }
        }

        private void GetConnectedBricks(Brick brick, HashSet<Brick> result, bool recursive)
        {
            foreach (var part in brick.parts)
            {
                if (part.connectivity)
                {
                    foreach(var field in part.connectivity)
                    {
                        if(field is PlanarField pf)
                        {
                            var connected = pf.GetConnectedConnections();
                            foreach (var connection in connected)
                            {
                                var connectedTo = pf.GetConnection(connection);
                                if (connectedTo == null)
                                {
                                    continue;
                                }
                                var connectedBrick = connectedTo.field.connectivity.part.brick;
                                if (connectedBrick && !result.Contains(connectedBrick))
                                {
                                    result.Add(connectedBrick);
                                    if (recursive)
                                    {
                                        GetConnectedBricks(connectedBrick, result, recursive);
                                    }
                                }
                            }
                        }
                        else if(field is AxleField af)
                        {
                            foreach (var connectedTo in af.connectedTo)
                            {
                                var connectedBrick = connectedTo.field.connectivity.part.brick;
                                if (connectedBrick && !result.Contains(connectedBrick))
                                {
                                    result.Add(connectedBrick);
                                    if (recursive)
                                    {
                                        GetConnectedBricks(connectedBrick, result, recursive);
                                    }
                                }
                            }
                        }
                        else if(field is FixedField ff)
                        {
                            if(ff.connectedField)
                            {
                                var connectedBrick = ff.connectedField.connectivity.part.brick;
                                if(connectedBrick && !result.Contains(connectedBrick))
                                {
                                    result.Add(connectedBrick);
                                    if(recursive)
                                    {
                                        GetConnectedBricks(connectedBrick, result, recursive);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

#if UNITY_EDITOR
        void OnDestroy()
        {
            if(!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                onDestroy?.Invoke(this);
            }
        }
#endif
    }
}