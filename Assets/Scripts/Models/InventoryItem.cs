using System;

namespace OEMS.Models
{
    /// <summary>
    /// Abstract base class for all inventory items.
    /// Demonstrates ABSTRACTION and INHERITANCE (OOP concepts).
    /// </summary>
    [Serializable]
    public abstract class InventoryItem
    {
        public string itemID;
        public string itemName;
        public int totalQuantity;
        public int availableQuantity;
        public int damagedQuantity;
        public int consumedQuantity;
        public string category;     // "Dispensable" or "Indispensable"
        public string description;
        public string dateAdded;

        protected InventoryItem() { }

        protected InventoryItem(string id, string name, int qty, string desc)
        {
            itemID = id;
            itemName = name;
            totalQuantity = qty;
            availableQuantity = qty;
            damagedQuantity = 0;
            consumedQuantity = 0;
            description = desc;
            dateAdded = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// Whether the item has to be returned (overridden by child classes).
        /// </summary>
        public abstract bool IsReturnable();

        /// <summary>
        /// Returns the category label (overridden by child classes).
        /// </summary>
        public abstract string GetCategory();

        public int AssignedQuantity
        {
            get { return totalQuantity - availableQuantity - damagedQuantity - consumedQuantity; }
        }
    }
}
