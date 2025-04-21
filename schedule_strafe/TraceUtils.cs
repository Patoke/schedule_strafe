using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace schedule_strafe
{
    // credits to Fragsurf for this code, this was stripped from box collision testing
    public class Tracer
    {
        public struct Trace
        {

            public Vector3 startPos;
            public Vector3 endPos;
            public float fraction;
            public bool startSolid;
            public Collider hitCollider;
            public Vector3 hitPoint;
            public Vector3 planeNormal;
            public float distance;

        }

        public static void GetCapsulePoints(CapsuleCollider capc, Vector3 origin, out Vector3 p1, out Vector3 p2)
        {
            var distanceToPoints = capc.height / 2f - capc.radius;
            p1 = origin + capc.center + Vector3.up * distanceToPoints;
            p2 = origin + capc.center - Vector3.up * distanceToPoints;
        }

        public static Trace TraceCollider(Collider collider, Vector3 origin, Vector3 end, int layerMask, float colliderScale = 1f)
        {

            if (collider is CapsuleCollider)
            {

                // Capsule collider trace
                var capc = (CapsuleCollider)collider;

                Vector3 point1, point2;
                GetCapsulePoints(capc, origin, out point1, out point2);

                return TraceCapsule(point1, point2, capc.radius, origin, end, capc.contactOffset, layerMask, colliderScale);

            }

            throw new System.NotImplementedException("Trace missing for collider: " + collider.GetType());

        }

        public static Trace TraceCapsule(Vector3 point1, Vector3 point2, float radius, Vector3 start, Vector3 destination, float contactOffset, int layerMask, float colliderScale = 1f)
        {
            var result = new Trace()
            {
                startPos = start,
                endPos = destination
            };

            var longSide = Mathf.Sqrt(contactOffset * contactOffset + contactOffset * contactOffset);
            radius *= (1f - contactOffset);
            var direction = (destination - start).normalized;
            var maxDistance = Vector3.Distance(start, destination) + longSide;

            RaycastHit hit;
            if (Physics.CapsuleCast(
                point1: point1 - Vector3.up * colliderScale * 0.5f,
                point2: point2 + Vector3.up * colliderScale * 0.5f,
                radius: radius * colliderScale,
                direction: direction,
                hitInfo: out hit,
                maxDistance: maxDistance,
                layerMask: layerMask,
                queryTriggerInteraction: QueryTriggerInteraction.Ignore))
            {

                result.fraction = hit.distance / maxDistance;
                result.hitCollider = hit.collider;
                result.hitPoint = hit.point;
                result.planeNormal = hit.normal;
                result.distance = hit.distance;

                RaycastHit normalHit;
                Ray normalRay = new Ray(hit.point - direction * 0.001f, direction);
                if (hit.collider.Raycast(normalRay, out normalHit, 0.002f))
                {

                    result.planeNormal = normalHit.normal;

                }

            }
            else
                result.fraction = 1;

            return result;

        }
    }
}