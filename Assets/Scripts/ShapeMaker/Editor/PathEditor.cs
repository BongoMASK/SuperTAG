using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PathCreator))]
public class PathEditor : Editor {

    PathCreator creator;
    Path path;

    void OnSceneGUI() {
        Input();
        Draw();
    }

    void Input() {
        Event guiEvent = Event.current;
        Vector2 mousePos = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition).origin;

        if (guiEvent.type == EventType.MouseDown && guiEvent.button == 0 && guiEvent.shift) {
            Undo.RecordObject(creator, "Add segment");
            path.AddSegment(mousePos);
        }
    }

    void Draw() {

        for (int i = 0; i < path.NumSegments; i++) {
            Vector2[] points = path.GetPointsInSegment(i);
            Handles.color = Color.black;
            //Handles.DrawLine(points[1], points[0]);
            //Handles.DrawLine(points[2], points[3]);
            //Handles.DrawBezier(points[0], points[3], points[1], points[2], Color.green, null, 2);
            for (int j = 0; j < points.Length; j++) {
                if(j+3 < points.Length)
                    Handles.DrawLine(points[j], points[j + 3]);
            }
        }

        Handles.color = Color.red;
        for (int i = 0; i < path.NumPoints; i++) {
            var fmh_43_62_638799161353190846 = Quaternion.identity; Vector2 newPos = Handles.FreeMoveHandle(path[i], .1f, Vector2.zero, Handles.CylinderHandleCap);
            if (path[i] != newPos) {
                Undo.RecordObject(creator, "Move point");
                path.MovePoint(i, newPos);
            }
        }
    }

    void OnEnable() {
        creator = (PathCreator)target;
        if (creator.path == null) {
            creator.CreatePath();
        }
        path = creator.path;
    }
}