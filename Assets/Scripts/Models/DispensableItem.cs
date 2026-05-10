using System;

namespace OEMS.Models
{
    /// <summary>
    /// Items that are consumed and NOT returned (pen, paper, notebook).
    /// Demonstrates INHERITANCE.
    /// </summary>
    [Serializable]
    public class DispensableItem : InventoryItem
    {
        public DispensableItem() : base() { }

        public DispensableItem(string id, string name, int qty, string desc)
            : base(id, name, qty, desc)
        {
            category = "Dispensable";
        }

        public override bool IsReturnable() { return false; }
        public override string GetCategory() { return "Dispensable"; }
    }
}
