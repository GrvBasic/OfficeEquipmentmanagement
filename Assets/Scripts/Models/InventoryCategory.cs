using System;
using System.Collections.Generic;

namespace OEMS.Models
{
    /// <summary>
    /// Represents a named category for inventory items.
    /// Each category defines whether its items must be returned (isReturnable).
    /// Corresponds to the InventoryCategory entity in the ER diagram.
    /// </summary>
    [Serializable]
    public class InventoryCategory
    {
        public string categoryName;     // PK — unique name, e.g. "Electronics", "Stationery"
        public bool   isReturnable;     // true = Indispensable, false = Dispensable
        public string description;
        public string dateAdded;

        public InventoryCategory() { }

        public InventoryCategory(string name, bool returnable, string desc = "")
        {
            categoryName  = name;
            isReturnable  = returnable;
            description   = desc;
            dateAdded     = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public override string ToString()
        {
            return string.Format("{0} [{1}]", categoryName, isReturnable ? "Indispensable" : "Dispensable");
        }
    }

    /// <summary>
    /// Seed data — default categories loaded when no data file exists yet.
    /// Admin can add more at runtime.
    /// </summary>
    public static class DefaultCategories
    {
        public static List<InventoryCategory> Get()
        {
            return new List<InventoryCategory>
            {
                new InventoryCategory("Electronics",  true,  "Returnable devices: laptops, phones, mice"),
                new InventoryCategory("Furniture",    true,  "Returnable items: chairs, desks"),
                new InventoryCategory("Stationery",   false, "Consumable supplies: pens, notebooks, paper"),
                new InventoryCategory("Accessories",  false, "Consumable accessories: cables, adapters"),
            };
        }
    }
}
