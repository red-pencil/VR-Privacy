// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System;
using UnityEngine;

namespace LEGOModelImporter
{
    [Serializable]
    public class AxleFeature : Connection
    {
        public enum AxelType 
        {
            unusedReceptor,
            unusedConnector,
            roundAxleReceptor,
            roundAxleConnector,
            crossAxleReceptor,
            crossAxleConnector,
            secondaryPinReceptor,
            secondaryPinConnector,
            plateRoundCrossAxleReceptor,
            unusedPlateRoundCrossAxleConnector,
            miniFigNeckReceptor,
            miniFigNeckConnector,
            roundCrossAxleReceptor,
            roundCrossAxleConnector,
            tinyPinReceptor,
            tinyPinConnector,
            unusedCrossAxlePegHoleCapAlignmentReceptor,
            crossAxlePegHoleCapAlignmentConnector,
            unusedRoundAxleReceptorDontRejectSecondaryPinConnector,
            roundAxleConnectorDontRejectSecondaryPinConnector,
            unusedSecondaryPinReceptorDontRejectTinyPinConnector,
            secondaryPinConnectorDontRejectTinyPinConnector
        }

        public AxelType axleType;
        public AxleField Field {get {return field as AxleField;}}

        internal const float CAPPED_EPSILON = 0.6f;
        private const float AXIS_EPSILON = 0.1f;

        ///<summary> 
        /// Connectivity interaction matrix
        /// Generated from connectivity matrix sheet: AxleConnectivityMatrix.xlsx through python script .\load_connectivity_matrix.py.
        /// Last updated on: Wed Dec  9 11:34:42 2020
        ///</summary>
        private const int connectivityMatrixHeight = 22;
        private const int connectivityMatrixWidth = 22;

        private static readonly ConnectionInteraction[] connectivityMatrix = new ConnectionInteraction[connectivityMatrixWidth * connectivityMatrixHeight] {
        ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, 
        ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, 
        ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Hinge, ConnectionInteraction.Reject, ConnectionInteraction.Cylindrical, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Cylindrical, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Cylindrical, ConnectionInteraction.Reject, ConnectionInteraction.Hinge, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, 
        ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Hinge, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Cylindrical, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, 
        ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Prismatic, ConnectionInteraction.Ignore, ConnectionInteraction.Cylindrical, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Cylindrical, 
        ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Cylindrical, ConnectionInteraction.Reject, ConnectionInteraction.Prismatic, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Cylindrical, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Cylindrical, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, 
        ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Cylindrical, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Cylindrical, 
        ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Cylindrical, ConnectionInteraction.Ignore, ConnectionInteraction.Cylindrical, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, 
        ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Cylindrical, ConnectionInteraction.Reject, ConnectionInteraction.Cylindrical, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Cylindrical, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Cylindrical, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, 
        ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, 
        ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Cylindrical, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, 
        ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Cylindrical, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, 
        ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Cylindrical, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Cylindrical, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, 
        ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Cylindrical, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Cylindrical, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Cylindrical, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, 
        ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Cylindrical, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, 
        ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Cylindrical, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, 
        ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, 
        ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Cylindrical, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, 
        ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, 
        ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Hinge, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Cylindrical, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, 
        ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, 
        ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Cylindrical, ConnectionInteraction.Ignore, ConnectionInteraction.Cylindrical, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Ignore, ConnectionInteraction.Reject, 
        };

        public override ConnectionInteraction MatchTypes(Connection rhs)
        {
            var index = (int)axleType + (int)(rhs as AxleFeature).axleType * connectivityMatrixWidth;
            return connectivityMatrix[index];
        }

        /// <summary>
        /// Find the offset this connection should have to give a visual 'click' animation
        /// <paramref name="other"/> determines the direction of the offset in case it is grabbing.
        /// </summary>
        /// <returns>The offset</returns>
        public Vector3 GetPreconnectOffset(AxleFeature other)
        {
            if (Field.grabbing)
            {
                return field.transform.right * 0.1f;
            }
            else if (other.Field.grabbing)
            {
                return -other.field.transform.right * 0.1f;
            }
            
            return Vector3.zero;
        }

