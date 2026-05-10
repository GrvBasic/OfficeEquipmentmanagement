using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OEMS.Core;
using OEMS.Models;

namespace OEMS.UI
{
    /// <summary>
    /// Generic list / report viewer. Same prefab used for Employees,
    /// Inventory, and Assignments listings.
    /// </summary>
    public class ListViewController : MonoBehaviour
    {
        [Header("Title")]
        public TextMeshProUGUI titleText;

        [Header("Content")]
        public TextMeshProUGUI contentText;

        [Header("Buttons")]
        public Button backButton;

        private void OnEnable()
        {
            if (backButton)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(() => UIManager.Instance.ShowDashboard());
            }
        }

        public void PopulateEmployees()
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            if (titleText) titleText.text = "Registered Employees (" + dm.Employees.Count + ")";

            var sb = new StringBuilder();
            if (dm.Employees.Count == 0)
            {
                sb.Append("No employees registered yet.");
            }
            else
            {
                foreach (var e in dm.Employees)
                {
                    sb.AppendLine("<b>" + e.employeeID + "</b> - " + e.name);
                    sb.AppendLine("  Dept: " + e.department + " | Contact: " + e.contact);
                    sb.AppendLine("  Registered: " + e.dateRegistered);
                    var active = dm.GetActiveAssignments(e.employeeID);
                    sb.AppendLine("  Active items: " + active.Count);
                    sb.AppendLine();
                }
            }
            if (contentText) contentText.text = sb.ToString();
        }

        public void PopulateInventory()
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            var all = dm.GetAllItems();
            if (titleText) titleText.text = "Inventory Items (" + all.Count + ")";

            var sb = new StringBuilder();
            if (all.Count == 0)
            {
                sb.Append("No inventory items added yet.");
            }
            else
            {
                foreach (var i in all)
                {
                    sb.AppendLine("<b>" + i.itemID + "</b> - " + i.itemName + "  [" + i.GetCategory() + "]");
                    sb.AppendLine("  Total: " + i.totalQuantity +
                                  " | Available: " + i.availableQuantity +
                                  " | Assigned: " + i.AssignedQuantity);
                    sb.AppendLine("  Damaged: " + i.damagedQuantity +
                                  " | Consumed: " + i.consumedQuantity);
                    if (!string.IsNullOrEmpty(i.description))
                        sb.AppendLine("  Desc: " + i.description);
                    sb.AppendLine();
                }
            }
            if (contentText) contentText.text = sb.ToString();
        }

        public void PopulateAssignments()
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            if (titleText) titleText.text = "Assignment History (" + dm.Assignments.Count + ")";

            var sb = new StringBuilder();
            if (dm.Assignments.Count == 0)
            {
                sb.Append("No assignments yet.");
            }
            else
            {
                // newest first
                for (int k = dm.Assignments.Count - 1; k >= 0; k--)
                {
                    var a = dm.Assignments[k];
                    sb.AppendLine("<b>" + a.assignmentID + "</b> - " + StatusColor(a.status));
                    sb.AppendLine("  " + a.itemName + " (x" + a.quantity + ") → " + a.employeeName);
                    sb.AppendLine("  Issued: " + a.assignedDate);
                    if (!string.IsNullOrEmpty(a.returnedDate))
                        sb.AppendLine("  Returned: " + a.returnedDate);
                    if (!string.IsNullOrEmpty(a.notes))
                        sb.AppendLine("  Notes: " + a.notes);
                    sb.AppendLine();
                }
            }
            if (contentText) contentText.text = sb.ToString();
        }

        private string StatusColor(AssignmentStatus s)
        {
            switch (s)
            {
                case AssignmentStatus.Active:           return "<color=#2563eb>ACTIVE</color>";
                case AssignmentStatus.ReturnedGood:     return "<color=#16a34a>RETURNED (Good)</color>";
                case AssignmentStatus.ReturnedDamaged:  return "<color=#ea580c>RETURNED (Damaged)</color>";
                case AssignmentStatus.Consumed:         return "<color=#6b7280>CONSUMED</color>";
                default: return s.ToString();
            }
        }
    }
}
