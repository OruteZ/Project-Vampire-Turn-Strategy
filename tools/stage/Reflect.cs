using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace StagePlay
{
    // Reads the data dump that export.cs writes from the Unity data assets: every instance field, by name.
    public static class Reflect
    {
        const BindingFlags All = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static object Load(JsonElement e, Type t)
        {
            if (e.ValueKind == JsonValueKind.Null) return null;
            if (t == typeof(string)) return e.GetString();
            if (t == typeof(int)) return e.GetInt32();
            if (t == typeof(bool)) return e.GetBoolean();
            if (t == typeof(float)) return e.GetSingle();
            if (t.IsEnum) return Enum.ToObject(t, e.GetInt32());
            if (t.IsArray)
            {
                var et = t.GetElementType();
                var a = Array.CreateInstance(et, e.GetArrayLength());
                var i = 0;
                foreach (var x in e.EnumerateArray()) a.SetValue(Load(x, et), i++);
                return a;
            }
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
            {
                var l = (IList)Activator.CreateInstance(t);
                foreach (var x in e.EnumerateArray()) l.Add(Load(x, t.GetGenericArguments()[0]));
                return l;
            }
            if (Nullable.GetUnderlyingType(t) is Type u) return Load(e, u);
            var o = System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(t);
            foreach (var f in t.GetFields(All))
                if (e.TryGetProperty(f.Name, out var v)) f.SetValue(o, Load(v, f.FieldType));
            return o;
        }
    }
}
