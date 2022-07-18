// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System;
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
    public abstract class ConnectionField : MonoBehaviour
    {        
        public enum FieldKind
        {
            connector,
            receptor
        }     
        
        [HideInInspector] public Connectivity connectivity;
        public FieldKind kind;

        /// <summary>
        /// Get name of physics layer of the field kind
        /// </summary>
        /// <param name="kind">The kind to get the physics layer of</param>
        /// <returns></returns>
        public static string GetLayer(FieldKind kind)
        {
            return kind == FieldKind.connector ? BrickBuildingUtility.ConnectivityConnectorLayerName : BrickBuildingUtility.ConnectivityReceptorLayerName;
        }

        public static LayerMask GetMask(FieldKind kind, bool bothKinds = false)
        {
            if (bothKinds)
            {
                return BrickBuildingUtility.ConnectivityMask;
            }
            else
            {
                var opposite = kind == FieldKind.connector ? FieldKind.receptor : FieldKind.connector;
                return opposite == FieldKind.connector ? BrickBuildingUtility.ConnectivityConnectorMask : BrickBuildingUtility.ConnectivityReceptorMask;
            }
        }

        /// <summary>
        /// Query the possible connections for this field
        /// </summary>
        /// <param name="reject">Out parameter that signifies whether or not this is a rejection</param>
        /// <param name="bothkinds">Optional boolean to specify whether we want to check for both connection field kinds</param>
        /// <param name="onlyConnectTo">An optional filter field if you only want to check connections on specific fields</param>
        /// <returns>A list of tuples for the possible connections</returns>
        public abstract HashSet<(Connection, Connection)> QueryConnections(out bool reject, bool bothkinds = false, ICollection<ConnectionField> onlyConnectTo = null);

        /// <summary>
        /// Disconnect all connections for this field.
        /// </summary>
        /// <returns>The fields that were disconnected</returns>
        public abstract HashSet<ConnectionField> DisconnectAll();
        
        /// <summary>
        /// Disconnect all invalid connections for this field.
        /// </summary>
        /// <returns>The fields that were disconnected</returns>
        public abstract HashSet<ConnectionField> DisconnectAllInvalid();       

        /// <summary>
        /// Disconnect from all connections not connected to a list of bricks.
        /// Used to certain cases where you may want to keep connections with a 
        /// selection of bricks.
        /// </summary>
        /// <param name="bricksToKeep">List of bricks to keep connections to</param>
        /// <returns></returns>
        public abstract HashSet<ConnectionField> DisconnectInverse(ICollection<Brick> bricksToKeep);                                  
    }
}