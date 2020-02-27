
// Convenience utilities written by Malte Hildingsson, malte@hapki.se.
// No copyright is claimed, and you may use it for any purpose you like.
// No warranty for any purpose is expressed or implied.

using System.Collections.Generic;

namespace Hapki {

public interface IAuto {
    void OnEnable();
    void OnDisable();
}

public static class AutoExtensions {
    public static void AutoAdd<T>(this T auto) where T : IAuto {
        AutoList<T> _;
        _ += auto;
    }

    public static void AutoRemove<T>(this T auto) where T : IAuto {
        AutoList<T> _;
        _ -= auto;
    }
}

public struct AutoList<T> where T : IAuto {
    static readonly List<T> list = new List<T>();

    public int Count {
        get { return list.Count; }
    }

    public T this[int index] {
        get { return list[index]; }
        set { list[index] = value; }
    }

    public List<T>.Enumerator GetEnumerator() {
        return list.GetEnumerator();
    }

    public static AutoList<T> operator+(AutoList<T> _, T auto) {
        list.Add(auto);
        return _;
    }

    public static AutoList<T> operator-(AutoList<T> _, T auto) {
        list.Remove(auto);
        return _;
    }
}

} // Hapki

