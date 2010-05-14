using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;
using System.Threading;
using Aurora.DataManager;
using Mono.Data.SqliteClient;
using Aurora.Framework;

namespace Aurora.DataManager.SQLite
{
    public class SQLiteRegion : SQLiteLoader, IRegionData
    {
    }
}
