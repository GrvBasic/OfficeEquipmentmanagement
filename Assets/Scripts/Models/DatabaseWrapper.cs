using System;
using System.Collections.Generic;

namespace OEMS.Models
{
    /// <summary>
    /// LEGACY — single-file container kept ONLY for one-time migration from the
    /// old monolithic oems_data.txt format. New code persists each entity to its
    /// own file via EmployeeDatabase / InventoryDatabase / AssignmentDatabase
    /// (database-style separation).
    ///
    /// DataManager checks for an old oems_data.txt on startup and, if found,
    /// imports it into the three new tables and renames the old file to
    /// oems_data.txt.legacy so it isn't re-imported.
    /// </summary>
    [Serializable]
    public class DatabaseWrapper
    {
        public List<InventoryCategory>  categories        = new List<InventoryCategory>();
        public List<Employee>           employees         = new List<Employee>();
        public List<DispensableItem>    dispensableItems  = new List<DispensableItem>();
        public List<IndispensableItem>  indispensableItems= new List<IndispensableItem>();
        public List<Assignment>         assignments       = new List<Assignment>();

        public int employeeCounter   = 1;
        public int assetCounter      = 1;
        public int batchCounter      = 1;
        public int assignmentCounter = 1;
    }
}