        /// <summary>
        /// Check whether connecting to a feature would be valid
        /// </summary>
        /// <param name="rhs">The feature we are checking</param>
        /// <param name="match">The resulting interaction</param>
        /// <returns>Whether this connection would be valid at this transformation</returns>
        public bool CheckConnectionTransformationValid(AxleFeature rhs, out ConnectionInteraction match, bool allowOverlap = false)
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

            if (match == ConnectionInteraction.Ignore)
            {
                return true;
            }

            var connector = Field.kind == ConnectionField.FieldKind.connector ? Field : rhs.Field;
            var receptor = Field.kind == ConnectionField.FieldKind.receptor ? Field : rhs.Field;

            if(!MatchGrabbing(connector, receptor))
            {
                return false;
            }

            // 1. Check if receptor is on the same XZ position in the local space of the connector first, if not -> Invalid
            // 2. Check if receptor is over the edge of the connector (Y-coordinate in the connector space), if not -> Invalid
            // 3. Check if angle of up vector of receptor is not equal to up vector of connector, if not -> Invalid

            var cosAngle = Vector3.Dot(receptor.transform.up, connector.transform.up);
            var sameDir = cosAngle > BrickBuildingUtility.Cos45Epsilon;
            if (!sameDir && cosAngle > -BrickBuildingUtility.Cos45Epsilon)
            {
                return false;
            }

            var receptorBottomConnectorSpace = connector.transform.InverseTransformPoint(receptor.transform.position);        
            
            if (Mathf.Abs(receptorBottomConnectorSpace.x) > AXIS_EPSILON || Mathf.Abs(receptorBottomConnectorSpace.z) > AXIS_EPSILON)
            {
                return false;
            }

            // Both the connector and receptor have a y0 and y1. There only needs to be an overlap, which means either y0 or y1 can be over the edge. They can not both be over the same edge, but they can be 
            // over different edges.
            if (!GetOverlap(receptor, connector, receptor.transform.localToWorldMatrix, connector.transform.localToWorldMatrix, out float overlapCompensation, out _))
            {
                if (Mathf.Abs(overlapCompensation) > AXIS_EPSILON && !allowOverlap)
                {
                    match = ConnectionInteraction.Reject;
                }
                return false;
            }

            if (!allowOverlap && Mathf.Abs(overlapCompensation) > AXIS_EPSILON)
            {
                match = ConnectionInteraction.Reject;
                return false;
            }

            return true;
        }

        internal static bool GetOverlap(AxleField lhs, AxleField rhs, Matrix4x4 lhsTransform, Matrix4x4 rhsTransform, out float cappedCompensation, out float overlap)
        {
            return GetOverlap(lhs, rhs, lhsTransform, rhsTransform, out cappedCompensation, out overlap, Matrix4x4.identity);
        }

