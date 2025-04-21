using MelonLoader;
using HarmonyLib;
using ScheduleOne.PlayerScripts;
using ScheduleOne;
using ScheduleOne.UI;
using UnityEngine;
using System.Reflection;
using ScheduleOne.DevUtilities;

[assembly: MelonInfo(typeof(schedule_strafe.GameMovement), "schedule_strafe", "1.0.0", "Patoke", null)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace schedule_strafe
{
    public class GameMovement : MelonMod
    {
        public override void OnInitializeMelon()
        {
            Cvars.SetupCvars();
            LoggerInstance.Msg("Mod initialized.");
        }

        public override void OnGUI() 
        {
            var box_width = 150f;
            var box_height = 25f;

            var horizontal_speed = MathF.Sqrt(MovementPatches.velocity.x * MovementPatches.velocity.x + MovementPatches.velocity.z * MovementPatches.velocity.z);

            GUI.Label(new Rect(Screen.width / 2f - box_width / 2f, Screen.height / 2f + Cvars.cl_speedcounter_offset.Value, box_width, box_height), "speed: " + horizontal_speed);
        }
    }

    [HarmonyPatch(typeof(PlayerMovement), "Move")]
    public static class MovementPatches
    {
        public static int groundedFrames = 0;   // only used to remove friction for the first grounded frame
        public static bool isGrounded = true;   // fixed IsGrounded variable that uses the collider's collision flags rather than doing its own sphere cast
        public static Vector3 velocity;
        public static float fmove;              // forwardmove
        public static float smove;              // sidemove

        public static void Friction(PlayerMovement __instance)
        {
            // we apply friction even if we can't move so we don't slide forever when we lose input focus
            var speed = velocity.magnitude;

            // yoink https://github.com/Olezen/UnitySourceMovement/blob/master/Modified%20fragsurf/Movement/SurfPhysics.cs#L168
            if (speed < 0.0001905f)
            {
                return;
            }

            float friction = Cvars.sv_friction.Value / __instance.SlipperyMovementMultiplier;

            float control = speed < Cvars.sv_stopspeed.Value ? Cvars.sv_stopspeed.Value : speed;
            var drop = control * friction * Time.deltaTime;

            var newspeed = speed - drop;
            if (newspeed < 0f)
                newspeed = 0f;

            if (newspeed != speed)
            {
                newspeed /= speed;
                velocity *= newspeed;
            }
        }

        public static void Accelerate(PlayerMovement __instance, float wishspeed, Vector3 wishdir)
        {
            if (!__instance.canMove)
            {
                return;
            }

            var currentspeed = Vector3.Dot(velocity, wishdir);
            var addspeed = wishspeed - currentspeed;

            if (addspeed < 0f)
            {
                return;
            }

            var accelspeed = Cvars.sv_accelerate.Value * Time.deltaTime * wishspeed;

            if (accelspeed > addspeed)
            {
                accelspeed = addspeed;
            }

            velocity += wishdir * accelspeed;
        }

        public static void AirAccelerate(PlayerMovement __instance, float wishspeed, Vector3 wishdir)
        {
            if (!__instance.canMove)
            {
                return;
            }

            var wishspd = wishspeed; // clamped wishspeed
            // don't allow player to veer too much by pressing movement keys
            if (wishspd > 1f)
            {
                wishspd = 1f;
            }

            var currentspeed = Vector3.Dot(velocity, wishdir);
            var addspeed = wishspd - currentspeed;

            if (addspeed < 0f)
            {
                return;
            }

            var accelspeed = Cvars.sv_airaccelerate.Value * Time.deltaTime * wishspeed;

            if (accelspeed > addspeed)
            {
                accelspeed = addspeed;
            }

            velocity += wishdir * accelspeed;
        }

        public static bool Prefix(PlayerMovement __instance, float ___timeSinceStaminaDrain, float ___crouchSpeedMultipler, float ___gravityMultiplier, float ___jumpForce, ref bool ___sprintActive, ref bool ___sprintReleased, ref bool ___isJumping, ref float ___timeGrounded)
        {
            var isSprinting = Traverse.Create(__instance).Property("isSprinting");
            var CurrentSprintMultiplier = Traverse.Create(__instance).Property("CurrentSprintMultiplier");

            isSprinting.SetValue(false);
            if (!__instance.Controller.enabled)
            {
                CurrentSprintMultiplier.SetValue(Mathf.MoveTowards(__instance.CurrentSprintMultiplier, 1f, Time.deltaTime * 4f));
                return false;
            }
            if (__instance.currentVehicle != null)
            {
                return false;
            }

            // get player inputs
            // reset move values each frame
            fmove = smove = 0;
            if (GameInput.GetButton(GameInput.ButtonCode.Forward))
            {
                fmove += Cvars.cl_forwardspeed.Value;
            }
            if (GameInput.GetButton(GameInput.ButtonCode.Backward))
            {
                fmove -= Cvars.cl_forwardspeed.Value;
            }
            if (GameInput.GetButton(GameInput.ButtonCode.Left))
            {
                smove -= Cvars.cl_sidespeed.Value;
            }
            if (GameInput.GetButton(GameInput.ButtonCode.Right))
            {
                smove += Cvars.cl_sidespeed.Value;
            }

            if (GameInput.GetButtonDown(GameInput.ButtonCode.Sprint) && !___sprintActive)
            {
                ___sprintActive = true;
                ___sprintReleased = false;
            }
            else if (GameInput.GetButton(GameInput.ButtonCode.Sprint) && Singleton<Settings>.Instance.SprintMode == InputSettings.EActionMode.Hold)
            {
                ___sprintActive = true;
            }
            else if (Singleton<Settings>.Instance.SprintMode == InputSettings.EActionMode.Hold)
            {
                ___sprintActive = false;
            }
            if (!GameInput.GetButton(GameInput.ButtonCode.Sprint))
            {
                ___sprintReleased = true;
            }
            if (GameInput.GetButtonDown(GameInput.ButtonCode.Sprint) && ___sprintReleased)
            {
                ___sprintActive = !___sprintActive;
            }

            // check if player is grounded each iteration of Update instead of FixedUpdate as well as check properly for player bounds
            {
                // use this for a messed up grounded check that allows u to jump REALLY high
                //float maxDistance = PlayerMovement.StandingControllerHeight * (__instance.Player.Crouched ? PlayerMovement.CrouchHeightMultiplier : 1f) / 2f;
                //RaycastHit raycastHit;
                //isGrounded = Physics.SphereCast(__instance.Player.transform.position, PlayerMovement.ControllerRadius * 0.75f, -__instance.Player.transform.up, out raycastHit, __instance.Controller.collisionFlags, __instance.Player.GroundDetectionMask);

                isGrounded = __instance.Player.CharacterController.collisionFlags.HasFlag(CollisionFlags.CollidedBelow);
                if (isGrounded)
                {
                    groundedFrames++;
                }
                else
                {
                    groundedFrames = 0;
                }
            }

            // add half the gravity component at the start of the frame
            if (!isGrounded)
            {
                velocity.y += (Physics.gravity.y * ___gravityMultiplier * Time.deltaTime * PlayerMovement.GravityMultiplier) * 0.5f;
            }

            // keep game's jump routine for the cool funkyness of it
            if (__instance.canMove && __instance.canJump && isGrounded && !___isJumping && !GameInput.IsTyping && !Singleton<PauseMenu>.Instance.IsPaused && GameInput.GetButton(GameInput.ButtonCode.Jump))
            {
                ___isJumping = true;

                if (__instance.onJump != null)
                {
                    __instance.onJump();
                }
                Player.Local.PlayJumpAnimation();

                // inlined Jump
                {
                    __instance.Controller.velocity.Set(__instance.Controller.velocity.x, 0f, __instance.Controller.velocity.z);
                    // use this for the original funkyness of jumping
                    //velocity.y += ___jumpForce * PlayerMovement.JumpMultiplier;
                    ___timeGrounded = 0f;

                    var flGroundFactor = 1f; // this should be reading surface data for bouncyness and such
                    var GAMEMOVEMENT_JUMP_HEIGHT = 2.1f;

                    var flMul = MathF.Sqrt(2f * ___jumpForce * PlayerMovement.JumpMultiplier * GAMEMOVEMENT_JUMP_HEIGHT);

                    //if (__instance.isCrouched)
                    //{
                        velocity.y = flGroundFactor * flMul;
                    //}
                    //else
                    //{
                    //    velocity.y += flGroundFactor * flMul;
                    //}
                }
            }

            MethodInfo methodInfo = typeof(PlayerMovement).GetMethod("TryToggleCrouch", BindingFlags.NonPublic | BindingFlags.Instance);
            if (__instance.canMove && !GameInput.IsTyping && !Singleton<PauseMenu>.Instance.IsPaused && GameInput.GetButtonDown(GameInput.ButtonCode.Crouch))
            {
                methodInfo.Invoke(__instance, []);
            }

            if (isGrounded)
            {
                ___isJumping = false;
                if (__instance.onJump != null)
                {
                    __instance.onLand();
                }
            }

            // slow down player when crouched
            if (__instance.isCrouched)
            {
                float frac = 0.33333333f; // this value from the source engine is way better than what the game does
                //float frac = 1f - (1f - ___crouchSpeedMultipler) * (1f - __instance.standingScale);
                fmove *= frac;
                smove *= frac;
            }

            // slow down player when zapped by a taser
            if (Player.Local.IsTased)
            {
                //float frac = 0.5f;
                float frac = 0.1f; // maybe a higher penalty would make it harder to get away?
                fmove *= frac;
                smove *= frac;
            }

            // sprinting logic
            isSprinting.SetValue(false);
            // @Patoke todo: check for __instance.sprintBlockers.Count == 0
            var sprintMultiplier = 1.25f;//PlayerMovement.SprintMultiplier; // use a smaller sprint multiplier, default one is too big for this game movement code
            if (___sprintActive && __instance.canMove && !__instance.isCrouched && !__instance.Player.IsTased && (fmove != 0f || smove != 0f))
            {
                if (__instance.CurrentStaminaReserve > 0f || !__instance.SprintingRequiresStamina)
                {
                    CurrentSprintMultiplier.SetValue(Mathf.MoveTowards(__instance.CurrentSprintMultiplier, sprintMultiplier, Time.deltaTime * 4f));
                    if (__instance.SprintingRequiresStamina)
                    {
                        __instance.ChangeStamina(-12.5f * Time.deltaTime, true);
                    }
                    isSprinting.SetValue(true);
                }
                else
                {
                    ___sprintActive = false;
                    CurrentSprintMultiplier.SetValue(Mathf.MoveTowards(__instance.CurrentSprintMultiplier, 1f, Time.deltaTime * 4f));
                }
            }
            else
            {
                ___sprintActive = false;
                CurrentSprintMultiplier.SetValue(Mathf.MoveTowards(__instance.CurrentSprintMultiplier, 1f, Time.deltaTime * 4f));
            }
            if (!__instance.isSprinting && ___timeSinceStaminaDrain > 1f)
            {
                CurrentSprintMultiplier.SetValue(Mathf.MoveTowards(__instance.CurrentSprintMultiplier, 1f, Time.deltaTime * 4f));
            }

            if (__instance.isSprinting)
            {
                float frac = __instance.CurrentSprintMultiplier;
                fmove *= frac;
                smove *= frac;
            }
            
            Vector3 wishdir = new Vector3(0, 0, 0);

            Vector3 forward = __instance.Player.transform.forward.normalized;
            Vector3 right = __instance.Player.transform.right.normalized;
            forward.y = right.y = 0;

            wishdir.x = forward.x * fmove + right.x * smove;
            wishdir.z = forward.z * fmove + right.z * smove;

            var wishspeed = wishdir.magnitude;
            wishdir = wishdir.normalized;

            // remove vertical component
            wishdir.y = 0f;

            // @Patoke todo: allow for normal prestrafing, if this code is not present u move faster diagonally
            // clamp to max defined speed
            var maxspeed = Cvars.sv_maxspeed.Value * __instance.CurrentSprintMultiplier; // scale so sprinting is usable
            if (wishspeed != 0f && wishspeed > maxspeed)
            {
                wishspeed = maxspeed;
            }

            // WalkMove, don't apply ground friction or acceleration for the first grounded tick, used to emulate the lack of velocity loss when bhopping
            if (isGrounded && groundedFrames > 1)
            {
                Friction(__instance);
                Accelerate(__instance, wishspeed, wishdir);

                if (velocity.magnitude < 0.001905f)
                {
                    velocity = Vector3.zero;
                }
            }
            else // AirMove
            {
                AirAccelerate(__instance, wishspeed, wishdir);
            }

            CollisionTest.TryPlayerMove(__instance, velocity, out velocity);

            // add remainding half of the gravity component at the end
            if (!isGrounded)
            {
                velocity.y += (Physics.gravity.y * ___gravityMultiplier * Time.deltaTime * PlayerMovement.GravityMultiplier) * 0.5f;
            }

            __instance.Controller.Move(velocity * Time.deltaTime);

            // stop original game movement code from running
            return false;
        }
    }
}