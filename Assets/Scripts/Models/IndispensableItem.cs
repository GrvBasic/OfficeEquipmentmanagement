using System;

namespace OEMS.Models
{

    [Serializable]
    public class IndispensableItem : InventoryItem
    {
       
        public string serialNumber;

        public IndispensableItem() : base() { }

      
        public IndispensableItem(string id, string name, string catName,
                                 string desc = "", string serial = "")
            : base(id, name, catName, 1, desc)   
        {
            serialNumber = serial;
        }

        public override bool IsReturnable()    { return true; }
        public override string GetCategory()   { return "Indispensable"; }
    }
}
