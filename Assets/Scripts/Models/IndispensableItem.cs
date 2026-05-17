using System;

namespace OEMS.Models
{
    /// <summary>
    /// Represents a single PHYSICAL UNIT of a returnable asset (laptop, mouse, phone).
    /// totalQuantity is always 1 — each unit has its own ASSET-XXXX id so we know
    /// exactly which device is with which employee at any time.
    /// Demonstrates INHERITANCE.
    /// </summary>
    [Serializable]
    public class IndispensableItem : InventoryItem
    {
        /// <summary>Optional serial number or admin-assigned tag (e.g. "SN-XPS-2024").</summary>
        public string serialNumber;

        public IndispensableItem() : base() { }

        /// <param name="id">Auto-generated ASSET-XXXX id.</param>
        /// <param name="name">Item name, e.g. "Dell Laptop XPS 15".</param>
        /// <param name="catName">Category FK, e.g. "Electronics".</param>
        /// <param name="desc">Optional description.</param>
        /// <param name="serial">Optional serial / asset tag.</param>
        public IndispensableItem(string id, string name, string catName,
                                 string desc = "", string serial = "")
            : base(id, name, catName, 1, desc)   // always qty = 1
        {
            serialNumber = serial;
        }

        public override bool IsReturnable()    { return true; }
        public override string GetCategory()   { return "Indispensable"; }
    }
}
