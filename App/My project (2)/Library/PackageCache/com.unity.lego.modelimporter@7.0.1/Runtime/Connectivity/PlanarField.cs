// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LEGOModelImporter
{
    #if UNITY_EDITOR
    [ExecuteAlways]
    #endif
    public class PlanarField : ConnectionField
    {
        [Serializable]
        public struct ConnectionTuple
        {
            public PlanarField field;
            public int indexOfConnection;

            public static ConnectionTuple Empty()
            {
                return new ConnectionTuple {field = null, indexOfConnection = -1};
            }

            public void Reset()
            {
                field = null;
                indexOfConnection = -1;
            }

            public bool IsEmpty()
            {
                return field == null || indexOfConnection == -1;
            }
        }

        [HideInInspector] public PlanarFeature[] connections;
        [HideInInspector] public List<int> connected = new List<int>();
        [HideInInspector] public int connectableConnections;
        [HideInInspector] public ConnectionTuple[] connectedTo;

        public Vector2Int gridSize;

#if UNITY_EDITOR
        void OnEnable()
        {
            // Re-establish connections in case we already have some.
            // If we have some in Awake, that usually means we have undone a destroy and need
            // to re-establish one-way connections.
            List<int> markedForRemoval = new List<int>();
            foreach (var connection in connected)
            {
                var connectionTuple = connectedTo[connection];
                var otherField = connectionTuple.field;
                var otherIndex = connectionTuple.indexOfConnection;

                if(!otherField)
                {
                    markedForRemoval.Add(connection);
                    continue;
                }

                if(otherField.connectedTo.Length <= otherIndex || otherIndex == -1)
                {
                    markedForRemoval.Add(connection);
                    continue;
                }

                if (!otherField.connectedTo[otherIndex].field)
                {
                    otherField.connectedTo[otherIndex].field = this;
                    otherField.connectedTo[otherIndex].indexOfConnection = connection;
                    otherField.OnConnect(otherField.connections[otherIndex]);
                    otherField.connections[otherIndex].UpdateKnobsAndTubes();
                    Connection.RegisterPrefabChanges(otherField);
                }
            }

            foreach(int connection in markedForRemoval)
            {
                connected.Remove(connection);
                connections[connection].UpdateKnobsAndTubes();
            }
            Connection.RegisterPrefabChanges(this);
        }

        public static event System.Action<ICollection<ConnectionField>> dirtied;

        void OnDestroy()
        {
            if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                var toRecord = new HashSet<ConnectionField>();
                var toDisconnect = new HashSet<int>();
                foreach (var connection in connected)
                {
                    var otherConnection = connectedTo[connection];
                    if (otherConnection.field)
                    {
                        toRecord.Add(otherConnection.field);
                    }
                    toDisconnect.Add(connection);
                }

                foreach (var connection in toDisconnect)
                {
                    var field = connectedTo[connection].field;
                    if(!field)
                    {
                        connectedTo[connection].Reset();
                        OnDisconnect(connections[connection]);
                    }
                    else
                    {
                        Disconnect(connection, false);
                        Connection.RegisterPrefabChanges(field);
                    }                    
                }

                Connection.RegisterPrefabChanges(this);
                dirtied?.Invoke(toRecord);
            }
        }
#endif

        #region overrides

        public override HashSet<(Connection, Connection)> QueryConnections(out bool reject, bool bothkinds = false, ICollection<ConnectionField> onlyConnectTo = null)
        {
            LayerMask mask = GetMask(kind, bothkinds);

            HashSet<(Connection, Connection)> validConnections = new HashSet<(Connection, Connection)>();
            reject = false;

            // PhysicsScene
            var physicsScene = gameObject.scene.GetPhysicsScene();
            var size = new Vector3((gridSize.x + 1) * BrickBuildingUtility.LU_5, BrickBuildingUtility.LU_1 * .2f, (gridSize.y + 1) * BrickBuildingUtility.LU_5);
            var center = new Vector3((size.x - BrickBuildingUtility.LU_5) * -0.5f, 0.0f, (size.z - BrickBuildingUtility.LU_5) * 0.5f);

            var hits = physicsScene.OverlapBox(transform.TransformPoint(center), size * 0.5f, BrickBuildingUtility.ColliderBuffer, transform.rotation, mask, QueryTriggerInteraction.Collide);
            for (var i = 0; i < hits; i++)
            {
                var overlap = BrickBuildingUtility.ColliderBuffer[i];
                if(overlap.TryGetComponent(out PlanarField field))
                {
                    if (field == null || field == this || field.connectivity.part == connectivity.part)
                    {
                        continue;
                    }

                    if (onlyConnectTo != null && !onlyConnectTo.Contains(field))
                    {
                        continue;
                    }

                    if (Mathf.Abs(Vector3.Dot(field.transform.up, transform.up)) < 0.95f)
                    {
                        continue;
                    }

                    if (!GetOverlap(field, this, field.transform.localToWorldMatrix, transform.localToWorldMatrix, out Vector2Int min, out Vector2Int max))
                    {
                        continue;
                    }

                    var overlapConnections = GetConnectionsOnOverlap(field, this, transform.localToWorldMatrix, min, max, out reject);
                    foreach (var c in overlapConnections)
                    {
                        validConnections.Add(c);
                    }

                    if (reject)
                    {
                        return validConnections;
                    }
                }
                
            }
            return validConnections;
        }

        /// <summary>
        /// Disconnect all connections for this field.
        /// </summary>
        /// <returns>The fields that were disconnected</returns>
        public override HashSet<ConnectionField> DisconnectAll()
        {
            // Return the fields that were disconnected if needed by caller.
            HashSet<ConnectionField> result = new HashSet<ConnectionField>();

            List<(PlanarFeature, PlanarFeature)> toBeDisconnected = new List<(PlanarFeature, PlanarFeature)>();

            foreach (var connection in connected)
            {
                var otherConnection = GetConnection(connection);
                toBeDisconnected.Add((connections[connection], otherConnection));
                result.Add(otherConnection.field);
            }
            Disconnect(toBeDisconnected);

            return result;
        }

        public override HashSet<ConnectionField> DisconnectAllInvalid()
        {
            // Return the fields that were disconnected if needed by caller.
            HashSet<ConnectionField> result = new HashSet<ConnectionField>();

            List<(PlanarFeature, PlanarFeature)> toBeDisconnected = new List<(PlanarFeature, PlanarFeature)>();

            foreach (var connection in connected)
            {
                var otherConnection = GetConnection(connection);
                var conn = connections[connection];
                if (!conn.CheckConnectionTransformationValid(otherConnection, out _))
                {
                    toBeDisconnected.Add((conn, otherConnection));
                    result.Add(otherConnection.field);
                }
            }
            Disconnect(toBeDisconnected);
            return result;
        }

        public override HashSet<ConnectionField> DisconnectInverse(ICollection<Brick> bricksToKeep)
        {
            // Return the fields that were disconnected if needed by caller.
            HashSet<ConnectionField> result = new HashSet<ConnectionField>();

            List<(PlanarFeature, PlanarFeature)> toBeDisconnected = new List<(PlanarFeature, PlanarFeature)>();
            foreach (var connection in connected)
            {
                var connectedTo = GetConnection(connection);
                if (!bricksToKeep.Contains(connectedTo.field.connectivity.part.brick))
                {
                    toBeDisconnected.Add((connections[connection], connectedTo));
                    result.Add(connectedTo.field);
                }
            }
            Disconnect(toBeDisconnected);
            return result;
        }

        #endregion

        /// <summary>
        /// Compute the overlap between two fields in their current transformations
        /// </summary>
        /// <param name="f1">The first field</param>
        /// <param name="f2">The second field</param>
        /// <param name="min">Out parameter for the minimum position of the overlap</param>
        /// <param name="max">Out parameter for the maximum position of the overlap</param>
        /// <returns></returns>
        public static bool GetOverlap(PlanarField f1, PlanarField f2, Matrix4x4 f1Transform, Matrix4x4 f2Transform, out Vector2Int min, out Vector2Int max)
        {
            // The size vectors in their respective local spaces
            var f1Size = new Vector3(f1.gridSize.x, 0.0f, f1.gridSize.y) * BrickBuildingUtility.LU_5;
            var f2Size = new Vector3(f2.gridSize.x, 0.0f, f2.gridSize.y) * BrickBuildingUtility.LU_5;

            // Each corner of the f1 grid in its local space
            var f1_1 = new Vector3(0.0f, 0.0f, 0.0f);
            var f1_2 = new Vector3(-f1Size.x, 0.0f, f1Size.z);
            var f1_3 = new Vector3(-f1Size.x, 0.0f, 0.0f);
            var f1_4 = new Vector3(0.0f, 0.0f, f1Size.z);

            // Each corner of the f2 grid in its local space
            var f2_1 = new Vector3(0.0f, 0.0f, 0.0f);
            var f2_2 = new Vector3(-f2Size.x, 0.0f, f2Size.z);
            var f2_3 = new Vector3(-f2Size.x, 0.0f, 0.0f);
            var f2_4 = new Vector3(0.0f, 0.0f, f2Size.z);

            // For transforming into f1 space later
            var f1Inverse = f1Transform.inverse;

            // Transform f2 corner points into f1 space
            var s1 = f1Inverse.MultiplyPoint(f2Transform.MultiplyPoint(f2_1));
            var s2 = f1Inverse.MultiplyPoint(f2Transform.MultiplyPoint(f2_2));
            var s3 = f1Inverse.MultiplyPoint(f2Transform.MultiplyPoint(f2_3));
            var s4 = f1Inverse.MultiplyPoint(f2Transform.MultiplyPoint(f2_4));

            // Find all overlap corners
            var sMinX = Mathf.Min(s1.x, Mathf.Min(s2.x, Mathf.Min(s3.x, s4.x)));
            var sMinZ = Mathf.Min(s1.z, Mathf.Min(s2.z, Mathf.Min(s3.z, s4.z)));

            var sMaxX = Mathf.Max(s1.x, Mathf.Max(s2.x, Mathf.Max(s3.x, s4.x)));
            var sMaxZ = Mathf.Max(s1.z, Mathf.Max(s2.z, Mathf.Max(s3.z, s4.z)));

            var fMinX = Mathf.Min(f1_1.x, Mathf.Min(f1_2.x, Mathf.Min(f1_3.x, f1_4.x)));
            var fMinZ = Mathf.Min(f1_1.z, Mathf.Min(f1_2.z, Mathf.Min(f1_3.z, f1_4.z)));

            var fMaxX = Mathf.Max(f1_1.x, Mathf.Max(f1_2.x, Mathf.Max(f1_3.x, f1_4.x)));
            var fMaxZ = Mathf.Max(f1_1.z, Mathf.Max(f1_2.z, Mathf.Max(f1_3.z, f1_4.z)));

            if (sMinX > fMaxX || fMinX > sMaxX || sMinZ > fMaxZ || fMinZ > sMaxZ)
            {
                min = Vector2Int.zero;
                max = Vector2Int.zero;
                return false;
            }

            var minX = Mathf.RoundToInt(Mathf.Max(sMinX, fMinX) / BrickBuildingUtility.LU_5);
            var maxX = Mathf.RoundToInt(Mathf.Min(sMaxX, fMaxX) / BrickBuildingUtility.LU_5);
            var minZ = Mathf.RoundToInt(Mathf.Max(sMinZ, fMinZ) / BrickBuildingUtility.LU_5);
            var maxZ = Mathf.RoundToInt(Mathf.Min(sMaxZ, fMaxZ) / BrickBuildingUtility.LU_5);

            min = new Vector2Int(minX, minZ);
            max = new Vector2Int(maxX, maxZ);

            return true;
        }

        /// <summary>
        /// Get the relative position and rotation of a possible connection
        /// </summary>
        /// <param name="src">The feature connecting</param>
        /// <param name="dst">The feature being connected to</param>
        /// <param name="pivot">The pivot we rotate around</param>
        /// <param name="offset">out parameter for the relative position</param>
        /// <param name="angle">out parameter for the angle needed for the rotation</param>
        /// <param name="axis">out parameter for the axis needed for the rotation</param>
        public static void GetConnectedTransformation(PlanarFeature src, PlanarFeature dst, Vector3 pivot, out Vector3 offset, out float angle, out Vector3 axis)
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
            AlignRotation(src.field.transform.localToWorldMatrix, dst.field.transform.localToWorldMatrix, out Quaternion rot);
            rot.ToAngleAxis(out angle, out axis);

            // We rotate around a pivot, so we need angle and axis
            var srcPosition = (rot * (src.GetPosition() - pivot)) + pivot;
            var dstPosition = dst.GetPosition();

            // Offset of connections after pivot rotation
            offset = dstPosition - srcPosition;
        }


        /// <summary>
        /// Get a list of connectable connections that are connected
        /// </summary>
        /// <returns></returns>
        public List<int> GetConnectedConnections()
        {
            return connected;
        }

        /// <summary>
        /// Check if this field has any available connections
        /// </summary>
        /// <returns>Whether or not the connection field has any available connections</returns>
        public bool HasAvailableConnections()
        {
            return connected.Count < connectableConnections;
        }    

        public void OnConnect(PlanarFeature connection)
        {
            var index = connection.index;
            connected.Add(index);
        }

        public void OnDisconnect(PlanarFeature connection)
        {
            var index = connection.index;
            connected.Remove(index);
        }

        internal bool HasConnection(int index)
        {
            if(connectedTo == null) return false;
            var entry = connectedTo[index];
            if(entry.IsEmpty())
            {
                entry.Reset();
                return false;
            }
            return true;
        }

        /// <summary>
        /// Get the connection this connection is connected to
        /// </summary>
        /// <remarks> Assumes that there is a valid connection.</remarks>
        /// <param name="index">The index to the connection to check</param>
        /// <returns>Connection that the given connection is connected to</returns>
        public PlanarFeature GetConnection(int index)
        {
            var entry = connectedTo[index];
            return entry.field?.connections[entry.indexOfConnection];
        }

        /// <summary>
        /// Connect two connections
        /// </summary>
        /// <param name="src">The source connection</param>
        /// <param name="dst">The destination connection</param>
        /// <param name="updateKnobsAndTubes">Whether or not to update knob and tube visibility</param>
        public void Connect(PlanarFeature src, PlanarFeature dst, bool updateKnobsAndTubes = true)
        {
            Connect(src.index, dst, updateKnobsAndTubes);
        }

        /// <summary>
        /// Convert a local 3D position to a 2D/XZ grid position
        /// </summary>
        /// <param name="localPos">The local position to convert</param>
        /// <returns></returns>
        internal static Vector2Int ToGridPos(Vector3 localPos)
        {
            return new Vector2Int(Mathf.RoundToInt(localPos.x / BrickBuildingUtility.LU_5) * -1, Mathf.RoundToInt(localPos.z / BrickBuildingUtility.LU_5));
        }        

        /// <summary>
        /// Given a world position, check if this field has any connections
        /// </summary>
        /// <param name="worldPos">The world position to check</param>
        /// <returns></returns>
        internal PlanarFeature GetConnectionAt(Vector3 worldPos)
        {            
            var localPos = transform.InverseTransformPoint(worldPos);
            return GetConnectionAt(ToGridPos(localPos));
        }

        /// <summary>
        /// Get a connection at the given coordinates, null if there are none
        /// </summary>
        /// <param name="gridPos">Local grid coordinates</param>
        /// <returns></returns>
        internal PlanarFeature GetConnectionAt(Vector2Int gridPos)
        {            
            if(gridPos.x > gridSize.x + 1 || gridPos.y > gridSize.y + 1 ||
                gridPos.x < 0 || gridPos.y < 0)
            {
                return null;
            }
                        
            var index = GetIndex(gridPos);
            if(index >= connections.Length || index < 0)
            {
                return null;
            }
            return connections[index];
        }

        internal int GetIndex(Vector2Int gridPos)
        {
            return gridPos.x + (gridSize.x + 1) * gridPos.y;
        }

        /// <summary>
        /// Get the world position of a connection
        /// </summary>
        /// <param name="connection">The connection</param>
        /// <returns>The world position</returns>
        internal Vector3 GetPosition(PlanarFeature connection)
        {
            var row = connection.index % (gridSize.x + 1);
            var column = connection.index / (gridSize.x + 1);

            var x = row * -BrickBuildingUtility.LU_5;
            var z = column * BrickBuildingUtility.LU_5;
            var worldPos = transform.TransformPoint(new Vector3(x, 0.0f, z));
            return worldPos;
        }

        /// <summary>
        /// Create a rotation that aligns the orientation of a transform to another
        /// The rotation will not be applied in this function.
        /// </summary>
        /// <param name="source">The transform we want to align</param>
        /// <param name="destination">The transform we want to align to</param>
        /// <param name="resultRotation">Output parameter for the resulting rotation</param>
        /// <returns></returns>
        internal static bool AlignRotation(Matrix4x4 source, Matrix4x4 destination, out Quaternion resultRotation)
        {
            var srcForward = MathUtils.GetColumn(source, 0);
            var srcUp = MathUtils.GetColumn(source, 1);
            var srcRight = MathUtils.GetColumn(source, 2);

            var dstUp = MathUtils.GetColumn(destination, 1);
            var cosAngle = Vector3.Dot(srcUp, dstUp);

            // Ignore if we need to rotate more than 80 degrees to align up vectors. 
            if (cosAngle < BrickBuildingUtility.Cos80)
            {
                resultRotation = Quaternion.identity;
                return false;
            }

            // Find rotation needed to align up vectors
            Quaternion alignedRotation = Quaternion.FromToRotation(srcUp, dstUp);

            // Set the rotation to the aligned rotation
            srcForward = alignedRotation * srcForward;
            srcRight = alignedRotation * srcRight;

            // Find the rotation needed to align to the destination
            resultRotation = MathUtils.AlignRotation(destination, srcRight, srcForward);

            // Combine up-alignment with forward/right alignment
            resultRotation *= alignedRotation;
            return true;
        }

        internal static HashSet<(PlanarFeature, PlanarFeature)> GetConnectionsOnOverlap(PlanarField lhs, PlanarField rhs, Matrix4x4 rhsTransformation, Vector2Int min, Vector2Int max, out bool reject)
        {
            reject = false;
            HashSet<(PlanarFeature, PlanarFeature)> validConnections = new HashSet<(PlanarFeature, PlanarFeature)>();
            var rhsInverse = rhsTransformation.inverse;

            for (int xDst = 0; xDst < Mathf.Abs(max.x - min.x) + 1; xDst++)
            {
                for (int zDst = 0; zDst < Mathf.Abs(max.y - min.y) + 1; zDst++)
                {
                    var posLhs = min + new Vector2Int(xDst, zDst);
                    posLhs.x = -posLhs.x;

                    var lhsIndex = lhs.GetIndex(posLhs);
                    var lhsConnection = lhs.connections[lhsIndex];
                    if (lhsConnection != null && !lhsConnection.HasConnection())
                    {
                        var worldPos = lhsConnection.GetPosition();
                        var localPos = rhsInverse.MultiplyPoint(worldPos);
                        var index = rhs.GetIndex(ToGridPos(localPos));

                        if (index >= rhs.connections.Length || index < 0)
                        {
                            continue;
                        }

                        var rhsConnection = rhs.connections[index];
                        if (rhsConnection != null && !rhsConnection.HasConnection())
                        {
                            var pairMatch = lhsConnection.MatchTypes(rhsConnection);
                            if (pairMatch == Connection.ConnectionInteraction.Ignore)
                            {
                                continue;
                            }
                            else if (pairMatch == Connection.ConnectionInteraction.Reject)
                            {
                                reject = true;
                                validConnections.Clear();
                                validConnections.Add((rhsConnection, lhsConnection));
                                return validConnections;
                            }

                            validConnections.Add((rhsConnection, lhsConnection));
                        }
                    }
                }    
            }

            return validConnections;
        }        

        internal void Disconnect(int index, bool updateKnobsAndTubes = true)
        {
            if(index >= connectedTo.Length)
            {
                return;
            }

            var entry = connectedTo[index];
            var otherField = entry.field;            
            var indexOfConnection = entry.indexOfConnection;

            connectedTo[index].Reset();
            OnDisconnect(connections[index]);

            if(!otherField)
            {                
                if(updateKnobsAndTubes)
                {
                    connections[index].UpdateKnobsAndTubes();
                }
                return;
            }            

            otherField.connectedTo[indexOfConnection].Reset();
            otherField.OnDisconnect(otherField.connections[indexOfConnection]);

            if(updateKnobsAndTubes)
            {
                connections[index].UpdateKnobsAndTubes();
                otherField.connections[indexOfConnection].UpdateKnobsAndTubes();
            }
        }

        internal void Disconnect(PlanarFeature connection, bool updateKnobsAndTubes = true)
        {
            if(connection.index >= connections.Length)
            {
                return;
            }
            Disconnect(connection.index, updateKnobsAndTubes);            
        }

        internal void Connect(int src, PlanarFeature dst, bool updateKnobsAndTubes = true)
        {
            // Ignore if this index is out of bounds (could be that it doesn't belong to this field)
            if(src >= connections.Length)
            {
                return;
            }

            var srcConnection = connections[src];            
            // Ignore if same field
            if(dst != null && srcConnection.field == dst.field)
            {
                return;
            }

            var entry = connectedTo[src];            

            if(dst != null && !entry.IsEmpty() && dst.field == entry.field)
            {
                return;
            }

            // Disconnect old connection.
            if(!entry.IsEmpty())
            {
                entry.field.Disconnect(entry.indexOfConnection, updateKnobsAndTubes);
            }

            if(dst == null)
            {
                connectedTo[src].Reset();
                OnDisconnect(srcConnection);
                srcConnection.UpdateKnobsAndTubes();
                Connection.RegisterPrefabChanges(this);
                return;
            }
                     
            var dstIndex = dst.index;

            // If the same connection has already been made, Ignore as well.
            if(entry.field == dst.field && entry.indexOfConnection == dstIndex)
            {
                return;
            }

            connectedTo[src] = new ConnectionTuple{field = dst.Field, indexOfConnection = dstIndex};
            dst.Field.Connect(dstIndex, srcConnection);

            OnConnect(srcConnection);

            if(updateKnobsAndTubes)
            {
                srcConnection.UpdateKnobsAndTubes();
            }
        }

        private static void Disconnect(ICollection<(PlanarFeature, PlanarFeature)> toBeDisconnected, bool updateKnobsAndTubes = true)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                HashSet<UnityEngine.Object> toRecord = new HashSet<UnityEngine.Object>();
                foreach (var (c1, c2) in toBeDisconnected)
                {
                    toRecord.Add(c1.field);
                    if (c1.knob)
                    {
                        toRecord.Add(c1.knob.gameObject);
                    }
                    foreach (var tube in c1.tubes)
                    {
                        if (tube)
                        {
                            toRecord.Add(tube.gameObject);
                        }
                    }
                    toRecord.Add(c2.field);
                    if (c2.knob)
                    {
                        toRecord.Add(c2.knob.gameObject);
                    }
                    foreach (var tube in c2.tubes)
                    {
                        if (tube)
                        {
                            toRecord.Add(tube.gameObject);
                        }
                    }
                }
                Undo.RegisterCompleteObjectUndo(toRecord.ToArray(), "Disconnecting fields.");
            }
#endif
            foreach ((PlanarFeature c1, PlanarFeature c2) in toBeDisconnected)
            {
                c1.Field.Disconnect(c1, updateKnobsAndTubes);
                Connection.RegisterPrefabChanges(c1.field);
                Connection.RegisterPrefabChanges(c2.field);
            }
        }
    }
}
