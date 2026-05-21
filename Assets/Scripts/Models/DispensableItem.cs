using System;

namespace OEMS.Models
{
    
    [Serializable]
    public class DispensableItem : InventoryItem
    {
      
        public string batchReference;

        public DispensableItem() : base() { }

       
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