        /// <summary>
        /// Function that checks if two axle fields overlap.
        /// Gives both the capped overlap and the general overlap.
        /// </summary>
        /// <param name="lhs">The first axle field</param>
        /// <param name="rhs">The second axle field</param>
        /// <param name="lhsTransform">A matrix that specifies the local to world of lhs</param>
        /// <param name="rhsTransform">A matrix that specifies the local to world of rhs</param>
        /// <param name="cappedCompensation">The amount (in lhs space) you need to compensate to not overlap capped axles</param>
        /// <param name="overlap">The amount the axles overlap (doesn't take capping into account)</param>
        /// <param name="resultSpace">The space you want your capped compensation and overlap to be in</param>
        /// <param name="capped_epsilon">An epsilon for overlap checking</param>
        /// <returns>Whether or not the axles overlap at all</returns>
        internal static bool GetOverlap(AxleField lhs, AxleField rhs, Matrix4x4 lhsTransform, Matrix4x4 rhsTransform, out float cappedCompensation, out float overlap, Matrix4x4 resultSpace, float cappedEpsilon = CAPPED_EPSILON, float overlapEpsilon = CAPPED_EPSILON)
        {
            var lhsInverse = lhsTransform.inverse;            
            var rhsPosition = MathUtils.GetColumn(rhsTransform, 3);
            var rhsUp = MathUtils.GetColumn(rhsTransform, 1);
            var rhsBottomLocal = lhsInverse.MultiplyPoint(rhsPosition);
            var rhsTopLocal = lhsInverse.MultiplyPoint(rhsPosition + rhsUp * rhs.length);
            var rhsBottomY = Mathf.Min(rhsBottomLocal.y, rhsTopLocal.y);
            var rhsTopY = Mathf.Max(rhsBottomLocal.y, rhsTopLocal.y);

            var lhsBottomY = 0.0f;
            var lhsTopY = lhs.length;

            cappedCompensation = 0.0f;

            if (lhs.startCapped)
            {
                // if lhs is startcapped, that means rhsBottomY can not be below 0.0f
                // Debug.Log($" lhs start {rhsBottomY}");
                if(rhsBottomY < 0.0f)
                {
                    // We need to move lhs down the difference
                    cappedCompensation = rhsBottomY;
                }
            }

            if (lhs.endCapped)
            {
                // if lhs is endcapped, that means that rhsTopY can not be above lhsTopY
                // Debug.Log($" lhs end {rhsTopY} {lhsTopY}");
                
                if(rhsTopY > lhsTopY)
                {
                    // Move LHS down the difference
                    cappedCompensation = rhsTopY - lhsTopY;
                }
            }

            if (rhs.startCapped || rhs.endCapped)
            {
                var lhsUp = MathUtils.GetColumn(lhsTransform, 1);
                var cosAngle = Vector3.Dot(lhsUp, rhsUp);
                var sameDir = cosAngle > BrickBuildingUtility.Cos45Epsilon;

                var bottomSubBottom = rhsBottomY - lhsBottomY;
                var topSubTop = rhsTopY - lhsTopY;

                // if rhs is startcapped and
                // - we face the same direction, that means that rhsBottomY can not be larger than lhsBottomY
                // - we face the opposite direction, that means rhsTopY can not be smaller than lhsTopY

                // if rhs is endcapped and
                // - we face the same direction, that means that rhsTopY can not be below lhsTopY
                // - we face the opposite direction, that means that rhsBottomY can not be larger than lhsBottomY

                if (sameDir)
                {
                    if (rhs.startCapped && bottomSubBottom > 0.0f)
                    {
                        cappedCompensation = bottomSubBottom;
                    }
                    else if (rhs.endCapped && topSubTop < 0.0f)
                    {
                        cappedCompensation = topSubTop;
                    }
                }
                else
                {
                    if (rhs.startCapped && topSubTop < 0.0f)
                    {
                        cappedCompensation = topSubTop;
                    }
                    else if (rhs.endCapped && bottomSubBottom > 0.0f)
                    {
                        cappedCompensation = bottomSubBottom;
                    }
                }
            }            

            overlap = 0.0f;

            // Only have epsilon in case there is a cap
            if (Mathf.Abs(cappedCompensation) > cappedEpsilon)
            {
                return false;
            }

            var overlapBottom = Mathf.Max(lhsBottomY, rhsBottomY);
            var overlapTop = Mathf.Min(lhsTopY, rhsTopY);
            overlap = overlapTop - overlapBottom;

            // Compensation is always in lhs space, so we transform it to a result space if one is given
            if (!resultSpace.isIdentity && Mathf.Abs(cappedCompensation) > 0.0f)
            {
                var resultUp = MathUtils.GetColumn(resultSpace, 1);
                var cosAngle = Vector3.Dot(MathUtils.GetColumn(lhsTransform, 1), resultUp);
                var sameDir = cosAngle > BrickBuildingUtility.Cos45Epsilon;
                if (!sameDir)
                {
                    cappedCompensation = -cappedCompensation;
                    overlap = -overlap;
                }
            }

            if (lhsTopY < rhsBottomY - overlapEpsilon || lhsBottomY > rhsTopY + overlapEpsilon)
            {
                return false;
            }

            return true;
        }

        private static bool MatchGrabbing(AxleField connector, AxleField receptor)
        {
            if (!receptor.grabbing && connector.requireGrabbing)
            {
                return false;
            }

            if (connector.startCapped && connector.endCapped && !receptor.grabbing)
            {
                return false;
            }

            return true;
        }
    }
}