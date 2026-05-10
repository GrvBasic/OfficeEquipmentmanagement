using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OEMS.Core;
using OEMS.Models;

namespace OEMS.UI
{
    /// <summary>
    /// ASSIGNMENT MODULE controller (per synopsis section 9.3).
    /// </summary>
    public class AssignmentController : MonoBehaviour
    {
        [Header("Selection")]
        public TMP_Dropdown employeeDropdown;
        public TMP_Dropdown itemDropdown;
        public TMP_InputField quantityInput;

        [Header("Display")]
        public TextMeshProUGUI itemAvailableText;

        [Header("Buttons")]
        public Button submitButton;
        public Button backButton;

        private List<Employee> currentEmployees = new List<Employee>();
        private List<InventoryItem> currentItems = new List<InventoryItem>();

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

            // -- Employees --
            currentEmployees = new List<Employee>(dm.Employees);
            if (employeeDropdown != null)
            {
                employeeDropdown.ClearOptions();
                var opts = new List<string>();
                if (currentEmployees.Count == 0)
                    opts.Add("-- No employees registered --");
                else
                    foreach (var e in currentEmployees) opts.Add(e.ToString());
                employeeDropdown.AddOptions(opts);
            }

            // -- Items (only available ones) --
            currentItems = dm.GetAvailableItems();
            if (itemDropdown != null)
            {
                itemDropdown.ClearOptions();
                var opts = new List<string>();
                if (currentItems.Count == 0)
                    opts.Add("-- No items available --");
                else
                    foreach (var i in currentItems)
                        opts.Add(string.Format("{0} - {1} [{2}] (Avail: {3})",
                            i.itemID, i.itemName, i.GetCategory(), i.availableQuantity));
                itemDropdown.AddOptions(opts);
            }

            UpdateAvailableLabel();
            if (quantityInput) quantityInput.text = "1";
        }

        private void OnItemChanged(int idx) { UpdateAvailableLabel(); }

        private void UpdateAvailableLabel()
        {
            if (itemAvailableText == null) return;
            if (itemDropdown == null || currentItems.Count == 0)
            {
                itemAvailableText.text = "Available: 0";
                return;
            }
            int idx = itemDropdown.value;
            if (idx < 0 || idx >= currentItems.Count)
            {
                itemAvailableText.text = "Available: 0";
                return;
            }
            itemAvailableText.text = "Available: " + currentItems[idx].availableQuantity;
        }

        public void SubmitAssignment()
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            if (currentEmployees.Count == 0 || currentItems.Count == 0)
            {
                UIManager.Instance.ShowToast("Need at least one employee and one available item.");
                return;
            }

            int empIdx = employeeDropdown != null ? employeeDropdown.value : -1;
            int itmIdx = itemDropdown != null ? itemDropdown.value : -1;
            if (empIdx < 0 || empIdx >= currentEmployees.Count || itmIdx < 0 || itmIdx >= currentItems.Count)
            {
                UIManager.Instance.ShowToast("Invalid selection.");
                return;
            }

            int qty;
            if (quantityInput == null || !int.TryParse(quantityInput.text, out qty) || qty <= 0)
            {
                UIManager.Instance.ShowToast("Enter a valid quantity.");
                return;
            }

            var emp = currentEmployees[empIdx];
            var item = currentItems[itmIdx];

            if (qty > item.availableQuantity)
            {
                UIManager.Instance.ShowToast("Only " + item.availableQuantity + " available.");
                return;
            }

            var asn = dm.AssignItem(emp.employeeID, item.itemID, qty);
            if (asn != null)
            {
                UIManager.Instance.ShowToast("Assigned " + qty + "x " + item.itemName + " to " + emp.name);
                RefreshDropdowns();
            }
            else
            {
                UIManager.Instance.ShowToast("Assignment failed.");
            }
        }
    }
}
