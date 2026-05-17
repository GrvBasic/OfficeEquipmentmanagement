using System;

namespace OEMS.Models
{
    /// <summary>
    /// Item-level status — tracks the current physical state of a unit or batch.
    /// Matches Inventory.status in the ER diagram.
    /// </summary>
    public enum ItemStatus
    {
        Good,       // Available / in usable condition
        Assigned,   // Currently held by an employee (indispensable units only)
        Damaged,    // Returned in damaged condition / written off
        Consumed    // Used up (dispensable) or permanently removed
    }

    /// <summary>
    /// Abstract base class for all inventory items.
    /// Demonstrates ABSTRACTION and INHERITANCE (OOP concepts).
    ///
    /// Two concrete subclasses:
    ///   - IndispensableItem : one record per PHYSICAL UNIT (qty always 1).
    ///     Each laptop, mouse, phone gets its own ASSET-XXXX id.
    ///   - DispensableItem   : one record per BATCH (qty = stock count).
    ///     Each stock-entry of pens/paper gets its own BATCH-XXXX id.
    /// </summary>
    [Serializable]
    public abstract class InventoryItem
    {
        public string     itemID;           // ASSET-XXXX or BATCH-XXXX
        public string     itemName;         // e.g. "Dell Laptop XPS 15"
        public string     categoryName;     // FK → InventoryCategory.categoryName
        public ItemStatus itemStatus;       // Good / Assigned / Damaged / Consumed
        public string     description;
        public string     dateAdded;

        // Quantity fields — only meaningful for DispensableItem batches.
        // IndispensableItem always has totalQuantity = availableQuantity = 1.
        public int totalQuantity;
        public int availableQuantity;
        public int damagedQuantity;
        public int consumedQuantity;

        protected InventoryItem() { }

        protected InventoryItem(string id, string name, string catName, int qty, string desc)
        {
            itemID            = id;
            itemName          = name;
            categoryName      = catName;
            totalQuantity     = qty;
            availableQuantity = qty;
            damagedQuantity   = 0;
            consumedQuantity  = 0;
            itemStatus        = ItemStatus.Good;
            description       = desc;
            dateAdded         = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>Whether this item must be returned after assignment.</summary>
        public abstract bool IsReturnable();

        /// <summary>Human-readable category label.</summary>
        public abstract string GetCategory();

        /// <summary>
        /// Units currently out with employees.
        /// For indispensable: 0 or 1. For dispensable: derived from qty counters.
        /// </summary>
        public int AssignedQuantity
        {
            get { return totalQuantity - availableQuantity - damagedQuantity - consumedQuantity; }
        }
    }
}
