using System;

namespace OEMS.Models
{
    /// <summary>
    /// Items that MUST be returned (laptop, mouse, phone).
    /// Demonstrates INHERITANCE.
    /// </summary>
    [Serializable]
    public class IndispensableItem : InventoryItem
    {
        public IndispensableItem() : base() { }

        public IndispensableItem(string id, string name, int qty, string desc)
            : base(id, name, qty, desc)
        {
            category = "Indispensable";
        }

        public override bool IsReturnable() { return true; }
        public override string GetCategory() { return "Indispensable"; }
    }
}
