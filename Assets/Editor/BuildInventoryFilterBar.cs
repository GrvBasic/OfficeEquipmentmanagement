using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class BuildInventoryFilterBar
{
    // Status filter buttons to create: (gameObjectName, label, fieldName, hex colour)
    static readonly string[,] Buttons = new string[,]
    {
        { "Btn_FilterAvailable", "Available", "filterAvailableButton", "#16a34a" },
        { "Btn_FilterAssigned",  "Assigned",  "filterAssignedButton",  "#2563eb" },
        { "Btn_FilterDamaged",   "Damaged",   "filterDamagedButton",   "#ea580c" },
        { "Btn_FilterConsumed",  "Consumed",  "filterConsumedButton",  "#6b7280" },
        { "Btn_FilterAll",       "All",       "filterAllInventoryButton", "#334155" },
    };

    public static string Execute()
    {
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return "Canvas not found";
        var panelT = canvas.transform.Find("ViewInventoryPanel");
        if (panelT == null) return "ViewInventoryPanel not found";
        var panel = panelT.gameObject;

        if (panel.transform.Find("FilterBar") != null)
            return "FilterBar already exists — nothing to do.";

        var backT = panel.transform.Find("Btn_Back");
        if (backT == null) return "Btn_Back not found (used as a style template)";

        // ── 1) FilterBar container, anchored just below the Title ─────────────
        var barGO = new GameObject("FilterBar",
            typeof(RectTransform), typeof(HorizontalLayoutGroup));
        barGO.transform.SetParent(panel.transform, false);

        var barRT = barGO.GetComponent<RectTransform>();
        barRT.anchorMin        = new Vector2(0f, 1f);
        barRT.anchorMax        = new Vector2(1f, 1f);
        barRT.pivot            = new Vector2(0.5f, 1f);
        barRT.anchoredPosition = new Vector2(0f, -190f);   // Title sits at -80..-180
        barRT.sizeDelta        = new Vector2(-80f, 90f);   // 40px horizontal padding

        var hlg = barGO.GetComponent<HorizontalLayoutGroup>();
        hlg.spacing                = 12f;
        hlg.childAlignment         = TextAnchor.MiddleCenter;
        hlg.childForceExpandWidth  = true;
        hlg.childForceExpandHeight = true;
        hlg.childControlWidth      = true;
        hlg.childControlHeight     = true;

        // ── 2) Create each filter button by cloning Btn_Back's style ─────────
        var controller = panel.GetComponent("OEMS.UI.ListViewController");
        var so = controller != null ? new SerializedObject((Object)controller) : null;

        for (int i = 0; i < Buttons.GetLength(0); i++)
        {
            string goName = Buttons[i, 0];
            string label  = Buttons[i, 1];
            string field  = Buttons[i, 2];
            string hex    = Buttons[i, 3];

            var clone = Object.Instantiate(backT.gameObject, barGO.transform);
            clone.name = goName;

            // Reset RectTransform — HorizontalLayoutGroup drives size/position.
            var cRT = clone.GetComponent<RectTransform>();
            cRT.anchorMin        = new Vector2(0f, 0f);
            cRT.anchorMax        = new Vector2(1f, 1f);
            cRT.pivot            = new Vector2(0.5f, 0.5f);
            cRT.anchoredPosition = Vector2.zero;
            cRT.sizeDelta        = Vector2.zero;

            var le = clone.GetComponent<LayoutElement>();
            if (le == null) le = clone.AddComponent<LayoutElement>();
            le.flexibleWidth  = 1f;
            le.minHeight      = 70f;
            le.preferredHeight = 80f;

            // Tint the button image.
            var img = clone.GetComponent<Image>();
            if (img != null)
            {
                Color c;
                if (ColorUtility.TryParseHtmlString(hex, out c)) img.color = c;
            }

            // Set the label + a readable font size; text is white for contrast.
            var txt = clone.GetComponentInChildren<TextMeshProUGUI>();
            if (txt != null)
            {
                txt.text     = label;
                txt.fontSize = 28f;
                txt.color    = Color.white;
                txt.alignment = TextAlignmentOptions.Center;
                txt.enableAutoSizing = false;
                txt.textWrappingMode = TextWrappingModes.NoWrap;
            }

            // Clear any inherited onClick from the template (re-wired at runtime).
            var btn = clone.GetComponent<Button>();
            if (btn != null)
            {
                var pBtn = new SerializedObject(btn);
                var calls = pBtn.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
                if (calls != null) { calls.ClearArray(); pBtn.ApplyModifiedProperties(); }
            }

            // Wire the controller field reference to this button.
            if (so != null && btn != null)
            {
                var prop = so.FindProperty(field);
                if (prop != null) prop.objectReferenceValue = btn;
            }
        }

        if (so != null) so.ApplyModifiedProperties();

        // ── 3) Make room: push the Scroll's top edge below the filter bar ────
        var scrollT = panel.transform.Find("Scroll");
        if (scrollT != null)
        {
            var sRT = scrollT.GetComponent<RectTransform>();
            // Keep ~280 bottom inset (for Btn_Back), set ~295 top inset (Title + bar).
            float topInset = 295f, bottomInset = 280f;
            sRT.anchorMin = new Vector2(0f, 0f);
            sRT.anchorMax = new Vector2(1f, 1f);
            sRT.pivot     = new Vector2(0.5f, 0.5f);
            sRT.sizeDelta        = new Vector2(-80f, -(topInset + bottomInset));
            sRT.anchoredPosition = new Vector2(0f, (bottomInset - topInset) / 2f);
        }

        EditorSceneManager.MarkSceneDirty(panel.scene);
        return "FilterBar built with 5 buttons; Scroll resized; controller refs wired.";
    }
}
