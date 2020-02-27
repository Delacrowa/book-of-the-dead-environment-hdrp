using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

[InitializeOnLoad]
public class ReadMe : EditorWindow {
    public static readonly string kRequireVersion = "2018.2.0b9";

    public static readonly string kShowOnStart = "BotDE.ReadMe.ShowOnStart";
    public static readonly int kShowOnStartCookie = 1;

    public static readonly string kShownThisSession = "BotDE.ReadMe.ShownThisSession";

    static string _salt;

    static string salt {
        get {
            if (string.IsNullOrEmpty(_salt))
                _salt = Application.dataPath.GetHashCode().ToString("X8");
            return _salt;
        }
    }

    static ReadMe() {
        EditorApplication.delayCall += () => {
            CheckPrefsAndShow();
            EnsureLinearProject();
        };
    }

    static int CompareVersions(string version1, string version2) {
        var regex = new Regex(@"^(?<major>\d+)(\.(?<minor>\d+))*((?<early>[a-z])(?<number>\d+))?");
        var match1 = regex.Match(version1);
        var match2 = regex.Match(version2);

        Func<string, int> compareInteger = group => {
            var captures1 = match1.Groups[group].Captures;
            var captures2 = match2.Groups[group].Captures;
            int count1 = captures1.Count;
            int count2 = captures2.Count;

            for (int i = 0; i < Mathf.Min(count1, count2); ++i) {
                int value1 = int.Parse(captures1[i].Value);
                int value2 = int.Parse(captures2[i].Value);

                if (value1 != value2)
                    return value1 - value2;
            }

            for (int i = count2; i < count1; ++i) {
                int value1 = int.Parse(captures1[i].Value);

                if (value1 != 0)
                    return value1 - 0;
            }

            for (int i = count1; i < count2; ++i) {
                int value2 = int.Parse(captures2[i].Value);

                if (value2 != 0)
                    return 0 - value2;
            }

            return 0;
        };

        Func<string, int> compareString = group => {
            var captures1 = match1.Groups[group].Captures;
            var captures2 = match2.Groups[group].Captures;
            int count1 = captures1.Count;
            int count2 = captures2.Count;

            for (int i = 0; i < Mathf.Min(count1, count2); ++i) {
                var value1 = captures1[i].Value;
                var value2 = captures2[i].Value;

                if (string.Compare(value1, value2) != 0)
                    return string.Compare(value1, value2);
            }

            return count2 - count1;
        };

        int diff;

        if ((diff = compareInteger("major")) != 0)
            return diff;

        if ((diff = compareInteger("minor")) != 0)
            return diff;

        if ((diff = compareString("early")) != 0)
            return diff;

        if ((diff = compareInteger("number")) != 0)
            return diff;

        return 0;
    }

    static string ParseMarkdown(string path) {
        // strict markdown subset:
        var h1 = new Regex(@"^\=+\s*$");
        var h2 = new Regex(@"^\-+\s*$");
        var hr = new Regex(@"^\*+\s*$");
        var li = new Regex(@"^\-\s+(.*)$");

        var lines = new Queue<string>(File.ReadAllLines(path));
        var builder = new StringBuilder();

        bool list = true;
        Action endList = () => {
            if (list)
                builder.Append('\n');
            list = false;
        };

        while (lines.Count > 0) {
            var line = lines.Dequeue();
            Match match;

            if (string.IsNullOrEmpty(line)) {
                endList();
                continue;
            } else if (hr.IsMatch(line)) {
                endList();
                builder.Append('\n');
                for (int i = 0; i < 20; ++i)
                    builder.Append("\u2e3b");
                builder.Append("\n\n");
                continue;
            } else if ((match = li.Match(line)).Success) {
                builder.AppendFormat(" \u2022 {0}\n", match.Groups[1].Captures[0].Value);
                list = true;
                continue;
            } else if (lines.Count > 0)
                if (h1.IsMatch(lines.Peek())) {
                    lines.Dequeue();
                    endList();
                    builder.AppendFormat("\n<size=24><b>{0}</b></size>\n\n", line);
                    continue;
                } else if (h2.IsMatch(lines.Peek())) {
                    lines.Dequeue();
                    endList();
                    builder.AppendFormat("\n<size=18><b>{0}</b></size>\n\n", line);
                    continue;
                }

            builder.AppendFormat("{0}\n\n", line);
        }

        return builder.ToString();
    }

