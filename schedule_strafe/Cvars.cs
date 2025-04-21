using MelonLoader;
using MelonLoader.Preferences;
using System;
using System.Collections.Generic;
using System.Text;

namespace schedule_strafe
{
    public static class Cvars
    {
        private static MelonPreferences_Category cvars_category;

        public static MelonPreferences_Entry<float> sv_maxspeed;

        public static MelonPreferences_Entry<float> sv_stopspeed;
        public static MelonPreferences_Entry<float> sv_friction;
        public static MelonPreferences_Entry<float> sv_accelerate;
        public static MelonPreferences_Entry<float> sv_airaccelerate;
        public static MelonPreferences_Entry<float> sv_standable_normal;

        public static MelonPreferences_Entry<float> cl_forwardspeed;
        public static MelonPreferences_Entry<float> cl_sidespeed;

        public static MelonPreferences_Entry<float> cl_speedcounter_offset;
        public static MelonPreferences_Entry<bool> cl_speedcounter;

        public static MelonPreferences_Entry<bool> sv_autobunnyhopping;

        public static void SetupCvars()
        {
            cvars_category = MelonPreferences.CreateCategory("ScheduleStrafe");

            sv_accelerate = cvars_category.CreateEntry<float>("sv_accelerate", 5f, "Ground acceleration value", (string)null, false, false, (ValueValidator)null, (string)null);
            sv_airaccelerate = cvars_category.CreateEntry<float>("sv_airaccelerate", 12f, "Air acceleration value", (string)null, false, false, (ValueValidator)null, (string)null);
            sv_friction = cvars_category.CreateEntry<float>("sv_friction", 5.2f, "Ground slippery-ness, the lower, the more slippery the ground is", (string)null, false, false, (ValueValidator)null, (string)null);
            sv_stopspeed = cvars_category.CreateEntry<float>("sv_stopspeed", 0.01524f, "If ground speed is below this value, stop moving", (string)null, false, false, (ValueValidator)null, (string)null);
            sv_maxspeed = cvars_category.CreateEntry<float>("sv_maxspeed", 5f, "Max ground speed value", (string)null, false, false, (ValueValidator)null, (string)null);
            sv_standable_normal = cvars_category.CreateEntry<float>("sv_standable_normal", 0.7f, "Surface angle when we start to \"surf\"", (string)null, false, false, (ValueValidator)null, (string)null);

            cl_forwardspeed = cvars_category.CreateEntry<float>("cl_forwardspeed", 5f, "How fast we move forwards", (string)null, false, false, (ValueValidator)null, (string)null);
            cl_sidespeed = cvars_category.CreateEntry<float>("cl_sidespeed", 5f, "How fast we move sideways", (string)null, false, false, (ValueValidator)null, (string)null);

            cl_speedcounter_offset = cvars_category.CreateEntry<float>("cl_speedcounter_offset", 100f, "Vertical offset of the speed counter to the center of the screen", (string)null, false, false, (ValueValidator)null, (string)null);
            cl_speedcounter = cvars_category.CreateEntry<bool>("cl_speedcounter", true, "Should display the speed counter", (string)null, false, false, (ValueValidator)null, (string)null);

            sv_autobunnyhopping = cvars_category.CreateEntry<bool>("sv_autobunnyhopping", true, "Should we automatically bunnyhop", (string)null, false, false, (ValueValidator)null, (string)null);
        }
    }
}
