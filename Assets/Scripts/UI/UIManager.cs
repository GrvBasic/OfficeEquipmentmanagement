using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OEMS.UI
{
    /// <summary>
    /// Master UI controller. Owns every screen panel and switches between them.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Screen Panels")]
        public GameObject dashboardPanel;
        public GameObject registrationPanel;
        public GameObject inventoryPanel;
        public GameObject assignmentPanel;
        public GameObject returnPanel;
        public GameObject viewEmployeesPanel;
        public GameObject viewInventoryPanel;
        public GameObject viewAssignmentsPanel;

        [Header("Toast / Notification")]
        public GameObject toastPanel;
        public TextMeshProUGUI toastText;
        public float toastDuration = 2f;

        private GameObject currentPanel;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            HideAllPanels();
            ShowDashboard();
        }

        // ============================================================
        // SCREEN NAVIGATION
        // ============================================================
        private void HideAllPanels()
        {
            if (dashboardPanel)         dashboardPanel.SetActive(false);
            if (registrationPanel)      registrationPanel.SetActive(false);
            if (inventoryPanel)         inventoryPanel.SetActive(false);
            if (assignmentPanel)        assignmentPanel.SetActive(false);
            if (returnPanel)            returnPanel.SetActive(false);
            if (viewEmployeesPanel)     viewEmployeesPanel.SetActive(false);
            if (viewInventoryPanel)     viewInventoryPanel.SetActive(false);
            if (viewAssignmentsPanel)   viewAssignmentsPanel.SetActive(false);
        }

        private void ShowPanel(GameObject panel)
        {
            HideAllPanels();
            if (panel != null)
            {
                panel.SetActive(true);
                currentPanel = panel;
            }
        }

        public void ShowDashboard()
        {
            ShowPanel(dashboardPanel);
            var dash = dashboardPanel != null ? dashboardPanel.GetComponent<DashboardController>() : null;
            if (dash != null) dash.RefreshStats();
        }

        public void ShowRegistration()    { ShowPanel(registrationPanel); }
        public void ShowInventory()       { ShowPanel(inventoryPanel); }
        public void ShowAssignment()
        {
            ShowPanel(assignmentPanel);
            var ac = assignmentPanel != null ? assignmentPanel.GetComponent<AssignmentController>() : null;
            if (ac != null) ac.RefreshDropdowns();
        }
        public void ShowReturn()
        {
            ShowPanel(returnPanel);
            var rc = returnPanel != null ? returnPanel.GetComponent<ReturnController>() : null;
            if (rc != null) rc.RefreshList();
        }
        public void ShowViewEmployees()
        {
            ShowPanel(viewEmployeesPanel);
            var v = viewEmployeesPanel != null ? viewEmployeesPanel.GetComponent<ListViewController>() : null;
            if (v != null) v.PopulateEmployees();
        }
        public void ShowViewInventory()
        {
            ShowPanel(viewInventoryPanel);
            var v = viewInventoryPanel != null ? viewInventoryPanel.GetComponent<ListViewController>() : null;
            if (v != null) v.PopulateInventory();
        }
        public void ShowViewAssignments()
        {
            ShowPanel(viewAssignmentsPanel);
            var v = viewAssignmentsPanel != null ? viewAssignmentsPanel.GetComponent<ListViewController>() : null;
            if (v != null) v.PopulateAssignments();
        }

        // ============================================================
        // TOAST NOTIFICATIONS
        // ============================================================
        public void ShowToast(string message)
        {
            if (toastPanel == null || toastText == null)
            {
                Debug.Log("[Toast] " + message);
                return;
            }
            StopAllCoroutines();
            StartCoroutine(ToastRoutine(message));
        }

        private System.Collections.IEnumerator ToastRoutine(string message)
        {
            toastText.text = message;
            toastPanel.SetActive(true);
            yield return new WaitForSeconds(toastDuration);
            toastPanel.SetActive(false);
        }

        // ============================================================
        // ANDROID BACK BUTTON
        // ============================================================
        private void Update()
        {
           /* if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (currentPanel != dashboardPanel)
                    ShowDashboard();
            }*/
        }
    }
}
