using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Lifetime;
using System.Threading;
using System.Xml;
using log4net;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Region.CoreModules.Framework.EventQueue;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenMetaverse;
using OpenMetaverse.StructuredData;
using OpenSim.Framework.Console;
using Aurora.Framework;
using Aurora.ScriptEngine.AuroraDotNetEngine.Plugins;
using Aurora.ScriptEngine.AuroraDotNetEngine.CompilerTools;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs.Interfaces;
using Aurora.ScriptEngine.AuroraDotNetEngine.APIs;
using Aurora.ScriptEngine.AuroraDotNetEngine.Runtime;

namespace Aurora.ScriptEngine.AuroraDotNetEngine
{
    #region SQL serializer

    public class ScriptDataSQLSerializer
    {
        public static void SaveState(ScriptData instance, ScriptEngine engine)
        {
            StateSave Insert = new StateSave();
            Insert.State = instance.State;
            Insert.ItemID = instance.ItemID;
            string source = instance.Source.Replace("\n", " ");
            Insert.Source = source.Replace("'", " ");
            Insert.Running = instance.Running;
            //Vars
            Dictionary<string, Object> vars = new Dictionary<string, object>();
            if (instance.Script != null)
                vars = instance.Script.GetVars();
            string varsmap = "";
            foreach (KeyValuePair<string, Object> var in vars)
            {
                varsmap += var.Key + "," + var.Value + "\n";
            }
            Insert.Variables = varsmap;
            //Plugins
            object[] Plugins = engine.GetSerializationData( instance.ItemID, instance.part.UUID);
            string plugins = "";
            foreach (object plugin in Plugins)
                plugins += plugin + ",";
            Insert.Plugins = plugins;

            //perms
            string perms = "";
            if (instance.InventoryItem != null)
            {
                if (instance.InventoryItem.PermsMask != 0 && instance.InventoryItem.PermsGranter != UUID.Zero)
                {
                    perms += instance.InventoryItem.PermsGranter.ToString() + "," + instance.InventoryItem.PermsMask.ToString();

                }
            }
            Insert.Permissions = perms;

            Insert.MinEventDelay = instance.EventDelayTicks;
            string[] AN = instance.AssemblyName.Split('\\');
            if(AN.Length > 2)
                Insert.AssemblyName = instance.AssemblyName.Split('\\')[2];
            else
            	Insert.AssemblyName = instance.AssemblyName;
            Insert.Disabled = instance.Disabled;
            Insert.UserInventoryID = instance.UserInventoryItemID;
            IScriptDataConnector ScriptFrontend = Aurora.DataManager.DataManager.RequestPlugin<IScriptDataConnector>();
            if(ScriptFrontend != null)
                ScriptFrontend.SaveStateSave(Insert);
        }

        public static void Deserialize(ScriptData instance, ScriptEngine engine, StateSave save)
        {
            Dictionary<string, object> vars = save.Variables as Dictionary<string, object>;
            instance.State = save.State;
            instance.Running = save.Running;

            if (vars != null && vars.Count != 0)
                instance.Script.SetVars(vars);

            instance.PluginData = (object[])save.Plugins;
            if (save.Permissions != " " && save.Permissions != "")
            {
                instance.InventoryItem.PermsGranter = new UUID(save.Permissions.Split(',')[0]);
                instance.InventoryItem.PermsMask = int.Parse(save.Permissions.Split(',')[1], NumberStyles.Integer, Culture.NumberFormatInfo);
            }
            instance.EventDelayTicks = (long)save.MinEventDelay;
            instance.AssemblyName = save.AssemblyName;
            instance.Disabled = save.Disabled;
            instance.UserInventoryItemID = save.UserInventoryID;
            // Add it to our script memstruct
            ScriptEngine.ScriptProtection.AddNewScript(instance);
        }
    }

    #endregion

    #region XML serializer

