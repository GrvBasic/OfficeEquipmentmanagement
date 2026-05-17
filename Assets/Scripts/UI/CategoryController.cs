using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OEMS.Core;
using OEMS.Models;

namespace OEMS.UI
{
    /// <summary>
    /// CATEGORY MANAGEMENT screen.
    /// Lets the admin add custom inventory categories (e.g. "Furniture", "Tools")
    /// and choose whether they are Indispensable (returnable) or Dispensable (consumable).
    /// Corresponds to the InventoryCategory entity in the ER diagram.
    /// </summary>
    public class CategoryController : MonoBehaviour
    {
        [Header("Add Category Form")]
        public TMP_InputField categoryNameInput;
        public Toggle         indispensableToggle;   // ON = returnable (Indispensable)
        public Toggle         dispensableToggle;      // ON = consumable (Dispensable)
        public TMP_InputField descriptionInput;

        [Header("Category List")]
        public TextMeshProUGUI categoryListText;

        [Header("Buttons")]
        public Button submitButton;
        public Button backButton;

        // ═════════════════════════════════════════════════════════════════════
        private void OnEnable()
        {
            HookUpButtons();
            ResetForm();
            RefreshList();
        }

        private void HookUpButtons()
        {
            if (submitButton)
            {
                submitButton.onClick.RemoveAllListeners();
                submitButton.onClick.AddListener(SubmitCategory);
            }
            if (backButton)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(() => UIManager.Instance.ShowDashboard());
            }
        }

        private void ResetForm()
        {
            if (categoryNameInput) categoryNameInput.text = "";
            if (descriptionInput)  descriptionInput.text  = "";
            if (indispensableToggle) indispensableToggle.isOn = true;
        }

        // ─────────────────────────────────────────────────────────────────────
        // ADD CATEGORY
        // ─────────────────────────────────────────────────────────────────────
        public void SubmitCategory()
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            string name = categoryNameInput ? categoryNameInput.text.Trim() : "";
            string desc = descriptionInput  ? descriptionInput.text.Trim()  : "";

            if (string.IsNullOrEmpty(name))
            {
                UIManager.Instance.ShowToast("Category name is required.");
                return;
            }

            bool isReturnable = indispensableToggle != null && indispensableToggle.isOn;
            var cat = new InventoryCategory(name, isReturnable, desc);

            if (dm.AddCategory(cat))
            {
                UIManager.Instance.ShowToast(
                    "Category \"" + name + "\" added (" +
                    (isReturnable ? "Indispensable" : "Dispensable") + ").");
                ResetForm();
                RefreshList();
            }
            else
            {
                UIManager.Instance.ShowToast("Category \"" + name + "\" already exists.");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        // REFRESH LIST
        // ─────────────────────────────────────────────────────────────────────
        public void RefreshList()
        {
            var dm = DataManager.Instance;
            if (dm == null || categoryListText == null) return;

            var cats = dm.Categories;
            if (cats.Count == 0)
            {
                categoryListText.text = "No categories yet.";
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("<b>Indispensable (Returnable)</b>");
            bool anyInd = false;
            foreach (var c in cats)
                if (c.isReturnable)
                {
                    sb.AppendLine("  <color=#2563eb>● " + c.categoryName + "</color>"
                                  + (string.IsNullOrEmpty(c.description) ? "" : "  — " + c.description));
                    anyInd = true;
                }
            if (!anyInd) sb.AppendLine("  (none)");

            sb.AppendLine();
            sb.AppendLine("<b>Dispensable (Consumable)</b>");
            bool anyDis = false;
            foreach (var c in cats)
                if (!c.isReturnable)
                {
                    sb.AppendLine("  <color=#6b7280>● " + c.categoryName + "</color>"
                                  + (string.IsNullOrEmpty(c.description) ? "" : "  — " + c.description));
                    anyDis = true;
                }
            if (!anyDis) sb.AppendLine("  (none)");

            categoryListText.text = sb.ToString();
        }
    }
}
