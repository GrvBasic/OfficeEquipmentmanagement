#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Diagnostics;
using OEMS.Core;
using OEMS.Models;

namespace OEMS.Editor
{
    /// <summary>
    /// Convenience editor menu items.
    /// </summary>
    public static class EditorTools
    {
        [MenuItem("OEMS/Tools/Open Persistent Data Folder", priority = 50)]
        public static void OpenPersistentDataFolder()
        {
            string path = Application.persistentDataPath;
            UnityEngine.Debug.Log("Persistent data path: " + path);
#if UNITY_EDITOR_WIN
            Process.Start("explorer.exe", path.Replace("/", "\\"));
#elif UNITY_EDITOR_OSX
            Process.Start("open", path);
#else
            Process.Start("xdg-open", path);
#endif
        }

        [MenuItem("OEMS/Tools/Reset All Data", priority = 51)]
        public static void ResetAllData()
        {
            if (!EditorUtility.DisplayDialog("Reset Data",
                "This will delete all employees, inventory and assignments saved on this device. Continue?",
                "Reset", "Cancel")) return;

            string path = System.IO.Path.Combine(Application.persistentDataPath, "oems_data.txt");
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
                UnityEngine.Debug.Log("[OEMS] Data file deleted.");
            }
            else
            {
                UnityEngine.Debug.Log("[OEMS] No data file to delete.");
            }
        }

        [MenuItem("OEMS/Tools/Seed Sample Data (Play Mode)", priority = 52)]
        public static void SeedSampleData()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Play Mode Required",
                    "Enter Play Mode first, then run this command.", "OK");
                return;
            }

            var dm = DataManager.Instance;
            if (dm == null) { UnityEngine.Debug.LogError("DataManager not found."); return; }

            // Sample employees
            dm.AddEmployee(new Employee(dm.GenerateEmployeeID(), "Aarav Sharma",   "Engineering", "9810000001"));
            dm.AddEmployee(new Employee(dm.GenerateEmployeeID(), "Priya Patel",    "HR",          "9810000002"));
            dm.AddEmployee(new Employee(dm.GenerateEmployeeID(), "Rohit Kumar",    "Finance",     "9810000003"));

            // Sample inventory
            dm.AddInventoryItem(new IndispensableItem(dm.GenerateItemID(), "Laptop",        10, "Dell Latitude"));
            dm.AddInventoryItem(new IndispensableItem(dm.GenerateItemID(), "Wireless Mouse", 25, "Logitech"));
            dm.AddInventoryItem(new IndispensableItem(dm.GenerateItemID(), "Office Phone",   8,  "VOIP handset"));
            dm.AddInventoryItem(new DispensableItem  (dm.GenerateItemID(), "Pen",            100, "Blue ballpoint"));
            dm.AddInventoryItem(new DispensableItem  (dm.GenerateItemID(), "Notebook",       50, "A4 ruled"));
            dm.AddInventoryItem(new DispensableItem  (dm.GenerateItemID(), "Printer Paper",  30, "A4 ream"));

            UnityEngine.Debug.Log("[OEMS] Sample data seeded.");
        }

        [MenuItem("OEMS/Help/About", priority = 100)]
        public static void About()
        {
            EditorUtility.DisplayDialog("Office Equipment Management System",
                "BCSP-064 IGNOU BCA Project\n" +
                "Built with Unity Engine + C#\n" +
                "Offline file-based local storage\n\n" +
                "Menu commands:\n" +
                "• OEMS → Setup → Build Full Scene\n" +
                "• OEMS → Tools → Open Persistent Data Folder\n" +
                "• OEMS → Tools → Reset All Data\n" +
                "• OEMS → Tools → Seed Sample Data (Play Mode)",
                "OK");
        }
    }
}
#endif