    public class ScriptDataXMLSerializer
    {
        public static string GetXMLState(ScriptData instance, ScriptEngine engine)
        {
            if (instance.Script == null)
                return "";
            //Update PluginData
            instance.PluginData = engine.GetSerializationData(instance.ItemID, instance.part.UUID);

            bool running = instance.Running;

            XmlDocument xmldoc = new XmlDocument();

            XmlNode xmlnode = xmldoc.CreateNode(XmlNodeType.XmlDeclaration,
                                                "", "");
            xmldoc.AppendChild(xmlnode);

            XmlElement rootElement = xmldoc.CreateElement("", "ScriptState",
                                                          "");
            xmldoc.AppendChild(rootElement);

            XmlElement state = xmldoc.CreateElement("", "State", "");
            state.AppendChild(xmldoc.CreateTextNode(instance.State));

            rootElement.AppendChild(state);

            XmlElement run = xmldoc.CreateElement("", "Running", "");
            run.AppendChild(xmldoc.CreateTextNode(
                    running.ToString()));

            rootElement.AppendChild(run);

            Dictionary<string, Object> vars = instance.Script.GetVars();

            XmlElement variables = xmldoc.CreateElement("", "Variables", "");

            foreach (KeyValuePair<string, Object> var in vars)
                WriteTypedValue(xmldoc, variables, "Variable", var.Key,
                                var.Value);

            rootElement.AppendChild(variables);

            #region Queue

            //We don't do queue...
            XmlElement queue = xmldoc.CreateElement("", "Queue", "");
            rootElement.AppendChild(queue);

            #endregion

            XmlNode plugins = xmldoc.CreateElement("", "Plugins", "");
            DumpList(xmldoc, plugins,
                     new LSL_Types.list(instance.PluginData));

            rootElement.AppendChild(plugins);

            if (instance.InventoryItem != null)
            {
                if (instance.InventoryItem.PermsMask != 0 && instance.InventoryItem.PermsGranter != UUID.Zero)
                {
                    XmlNode permissions = xmldoc.CreateElement("", "Permissions", "");
                    XmlAttribute granter = xmldoc.CreateAttribute("", "granter", "");
                    granter.Value = instance.InventoryItem.PermsGranter.ToString();
                    permissions.Attributes.Append(granter);
                    XmlAttribute mask = xmldoc.CreateAttribute("", "mask", "");
                    mask.Value = instance.InventoryItem.PermsMask.ToString();
                    permissions.Attributes.Append(mask);
                    rootElement.AppendChild(permissions);
                }
            }

            if (instance.EventDelayTicks > 0.0)
            {
                XmlElement eventDelay = xmldoc.CreateElement("", "MinEventDelay", "");
                eventDelay.AppendChild(xmldoc.CreateTextNode(instance.EventDelayTicks.ToString()));
                rootElement.AppendChild(eventDelay);
            }
            Type type = instance.Script.GetType();
            FieldInfo[] mi = type.GetFields();
            string xml = xmldoc.InnerXml;

            XmlDocument sdoc = new XmlDocument();
            sdoc.LoadXml(xml);
            XmlNodeList rootL = sdoc.GetElementsByTagName("ScriptState");
            XmlNode rootNode = rootL[0];

            // Create <State UUID="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx">
            XmlDocument doc = new XmlDocument();
            XmlElement stateData = doc.CreateElement("", "State", "");
            XmlAttribute stateID = doc.CreateAttribute("", "UUID", "");
            stateID.Value = instance.ItemID.ToString();
            stateData.Attributes.Append(stateID);
            XmlAttribute assetID = doc.CreateAttribute("", "Asset", "");
            assetID.Value = instance.InventoryItem.AssetID.ToString();
            stateData.Attributes.Append(assetID);
            XmlAttribute engineName = doc.CreateAttribute("", "Engine", "");
            engineName.Value = engine.ScriptEngineName;
            stateData.Attributes.Append(engineName);
            doc.AppendChild(stateData);

            // Add <ScriptState>...</ScriptState>
            XmlNode xmlstate = doc.ImportNode(rootNode, true);
            stateData.AppendChild(xmlstate);

            string assemName = instance.AssemblyName;

            XmlElement assemblyData = doc.CreateElement("", "Assembly", "");
            XmlAttribute assemblyName = doc.CreateAttribute("", "Filename", "");

            assemblyName.Value = assemName;
            assemblyData.Attributes.Append(assemblyName);

            assemblyData.InnerText = assemName;

            stateData.AppendChild(assemblyData);

            XmlElement mapData = doc.CreateElement("", "LineMap", "");
            XmlAttribute mapName = doc.CreateAttribute("", "Filename", "");

            mapName.Value = assemName + ".map";
            mapData.Attributes.Append(mapName);

            mapData.InnerText = assemName;

            stateData.AppendChild(mapData);

            return doc.InnerXml;
        }

