#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PolarBezierCurveVisualizer2D))]
public class PolarBezierCurveVisualizer2DEditor : Editor
{
	void OnSceneGUI()
	{
		var viz = (PolarBezierCurveVisualizer2D)target;
		if (viz.points == null || viz.points.Count == 0) return;

		Vector2 origin = GetOrigin(viz);

		Undo.RecordObject(viz, "Move Polar Bezier Control Point");

		for (int i = 0; i < viz.points.Count; i++)
		{
			var pp = viz.points[i];
			Vector3 worldPos = PolarToWorld(origin, pp);

			float size = HandleUtility.GetHandleSize(worldPos) * (viz.handleSize * 1.8f);
			Handles.color = (i == 0) ? Color.green : (i == viz.points.Count - 1 ? Color.red : Color.yellow);

			Vector3 newWorldPos = Handles.FreeMoveHandle(
				worldPos,
				size,
				Vector3.zero,
				Handles.SphereHandleCap
			);

			if (newWorldPos != worldPos)
			{
				Vector2 d = (Vector2)newWorldPos - origin;
				pp.radius = d.magnitude;
				pp.angleDeg = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;

				EditorUtility.SetDirty(viz);
			}

			Handles.Label(worldPos + Vector3.up * size * 0.8f, $"P{i}");
		}
	}

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		var viz = (PolarBezierCurveVisualizer2D)target;

		EditorGUILayout.Space(10);
		EditorGUILayout.LabelField("Edit Points", EditorStyles.boldLabel);

		EditorGUILayout.BeginHorizontal();
		if (GUILayout.Button("Add Point"))
		{
			Undo.RecordObject(viz, "Add Control Point");
			AddPointNearLast(viz);
			EditorUtility.SetDirty(viz);
		}

		GUI.enabled = viz.points != null && viz.points.Count > 2;
		if (GUILayout.Button("Remove Last"))
		{
			Undo.RecordObject(viz, "Remove Control Point");
			viz.points.RemoveAt(viz.points.Count - 1);
			EditorUtility.SetDirty(viz);
		}
		GUI.enabled = true;

		EditorGUILayout.EndHorizontal();
	}

	static void AddPointNearLast(PolarBezierCurveVisualizer2D viz)
	{
		if (viz.points == null) viz.points = new System.Collections.Generic.List<PolarBezierCurveVisualizer2D.PolarPoint>();

		if (viz.points.Count == 0)
		{
			viz.points.Add(new PolarBezierCurveVisualizer2D.PolarPoint(2f, 0f));
			viz.points.Add(new PolarBezierCurveVisualizer2D.PolarPoint(2f, 45f));
			return;
		}

		var last = viz.points[viz.points.Count - 1];
		viz.points.Add(new PolarBezierCurveVisualizer2D.PolarPoint(
			Mathf.Max(0f, last.radius + 0.5f),
			last.angleDeg + 15f
		));
	}

	static Vector2 GetOrigin(PolarBezierCurveVisualizer2D viz)
	{
		Vector2 o = viz.origin ? (Vector2)viz.origin.position : (Vector2)viz.transform.position;
		return o + viz.originOffset;
	}

	static Vector3 PolarToWorld(Vector2 origin, PolarBezierCurveVisualizer2D.PolarPoint pp)
	{
		float rad = pp.angleDeg * Mathf.Deg2Rad;
		Vector2 p = origin + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * pp.radius;
		return new Vector3(p.x, p.y, 0f);
	}
}
#endif
