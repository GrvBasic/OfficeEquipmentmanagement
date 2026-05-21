using System;

namespace OEMS.Models
{

    public enum ItemStatus
    {
        Good,       
        Assigned,   
        Damaged,   
        Consumed    
    }


    [Serializable]
    public abstract class InventoryItem
    {
        public string     itemID;          
        public string     itemName;        
        public string     categoryName;     
        public ItemStatus itemStatus;      
        public string     description;
        public string     dateAdded;


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


        public abstract bool IsReturnable();

  
        public abstract string GetCategory();

   
        public int AssignedQuantity
        {
            get { return totalQuantity - availableQuantity - damagedQuantity - consumedQuantity; }
        }
    }
}
