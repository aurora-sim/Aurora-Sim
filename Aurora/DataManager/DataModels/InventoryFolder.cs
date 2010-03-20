using System;
using FluentNHibernate.Mapping;

namespace Aurora.DataManager.DataModels
{
    public class InventoryFolderMap : ClassMap<InventoryFolder>
    {
        public InventoryFolderMap()
        {
            Id(x => x.Id);
            References(x => x.Parent);
        }
    }

    public class InventoryFolder
    {
        public virtual InventoryObjectType PreferredAssetType { get; set; }
        public virtual int Id { get; set; }
        public virtual InventoryFolder Parent { get; set; }
        public virtual string Name { get; set; }
    }
}