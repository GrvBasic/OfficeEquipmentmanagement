using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using OEMS.Models;

namespace OEMS.Core
{
    /// <summary>
    /// Singleton that owns ALL data operations and file-based local storage.
    ///
    /// STORAGE LAYOUT (database-style):
    ///   Each ER-diagram entity lives in its OWN JSON file under Application.persistentDataPath:
    ///     • oems_employees.json   → EmployeeDatabase    (Employee table + EMP-XXXX counter)
    ///     • oems_inventory.json   → InventoryDatabase   (InventoryCategory + Inventory tables
    ///                                                    + ASSET-XXXX / BATCH-XXXX counters)
    ///     • oems_assignments.json → AssignmentDatabase  (Assignment table + ASN-XXXX counter)
    ///
    ///   Each table is loaded/saved INDEPENDENTLY — mirroring how a real RDBMS keeps
    ///   tables in separate files. Save operations only rewrite the file(s) actually
    ///   affected (e.g. AddEmployee only rewrites oems_employees.json).
    ///
    /// MIGRATION:
    ///   On startup, if the legacy monolithic oems_data.txt exists and none of the
    ///   new files do yet, it is imported into the three new tables, then renamed
    ///   to oems_data.txt.legacy so it isn't re-imported.
    ///
    /// KEY DESIGN DECISIONS:
    ///   - IndispensableItem  = one record per physical UNIT (ASSET-XXXX).
    ///     Adding "Dell Laptop x3" creates three IndispensableItem records.
    ///   - DispensableItem    = one record per stock BATCH (BATCH-XXXX).
    ///     Adding "Blue Pen x50" creates one DispensableItem record with qty=50.
    ///   - Assignment.assignmentStatus (Assigned/Returned) is separate from
    ///     Assignment.itemCondition (Good/Damaged/Consumed/NA) per ER diagram.
    /// </summary>
    public class DataManager : MonoBehaviour
    {
        public static DataManager Instance { get; private set; }

        // ── Per-table file names ──────────────────────────────────────────────
        private const string EMPLOYEE_FILE_NAME    = "oems_employees.json";
        private const string INVENTORY_FILE_NAME   = "oems_inventory.json";
        private const string ASSIGNMENT_FILE_NAME  = "oems_assignments.json";
        private const string LEGACY_FILE_NAME      = "oems_data.txt";

        private string EmployeeFilePath   { get { return Path.Combine(Application.persistentDataPath, EMPLOYEE_FILE_NAME);   } }
        private string InventoryFilePath  { get { return Path.Combine(Application.persistentDataPath, INVENTORY_FILE_NAME);  } }
        private string AssignmentFilePath { get { return Path.Combine(Application.persistentDataPath, ASSIGNMENT_FILE_NAME); } }
        private string LegacyFilePath     { get { return Path.Combine(Application.persistentDataPath, LEGACY_FILE_NAME);     } }

        // ── In-memory tables ──────────────────────────────────────────────────
        private EmployeeDatabase   empDb    = new EmployeeDatabase();
        private InventoryDatabase  invDb    = new InventoryDatabase();
        private AssignmentDatabase assignDb = new AssignmentDatabase();

        // ── Read-only access ──────────────────────────────────────────────────
        public List<InventoryCategory>  Categories         { get { return invDb.categories;         } }
        public List<Employee>           Employees          { get { return empDb.employees;          } }
        public List<DispensableItem>    DispensableItems   { get { return invDb.dispensableItems;   } }
        public List<IndispensableItem>  IndispensableItems { get { return invDb.indispensableItems; } }
        public List<Assignment>         Assignments        { get { return assignDb.assignments;     } }

        // ═════════════════════════════════════════════════════════════════════
        // SINGLETON SETUP
        // ═════════════════════════════════════════════════════════════════════
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadData();
        }

