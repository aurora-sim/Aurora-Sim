using System;
using System.Collections.Generic;
using System.Text;
using Aurora.Framework;
using Aurora.DataManager;

namespace Aurora.Modules
{
    public class GenericData: IGenericData
    {
        #region IGenericData Members

        public bool Update(string table, string[] setValues, string[] setRows, string[] keyRows, string[] keyValues)
        {
            if (Aurora.DataManager.DataManager.DefaultGenericPlugin != null)
                Aurora.DataManager.DataManager.DefaultGenericPlugin.Update(table, setValues, setRows, keyRows, keyValues);
            else
            {
                foreach (IGenericData plugin in Aurora.DataManager.DataManager.AllGenericPlugins)
                {
                    bool success = plugin.Update(table, setValues, setRows, keyRows, keyValues);
                    if (success)
                        return true;
                }
            }
            return false;
        }

        public List<string> Query(string keyRow, string keyValue, string table, string wantedValue)
        {
            if (Aurora.DataManager.DataManager.DefaultGenericPlugin != null)
                return Aurora.DataManager.DataManager.DefaultGenericPlugin.Query(keyRow, keyValue, table, wantedValue);
            else
            {
                foreach (IGenericData plugin in Aurora.DataManager.DataManager.AllGenericPlugins)
                {
                    List<string> success = plugin.Query(keyRow, keyValue, table, wantedValue);
                    if (success.Count != 0)
                        if(success[0] != "")
                            return success;
                }
            }
            return new List<string>();
        }

        public List<string> Query(string[] keyRow, string[] keyValue, string table, string wantedValue)
        {
            if (Aurora.DataManager.DataManager.DefaultGenericPlugin != null)
                return Aurora.DataManager.DataManager.DefaultGenericPlugin.Query(keyRow, keyValue, table, wantedValue);
            else
            {
                foreach (IGenericData plugin in Aurora.DataManager.DataManager.AllGenericPlugins)
                {
                    List<string> success = plugin.Query(keyRow, keyValue, table, wantedValue);
                    if (success.Count != 0)
                        if(success[0] != "")
                            return success;
                }
            }
            return new List<string>();
        }

        public bool Insert(string table, string[] values)
        {
            if (Aurora.DataManager.DataManager.DefaultGenericPlugin != null)
                Aurora.DataManager.DataManager.DefaultGenericPlugin.Insert(table, values);
            else
            {
                foreach (IGenericData plugin in Aurora.DataManager.DataManager.AllGenericPlugins)
                {
                    bool success = plugin.Insert(table, values);
                    if (success)
                        return true;
                }
            }
            return false;
        }

        public bool Delete(string table, string[] keys, string[] values)
        {
            if (Aurora.DataManager.DataManager.DefaultGenericPlugin != null)
                Aurora.DataManager.DataManager.DefaultGenericPlugin.Delete(table, keys, values);
            else
            {
                foreach (IGenericData plugin in Aurora.DataManager.DataManager.AllGenericPlugins)
                {
                    bool success = plugin.Delete(table, keys, values);
                    if (success)
                        return true;
                }
            }
            return false;
        }

        public bool Insert(string table, string[] values, string updateKey, string updateValue)
        {
            if (Aurora.DataManager.DataManager.DefaultGenericPlugin != null)
                Aurora.DataManager.DataManager.DefaultGenericPlugin.Insert(table, values, updateKey, updateValue);
            else
            {
                foreach (IGenericData plugin in Aurora.DataManager.DataManager.AllGenericPlugins)
                {
                    bool success = plugin.Insert(table, values, updateKey, updateValue);
                    if (success)
                        return true;
                }
            }
            return false;
        }

        #endregion
    }
}
