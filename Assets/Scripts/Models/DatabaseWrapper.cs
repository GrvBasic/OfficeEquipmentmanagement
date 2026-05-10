using System;
using System.Collections.Generic;

namespace OEMS.Models
{
    /// <summary>
    /// Serializable container for persisting all data to a single JSON text file.
    /// Unity's JsonUtility can't serialize abstract types, so we keep
    /// dispensable / indispensable lists separate.
    /// </summary>
    [Serializable]
    public class DatabaseWrapper
    {
        public List<Employee> employees = new List<Employee>();
        public List<DispensableItem> dispensableItems = new List<DispensableItem>();
        public List<IndispensableItem> indispensableItems = new List<IndispensableItem>();
        public List<Assignment> assignments = new List<Assignment>();

        public int employeeCounter = 1;
        public int itemCounter = 1;
        public int assignmentCounter = 1;
    }
}
