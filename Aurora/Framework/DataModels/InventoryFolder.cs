using System;
using FluentNHibernate.Mapping;

namespace Aurora.Framework
{
    public class InventoryFolderMap : ClassMap<InventoryFolder>
    {
        public InventoryFolderMap()
        {
            Id(x => x.Id);
            Map(x => x.PreferredAssetType);
            Map(x => x.FolderId);
            References(x => x.ParentFolder);
            Map(x => x.Name);
            Map(x => x.Owner);
        }
    }

    public class InventoryFolder
    {
        public virtual int Id { get; set; }
        public virtual int PreferredAssetType { get; set; }
        public virtual string FolderId { get; set; }
        public virtual InventoryFolder ParentFolder { get; set; }
        public virtual string Name { get; set; }
        public virtual string Owner { get; set; }
    }
}