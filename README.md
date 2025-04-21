# ScheduleStrafe

Source engine game movement code reimplementation for Schedule One, walk like in Half-Life 2, Bhop like in any other source game.

## Configuration

Access all Cvars through ``MelonPreferences.cfg``:

- **sv_accelerate**:  Ground acceleration value (How fast do we reach the max ground speed)
- **sv_airaccelerate**: Air acceleration value (How hard can we strafe)
- **sv_friction**: Ground slippery-ness, the lower, the more slippery the ground is
- **sv_stopspeed**: If ground speed is below this value, stop moving
- **sv_maxspeed**: Max ground speed value
- **sv_standable_normal**: Surface angle when we start to "surf"
- **cl_forwardspeed**: How fast we move forwards
- **cl_sidespeed**: How fast we move sideways
- **cl_speedcounter_offset**: Vertical offset of the speed counter to the center of the screen
- **cl_speedcounter**: Should display the speed counter
- **sv_autobunnyhopping**: Should we automatically bunnyhop

## Credits

- Fragsurf implementation of Source-like capsule casting
