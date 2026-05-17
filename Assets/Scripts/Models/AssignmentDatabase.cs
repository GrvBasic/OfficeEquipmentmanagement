using System;
using System.Collections.Generic;

namespace OEMS.Models
{
    /// <summary>
    /// Serializable container for the ASSIGNMENT table.
    /// Persisted to its own JSON file (oems_assignments.json).
    ///
    /// Assignment is the bridge / linking entity between Employee and Inventory.
    /// It is kept in a separate file (separate "table") to keep it independently
    /// loadable and to mirror a real relational schema.
    ///
    /// Holds: all Assignment records + the ASN-XXXX id counter.
    /// </summary>
    [Serializable]
    public class AssignmentDatabase
    {
        public List<Assignment> assignments       = new List<Assignment>();
        public int              assignmentCounter = 1;   // → ASN-XXXX
    }
}
