using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PainterWindow : EditorWindow {
  [MenuItem("Editor/Painter")]
  static void ShowWindow() {
    EditorWindow.GetWindow<PainterWindow>(false, "Painter", true);
  }

  GUISkin skin;

  Vector3 mHitPos = Vector3.zero;
  Vector3 mHitNrm = Vector3.up;
  float mBrushSize = 0.5f;
  float mBrushPressure = 0.25f;
  Color mBrushColor = Color.black;
  bool mIsPainting = false;
  bool mIsCursorOnTarget = false;
  Tool mLastTool = Tool.None;
  Mesh mMesh = null;
  Collider mColl = null;
  readonly List<Vector3> mVerts = new List<Vector3>();
  readonly List<Color> mColors = new List<Color>();

  void OnEnable() {
    skin = Resources.Load<GUISkin>("Skin");

    SceneView.duringSceneGui -= onSceneGUI;
    SceneView.duringSceneGui += onSceneGUI;

    Selection.selectionChanged -= OnSelectionChanged;
    Selection.selectionChanged += OnSelectionChanged;
  }

  void OnDestroy() {
    SceneView.duringSceneGui -= onSceneGUI;
    Selection.selectionChanged -= OnSelectionChanged;
  }

  void onSceneGUI(SceneView view) {
    Handles.BeginGUI();
    GUILayout.BeginArea(new Rect(10, 10, 100, 100), GUI.skin.box);

    bool isPaintingChanged = GUILayout.Toggle(mIsPainting, "isPainting", GUI.skin.button);
    if (isPaintingChanged != mIsPainting) {
      mIsPainting = OnTogglePainting(isPaintingChanged);
    }

    mBrushColor = EditorGUILayout.ColorField(mBrushColor);

    GUILayout.EndArea();
    Handles.EndGUI();

    /* Events */
    var e = Event.current;

    if (e.type == EventType.KeyUp) {
      if (e.keyCode == KeyCode.U) {
        mIsPainting = OnTogglePainting(!mIsPainting);
      }
    }

    if (mIsPainting) {
      HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
      // int controlId = GUIUtility.GetControlID(FocusType.Passive);
      // GUIUtility.hotControl = controlId;

      if (e.type == EventType.MouseMove || e.type == EventType.MouseDrag) {
        var ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        bool currentCursorOnTarget = mColl.Raycast(ray, out var hit, 5f);

        if (currentCursorOnTarget != mIsCursorOnTarget) {
          mIsCursorOnTarget = currentCursorOnTarget;
          view.Repaint();
        }


        if (currentCursorOnTarget) {
          mHitPos = hit.point;
          mHitNrm = hit.normal;

          view.Repaint();
        }
      }

      if (e.type == EventType.MouseDrag && e.button == 0) {
        if (e.control) {
          mBrushSize = Mathf.Clamp(mBrushSize + e.delta.x * 0.01f, 0.2f, 1f);
          e.Use();
        }
        else if (e.shift) {
          mBrushPressure = Mathf.Clamp01(mBrushPressure + e.delta.x * 0.005f);
          e.Use();
        }
        else {
          for (var i = 0; i < mVerts.Count; i++) {
            var pos = mColl.transform.TransformPoint(mVerts[i]);
            float sqrMag = (pos - mHitPos).sqrMagnitude;

            if (sqrMag > mBrushSize*mBrushSize) {
              continue;
            }

            mColors[i] = mBrushColor;
          }
          
          mMesh.SetColors(mColors);
        }
      }

      if (e.type == EventType.Repaint) {
        // Debug.Log("REPAINT");

        if (mIsCursorOnTarget) {
          Handles.DrawWireDisc(mHitPos, mHitNrm, mBrushSize);
          Handles.color = new Color(1, 0, 0, mBrushPressure);
          Handles.DrawSolidDisc(mHitPos, mHitNrm, mBrushSize);
        }
      }
    }
  }

  void OnGUI() {
    GUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
    GUILayout.Box("This is long box name", skin.box);
    GUILayout.Button("Button");
    GUILayout.EndHorizontal();

    GUI.Box(new Rect(20, 20, 80, 80), "Absolute");

    GUILayout.Button("Button");
  }

  bool OnTogglePainting(bool isPaintingChanged) {
    if (!mColl) {
      return false;
    }

    if (isPaintingChanged) {
      mLastTool = Tools.current;
    }

    Tools.current = isPaintingChanged ? Tool.None : mLastTool;

    return isPaintingChanged;
  }

  void OnSelectionChanged() {
    mMesh = null;
    mColl = null;
    mVerts.Clear();
    mColors.Clear();

    if (mIsPainting) {
      OnTogglePainting(false);
    }

    var go = Selection.activeGameObject;

    if (go) {
      var mf = go.GetComponent<MeshFilter>();
      var coll = go.GetComponent<Collider>();
      if (mf && coll) {
        mColl = coll;
        mMesh = mf.sharedMesh;

        EnsureCapacity(mVerts, mMesh.vertexCount);
        mMesh.GetVertices(mVerts);

        EnsureCapacity(mColors, mMesh.vertexCount);
        if (mMesh.colors.Length == 0) {
          FillDefaultColors();
          mMesh.SetColors(mColors);
        }
        else {
          mMesh.GetColors(mColors);
        }
      }
    }
  }

  void EnsureCapacity<T>(List<T> list, int num) {
    if (list.Capacity < num) {
      list.Capacity = num;
    }
  }

  void FillDefaultColors() {
    mColors.Clear();
    for (var i = 0; i < mColors.Capacity; ++i) {
      mColors.Add(Color.black);
    }
  }
}