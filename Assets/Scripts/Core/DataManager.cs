using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using OEMS.Models;

namespace OEMS.Core
{
    
    public class DataManager : MonoBehaviour
    {
        public static DataManager Instance { get; private set; }

        
        private const string EMPLOYEE_FILE_NAME    = "oems_employees.json";
        private const string INVENTORY_FILE_NAME   = "oems_inventory.json";
        private const string ASSIGNMENT_FILE_NAME  = "oems_assignments.json";
        private const string LEGACY_FILE_NAME      = "oems_data.txt";

        private string EmployeeFilePath   { get { return Path.Combine(Application.persistentDataPath, EMPLOYEE_FILE_NAME);   } }
        private string InventoryFilePath  { get { return Path.Combine(Application.persistentDataPath, INVENTORY_FILE_NAME);  } }
        private string AssignmentFilePath { get { return Path.Combine(Application.persistentDataPath, ASSIGNMENT_FILE_NAME); } }
        private string LegacyFilePath     { get { return Path.Combine(Application.persistentDataPath, LEGACY_FILE_NAME);     } }

  
        private EmployeeDatabase   empDb    = new EmployeeDatabase();
        private InventoryDatabase  invDb    = new InventoryDatabase();
        private AssignmentDatabase assignDb = new AssignmentDatabase();

      
        public List<InventoryCategory>  Categories         { get { return invDb.categories;         } }
        public List<Employee>           Employees          { get { return empDb.employees;          } }
        public List<DispensableItem>    DispensableItems   { get { return invDb.dispensableItems;   } }
        public List<IndispensableItem>  IndispensableItems { get { return invDb.indispensableItems; } }
        public List<Assignment>         Assignments        { get { return assignDb.assignments;     } }

       
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadData();
        }

 
        private void WriteJsonData(string path, string json)
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
                WriteJsonData(EmployeeFilePath, JsonUtility.ToJson(empDb, true));
                Debug.Log(" Saved employees → " + EmployeeFilePath);
            }
            catch (Exception e) { Debug.LogError("SaveEmployees failed: " + e.Message); }
        }

        public void SaveInventory()
        {
            try
            {
                WriteJsonData(InventoryFilePath, JsonUtility.ToJson(invDb, true));
                Debug.Log("Saved inventory → " + InventoryFilePath);
            }
            catch (Exception e) { Debug.LogError(" SaveInventory failed: " + e.Message); }
        }

        public void SaveAssignments()
        {
            try
            {
                WriteJsonData(AssignmentFilePath, JsonUtility.ToJson(assignDb, true));
                Debug.Log(" Saved assignments → " + AssignmentFilePath);
            }
            catch (Exception e) { Debug.LogError(" SaveAssignments failed: " + e.Message); }
        }

        /// Save every table.
      
        public void SaveData()
        {
            SaveEmployees();
            SaveInventory();
            SaveAssignments();
        }

        public void LoadData()
        {
      

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

        /// Generic per-table loader. Falls back to a fresh instance on miss / error.
        private T LoadTable<T>(string path, string label) where T : class, new()
        {
            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    T loaded = JsonUtility.FromJson<T>(json);
                    if (loaded == null) loaded = new T();
                    Debug.Log(" Loaded " + label + " ← " + path);
                    return loaded;
                }
                Debug.Log(" No " + label + " file — starting fresh.");
            }
            catch (Exception e)
            {
                Debug.LogError(" Load " + label + " failed: " + e.Message);
            }
            return new T();
        }

     

        public void ResetAllData()
        {
            empDb    = new EmployeeDatabase();
            invDb    = new InventoryDatabase { categories = DefaultCategories.Get() };
            assignDb = new AssignmentDatabase();
            SaveData();
        }


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
            RegisterDepartment(e.department);   // remember the department for next time
            SaveEmployees();
            return true;
        }

        // ── DEPARTMENT REGISTRY (employee file) ───────────────────────────────

        /// <summary>
        /// All known department names, sorted alphabetically. Includes any used by
        /// existing employees plus any explicitly added through the registry.
        /// </summary>
        public List<string> GetDepartments()
        {
            EnsureDepartmentsBackfilled();
            var list = new List<string>(empDb.departments);
            list.Sort(System.StringComparer.OrdinalIgnoreCase);
            return list;
        }

        /// <summary>
        /// Add a department name to the registry if it isn't already present
        /// (case-insensitive). Persists immediately. Returns true if newly added.
        /// </summary>
        public bool AddDepartment(string name)
        {
            bool added = RegisterDepartment(name);
            if (added) SaveEmployees();
            return added;
        }

        /// <summary>In-memory add without saving (callers decide when to persist).</summary>
        private bool RegisterDepartment(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            name = name.Trim();
            if (empDb.departments == null) empDb.departments = new List<string>();
            if (empDb.departments.Exists(d => string.Equals(d, name,
                    System.StringComparison.OrdinalIgnoreCase)))
                return false;
            empDb.departments.Add(name);
            return true;
        }

        /// <summary>Make sure every department used by an existing employee is in the registry.</summary>
        private void EnsureDepartmentsBackfilled()
        {
            if (empDb.departments == null) empDb.departments = new List<string>();
            bool changed = false;
            foreach (var emp in empDb.employees)
                if (RegisterDepartment(emp.department)) changed = true;
            if (changed) SaveEmployees();
        }

       
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

        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return false;
            return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
                                 RegexOptions.IgnoreCase);
        }


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


        public List<InventoryItem> GetAvailableItems()
        {
            var list = new List<InventoryItem>();
            foreach (var x in invDb.indispensableItems)
                if (x.itemStatus == ItemStatus.Good) list.Add(x);
            foreach (var x in invDb.dispensableItems)
                if (x.availableQuantity > 0) list.Add(x);
            return list;
        }


        public List<IndispensableItem> GetAvailableUnits()
        {
            return invDb.indispensableItems.FindAll(x => x.itemStatus == ItemStatus.Good);
        }


        public List<DispensableItem> GetAvailableBatches()
        {
            return invDb.dispensableItems.FindAll(x => x.availableQuantity > 0);
        }


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

    
        public string GenerateAssignmentID()
        {
            string id = "ASN-" + assignDb.assignmentCounter.ToString("D4");
            assignDb.assignmentCounter++;
            return id;
        }

 
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

            
            SaveInventory();
            SaveAssignments();
            return a;
        }

    
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


        public List<Assignment> GetItemsByEmployee(string employeeID)
        {
            return assignDb.assignments.FindAll(
                a => a.employeeID == employeeID &&
                     a.assignmentStatus == AssignmentStatus.Assigned);
        }


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
