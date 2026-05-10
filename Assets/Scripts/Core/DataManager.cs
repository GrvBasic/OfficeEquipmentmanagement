using System.Collections.Generic;
using System.IO;
using UnityEngine;
using OEMS.Models;

namespace OEMS.Core
{
    /// <summary>
    /// Singleton that handles ALL data operations and file-based local storage.
    /// Per project synopsis: "All information is stored locally in text file".
    /// Uses Unity's Application.persistentDataPath which works on Android/Windows/macOS.
    /// </summary>
    public class DataManager : MonoBehaviour
    {
        public static DataManager Instance { get; private set; }

        private const string DATA_FILE_NAME = "oems_data.txt";
        private string DataFilePath { get { return Path.Combine(Application.persistentDataPath, DATA_FILE_NAME); } }

        private DatabaseWrapper db = new DatabaseWrapper();

        // ----- Public read-only access to lists -----
        public List<Employee>          Employees           { get { return db.employees; } }
        public List<DispensableItem>   DispensableItems    { get { return db.dispensableItems; } }
        public List<IndispensableItem> IndispensableItems  { get { return db.indispensableItems; } }
        public List<Assignment>        Assignments         { get { return db.assignments; } }

        // ============================================================
        // SINGLETON SETUP
        // ============================================================
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadData();
        }

        // ============================================================
        // FILE I/O
        // ============================================================
        public void SaveData()
        {
            try
            {
                string json = JsonUtility.ToJson(db, true);
                File.WriteAllText(DataFilePath, json);
                Debug.Log("[OEMS] Data saved to: " + DataFilePath);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[OEMS] Save failed: " + e.Message);
            }
        }

