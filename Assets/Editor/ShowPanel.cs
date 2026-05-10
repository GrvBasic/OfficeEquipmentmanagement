using UnityEngine;
using UnityEditor;

public class ShowPanel
{
    public static string TargetPanel = "DashboardPanel";

    public static void Execute()
    {
        string targetPanel = TargetPanel;

        Canvas[] canvases = GameObject.FindObjectsOfType<Canvas>(true);
        Canvas rootCanvas = null;
        foreach (var c in canvases)
        {
            if (c.isRootCanvas) { rootCanvas = c; break; }
        }
        if (rootCanvas == null) { Debug.LogError("No Canvas!"); return; }

        Transform tf = rootCanvas.transform;
        string[] allPanels = new string[]
        {
            "DashboardPanel", "RegistrationPanel", "InventoryPanel",
            "AssignmentPanel", "ReturnPanel", "ViewEmployeesPanel",
            "ViewInventoryPanel", "ViewAssignmentsPanel", "Toast"
        };

        foreach (string p in allPanels)
        {
            Transform panel = tf.Find(p);
            if (panel != null)
                panel.gameObject.SetActive(p == targetPanel);
        }

        Debug.Log($"[ShowPanel] Showing: {targetPanel}");
    }
}
