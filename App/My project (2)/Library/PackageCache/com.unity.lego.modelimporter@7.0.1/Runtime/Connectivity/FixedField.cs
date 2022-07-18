// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace LEGOModelImporter
{
#if UNITY_EDITOR
    [ExecuteAlways]
#endif
    public class FixedField : ConnectionField
    {
        public FixedFeature feature;
        public FixedField connectedField;

#if UNITY_EDITOR
        private void OnEnable()
        {
            if (connectedField)
            {
                // Re-establish connections in case we already have some.
                // If we have some in Awake, that usually means we have undone a destroy and need
                // to re-establish one-way connections.
                if (connectedField.connectedField != this)
                {
                    connectedField.connectedField = this;
                }
            }
        }

        private void OnDestroy()
        {
            if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                if (connectedField)
                {
                    if (connectedField.connectedField)
                    {
                        var otherField = connectedField;
                        Disconnect(connectedField.feature);
                        Connection.RegisterPrefabChanges(otherField);
                        Connection.RegisterPrefabChanges(this);
                    }
                }
            }
        }
#endif   

        public override HashSet<ConnectionField> DisconnectAll()
        {
            HashSet<ConnectionField> result = new HashSet<ConnectionField>();

            if (!connectedField)
            {
                return result;
            }

            result.Add(connectedField);
            Disconnect((this, connectedField));
            connectedField = null;

            return result;
        }

        public override HashSet<ConnectionField> DisconnectAllInvalid()
        {
            HashSet<ConnectionField> result = new HashSet<ConnectionField>();

            if (!connectedField)
            {
                return result;
            }

            if (!feature.CheckConnectionTransformationValid(connectedField.feature, out _))
            {
                result.Add(connectedField);
                Disconnect((this, connectedField));
                connectedField = null;
            }
            return result;
        }

        public override HashSet<ConnectionField> DisconnectInverse(ICollection<Brick> bricksToKeep)
        {
            HashSet<ConnectionField> result = new HashSet<ConnectionField>();

            if (!connectedField)
            {
                return result;
            }

            if (!bricksToKeep.Contains(connectedField.connectivity.part.brick))
            {
                result.Add(connectedField);
                Disconnect((this, connectedField));
                connectedField = null;
            }
            return result;
        }

        public override HashSet<(Connection, Connection)> QueryConnections(out bool reject, bool bothkinds = false, ICollection<ConnectionField> onlyConnectTo = null)
        {
            LayerMask mask = GetMask(kind, bothkinds);

            HashSet<(Connection, Connection)> validConnections = new HashSet<(Connection, Connection)>();
            reject = false;

            // PhysicsScene
            var physicsScene = gameObject.scene.GetPhysicsScene();

            var size = new Vector3(BrickBuildingUtility.LU_1 * 0.5f, BrickBuildingUtility.LU_1 * .5f, BrickBuildingUtility.LU_1 * 0.5f);
            var hits = physicsScene.OverlapBox(transform.position, size, BrickBuildingUtility.ColliderBuffer, transform.rotation, mask, QueryTriggerInteraction.Collide);
            for (var i = 0; i < hits; i++)
            {
                var overlap = BrickBuildingUtility.ColliderBuffer[i];
                if(overlap.TryGetComponent(out FixedField field))
                {
                    if (field == null || field == this || field.connectivity.part == connectivity.part)
                    {
                        continue;
                    }

                    if (onlyConnectTo != null && !onlyConnectTo.Contains(field))
                    {
                        continue;
                    }

                    if (!feature.CheckConnectionTransformationValid(field.feature, out Connection.ConnectionInteraction fixedMatch))
                    {
                        if (fixedMatch != Connection.ConnectionInteraction.Reject)
                        {
                            continue;
                        }
                        else
                        {
                            reject = true;
                            validConnections.Clear();
                            validConnections.Add((feature, field.feature));
                            return validConnections;
                        }
                    }

                    if (Connection.IsConnectable(fixedMatch))
                    {
                        validConnections.Add((feature, field.feature));
                    }
                }
            }

            return validConnections;
        }

        /// <summary>
        /// Connect this field to another field through a feature
        /// </summary>
        /// <param name="dst">The feature to connect to</param>
        public void Connect(FixedFeature dst)
        {
            if(dst != null && (dst.Field == this || dst.Field == connectedField))
            {
                return;
            }

            if(dst == null)
            {
                if(connectedField)
                {
                    connectedField.connectedField = null;
                    connectedField = null;
                }
                return;
            }

            if (connectedField)
            {
                connectedField.connectedField = null;
            }

            connectedField = dst.Field;
            dst.Field.Connect(feature);
        }

        internal void Disconnect(FixedFeature feature)
        {
            if (connectedField == feature.Field)
            {
                var connected = connectedField.connectedField;
                connectedField.connectedField = null;
                connected.connectedField = null;
            }
        }

        internal static void Disconnect((FixedField, FixedField) toDisconnect)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Undo.RegisterCompleteObjectUndo(toDisconnect.Item1, "Disconnecting fields.");
                Undo.RegisterCompleteObjectUndo(toDisconnect.Item2, "Disconnecting fields.");
            }
#endif
            toDisconnect.Item1.Disconnect(toDisconnect.Item2.feature);
            Connection.RegisterPrefabChanges(toDisconnect.Item1);
            Connection.RegisterPrefabChanges(toDisconnect.Item2);
        }

        internal static bool AlignRotation(Matrix4x4 source, Matrix4x4 destination, FixedFeature.AxisType axisType, out Quaternion resultRotation)
        {
            // If axisType == Mono: Only up needs to match
            // If axisType == Dual: Up and right need to match
            var srcUp = MathUtils.GetColumn(source, 1);

            var dstUp = MathUtils.GetColumn(destination, 1);
            var cosAngle = Vector3.Dot(srcUp, dstUp);

            // Ignore if we need to rotate more than 80 degrees to align up vectors.
            if (cosAngle < BrickBuildingUtility.Cos80)
            {
                resultRotation = Quaternion.identity;
                return false;
            }

            var srcRight = MathUtils.GetColumn(source, 2);

            // Find rotation needed to align up vectors
            Quaternion alignedRotation = Quaternion.FromToRotation(srcUp, dstUp);
            resultRotation = Quaternion.identity;

            if(axisType == FixedFeature.AxisType.Dual)
            {
                // We need to align both up and right, so we align the right vector here
                // Set the rotation to the aligned rotation
                srcRight = alignedRotation * srcRight;

                // Find the rotation needed to align to the destination right
                resultRotation = MathUtils.AlignRotation(destination, srcRight);
            }

            // Combine up-alignment with forward/right alignment
            resultRotation *= alignedRotation;
            return true;
        }

        internal static void GetConnectedTransformation(FixedFeature src, FixedFeature dst, Vector3 pivot, out Vector3 offset, out float angle, out Vector3 axis)
        {
            if (src.field == null || dst.field == null)
            {
                // Unsupported connectivity type
                offset = Vector3.zero;
                angle = 0.0f;
                axis = Vector3.zero;
                return;
            }

            // Find the required rotation for the source to align with the destination
            AlignRotation(src.field.transform.localToWorldMatrix, dst.field.transform.localToWorldMatrix, dst.axisType, out Quaternion rot);
            rot.ToAngleAxis(out angle, out axis);

            // We rotate around a pivot, so we need angle and axis
            var srcPosition = (rot * (src.Field.transform.position - pivot)) + pivot;
            var dstPosition = dst.Field.transform.position;

            // Offset of connections after pivot rotation
            offset = dstPosition - srcPosition;
        }
    }
}
