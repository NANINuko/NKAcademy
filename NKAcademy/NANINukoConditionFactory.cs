using System;
using System.Collections.Generic;
using UnityEngine;

namespace NANINuko.Conditions
{
    internal static class NANINukoConditionFactory
    {
        private static readonly Dictionary<string, Type> RegisteredTypes = new Dictionary<string, Type>();

        public static void Init()
        {
            Register("ConBananaSmoothie", typeof(ConBananaSmoothie));
            Register("ConStrawberryMilk", typeof(ConStrawberryMilk));
            Register("ConHoneyLemonade", typeof(ConHoneyLemonade));
            Register("ConMysteriousButterflyTea", typeof(ConMysteriousButterflyTea));
            Register("ConProteinMilkLight", typeof(ConProteinMilkLight));
            Register("ConChiirinSoda", typeof(ConChiirinSoda));
            Register("ConDebugMode", typeof(ConDebugMode));

            Debug.Log("[NANINukoConditionFactory] All static conditions registered.");
        }

        private static void Register(string id, Type type)
        {
            if (RegisteredTypes.ContainsKey(id))
                return;

            RegisteredTypes[id] = type;
            Debug.Log("[NANINukoConditionFactory] Registered: " + id);
        }

        public static Type Get(string name)
        {
            RegisteredTypes.TryGetValue(name, out var t);
            return t;
        }
    }

    public class ConBananaSmoothie : Condition { }
    public class ConStrawberryMilk : Condition { }
    public class ConHoneyLemonade : Condition { }
    public class ConMysteriousButterflyTea : Condition { }
    public class ConProteinMilkLight : Condition { }
    public class ConChiirinSoda : Condition { }
    public class ConDebugMode : Condition { }
}