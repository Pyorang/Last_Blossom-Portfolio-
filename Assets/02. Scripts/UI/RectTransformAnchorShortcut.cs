#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class RectTransformAnchorShortcut
{
    // % (Ctrl), & (Alt), a (A키) 단축키로 작동합니다.
    [MenuItem("Tools/UI/Anchors to Corners %&a")]
    private static void AnchorsToCorners()
    {
        // 현재 선택된 오브젝트가 없으면 중단합니다.
        if (Selection.activeGameObject == null) return;

        foreach (GameObject go in Selection.gameObjects)
        {
            RectTransform t = go.GetComponent<RectTransform>();
            RectTransform pt = t.parent as RectTransform;

            if (t == null || pt == null) continue;

            // 되돌리기(Undo)를 지원하도록 기록합니다.
            Undo.RecordObject(t, "Anchors to Corners");

            // 현재 위치 기준으로 앵커의 최소/최대 값을 계산합니다.
            Vector2 newMin = new Vector2(t.anchorMin.x + t.offsetMin.x / pt.rect.width,
                                         t.anchorMin.y + t.offsetMin.y / pt.rect.height);
            Vector2 newMax = new Vector2(t.anchorMax.x + t.offsetMax.x / pt.rect.width,
                                         t.anchorMax.y + t.offsetMax.y / pt.rect.height);

            t.anchorMin = newMin;
            t.anchorMax = newMax;

            // 앵커가 모서리에 맞춰졌으므로 오프셋 값을 0으로 초기화합니다.
            t.offsetMin = Vector2.zero;
            t.offsetMax = Vector2.zero;
        }
    }
}
#endif
