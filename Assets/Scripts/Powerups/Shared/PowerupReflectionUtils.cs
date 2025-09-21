using UnityEngine;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SnakeGame.Powerups
{
    public static class PowerupReflectionUtils
    {
        public class ScaledField
        {
            public object target;
            public FieldInfo field;
            public float original;
        }

        public static List<ScaledField> ScaleFloatFieldsIfNameContains(object target, string[] namePartsLower, float mul)
        {
            List<ScaledField> list = new List<ScaledField>();
            Type t = target.GetType();
            FieldInfo[] fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo f = fields[i];
                if (f.FieldType != typeof(float)) continue;
                string lname = f.Name.ToLowerInvariant();
                bool ok = false;
                for (int p = 0; p < namePartsLower.Length; p++)
                {
                    if (lname.Contains(namePartsLower[p]))
                    {
                        ok = true;
                        break;
                    }
                }
                if (!ok) continue;
                float v = (float)f.GetValue(target);
                ScaledField sf = new ScaledField();
                sf.target = target;
                sf.field = f;
                sf.original = v;
                list.Add(sf);
                f.SetValue(target, v * mul);
            }
            return list;
        }

        public static List<ScaledField> ScaleFloatFieldsIfNameMatches(object target, string[] namesLower, float mul)
        {
            List<ScaledField> list = new List<ScaledField>();
            Type t = target.GetType();
            FieldInfo[] fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo f = fields[i];
                if (f.FieldType != typeof(float)) continue;
                string lname = f.Name.ToLowerInvariant();
                bool ok = false;
                for (int p = 0; p < namesLower.Length; p++)
                {
                    if (lname == namesLower[p])
                    {
                        ok = true;
                        break;
                    }
                }
                if (!ok) continue;
                float v = (float)f.GetValue(target);
                ScaledField sf = new ScaledField();
                sf.target = target;
                sf.field = f;
                sf.original = v;
                list.Add(sf);
                f.SetValue(target, v * mul);
            }
            return list;
        }

        public static void Revert(ScaledField sf)
        {
            if (sf == null || sf.field == null || sf.target == null) return;
            sf.field.SetValue(sf.target, sf.original);
        }
    }
}
