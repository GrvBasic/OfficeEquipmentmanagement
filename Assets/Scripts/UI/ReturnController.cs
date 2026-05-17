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
    /// Lists all active (Assigned) assignments. Admin picks one,
    /// selects the item condition (Good / Damaged / Consumed), and confirms.
    ///
    /// Uses the new split model:
    ///   assignmentStatus → Returned
    ///   itemCondition    → Good / Damaged / Consumed
    /// </summary>
    public class ReturnController : MonoBehaviour
    {
        [Header("Selection")]
        public TMP_Dropdown     assignmentDropdown;
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

        private List<Assignment> currentList = new List<Assignment>();

        // ═════════════════════════════════════════════════════════════════════
        private void OnEnable()
        {
            HookUpButtons();
            RefreshList();
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
            if (assignmentDropdown)
            {
                assignmentDropdown.onValueChanged.RemoveAllListeners();
                assignmentDropdown.onValueChanged.AddListener(OnSelectionChanged);
            }
        }

        public void RefreshList()
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            // Only returnable (indispensable) active assignments show here
            // Dispensable items are already consumed on assign, but we still let
            // admin process them if needed (mark notes etc.)
            currentList = dm.GetAllActiveAssignments();

            if (assignmentDropdown != null)
            {
                assignmentDropdown.ClearOptions();
                var opts = new List<string>();
                if (currentList.Count == 0)
                {
                    opts.Add("-- No active assignments --");
                }
                else
                {
                    foreach (var a in currentList)
                        opts.Add(string.Format("{0}  |  {1}  →  {2}  (x{3})",
                            a.assignmentID, a.itemID, a.employeeFullName, a.quantity));
                }
                assignmentDropdown.AddOptions(opts);
            }

            if (goodToggle)   goodToggle.isOn = true;
            if (remarksInput) remarksInput.text = "";
            UpdateDetailText();
        }

        private void OnSelectionChanged(int _) { UpdateDetailText(); }

        private void UpdateDetailText()
        {
            if (assignmentDetailText == null) return;
            int idx = assignmentDropdown ? assignmentDropdown.value : -1;
            if (idx < 0 || idx >= currentList.Count) { assignmentDetailText.text = ""; return; }

            var a = currentList[idx];
            assignmentDetailText.text = string.Format(
                "<b>{0}</b>\n" +
                "Item: {1} ({2})\n" +
                "Employee: {3} ({4})\n" +
                "Qty: {5}   Issued: {6}\n" +
                "Type: {7}",
                a.assignmentID,
                a.itemName, a.itemID,
                a.employeeFullName, a.employeeID,
                a.quantity, a.assignedDate,
                a.isReturnable ? "Indispensable (returnable)" : "Dispensable (consumable)");
        }

        // ─────────────────────────────────────────────────────────────────────
        public void SubmitReturn()
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            if (currentList.Count == 0)
            {
                UIManager.Instance.ShowToast("No active assignments to process.");
                return;
            }

            int idx = assignmentDropdown ? assignmentDropdown.value : -1;
            if (idx < 0 || idx >= currentList.Count)
            {
                UIManager.Instance.ShowToast("Invalid selection.");
                return;
            }

            // Determine condition from toggles
            ItemCondition condition = ItemCondition.Good;
            if (damagedToggle  && damagedToggle.isOn)  condition = ItemCondition.Damaged;
            else if (consumedToggle && consumedToggle.isOn) condition = ItemCondition.Consumed;

            var a       = currentList[idx];
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
                    "\"" + itemName + "\" returned — " + condLabel + ".");
                RefreshList();
            }
            else
            {
                UIManager.Instance.ShowToast("Return failed. Assignment may already be closed.");
            }
        }
    }
}
