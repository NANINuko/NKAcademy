using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace NANINuko.Framework.Patches
{
    [HarmonyPatch]
    internal static class NANINukoClassCacheAssemblyRegistrarPatch
    {
        static MethodBase TargetMethod()
        {
            return FindCoreStartCase();
        }

        static void Prefix()
        {
            try
            {
                RegisterMarkedAssembliesToClassCache();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[NANINuko] RegisterMarkedAssembliesToClassCache failed: " + ex);
            }
        }

        private static MethodBase FindCoreStartCase()
        {
            var coreType = FindTypeByName("Core");
            if (coreType == null)
                return null;

            return AccessTools.Method(coreType, "StartCase");
        }

private static void RegisterMarkedAssembliesToClassCache()
{
    var markerType = typeof(NANINukoAssemblyMarkerAttribute);
    var assembliesToRegister = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    Debug.Log("[NANINuko] === RegisterMarkedAssembliesToClassCache START ===");

    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
    {
        var asmName = SafeAssemblyName(asm);
        if (string.IsNullOrWhiteSpace(asmName))
            continue;

        Type[] types;
        try
        {
            types = asm.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types;
        }
        catch
        {
            continue;
        }

        if (types == null)
            continue;

        for (int i = 0; i < types.Length; i++)
        {
            var t = types[i];
            if (t == null)
                continue;

            if (!t.IsDefined(markerType, inherit: false))
                continue;

            Debug.Log("[NANINuko] Marker found in assembly: " + asmName);
            assembliesToRegister.Add(asmName);
            break;
        }
    }

    Debug.Log("[NANINuko] Marked assemblies count: " + assembliesToRegister.Count);

    if (assembliesToRegister.Count == 0)
    {
        Debug.Log("[NANINuko] No marked assemblies found.");
        return;
    }

    var classCacheSets = FindClassCacheAssemblySets();
    Debug.Log("[NANINuko] ClassCache assembly sets found: " + classCacheSets.Count);

    if (classCacheSets.Count == 0)
    {
        Debug.LogWarning("[NANINuko] ClassCache.assemblies not found.");
        return;
    }

    foreach (var set in classCacheSets)
    {
        foreach (var asmName in assembliesToRegister)
        {
            Debug.Log("[NANINuko] Trying to add assembly: " + asmName);

            if (set.Add(asmName))
            {
                Debug.Log("[NANINuko] ClassCache assembly registered: " + asmName);
            }
            else
            {
                Debug.Log("[NANINuko] Assembly already existed or add failed: " + asmName);
            }
        }
    }

    Debug.Log("[NANINuko] === RegisterMarkedAssembliesToClassCache END ===");
}

private static List<HashSet<string>> FindClassCacheAssemblySets()
{
    var result = new List<HashSet<string>>();
    var seenTypes = new HashSet<Type>();

    Debug.Log("[NANINuko] Searching for ClassCache.assemblies fields...");

    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
    {
        Type[] types;
        try
        {
            types = asm.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            types = ex.Types;
        }
        catch
        {
            continue;
        }

        if (types == null)
            continue;

        foreach (var t in types)
        {
            if (t == null)
                continue;

            if (!t.Name.StartsWith("ClassCache", StringComparison.Ordinal))
                continue;

            Debug.Log("[NANINuko] ClassCache-like type found: " + t.FullName);

            Type targetType = t;

            if (t.IsGenericTypeDefinition)
            {
                try
                {
                    targetType = t.MakeGenericType(typeof(object));
                }
                catch
                {
                    targetType = t;
                }
            }

            if (!seenTypes.Add(targetType))
                continue;

            var field = targetType.GetField(
                "assemblies",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
            );

            if (field == null)
            {
                Debug.Log("[NANINuko] assemblies field NOT found in: " + targetType.FullName);
                continue;
            }

            var value = field.GetValue(null) as HashSet<string>;
            if (value == null)
            {
                Debug.Log("[NANINuko] assemblies field exists but is NULL in: " + targetType.FullName);
                continue;
            }

            Debug.Log("[NANINuko] assemblies field FOUND in: " + targetType.FullName);
            result.Add(value);
        }
    }

    Debug.Log("[NANINuko] Total assemblies sets collected: " + result.Count);
    return result;
}

        private static Type FindTypeByName(string name)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var asmName = SafeAssemblyName(asm);
                if (!asmName.Contains("Elin"))
                    continue;

                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch
                {
                    continue;
                }

                if (types == null)
                    continue;

                foreach (var t in types)
                {
                    if (t != null && t.Name == name)
                        return t;
                }
            }

            return AccessTools.TypeByName(name);
        }

        private static string SafeAssemblyName(Assembly asm)
        {
            try
            {
                return asm?.GetName().Name ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
