
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AxelF {
namespace Editor {

public class Monitor : EditorWindow {
    static Dictionary<string, int> _srcColors = new Dictionary<string, int>();
    public static int GetSourceColor(string name) {
        int i;
        _srcColors.TryGetValue(name, out i);
        return i;
    }

    static StringBuilder _builder = new StringBuilder(256);
    static Dictionary<int, string> _intLookup = new Dictionary<int, string>();

    static Texture2D GetIcon() {
        const int w = 16;
        var p = new Color[w * w];
        for (int i = 0; i < w * w; ++i) {
            if (i%w == 0 || i%w == 1)
                p[i] = Color.black;
            else if (i%w == w/2-1 || i%w == w/2)
                p[i] = Color.black;
            else if (i%w == w-2 || i%w == w-1)
                p[i] = Color.black;
            else if (i/w == 0 || i/w == 1)
                p[i] = Color.black;
            else if (i/w == w/4*1-1 || i/w == w/4*1)
                p[i] = Color.black;
            else if (i/w == w/4*2-1 || i/w == w/4*2)
                p[i] = Color.black;
            else if (i/w == w/4*3-1 || i/w == w/4*3)
                p[i] = Color.black;
            else if (i/w == w-2 || i/w == w-1)
                p[i] = Color.black;
            else if (i/w < w/4*1)
                p[i] = Color.green;
            else if (i/w < w/4*2)
                p[i] = Color.green;
            else if (i/w < w/4*3)
                p[i] = Color.yellow;
            else if (i/w < w/4*4)
                p[i] = Color.red;
        }
        var icon = new Texture2D(w, w, TextureFormat.RGBA32, false);
        icon.hideFlags = HideFlags.HideAndDontSave;
        icon.filterMode = FilterMode.Point;
        icon.SetPixels(p);
        icon.Apply();
        return icon;
    }

    List<Synthesizer.ActiveSource> _srcInfo;
    GUIStyle _lineStyle;
    int _paintCount;
    static bool _showSynthesizer = true;
    static bool _showSequencer = true;
    static bool _showOcclusion;
    Vector3 _scrollOcclusion;
    Vector3 _scrollSynthesizer;
    Vector3 _scrollSequencer;

    [MenuItem("AxelF/Window/Monitor")]
    static void Open() {
        ((Monitor) EditorWindow.GetWindow(typeof(Monitor))).Show();
    }

    protected void OnEnable() {
        titleContent = new GUIContent("AxelF Monitor", GetIcon());

        _srcInfo = new List<Synthesizer.ActiveSource>();

        _lineStyle = new GUIStyle();
        _lineStyle.normal.background = EditorGUIUtility.whiteTexture;
        _lineStyle.stretchWidth = true;
        _lineStyle.margin = new RectOffset(0, 0, 7, 7);

        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        SceneView.onSceneGUIDelegate += this.OnSceneGUI;
    }

    protected void OnDisable() {
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
    }

    void OnPlayModeStateChanged(PlayModeStateChange change) {
        EditorApplication.update -= Repaint;
        if (Application.isPlaying)
            EditorApplication.update += Repaint;
    }

    protected void OnSceneGUI(SceneView sv) {
        if (!Heartbeat.listenerTransform)
            return;

        var p = Heartbeat.listenerTransform.position;
        var c = Handles.color;

        for (var i = Synthesizer.activeSources0.GetEnumerator(); i.MoveNext();)
            if (i.Current.keyOn <= 0f) {
                int j = Monitor.GetSourceColor(i.Current.info.audioSource.name);
                Handles.color = ColorizeDrawer.GetColor(j);
                var q = i.Current.info.audioSource.transform.position;
                var d = p - q;
                if (d.magnitude < 1f)
                    continue;
                var e = d.normalized;
                Handles.DrawLine(p - e*0.5f, q + e*0.5f);
                Handles.ConeHandleCap(
                    0, p - e*0.5f, Quaternion.LookRotation(e), 0.05f, Event.current.type);

                _builder.Append("\n\n");
                var ac = i.Current.info.audioSource.clip;
                _builder.Append(ac != null ? ac.name : "??");
                _builder.Append(" (");
                _builder.Append(i.Current.patch != null ? i.Current.patch.name : "-");
                _builder.Append(")");

                var s = _builder.ToString();
                _builder.Length = 0;

                Handles.BeginGUI();
                var k = GUI.color;
                GUI.color = Handles.color;
                var a = p - d * 0.5f;
                var x = new GUIContent(i.Current.info.audioSource.name);
                var y = new GUIContent(s);
                var l = HandleUtility.WorldPointToSizedRect(a, x, EditorStyles.boldLabel);
                var m = HandleUtility.WorldPointToSizedRect(a, y, EditorStyles.helpBox);
                float n = Mathf.Max(l.width, m.width);
                l.width = n;
                m.width = n;
                l.x -= m.width * 1.2f;
                l.y -= m.height * 0.5f;
                m.x = l.x - Mathf.Max(0f, m.width - l.width);
                m.y = l.y + 1f;
                EditorGUI.HelpBox(m, y.text, MessageType.None);
                EditorGUI.DropShadowLabel(l, x);
                GUI.color = k;
                Handles.EndGUI();
            }

        Handles.color = c;
    }

    protected void OnGUI() {
        ColorizeDrawer.Reset();
        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(110), GUILayout.ExpandHeight(true));
        DrawToolbarGUI();
        GUILayout.EndVertical();

        GUILayout.BeginVertical();
        DrawHeader();

        bool any = false;
        if (_showSynthesizer) {
            any = true;
            DrawSynthesizerGUI();
        }
        if (any) {
            any = false;
            DrawLine();
        }
        if (_showSequencer) {
            any = true;
            DrawSequencerGUI();
        }
        if (any) {
            any = false;
            DrawLine();
        }
        if (_showOcclusion) {
            any = true;
            DrawOcclusionGUI();
        }

        GUILayout.EndVertical();

        GUILayout.EndHorizontal();
    }

    void DrawToolbarGUI() {
        GUILayout.Label("Views", EditorStyles.boldLabel);
        DrawLine();
        _showSynthesizer = GUILayout.Toggle(_showSynthesizer, "Synthesizer");
        _showSequencer = GUILayout.Toggle(_showSequencer, "Sequencer");
        _showOcclusion = GUILayout.Toggle(_showOcclusion, "Occlusion");
    }

    void DrawOcclusionGUI() {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Mute", EditorStyles.boldLabel, GUILayout.Width(55));
        GUILayout.Label("Mix Group", EditorStyles.boldLabel, GUILayout.Width(110));
        GUILayout.Label("Output", EditorStyles.boldLabel, GUILayout.Width(55));
        GUILayout.Label("Source", EditorStyles.boldLabel, GUILayout.Width(110));
        GUILayout.Label("Patch", EditorStyles.boldLabel, GUILayout.Width(220));
        GUILayout.Label("Function", EditorStyles.boldLabel, GUILayout.Width(75));
        GUILayout.Label("Distance", EditorStyles.boldLabel, GUILayout.Width(75));
        GUILayout.Label("Occlusion", EditorStyles.boldLabel, GUILayout.Width(75));
        EditorGUILayout.EndHorizontal();

        int i = 0;
        var c = GUI.color;
        Rect p;
        _scrollOcclusion = EditorGUILayout.BeginScrollView(_scrollOcclusion);

        if (Application.isPlaying) {
            _srcColors.Clear();
            _srcInfo.Clear();
            for (var x = Synthesizer.activeSources0.GetEnumerator(); x.MoveNext();)
                _srcInfo.Add(x.Current);
            for (var x = Synthesizer.freeSources.GetEnumerator(); x.MoveNext();)
                _srcInfo.Add(new Synthesizer.ActiveSource {info = x.Current});
            _srcInfo.Sort((x, y) => string.Compare(x.info.audioSource.name, y.info.audioSource.name));

            var lp = Heartbeat.listenerTransform.position;
            for (var x = _srcInfo.GetEnumerator(); x.MoveNext();) {
                var z = x.Current;
                if (z.info.occlusion.occlusion.function == OcclusionFunction.None)
                    continue;

                EditorGUILayout.BeginHorizontal();
                float v;
                bool dis = z.handle == 0 || z.keyOn > 0f;
                {
                    _srcColors[z.info.audioSource.name] = i;
                    if (dis)
                        GUI.color = ColorizeDrawer.disabledColor;
                    else
                        GUI.color = ColorizeDrawer.GetColor(i);
                    // Mute
                    if (z.info.audioSource != null)
                        z.info.audioSource.mute = EditorGUILayout.Toggle(
                            z.info.audioSource.mute, GUILayout.Width(50));
                    else
                        EditorGUILayout.Toggle(false, GUILayout.Width(50));
                    GUILayout.Space(5);
                    // Mix Group
                    GUILayout.Label(
                        (!dis && z.info.audioSource &&
                            z.info.audioSource.outputAudioMixerGroup != null ?
                                z.info.audioSource.outputAudioMixerGroup.name :
                                "(None)"),
                        GUILayout.Width(100));
                    GUILayout.Space(10);
                    // Output
                    GUILayout.Label(
                        (!dis && !z.info.audioSource.isVirtual ?
                            "\u2713" : ""),
                        GUILayout.Width(50));
                    GUILayout.Space(5);
                    // Source
                    GUILayout.Label(
                        (!dis ? z.info.audioSource.name : ""),
                        GUILayout.Width(110));
                    // Patch
                    GUILayout.Label(
                        (!dis ? z.patch.name : ""),
                        GUILayout.Width(220));
                    // Function
                    GUILayout.Label(
                        (!dis ? z.info.occlusion.occlusion.function.ToString() : ""),
                        GUILayout.Width(70));
                    GUILayout.Space(5);
                    // Distance
                    v = !dis ?
                        (z.info.audioSource.transform.position - lp).magnitude : 0f;
                    GUILayout.Label(v.ToString("N3"), GUILayout.Width(70));
                    GUILayout.Space(5);
                    // Occlusion
                    p = GUILayoutUtility.GetRect(
                        GUIContent.none, EditorStyles.label, GUILayout.Width(70));
                    v = !dis ? 1f - z.info.occlusion.GetCurrent() : 0f;
                    EditorGUI.ProgressBar(p, v, v.ToString("N3"));
                    GUILayout.Space(5);
                }

                EditorGUILayout.EndHorizontal();
                ++i;
            }
        }

        GUI.color = c;
        EditorGUILayout.EndScrollView();
    }

    void DrawLine() {
        var c = GUI.color;
        var p = GUILayoutUtility.GetRect(GUIContent.none, _lineStyle, GUILayout.Height(1));
        if (Event.current.type == EventType.Repaint) {
            GUI.color = EditorGUIUtility.isProSkin ?
                new Color(0.157f, 0.157f, 0.157f) : new Color(0.5f, 0.5f, 0.5f);
            _lineStyle.Draw(p, false, false, false, false);
        }
        GUI.color = c;
    }

    void DrawHeader() {
        string nas, nfs, azs, a5s;
        int na = Synthesizer.activeSources0.Count;
        if (!_intLookup.TryGetValue(na, out nas)) {
            nas = na.ToString();
            _intLookup[na] = nas;
        }
        int nf = Synthesizer.freeSources.Count;
        if (!_intLookup.TryGetValue(nf, out nfs)) {
            nfs = nf.ToString();
            _intLookup[nf] = nfs;
        }
        int az = 0;
        foreach (var i in Zone.allZones)
            if (i.IsActive()) {
                if (_builder.Length > 0)
                    _builder.Append(", ");
                _builder.Append(i.name);
                ++az;
            }
        if (az == 0) {
            if (!_intLookup.TryGetValue(az, out azs)) {
                azs = az.ToString();
                _intLookup[az] = azs;
            }
        } else {
            if (_builder.Length > 0) {
                _builder.Insert(0, " (");
                _builder.Append(")");
            }
            _builder.Insert(0, az);
            if (_builder.Length > 50) {
                _builder.Length = 50;
                _builder.Append("...");
            }
            azs = _builder.ToString();
            _builder.Length = 0;
        }
        int a5 = AudioSlapback.allSlapbacks.Count;
        if (!_intLookup.TryGetValue(a5, out a5s)) {
            a5s = a5.ToString();
            _intLookup[a5] = a5s;
        }
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Active Sources", EditorStyles.boldLabel, GUILayout.Width(110));
        GUILayout.Label(nas, EditorStyles.boldLabel, GUILayout.Width(35));
        GUILayout.Label("Free Sources", EditorStyles.boldLabel, GUILayout.Width(100));
        GUILayout.Label(nfs, EditorStyles.boldLabel, GUILayout.Width(110));
        GUILayout.Label("Active Zones", EditorStyles.boldLabel, GUILayout.Width(95));
        GUILayout.Label(azs, EditorStyles.boldLabel, GUILayout.Width(220));
        GUILayout.Label("Active Slapbacks", EditorStyles.boldLabel, GUILayout.Width(125));
        GUILayout.Label(a5s, EditorStyles.boldLabel, GUILayout.Width(110));
        EditorGUILayout.EndHorizontal();

        DrawLine();
    }

    void DrawSynthesizerGUI() {
        bool shift = Event.current.shift;

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Mute", EditorStyles.boldLabel, GUILayout.Width(55));
        GUILayout.Label("Mix Group", EditorStyles.boldLabel, GUILayout.Width(110));
        GUILayout.Label("Output", EditorStyles.boldLabel, GUILayout.Width(55));
        GUILayout.Label("Source", EditorStyles.boldLabel, GUILayout.Width(110));
        GUILayout.Label(shift ? "Audio Clip" : "Patch", EditorStyles.boldLabel, GUILayout.Width(220));
        GUILayout.Label("Keyed", EditorStyles.boldLabel, GUILayout.Width(55));
        GUILayout.Label("Time", EditorStyles.boldLabel, GUILayout.Width(110));
        GUILayout.Label("Envelope", EditorStyles.boldLabel, GUILayout.Width(110));
        GUILayout.Label("Volume", EditorStyles.boldLabel, GUILayout.Width(110));
        GUILayout.Label("Distance", EditorStyles.boldLabel, GUILayout.Width(75));
        EditorGUILayout.EndHorizontal();

        int i = 0;
        var c = GUI.color;
        Rect p;
        _scrollSynthesizer = EditorGUILayout.BeginScrollView(_scrollSynthesizer);

        if (Application.isPlaying && Heartbeat.listenerTransform) {
            _srcColors.Clear();
            _srcInfo.Clear();
            for (var x = Synthesizer.activeSources0.GetEnumerator(); x.MoveNext();)
                _srcInfo.Add(x.Current);
            for (var x = Synthesizer.freeSources.GetEnumerator(); x.MoveNext();)
                _srcInfo.Add(new Synthesizer.ActiveSource {info = x.Current});
            _srcInfo.Sort((x, y) => string.Compare(x.info.audioSource.name, y.info.audioSource.name));

            var lp = Heartbeat.listenerTransform.position;
            for (var x = _srcInfo.GetEnumerator(); x.MoveNext();) {
                var z = x.Current;

                EditorGUILayout.BeginHorizontal();
                float v;
                bool dis = z.handle == 0 || z.keyOn > 0f;
                {
                    _srcColors[z.info.audioSource.name] = i;
                    if (dis)
                        GUI.color = ColorizeDrawer.disabledColor;
                    else
                        GUI.color = ColorizeDrawer.GetColor(i);
                    // Mute
                    if (z.info.audioSource != null)
                        z.info.audioSource.mute = EditorGUILayout.Toggle(
                            z.info.audioSource.mute, GUILayout.Width(50));
                    else
                        EditorGUILayout.Toggle(false, GUILayout.Width(50));
                    GUILayout.Space(5);
                    // Mix Group
                    GUILayout.Label(
                        (!dis && z.info.audioSource &&
                            z.info.audioSource.outputAudioMixerGroup != null ?
                                z.info.audioSource.outputAudioMixerGroup.name :
                                "(None)"),
                        GUILayout.Width(100));
                    GUILayout.Space(10);
                    // Output
                    GUILayout.Label(
                        (!dis && !z.info.audioSource.isVirtual ?
                            "\u2713" : ""),
                        GUILayout.Width(50));
                    GUILayout.Space(5);
                    // Source
                    GUILayout.Label(
                        (!dis ? z.info.audioSource.name : ""),
                        GUILayout.Width(110));
                    // Patch
                    if (shift)
                        GUILayout.Label(
                            (!dis ? z.info.audioSource.clip != null ?
                                z.info.audioSource.clip.name : "-" : ""),
                            GUILayout.Width(220));
                    else
                        GUILayout.Label(
                            (!dis ? z.patch != null ? z.patch.name : "-" : ""),
                            GUILayout.Width(220));
                    // Keyed
                    if (dis || z.keyOff)
                        GUILayout.Label("Off", GUILayout.Width(50));
                    else
                        GUILayout.Label("On", GUILayout.Width(50));
                    GUILayout.Space(5);
                    // Time
                    p = GUILayoutUtility.GetRect(
                        GUIContent.none, EditorStyles.label, GUILayout.Width(100));
                    v = !dis ? z.info.audioSource.time : 0f;
                    EditorGUI.ProgressBar(
                        p, v / (!dis ? z.info.audioSource.clip.length : 1f),
                        v.ToString("N3"));
                    GUILayout.Space(10);
                    // Envelope
                    p = GUILayoutUtility.GetRect(
                        GUIContent.none, EditorStyles.label, GUILayout.Width(50));
                    v = !dis ? z.envelope.GetAttackValue() : 0f;
                    EditorGUI.ProgressBar(p, v, v.ToString("N3"));
                    GUILayout.Space(5);
                    p = GUILayoutUtility.GetRect(
                        GUIContent.none, EditorStyles.label, GUILayout.Width(50));
                    v = !dis ? z.envelope.GetReleaseValue() : 0f;
                    EditorGUI.ProgressBar(p, v, v.ToString("N3"));
                    GUILayout.Space(5);
                    // Volume
                    p = GUILayoutUtility.GetRect(
                        GUIContent.none, EditorStyles.label, GUILayout.Width(100));
                    v = !dis ? z.GetVolume() : 0f;
                    EditorGUI.ProgressBar(p, v, v.ToString("N3"));
                    GUILayout.Space(10);
                    // Distance
                    v = !dis ?
                        (z.info.audioSource.transform.position - lp).magnitude : 0f;
                    GUILayout.Label(v.ToString("N3"), GUILayout.Width(70));
                    GUILayout.Space(5);
                }

                EditorGUILayout.EndHorizontal();
                ++i;
            }
        }

        GUI.color = c;
        EditorGUILayout.EndScrollView();
    }

    void DrawSequencerGUI() {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Pause", EditorStyles.boldLabel, GUILayout.Width(55));
        GUILayout.Label("Emitter", EditorStyles.boldLabel, GUILayout.Width(110));
        GUILayout.Label("Patch", EditorStyles.boldLabel, GUILayout.Width(220));
        GUILayout.Label("Period", EditorStyles.boldLabel, GUILayout.Width(110));
        GUILayout.Label("Repeat", EditorStyles.boldLabel, GUILayout.Width(75));
        GUILayout.Label("Looping", EditorStyles.boldLabel, GUILayout.Width(55));
        EditorGUILayout.EndHorizontal();

        int i = 0;
        var c = GUI.color;
        Rect p;
        _scrollSequencer = EditorGUILayout.BeginScrollView(_scrollSequencer);

        if (Application.isPlaying) {
            for (var x = Sequencer.activeCues0.GetEnumerator(); x.MoveNext();) {
                GUI.color = ColorizeDrawer.GetColor(i);
                var z = x.Current;
                EditorGUILayout.BeginHorizontal();

                // Pause
                if (z.emitter)
                    z.emitter.SetPaused(
                        EditorGUILayout.Toggle(
                            z.emitter.IsPaused(), GUILayout.Width(50)));
                else
                    EditorGUILayout.Toggle(false, GUILayout.Width(50));
                GUILayout.Space(5);
                // Emitter
                GUILayout.Label(z.emitter.name, GUILayout.Width(110));
                // Patch
                GUILayout.Label(z.emitter.patches[z.index].name, GUILayout.Width(220));
                // Period
                p = GUILayoutUtility.GetRect(
                    GUIContent.none, EditorStyles.label, GUILayout.Width(100));
                if (z.totalTime > 0f)
                    EditorGUI.ProgressBar(
                        p, z.currentTime / z.totalTime, z.currentTime.ToString("N3"));
                else if (z.emitter.modulation.period > 0f) {
                    float t = z.currentTime % z.emitter.modulation.period;
                    EditorGUI.ProgressBar(
                        p, t / z.emitter.modulation.period,
                        t.ToString("N3"));
                } else
                    EditorGUI.ProgressBar(p, 0f, z.currentTime.ToString("N3"));
                GUILayout.Space(10);
                // Repeat
                if (z.repeatCount == 0)
                    GUILayout.Label("Never", GUILayout.Width(70));
                else if (z.repeatCount > 0) {
                    string ris;
                    if (!_intLookup.TryGetValue(z.repeatIndex, out ris)) {
                        ris = z.repeatIndex.ToString();
                        _intLookup[z.repeatIndex] = ris;
                    }
                    p = GUILayoutUtility.GetRect(
                        GUIContent.none, EditorStyles.label, GUILayout.Width(70));
                    EditorGUI.ProgressBar(
                        p, z.repeatIndex / (float) z.repeatCount, ris);
                } else
                    GUILayout.Label("Forever", GUILayout.Width(70));
                GUILayout.Space(5);
                // Looping
                GUILayout.Label((z.looping ? "\u2713" : ""), GUILayout.Width(50));
                GUILayout.Space(5);

                EditorGUILayout.EndHorizontal();
                ++i;
            }

            GUI.color = c;
        }

        GUI.color = c;
        EditorGUILayout.EndScrollView();
    }
}

} // Editor
} // AxelF

