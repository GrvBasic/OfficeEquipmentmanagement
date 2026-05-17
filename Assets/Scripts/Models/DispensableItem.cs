using System;

namespace OEMS.Models
{
    /// <summary>
    /// Represents a BATCH of consumable supplies (pens, paper, notebooks).
    /// Each stock-entry gets its own BATCH-XXXX id so different deliveries are
    /// tracked separately. quantity fields track how many units remain in the batch.
    /// Demonstrates INHERITANCE.
    /// </summary>
    [Serializable]
    public class DispensableItem : InventoryItem
    {
        /// <summary>Optional supplier / purchase-order reference.</summary>
        public string batchReference;

        public DispensableItem() : base() { }

        /// <param name="id">Auto-generated BATCH-XXXX id.</param>
        /// <param name="name">Item name, e.g. "Blue Ballpoint Pen".</param>
        /// <param name="catName">Category FK, e.g. "Stationery".</param>
        /// <param name="qty">Number of units in this batch.</param>
        /// <param name="desc">Optional description.</param>
        /// <param name="batchRef">Optional purchase order / supplier ref.</param>
        public DispensableItem(string id, string name, string catName,
                               int qty, string desc = "", string batchRef = "")
            : base(id, name, catName, qty, desc)
        {
            batchReference = batchRef;
        }

        public override bool IsReturnable()    { return false; }
        public override string GetCategory()   { return "Dispensable"; }
    }
}
