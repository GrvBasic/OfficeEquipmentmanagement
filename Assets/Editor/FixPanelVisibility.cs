using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class FixPanelVisibility
{
    public static void Execute()
    {
        // Find the root Canvas
        Canvas[] canvases = GameObject.FindObjectsOfType<Canvas>(true);
        Canvas rootCanvas = null;
        foreach (var c in canvases)
        {
            if (c.isRootCanvas)
            {
                rootCanvas = c;
                break;
            }
        }

        if (rootCanvas == null)
        {
            Debug.LogError("No root Canvas found in scene!");
            return;
        }

        Transform canvasTf = rootCanvas.transform;

        // Panels that should be INACTIVE by default
        string[] inactivePanels = new string[]
        {
            "RegistrationPanel",
            "InventoryPanel",
            "AssignmentPanel",
            "ReturnPanel",
            "ViewEmployeesPanel",
            "ViewInventoryPanel",
            "ViewAssignmentsPanel",
            "Toast"
        };

        // Panels/objects that should be ACTIVE by default
        string[] activePanels = new string[]
        {
            "DashboardPanel",
            "Header",
            "Background"
        };

        foreach (string panelName in inactivePanels)
        {
            Transform panel = canvasTf.Find(panelName);
            if (panel != null)
            {
                panel.gameObject.SetActive(false);
                Debug.Log($"[FixUI] Deactivated: {panelName}");
            }
            else
            {
                Debug.LogWarning($"[FixUI] Panel not found: {panelName}");
            }
        }

        foreach (string panelName in activePanels)
        {
            Transform panel = canvasTf.Find(panelName);
            if (panel != null)
            {
                panel.gameObject.SetActive(true);
                Debug.Log($"[FixUI] Activated: {panelName}");
            }
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[FixUI] Done! Only DashboardPanel is now active.");
    }
}
