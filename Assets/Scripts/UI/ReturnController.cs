using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OEMS.Core;
using OEMS.Models;

namespace OEMS.UI
{
    /// <summary>
    /// RETURN MODULE controller (per synopsis section 9.4).
    /// </summary>
    public class ReturnController : MonoBehaviour
    {
        [Header("Selection")]
        public TMP_Dropdown assignmentDropdown;

        [Header("Condition Toggles")]
        public Toggle goodToggle;
        public Toggle damagedToggle;
        public Toggle consumedToggle;

        [Header("Notes")]
        public TMP_InputField notesInput;

        [Header("Buttons")]
        public Button submitButton;
        public Button backButton;

        private List<Assignment> currentList = new List<Assignment>();

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
        }

        public void RefreshList()
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            currentList = dm.GetAllActiveAssignments();
            // remove dispensable / consumed items from return list (they're already consumed)
            currentList.RemoveAll(a => a.status == AssignmentStatus.Consumed);

            if (assignmentDropdown != null)
            {
                assignmentDropdown.ClearOptions();
                var opts = new List<string>();
                if (currentList.Count == 0)
                    opts.Add("-- No active assignments --");
                else
                    foreach (var a in currentList)
                        opts.Add(string.Format("{0} | {1} → {2} (x{3})",
                            a.assignmentID, a.itemName, a.employeeName, a.quantity));
                assignmentDropdown.AddOptions(opts);
            }

            if (goodToggle) goodToggle.isOn = true;
            if (notesInput) notesInput.text = "";
        }

        public void SubmitReturn()
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            if (currentList.Count == 0)
            {
                UIManager.Instance.ShowToast("No assignments to return.");
                return;
            }

            int idx = assignmentDropdown != null ? assignmentDropdown.value : -1;
            if (idx < 0 || idx >= currentList.Count)
            {
                UIManager.Instance.ShowToast("Invalid selection.");
                return;
            }

            AssignmentStatus condition = AssignmentStatus.ReturnedGood;
            if (damagedToggle && damagedToggle.isOn) condition = AssignmentStatus.ReturnedDamaged;
            else if (consumedToggle && consumedToggle.isOn) condition = AssignmentStatus.Consumed;

            var a = currentList[idx];
            string notes = notesInput ? notesInput.text : "";

            if (dm.ReturnItem(a.assignmentID, condition, notes))
            {
                string conditionText = condition.ToString().Replace("Returned", "").Replace("Consumed", "Consumed");
                UIManager.Instance.ShowToast("Return processed: " + a.itemName + " (" + conditionText + ")");
                RefreshList();
            }
            else
            {
                UIManager.Instance.ShowToast("Return failed.");
            }
        }
    }
}
