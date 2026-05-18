using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class WrapInventoryFormInScroll
{
    /// <summary>Default entry point — wraps InventoryPanel/Form.</summary>
    public static string Execute()
    {
        return WrapPanel("InventoryPanel");
    }

    /// <summary>Entry point for RegistrationPanel/Form.</summary>
    public static string ExecuteRegistration()
    {
        return WrapPanel("RegistrationPanel");
    }

    /// <summary>
    /// Wrap Canvas/{panelName}/Form inside a ScrollRect so it is scrollable
    /// on screens that aren't tall enough to fit every field. Idempotent —
    /// if the wrapper already exists this is a no-op.
    /// </summary>
    public static string WrapPanel(string panelName)
    {
        // Find panel via Canvas root (works even when the panel is inactive).
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return "Canvas not found";
        var panelT = canvas.transform.Find(panelName);
        if (panelT == null) return panelName + " not found";
        var panel = panelT.gameObject;

        // Already wrapped?
        if (panel.transform.Find("ScrollView/Viewport/Form") != null)
            return "Already wrapped — nothing to do.";

        var formT = panel.transform.Find("Form");
        if (formT == null) return "Form not found under " + panelName;
        var form = formT.gameObject;

        // ── 1) Create the ScrollView root ─────────────────────────────────────
        var scrollGO = new GameObject("ScrollView",
            typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        scrollGO.transform.SetParent(panel.transform, false);

        var sRT = scrollGO.GetComponent<RectTransform>();
        sRT.anchorMin        = new Vector2(0f, 0f);
        sRT.anchorMax        = new Vector2(1f, 1f);
        sRT.pivot            = new Vector2(0.5f, 0.5f);
        sRT.anchoredPosition = Vector2.zero;
        // Leave ~180px at top for Title and ~180px at bottom for the action buttons.
        // Horizontal padding 40 each side (matches the Form's previous width inset).
        sRT.sizeDelta = new Vector2(-80f, -360f);

        // Make the background transparent — we just need the rect for clipping/layout.
        var sBg = scrollGO.GetComponent<Image>();
        sBg.color = new Color(1f, 1f, 1f, 0f);
        sBg.raycastTarget = true; // so users can scroll by dragging anywhere

        // ── 2) Create Viewport (with Mask) ────────────────────────────────────
        var viewGO = new GameObject("Viewport",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
        viewGO.transform.SetParent(scrollGO.transform, false);

        var vRT = viewGO.GetComponent<RectTransform>();
        vRT.anchorMin        = new Vector2(0f, 0f);
        vRT.anchorMax        = new Vector2(1f, 1f);
        vRT.pivot            = new Vector2(0f, 1f);
        vRT.anchoredPosition = Vector2.zero;
        vRT.sizeDelta        = Vector2.zero;

        var vImg = viewGO.GetComponent<Image>();
        vImg.color = new Color(1f, 1f, 1f, 1f);

        var vMask = viewGO.GetComponent<Mask>();
        vMask.showMaskGraphic = false;

        // ── 3) Reparent Form into Viewport as the scrollable content ─────────
        Undo.SetTransformParent(form.transform, viewGO.transform, "Reparent Form under Viewport");
        form.transform.SetParent(viewGO.transform, false);
        form.transform.SetAsFirstSibling();

        var fRT = form.GetComponent<RectTransform>();
        fRT.anchorMin        = new Vector2(0f, 1f);
        fRT.anchorMax        = new Vector2(1f, 1f);
        fRT.pivot            = new Vector2(0.5f, 1f);
        fRT.anchoredPosition = Vector2.zero;
        // Width managed by stretch + sizeDelta.x=0; height managed by
        // ContentSizeFitter+VerticalLayoutGroup already on Form.
        fRT.sizeDelta = new Vector2(0f, fRT.sizeDelta.y);

        // ── 4) Wire up the ScrollRect ────────────────────────────────────────
        var scroll = scrollGO.GetComponent<ScrollRect>();
        scroll.content              = fRT;
        scroll.viewport             = vRT;
        scroll.horizontal           = false;
        scroll.vertical             = true;
        scroll.movementType         = ScrollRect.MovementType.Clamped;
        scroll.inertia              = true;
        scroll.decelerationRate     = 0.135f;
        scroll.scrollSensitivity    = 30f;
        scroll.horizontalScrollbar  = null;
        scroll.verticalScrollbar    = null;

        // Sit just below Title so action buttons remain visually on top.
        var titleT = panel.transform.Find("Title");
        if (titleT != null)
            scrollGO.transform.SetSiblingIndex(titleT.GetSiblingIndex() + 1);

        EditorSceneManager.MarkSceneDirty(panel.scene);

        return "Wrapped " + panelName + "/Form inside ScrollView/Viewport. Form is now scrollable.";
    }
}
