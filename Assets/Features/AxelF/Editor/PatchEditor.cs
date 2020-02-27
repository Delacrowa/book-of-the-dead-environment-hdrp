
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace AxelF {
namespace Editor {

[CanEditMultipleObjects]
[CustomEditor(typeof(Patch))]
public class PatchEditor : UnityEditor.Editor {
    float _random;
    float[] _weights;
    string _played;
    bool _foldout;

    public override void OnInspectorGUI() {
        var a = (Patch) target;

        ColorizeDrawer.Reset();
        DrawDefaultInspector();
        GUILayout.Space(16);

        if (GUILayout.Button("Set Clips To Selected")) {
            var assets = Selection.GetFiltered(typeof(AudioClip), SelectionMode.Assets);
            Array.Sort(assets, (x, y) => string.Compare(x.name, y.name));
            a.program.clips = new AudioProgram.AudioClipParams[assets.Length];

            for (int i = 0, n = assets.Length; i < n; ++i)
                a.program.clips[i] = new AudioProgram.AudioClipParams {clip = (AudioClip) assets[i]};
        }

        GUILayout.Space(16);

        if (a.sequence != null && a.sequence.timing != null && a.sequence.timing.Length > 0)
            DrawAudioSequenceInspectorGUI(a);
        else if (a.program != null)
            DrawAudioProgramInspectorGUI(a.program);
    }

    void DrawAudioProgramInspectorGUI(AudioProgram a) {
        AudioClip c = null;

        GUILayout.BeginHorizontal();
        GUI.color = new Color(0.75f, 1.00f, 0.75f);
        if (GUILayout.Button("\u25b6")) {
            float gain;
            if (a.randomize) {
                _random = Randomizer.zeroToOne;
                a.weighted.count = a.clips.Length;
                _weights = (float[]) a.weighted.weights.Clone();
                c = a.GetClip(_random, out gain);
            } else
                c = a.GetClip(out gain);
            if (c != null) {
                _played = c.name;
                Synthesizer.KeyOn(null, c, a.parameters, null, new Vector3(), 1f + gain);
            }
        }
        GUI.color = new Color(1.00f, 0.75f, 0.75f);
        if (GUILayout.Button("\u2585"))
            Synthesizer.StopAll();
        GUILayout.EndHorizontal();
        GUILayout.Space(16);

        GUI.color = Color.white;
        _foldout = EditorGUILayout.Foldout(_foldout, "Randomization");
        if (_foldout && _weights != null) {
            float s = 0;
            for (int i = 0, n = _weights.Length; i < n; ++i)
                s += _weights[i];
            float t = _random * s;
            GUILayout.BeginHorizontal();
            GUILayout.Label(s.ToString("N2"));
            GUILayout.Label(t.ToString("N2"));
            GUILayout.Label("\t");
            GUILayout.EndHorizontal();
            for (int i = 0, n = _weights.Length; i < n; ++i) {
                if (t >= _weights[i])
                    GUI.color = Color.white;
                else if (a.clips[i].clip.name == _played)
                    GUI.color = Color.green;
                else
                    GUI.color = Color.gray;
                t -= _weights[i];
                GUILayout.BeginHorizontal();
                GUILayout.Label(_weights[i].ToString("N2"));
                GUILayout.Label(t.ToString("N2"));
                GUILayout.Label(a.clips[i].clip.name);
                GUILayout.EndHorizontal();
            }
        }
    }

    void DrawAudioSequenceInspectorGUI(Patch patch) {
        bool looping;

        GUILayout.BeginHorizontal();
        GUI.color = new Color(0.75f, 1.00f, 0.75f);
        if (GUILayout.Button("\u25b6"))
            Synthesizer.KeyOn(out looping, patch);
        GUI.color = new Color(1.00f, 0.75f, 0.75f);
        if (GUILayout.Button("\u2585"))
            Synthesizer.StopAll();
        GUILayout.EndHorizontal();
        GUILayout.Space(16);
    }
}

} // Editor
} // AxelF

