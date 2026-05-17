using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OEMS.Core;
using OEMS.Models;

namespace OEMS.UI
{
    /// <summary>
    /// Generic list / report viewer used by multiple screens:
    ///   - Employee List         (PopulateEmployees)
    ///   - Available Inventory   (PopulateInventory)
    ///   - Assignment History    (PopulateAssignments)
    ///   - Items by Employee     (PopulateEmployeeItems)
    ///
    /// Each employee row has a "View Items" button generated at runtime.
    /// </summary>
    public class ListViewController : MonoBehaviour
    {
        [Header("Header")]
        public TextMeshProUGUI titleText;

        [Header("Content")]
        public TextMeshProUGUI contentText;

        [Header("Employee Item Buttons Container")]
        // A ScrollRect content transform where per-employee "View Items" buttons
        // are spawned. Leave null if using text-only layout.
        public Transform employeeButtonContainer;
        public GameObject employeeButtonPrefab;     // Button prefab with TMP child

        [Header("Back / Filter Buttons")]
        public Button backButton;
        public Button filterActiveButton;       // assignments filter: show Active only
        public Button filterAllButton;          // assignments filter: show All

        private bool showActiveOnly = false;    // for assignment history toggle

        // ═════════════════════════════════════════════════════════════════════
        private void OnEnable()
        {
            if (backButton)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(() => UIManager.Instance.ShowDashboard());
            }
            if (filterActiveButton)
            {
                filterActiveButton.onClick.RemoveAllListeners();
                filterActiveButton.onClick.AddListener(() => { showActiveOnly = true;  PopulateAssignments(); });
            }
            if (filterAllButton)
            {
                filterAllButton.onClick.RemoveAllListeners();
                filterAllButton.onClick.AddListener(() => { showActiveOnly = false; PopulateAssignments(); });
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // EMPLOYEE LIST
        // ═════════════════════════════════════════════════════════════════════
        public void PopulateEmployees()
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            if (titleText) titleText.text = "Registered Employees  (" + dm.Employees.Count + ")";

            // Clear dynamic buttons
            ClearEmployeeButtons();

            var sb = new StringBuilder();
            if (dm.Employees.Count == 0)
            {
                sb.Append("No employees registered yet.");
            }
            else
            {
                foreach (var e in dm.Employees)
                {
                    int activeCount = dm.GetActiveAssignmentsForEmployee(e.employeeID).Count;
                    sb.AppendLine("<b>" + e.employeeID + "</b>  " + e.FullName);
                    sb.AppendLine("  Dept: " + e.department
                                  + (string.IsNullOrEmpty(e.email) ? "" : "  |  " + e.email));
                    sb.AppendLine("  Registered: " + e.dateRegistered);
                    sb.AppendLine("  Active items held: <b>" + activeCount + "</b>");
                    sb.AppendLine();

                    // Spawn a "View Items →" button for this employee
                    SpawnEmployeeViewButton(e);
                }
            }
            if (contentText) contentText.text = sb.ToString();
        }

        private void SpawnEmployeeViewButton(Employee emp)
        {
            if (employeeButtonContainer == null || employeeButtonPrefab == null) return;

            var go  = Instantiate(employeeButtonPrefab, employeeButtonContainer);
            var btn = go.GetComponent<Button>();
            var lbl = go.GetComponentInChildren<TextMeshProUGUI>();

            if (lbl) lbl.text = "View Items → " + emp.FullName + " (" + emp.employeeID + ")";
            if (btn)
            {
                string eid = emp.employeeID;   // capture for lambda
                btn.onClick.AddListener(() => UIManager.Instance.ShowEmployeeItems(eid));
            }
        }

        private void ClearEmployeeButtons()
        {
            if (employeeButtonContainer == null) return;
            foreach (Transform child in employeeButtonContainer)
                Destroy(child.gameObject);
        }

        // ═════════════════════════════════════════════════════════════════════
        // AVAILABLE INVENTORY
        // ═════════════════════════════════════════════════════════════════════
        public void PopulateInventory()
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            var all = dm.GetAllItems();
            if (titleText) titleText.text = "Inventory  (" + all.Count + " records)";

