using System;
using FluentNHibernate.Mapping;

namespace Aurora.Framework
{
    public class AuroraInventoryFolderMap : ClassMap<AuroraInventoryFolder>
    {
        public AuroraInventoryFolderMap()
        {
            Id(x => x.ID);
            Map(x => x.PreferredAssetType);
            Map(x => x.FolderID);
            References(x => x.ParentFolder);
            Map(x => x.Name);
            Map(x => x.Owner);
        }
    }

    public class AuroraInventoryFolder
    {
        public virtual int ID { get; set; }
        public virtual int PreferredAssetType { get; set; }
        public virtual string FolderID { get; set; }
        public virtual AuroraInventoryFolder ParentFolder { get; set; }
        public virtual string Name { get; set; }
        public virtual string Owner { get; set; }
    }
}