        // ═════════════════════════════════════════════════════════════════════
        // FILE I/O — per-table save/load
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>Atomic write: serialise, write to .tmp, then move into place.</summary>
        private void WriteJsonAtomic(string path, string json)
        {
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, json);
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }

        public void SaveEmployees()
        {
            try
            {
                WriteJsonAtomic(EmployeeFilePath, JsonUtility.ToJson(empDb, true));
                Debug.Log("[OEMS] Saved employees → " + EmployeeFilePath);
            }
            catch (Exception e) { Debug.LogError("[OEMS] SaveEmployees failed: " + e.Message); }
        }

        public void SaveInventory()
        {
            try
            {
                WriteJsonAtomic(InventoryFilePath, JsonUtility.ToJson(invDb, true));
                Debug.Log("[OEMS] Saved inventory → " + InventoryFilePath);
            }
            catch (Exception e) { Debug.LogError("[OEMS] SaveInventory failed: " + e.Message); }
        }

        public void SaveAssignments()
        {
            try
            {
                WriteJsonAtomic(AssignmentFilePath, JsonUtility.ToJson(assignDb, true));
                Debug.Log("[OEMS] Saved assignments → " + AssignmentFilePath);
            }
            catch (Exception e) { Debug.LogError("[OEMS] SaveAssignments failed: " + e.Message); }
        }

        /// <summary>Save every table. Use only when multiple tables changed at once
        /// (ResetAllData, legacy migration). Otherwise prefer the targeted Save* methods.</summary>
        public void SaveData()
        {
            SaveEmployees();
            SaveInventory();
            SaveAssignments();
        }

        public void LoadData()
        {
            // 1. One-time migration from the old single file (if present and new files absent).
            TryMigrateLegacyFile();

            // 2. Load each table independently.
            empDb    = LoadTable<EmployeeDatabase>  (EmployeeFilePath,   "employees");
            invDb    = LoadTable<InventoryDatabase> (InventoryFilePath,  "inventory");
            assignDb = LoadTable<AssignmentDatabase>(AssignmentFilePath, "assignments");

            // 3. Seed default categories on first run (only if inventory file was empty).
            if (invDb.categories == null || invDb.categories.Count == 0)
            {
                invDb.categories = DefaultCategories.Get();
                SaveInventory();
            }
        }

        /// <summary>Generic per-table loader. Falls back to a fresh instance on miss / error.</summary>
        private T LoadTable<T>(string path, string label) where T : class, new()
        {
            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    T loaded = JsonUtility.FromJson<T>(json);
                    if (loaded == null) loaded = new T();
                    Debug.Log("[OEMS] Loaded " + label + " ← " + path);
                    return loaded;
                }
                Debug.Log("[OEMS] No " + label + " file — starting fresh.");
            }
            catch (Exception e)
            {
                Debug.LogError("[OEMS] Load " + label + " failed: " + e.Message);
            }
            return new T();
        }

        /// <summary>
        /// If a legacy oems_data.txt exists and the new per-table files don't, import it
        /// into the three new tables, then rename the legacy file so it isn't re-imported.
        /// </summary>
        private void TryMigrateLegacyFile()
        {
            try
            {
                if (!File.Exists(LegacyFilePath)) return;
                bool anyNewExists = File.Exists(EmployeeFilePath)
                                 || File.Exists(InventoryFilePath)
                                 || File.Exists(AssignmentFilePath);
                if (anyNewExists) return;   // new files take precedence; don't overwrite

                Debug.Log("[OEMS] Migrating legacy oems_data.txt → split files…");
                string json = File.ReadAllText(LegacyFilePath);
                var legacy = JsonUtility.FromJson<DatabaseWrapper>(json);
                if (legacy == null) { Debug.LogWarning("[OEMS] Legacy file empty/corrupt — skipping migration."); return; }

                empDb = new EmployeeDatabase
                {
                    employees       = legacy.employees       ?? new List<Employee>(),
                    employeeCounter = legacy.employeeCounter
                };
                invDb = new InventoryDatabase
                {
                    categories         = legacy.categories         ?? new List<InventoryCategory>(),
                    dispensableItems   = legacy.dispensableItems   ?? new List<DispensableItem>(),
                    indispensableItems = legacy.indispensableItems ?? new List<IndispensableItem>(),
                    assetCounter       = legacy.assetCounter,
                    batchCounter       = legacy.batchCounter
                };
                assignDb = new AssignmentDatabase
                {
                    assignments       = legacy.assignments       ?? new List<Assignment>(),
                    assignmentCounter = legacy.assignmentCounter
                };

                SaveData();
                File.Move(LegacyFilePath, LegacyFilePath + ".legacy");
                Debug.Log("[OEMS] Migration complete — legacy file renamed to oems_data.txt.legacy.");
            }
            catch (Exception e)
            {
                Debug.LogError("[OEMS] Legacy migration failed: " + e.Message);
            }
        }

        public void ResetAllData()
        {
            empDb    = new EmployeeDatabase();
            invDb    = new InventoryDatabase { categories = DefaultCategories.Get() };
            assignDb = new AssignmentDatabase();
            SaveData();
        }

        // ═════════════════════════════════════════════════════════════════════
        // CATEGORY OPERATIONS  (inventory file)
        // ═════════════════════════════════════════════════════════════════════
        public bool AddCategory(InventoryCategory cat)
        {
            if (cat == null || string.IsNullOrEmpty(cat.categoryName)) return false;
            if (FindCategory(cat.categoryName) != null) return false;
            invDb.categories.Add(cat);
            SaveInventory();
            return true;
        }

        public bool RemoveCategory(string categoryName)
        {
            // Block if any item is using this category
            if (invDb.dispensableItems.Exists(x => x.categoryName == categoryName)) return false;
            if (invDb.indispensableItems.Exists(x => x.categoryName == categoryName)) return false;
            int n = invDb.categories.RemoveAll(c => c.categoryName == categoryName);
            if (n > 0) { SaveInventory(); return true; }
            return false;
        }

        public InventoryCategory FindCategory(string categoryName)
        {
            return invDb.categories.Find(c => c.categoryName == categoryName);
        }

        // ═════════════════════════════════════════════════════════════════════
        // EMPLOYEE OPERATIONS  (employee file)
        // ═════════════════════════════════════════════════════════════════════
        public string GenerateEmployeeID()
        {
            string id = "EMP-" + empDb.employeeCounter.ToString("D4");
            empDb.employeeCounter++;
            return id;
        }

        public bool AddEmployee(Employee e)
        {
            if (e == null || string.IsNullOrEmpty(e.employeeID)) return false;
            if (FindEmployee(e.employeeID) != null) return false;
            empDb.employees.Add(e);
            SaveEmployees();
            return true;
        }

        /// <summary>Update mutable fields on an existing employee record.</summary>
        public bool UpdateEmployee(string employeeID, string firstName, string lastName,
                                   string email, string department)
        {
            var emp = FindEmployee(employeeID);
            if (emp == null) return false;
            emp.firstName  = firstName;
            emp.lastName   = lastName;
            emp.email      = email;
            emp.department = department;
            SaveEmployees();
            return true;
        }

        public bool RemoveEmployee(string employeeID)
        {
            // Block removal if any active assignments remain
            if (assignDb.assignments.Exists(a =>
                    a.employeeID == employeeID &&
                    a.assignmentStatus == AssignmentStatus.Assigned))
                return false;
            int n = empDb.employees.RemoveAll(x => x.employeeID == employeeID);
            if (n > 0) { SaveEmployees(); return true; }
            return false;
        }

        public Employee FindEmployee(string employeeID)
        {
            return empDb.employees.Find(x => x.employeeID == employeeID);
        }

        /// <summary>Simple RFC-5322-ish email validation.</summary>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return false;
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                                 RegexOptions.IgnoreCase);
        }

        // ═════════════════════════════════════════════════════════════════════
        // INVENTORY — ID GENERATION  (inventory file)
        // ═════════════════════════════════════════════════════════════════════
        public string GenerateAssetID()
        {
            string id = "ASSET-" + invDb.assetCounter.ToString("D4");
            invDb.assetCounter++;
            return id;
        }

        public string GenerateBatchID()
        {
            string id = "BATCH-" + invDb.batchCounter.ToString("D4");
            invDb.batchCounter++;
            return id;
        }

        // ═════════════════════════════════════════════════════════════════════
        // INVENTORY — ADD  (inventory file)
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Add a DISPENSABLE batch (pens, paper …).
        /// Creates ONE DispensableItem record with a unique BATCH-XXXX id.
        /// </summary>
        public DispensableItem AddDispensableBatch(string itemName, string catName,
                                                   int qty, string desc = "",
                                                   string batchRef = "")
        {
            if (string.IsNullOrEmpty(itemName) || qty <= 0) return null;
            var item = new DispensableItem(GenerateBatchID(), itemName, catName,
                                           qty, desc, batchRef);
            invDb.dispensableItems.Add(item);
            SaveInventory();
            return item;
        }

        /// <summary>
        /// Add INDISPENSABLE units (laptops, mice …).
        /// Creates N separate IndispensableItem records, each with its own ASSET-XXXX id.
        /// </summary>
        public List<IndispensableItem> AddIndispensableUnits(string itemName, string catName,
                                                              int unitCount, string desc = "",
                                                              string serialPrefix = "")
        {
            if (string.IsNullOrEmpty(itemName) || unitCount <= 0) return null;
            var created = new List<IndispensableItem>();
            for (int i = 0; i < unitCount; i++)
            {
                string sn = string.IsNullOrEmpty(serialPrefix)
                    ? ""
                    : serialPrefix + "-" + (i + 1).ToString("D2");
                var unit = new IndispensableItem(GenerateAssetID(), itemName, catName, desc, sn);
                invDb.indispensableItems.Add(unit);
                created.Add(unit);
            }
            SaveInventory();
            return created;
        }

        // ═════════════════════════════════════════════════════════════════════
        // INVENTORY — QUERY
        // ═════════════════════════════════════════════════════════════════════
        public InventoryItem FindItem(string itemID)
        {
            var d = invDb.dispensableItems.Find(x => x.itemID == itemID);
            if (d != null) return d;
            return invDb.indispensableItems.Find(x => x.itemID == itemID);
        }

        public List<InventoryItem> GetAllItems()
        {
            var all = new List<InventoryItem>();
            all.AddRange(invDb.dispensableItems);
            all.AddRange(invDb.indispensableItems);
            return all;
        }

        /// <summary>Items that can be assigned right now.</summary>
        public List<InventoryItem> GetAvailableItems()
        {
            var list = new List<InventoryItem>();
            foreach (var x in invDb.indispensableItems)
                if (x.itemStatus == ItemStatus.Good) list.Add(x);
            foreach (var x in invDb.dispensableItems)
                if (x.availableQuantity > 0) list.Add(x);
            return list;
        }

        /// <summary>Available indispensable units only.</summary>
        public List<IndispensableItem> GetAvailableUnits()
        {
            return invDb.indispensableItems.FindAll(x => x.itemStatus == ItemStatus.Good);
        }

        /// <summary>Dispensable batches that still have stock.</summary>
        public List<DispensableItem> GetAvailableBatches()
        {
            return invDb.dispensableItems.FindAll(x => x.availableQuantity > 0);
        }

        /// <summary>Filter all items by ItemStatus for the inventory status view.</summary>
        public List<InventoryItem> GetItemsByStatus(ItemStatus status)
        {
            var list = new List<InventoryItem>();
            foreach (var x in invDb.indispensableItems)
                if (x.itemStatus == status) list.Add(x);
            foreach (var x in invDb.dispensableItems)
            {
                if (status == ItemStatus.Good     && x.availableQuantity > 0) list.Add(x);
                if (status == ItemStatus.Damaged  && x.damagedQuantity   > 0) list.Add(x);
                if (status == ItemStatus.Consumed && x.consumedQuantity  > 0) list.Add(x);
            }
            return list;
        }

        // ═════════════════════════════════════════════════════════════════════
        // INVENTORY — DELETE  (inventory file)
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>Delete an item only when it has no active assignments.</summary>
        public bool DeleteItem(string itemID)
        {
            if (assignDb.assignments.Exists(a =>
                    a.itemID == itemID &&
                    a.assignmentStatus == AssignmentStatus.Assigned))
                return false;
            int n = invDb.dispensableItems.RemoveAll(x => x.itemID == itemID);
            if (n == 0)
                n = invDb.indispensableItems.RemoveAll(x => x.itemID == itemID);
            if (n > 0) { SaveInventory(); return true; }
            return false;
        }

        // ═════════════════════════════════════════════════════════════════════
        // ASSIGNMENT OPERATIONS  (assignment file + inventory file)
        // ═════════════════════════════════════════════════════════════════════
        public string GenerateAssignmentID()
        {
            string id = "ASN-" + assignDb.assignmentCounter.ToString("D4");
            assignDb.assignmentCounter++;
            return id;
        }

        /// <summary>
        /// Assign an item to an employee.
        ///   Indispensable unit: qty forced to 1; status → Assigned.
        ///   Dispensable batch:  deduct qty from available; mark consumed immediately.
        ///
        /// Writes to BOTH the inventory file (item quantities / status mutate)
        /// and the assignment file (new transaction recorded).
        /// </summary>
        public Assignment AssignItem(string employeeID, string itemID, int quantity)
        {
            var emp  = FindEmployee(employeeID);
            var item = FindItem(itemID);
            if (emp == null || item == null) return null;

            if (item is IndispensableItem)
            {
                var unit = (IndispensableItem)item;
                if (unit.itemStatus != ItemStatus.Good) return null;
                unit.itemStatus        = ItemStatus.Assigned;
                unit.availableQuantity = 0;
                quantity               = 1;
            }
            else
            {
                if (quantity <= 0 || item.availableQuantity < quantity) return null;
                item.availableQuantity -= quantity;
                item.consumedQuantity  += quantity;
            }

            var a = new Assignment(GenerateAssignmentID(), emp, item, quantity);
            assignDb.assignments.Add(a);

            // Inventory mutated → persist inventory; new assignment row → persist assignments.
            SaveInventory();
            SaveAssignments();
            return a;
        }

        // ═════════════════════════════════════════════════════════════════════
        // RETURN OPERATIONS  (assignment file + inventory file)
        // ═════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Process a return. assignmentStatus → Returned; itemCondition set.
        /// Only Assigned assignments can be returned.
        /// </summary>
        public bool ReturnItem(string assignmentID, ItemCondition condition, string remarks)
        {
            var a = assignDb.assignments.Find(x => x.assignmentID == assignmentID);
            if (a == null || a.assignmentStatus != AssignmentStatus.Assigned) return false;
            if (condition == ItemCondition.NA) return false;

            var item = FindItem(a.itemID);
            if (item == null) return false;

            if (item is IndispensableItem)
            {
                var unit = (IndispensableItem)item;
                switch (condition)
                {
                    case ItemCondition.Good:
                        unit.itemStatus        = ItemStatus.Good;
                        unit.availableQuantity = 1;
                        break;
                    case ItemCondition.Damaged:
                        unit.itemStatus      = ItemStatus.Damaged;
                        unit.damagedQuantity = 1;
                        break;
                    case ItemCondition.Consumed:
                        unit.itemStatus       = ItemStatus.Consumed;
                        unit.consumedQuantity = 1;
                        break;
                }
            }
            else // DispensableItem
            {
                switch (condition)
                {
                    case ItemCondition.Good:
                        item.availableQuantity += a.quantity;
                        item.consumedQuantity  -= a.quantity;
                        break;
                    case ItemCondition.Damaged:
                        item.damagedQuantity  += a.quantity;
                        item.consumedQuantity -= a.quantity;
                        break;
                    case ItemCondition.Consumed:
                        break; // already tracked as consumed on assign
                }
            }

            a.assignmentStatus = AssignmentStatus.Returned;
            a.itemCondition    = condition;
            a.returnedDate     = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            a.remarks          = remarks ?? "";

            SaveInventory();
            SaveAssignments();
            return true;
        }

        // ═════════════════════════════════════════════════════════════════════
        // ASSIGNMENT QUERIES
        // ═════════════════════════════════════════════════════════════════════
        public List<Assignment> GetActiveAssignmentsForEmployee(string employeeID)
        {
            return assignDb.assignments.FindAll(
                a => a.employeeID == employeeID &&
                     a.assignmentStatus == AssignmentStatus.Assigned);
        }

        public List<Assignment> GetAllActiveAssignments()
        {
            return assignDb.assignments.FindAll(
                a => a.assignmentStatus == AssignmentStatus.Assigned);
        }

        public List<Assignment> GetAssignmentHistoryForEmployee(string employeeID)
        {
            return assignDb.assignments.FindAll(a => a.employeeID == employeeID);
        }

        /// <summary>Items currently out with a specific employee (for the per-employee view).</summary>
        public List<Assignment> GetItemsByEmployee(string employeeID)
        {
            return assignDb.assignments.FindAll(
                a => a.employeeID == employeeID &&
                     a.assignmentStatus == AssignmentStatus.Assigned);
        }

        // ═════════════════════════════════════════════════════════════════════
        // DASHBOARD STATISTICS
        // ═════════════════════════════════════════════════════════════════════
        public int GetTotalEmployeeCount()  { return empDb.employees.Count; }
        public int GetTotalCategoryCount()  { return invDb.categories.Count; }
        public int GetTotalItemTypeCount()  { return invDb.dispensableItems.Count + invDb.indispensableItems.Count; }
        public int GetActiveAssignmentCount()
        {
            return assignDb.assignments.FindAll(
                a => a.assignmentStatus == AssignmentStatus.Assigned).Count;
        }

        public int GetTotalAvailableQuantity()
        {
            int t = 0;
            foreach (var x in invDb.indispensableItems)
                if (x.itemStatus == ItemStatus.Good) t++;
            foreach (var x in invDb.dispensableItems)
                t += x.availableQuantity;
            return t;
        }

        public int GetTotalAssignedCount()
        {
            int t = 0;
            foreach (var x in invDb.indispensableItems)
                if (x.itemStatus == ItemStatus.Assigned) t++;
            foreach (var a in assignDb.assignments)
                if (a.assignmentStatus == AssignmentStatus.Assigned && !a.isReturnable)
                    t += a.quantity;
            return t;
        }

        public int GetTotalDamagedCount()
        {
            int t = 0;
            foreach (var x in invDb.indispensableItems)
                if (x.itemStatus == ItemStatus.Damaged) t++;
            foreach (var x in invDb.dispensableItems)
                t += x.damagedQuantity;
            return t;
        }

        public int GetTotalConsumedCount()
        {
            int t = 0;
            foreach (var x in invDb.indispensableItems)
                if (x.itemStatus == ItemStatus.Consumed) t++;
            foreach (var x in invDb.dispensableItems)
                t += x.consumedQuantity;
            return t;
        }
    }
}
