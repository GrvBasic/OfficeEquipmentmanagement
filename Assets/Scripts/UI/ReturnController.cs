using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OEMS.Core;
using OEMS.Models;

namespace OEMS.UI
{
    /// <summary>
    /// RETURN MODULE.
    /// New two-step selection design:
    ///   1) Employee dropdown   — lists employees who currently hold one or
    ///                            more indispensable (returnable) items.
    ///   2) Item dropdown       — lists the indispensable items currently
    ///                            assigned to the selected employee.
    /// Admin picks one item, chooses its condition (Good / Damaged / Consumed),
    /// adds optional remarks, then confirms the return.
    ///
    /// Note: dispensable items are consumed on assign and are NOT returnable,
    /// so they are intentionally excluded from this flow.
    /// </summary>
    public class ReturnController : MonoBehaviour
    {
        [Header("Selection")]
        public TMP_Dropdown     employeeDropdown;
        public TMP_Dropdown     itemDropdown;
        public TextMeshProUGUI  assignmentDetailText;   // shows selected assignment details

        [Header("Condition")]
        public Toggle           goodToggle;
        public Toggle           damagedToggle;
        public Toggle           consumedToggle;

        [Header("Remarks")]
        public TMP_InputField   remarksInput;

        [Header("Buttons")]
        public Button submitButton;
        public Button backButton;

        // Cached selection state
        private List<Employee>   employeesWithReturns = new List<Employee>();
        private List<Assignment> currentEmployeeItems = new List<Assignment>();

        // ═════════════════════════════════════════════════════════════════════
        private void OnEnable()
        {
            HookUpButtons();
            RefreshEmployees();
        }

        private void HookUpButtons()
        {
            if (submitButton)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(SubmitReturn);
            }
            if (backButton)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(() => UIManager.Instance.ShowDashboard());
            }
            if (employeeDropdown)
            {
                employeeDropdown.onValueChanged.RemoveAllListeners();
                employeeDropdown.onValueChanged.AddListener(OnEmployeeChanged);
            }
            if (itemDropdown)
            {
                itemDropdown.onValueChanged.RemoveAllListeners();
                itemDropdown.onValueChanged.AddListener(OnItemChanged);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        /// <summary>Populate the employee dropdown with employees that hold at
        /// least one active indispensable assignment.</summary>
        public void RefreshEmployees()
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            // Only employees with at least one active *returnable* assignment.
            employeesWithReturns = new List<Employee>();
            foreach (var emp in dm.Employees)
            {
                var actives = dm.GetActiveAssignmentsForEmployee(emp.employeeID);
                bool hasReturnable = actives.Exists(a => a.isReturnable);
                if (hasReturnable) employeesWithReturns.Add(emp);
            }

            if (employeeDropdown != null)
            {
                employeeDropdown.ClearOptions();
                var opts = new List<string>();
                if (employeesWithReturns.Count == 0)
                {
                    opts.Add("-- No employees with returnable items --");
                }
                else
                {
                    foreach (var e in employeesWithReturns)
                        opts.Add(e.employeeID + "  -  " + e.FullName);
                }
                employeeDropdown.AddOptions(opts);
                employeeDropdown.value = 0;
                employeeDropdown.RefreshShownValue();
            }

            // Reset condition + remarks
            if (goodToggle)   goodToggle.isOn = true;
            if (remarksInput) remarksInput.text = "";

            RefreshItemsForCurrentEmployee();
        }

