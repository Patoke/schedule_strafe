using ScheduleOne.PlayerScripts;
using UnityEngine;

namespace schedule_strafe
{
    public static class CollisionTest
    {
        public const float DIST_EPSILON = 0.03125f;
        public const float MINIMUM_MOVE_FRACTION = 0.0001f;
        public const float EFFECTIVELY_HORIZONTAL_NORMAL_Z = 0.0001f;
        public const int MAX_CLIP_PLANES = 5;

        public static int ClipVelocity(Vector3 in_velocity, Vector3 plane_normal, out Vector3 velocity, float overbounce)
        {
            // what type of plane were we blocked against
            int blocked = 0x0;

            float angle = plane_normal.y; // in the source engine, the y axis is the z axis
            if (angle > 0f)
            {
                blocked |= 0x1; // the collided plane is a floor
            }
            if (angle == 0f)
            {
                blocked |= 0x2; // the collided plane is a wall/step
            }

            // Determine how far along plane to slide based on incoming direction.
            float backoff = Vector3.Dot(in_velocity, plane_normal);

            Vector3 change = plane_normal * backoff;
            velocity = in_velocity - change;

            // iterate once to make sure we aren't still moving through the plane
            float adjust = Vector3.Dot(velocity, plane_normal);
            if (adjust < 0f)
            {
                // min this against a small number (but no further from zero than -DIST_EPSILON) to account for crossing a plane with a near-parallel normal
                adjust = Math.Min(adjust, -DIST_EPSILON);
                velocity -= plane_normal * adjust;
            }

            return blocked;
        }

        public static int TryPlayerMove(PlayerMovement __instance, in Vector3 in_velocity, out Vector3 velocity)
        {
            // @Patoke yappington: tip for game devs, don't use capsule colliders for games which don't require precise corner cutoffs or player collisions
            //  this just slows down ur game and makes velocity reflection (clipping) a nightmare
            //  apart from reflecting velocity from the collided planes, u also have to be aware of what is the collided point on the outside of ur capsule and its angle
            //  wall strafing is a disaster thanks to this since it would only work on straight axis walls
            //  as long as they're sligtly slanted, the collision is gonna assume a box collider on the side it hit the wall, disregarding the capsule point angle, therefore, stopping abruptly
            //  this isn't limited to wall strafing, anything that hits our collider from the side will stop us abruptly
            // @Patoke todo: fix clipping code not accounting for capsule angle
            float time_left = Time.deltaTime, allFraction = 0f;
            int numbumps = 4, blocked = 0x0, numplanes = 0;

            Vector3[] planes = new Vector3[MAX_CLIP_PLANES];

            velocity = in_velocity;

            Vector3 new_velocity = Vector3.zero;
            Vector3 original_velocity = velocity; // velocity pre-clipping
            Vector3 primal_velocity = velocity; // unclipped velocity (raw velocity, unmodified copy)

            for (int bumpcount = 0; bumpcount < numbumps; bumpcount++)
            {
                if (velocity.magnitude == 0f)
                {
                    break;
                }

                // extrapolate our current position towards our movement direction
                // assume we can move directly to the end position
                Vector3 end = __instance.transform.position + time_left * velocity;

                // we don't do a bbox cast here, rather we use a capsule cast as the game uses capsules for collisions
                // we ignore all trash colliders
                Tracer.Trace pm = Tracer.TraceCollider(__instance.Player.CapCol, __instance.transform.position, end, ~(1 << LayerMask.NameToLayer("Trash")));

                if (pm.fraction > 0f && pm.fraction < MINIMUM_MOVE_FRACTION)
                {
                    // HACK: extremely tiny move fractions cause problems in later computations that determine values using portions of distance moved.
                    pm.fraction = 0f;
                }

                allFraction += pm.fraction;

                // we don't care if the player gets stuck, the collider is the one who cares about this stuff, not us
                if (pm.fraction > 0f)
                {
                    original_velocity = velocity;
                    numplanes = 0;
                }

                // we covered the entire distance, no need to calculate anything else
                if (pm.fraction == 1f)
                {
                    break;
                }

                // if we have a high y component in the normal, assume it's a floor
                if (pm.planeNormal.y > Cvars.sv_standable_normal.Value)
                {
                    blocked |= 0x1;
                }

                // if we have an extremely near to 0 or 0 y component, assume it's a wall/step
                if (Math.Abs(pm.planeNormal.y) < EFFECTIVELY_HORIZONTAL_NORMAL_Z)
                {
                    pm.planeNormal.y = 0f;
                    blocked |= 0x2;
                }

                time_left -= time_left * pm.fraction;

                // somehow we collided with too many planes at once, stop our movement and break out
                if (numplanes > MAX_CLIP_PLANES)
                {
                    velocity = Vector3.zero;
                    break;
                }

                planes[numplanes] = pm.planeNormal;
                numplanes++;

                // @Patoke todo: ignore movetype and groundentity checks, we don't wanna deal with that atm
                if (numplanes == 1)
                {
                    for (int i = 0; i < numplanes; i++)
                    {
                        if (planes[i].y > Cvars.sv_standable_normal.Value)
                        {
                            ClipVelocity(original_velocity, planes[i], out new_velocity, 1f);
                            original_velocity = new_velocity;
                        }
                        else
                        {
                            // @Patoke todo: specify bounce values
                            ClipVelocity(original_velocity, planes[i], out new_velocity, 1f);
                        }
                    }

                    velocity = new_velocity;
                    original_velocity = new_velocity;
                }
                else
                {
                    int i = 0;
                    for (; i < numplanes; i++)
                    {
                        ClipVelocity(original_velocity, planes[i], out velocity, 1f);

                        int j = 0;
                        for (; j < numplanes; j++)
                        {
                            if (j != i)
                            {
                                // are we now moving against this plane? if so, break
                                if (Vector3.Dot(velocity, planes[j]) < 0f)
                                {
                                    break;
                                }
                            }
                        }

                        if (j == numplanes)  // Didn't have to clip, so we're ok
                            break;
                    }

                    // did we go all the way through plane set
                    if (i != numplanes)
                    {   // go along this plane
                        // pmove.velocity is set in clipping call, no need to set again.
                        ;
                    }
                    else
                    {   // go along the crease
                        if (numplanes != 2)
                        {
                            velocity = Vector3.zero;
                            break;
                        }

                        Vector3 dir = Vector3.Cross(planes[0], planes[1]).normalized;
                        float dir_dot = Vector3.Dot(dir, velocity);

                        velocity.x *= dir.x * dir_dot;
                        velocity.y *= dir.y * dir_dot;
                        velocity.z *= dir.z * dir_dot;
                    }

                    // if original velocity is against the original velocity, stop dead
                    // to avoid tiny occilations in sloping corners
                    float d = Vector3.Dot(velocity, primal_velocity);
                    if (d <= 0)
                    {
                        velocity = Vector3.zero;
                        break;
                    }
                }
            }

            if (allFraction == 0f)
            {
                velocity = Vector3.zero;
            }

            return blocked;
        }
    }
}
