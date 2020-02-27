
// Convenience utilities written by Malte Hildingsson, malte@hapki.se.
// No copyright is claimed, and you may use it for any purpose you like.
// No warranty for any purpose is expressed or implied.

using System;
using UnityEngine;

namespace Hapki {

public static class EnumExtensions {
    public static T[] GetValuesAsInstances<T>(Type type) {
        var names = Enum.GetNames(type);
        var array = (T[]) Array.CreateInstance(typeof(T), names.Length);
        var assembly = type.Assembly.FullName;
        var space = type.Namespace;
        for (int i = 0, n = names.Length; i < n; ++i) {
            var fullName = space != null ? space + "." + names[i] : names[i];
            array[i] = (T) Activator.CreateInstance(assembly, fullName).Unwrap();
        }
        return array;
    }

    public static T Parse<T>(string name)
            where T : struct, IConvertible {
#if NET_4_6
        T e;
        Enum.TryParse(name, out e);
        return e;
#else
        try {
            return (T) Enum.Parse(typeof(T), name);
        } catch {
            return default(T);
        }
#endif
    }
}

public class EnumFlagsAttribute : PropertyAttribute {
}

public class SerializedEnumAttribute : PropertyAttribute {
    public readonly Type type;

    public SerializedEnumAttribute(Type type) {
        this.type = type;
    }
}

} // Hapki