        public void LoadData()
        {
            try
            {
                if (File.Exists(DataFilePath))
                {
                    string json = File.ReadAllText(DataFilePath);
                    db = JsonUtility.FromJson<DatabaseWrapper>(json);
                    if (db == null) db = new DatabaseWrapper();
                    Debug.Log("[OEMS] Data loaded from: " + DataFilePath);
                }
                else
                {
                    db = new DatabaseWrapper();
                    Debug.Log("[OEMS] No existing data, starting fresh.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[OEMS] Load failed: " + e.Message);
                db = new DatabaseWrapper();
            }
        }

        public void ResetAllData()
        {
            db = new DatabaseWrapper();
            SaveData();
        }

        // ============================================================
        // EMPLOYEE OPERATIONS  (Registration Module)
        // ============================================================
        public string GenerateEmployeeID()
        {
            string id = "EMP" + db.employeeCounter.ToString("D4");
            db.employeeCounter++;
            return id;
        }

        public bool AddEmployee(Employee e)
        {
            if (e == null || string.IsNullOrEmpty(e.employeeID)) return false;
            if (FindEmployee(e.employeeID) != null) return false;   // duplicate
            db.employees.Add(e);
            SaveData();
            return true;
        }

        public bool RemoveEmployee(string employeeID)
        {
            // can't remove employee with active assignments
            foreach (var a in db.assignments)
            {
                if (a.employeeID == employeeID && a.status == AssignmentStatus.Active)
                    return false;
            }
            int removed = db.employees.RemoveAll(x => x.employeeID == employeeID);
            if (removed > 0) { SaveData(); return true; }
            return false;
        }

        public Employee FindEmployee(string employeeID)
        {
            return db.employees.Find(x => x.employeeID == employeeID);
        }

        // ============================================================
        // INVENTORY OPERATIONS  (Inventory Management Module)
        // ============================================================
        public string GenerateItemID()
        {
            string id = "ITM" + db.itemCounter.ToString("D4");
            db.itemCounter++;
            return id;
        }

        public bool AddInventoryItem(InventoryItem item)
        {
            if (item == null) return false;
            if (FindItem(item.itemID) != null) return false;        // duplicate
            if (item is DispensableItem)
                db.dispensableItems.Add((DispensableItem)item);
            else if (item is IndispensableItem)
                db.indispensableItems.Add((IndispensableItem)item);
            else
                return false;
            SaveData();
            return true;
        }

        public InventoryItem FindItem(string itemID)
        {
            var d = db.dispensableItems.Find(x => x.itemID == itemID);
            if (d != null) return d;
            var i = db.indispensableItems.Find(x => x.itemID == itemID);
            return i;
        }

        public List<InventoryItem> GetAllItems()
        {
            var all = new List<InventoryItem>();
            all.AddRange(db.dispensableItems.ToArray());
            all.AddRange(db.indispensableItems.ToArray());
            return all;
        }

        public List<InventoryItem> GetAvailableItems()
        {
            var available = new List<InventoryItem>();
            foreach (var x in db.dispensableItems)
                if (x.availableQuantity > 0) available.Add(x);
            foreach (var x in db.indispensableItems)
                if (x.availableQuantity > 0) available.Add(x);
            return available;
        }

        // ============================================================
        // ASSIGNMENT OPERATIONS  (Assignment Module)
        // ============================================================
        public string GenerateAssignmentID()
        {
            string id = "ASN" + db.assignmentCounter.ToString("D4");
            db.assignmentCounter++;
            return id;
        }

        public Assignment AssignItem(string employeeID, string itemID, int quantity)
        {
            var emp = FindEmployee(employeeID);
            var item = FindItem(itemID);

            if (emp == null || item == null) return null;
            if (quantity <= 0 || item.availableQuantity < quantity) return null;

            // reduce available stock
            item.availableQuantity -= quantity;

            // for dispensable items, mark as consumed immediately
            if (!item.IsReturnable())
                item.consumedQuantity += quantity;

            var a = new Assignment(GenerateAssignmentID(), emp, item, quantity);
            db.assignments.Add(a);
            SaveData();
            return a;
        }

        public List<Assignment> GetActiveAssignments(string employeeID)
        {
            return db.assignments.FindAll(
                a => a.employeeID == employeeID && a.status == AssignmentStatus.Active);
        }

        public List<Assignment> GetAllActiveAssignments()
        {
            return db.assignments.FindAll(a => a.status == AssignmentStatus.Active);
        }

        // ============================================================
        // RETURN OPERATIONS  (Return Module)
        // ============================================================
        public bool ReturnItem(string assignmentID, AssignmentStatus returnStatus, string notes)
        {
            var a = db.assignments.Find(x => x.assignmentID == assignmentID);
            if (a == null) return false;
            if (a.status != AssignmentStatus.Active) return false;

            var item = FindItem(a.itemID);
            if (item == null) return false;

            switch (returnStatus)
            {
                case AssignmentStatus.ReturnedGood:
                    item.availableQuantity += a.quantity;
                    break;
                case AssignmentStatus.ReturnedDamaged:
                    item.damagedQuantity += a.quantity;
                    break;
                case AssignmentStatus.Consumed:
                    item.consumedQuantity += a.quantity;
                    break;
                default:
                    return false;
            }

            a.status = returnStatus;
            a.returnedDate = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            a.notes = notes;
            SaveData();
            return true;
        }

        // ============================================================
        // DASHBOARD STATISTICS
        // ============================================================
        public int GetTotalEmployeeCount() { return db.employees.Count; }

        public int GetTotalItemTypes() { return db.dispensableItems.Count + db.indispensableItems.Count; }

        public int GetTotalAvailableQuantity()
        {
            int total = 0;
            foreach (var x in db.dispensableItems)   total += x.availableQuantity;
            foreach (var x in db.indispensableItems) total += x.availableQuantity;
            return total;
        }

        public int GetTotalAssignedQuantity()
        {
            int total = 0;
            foreach (var x in db.indispensableItems) total += x.AssignedQuantity;
            return total;
        }

        public int GetTotalDamagedQuantity()
        {
            int total = 0;
            foreach (var x in db.dispensableItems)   total += x.damagedQuantity;
            foreach (var x in db.indispensableItems) total += x.damagedQuantity;
            return total;
        }

        public int GetTotalConsumedQuantity()
        {
            int total = 0;
            foreach (var x in db.dispensableItems)   total += x.consumedQuantity;
            foreach (var x in db.indispensableItems) total += x.consumedQuantity;
            return total;
        }
    }
}
