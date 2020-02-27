
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gameplay.Editor {

static class GameGizmos {
	[DrawGizmo(GizmoType.Pickable | GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
	static void DrawSpawnPoint(SpawnPoint point, GizmoType type) {
		var scale = point.transform.localScale.y;
		var position = point.transform.position;

		if ((type & GizmoType.Selected) != 0) {
			Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
			Gizmos.DrawSphere(position, scale);
			Gizmos.DrawWireSphere(position, scale);

			Gizmos.color = new Color(0f, 1f, 1f, 1f);
			Gizmos.matrix = point.transform.localToWorldMatrix;
			Gizmos.DrawCube(Vector3.forward * 0.5f, new Vector3(0.1f, 0.1f, 1f));
			Gizmos.DrawCube(Vector3.zero, new Vector3(0.5f, 0.1f, 0.1f));
		}

		Gizmos.DrawIcon(position, "SpawnPoint.png");
	}
}

} // Gameplay.Editor

