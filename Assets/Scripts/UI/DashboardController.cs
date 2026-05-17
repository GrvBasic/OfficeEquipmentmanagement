using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OEMS.Core;

namespace OEMS.UI
{
    /// <summary>
    /// Dashboard — at-a-glance statistics and navigation to all modules.
    /// Stats updated every time the panel becomes visible.
    /// </summary>
    public class DashboardController : MonoBehaviour
    {
        [Header("Stat Tiles")]
        public TextMeshProUGUI employeeCountText;
        public TextMeshProUGUI categoryCountText;
        public TextMeshProUGUI itemCountText;           // total items/units in system
        public TextMeshProUGUI availableQtyText;
        public TextMeshProUGUI assignedQtyText;
        public TextMeshProUGUI activeAssignmentsText;
        public TextMeshProUGUI damagedQtyText;
        public TextMeshProUGUI consumedQtyText;

        [Header("Navigation Buttons")]
        public Button registerEmployeeButton;
        public Button manageCategoriesButton;   // NEW
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
            Wire(registerEmployeeButton,  () => UIManager.Instance.ShowRegistration());
            Wire(manageCategoriesButton,  () => UIManager.Instance.ShowCategory());
            Wire(addInventoryButton,      () => UIManager.Instance.ShowInventory());
            Wire(assignItemButton,        () => UIManager.Instance.ShowAssignment());
            Wire(returnItemButton,        () => UIManager.Instance.ShowReturn());
            Wire(viewEmployeesButton,     () => UIManager.Instance.ShowViewEmployees());
            Wire(viewInventoryButton,     () => UIManager.Instance.ShowViewInventory());
            Wire(viewAssignmentsButton,   () => UIManager.Instance.ShowViewAssignments());
        }

        private static void Wire(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn == null) return;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
        }

        public void RefreshStats()
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            if (employeeCountText)     employeeCountText.text     = dm.GetTotalEmployeeCount().ToString();
            if (categoryCountText)     categoryCountText.text     = dm.GetTotalCategoryCount().ToString();
            if (itemCountText)         itemCountText.text         = dm.GetTotalItemTypeCount().ToString();
            if (availableQtyText)      availableQtyText.text      = dm.GetTotalAvailableQuantity().ToString();
            if (assignedQtyText)       assignedQtyText.text       = dm.GetTotalAssignedCount().ToString();
            if (activeAssignmentsText) activeAssignmentsText.text = dm.GetActiveAssignmentCount().ToString();
            if (damagedQtyText)        damagedQtyText.text        = dm.GetTotalDamagedCount().ToString();
            if (consumedQtyText)       consumedQtyText.text       = dm.GetTotalConsumedCount().ToString();
        }
    }
}
