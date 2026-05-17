using System;
using System.Collections.Generic;

namespace OEMS.Models
{
    /// <summary>
    /// Serializable container for the INVENTORY tables.
    /// Persisted to its own JSON file (oems_inventory.json).
    ///
    /// Per the ER diagram, InventoryCategory and Inventory are separate entities;
    /// they are bundled into one physical file here because they're tightly coupled
    /// (BelongsTo relationship) and almost always read/written together. This is the
    /// same pattern many real systems use when grouping closely-related tables.
    ///
    /// Holds:
    ///   - categories            (InventoryCategory entity)
    ///   - dispensableItems      (Inventory entity, one record per BATCH)
    ///   - indispensableItems    (Inventory entity, one record per UNIT)
    ///   - ID counters for ASSET-XXXX and BATCH-XXXX
    /// </summary>
    [Serializable]
    public class InventoryDatabase
    {
        public List<InventoryCategory> categories         = new List<InventoryCategory>();
        public List<DispensableItem>   dispensableItems   = new List<DispensableItem>();
        public List<IndispensableItem> indispensableItems = new List<IndispensableItem>();

        public int assetCounter = 1;   // → ASSET-XXXX
        public int batchCounter = 1;   // → BATCH-XXXX
    }
}
