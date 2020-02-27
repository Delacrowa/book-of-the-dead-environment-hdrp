using System;
using System.Collections.Generic;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public sealed class LinkedRangeAttribute : PropertyAttribute {
    public readonly float min;
    public readonly float max;
    public readonly string linkedField;
    public readonly string linkField;

    public LinkedRangeAttribute(float min, float max, string linkedField, string linkField) {
        this.min = min;
        this.max = max;
        this.linkedField = linkedField;
        this.linkField = linkField;
    }
}