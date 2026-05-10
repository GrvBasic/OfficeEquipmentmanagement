using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OEMS.Core;
using OEMS.Models;

namespace OEMS.UI
{
    /// <summary>
    /// INVENTORY MANAGEMENT MODULE controller (per synopsis section 9.2).
    /// </summary>
    public class InventoryController : MonoBehaviour
    {
        [Header("Input Fields")]
        public TMP_InputField itemNameInput;
        public TMP_InputField quantityInput;
        public TMP_InputField descriptionInput;

        [Header("Category Selection")]
        public Toggle dispensableToggle;
        public Toggle indispensableToggle;

        [Header("Display")]
        public TextMeshProUGUI generatedIdText;

        [Header("Buttons")]
        public Button submitButton;
        public Button backButton;

        private string pendingID;

        private void OnEnable()
        {
            HookUpButtons();
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
        }

        private void ResetForm()
        {
            if (itemNameInput)    itemNameInput.text = "";
            if (quantityInput)    quantityInput.text = "";
            if (descriptionInput) descriptionInput.text = "";
            if (indispensableToggle) indispensableToggle.isOn = true;

            if (DataManager.Instance != null)
            {
                pendingID = DataManager.Instance.GenerateItemID();
                if (generatedIdText) generatedIdText.text = "Item ID: " + pendingID;
            }
        }

        public void SubmitItem()
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            string name = itemNameInput ? itemNameInput.text.Trim() : "";
            string qtyStr = quantityInput ? quantityInput.text.Trim() : "";
            string desc = descriptionInput ? descriptionInput.text.Trim() : "";

            int qty;
            if (string.IsNullOrEmpty(name) || !int.TryParse(qtyStr, out qty) || qty <= 0)
            {
                UIManager.Instance.ShowToast("Enter valid name and quantity.");
                return;
            }

            InventoryItem item;
            if (dispensableToggle != null && dispensableToggle.isOn)
                item = new DispensableItem(pendingID, name, qty, desc);
            else
                item = new IndispensableItem(pendingID, name, qty, desc);

            if (dm.AddInventoryItem(item))
            {
                UIManager.Instance.ShowToast("Added " + qty + "x " + name + " (" + item.GetCategory() + ")");
                ResetForm();
            }
            else
            {
                UIManager.Instance.ShowToast("Failed to add item.");
            }
        }
    }
}