        /// <summary>Populate the items dropdown with the indispensable assignments
        /// for the currently-selected employee.</summary>
        private void RefreshItemsForCurrentEmployee()
        {
            currentEmployeeItems.Clear();

            var dm = DataManager.Instance;
            if (dm != null && employeesWithReturns.Count > 0 && employeeDropdown != null)
            {
                int idx = employeeDropdown.value;
                if (idx >= 0 && idx < employeesWithReturns.Count)
                {
                    var emp = employeesWithReturns[idx];
                    var actives = dm.GetActiveAssignmentsForEmployee(emp.employeeID);
                    // Only indispensable (returnable) items belong in the return flow.
                    foreach (var a in actives)
                        if (a.isReturnable) currentEmployeeItems.Add(a);
                }
            }

            if (itemDropdown != null)
            {
                itemDropdown.ClearOptions();
                var opts = new List<string>();
                if (currentEmployeeItems.Count == 0)
                {
                    opts.Add("-- No indispensable items assigned --");
                }
                else
                {
                    foreach (var a in currentEmployeeItems)
                        opts.Add(string.Format("{0}  -  {1}  [{2}]",
                            a.itemID, a.itemName, a.assignmentID));
                }
                itemDropdown.AddOptions(opts);
                itemDropdown.value = 0;
                itemDropdown.RefreshShownValue();
            }

            UpdateDetailText();
        }

        private void OnEmployeeChanged(int _)
        {
            RefreshItemsForCurrentEmployee();
        }

        private void OnItemChanged(int _)
        {
            UpdateDetailText();
        }

        private void UpdateDetailText()
        {
            if (assignmentDetailText == null) return;
            if (currentEmployeeItems.Count == 0)
            {
                assignmentDetailText.text = "<i>Select an employee with active indispensable items.</i>";
                return;
            }

            int idx = itemDropdown ? itemDropdown.value : -1;
            if (idx < 0 || idx >= currentEmployeeItems.Count) { assignmentDetailText.text = ""; return; }

            var a = currentEmployeeItems[idx];
            assignmentDetailText.text = string.Format(
                "<b>{0}</b>\n" +
                "Item: {1} ({2})\n" +
                "Employee: {3} ({4})\n" +
                "Qty: {5}   Issued: {6}\n" +
                "Type: Indispensable (returnable)",
                a.assignmentID,
                a.itemName, a.itemID,
                a.employeeFullName, a.employeeID,
                a.quantity, a.assignedDate);
        }

        // ─────────────────────────────────────────────────────────────────────
        public void SubmitReturn()
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            if (employeesWithReturns.Count == 0)
            {
                UIManager.Instance.ShowToast("No employees have returnable items.");
                return;
            }
            if (currentEmployeeItems.Count == 0)
            {
                UIManager.Instance.ShowToast("Selected employee has no items to return.");
                return;
            }

            int itemIdx = itemDropdown ? itemDropdown.value : -1;
            if (itemIdx < 0 || itemIdx >= currentEmployeeItems.Count)
            {
                UIManager.Instance.ShowToast("Invalid item selection.");
                return;
            }

            // Determine condition from toggles
            ItemCondition condition = ItemCondition.Good;
            if (damagedToggle  && damagedToggle.isOn)  condition = ItemCondition.Damaged;
            else if (consumedToggle && consumedToggle.isOn) condition = ItemCondition.Consumed;

            var a       = currentEmployeeItems[itemIdx];
            string rmks = remarksInput ? remarksInput.text : "";

            // Show confirm dialog before committing
            string condLabel = condition.ToString();
            UIManager.Instance.ShowConfirm(
                string.Format("Return {0} ({1})\nCondition: {2}\nEmployee: {3}",
                    a.itemName, a.itemID, condLabel, a.employeeFullName),
                () => OnConfirmReturn(a.assignmentID, condition, rmks, a.itemName, condLabel));
        }

        private void OnConfirmReturn(string assignmentID, ItemCondition condition,
                                     string remarks, string itemName, string condLabel)
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            if (dm.ReturnItem(assignmentID, condition, remarks))
            {
                UIManager.Instance.ShowToast(
                    "\"" + itemName + "\" returned - " + condLabel + ".");
                RefreshEmployees();
            }
            else
            {
                UIManager.Instance.ShowToast("Return failed. Assignment may already be closed.");
            }
        }
    }
}
