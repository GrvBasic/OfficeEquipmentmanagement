using System;

namespace OEMS.Models
{
    /// <summary>
    /// Lifecycle state of an assignment transaction.
    /// Matches Assignment.assignmentStatus in the ER diagram (only 2 values).
    /// </summary>
    public enum AssignmentStatus
    {
        Assigned,   // Item is currently with the employee
        Returned    // Item has been returned (check itemCondition for details)
    }

    /// <summary>
    /// Physical condition of the item at the time it was returned.
    /// Matches Assignment.itemCondition in the ER diagram.
    /// NA = not yet returned.
    /// </summary>
    public enum ItemCondition
    {
        NA,         // Not yet returned — item still with employee
        Good,       // Returned in working condition → goes back to available stock
        Damaged,    // Returned damaged → moved to damaged count, not available
        Consumed    // Used up (dispensable) or permanently lost → removed from stock
    }


    [Serializable]
    public class Assignment
    {
        public string           assignmentID;
        public string           employeeID;
        public string           employeeFullName;    
        public string           itemID;             
        public string           itemName;           
        public string           categoryName;       
        public bool             isReturnable;       
        public int              quantity;           
        public string           assignedDate;
        public string           returnedDate;        
        public AssignmentStatus assignmentStatus;
        public ItemCondition    itemCondition;      
        public string           remarks;

        public Assignment() { }

        public Assignment(string aid, Employee emp, InventoryItem item, int qty)
        {
            assignmentID      = aid;
            employeeID        = emp.employeeID;
            employeeFullName  = emp.FullName;
            itemID            = item.itemID;
            itemName          = item.itemName;
            categoryName      = item.categoryName;
            isReturnable      = item.IsReturnable();
            quantity          = qty;
            assignedDate      = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            returnedDate      = "";
            assignmentStatus  = AssignmentStatus.Assigned;
            itemCondition     = ItemCondition.NA;
            remarks           = "";
        }
    }
}
