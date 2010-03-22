using OpenMetaverse;

namespace Aurora.Framework
{
    public class InventoryObject
    {
        public virtual UUID UUID { get; set; }
        public virtual InventoryType Type { get; set;}
        public virtual bool Active { get; set; }
        public virtual string Name { get; set; }

    }
}