using System;

namespace OEMS.Models
{
    public enum AssignmentStatus
    {
        Active,         // currently held by employee
        ReturnedGood,   // returned in good condition
        ReturnedDamaged,// returned damaged
        Consumed        // dispensable / unrecoverable
    }

    /// <summary>
    /// Records every transaction of items being given to or returned by an employee.
    /// Bridge entity between Employee and Inventory (per ER diagram in synopsis).
    /// </summary>
    [Serializable]
    public class Assignment
    {
        public string assignmentID;
        public string employeeID;
        public string employeeName;
        public string itemID;
        public string itemName;
        public string category;             // Dispensable / Indispensable
        public int quantity;
        public string assignedDate;
        public string returnedDate;
        public AssignmentStatus status;
        public string notes;

        public Assignment() { }

        public Assignment(string aid, Employee emp, InventoryItem item, int qty)
        {
            assignmentID  = aid;
            employeeID    = emp.employeeID;
            employeeName  = emp.name;
            itemID        = item.itemID;
            itemName      = item.itemName;
            category      = item.GetCategory();
            quantity      = qty;
            assignedDate  = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            returnedDate  = "";
            status        = item.IsReturnable() ? AssignmentStatus.Active : AssignmentStatus.Consumed;
            notes         = "";
        }
    }
}
