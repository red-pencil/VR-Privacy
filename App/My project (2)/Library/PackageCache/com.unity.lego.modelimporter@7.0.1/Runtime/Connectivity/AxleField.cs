// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace LEGOModelImporter
{
    #if UNITY_EDITOR
    [ExecuteAlways]
    #endif
    public class AxleField : ConnectionField
    {
        [Serializable]
        public class ConnectionTuple
        {
            public AxleField field;
            public Connection.ConnectionInteraction interaction;
        }

        public bool requireGrabbing;
        public bool startCapped;
        public bool endCapped;
        public bool grabbing;
        public float length;
        public AxleFeature feature;
        public List<ConnectionTuple> connectedTo = new List<ConnectionTuple>();

#if UNITY_EDITOR
        private void OnEnable()
        {
            if (connectedTo.Count > 0)
            {
                foreach (var connection in connectedTo)
                {
                    var connectedField = connection.field;
                    if (connectedField)
                    {
                        if (!connectedField.HasConnectionTo(this))
                        {
                            connectedField.connectedTo.Add(new ConnectionTuple { field = this, interaction = feature.MatchTypes(connectedField.feature) });
                        }
                    }
                }
            }
        }

        void OnDestroy()
        {
            if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                var toDisconnect = new List<ConnectionField>();

                foreach (var connection in connectedTo)
                {
                    toDisconnect.Add(connection.field);
                }

                foreach (AxleField field in toDisconnect)
                {
                    field.Disconnect(feature);
                    Connection.RegisterPrefabChanges(field);
                }
                Connection.RegisterPrefabChanges(this);
            }
        }
#endif
        public override HashSet<(Connection, Connection)> QueryConnections(out bool reject, bool bothkinds = false, ICollection<ConnectionField> onlyConnectTo = null)
        {
            LayerMask mask = GetMask(kind, bothkinds);

            HashSet<(Connection, Connection)> validConnections = new HashSet<(Connection, Connection)>();
            reject = false;

            // PhysicsScene
            var physicsScene = gameObject.scene.GetPhysicsScene();
            var radius = BrickBuildingUtility.LU_1;

            var epsilon = 0.01f;
            var bottom = new Vector3(0.0f, radius + epsilon, 0.0f);
            var top = new Vector3(0.0f, length - radius - epsilon, 0.0f);

            var hits = physicsScene.OverlapCapsule(transform.TransformPoint(bottom), transform.TransformPoint(top), radius, BrickBuildingUtility.ColliderBuffer, mask, QueryTriggerInteraction.Collide);
            for (var i = 0; i < hits; i++)
            {
                var overlap = BrickBuildingUtility.ColliderBuffer[i];
                if(overlap.TryGetComponent(out AxleField field))
                {
                    if (field == null || field == this || field.connectivity.part == connectivity.part)
                    {
                        continue;
                    }

                    if (onlyConnectTo != null && !onlyConnectTo.Contains(field))
                    {
                        continue;
                    }

                    var cosAngle = Vector3.Dot(field.transform.up, transform.up);
                    var sameDir = cosAngle > BrickBuildingUtility.Cos45Epsilon;
                    if (!sameDir && cosAngle > -BrickBuildingUtility.Cos45Epsilon)
                    {
                        continue;
                    }
                    if (!feature.CheckConnectionTransformationValid(field.feature, out Connection.ConnectionInteraction axleMatch))
                    {
                        if (axleMatch != Connection.ConnectionInteraction.Reject)
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

                    if (Connection.IsConnectable(axleMatch))
                    {
                        validConnections.Add((feature, field.feature));
                    }
                }
            }
            return validConnections;
        }

        public override HashSet<ConnectionField> DisconnectAll()
        {
            // Return the fields that were disconnected if needed by caller.
            HashSet<ConnectionField> result = new HashSet<ConnectionField>();

            List<(AxleFeature, AxleFeature)> toBeDisconnected = new List<(AxleFeature, AxleFeature)>();

            foreach (var connection in connectedTo)
            {
                toBeDisconnected.Add((feature, connection.field.feature));
                result.Add(connection.field);
            }
            Disconnect(toBeDisconnected);
            return result;
        }

        public override HashSet<ConnectionField> DisconnectAllInvalid()
        {
            // Return the fields that were disconnected if needed by caller.
            HashSet<ConnectionField> result = new HashSet<ConnectionField>();

            List<(AxleFeature, AxleFeature)> toBeDisconnected = new List<(AxleFeature, AxleFeature)>();

            foreach (var connection in connectedTo)
            {
                if (!feature.CheckConnectionTransformationValid(connection.field.feature, out _))
                {
                    toBeDisconnected.Add((feature, connection.field.feature));
                    result.Add(connection.field);
                }
            }

            Disconnect(toBeDisconnected);
            return result;
        }

        public override HashSet<ConnectionField> DisconnectInverse(ICollection<Brick> bricksToKeep)
        {
            // Return the fields that were disconnected if needed by caller.
            HashSet<ConnectionField> result = new HashSet<ConnectionField>();
            
            List<(AxleFeature, AxleFeature)> toBeDisconnected = new List<(AxleFeature, AxleFeature)>();
            foreach (var connection in connectedTo)
            {
                if (!bricksToKeep.Contains(connection.field.connectivity.part.brick))
                {
                    toBeDisconnected.Add((feature, connection.field.feature));
                    result.Add(connection.field);
                }
            }
            Disconnect(toBeDisconnected);
            return result;
        }

        /// <summary>
        /// Connect this field to another feature
        /// </summary>
        /// <param name="dst">The destination feature</param>
        public void Connect(AxleFeature dst)
        {
            // Ignore if same field
            if (dst != null && this == dst.field)
            {
                return;
            }

            if (HasConnectionTo(dst.Field))
            {
                return;
            }

            if (dst == null)
            {
                foreach (var connected in connectedTo)
                {
                    connected.field.Disconnect(feature);
                }
                return;
            }

            var interaction = feature.MatchTypes(dst);
            connectedTo.Add(new ConnectionTuple { field = dst.Field, interaction = interaction });
            dst.Field.connectedTo.Add(new ConnectionTuple { field = this, interaction = interaction });
        }

        /// <summary>
        /// Get the relative position and rotation of a possible connection
        /// </summary>
        /// <param name="src"></param>
        /// <param name="dst"></param>
        /// <param name="pivot"></param>
        /// <param name="intersectionPoint"></param>
        /// <param name="offset"></param>
        /// <param name="angle"></param>
        /// <param name="axis"></param>
        public static void GetConnectedTransformation(AxleFeature src, AxleFeature dst, Vector3 pivot, Vector3 intersectionPoint, out Vector3 offset, out float angle, out Vector3 axis)
        {
            offset = Vector3.zero;
            angle = 0.0f;
            axis = Vector3.zero;

            if (AlignRotation(src.field.transform.localToWorldMatrix, dst.field.transform.localToWorldMatrix, out Quaternion resultRotation, out _))
            {
                // We rotate around a pivot, so we need angle and axis
                resultRotation.ToAngleAxis(out angle, out axis);
                var srcPosition = (resultRotation * (src.field.transform.position - pivot)) + pivot;
                offset = intersectionPoint - srcPosition;
            }
        }

        /// <summary>
        /// Create a rotation that aligns the orientation of a transform to another
        /// The rotation will not be applied in this function.
        /// </summary>
        /// <param name="source">The transform we want to align</param>
        /// <param name="destination">The transform we want to align to</param>
        /// <param name="resultRotation">Output parameter for the resulting rotation</param>
        /// <returns></returns>
        internal static bool AlignRotation(Matrix4x4 source, Matrix4x4 destination, out Quaternion resultRotation, out bool sameDir)
        {
            var srcForward = MathUtils.GetColumn(source, 0);
            var srcUp = MathUtils.GetColumn(source, 1);
            var srcRight = MathUtils.GetColumn(source, 2);

            var dstUp = MathUtils.GetColumn(destination, 1);

            // Find rotation needed to align up vectors
            var rot = Quaternion.FromToRotation(srcUp, dstUp);
            var cosAngle = Vector3.Dot(srcUp, dstUp);
            sameDir = cosAngle > BrickBuildingUtility.Cos45Epsilon;
            if (!sameDir)
            {
                if (cosAngle > -BrickBuildingUtility.Cos45Epsilon)
                {
                    resultRotation = Quaternion.identity;
                    return false;
                }
                rot = Quaternion.FromToRotation(srcUp, -dstUp);
            }

            // Set the rotation to the aligned rotation
            srcForward = rot * srcForward;
            srcRight = rot * srcRight;

            // Find the rotation needed to align to the destination
            resultRotation = MathUtils.AlignRotation(destination, srcRight, srcForward);

            // Combine up-alignment with forward/right alignment
            resultRotation *= rot;

            return true;
        }

        internal bool HasConnectionTo(AxleField field)
        {
            foreach (var tuple in connectedTo)
            {
                if (tuple.field == field)
                {
                    return true;
                }
            }
            return false;
        }

        public void Disconnect(AxleFeature toDisconnect)
        {
            ConnectionTuple tuple = null;
            foreach (var connection in connectedTo)
            {
                if (connection.field == toDisconnect.field)
                {
                    tuple = connection;
                    break;
                }
            }

            if (tuple == null)
            {
                return;
            }

            connectedTo.Remove(tuple);
            toDisconnect.Field.Disconnect(feature);
        }

        private static void Disconnect(ICollection<(AxleFeature, AxleFeature)> toBeDisconnected)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                HashSet<UnityEngine.Object> toRecord = new HashSet<UnityEngine.Object>();
                foreach (var (c1, c2) in toBeDisconnected)
                {
                    if(!c1.field || !c2.field)
                    {
                        continue;
                    }
                    toRecord.Add(c1.field);
                    toRecord.Add(c2.field);
                }
                Undo.RegisterCompleteObjectUndo(toRecord.ToArray(), "Disconnecting fields.");
            }
#endif
            foreach ((AxleFeature c1, AxleFeature c2) in toBeDisconnected)
            {
                if(!c1.field || !c2.field)
                {
                    continue;
                }

                c1.Field.Disconnect(c2);
                Connection.RegisterPrefabChanges(c1.field);
                Connection.RegisterPrefabChanges(c2.field);
            }
        }
    }
}
