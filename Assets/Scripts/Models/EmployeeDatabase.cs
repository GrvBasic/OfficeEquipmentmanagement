using System;
using System.Collections.Generic;

namespace OEMS.Models
{
    /// <summary>
    /// Serializable container for the EMPLOYEE table.
    /// Persisted to its own JSON file (oems_employees.json) — mirroring a
    /// real database where Employee is its own table separate from Inventory
    /// and Assignment.
    ///
    /// Holds: all Employee records + the EMP-XXXX id counter.
    /// </summary>
    [Serializable]
    public class EmployeeDatabase
    {
        public List<Employee> employees       = new List<Employee>();
        public int            employeeCounter = 1;   // → EMP-XXXX
    }
}
