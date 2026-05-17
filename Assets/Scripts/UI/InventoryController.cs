using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OEMS.Core;
using OEMS.Models;

namespace OEMS.UI
{
    /// <summary>
    /// INVENTORY MANAGEMENT MODULE.
    ///
    /// Indispensable (returnable) items — e.g. laptops:
    ///   Entering qty=3 creates THREE separate IndispensableItem records,
    ///   each with its own ASSET-XXXX id. Every unit is individually trackable.
    ///
    /// Dispensable (consumable) items — e.g. pens:
    ///   Creates ONE DispensableItem batch record (BATCH-XXXX) with the given qty.
    ///   Each new stock-entry of the same item still gets a fresh BATCH id.
    ///
    /// Also provides Delete Item (with active-assignment guard).
    /// </summary>
    public class InventoryController : MonoBehaviour
    {
        [Header("Add Item Form")]
        public TMP_InputField   itemNameInput;
        public TMP_InputField   quantityInput;
        public TMP_InputField   descriptionInput;
        public TMP_InputField   serialPrefixInput;   // optional: serial / batch ref
        public TMP_Dropdown     categoryDropdown;

        [Header("Display")]
        public TextMeshProUGUI  previewText;         // shows what will be created

        [Header("Buttons")]
        public Button submitButton;
        public Button backButton;

        // ── Delete section ────────────────────────────────────────────────────
        [Header("Delete Item")]
        public TMP_InputField   deleteItemIdInput;
        public Button           deleteButton;

        // internal state
        private List<InventoryCategory> currentCategories = new List<InventoryCategory>();

        // ═════════════════════════════════════════════════════════════════════
        private void OnEnable()
        {
            HookUpButtons();
            RefreshCategoryDropdown();
            ResetForm();
        }

        private void HookUpButtons()
        {
            if (submitButton)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(SubmitItem);
            }
            if (backButton)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(() => UIManager.Instance.ShowDashboard());
            }
            if (deleteButton)
            {
                deleteButton.onClick.RemoveAllListeners();
                deleteButton.onClick.AddListener(DeleteItem);
            }
            if (categoryDropdown)
            {
                categoryDropdown.onValueChanged.RemoveAllListeners();
                categoryDropdown.onValueChanged.AddListener(_ => UpdatePreview());
            }
            if (quantityInput)
            {
                quantityInput.onValueChanged.RemoveAllListeners();
                quantityInput.onValueChanged.AddListener(_ => UpdatePreview());
            }
        }

        public void RefreshCategoryDropdown()
        {
            var dm = DataManager.Instance;
            if (dm == null || categoryDropdown == null) return;

            currentCategories = new List<InventoryCategory>(dm.Categories);
            categoryDropdown.ClearOptions();

            if (currentCategories.Count == 0)
            {
                categoryDropdown.AddOptions(new List<string> { "-- No categories --" });
                return;
            }

            var opts = new List<string>();
            foreach (var c in currentCategories)
                opts.Add(c.categoryName + (c.isReturnable ? " (Indispensable)" : " (Dispensable)"));
            categoryDropdown.AddOptions(opts);
            UpdatePreview();
        }

        private void ResetForm()
        {
            if (itemNameInput)    itemNameInput.text    = "";
            if (quantityInput)    quantityInput.text    = "1";
            if (descriptionInput) descriptionInput.text = "";
            if (serialPrefixInput)serialPrefixInput.text= "";
            UpdatePreview();
        }

        // Live preview of what will be created
        private void UpdatePreview()
        {
            if (previewText == null) return;

            int idx = categoryDropdown ? categoryDropdown.value : -1;
            if (idx < 0 || idx >= currentCategories.Count)
            {
                previewText.text = "";
                return;
            }

            var cat = currentCategories[idx];
            int qty;
            int.TryParse(quantityInput ? quantityInput.text : "1", out qty);
            if (qty <= 0) qty = 1;

            if (cat.isReturnable)
                previewText.text = string.Format(
                    "<color=#2563eb>Will create {0} individual ASSET unit{1} " +
                    "(each gets its own ASSET-XXXX id)</color>",
                    qty, qty > 1 ? "s" : "");
            else
                previewText.text = string.Format(
                    "<color=#6b7280>Will create 1 batch record (BATCH-XXXX) " +
                    "with {0} unit{1}</color>",
                    qty, qty > 1 ? "s" : "");
        }

        // ─────────────────────────────────────────────────────────────────────
        // SUBMIT
        // ─────────────────────────────────────────────────────────────────────
        public void SubmitItem()
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            string name   = itemNameInput    ? itemNameInput.text.Trim()    : "";
            string qtyStr = quantityInput    ? quantityInput.text.Trim()    : "";
            string desc   = descriptionInput ? descriptionInput.text.Trim() : "";
            string serial = serialPrefixInput? serialPrefixInput.text.Trim(): "";

            if (string.IsNullOrEmpty(name))
            {
                UIManager.Instance.ShowToast("Item name is required.");
                return;
            }

            int qty;
            if (!int.TryParse(qtyStr, out qty) || qty <= 0)
            {
                UIManager.Instance.ShowToast("Enter a valid quantity (> 0).");
                return;
            }

            int catIdx = categoryDropdown ? categoryDropdown.value : -1;
            if (catIdx < 0 || catIdx >= currentCategories.Count)
            {
                UIManager.Instance.ShowToast("Select a category first.");
                return;
            }

            var cat = currentCategories[catIdx];

            if (cat.isReturnable)
            {
                // Indispensable — create one record per physical unit
                var units = dm.AddIndispensableUnits(name, cat.categoryName, qty, desc, serial);
                if (units != null && units.Count > 0)
                {
                    string ids = units[0].itemID +
                                 (units.Count > 1 ? " … " + units[units.Count - 1].itemID : "");
                    UIManager.Instance.ShowToast(
                        "Created " + units.Count + " unit" + (units.Count > 1 ? "s" : "") +
                        " of \"" + name + "\"\nIDs: " + ids);
                    ResetForm();
                }
                else
                {
                    UIManager.Instance.ShowToast("Failed to add units.");
                }
            }
            else
            {
                // Dispensable — create one batch record
                var batch = dm.AddDispensableBatch(name, cat.categoryName, qty, desc, serial);
                if (batch != null)
                {
                    UIManager.Instance.ShowToast(
                        "Batch \"" + name + "\" added → " + batch.itemID +
                        " (" + qty + " units)");
                    ResetForm();
                }
                else
                {
                    UIManager.Instance.ShowToast("Failed to add batch.");
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // DELETE ITEM
        // ─────────────────────────────────────────────────────────────────────
        public void DeleteItem()
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            string id = deleteItemIdInput ? deleteItemIdInput.text.Trim() : "";
            if (string.IsNullOrEmpty(id))
            {
                UIManager.Instance.ShowToast("Enter an Item ID to delete.");
                return;
            }

            var item = dm.FindItem(id);
            if (item == null)
            {
                UIManager.Instance.ShowToast("Item " + id + " not found.");
                return;
            }

            UIManager.Instance.ShowConfirm(
                "Delete \"" + item.itemName + "\" (" + id + ")?\nThis cannot be undone.",
                () => OnConfirmDelete(id, item.itemName));
        }

        private void OnConfirmDelete(string id, string name)
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            if (dm.DeleteItem(id))
            {
                UIManager.Instance.ShowToast("\"" + name + "\" (" + id + ") deleted.");
                if (deleteItemIdInput) deleteItemIdInput.text = "";
            }
            else
            {
                UIManager.Instance.ShowToast(
                    "Cannot delete \"" + name + "\" — it has an active assignment.");
            }
        }
    }
}
