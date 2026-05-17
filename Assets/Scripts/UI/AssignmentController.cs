using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OEMS.Core;
using OEMS.Models;

namespace OEMS.UI
{
    /// <summary>
    /// ASSIGNMENT MODULE.
    /// Indispensable items: dropdown shows individual ASSET-XXXX unit IDs
    ///   — qty is locked to 1 since each unit is one physical device.
    /// Dispensable items: dropdown shows BATCH-XXXX batches + available qty
    ///   — admin enters how many units to issue from that batch.
    /// </summary>
    public class AssignmentController : MonoBehaviour
    {
        [Header("Selection")]
        public TMP_Dropdown     employeeDropdown;
        public TMP_Dropdown     itemDropdown;
        public TMP_InputField   quantityInput;
        public TextMeshProUGUI  itemInfoText;       // shows unit ID or batch remaining qty

        [Header("Buttons")]
        public Button submitButton;
        public Button backButton;

        private List<Employee>      currentEmployees = new List<Employee>();
        private List<InventoryItem> currentItems     = new List<InventoryItem>();

        // ═════════════════════════════════════════════════════════════════════
        private void OnEnable()
        {
            HookUpButtons();
            RefreshDropdowns();
        }

        private void HookUpButtons()
        {
            if (submitButton)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(SubmitAssignment);
            }
            if (backButton)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(() => UIManager.Instance.ShowDashboard());
            }
            if (itemDropdown)
            {
                itemDropdown.onValueChanged.RemoveAllListeners();
                itemDropdown.onValueChanged.AddListener(OnItemChanged);
            }
        }

        public void RefreshDropdowns()
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            // ── Employees ────────────────────────────────────────────────────
            currentEmployees = new List<Employee>(dm.Employees);
            if (employeeDropdown != null)
            {
                employeeDropdown.ClearOptions();
                var opts = new List<string>();
                if (currentEmployees.Count == 0)
                    opts.Add("-- No employees registered --");
                else
                    foreach (var e in currentEmployees)
                        opts.Add(e.employeeID + "  " + e.FullName + " (" + e.department + ")");
                employeeDropdown.AddOptions(opts);
            }

            // ── Available items ───────────────────────────────────────────────
            currentItems = dm.GetAvailableItems();
            if (itemDropdown != null)
            {
                itemDropdown.ClearOptions();
                var opts = new List<string>();
                if (currentItems.Count == 0)
                {
                    opts.Add("-- No items available --");
                }
                else
                {
                    foreach (var i in currentItems)
                    {
                        if (i is IndispensableItem)
                            opts.Add(string.Format("{0}  {1}  [{2}]  — unit",
                                i.itemID, i.itemName, i.categoryName));
                        else
                            opts.Add(string.Format("{0}  {1}  [{2}]  — batch  avail:{3}",
                                i.itemID, i.itemName, i.categoryName, i.availableQuantity));
                    }
                }
                itemDropdown.AddOptions(opts);
            }

            UpdateItemInfo();
            if (quantityInput) quantityInput.text = "1";
        }

        private void OnItemChanged(int idx)
        {
            UpdateItemInfo();
            // Lock quantity to 1 for indispensable units
            if (idx >= 0 && idx < currentItems.Count)
            {
                bool isUnit = currentItems[idx] is IndispensableItem;
                if (quantityInput)
                {
                    quantityInput.text         = "1";
                    quantityInput.interactable  = !isUnit;
                }
            }
        }

        private void UpdateItemInfo()
        {
            if (itemInfoText == null) return;
            int idx = itemDropdown ? itemDropdown.value : -1;
            if (idx < 0 || idx >= currentItems.Count) { itemInfoText.text = ""; return; }

            var item = currentItems[idx];
            if (item is IndispensableItem)
                itemInfoText.text = string.Format(
                    "<color=#2563eb>Unit ID: {0}  |  {1}  |  Status: {2}</color>",
                    item.itemID, item.itemName, item.itemStatus);
            else
                itemInfoText.text = string.Format(
                    "<color=#6b7280>Batch: {0}  |  Available: {1} units</color>",
                    item.itemID, item.availableQuantity);
        }

        // ─────────────────────────────────────────────────────────────────────
        public void SubmitAssignment()
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            if (currentEmployees.Count == 0 || currentItems.Count == 0)
            {
                UIManager.Instance.ShowToast("Need at least one employee and one available item.");
                return;
            }

            int empIdx = employeeDropdown ? employeeDropdown.value : -1;
            int itmIdx = itemDropdown     ? itemDropdown.value     : -1;
            if (empIdx < 0 || empIdx >= currentEmployees.Count ||
                itmIdx < 0 || itmIdx >= currentItems.Count)
            {
                UIManager.Instance.ShowToast("Invalid selection.");
                return;
            }

            var emp  = currentEmployees[empIdx];
            var item = currentItems[itmIdx];

            int qty = 1;
            if (item is DispensableItem)
            {
                if (quantityInput == null || !int.TryParse(quantityInput.text, out qty) || qty <= 0)
                {
                    UIManager.Instance.ShowToast("Enter a valid quantity.");
                    return;
                }
                if (qty > item.availableQuantity)
                {
                    UIManager.Instance.ShowToast(
                        "Only " + item.availableQuantity + " units available in this batch.");
                    return;
                }
            }

            var asn = dm.AssignItem(emp.employeeID, item.itemID, qty);
            if (asn != null)
            {
                string msg = item is IndispensableItem
                    ? string.Format("Unit {0} ({1}) assigned to {2}  [{3}]",
                        item.itemID, item.itemName, emp.FullName, asn.assignmentID)
                    : string.Format("{0}x {1} issued to {2}  [{3}]",
                        qty, item.itemName, emp.FullName, asn.assignmentID);
                UIManager.Instance.ShowToast(msg);
                RefreshDropdowns();
            }
            else
            {
                UIManager.Instance.ShowToast("Assignment failed. Check availability.");
            }
        }
    }
}