            var sb = new StringBuilder();
            if (all.Count == 0) { sb.Append("No inventory items added yet."); }
            else
            {
                // ── Indispensable units ──────────────────────────────────────
                sb.AppendLine("<b>─── Indispensable Units (ASSET-XXXX) ───</b>");
                bool anyInd = false;
                foreach (var i in dm.IndispensableItems)
                {
                    anyInd = true;
                    string colour = StatusColour(i.itemStatus);
                    sb.AppendLine(string.Format(
                        "<color={0}><b>{1}</b>  {2}  [{3}]  Status: {4}</color>",
                        colour, i.itemID, i.itemName, i.categoryName, i.itemStatus));
                    if (i is IndispensableItem indItem && !string.IsNullOrEmpty(indItem.serialNumber))
                        sb.AppendLine("  Serial: " + indItem.serialNumber);
                    sb.AppendLine("  Added: " + i.dateAdded);
                    sb.AppendLine();
                }
                if (!anyInd) sb.AppendLine("  (none)\n");

                // ── Dispensable batches ──────────────────────────────────────
                sb.AppendLine("<b>─── Dispensable Batches (BATCH-XXXX) ───</b>");
                bool anyDis = false;
                foreach (var d in dm.DispensableItems)
                {
                    anyDis = true;
                    string colour = d.availableQuantity > 0 ? "#16a34a" : "#6b7280";
                    sb.AppendLine(string.Format(
                        "<color={0}><b>{1}</b>  {2}  [{3}]</color>",
                        colour, d.itemID, d.itemName, d.categoryName));
                    sb.AppendLine(string.Format(
                        "  Total: {0}  |  <color=#16a34a>Available: {1}</color>" +
                        "  |  <color=#ea580c>Damaged: {2}</color>" +
                        "  |  <color=#6b7280>Consumed: {3}</color>",
                        d.totalQuantity, d.availableQuantity,
                        d.damagedQuantity, d.consumedQuantity));
                    if (d is DispensableItem disItem && !string.IsNullOrEmpty(disItem.batchReference))
                        sb.AppendLine("  Ref: " + disItem.batchReference);
                    sb.AppendLine("  Added: " + d.dateAdded);
                    sb.AppendLine();
                }
                if (!anyDis) sb.AppendLine("  (none)\n");
            }

            if (contentText) contentText.text = sb.ToString();
        }

