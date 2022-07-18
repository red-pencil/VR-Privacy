// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LEGOModelImporter
{
    public class Connectivity : MonoBehaviour, IEnumerable<ConnectionField>
    {
        [Serializable]
        public class ConnectionIterator : IEnumerable<(Connection, Connection)>
        {
            [HideInInspector] public List<(PlanarFeature, PlanarFeature)> planarFeatures = new List<(PlanarFeature, PlanarFeature)>();
            [HideInInspector] public List<(AxleFeature, AxleFeature)> axleFeatures = new List<(AxleFeature, AxleFeature)>();
            [HideInInspector] public List<(FixedFeature, FixedFeature)> fixedFeatures = new List<(FixedFeature, FixedFeature)>();

            public ConnectionIterator(List<(PlanarFeature, PlanarFeature)> planarConnections, List<(AxleFeature, AxleFeature)> axleConnections, List<(FixedFeature, FixedFeature)> fixedConnections)
            {
                planarFeatures = planarConnections;
                axleFeatures = axleConnections;
                fixedFeatures = fixedConnections;
            }

            public IEnumerator<(Connection, Connection)> GetEnumerator()
            {
                foreach (var feature in planarFeatures)
                {
                    yield return feature;
                }

                foreach (var feature in axleFeatures)
                {
                    yield return feature;
                }

                foreach (var feature in fixedFeatures)
                {
                    yield return feature;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        public int version = 0;
        public Part part;
        public Bounds extents;

        [HideInInspector] public List<PlanarField> planarFields = new List<PlanarField>();
        [HideInInspector] public List<AxleField> axleFields = new List<AxleField>();
        [HideInInspector] public List<FixedField> fixedFields = new List<FixedField>();

        public HashSet<ConnectionField> DisconnectAllInvalid()
        {
            var result = new HashSet<ConnectionField>();

            foreach(var field in this)
            {
                result.UnionWith(field.DisconnectAllInvalid());
            }

            return result;
        }

        public HashSet<ConnectionField> DisconnectAll()
        {
            var result = new HashSet<ConnectionField>();
            foreach (var field in this)
            {
                result.UnionWith(field.DisconnectAll());
            }

            return result;
        }

        public HashSet<ConnectionField> DisconnectInverse(ICollection<Brick> bricksToKeep)
        {
            var result = new HashSet<ConnectionField>();

            foreach (var field in this)
            {
                result.UnionWith(field.DisconnectInverse(bricksToKeep));
            }

            return result;
        }

        public IEnumerator<ConnectionField> GetEnumerator()
        {
            foreach (var field in planarFields)
            {
                yield return field;
            }

            foreach (var field in axleFields)
            {
                yield return field;
            }

            foreach (var field in fixedFields)
            {
                yield return field;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Query the possible connections for a field
        /// </summary>
        /// <param name="reject">Out parameter that signifies the type of match (ignore, reject or connect)</param>
        /// <param name="bothkinds">Optional boolean to specify whether we want to check for both connection field kinds</param>
        /// <param name="onlyConnectTo">An optional filter field if you only want to check connections on specific bricks</param>
        /// <returns>A list of tuples for the possible connections</returns>
        public static HashSet<(Connection, Connection)> QueryConnections(ConnectionField field, out bool reject, HashSet<Brick> onlyConnectTo, bool bothkinds = false)
        {
            HashSet<ConnectionField> onlyConnectToFields = null;
            if(onlyConnectTo != null && onlyConnectTo.Count > 0)
            {
                onlyConnectToFields = new HashSet<ConnectionField>();
                foreach(var brick in onlyConnectTo)
                {
                    foreach(var part in brick.parts)
                    {
                        if(!part.connectivity)
                        {
                            continue;
                        }

                        if (field is PlanarField)
                        {
                            onlyConnectToFields.UnionWith(part.connectivity.planarFields);
                        }
                        else if (field is AxleField)
                        {
                            onlyConnectToFields.UnionWith(part.connectivity.axleFields);
                        }
                        else if(field is FixedField)
                        {
                            onlyConnectToFields.UnionWith(part.connectivity.fixedFields);
                        }
                    }
                }
            }
            return field.QueryConnections(out reject, bothkinds, onlyConnectToFields);
        }

        /// <summary>
        /// Query all fields in this connectivity object for possible connections
        /// </summary>
        /// <param name="reject">Out parameter that tells us whether a connection at the current position should be rejected.</param>
        /// <param name="onlyConnectTo">Optional filter parameter for restricting which bricks we want to connecto.</param>
        /// <returns></returns>
        public ConnectionIterator QueryConnections(out bool reject, ICollection<Brick> onlyConnectTo = null)
        {
            HashSet<ConnectionField> onlyConnectToFields = null;
            if(onlyConnectTo != null && onlyConnectTo.Count > 0)
            {
                onlyConnectToFields = new HashSet<ConnectionField>();
                foreach(var brick in onlyConnectTo)
                {
                    foreach(var part in brick.parts)
                    {
                        if(!part.connectivity)
                        {
                            continue;
                        }
                        onlyConnectToFields.UnionWith(part.connectivity);
                    }
                }
            }
            return QueryConnections(out reject, false, onlyConnectToFields);
        }

        /// <summary>
        /// Query all fields in this connectivity object for possible connections
        /// </summary>
        /// <param name="reject">Out parameter that tells us whether a connection at the current position should be rejected.</param>
        /// <param name="bothKinds">Determines whether we want to query for opposite kind fields.</param>
        /// <param name="onlyConnectTo">Optional filter parameter for restricting which bricks we want to connect to.</param>
        /// <returns></returns>
        public ConnectionIterator QueryConnections(out bool reject, bool bothKinds, ICollection<ConnectionField> onlyConnectTo = null)
        {
            List<(PlanarFeature, PlanarFeature)> planarToBeConnected = new List<(PlanarFeature, PlanarFeature)>();
            List<(AxleFeature, AxleFeature)> axlesToBeConnected = new List<(AxleFeature, AxleFeature)>();
            List<(FixedFeature, FixedFeature)> fixedToBeConnected = new List<(FixedFeature, FixedFeature)>();

            foreach (var field in this)
            {
                // Make a new query for all nearby connections                
                var connections = field.QueryConnections(out reject, bothKinds, onlyConnectTo);
                if (reject)
                {
                    return new ConnectionIterator(planarToBeConnected, axlesToBeConnected, fixedToBeConnected);
                }

                foreach (var connection in connections)
                {
                    if(connection.Item1 is PlanarFeature p1 && connection.Item2 is PlanarFeature p2)
                    {
                        planarToBeConnected.Add((p1, p2));
                    }
                    else if (connection.Item1 is AxleFeature a1 && connection.Item2 is AxleFeature a2)
                    {
                        axlesToBeConnected.Add((a1, a2));
                    }
                    else if(connection.Item1 is FixedFeature f1 && connection.Item2 is FixedFeature f2)
                    {
                        fixedToBeConnected.Add((f1, f2));
                    }
                }
            }

            reject = false;
            return new ConnectionIterator(planarToBeConnected, axlesToBeConnected, fixedToBeConnected);
        }
    }
}