// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using LEGOMaterials;
using UnityEditor;
using System.IO;
#endif

namespace LEGOModelImporter
{

    public class Part : MonoBehaviour
    {
        public int designID;
        public bool legacy;
        public Connectivity connectivity;
        public List<int> materialIDs = new List<int>(); 
        public Colliders colliders;
        public Brick brick;
        public List<Knob> knobs = new List<Knob>();
        public List<Tube> tubes = new List<Tube>();

        static readonly float collisionEpsilon = 0.02f;

        /// <summary>
        /// Check if the part collides with any other part in the scene
        /// </summary>
        /// <param name="part">The part that we want to check collision for</param>
        /// <returns></returns>
        public static bool IsColliding(Part part, Matrix4x4 localToWorld, Collider[] colliders, out int hits, ICollection<Brick> ignoredBricks = null, bool earlyOut = true)
        {
            var partObjectToLocal = Matrix4x4.TRS(part.transform.localPosition, part.transform.localRotation, part.transform.localScale);
            var partToWorld = localToWorld * partObjectToLocal;            
            hits = 0;
            bool colliding = false;
            var outputBuffer = new HashSet<Collider>();
            PhysicsScene physicsScene = part.gameObject.scene.GetPhysicsScene();

            if(!part.colliders)
            {
                return false;
            }

            for(int c = 0; c < part.colliders.colliders.Count; c++)
            {
                Collider collider = part.colliders.colliders[c];
                // FIXME Is there a more elegant way to handle this?
                System.Type colliderType = collider.GetType();
                var colliderObjectToLocal = Matrix4x4.TRS(collider.transform.localPosition, collider.transform.localRotation, collider.transform.localScale);
                var colliderToWorld = partToWorld * colliderObjectToLocal;
                var currentHits = 0;
                if (colliderType == typeof(BoxCollider))
                {
                    var rotation = MathUtils.MatrixToQuaternion(colliderToWorld);
                    BoxCollider boxCollider = (BoxCollider)collider;
                    var center = colliderToWorld.MultiplyPoint(boxCollider.center);
                    currentHits = physicsScene.OverlapBox(center, (boxCollider.size / 2.0f) - Vector3.one * collisionEpsilon, colliders, rotation, BrickBuildingUtility.IgnoreMask, QueryTriggerInteraction.Ignore);
                }
                else if (colliderType == typeof(SphereCollider))
                {
                    SphereCollider sphereCollider = (SphereCollider)collider;
                    var center = colliderToWorld.MultiplyPoint(sphereCollider.center);
                    currentHits = physicsScene.OverlapSphere(center, sphereCollider.radius - collisionEpsilon, colliders, BrickBuildingUtility.IgnoreMask, QueryTriggerInteraction.Ignore);
                }

                if (currentHits > 0)
                {
                    for (int i = 0; i < currentHits; i++)
                    {
                        Collider overlap = colliders[i];
                        // FIXME Possibly need to make this more efficient. Perhaps each collider has a PartCollider component, which can be used to reference the part.
                        Part overlapPart = overlap.GetComponentInParent<Part>();
                        if (overlapPart != null)
                        {
                            if (part == overlapPart)
                            {
                                continue;
                            }

                            if (ignoredBricks != null)
                            {
                                if (ignoredBricks.Contains(overlapPart.brick))
                                {
                                    continue;
                                }
                            }
                            colliding = true;
                            for(var j = 0; j < currentHits; j++)
                            {
                                outputBuffer.Add(colliders[j]);
                            }

                            if(earlyOut)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            var k = 0;
            foreach(var collider in outputBuffer)
            {
                colliders[k++] = collider;
            }

            hits = k;
            return colliding;
        }

        /// <summary>
        /// Disconnect all fields and their connections on this part
        /// </summary>
        public void DisconnectAll()
        {
            if(!connectivity)
            {
                return;
            }

            foreach(var field in connectivity)
            {
                field.DisconnectAll();
            }
        }

        /// <summary>
        /// Disconnect all invalid connections for this part.
        /// </summary>
        public void DisconnectAllInvalid()
        {
            if(!connectivity)
            {
                return;
            }

            foreach (var field in connectivity)
            {
                field.DisconnectAllInvalid();
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
            if(!connectivity)
            {
                return;
            }

            foreach (var field in connectivity)
            {
                field.DisconnectInverse(bricksToKeep);
            }
        }

#if UNITY_EDITOR
        public static Material GetMaterial(int id)
        {
            // FIXME Remove when colour palette experiments are over.
            var useBI = MouldingColour.GetBI();
            var path = MaterialPathUtility.GetPath((MouldingColour.Id)id, false, useBI);
            if (File.Exists(path))
            {
                return AssetDatabase.LoadAssetAtPath<Material>(path);
            }
            else
            {
                path = MaterialPathUtility.GetPath((MouldingColour.Id)id, true, useBI);
                return AssetDatabase.LoadAssetAtPath<Material>(path);
            }
        }
#endif
    }

}