        public static void SetXMLState(string xml, ScriptData instance, ScriptEngine engine)
        {
            XmlDocument doc = new XmlDocument();

            Dictionary<string, object> vars = instance.Script.GetVars();

            doc.LoadXml(xml);

            XmlNodeList rootL = doc.GetElementsByTagName("ScriptState");
            if (rootL.Count != 1)
            {
                return;
            }
            XmlNode rootNode = rootL[0];

            if (rootNode != null)
            {
                object varValue;
                XmlNodeList partL = rootNode.ChildNodes;

                foreach (XmlNode part in partL)
                {
                    switch (part.Name)
                    {
                        case "State":
                            instance.State = part.InnerText;
                            break;
                        case "Running":
                            instance.Running = bool.Parse(part.InnerText);
                            break;
                        case "Variables":
                            XmlNodeList varL = part.ChildNodes;
                            foreach (XmlNode var in varL)
                            {
                                string varName;
                                varValue = ReadTypedValue(var, out varName);

                                if (vars.ContainsKey(varName))
                                    vars[varName] = varValue;
                            }
                            instance.Script.SetVars(vars);
                            break;
                        case "Plugins":
                            instance.PluginData = ReadList(part).Data;
                            break;
                        case "Permissions":
                            string tmpPerm;
                            int mask = 0;
                            tmpPerm = part.Attributes.GetNamedItem("mask").Value;
                            if (tmpPerm != null)
                            {
                                int.TryParse(tmpPerm, out mask);
                                if (mask != 0)
                                {
                                    tmpPerm = part.Attributes.GetNamedItem("granter").Value;
                                    if (tmpPerm != null)
                                    {
                                        UUID granter = new UUID();
                                        UUID.TryParse(tmpPerm, out granter);
                                        if (granter != UUID.Zero)
                                        {
                                            instance.InventoryItem.PermsMask = mask;
                                            instance.InventoryItem.PermsGranter = granter;
                                        }
                                    }
                                }
                            }
                            break;
                        case "MinEventDelay":
                            double minEventDelay = 0.0;
                            double.TryParse(part.InnerText, NumberStyles.Float, Culture.NumberFormatInfo, out minEventDelay);
                            instance.EventDelayTicks = (long)minEventDelay;
                            break;
                    }
                }
            }
        }

        #region Helpers

        private static LSL_Types.list ReadList(XmlNode parent)
        {
            List<Object> olist = new List<Object>();

            XmlNodeList itemL = parent.ChildNodes;
            foreach (XmlNode item in itemL)
                olist.Add(ReadTypedValue(item));

            return new LSL_Types.list(olist.ToArray());
        }

        private static object ReadTypedValue(XmlNode tag, out string name)
        {
            name = tag.Attributes.GetNamedItem("name").Value;

            return ReadTypedValue(tag);
        }

        private static object ReadTypedValue(XmlNode tag)
        {
            Object varValue;
            string assembly;

            string itemType = tag.Attributes.GetNamedItem("type").Value;

            if (itemType == "list")
                return ReadList(tag);

            if (itemType == "OpenMetaverse.UUID")
            {
                UUID val = new UUID();
                UUID.TryParse(tag.InnerText, out val);

                return val;
            }

            Type itemT = Type.GetType(itemType);
            if (itemT == null)
            {
                Object[] args =
                    new Object[] { tag.InnerText };

                assembly = itemType + ", Aurora.ScriptEngine.AuroraDotNetEngine";
                itemT = Type.GetType(assembly);
                if (itemT == null)
                    return null;

                varValue = Activator.CreateInstance(itemT, args);

                if (varValue == null)
                    return null;
            }
            else
            {
                varValue = Convert.ChangeType(tag.InnerText, itemT);
            }
            return varValue;
        }

        private static void DumpList(XmlDocument doc, XmlNode parent,
                LSL_Types.list l)
        {
            foreach (Object o in l.Data)
                WriteTypedValue(doc, parent, "ListItem", "", o);
        }

        private static void WriteTypedValue(XmlDocument doc, XmlNode parent,
                string tag, string name, object value)
        {
            Type t = value.GetType();
            XmlAttribute typ = doc.CreateAttribute("", "type", "");
            XmlNode n = doc.CreateElement("", tag, "");

            if (value is LSL_Types.list)
            {
                typ.Value = "list";
                n.Attributes.Append(typ);

                DumpList(doc, n, (LSL_Types.list)value);

                if (name != String.Empty)
                {
                    XmlAttribute nam = doc.CreateAttribute("", "name", "");
                    nam.Value = name;
                    n.Attributes.Append(nam);
                }

                parent.AppendChild(n);
                return;
            }

            n.AppendChild(doc.CreateTextNode(value.ToString()));

            typ.Value = t.ToString();
            n.Attributes.Append(typ);
            if (name != String.Empty)
            {
                XmlAttribute nam = doc.CreateAttribute("", "name", "");
                nam.Value = name;
                n.Attributes.Append(nam);
            }

            parent.AppendChild(n);
        }

        #endregion
    }

    #endregion
}
