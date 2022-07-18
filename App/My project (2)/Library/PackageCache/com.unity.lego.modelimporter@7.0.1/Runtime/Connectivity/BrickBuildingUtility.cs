// Copyright (C) LEGO System A/S - All Rights Reserved
// Unauthorized copying of this file, via any medium is strictly prohibited

using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace LEGOModelImporter
{
    public class BrickBuildingUtility
    {
        private static readonly int ignoreMask = ~LayerMask.GetMask(ConnectivityReceptorLayerName, ConnectivityConnectorLayerName);
        private static readonly Matrix4x4 LU_5_OFFSET = Matrix4x4.Translate(new Vector3(-LU_5, 0.0f, -LU_5));
        private static readonly Collider[] colliderBuffer = new Collider[512];
        private static readonly RaycastHit[] raycastBuffer = new RaycastHit[64];

        public static readonly string ConnectivityConnectorLayerName = "ConnectivityConnector";
        public static readonly string ConnectivityReceptorLayerName = "ConnectivityReceptor";
        public static readonly int ConnectivityConnectorMask = LayerMask.GetMask(ConnectivityConnectorLayerName);
        public static readonly int ConnectivityReceptorMask = LayerMask.GetMask(ConnectivityReceptorLayerName);
        public static readonly int ConnectivityMask = LayerMask.GetMask(ConnectivityConnectorLayerName, ConnectivityReceptorLayerName);

        /// <summary>
        /// One LEGO unit is 8mm, so that corresponds to 0.08 units in Unity units
        /// </summary>
        public const float LU_1 = 0.08f;
        public const float LU_5 = 5 * LU_1;
        public const float LU_10 = 10 * LU_1;

        /// <summary>
        /// Tolerances for determining if a connection is valid
        /// </summary>
        public const float AxleDistanceTolerance = LU_1 * 2;
        public const float FixedDistanceTolerance = LU_10 * 2;

        public const float Cos80 = 0.173612f;
        public const float Cos45Epsilon = 0.69465837f; // Epsilon required to handle randomized rotations
        public const float MaxRayDistance = 250.0f;

        // Defines the number of bricks to consider when finding best connection. Reduce to improve performance.
        public const int DefaultMaxBricksToConsiderWhenFindingConnections = 3;
        
        public static int IgnoreMask => ignoreMask;

        // Defines the order of the neighbouring connection field positions when matching two connection fields.
        public static readonly Vector2[] ConnectionFieldOffsets =
            new Vector2[] {
                        new Vector2(0.0f, 0.0f), // No offset should always be first.
                        new Vector2(0.0f, 1.0f), new Vector2(0.0f, -1.0f),
                        new Vector2(1.0f, 0.0f), new Vector2(-1.0f, 0.0f),
                        new Vector2(1.0f, 1.0f), new Vector2(1.0f, -1.0f),
                        new Vector2(-1.0f, 1.0f), new Vector2(-1.0f, -1.0f)
            };
        public static Collider[] ColliderBuffer => colliderBuffer;
        public static RaycastHit[] RaycastBuffer => raycastBuffer;

        /// <summary>
        /// Align a vector to LU in local space of a building plane.
        /// Offset by LU_5 in local space.
        /// </summary>
        /// <param name="position">The position to align. Is both input and output</param>
        /// <param name="transformation">A matrix whose local space we compute the alignment in</param>
        /// <param name="LU">The LU value to align to. Default value LU_5</param>
        /// <returns>The grid aligned vector aligned to given LU</returns>
        public static void AlignToGrid(ref Vector3 position, Matrix4x4 transformation, float LU = LU_5)
        {
            transformation = LU_5_OFFSET * transformation;
            var localPos = transformation.inverse.MultiplyPoint(position);
            localPos.x = Mathf.Round(localPos.x / LU) * LU;
            localPos.z = Mathf.Round(localPos.z / LU) * LU;
            position = transformation.MultiplyPoint(localPos);
        }

        /// <summary>
        /// Compute a grid-aligned position for the brick on intersecting geometry or on a pre-defined world plane
        /// </summary>
        /// <param name="ray">The ray to shoot into the scene and intersect with a plane</param>
        /// <param name="worldPlane">A fallback plane we want to intersect with/find a new position on if no geometry is hit</param>
        /// <param name="physicsScene">The physics scene we are working in</param>
        /// <param name="collidingHit">Out parameter for a raycast hit</param>
        /// <returns>The grid aligned position aligned to LU_5</returns>
        public static bool GetGridAlignedPosition(Ray ray, Plane worldPlane, PhysicsScene physicsScene, float maxDistance, out RaycastHit collidingHit)
        {
            var hits = physicsScene.Raycast(ray.origin, ray.direction, raycastBuffer, maxDistance, ignoreMask, QueryTriggerInteraction.Ignore);
            if(hits > 0)
            {
                var shortestDistance = float.PositiveInfinity;
                var raycastHit = new RaycastHit();
                var hasHit = false;
                for(var i = 0; i < hits; i++)
                {
                    var hit = raycastBuffer[i];
                    var go = hit.collider.gameObject;
                    var brick = go.GetComponentInParent<Brick>();
                    if(brick == null)
                    {
                        var distance = MathUtils.DistanceSquared(ray.origin, hit.point);
                        if(distance < shortestDistance)
                        {
                            hasHit = true;
                            shortestDistance = distance;
                            raycastHit = hit;
                        }
                    }
                }

                if(hasHit)
                {
                    collidingHit = raycastHit;
                    return true;
                }
            }

            collidingHit = new RaycastHit();
            // Check if we hit the ground
            if (worldPlane.Raycast(ray, out float enter))
            {
                if(enter > maxDistance)
                {
                    return false;
                }
                collidingHit.point = ray.GetPoint(enter);
                collidingHit.distance = enter;
                collidingHit.normal = worldPlane.normal;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check whether a transformation will result in a brick colliding with another brick
        /// </summary>
        /// <param name="brick">The brick to check</param>
        /// <param name="position">The position the brick will be checked in</param>
        /// <param name="rotation">The rotation the brick will be checked in</param>
        /// <param name="ignoreList">Optional list of bricks to ignore in the collision check</param>
        /// <returns></returns>
        public static bool IsCollidingAtTransformation(Brick brick, Matrix4x4 localToWorld, ICollection<Brick> ignoreList = null)
        {
            foreach (var part in brick.parts)
            {
                var isColliding = Part.IsColliding(part, localToWorld, colliderBuffer, out _, ignoreList);
                if (isColliding)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Compute the bounding corners of some bounds in some transformation
        /// </summary>
        /// <param name="bounds">The bounds to get the corners of</param>
        /// <param name="transformation">The transformation to transform the points into</param>
        /// <returns></returns>
        public static Vector3[] GetBoundingCorners(Bounds bounds, Matrix4x4 transformation)
        {
            var min = transformation.MultiplyPoint(bounds.min);
            var max = transformation.MultiplyPoint(bounds.max);

            var p0 = new Vector3(min.x, min.y, min.z);
            var p1 = new Vector3(max.x, min.y, min.z);
            var p2 = new Vector3(min.x, max.y, min.z);
            var p3 = new Vector3(min.x, min.y, max.z);

            var p4 = new Vector3(max.x, max.y, min.z);
            var p5 = new Vector3(max.x, min.y, max.z);
            var p6 = new Vector3(min.x, max.y, max.z);
            var p7 = new Vector3(max.x, max.y, max.z);

            var result = new Vector3[] { p0, p1, p2, p3, p4, p5, p6, p7 };

            return result;
        }

        /// <summary>
        /// Compute a min and max point for an array of points
        /// </summary>
        /// <param name="positions">The array of points</param>
        /// <param name="min">Out parameter for the minimum</param>
        /// <param name="max">Out parameter for the maximum</param>
        public static void GetMinMax(Vector3[] positions, out Vector3 min, out Vector3 max)
        {
            min = positions[0];
            max = positions[0];

            foreach (var position in positions)
            {
                min.x = Mathf.Min(position.x, min.x);
                min.y = Mathf.Min(position.y, min.y);
                min.z = Mathf.Min(position.z, min.z);

                max.x = Mathf.Max(position.x, max.x);
                max.y = Mathf.Max(position.y, max.y);
                max.z = Mathf.Max(position.z, max.z);
            }
        }

        /// <summary>
        /// Compute AABB bounds for a set of bricks
        /// </summary>
        /// <param name="bricks">The set of bricks</param>
        /// <param name="transformation">Transformation matrix to transform all bounds into</param>
        /// <returns>The AABB Bounds object for the bricks</returns>
        public static Bounds ComputeBounds(ICollection<Brick> bricks, Matrix4x4 transformation)
        {
            if(bricks.Count() == 0)
            {
                return new Bounds();
            }
            
            var brickEnumerator = bricks.GetEnumerator();
            brickEnumerator.MoveNext();

            var firstBrick = brickEnumerator.Current;

            // Get the bounding corners in world space
            var corners = GetBoundingCorners(firstBrick.totalBounds, transformation * firstBrick.transform.localToWorldMatrix);

            // Get actual min max in world space
            GetMinMax(corners, out Vector3 min, out Vector3 max);

            var totalBounds = new Bounds
            {
                min = min,
                max = max
            };

            while (brickEnumerator.MoveNext())
            {
                var brick = brickEnumerator.Current;

                corners = GetBoundingCorners(brick.totalBounds, transformation * brick.transform.localToWorldMatrix);
                GetMinMax(corners, out Vector3 minWS, out Vector3 maxWS);

                var bounds = new Bounds
                {
                    min = minWS,
                    max = maxWS
                };
                totalBounds.Encapsulate(bounds);
            }

            return totalBounds;
        }

        /// <summary>
        /// Compute bounds of list of bricks in world space
        /// </summary>
        /// <param name="bricks">The list of bricks</param>
        /// <returns></returns>
        public static Bounds ComputeBounds(ICollection<Brick> bricks)
        {
            return ComputeBounds(bricks, Matrix4x4.identity);
        }

        /// <summary>
        /// Computes the offset required to have all bricks above the plane defined by transformation
        /// </summary>
        /// <param name="focusBrickPosition">The position of the brick everything is relative to</param>
        /// <param name="bricks">The list of bricks to move</param>
        /// <param name="ray">The ray we are shooting into the scene</param>
        /// <param name="pickupOffset">The offset at which the focusBrick has been picked up</param>
        /// <param name="transformation">The transformation we want to do the computation in the space of</param>
        /// <returns></returns>
        public static Vector3 GetOffsetToGrid(Vector3 focusBrickPosition, HashSet<Brick> bricks, Ray ray, Vector3 pickupOffset, Matrix4x4 transformation)
        {
            // Compute the bounds in the local space of transformation
            var boundsLocal = ComputeBounds(bricks, transformation);

            // We only need the minimum
            var boundsMinLocal = boundsLocal.min;

            // Transform all necessary values into local space
            var rayOriginLocal = transformation.MultiplyPoint(ray.origin);
            var rayDirectionLocal = transformation.MultiplyVector(ray.direction);
            var focusPosLocal = transformation.MultiplyPoint(focusBrickPosition);
            var pickupOffsetLocal = transformation.MultiplyVector(pickupOffset);

            // To offset the ray we need to find exactly where we clicked on the brick in local space
            var pickupPosLocal = focusPosLocal + pickupOffsetLocal;
            var offsetToBoundsMin = pickupPosLocal - boundsMinLocal;

            // Offset ray origin
            var offsetOrigin = rayOriginLocal - offsetToBoundsMin;

            // Normally:
            // targetPos.y = rayT * rayDirection.y + offsetRayOrigin.y
            // => rayT = (targetPos.y - offsetRayOrigin.y) / rayDirection.y
            // But since we are always aligning to a local space, we always want y == 0.0f, so targetPos.y cancels out
            // leaving us with -offsetOrigin.y.
            var t = (-offsetOrigin.y) / rayDirectionLocal.y;

            // Compute the new position given t and ray
            var x = rayDirectionLocal.x * t + offsetOrigin.x;
            var z = rayDirectionLocal.z * t + offsetOrigin.z;
            var newPosLocal = new Vector3(x, 0.0f, z);
            
            // Compute offset
            return newPosLocal - boundsMinLocal;
        }

        ///<summary>
        ///Align a group of bricks on any intersecting geometry with a collider. Provide a fallback plane if nothing is hit
        ///</summary>
        ///<param name="sourceBrick">The brick we center this aligning around <paramref name="pickupOffset"/></param>
        ///<param name="bricks">The list of bricks</param>
        ///<param name="bounds">The current world space bounds of the bricks</param>
        ///<param name="pivot">The pivot used for rotating the bricks into an aligned orientation</param>
        ///<param name="pickupOffset">The offset at which we picked up the source brick</param>
        ///<param name="ray">The mouse ray we shoot into the scene</param>
        ///<param name="fallbackWorldPlane">A fallback plane if no geometry is hit</param>
        ///<param name="maxDistance">The max distance we shoot our ray</param>
        ///<param name="offset">Out parameter for the offset needed to place bricks on intersecting geometry</param>
        ///<param name="alignedOffset">Out parameter for the offset aligned to LU_10</param>
        ///<param name="rotation">Out parameter for the rotation needed to align</param>
        ///<param name="hit">Output parameter for hit information</param>
        public static void AlignBricks(Brick sourceBrick, HashSet<Brick> bricks, Bounds bounds, Vector3 pivot,
        Vector3 pickupOffset, Ray ray, Plane fallbackWorldPlane, float maxDistance, out Vector3 alignedOffset, out Vector3 offset, out Vector3 prerotateOffset, out Quaternion rotation, out RaycastHit hit)
        {
            // Steps for placing selected bricks on intersecting geometry:
            // 1. Find a hit point (Raycast) on either geometry or fallback plane
            // 3. Find the closest axis of the focusbrick to the normal of the plane
            // 4. Orient all bricks around a pivot so that the found axis of the focusbrick aligns with the plane normal
            // 5. Compute bounds in the space of the plane (So they are properly aligned)
            // 6. Find out how much we need to offset to get above the grid/hit plane

            if(GetGridAlignedPosition(ray, fallbackWorldPlane, sourceBrick.gameObject.scene.GetPhysicsScene(), maxDistance, out hit))
            {
                // Any hit will have a normal (either geometry hit or fallback plane)
                var normal = hit.normal;

                // Check all possible world axes of source brick transform and find the one that is
                // closest (by angle) to the plane normal

                // 1. Get rotation to align up with normal
                // 2. Cache rotation and apply aligned rotation to sourceBrick temporarily
                // 3. Get rotation needed to align with forward/right vectors
                // 4. Revert sourceBrick to cached rotation

                var localOffset = sourceBrick.transform.InverseTransformDirection(pickupOffset);

                // Compute the rotation required to get the closest axis aligned to the plane normal
                var closestAxis = MathUtils.FindClosestAxis(sourceBrick.transform, normal, out MathUtils.VectorDirection vectorDirection);
                Quaternion rot = Quaternion.FromToRotation(closestAxis, normal);

                var axesToAlign = MathUtils.GetRelatedAxes(sourceBrick.transform, vectorDirection);
                axesToAlign.Item1 = rot * axesToAlign.Item1;
                axesToAlign.Item2 = rot * axesToAlign.Item2;

                // Compute the transformation matrix for the plane
                Vector3 origin;

                Vector3 up;
                Vector3 right;
                Vector3 forward;

                if(hit.collider != null)
                {
                    var hitTransform = hit.collider.transform;

                    // Find the axis closest to the normal on the transform
                    // to make sure we don't choose to align all bricks to the normal again
                    var hitRightAngle = Vector3.Angle(normal, hitTransform.right);
                    var hitUpAngle = Vector3.Angle(normal, hitTransform.up);
                    var hitForwardAngle = Vector3.Angle(normal, hitTransform.forward);
                    var hitLeftAngle = Vector3.Angle(normal, -hitTransform.right);
                    var hitDownAngle = Vector3.Angle(normal, -hitTransform.up);
                    var hitBackAngle = Vector3.Angle(normal, -hitTransform.forward);

                    var transformAxes = new List<Vector3>();

                    // Align the rotation of the transform to the normal
                    if (hitRightAngle <= hitUpAngle && hitRightAngle <= hitForwardAngle && hitRightAngle <= hitLeftAngle &&
                    hitRightAngle <= hitDownAngle && hitRightAngle <= hitBackAngle)
                    {
                        // normal points right
                        var fromTo = Quaternion.FromToRotation(hitTransform.right, normal);
                        transformAxes.Add(fromTo * hitTransform.up);
                        transformAxes.Add(fromTo * hitTransform.forward);
                    }
                    else if (hitUpAngle <= hitRightAngle && hitUpAngle <= hitForwardAngle && hitUpAngle <= hitLeftAngle &&
                    hitUpAngle <= hitDownAngle && hitUpAngle <= hitBackAngle)
                    {
                        // normal points up
                        transformAxes.Add(hitTransform.right);
                        transformAxes.Add(hitTransform.forward);
                    }
                    else if (hitForwardAngle <= hitRightAngle && hitForwardAngle <= hitUpAngle && hitForwardAngle <= hitLeftAngle &&
                    hitForwardAngle <= hitDownAngle && hitForwardAngle <= hitBackAngle)
                    {
                        // normal points forward
                        var fromTo = Quaternion.FromToRotation(hitTransform.forward, normal);
                        transformAxes.Add(fromTo * hitTransform.up);
                        transformAxes.Add(fromTo * hitTransform.right);
                    }
                    else if (hitLeftAngle <= hitUpAngle && hitLeftAngle <= hitForwardAngle && hitLeftAngle <= hitRightAngle &&
                    hitLeftAngle <= hitDownAngle && hitLeftAngle <= hitBackAngle)
                    {

                        // normal points left
                        var fromTo = Quaternion.FromToRotation(-hitTransform.right, normal);
                        transformAxes.Add(fromTo * -hitTransform.up);
                        transformAxes.Add(fromTo * -hitTransform.forward);
                    }
                    else if (hitDownAngle <= hitRightAngle && hitDownAngle <= hitForwardAngle && hitDownAngle <= hitLeftAngle &&
                    hitDownAngle <= hitUpAngle && hitDownAngle <= hitBackAngle)
                    {
                        // normal points down
                        var fromTo = Quaternion.FromToRotation(-hitTransform.up, normal);
                        transformAxes.Add(fromTo * -hitTransform.right);
                        transformAxes.Add(fromTo * -hitTransform.forward);
                    }
                    else if (hitBackAngle <= hitRightAngle && hitBackAngle <= hitUpAngle && hitBackAngle <= hitLeftAngle &&
                    hitBackAngle <= hitDownAngle && hitBackAngle <= hitForwardAngle)
                    {
                        // normal points back
                        var fromTo = Quaternion.FromToRotation(-hitTransform.forward, normal);
                        transformAxes.Add(fromTo * -hitTransform.up);
                        transformAxes.Add(fromTo * -hitTransform.right);
                    }

                    up = normal * hitTransform.localScale.y;
                    right = transformAxes[0] * hitTransform.localScale.x;
                    forward = transformAxes[1] * hitTransform.localScale.z;

                    var m = Matrix4x4.TRS(hitTransform.position, Quaternion.identity, Vector3.one);
                    m.SetColumn(0, right);
                    m.SetColumn(1, up);
                    m.SetColumn(2, forward);

                    rot = MathUtils.AlignRotation(m, axesToAlign.Item1, axesToAlign.Item2) * rot;

                    // We want to find a common origin for all bricks hitting this specific transform
                    // The origin needs to align with some common point. It doesn't matter what that is, as long
                    // as it is common for this specific transform.
                    // The alignment should only happen in the XZ plane of the transform, which means alignment
                    // on the local Y-axis is always 0 in plane space.

                    var localOrigin = m.inverse.MultiplyPoint(hitTransform.position);
                    var localHit = m.inverse.MultiplyPoint(hit.point);

                    localOrigin.y = localHit.y;
                    origin = m.MultiplyPoint(localOrigin);
                }
                else
                {
                    rot = MathUtils.AlignRotation(Matrix4x4.identity, axesToAlign.Item1, axesToAlign.Item2) * rot;
                    forward = Vector3.forward;
                    up = Vector3.up;
                    right = Vector3.right;
                    origin = new Vector3(0.0f, hit.point.y, 0.0f);
                }

                var planeTRS = Matrix4x4.TRS(origin, Quaternion.identity, Vector3.one);
                planeTRS.SetColumn(0, right.normalized);
                planeTRS.SetColumn(1, up.normalized);
                planeTRS.SetColumn(2, forward.normalized);

                prerotateOffset = GetOffsetToGrid(sourceBrick.transform.position, bricks, ray, pickupOffset, planeTRS.inverse);
                prerotateOffset = planeTRS.MultiplyVector(prerotateOffset);

                rotation = rot;
                rot.ToAngleAxis(out float angle, out Vector3 axis);

                var oldPositions = new List<Vector3>();
                var oldRotations = new List<Quaternion>();

                foreach (var brick in bricks)
                {
                    oldPositions.Add(brick.transform.position);
                    oldRotations.Add(brick.transform.rotation);

                    brick.transform.RotateAround(pivot, axis, angle);
                }

                var worldPickup = sourceBrick.transform.TransformDirection(localOffset);

                // Now compute how much we need to offset the aligned brick selection to get it above the intersected area
                offset = GetOffsetToGrid(sourceBrick.transform.position, bricks, ray, worldPickup, planeTRS.inverse);

                // Find out how far we are dragging the bricks along the ray to align with the plane.
                var localBoundsMin = planeTRS.inverse.MultiplyPoint(bounds.min);
                var boundsPos = localBoundsMin + offset;

                // If it is too far, drag it along the normal instead.
                if(Vector3.Distance(localBoundsMin, boundsPos) > 10.0f)
                {
                    var newRay = new Ray(hit.point + normal * 20.0f, -normal);
                    offset = GetOffsetToGrid(sourceBrick.transform.position, bricks, newRay, worldPickup, planeTRS.inverse);
                }

                // Transform to world space
                offset = planeTRS.MultiplyVector(offset);

                var newPos = offset + sourceBrick.transform.position;
                
                AlignToGrid(ref newPos, planeTRS, LU_10);
                alignedOffset = newPos - sourceBrick.transform.position;

                ResetTransformations(bricks, oldPositions, oldRotations);
            }
            else
            {
                // In case there was no hit, get the offset required to place the bricks at a fixed distance along the ray
                var pointOnRay = ray.GetPoint(maxDistance);
                offset = pointOnRay - sourceBrick.transform.position;
                prerotateOffset = pointOnRay - sourceBrick.transform.position;
                AlignToGrid(ref pointOnRay, Matrix4x4.identity, LU_10);
                alignedOffset = pointOnRay - sourceBrick.transform.position;
                rotation = Quaternion.identity;
            }
        }

        private static List<Brick> CastBrick(HashSet<Brick> castBricks, Ray ray, Brick[] bricksToCheck)
        {
            List<Brick> bricks = new List<Brick>();

            //Notes:
            // Create the cone from the ray and total brick bounds of the selection.
            // "Cast" the cone into space.
            // For each brick in the scene, check its bounding sphere against the cone.
            // For each brick that is within the cone, add it to the list.
            // Return list.

            // FIXME Consider using a truncated cone as we can get intersections with stuff that is not visible due to being closer than the camera's near clip plane.

            // Create cone.
            var totalBounds = ComputeBounds(castBricks);
            // Offset cone origin slightly forward to avoid intersecting with bricks too close to the origin of the ray.
            var coneOrigin = ray.origin + ray.direction * 1.0f;

            var radius = totalBounds.size.magnitude * 0.5f;
            float t = 0.0f;
            Vector3 q = Vector3.zero;
            if(!MathUtils.ClosestPtRaySphere(new Ray(coneOrigin, ray.direction), totalBounds.center, radius, ref t, ref q))
            {
                return bricks;
            }
            
            // For a ray/cone axis r = origin + direction * t, where t is the distance to the center of the brick's bounding volume, the radius of the cone should be the extents of the brick to subtend.
            var tanAngle = radius / t;
            var angle = Mathf.Atan(tanAngle);
            MathUtils.Cone cone = new MathUtils.Cone(coneOrigin, ray.direction, angle);

            // Cast into space and check bricks for overlap.
            foreach (var sceneBrick in bricksToCheck)
            {
                if(!sceneBrick.HasConnectivity())
                {
                    continue;
                }

                if (castBricks.Contains(sceneBrick))
                {
                    continue;
                }

                var totalBoundsForBrick = sceneBrick.totalBounds;

                if (totalBoundsForBrick.size.magnitude == 0)
                {
                    continue;
                }

                var sphereCenter = sceneBrick.transform.TransformPoint(totalBoundsForBrick.center);
                if (MathUtils.SphereIntersectCone(totalBoundsForBrick.size.magnitude * 0.5f, sphereCenter, cone))
                {
                    bricks.Add(sceneBrick);
                }
            }
            return bricks;
        }

        /// <summary>
        /// Connect to fields through a src and dst connection
        /// </summary>
        /// <param name="src">The feature connecting</param>
        /// <param name="dst">The feature being connected to</param>
        /// <param name="onlyConnectTo">Only connect to these fields</param>
        /// <param name="ignoreForCollision">Ignore these fields when checking for collision</param>
        /// <param name="recordUndo">Editor only flag, whether or not to record undo for this action</param>
        /// <returns>A list of fields that have been connected to</returns>
        public static HashSet<ConnectionField> Connect(Connection src, Connection dst, HashSet<Brick> onlyConnectTo = null, HashSet<Brick> ignoreForCollision = null, bool recordUndo = true)
        {
            // Return the fields that were connected if needed by caller.
            HashSet<ConnectionField> result = new HashSet<ConnectionField>();

            if (src == null || dst == null)
            {
                return result;
            }

            //FIXME: Is this even possible if we have non-null connections?
            if (src.field == null || dst.field == null)
            {
                // Unsupported field types
                return result;
            }

            if (!src.IsConnectionValid(dst, ignoreForCollision))
            {
                // Connection is invalid: Mismatched connection types or collision
                return result;
            }

            var dstField = dst.field;
            var srcField = src.field;

            var connections = srcField.connectivity.QueryConnections(out _, onlyConnectTo);

            if (src is PlanarFeature)
            {
                connections.planarFeatures.Add((src as PlanarFeature, dst as PlanarFeature));
            }
            else if (src is AxleFeature)
            {
                connections.axleFeatures.Add((src as AxleFeature, dst as AxleFeature));
            }
            else if(src is FixedFeature)
            {
                connections.fixedFeatures.Add((src as FixedFeature, dst as FixedFeature));
            }

#if UNITY_EDITOR
            if(!EditorApplication.isPlayingOrWillChangePlaymode && recordUndo)
            {
                HashSet<Object> toRecord = new HashSet<Object>();
                foreach (var (c1, c2) in connections.planarFeatures)
                {
                    toRecord.Add(c1.Field);
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

                    toRecord.Add(c2.Field);
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
                
                foreach(var (c1, c2) in connections.axleFeatures)
                {
                    toRecord.Add(c1.Field);
                    toRecord.Add(c2.Field);
                }

                foreach(var (c1, c2) in connections.fixedFeatures)
                {
                    toRecord.Add(c1.Field);
                    toRecord.Add(c2.Field);
                }
                Undo.RegisterCompleteObjectUndo(toRecord.ToArray(), "Connecting two connections.");
            }
#endif
            foreach (var (c1, c2) in connections.planarFeatures)
            {
                if(c1.field.connectivity.part.brick.colliding || c2.field.connectivity.part.brick.colliding)
                {
                    continue;
                }
                c1.Field.Connect(c1, c2);
                Connection.RegisterPrefabChanges(c1.field);
                Connection.RegisterPrefabChanges(c2.field);
            }

            foreach(var (c1, c2) in connections.axleFeatures)
            {
                if (c1.field.connectivity.part.brick.colliding || c2.field.connectivity.part.brick.colliding)
                {
                    continue;
                }
                c1.Field.Connect(c2);
                Connection.RegisterPrefabChanges(c1.field);
                Connection.RegisterPrefabChanges(c2.field);
            }

            foreach(var (c1, c2) in connections.fixedFeatures)
            {
                if (c1.field.connectivity.part.brick.colliding || c2.field.connectivity.part.brick.colliding)
                {
                    continue;
                }
                c1.Field.Connect(c2);
                Connection.RegisterPrefabChanges(c1.field);
                Connection.RegisterPrefabChanges(c2.field);
            }

            return result;
        }

        /// <summary>
        /// Finds the connection that best fits the given brick in a scene consisting of a list of given bricks
        /// </summary>
        /// <param name="pickupOffset">The offset at which we picked up the brick selection.false Relative to a single brick.</param>
        /// <param name="selectedBricks">The bricks we want to find a connection for</param>
        /// <param name="ray">The mouse ray for casting the bricks into the scene</param>
        /// <param name="viewTransform">The eye transformation matrix</param>
        /// <param name="bricksToCheck">A list of bricks we want to be able to connect to</param>
        /// <param name="maxTries">Optional parameter for amount of tries in finding a good connection</param>
        /// <returns></returns>
        public static bool FindBestConnection(Vector3 pickupOffset, HashSet<Brick> selectedBricks, Ray ray, Matrix4x4 viewTransform, Brick[] bricksToCheck, out ConnectionResult[] result, int maxTries = DefaultMaxBricksToConsiderWhenFindingConnections)
        {
            var bricksWithConnectivity = new HashSet<Brick>();
            foreach (var brick in selectedBricks)
            {
                if (!brick.HasConnectivity())
                {
                    continue;
                }
                bricksWithConnectivity.Add(brick);
            }

            // Cast the brick into the scene as a cone with a radius approximately the size of the bounds of the brick
            var possibleBricks = CastBrick(bricksWithConnectivity, ray, bricksToCheck);
            result = null;

            if (possibleBricks.Count == 0)
            {
                return false;
            }

            var inverseViewTransform = viewTransform.inverse;

            // Sort the list of bricks by their z-distance in camera space.
            possibleBricks.Sort((b1, b2) =>
            {
                var p1 = inverseViewTransform.MultiplyPoint(b1.transform.TransformPoint(b1.totalBounds.center));
                var p2 = inverseViewTransform.MultiplyPoint(b2.transform.TransformPoint(b2.totalBounds.center));

                return p1.z.CompareTo(p2.z);
            });

            HashSet<ConnectionField> selectedFields = new HashSet<ConnectionField>();

            foreach (var selectedBrick in bricksWithConnectivity)
            {
                foreach (var part in selectedBrick.parts)
                {
                    if (!part.connectivity)
                    {
                        continue;
                    }

                    foreach (var field in part.connectivity)
                    {
                        if (field is PlanarField pf && !pf.HasAvailableConnections())
                        {
                            continue;
                        }
                        else if (field is FixedField ff && ff.connectedField)
                        {
                            continue;
                        }
                        selectedFields.Add(field);
                    }
                }
            }

            if (selectedFields.Count == 0)
            {
                return false;
            }

            // Run through every brick we may connect to.
            // We need to consider more than just the closest brick since the sorting of possible bricks is not perfect.
            var maxSqr = float.PositiveInfinity;

            var tries = 0;
            var candidates = new ConnectionResult[maxTries];
            foreach (var brick in possibleBricks)
            {
                var currentIntersectionPoint = Vector3.zero;
                if (FindBestConnectionOnBrick(selectedBricks, selectedFields, pickupOffset, brick, ray, maxSqr, out ConnectionResult currentResult))
                {
                    candidates[tries++] = currentResult;
                    maxSqr = currentResult.maxSqrDistance;
                }

                // Stop when having found enough candidates.
                if (tries >= maxTries)
                {
                    break;
                }
            }
            result = candidates;
            return tries > 0;
        }

        /// <summary>
        /// Align the transformations of a list of bricks relative to a brick being transformed
        /// </summary>
        /// <param name="selectedBricks">The list of bricks to transform</param>
        /// <param name="pivot">The pivot for the rotation</param>
        /// <param name="axis">The axis we are rotating around</param>
        /// <param name="angle">The angle we are rotating by</param>
        /// <param name="offset">The offset we are moving the brick</param>
        public static void AlignTransformations(HashSet<Brick> selectedBricks, Vector3 pivot, Vector3 axis, float angle, Vector3 offset)
        {
            foreach(var selected in selectedBricks)
            {
                selected.transform.RotateAround(pivot, axis, angle);
                selected.transform.position += offset;
            }
        }

        /// <summary>
        /// Convenience struct for connection results to simplify API
        /// </summary>
        public struct ConnectionResult
        {
            public Connection srcConnection;
            public Connection dstConnection;
            public Connection.ConnectionInteraction interaction;
            public Vector3 intersectionPoint; // For axle features
            public float maxSqrDistance; // Can be used in comparisons

            public float angleToConnect;
            public Vector3 rotationAxisToConnect;
            public Vector3 connectionOffset;
            public bool colliding;
      
            public static ConnectionResult Empty()
            {
                ConnectionResult result = new ConnectionResult{};
                result.srcConnection = null;
                result.dstConnection = null;
                result.interaction = Connection.ConnectionInteraction.Ignore;
                result.intersectionPoint =  Vector3.zero;
                result.maxSqrDistance = float.PositiveInfinity;
                result.angleToConnect = 0.0f;
                result.rotationAxisToConnect = Vector3.zero;
                result.connectionOffset = Vector3.zero;
                result.colliding = false;
                return result;
            }

            public bool IsEmpty()
            {
                return (srcConnection == null && dstConnection == null) || (srcConnection != null && !srcConnection.field) || (dstConnection != null && !dstConnection.field);
            }
        }

        static List<Vector3> oldPositions = new List<Vector3>();
        static List<Quaternion> oldRotations = new List<Quaternion>();

        public static bool CheckConnectionCandidate(ConnectionField sourceField, ConnectionField destinationField, HashSet<Brick> selectedBricks, Vector3 pickupOffset, Ray ray, float maxSqrDistance, out ConnectionResult result)
        {
            oldPositions.Clear();
            oldRotations.Clear();

            foreach (var selected in selectedBricks)
            {
                oldPositions.Add(selected.transform.position);
                oldRotations.Add(selected.transform.rotation);
            }

            var selectedBrick = sourceField.connectivity.part.brick;
            var pivot = selectedBrick.transform.position + pickupOffset;
            result = ConnectionResult.Empty();

            if (destinationField is AxleField destinationAxle && sourceField is AxleField sourceAxle)
            {
                if (!AxleField.AlignRotation(sourceAxle.transform.localToWorldMatrix, destinationAxle.transform.localToWorldMatrix, out Quaternion newRot, out bool sameDir))
                {
                    return false;
                }

                // Align the field
                var sourceMatrix = MathUtils.RotateAround(sourceAxle.transform.localToWorldMatrix, pivot, newRot);
                var sourcePosition = MathUtils.GetColumn(sourceMatrix, 3);
                var sourceUp = MathUtils.GetColumn(sourceMatrix, 1);

                // Project 4 points (bottom and top of each axle) to a plane facing the camera.
                // We already have the origin (destination position)
                var origin = ray.origin;
                var cameraToDestinationTop = (destinationAxle.transform.position + destinationAxle.transform.up * destinationAxle.length) - origin;

                var sourceBottom = sourcePosition;
                var sourceTop = sourcePosition + sourceUp * sourceAxle.length;

                var cameraToSourceBottom = sourceBottom - origin;
                var cameraToSourceTop = sourceTop - origin;
                var planeWorld = new Plane(ray.direction, destinationAxle.transform.position);
                var r1 = new Ray(origin, cameraToDestinationTop.normalized);
                var r2 = new Ray(origin, cameraToSourceBottom.normalized);
                var r3 = new Ray(origin, cameraToSourceTop.normalized);
                var p2 = MathUtils.IntersectRayPlane(r1, planeWorld);
                var p3 = MathUtils.IntersectRayPlane(r2, planeWorld);
                var p4 = MathUtils.IntersectRayPlane(r3, planeWorld);

                // Find closest line segment between the two projected segments
                MathUtils.ClosestLineSegmentLineSegment(destinationAxle.transform.position, p2, p3, p4, out Vector3 pa, out Vector3 pb, out float mua, out float mub);

                // Check distance squared for a tolerance
                var distanceSquared = MathUtils.DistanceSquared(pa, pb);
                if (distanceSquared > AxleDistanceTolerance)
                {
                    return false;
                }

                // Find point along destination axis, and check the squared distance with previous max
                var f1 = mua * destinationAxle.length;
                var sqrDistance = MathUtils.DistanceSquared(destinationAxle.transform.position + destinationAxle.transform.up * f1, origin);
                if (sqrDistance >= maxSqrDistance)
                {
                    return false;
                }

                // Find distance along source axis and then compute the intersection depending on their relative direction
                var f2 = mub * sourceAxle.length;

                // The intersection point is the point on the destination that the source must be placed
                var intersectionPointLocal = sameDir ? f1 - f2 : f1 + f2;

                // The new position of the source axle in destination local space
                var sourceAxleNewPosition = new Vector3(0.0f, intersectionPointLocal, 0.0f);
                var intersectionPointWorld = destinationAxle.transform.TransformPoint(sourceAxleNewPosition);

                // Check if we have a valid overlap within an epsilon
                var oldAxlePos = MathUtils.GetColumn(sourceMatrix, 3);
                sourceMatrix = MathUtils.SetTranslation(sourceMatrix, intersectionPointWorld);
                var valid = AxleFeature.GetOverlap(sourceAxle, destinationAxle, sourceMatrix, destinationAxle.transform.localToWorldMatrix, out float overlapCompensation, out _, destinationAxle.transform.localToWorldMatrix);                
                sourceMatrix = MathUtils.SetTranslation(sourceMatrix, oldAxlePos);

                if (valid)
                {
                    // Compensate for capped overlap
                    sourceAxleNewPosition.y += overlapCompensation;
                    var axleIntersection = destinationAxle.transform.TransformPoint(sourceAxleNewPosition);

                    // Find the rotation and position we need to offset the other selected bricks with
                    AxleField.GetConnectedTransformation(sourceAxle.feature, destinationAxle.feature, pivot, axleIntersection, out Vector3 connectedOffset, out float angle, out Vector3 axis);
                    AlignTransformations(selectedBricks, pivot, axis, angle, connectedOffset);

                    if (CheckRejection(selectedBricks, out float compensation, destinationAxle.connectivity.part.brick))
                    {
                        ResetTransformations(selectedBricks, oldPositions, oldRotations);
                        return false;
                    }

                    // Rejection check may return a compensation for colliding axles
                    if (Mathf.Abs(compensation) > 0.0f)
                    {
                        ResetTransformations(selectedBricks, oldPositions, oldRotations);
                        if (!sameDir)
                        {
                            compensation = -compensation;
                        }

                        sourceAxleNewPosition.y += compensation;
                        axleIntersection = destinationAxle.transform.TransformPoint(sourceAxleNewPosition);

                        AxleField.GetConnectedTransformation(sourceAxle.feature, destinationAxle.feature, pivot, axleIntersection, out connectedOffset, out angle, out axis);
                        AlignTransformations(selectedBricks, pivot, axis, angle, connectedOffset);
                    }

                    sourceMatrix = MathUtils.SetTranslation(sourceMatrix, axleIntersection);

                    // The new compensated position may again break the overlap
                    if (!AxleFeature.GetOverlap(sourceAxle, destinationAxle, sourceMatrix, destinationAxle.transform.localToWorldMatrix, out _, out _, Matrix4x4.identity, AxleFeature.CAPPED_EPSILON, -0.1f))
                    {
                        ResetTransformations(selectedBricks, oldPositions, oldRotations);
                        return false;
                    }

                    Physics.SyncTransforms();
                    result.colliding = Colliding(selectedBricks);

                    ResetTransformations(selectedBricks, oldPositions, oldRotations);
                    result.maxSqrDistance = sqrDistance;
                    result.intersectionPoint = axleIntersection;
                    result.srcConnection = sourceAxle.feature; 
                    result.dstConnection = destinationAxle.feature;
                    result.interaction = sourceAxle.feature.MatchTypes(destinationAxle.feature);
                    result.angleToConnect = angle;
                    result.rotationAxisToConnect = axis;
                    result.connectionOffset = connectedOffset;
                    return true;
                }
            }
            else if(destinationField is FixedField destinationFixed && sourceField is FixedField sourceFixed)
            {
                if(!FixedField.AlignRotation(sourceFixed.transform.localToWorldMatrix, destinationField.transform.localToWorldMatrix, destinationFixed.feature.axisType, out Quaternion newRot))
                {
                    return false;
                }

                var sourceMatrix = MathUtils.RotateAround(sourceFixed.transform.localToWorldMatrix, pivot, newRot);
                // Project the selected field onto the static field along the ray direction
                Vector3 sourcePos = MathUtils.GetColumn(sourceMatrix, 3);
                var localRay = destinationFixed.transform.InverseTransformDirection(ray.direction);

                // Line-plane intersection with line starting at source position and going in direction of ray.
                // This is similar to how we check planar fields, but the overlap check reduces to a distance check.
                var localOrigin = destinationFixed.transform.InverseTransformPoint(sourcePos);
                var localProjectedT = (-Vector3.Dot(Vector3.up, localOrigin)) / Vector3.Dot(Vector3.up, localRay);
                var localNewPoint = localOrigin + localRay * localProjectedT;

                AlignToGrid(ref localNewPoint, Matrix4x4.identity);

                var squaredDst = MathUtils.DistanceSquared(ray.origin, destinationFixed.transform.position);
                if (squaredDst >= maxSqrDistance)
                {
                    return false;
                }

                if (localNewPoint.magnitude < FixedDistanceTolerance)
                {
                    FixedField.GetConnectedTransformation(sourceFixed.feature, destinationFixed.feature, pivot, out Vector3 connectedOffset, out float angle, out Vector3 axis);
                    AlignTransformations(selectedBricks, pivot, axis, angle, connectedOffset);

                    if (CheckRejection(selectedBricks, out _, destinationFixed.connectivity.part.brick))
                    {
                        ResetTransformations(selectedBricks, oldPositions, oldRotations);
                        return false;
                    }

                    Physics.SyncTransforms();
                    result.colliding = Colliding(selectedBricks);

                    ResetTransformations(selectedBricks, oldPositions, oldRotations);
                    result.srcConnection = sourceFixed.feature;
                    result.dstConnection = destinationFixed.feature;
                    result.interaction = sourceFixed.feature.MatchTypes(destinationFixed.feature);
                    result.maxSqrDistance = squaredDst;
                    result.angleToConnect = angle;
                    result.rotationAxisToConnect = axis;
                    result.connectionOffset = connectedOffset;
                    return true;
                    
                }
            }
            else if (destinationField is PlanarField destinationPlanar && sourceField is PlanarField sourcePlanar)
            {
                if (!PlanarField.AlignRotation(sourcePlanar.transform.localToWorldMatrix, destinationPlanar.transform.localToWorldMatrix, out Quaternion newRot))
                {
                    return false;
                }
                var sourceMatrix = MathUtils.RotateAround(sourcePlanar.transform.localToWorldMatrix, pivot, newRot);
                
                // Project the selected field onto the static field along the ray direction
                Vector3 sourcePos = MathUtils.GetColumn(sourceMatrix, 3);

                var localRay = destinationPlanar.transform.InverseTransformDirection(ray.direction);
                
                // Line-plane intersection with line starting at source position and going in direction of ray.
                var localOrigin = destinationPlanar.transform.InverseTransformPoint(sourcePos);
                var localProjectedT = (-Vector3.Dot(Vector3.up, localOrigin)) / Vector3.Dot(Vector3.up, localRay);
                var localNewPoint = localOrigin + localRay * localProjectedT;

                AlignToGrid(ref localNewPoint, Matrix4x4.identity);

                // Check neighbouring positions in the order defined by connectionFieldOffsets.
                // If we do not overlap with no offset, do not try the other offsets.
                for (var i = 0; i < ConnectionFieldOffsets.Length; i++)
                {
                    var offset = ConnectionFieldOffsets[i];
                    var localToWorld = destinationPlanar.transform.TransformPoint(localNewPoint - Vector3.right * offset.x * LU_5 + Vector3.forward * offset.y * LU_5);

                    sourceMatrix = MathUtils.SetTranslation(sourceMatrix, localToWorld);

                    if (!PlanarField.GetOverlap(destinationPlanar, sourcePlanar, destinationPlanar.transform.localToWorldMatrix, sourceMatrix, out Vector2Int min, out Vector2Int max))
                    {
                        // If we do not overlap with no offset, do not try the other offsets.
                        if (i == 0)
                        {
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    var overlapConnections = PlanarField.GetConnectionsOnOverlap(destinationPlanar, sourcePlanar, sourceMatrix, min, max, out bool reject);
                    if (reject)
                    {
                        continue;
                    }

                    foreach (var (c1, c2) in overlapConnections)
                    {
                        if (reject)
                        {
                            break;
                        }

                        var squaredDst = MathUtils.DistanceSquared(ray.origin, c2.GetPosition());
                        if (squaredDst >= maxSqrDistance)
                        {
                            continue;
                        }

                        // Find the rotation and position we need to offset the other selected bricks with
                        PlanarField.GetConnectedTransformation(c1, c2, pivot, out Vector3 connectedOffset, out float angle, out Vector3 axis);
                        AlignTransformations(selectedBricks, pivot, axis, angle, connectedOffset);

                        if (CheckRejection(selectedBricks, out _))
                        {
                            ResetTransformations(selectedBricks, oldPositions, oldRotations);
                            break;
                        }

                        Physics.SyncTransforms();
                        result.colliding = Colliding(selectedBricks);

                        ResetTransformations(selectedBricks, oldPositions, oldRotations);
                        result.srcConnection = c1;
                        result.dstConnection = c2;
                        result.interaction = c1.MatchTypes(c2);
                        result.maxSqrDistance = squaredDst;
                        result.angleToConnect = angle;
                        result.rotationAxisToConnect = axis;
                        result.connectionOffset = connectedOffset;
                        return true;
                    }
                }
            }
            return false;
        }

        static ConnectionResult[] candidates;

        internal static bool FindBestConnectionOnBrick(HashSet<Brick> selectedBricks, HashSet<ConnectionField> selectedFields, Vector3 pickupOffset, Brick brick, Ray ray, float maxSqrDistance, out ConnectionResult result)
        {
            var physicsScene = brick.gameObject.scene.GetPhysicsScene();
            var validConnectionPairs = new List<(ConnectionField, ConnectionField)>();

            foreach (var part in brick.parts)
            {
                if (!part.connectivity)
                {
                    continue;
                }

                validConnectionPairs.Clear();

                foreach (var field in part.connectivity)
                {
                    if(field is PlanarField pf && !pf.HasAvailableConnections())
                    {
                        continue;
                    }
                    else if(field is FixedField ff && ff.connectedField)
                    {
                        continue;
                    }

                    foreach (var selectedField in selectedFields)
                    {
                        if (selectedField.GetType() == field.GetType())
                        {
                            if (field.kind == selectedField.kind)
                            {
                                continue;
                            }

                            validConnectionPairs.Add((field, selectedField));
                        }
                    }
                }

                if (validConnectionPairs.Count == 0)
                {
                    continue;
                }

                // Sort by distance to ray origin and then by angle of the up angle in case the distance is the same
                var pairs = validConnectionPairs.OrderBy(f =>
                {
                    var field = f.Item1;                    
                    if (field is PlanarField planarField)
                    {
                        var fieldSize = new Vector3(planarField.gridSize.x, 0.0f, planarField.gridSize.y) * LU_5 * 0.5f;
                        var localCenter = new Vector3(-fieldSize.x, 0.0f, fieldSize.z);
                        var fieldCenter = planarField.transform.TransformPoint(localCenter);
                        return MathUtils.DistanceSquared(ray.origin, fieldCenter);
                    }
                    else if (field is AxleField axleField)
                    {
                        var sourceField = f.Item2 as AxleField;
                        var position = field.transform.position;
                        var bottomPosition = sourceField.transform.position;
                        var topPosition = sourceField.transform.TransformPoint(new Vector3(0.0f, sourceField.length, 0.0f));
                        
                        return Mathf.Min(MathUtils.DistanceSquared(position, bottomPosition), MathUtils.DistanceSquared(position, topPosition));
                    }
                    else if(field is FixedField fixedField)
                    {
                        return MathUtils.DistanceSquared(ray.origin, f.Item2.transform.position);
                    }
                    return float.PositiveInfinity;
                }).ThenByDescending(f => {
                    if (f.Item1 is PlanarField)
                    {
                        return Vector3.Dot(f.Item1.transform.up, f.Item2.transform.up);
                    }
                    else if (f.Item1 is AxleField)
                    {
                        var cosUp = Vector3.Dot(f.Item1.transform.up, f.Item2.transform.up);
                        var cosDown = Vector3.Dot(f.Item1.transform.up, -f.Item2.transform.up);
                        return Mathf.Max(cosUp, cosDown);
                    }
                    else if(f.Item1 is FixedField f1)
                    {
                        // Fixme: Check this comparison for how we properly want to sort.
                        FixedField f2 = f.Item2 as FixedField;
                        var cosUp = Vector3.Dot(f.Item1.transform.up, f.Item2.transform.up);
                        if(f2.feature.axisType == FixedFeature.AxisType.Dual)
                        {
                            var cosRight = Vector3.Dot(f.Item1.transform.right, f.Item2.transform.right);
                            cosUp = Mathf.Max(cosUp, cosRight);
                        }
                        return cosUp;
                    }
                    return 0.0f;
                });

                // Run through every possible connectionfield
                // First field is the field in the scene, second is the field you want to connect

                var maxTries = 3;
                if(candidates == null || maxTries > candidates.Length)
                {
                    candidates = new ConnectionResult[maxTries];
                }
                var candidateCount = 0;

                foreach (var (field, selectedField) in pairs)
                {
                    if (candidateCount == maxTries)
                    {
                        break;
                    }

                    if (CheckConnectionCandidate(selectedField, field, selectedBricks, pickupOffset, ray, maxSqrDistance, out ConnectionResult connectionResult))
                    {
                        candidates[candidateCount++] = connectionResult;
                    }
                }

                if(candidateCount > 0)
                {
                    var currentResult = ConnectionResult.Empty();

                    for(var i = 0; i < candidateCount; i++)
                    {
                        if(currentResult.IsEmpty())
                        {
                            currentResult = candidates[i];
                            continue;
                        }

                        if(currentResult.colliding && !candidates[i].colliding)
                        {
                            currentResult = candidates[i];
                        }
                    }
                    result = currentResult;
                    return true;
                }
            }
            result = ConnectionResult.Empty();
            return false;
        }

        public static bool CheckRejection(ICollection<Brick> bricks, out float overlapCompensation, Brick ignore = null)
        {
            overlapCompensation = 0.0f;
            foreach (var checkBrick in bricks)
            {
                for(int i = 0; i < checkBrick.parts.Count; i++)
                {
                    Part checkPart = checkBrick.parts[i];
                    for(int j = 0; j < checkPart.connectivity.axleFields.Count; j++)
                    {
                        AxleField checkField = checkPart.connectivity.axleFields[j];
                        var validConnections = checkField.QueryConnections(out bool reject, true);
                        if (reject)
                        {
                            if (validConnections.Count > 0)
                            {
                                foreach (var connection in validConnections)
                                {
                                    if (ignore != null && connection.Item2.field.connectivity.part.brick == ignore)
                                    {
                                        continue;
                                    }

                                    if (!bricks.Contains(connection.Item2.field.connectivity.part.brick))
                                    {
                                        if (connection.Item1 is AxleFeature a1 && connection.Item2 is AxleFeature a2)
                                        {
                                            // Check compensation on overlap
                                            AxleFeature.GetOverlap(a1.Field, a2.Field, a1.field.transform.localToWorldMatrix, a2.field.transform.localToWorldMatrix, out _, out float overlap, a2.field.transform.localToWorldMatrix);
                                            
                                            if (Mathf.Abs(overlap) > 0.0f)
                                            {
                                                // Transform the overlap into the correct space
                                                var a2Topa1Space = a1.field.transform.InverseTransformPoint(a2.field.transform.position + a2.field.transform.up * a2.Field.length);
                                                var a2Bottoma1Space = a1.field.transform.InverseTransformPoint(a2.field.transform.position);

                                                var lBottom = 0.0f;
                                                var lTop = a1.Field.length;
                                                var rBottom = Mathf.Min(a2Topa1Space.y, a2Bottoma1Space.y);
                                                var rTop = Mathf.Max(a2Topa1Space.y, a2Bottoma1Space.y);

                                                var srcUp = a1.field.transform.up;
                                                var dstUp = a2.field.transform.up;

                                                var rot = Quaternion.FromToRotation(srcUp, dstUp);
                                                var cosAngle = Vector3.Dot(srcUp, dstUp);
                                                var sameDir = cosAngle > Cos45Epsilon;

                                                if (lTop > rTop)
                                                {
                                                    overlap = Mathf.Abs(overlap);
                                                }
                                                else if (lBottom < rBottom)
                                                {
                                                    overlap = -Mathf.Abs(overlap);
                                                }

                                                overlapCompensation = overlap;
                                                return false;
                                            }
                                        }
                                        else if(connection.Item1 is PlanarFeature p1 && connection.Item2 is PlanarFeature p2)
                                        {
                                            var distance = Vector3.Distance(p1.GetPosition(), p2.GetPosition());
                                            if (distance > float.Epsilon)
                                            {
                                                return false;
                                            }
                                            return true;
                                        }
                                        else if(connection.Item1 is FixedFeature f1 && connection.Item2 is FixedFeature f2)
                                        {
                                            var distance = Vector3.Distance(f1.Field.transform.position, f2.Field.transform.position);
                                            if (distance > float.Epsilon)
                                            {
                                                return false;
                                            }
                                            return true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        private static bool Colliding(HashSet<Brick> bricks, Brick ignore = null)
        {
            foreach (var selected in bricks)
            {
                if (ignore != null && selected == ignore)
                {
                    continue;
                }

                for(int i = 0; i < selected.parts.Count; i++)
                {
                    if (Part.IsColliding(selected.parts[i], selected.transform.localToWorldMatrix, colliderBuffer, out _, bricks))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private static void ResetTransformations(HashSet<Brick> bricks, List<Vector3> positions, List<Quaternion> rotations, Brick ignore = null)
        {
            var j = 0;
            foreach(var selected in bricks)
            {
                if(ignore != null && selected == ignore)
                {
                    j++;
                    continue;
                }
                selected.transform.position = positions[j];
                selected.transform.rotation = rotations[j];
                j++;
            }
        }
    }
}
