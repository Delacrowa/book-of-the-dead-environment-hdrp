
// Convenience utilities written by Malte Hildingsson, malte@hapki.se.
// No copyright is claimed, and you may use it for any purpose you like.
// No warranty for any purpose is expressed or implied.

using System.Collections.Generic;

namespace Hapki.Syntax {

public interface IReductionSyntax<T> where T : IReductionSyntax<T> {
    void OnReduce(List<ReductionSyntax<T>> nodes, int index);
}

public struct ReductionSyntax<T> where T : IReductionSyntax<T> {
    static readonly List<ReductionSyntax<T>> nodes = new List<ReductionSyntax<T>>();

    readonly T _data;
    readonly int _index;

    ReductionSyntax(T data) {
        _data = data;
        _index = nodes.Count;
        nodes.Add(this);
    }

    void Reduce() {
        if (nodes.Count > _index + 1) {
            _data.OnReduce(nodes, _index + 1);
            if (_index > 0)
                nodes.RemoveRange(_index + 1, nodes.Count - (_index + 1));
            else
                nodes.Clear();
        }
    }

    public ReductionSyntax<T> this[T _] {
        get { return this; }
    }

    public static T operator-(ReductionSyntax<T> node) {
        node.Reduce();
        return node;
    }

    public static T operator-(T _, ReductionSyntax<T> node) {
        return -node;
    }

    public static implicit operator ReductionSyntax<T>(T data) {
        return new ReductionSyntax<T>(data);
    }

    public static implicit operator T(ReductionSyntax<T> node) {
        return node._data;
    }
}

} // Hapki.Syntax