    static void CheckPrefsAndShow() {
        int cookie = EditorPrefs.GetInt(kShowOnStart + salt, defaultValue: 0);
        if (cookie < kShowOnStartCookie && !SessionState.GetBool(kShownThisSession, defaultValue: false)) {
            EditorApplication.delayCall += ResetRenderPipeline;
            Show();
        }
    }

    [MenuItem("Help/About Book of the Dead: Environment", false, 1)]
    public static new void Show() {
        ((ReadMe) ScriptableObject.CreateInstance(typeof(ReadMe))).ShowUtility();
        SessionState.SetBool(kShownThisSession, true);
    }

    [PreferenceItem("Read Me")]
    static void OnPrefsGUI() {
        int cookie = EditorPrefs.GetInt(kShowOnStart + salt, defaultValue: 0);
        bool showOnStart = cookie < kShowOnStartCookie;

        if (EditorGUILayout.Toggle("Show On Start", showOnStart) != showOnStart)
            EditorPrefs.SetInt(kShowOnStart + salt, showOnStart ? kShowOnStartCookie : 0);
    }

    GUIStyle _style;
    Vector2 _scroll;
    string _text;

    protected void OnEnable() {
        titleContent = new GUIContent("About");
        minSize = new Vector2(640, 320);
        maxSize = new Vector2(1280, 960);

        try {
            _text = ParseMarkdown("Assets/ReadMe.md");
        } catch (Exception e) {
            Debug.LogException(e);
            _text = e.Message;
        }

        if (CompareVersions(Application.unityVersion, kRequireVersion) < 0)
            _text = "\n<color=red><size=18><b>This project requires at least Unity version " +
                kRequireVersion + ", current version is " + Application.unityVersion + "</b></size></color>" +
                _text;
    }

    protected void OnGUI() {
        if (_style == null) {
            _style = new GUIStyle(EditorStyles.textArea);
            _style.richText = true;
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        GUILayout.TextArea(_text, _style, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        int cookie = EditorPrefs.GetInt(kShowOnStart + salt, defaultValue: 0);
        bool showOnStart = cookie < kShowOnStartCookie;

        if (EditorGUILayout.ToggleLeft("Show On Start", showOnStart) != showOnStart)
            EditorPrefs.SetInt(kShowOnStart + salt, showOnStart ? kShowOnStartCookie : 0);

        if (GUILayout.Button("Load Book of the Dead: Environment")) {
            MultiSceneControllerEd.LoadForestScenes();
            Close();
        }

        if (GUILayout.Button("Load Asset Library")) {
            MultiSceneControllerEd.LoadAssetLibraryScene();
            Close();
        }
    }

    static void EnsureLinearProject() {
        if(PlayerSettings.colorSpace != ColorSpace.Linear) {
            Debug.Log("Forcing project to Linear colorspace.");

            PlayerSettings.colorSpace = ColorSpace.Linear;
            AssetDatabase.SaveAssets();
        }
    }

    static void ResetRenderPipeline() {
        var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
        if(hdrp) {
            Debug.Log("Resetting renderpipeline.");

            var fs = hdrp.GetFrameSettings();
            fs.enableVolumetrics = false;

            var miCleanUpRP = typeof(UnityEngine.Experimental.Rendering.RenderPipelineManager).GetMethod("CleanupRenderPipeline", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            miCleanUpRP.Invoke(null, null);
        }
    }
}
