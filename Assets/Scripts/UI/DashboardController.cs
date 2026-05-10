using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OEMS.Core;

namespace OEMS.UI
{
    /// <summary>
    /// Main menu / dashboard. Shows summary statistics and navigation buttons.
    /// </summary>
    public class DashboardController : MonoBehaviour
    {
        [Header("Stat Texts")]
        public TextMeshProUGUI employeeCountText;
        public TextMeshProUGUI itemTypesText;
        public TextMeshProUGUI availableQtyText;
        public TextMeshProUGUI assignedQtyText;
        public TextMeshProUGUI damagedQtyText;
        public TextMeshProUGUI consumedQtyText;

        [Header("Navigation Buttons")]
        public Button registerEmployeeButton;
        public Button addInventoryButton;
        public Button assignItemButton;
        public Button returnItemButton;
        public Button viewEmployeesButton;
        public Button viewInventoryButton;
        public Button viewAssignmentsButton;

        private void OnEnable()
        {
            HookUpButtons();
            RefreshStats();
        }

        private void HookUpButtons()
        {
            if (registerEmployeeButton)  { registerEmployeeButton.onClick.RemoveAllListeners(); registerEmployeeButton.onClick.AddListener(() => UIManager.Instance.ShowRegistration()); }
            if (addInventoryButton)      { addInventoryButton.onClick.RemoveAllListeners();     addInventoryButton.onClick.AddListener(() => UIManager.Instance.ShowInventory()); }
            if (assignItemButton)        { assignItemButton.onClick.RemoveAllListeners();       assignItemButton.onClick.AddListener(() => UIManager.Instance.ShowAssignment()); }
            if (returnItemButton)        { returnItemButton.onClick.RemoveAllListeners();       returnItemButton.onClick.AddListener(() => UIManager.Instance.ShowReturn()); }
            if (viewEmployeesButton)     { viewEmployeesButton.onClick.RemoveAllListeners();    viewEmployeesButton.onClick.AddListener(() => UIManager.Instance.ShowViewEmployees()); }
            if (viewInventoryButton)     { viewInventoryButton.onClick.RemoveAllListeners();    viewInventoryButton.onClick.AddListener(() => UIManager.Instance.ShowViewInventory()); }
            if (viewAssignmentsButton)   { viewAssignmentsButton.onClick.RemoveAllListeners();  viewAssignmentsButton.onClick.AddListener(() => UIManager.Instance.ShowViewAssignments()); }
        }

        public void RefreshStats()
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            if (employeeCountText) employeeCountText.text = dm.GetTotalEmployeeCount().ToString();
            if (itemTypesText)     itemTypesText.text     = dm.GetTotalItemTypes().ToString();
            if (availableQtyText)  availableQtyText.text  = dm.GetTotalAvailableQuantity().ToString();
            if (assignedQtyText)   assignedQtyText.text   = dm.GetTotalAssignedQuantity().ToString();
            if (damagedQtyText)    damagedQtyText.text    = dm.GetTotalDamagedQuantity().ToString();
            if (consumedQtyText)   consumedQtyText.text   = dm.GetTotalConsumedQuantity().ToString();
        }
    }
}
