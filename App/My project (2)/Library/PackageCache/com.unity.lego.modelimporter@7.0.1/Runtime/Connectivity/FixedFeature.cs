// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System;
using UnityEngine;

namespace LEGOModelImporter
{
    [Serializable]
    public class FixedFeature : Connection
    {
        public enum AxisType
        {
            Mono, // only the y-axis has to match between features
            Dual  // both the y- and x-axis have to match.
        }

        public int typeId;
        public AxisType axisType;
        public FixedField Field { get { return field as FixedField; } }

        public override ConnectionInteraction MatchTypes(Connection rhs)
        {
            if (rhs is FixedFeature f)
            {
                if (typeId == f.typeId)
                {
                    return ConnectionInteraction.Fixed;
                }
                else
                {
                    return ConnectionInteraction.Reject;
                }
            }
            return ConnectionInteraction.Ignore;
        }

        /// <summary>
        /// Check whether connecting to a feature would be valid
        /// </summary>
        /// <param name="rhs">The feature we are checking</param>
        /// <param name="match">The resulting interaction</param>
        /// <returns>Whether this connection would be valid at this transformation</returns>
        public bool CheckConnectionTransformationValid(FixedFeature rhs, out ConnectionInteraction match)
        {
            if (rhs == null)
            {
                match = ConnectionInteraction.Reject;
                return false;
            }

            match = MatchTypes(rhs);
            if (match == ConnectionInteraction.Reject)
            {
                return false;
            }

            var POS_EPSILON = 0.01f;
            var ROT_EPSILON = 0.95f;

            var rotationAligned = Vector3.Dot(field.transform.up, rhs.field.transform.up) > ROT_EPSILON;

            switch (axisType)
            {
                case AxisType.Dual:
                    rotationAligned = rotationAligned && Vector3.Dot(field.transform.right, rhs.field.transform.right) > ROT_EPSILON;
                    break;
            }

            var lhsPosition = Field.transform.position;
            var rhsPosition = rhs.Field.transform.position;
            return MathUtils.DistanceSquared(lhsPosition, rhsPosition) < POS_EPSILON && rotationAligned;
        }
    }
}