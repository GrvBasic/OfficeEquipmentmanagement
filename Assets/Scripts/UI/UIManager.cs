using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace OEMS.UI
{
    /// <summary>
    /// Master UI controller — owns every screen panel and switches between them.
    /// Also provides Toast notifications and a reusable Confirm dialog.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("Screen Panels")]
        public GameObject dashboardPanel;
        public GameObject registrationPanel;
        public GameObject categoryPanel;
        public GameObject inventoryPanel;
        public GameObject assignmentPanel;
        public GameObject returnPanel;
        public GameObject viewEmployeesPanel;
        public GameObject viewInventoryPanel;       // Available Inventory
        public GameObject viewAssignmentsPanel;     // Assignment History
        public GameObject employeeItemsPanel;       // Items held by a specific employee

        [Header("Toast Notification")]
        public GameObject      toastPanel;
        public TextMeshProUGUI toastText;
        public float           toastDuration = 2.5f;

        [Header("Confirm Dialog")]
        public GameObject      confirmPanel;
        public TextMeshProUGUI confirmMessageText;
        public Button          confirmYesButton;
        public Button          confirmNoButton;

        private GameObject currentPanel;
        private Action     pendingConfirmAction;

        // ═════════════════════════════════════════════════════════════════════
        // LIFECYCLE
        // ═════════════════════════════════════════════════════════════════════
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            // Wire confirm dialog buttons once
            if (confirmYesButton)
            {
                confirmYesButton.onClick.RemoveAllListeners();
                confirmYesButton.onClick.AddListener(OnConfirmYes);
            }
            if (confirmNoButton)
            {
                confirmNoButton.onClick.RemoveAllListeners();
                confirmNoButton.onClick.AddListener(OnConfirmNo);
            }
            if (confirmPanel) confirmPanel.SetActive(false);

            HideAllPanels();
            ShowDashboard();
        }

        // ═════════════════════════════════════════════════════════════════════
        // SCREEN NAVIGATION
        // ═════════════════════════════════════════════════════════════════════
        private void HideAllPanels()
        {
            SetActive(dashboardPanel,      false);
            SetActive(registrationPanel,   false);
            SetActive(categoryPanel,       false);
            SetActive(inventoryPanel,      false);
            SetActive(assignmentPanel,     false);
            SetActive(returnPanel,         false);
            SetActive(viewEmployeesPanel,  false);
            SetActive(viewInventoryPanel,  false);
            SetActive(viewAssignmentsPanel,false);
            SetActive(employeeItemsPanel,  false);
        }

        private void ShowPanel(GameObject panel)
        {
            HideAllPanels();
            if (panel == null) return;
            panel.SetActive(true);
            currentPanel = panel;
        }

        private static void SetActive(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }

        // ── Navigation helpers ────────────────────────────────────────────────
        public void ShowDashboard()
        {
            ShowPanel(dashboardPanel);
            var d = GetComponent<DashboardController>(dashboardPanel);
            if (d != null) d.RefreshStats();
        }

        public void ShowRegistration()  { ShowPanel(registrationPanel); }

        public void ShowCategory()
        {
            ShowPanel(categoryPanel);
            var c = GetComponent<CategoryController>(categoryPanel);
            if (c != null) c.RefreshList();
        }

        public void ShowInventory()     { ShowPanel(inventoryPanel); }

        public void ShowAssignment()
        {
            ShowPanel(assignmentPanel);
            var ac = GetComponent<AssignmentController>(assignmentPanel);
            if (ac != null) ac.RefreshDropdowns();
        }

        public void ShowReturn()
        {
            ShowPanel(returnPanel);
            var rc = GetComponent<ReturnController>(returnPanel);
            if (rc != null) rc.RefreshEmployees();
        }

        public void ShowViewEmployees()
        {
            ShowPanel(viewEmployeesPanel);
            var v = GetComponent<ListViewController>(viewEmployeesPanel);
            if (v != null) v.PopulateEmployees();
        }

        public void ShowViewInventory()
        {
            ShowPanel(viewInventoryPanel);
            var v = GetComponent<ListViewController>(viewInventoryPanel);
            if (v != null) v.PopulateInventory();
        }

        public void ShowViewAssignments()
        {
            ShowPanel(viewAssignmentsPanel);
            var v = GetComponent<ListViewController>(viewAssignmentsPanel);
            if (v != null)
            {
                // Coming in from the dashboard — clear any stale per-employee filter
                // left over from a previous ShowEmployeeAssignments call.
                v.ClearEmployeeFilter();
                v.PopulateAssignments();
            }
        }

        /// <summary>Show items currently held by a specific employee.</summary>
        public void ShowEmployeeItems(string employeeID)
        {
            ShowPanel(employeeItemsPanel);
            var v = GetComponent<ListViewController>(employeeItemsPanel);
            if (v != null) v.PopulateEmployeeItems(employeeID);
        }

        /// <summary>Show every assignment ever made to a specific employee
        /// (reuses the Assignment History panel, filtered to one employee).</summary>
        public void ShowEmployeeAssignments(string employeeID)
        {
            ShowPanel(viewAssignmentsPanel);
            var v = GetComponent<ListViewController>(viewAssignmentsPanel);
            if (v != null) v.PopulateAssignmentsForEmployee(employeeID);
        }

        // ═════════════════════════════════════════════════════════════════════
        // TOAST NOTIFICATION
        // ═════════════════════════════════════════════════════════════════════
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

        // ═════════════════════════════════════════════════════════════════════
        // CONFIRM DIALOG
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Show a modal confirm dialog.
        /// onConfirm is invoked only if the user taps "Yes".
        /// </summary>
        public void ShowConfirm(string message, Action onConfirm)
        {
            if (confirmPanel == null)
            {
                // No dialog prefab wired up — just execute immediately
                onConfirm?.Invoke();
                return;
            }
            pendingConfirmAction = onConfirm;
            if (confirmMessageText) confirmMessageText.text = message;
            confirmPanel.SetActive(true);
        }

        private void OnConfirmYes()
        {
            if (confirmPanel) confirmPanel.SetActive(false);
            pendingConfirmAction?.Invoke();
            pendingConfirmAction = null;
        }

        private void OnConfirmNo()
        {
            if (confirmPanel) confirmPanel.SetActive(false);
            pendingConfirmAction = null;
        }

        // ═════════════════════════════════════════════════════════════════════
        // ANDROID BACK BUTTON
        // ═════════════════════════════════════════════════════════════════════
        private void Update()
        {
         /*   if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (confirmPanel != null && confirmPanel.activeSelf)
                {
                    OnConfirmNo();   // dismiss dialog first
                }
                else if (currentPanel != dashboardPanel)
                {
                    ShowDashboard();
                }
            }*/
        }

        // Helper to avoid verbose null checks
        private static T GetComponent<T>(GameObject go) where T : Component
        {
            return go != null ? go.GetComponent<T>() : null;
        }
    }
}