        private static string StatusColour(ItemStatus s)
        {
            switch (s)
            {
                case ItemStatus.Good:     return "#16a34a";
                case ItemStatus.Assigned: return "#2563eb";
                case ItemStatus.Damaged:  return "#ea580c";
                case ItemStatus.Consumed: return "#6b7280";
                default:                  return "#ffffff";
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // ASSIGNMENT HISTORY
        // ═════════════════════════════════════════════════════════════════════
        public void PopulateAssignments()
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            List<Assignment> list = showActiveOnly
                ? dm.GetAllActiveAssignments()
                : dm.Assignments;

            string filterLabel = showActiveOnly ? "  [Active only]" : "  [All]";
            if (titleText) titleText.text =
                "Assignment History  (" + list.Count + ")" + filterLabel;

            var sb = new StringBuilder();
            if (list.Count == 0)
            {
                sb.Append(showActiveOnly ? "No active assignments." : "No assignments yet.");
            }
            else
            {
                // Newest first
                for (int k = list.Count - 1; k >= 0; k--)
                {
                    var a = list[k];
                    bool isActive = a.assignmentStatus == AssignmentStatus.Assigned;
                    string statusColour = isActive ? "#2563eb" : "#6b7280";

                    sb.AppendLine(string.Format(
                        "<b>{0}</b>  <color={1}>{2}</color>",
                        a.assignmentID, statusColour, a.assignmentStatus));
                    sb.AppendLine(string.Format(
                        "  Item: <b>{0}</b>  ({1})  [{2}]",
                        a.itemID, a.itemName, a.categoryName));
                    sb.AppendLine(string.Format(
                        "  Employee: {0}  ({1})  qty: {2}",
                        a.employeeFullName, a.employeeID, a.quantity));
                    sb.AppendLine("  Issued: " + a.assignedDate);

                    if (!isActive)
                    {
                        string condColour = ConditionColour(a.itemCondition);
                        sb.AppendLine("  Returned: " + a.returnedDate);
                        sb.AppendLine(string.Format(
                            "  Condition: <color={0}>{1}</color>",
                            condColour, a.itemCondition));
                    }
                    if (!string.IsNullOrEmpty(a.remarks))
                        sb.AppendLine("  Remarks: " + a.remarks);
                    sb.AppendLine();
                }
            }
            if (contentText) contentText.text = sb.ToString();
        }

        private static string ConditionColour(ItemCondition c)
        {
            switch (c)
            {
                case ItemCondition.Good:     return "#16a34a";
                case ItemCondition.Damaged:  return "#ea580c";
                case ItemCondition.Consumed: return "#6b7280";
                default:                     return "#ffffff";
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // PER-EMPLOYEE ITEMS VIEW
        // ═════════════════════════════════════════════════════════════════════
        public void PopulateEmployeeItems(string employeeID)
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            var emp = dm.FindEmployee(employeeID);
            if (emp == null)
            {
                if (titleText) titleText.text = "Employee not found";
                if (contentText) contentText.text = "No record for ID: " + employeeID;
                return;
            }

            var activeAssignments = dm.GetItemsByEmployee(employeeID);
            if (titleText) titleText.text =
                emp.FullName + "  (" + employeeID + ")\nItems held: " + activeAssignments.Count;

            var sb = new StringBuilder();
            sb.AppendLine("<b>Employee:</b> " + emp.FullName);
            sb.AppendLine("<b>ID:</b> " + emp.employeeID);
            sb.AppendLine("<b>Dept:</b> " + emp.department);
            if (!string.IsNullOrEmpty(emp.email))
                sb.AppendLine("<b>Email:</b> " + emp.email);
            sb.AppendLine();

            if (activeAssignments.Count == 0)
            {
                sb.AppendLine("<color=#6b7280>No items currently held.</color>");
            }
            else
            {
                sb.AppendLine("<b>Currently Held Items</b>");
                sb.AppendLine("─────────────────────────");
                foreach (var a in activeAssignments)
                {
                    sb.AppendLine(string.Format(
                        "<b><color=#2563eb>{0}</color></b>  {1}  [{2}]",
                        a.itemID, a.itemName, a.categoryName));
                    sb.AppendLine(string.Format(
                        "  Assignment: {0}  |  Qty: {1}",
                        a.assignmentID, a.quantity));
                    sb.AppendLine("  Issued on: " + a.assignedDate);
                    sb.AppendLine();
                }
            }

            // Also show past assignment history for this employee
            var history = dm.GetAssignmentHistoryForEmployee(employeeID);
            var returned = history.FindAll(a => a.assignmentStatus == AssignmentStatus.Returned);
            if (returned.Count > 0)
            {
                sb.AppendLine("<b>Return History</b>");
                sb.AppendLine("─────────────────────────");
                for (int k = returned.Count - 1; k >= 0; k--)
                {
                    var a = returned[k];
                    string condColour = ConditionColour(a.itemCondition);
                    sb.AppendLine(string.Format(
                        "<b>{0}</b>  {1}  ({2})",
                        a.itemID, a.itemName, a.assignmentID));
                    sb.AppendLine(string.Format(
                        "  Issued: {0}  →  Returned: {1}",
                        a.assignedDate, a.returnedDate));
                    sb.AppendLine(string.Format(
                        "  Condition: <color={0}>{1}</color>",
                        condColour, a.itemCondition));
                    if (!string.IsNullOrEmpty(a.remarks))
                        sb.AppendLine("  Remarks: " + a.remarks);
                    sb.AppendLine();
                }
            }

            if (contentText) contentText.text = sb.ToString();
        }
    }
}
