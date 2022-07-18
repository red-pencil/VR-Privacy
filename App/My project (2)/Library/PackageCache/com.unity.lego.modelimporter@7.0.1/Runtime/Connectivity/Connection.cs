// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace LEGOModelImporter
{
    [Serializable]
    public abstract class Connection
    {        
        public enum ConnectionInteraction
        {
            Reject,     // Impossible connection
            Ignore,     // No interaction
            Hinge,      // Interactions that can hinge
            Fixed,      // No movement allowed in interaction
            Prismatic,  // Degree of freedom in translation in one axis
            Cylindrical // Degree of freedom in rotation in one axis
        }
        
        public static bool IsConnectable(ConnectionInteraction match)
        {
            switch(match)
            {
                case ConnectionInteraction.Hinge:
                case ConnectionInteraction.Fixed:
                case ConnectionInteraction.Prismatic:
                case ConnectionInteraction.Cylindrical:
                return true;
                case ConnectionInteraction.Reject:
                case ConnectionInteraction.Ignore:
                return false;
            }
            return false;
        }

        public ConnectionField field;
        
        public abstract ConnectionInteraction MatchTypes(Connection rhs);

        /// <summary>
        /// Checks whether a connection is valid
        /// Note: You need to place the bricks at the right position yourself.
        ///       This function only checks whether or not given the position the bricks are in, if a connection is valid.
        /// </summary>
        /// <param name="dst">The connection to connect to</param>
        /// <param name="ignoredBricks">Bricks to ignore in collision check</param>
        /// <returns>Whether or not the connection is valid</returns>
        public bool IsConnectionValid(Connection dst, HashSet<Brick> ignoredBricks = null)
        {
            // Make sure types match
            var match = MatchTypes(dst);
            if(match == ConnectionInteraction.Reject)
            {
                return false;
            }

            // Prevent connecting to itself
            if (this == dst)
            {
                return false;
            }        

            var part = field.connectivity.part;
            var brick = part.brick;

            var dstField = dst.field;
            var otherPart = dstField.connectivity.part;

            //FIXME: Can parts connect to themselves?
            if (otherPart == part)
            {
                return false;
            }

            if(brick.colliding || otherPart.brick.colliding)
            {
                return false;
            }

            // Check if we collide with anything
            var parts = brick.parts;
            foreach (var p in parts)
            {
                if (Part.IsColliding(p, brick.transform.localToWorldMatrix, BrickBuildingUtility.ColliderBuffer, out _, ignoredBricks))
                {
                    return false;
                }
            }
            return true;
        }

        public static void RegisterPrefabChanges(UnityEngine.Object changedObject)
        {
#if UNITY_EDITOR
            if(PrefabUtility.IsPartOfPrefabInstance(changedObject))
            {                
                PrefabUtility.RecordPrefabInstancePropertyModifications(changedObject);
            }
#endif
        }
    }
}