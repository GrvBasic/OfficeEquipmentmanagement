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
    ///   - Employee List              (PopulateEmployees)
    ///   - Available Inventory        (PopulateInventory)
    ///   - Assignment History         (PopulateAssignments — optionally filtered to one employee)
    ///   - Items by Employee          (PopulateEmployeeItems)
    ///   - Employee Assignment list   (PopulateAssignmentsForEmployee — reuses the assignments view)
    ///
    /// Employee list rendering: each employee gets ONE EmployeeInfoButton spawned
    /// into the scroll content. The button's label IS the employee's info card
    /// (ID, name, dept, email, registered date, active item count). Clicking the
    /// button opens the Assignment History panel filtered to that employee.
    /// </summary>
    public class ListViewController : MonoBehaviour
    {
        [Header("Header")]
        public TextMeshProUGUI titleText;

        [Header("Content")]
        public TextMeshProUGUI contentText;

        [Header("Employee Item Buttons Container")]
        // A ScrollRect content transform where per-employee buttons are spawned.
        // Two buttons per employee ("View Items" and "View Assignments") are appended.
        // Leave null if using text-only layout.
        public Transform employeeButtonContainer;
        public GameObject employeeButtonPrefab;     // Button prefab with TMP child

        [Header("Back / Filter Buttons")]
        public Button backButton;
        public Button filterActiveButton;       // assignments filter: show Active only
        public Button filterAllButton;          // assignments filter: show All

        [Header("Inventory Status Filter Buttons")]
        public Button filterAvailableButton;    // inventory filter: Good / available stock
        public Button filterAssignedButton;     // inventory filter: currently assigned
        public Button filterDamagedButton;      // inventory filter: damaged
        public Button filterConsumedButton;     // inventory filter: consumed
        public Button filterAllInventoryButton; // inventory filter: show every record (optional)

        private bool   showActiveOnly  = false;  // for assignment history toggle
        private string filterEmployeeID = null;  // when set, PopulateAssignments shows only this employee's records

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

            // Inventory status filters (only present on the View Inventory panel).
            if (filterAvailableButton)
            {
                filterAvailableButton.onClick.RemoveAllListeners();
                filterAvailableButton.onClick.AddListener(() => PopulateInventoryByStatus(ItemStatus.Good));
            }
            if (filterAssignedButton)
            {
                filterAssignedButton.onClick.RemoveAllListeners();
                filterAssignedButton.onClick.AddListener(() => PopulateInventoryByStatus(ItemStatus.Assigned));
            }
            if (filterDamagedButton)
            {
                filterDamagedButton.onClick.RemoveAllListeners();
                filterDamagedButton.onClick.AddListener(() => PopulateInventoryByStatus(ItemStatus.Damaged));
            }
            if (filterConsumedButton)
            {
                filterConsumedButton.onClick.RemoveAllListeners();
                filterConsumedButton.onClick.AddListener(() => PopulateInventoryByStatus(ItemStatus.Consumed));
            }
            if (filterAllInventoryButton)
            {
                filterAllInventoryButton.onClick.RemoveAllListeners();
                filterAllInventoryButton.onClick.AddListener(() => PopulateInventory());
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        // EMPLOYEE LIST
        //
        // The Content transform under Scroll/Viewport is a VerticalLayoutGroup.
        // For each employee we instantiate one EmployeeInfoButton prefab into it —
        // the button's label IS the employee info (ID, name, dept, email, registered
        // date, active item count). Clicking the button opens the Assignment History
        // panel filtered to that employee (all assignments — active + returned).
        //
        // The legacy ContentText (still wired for back-compat) is cleared on each
        // refresh so it doesn't show stale text behind the buttons.
        // ═════════════════════════════════════════════════════════════════════
        public void PopulateEmployees()
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            if (titleText) titleText.text = "Registered Employees  (" + dm.Employees.Count + ")";

            // ContentText is no longer used for the employee list — the buttons carry
            // the info now. Clear any stale text so it doesn't render behind them.
            if (contentText) contentText.text = "";

            // Clear previously-spawned buttons
            ClearEmployeeButtons();

            if (dm.Employees.Count == 0)
            {
                // Fall back to the old text path when there's nothing to show.
                if (contentText) contentText.text = "No employees registered yet.";
                return;
            }

            foreach (var e in dm.Employees)
                SpawnEmployeeInfoButton(e);
        }

        /// <summary>
        /// Spawn ONE info-button for an employee. The button's label shows the full
        /// employee record; clicking it opens that employee's assignments view.
        /// </summary>
        private void SpawnEmployeeInfoButton(Employee emp)
        {
            if (employeeButtonContainer == null || employeeButtonPrefab == null) return;

            var dm = DataManager.Instance;
            int activeCount = dm != null
                ? dm.GetActiveAssignmentsForEmployee(emp.employeeID).Count : 0;

            var go  = Instantiate(employeeButtonPrefab, employeeButtonContainer);
            var btn = go.GetComponent<Button>();
            var lbl = go.GetComponentInChildren<TextMeshProUGUI>();

            if (lbl)
            {
                var sb = new StringBuilder();
                sb.AppendLine("<b>" + emp.employeeID + "</b>   " + emp.FullName);
                sb.AppendLine("Dept: " + emp.department
                              + (string.IsNullOrEmpty(emp.email) ? "" : "   |   " + emp.email));
                sb.AppendLine("Registered: " + emp.dateRegistered);
                sb.Append("Active items held: <b>" + activeCount + "</b>");
                lbl.text = sb.ToString();
            }
            if (btn)
            {
                string eid = emp.employeeID;   // capture for lambda
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => UIManager.Instance.ShowEmployeeAssignments(eid));
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

        /// <summary>
        /// Filtered inventory view — shows only items matching a given status.
        ///   Good     → "Available"
        ///   Assigned → currently issued (indispensable units only)
        ///   Damaged  → damaged
        ///   Consumed → consumed / used up
        /// Indispensable units match on their itemStatus; dispensable batches
        /// match when they carry stock in the corresponding bucket.
        /// </summary>
        public void PopulateInventoryByStatus(ItemStatus status)
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            var items = dm.GetItemsByStatus(status);
            string label = StatusLabel(status);
            if (titleText) titleText.text = label + " Inventory  (" + items.Count + ")";

            var sb = new StringBuilder();
            if (items.Count == 0)
            {
                sb.Append("No items with status \"" + label + "\".");
            }
            else
            {
                string colour = StatusColour(status);
                foreach (var i in items)
                {
                    if (i is IndispensableItem indItem)
                    {
                        sb.AppendLine(string.Format(
                            "<color={0}><b>{1}</b>  {2}  [{3}]</color>",
                            colour, indItem.itemID, indItem.itemName, indItem.categoryName));
                        if (!string.IsNullOrEmpty(indItem.serialNumber))
                            sb.AppendLine("  Serial: " + indItem.serialNumber);
                        sb.AppendLine("  Added: " + indItem.dateAdded);
                    }
                    else if (i is DispensableItem disItem)
                    {
                        int qty = status == ItemStatus.Good     ? disItem.availableQuantity
                                : status == ItemStatus.Damaged  ? disItem.damagedQuantity
                                : status == ItemStatus.Consumed ? disItem.consumedQuantity
                                : 0;
                        sb.AppendLine(string.Format(
                            "<color={0}><b>{1}</b>  {2}  [{3}]  —  {4}: {5}</color>",
                            colour, disItem.itemID, disItem.itemName, disItem.categoryName,
                            label, qty));
                        if (!string.IsNullOrEmpty(disItem.batchReference))
                            sb.AppendLine("  Ref: " + disItem.batchReference);
                        sb.AppendLine("  Added: " + disItem.dateAdded);
                    }
                    sb.AppendLine();
                }
            }

            if (contentText) contentText.text = sb.ToString();
        }

        private static string StatusLabel(ItemStatus s)
        {
            switch (s)
            {
                case ItemStatus.Good:     return "Available";
                case ItemStatus.Assigned: return "Assigned";
                case ItemStatus.Damaged:  return "Damaged";
                case ItemStatus.Consumed: return "Consumed";
                default:                  return s.ToString();
            }
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
        // ASSIGNMENT HISTORY  (supports optional per-employee filter)
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>Show the assignment history filtered to a single employee.</summary>
        public void PopulateAssignmentsForEmployee(string employeeID)
        {
            filterEmployeeID = employeeID;
            showActiveOnly   = false;   // default to showing all assignments for this employee
            PopulateAssignments();
        }

        /// <summary>Clear any active employee filter (called when entering the unfiltered view).</summary>
        public void ClearEmployeeFilter()
        {
            filterEmployeeID = null;
        }

        public void PopulateAssignments()
        {
            var dm = DataManager.Instance;
            if (dm == null) return;

            // Apply employee filter (if any) and active/all filter together.
            List<Assignment> list;
            if (!string.IsNullOrEmpty(filterEmployeeID))
            {
                list = showActiveOnly
                    ? dm.GetActiveAssignmentsForEmployee(filterEmployeeID)
                    : dm.GetAssignmentHistoryForEmployee(filterEmployeeID);
            }
            else
            {
                list = showActiveOnly
                    ? dm.GetAllActiveAssignments()
                    : dm.Assignments;
            }

            string filterLabel = showActiveOnly ? "  [Active only]" : "  [All]";
            string empLabel    = "";
            if (!string.IsNullOrEmpty(filterEmployeeID))
            {
                var emp = dm.FindEmployee(filterEmployeeID);
                empLabel = emp != null
                    ? "  —  " + emp.FullName + " (" + filterEmployeeID + ")"
                    : "  —  " + filterEmployeeID;
            }
            if (titleText) titleText.text =
                "Assignment History" + empLabel + "  (" + list.Count + ")" + filterLabel;

            var sb = new StringBuilder();
            if (list.Count == 0)
            {
                if (!string.IsNullOrEmpty(filterEmployeeID))
                    sb.Append(showActiveOnly
                        ? "No active assignments for this employee."
                        : "No assignments on record for this employee.");
                else
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
