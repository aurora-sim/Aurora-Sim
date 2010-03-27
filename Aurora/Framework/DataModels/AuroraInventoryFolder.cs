using System;
using FluentNHibernate.Mapping;

namespace Aurora.Framework
{
    public class AuroraInventoryFolderMap : ClassMap<AuroraInventoryFolder>
    {
        public AuroraInventoryFolderMap()
        {
            Id(x => x.Id);
            Map(x => x.PreferredAssetType);
            Map(x => x.FolderId);
            References(x => x.ParentFolder);
            Map(x => x.Name);
            Map(x => x.Owner);
        }
    }

    public class AuroraInventoryFolder
    {
        public virtual int Id { get; set; }
        public virtual int PreferredAssetType { get; set; }
        public virtual string FolderId { get; set; }
        public virtual AuroraInventoryFolder ParentFolder { get; set; }
        public virtual string Name { get; set; }
        public virtual string Owner { get; set; }
    }
}