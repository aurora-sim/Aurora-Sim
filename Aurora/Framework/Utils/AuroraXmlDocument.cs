using System;
using System.Linq;
using System.Xml;

namespace Aurora.Framework
{
    /// <summary>
    /// Summary description for NSXML.
    /// </summary>
    public sealed class AuroraXmlDocument : XmlDocument
    {
        #region Constructors
        /// <summary>
        /// Default Constructor
        /// </summary>
        /// 
        public AuroraXmlDocument()
        {
        }

        /// <summary>
        /// Creates a AuroraXmlDocument and loads it with the xml
        /// </summary>
        /// <param name="xml"></param>
        public AuroraXmlDocument(string xml)
            : this()
        {
            LoadXml(xml);
        }

        #endregion
        #region root work

        /// <summary>
        /// Creates a new AuroraXmlDocument with the specified rootName
        /// </summary>
        /// <param name="rootName"></param>
        /// <returns></returns>
        public static AuroraXmlDocument NewXmlDocumentWithRoot(string rootName)
        {
            return new AuroraXmlDocument("<" + rootName + "/>");
        }

        /// <summary>
        /// Adds the nodeName as the root element
        /// </summary>
        /// <param name="nodeName"></param>
        /// <returns></returns>
        public XmlElement AddRootElement(string nodeName)
        {
            return CreateElement(nodeName);
        }


        #endregion
        #region addnode functions

        /// <summary>
        /// Creates and returns an XmlNode with an attribute and attribute value. This does not place it in the DOM.
        /// </summary>
        /// <param name="nodeName">Name of the node to create</param>
        /// <param name="nodeValue">Value of the node to create</param>
        /// <param name="attributeName">Attribute name in the node to create</param>
        /// <param name="attributeValue">Attribute value in the node to create</param>
        /// <returns>XmlNode created</returns>
        public XmlNode CreateNode(string nodeName, string nodeValue, string attributeName, string attributeValue)
        {
            XmlNode node = CreateElement(nodeName);
            node.InnerText = nodeValue;

            if (!IsNull(attributeName))
            {
                XmlAttribute attribute = CreateAttribute(attributeName);
                attribute.Value = attributeValue;
                if (node.Attributes != null) node.Attributes.SetNamedItem(attribute);
            }
            return node;
        }

        private static bool IsNull(string val)
        {
            return val == null || val.Trim() == string.Empty;
        }

        /// <summary>
        /// Creates and returns an XmlNode. This does not place it in the DOM.
        /// </summary>
        /// <param name="nodeName">Name of the node to create</param>
        /// <param name="nodeValue">Value of the node to create</param>
        /// <returns>XmlNode created</returns>
        public XmlNode CreateNode(string nodeName, string nodeValue)
        {
            return CreateNode(nodeName, nodeValue, string.Empty, string.Empty);
        }

        /// <summary>
        /// Creates and returns an XmlNode. This does not place it in the DOM.
        /// </summary>
        /// <param name="nodeName">Name of the node to create</param>
        /// <param name="nodeValue">Value of the node to create</param>
        /// <returns>XmlNode created</returns>
        public XmlNode CreateNode(string nodeName, int nodeValue)
        {
            return CreateNode(nodeName, Convert.ToString(nodeValue), string.Empty, string.Empty);
        }

        /// <summary>
        /// Creates and returns an XmlNode with an attribute and attribute value. This does not place it in the DOM.
        /// </summary>
        /// <param name="nodeName">Name of the node to create</param>
        /// <param name="nodeValue">Value of the node to create</param>
        /// <param name="attributeName">Attribute name in the node to create</param>
        /// <param name="attributeValue">Attribute value in the node to create</param>
        /// <returns>XmlNode created</returns>
        public XmlNode CreateNode(string nodeName, int nodeValue, string attributeName, string attributeValue)
        {
            return CreateNode(nodeName, Convert.ToString(nodeValue), attributeName, attributeValue);
        }

        /// <summary>
        /// Creates and returns an XmlNode with an attribute and attribute value. This does not place it in the DOM.
        /// </summary>
        /// <param name="nodeName">Name of the node to create</param>
        /// <param name="nodeValue">Value of the node to create</param>
        /// <param name="attributeName">Attribute name in the node to create</param>
        /// <param name="attributeValue">Attribute value in the node to create</param>
        /// <returns>XmlNode created</returns>
        public XmlNode CreateNode(string nodeName, string nodeValue, string attributeName, int attributeValue)
        {
            return CreateNode(nodeName, nodeValue, attributeName, Convert.ToString(attributeValue));
        }

        /// <summary>
        /// Creates and returns an XmlNode with an attribute and attribute value. This does not place it in the DOM.
        /// </summary>
        /// <param name="nodeName">Name of the node to create</param>
        /// <param name="nodeValue">Value of the node to create</param>
        /// <param name="attributeName">Attribute name in the node to create</param>
        /// <param name="attributeValue">Attribute value in the node to create</param>
        /// <returns>XmlNode created</returns>
        public XmlNode CreateNode(string nodeName, int nodeValue, string attributeName, int attributeValue)
        {
            return CreateNode(nodeName, Convert.ToString(nodeValue), attributeName, Convert.ToString(attributeValue));
        }


        /// <summary>
        /// Adds a node to root
        /// </summary>
        /// <param name="selectedNode">XmlNode where to add</param>
        /// <param name="nodeToAdd">XmlNode to add</param>
        /// <returns>XmlNode added</returns>
        public XmlNode AddNode(XmlNode selectedNode, XmlNode nodeToAdd)
        {
            selectedNode.AppendChild(nodeToAdd);
            return nodeToAdd;
        }

        /// <summary>
        /// Adds a node to root
        /// </summary>
        /// <param name="nodeName">Name of the node to add</param>
        /// <param name="nodeValue">Value of node to add</param>
        /// <returns>XmlNode added</returns>
        public XmlNode AddNode(string nodeName, string nodeValue)
        {
            return AddNode(DocumentElement, CreateNode(nodeName, nodeValue));
        }

        /// <summary>
        /// Adds a node with an attribute and attribute value to root
        /// </summary>
        /// <param name="nodeName"></param>
        /// <param name="nodeValue"></param>
        /// <param name="attributeName"></param>
        /// <param name="attributeValue"></param>
        /// <returns>XmlNode added</returns>
        public XmlNode AddNode(string nodeName, string nodeValue, string attributeName, string attributeValue)
        {
            return AddNode(DocumentElement, CreateNode(nodeName, nodeValue, attributeName, attributeValue));
        }

        /// <summary>
        /// Adds a node to root named by xPath
        /// </summary>
        /// <param name="xPath"></param>
        /// <param name="nodeName"></param>
        /// <param name="nodeValue"></param>
        /// <returns>XmlNode added</returns>
        public XmlNode AddNode(string xPath, string nodeName, string nodeValue)
        {
            if (DocumentElement == null) throw new Exception("DocumentElemnt is null");
            return AddNode(DocumentElement.SelectSingleNode(xPath), CreateNode(nodeName, nodeValue));
        }

        /// <summary>
        /// Adds a node to root named by xPath
        /// </summary>
        /// <param name="xPath"></param>
        /// <param name="nodeName"></param>
        /// <param name="nodeValue"></param>
        /// <param name="attributeName"></param>
        /// <param name="attributeValue"></param>
        /// <returns>XmlNode added</returns>
        public XmlNode AddNode(string xPath, string nodeName, string nodeValue, string attributeName, string attributeValue)
        {
            if (DocumentElement == null) throw new Exception("DocumentElemnt is null");
            return AddNode(DocumentElement.SelectSingleNode(xPath), CreateNode(nodeName, nodeValue, attributeName, attributeValue));
        }

        #endregion
        #region AddXMLDoc funtions

        /// <summary>
        /// Combines to xml documents into one. 
        /// </summary>
        /// <param name="XmlDoc">XmlDoc you want to add to this Xmldoc</param>
        /// <param name="xPathFrom">XPath to the node or nodes you want to move</param>
        /// <param name="xPathTo">XPath to the node that you want to place the xml</param>
        /// <returns></returns>
        public AuroraXmlDocument AddXMLDoc(AuroraXmlDocument XmlDoc, string xPathFrom, string xPathTo)
        {
            if (DocumentElement == null) throw new Exception("DocumentElemnt is null");
            XmlNode node = XmlDoc.SelectSingleNode(xPathFrom);
            if (node != null)
            {
                if (node.ParentNode != null)
                {
                    XmlNode selectedNode = node.ParentNode.RemoveChild(node);
                    XmlNode selectedNode2 = ImportNode(selectedNode, true);
                    XmlNode singleNode = DocumentElement.SelectSingleNode(xPathTo);
                    if (singleNode != null)
                        singleNode.AppendChild(selectedNode2);
                }
            }
            return this;
        }

        #endregion
        #region editNode functions

        /// <summary>
        /// Edit the value of a node
        /// </summary>
        /// <param name="xPath">XPath to the node</param>
        /// <param name="theValue">New Value of the node</param>
        /// <returns>XmlNode edited</returns>
        public XmlNode EditNode(string xPath, string theValue)
        {
            return EditNode(xPath, theValue, null, null);
        }

        /// <summary>
        /// Edit the value of a node
        /// </summary>
        /// <param name="xPath">XPath to the node</param>
        /// <param name="theValue">New Value of the node</param>
        /// <returns>XmlNode edited</returns>
        public XmlNode EditNode(string xPath, int theValue)
        {
            return EditNode(xPath, Convert.ToString(theValue), null, null);
        }

        /// <summary>
        /// Edit the value of a node
        /// </summary>
        /// <param name="xPath">XPath to the node</param>
        /// <param name="theValue">New Value of the node</param>
        /// <param name="attributeName">Attribute name in the node to edit</param>
        /// <param name="attributeValue">Attribute value in the node to edit</param>
        /// <returns>XmlNode edited</returns>
        public XmlNode EditNode(string xPath, int theValue, string attributeName, string attributeValue)
        {
            return EditNode(xPath, Convert.ToString(theValue), attributeName, attributeValue);
        }

        /// <summary>
        /// Edit the value of a node
        /// </summary>
        /// <param name="xPath">XPath to the node</param>
        /// <param name="theValue">New Value of the node</param>
        /// <param name="attributeName">Attribute name in the node to edit</param>
        /// <param name="attributeValue">Attribute value in the node to edit</param>
        /// <returns>XmlNode edited</returns>
        public XmlNode EditNode(string xPath, string theValue, string attributeName, int attributeValue)
        {
            return EditNode(xPath, theValue, attributeName, Convert.ToString(attributeValue));
        }

        /// <summary>
        /// Edit the value of a node
        /// </summary>
        /// <param name="xPath">XPath to the node</param>
        /// <param name="theValue">New Value of the node</param>
        /// <param name="attributeName">Attribute name in the node to edit</param>
        /// <param name="attributeValue">Attribute value in the node to edit</param>
        /// <returns>XmlNode edited</returns>
        public XmlNode EditNode(string xPath, int theValue, string attributeName, int attributeValue)
        {
            return EditNode(xPath, Convert.ToString(theValue), attributeName, Convert.ToString(attributeValue));
        }

        /// <summary>
        /// Edit the value of a node
        /// </summary>
        /// <param name="xPath">XPath to the node</param>
        /// <param name="theValue">New Value of the node</param>
        /// <param name="attributeName">Attribute name in the node to edit</param>
        /// <param name="attributeValue">Attribute value in the node to edit</param>
        /// <returns>XmlNode edited</returns>
        public XmlNode EditNode(string xPath, string theValue, string attributeName, string attributeValue)
        {
            if (DocumentElement == null) throw new Exception("DocumentElemnt is null");
            XmlNode selectedNode = !IsNull(attributeName) ? DocumentElement.SelectSingleNode(xPath + "[@" + attributeName + "='" + attributeValue + "']") : DocumentElement.SelectSingleNode(xPath);

            if (selectedNode != null)
            {
                selectedNode.InnerText = theValue;
                return selectedNode;
            }
            throw new Exception("Node does not exist");
        }

        #endregion
        #region delete Node(s)

        /// <summary>
        /// Delete node
        /// </summary>
        /// <param name="xPath">XPath to the node to delete</param>
        /// <returns></returns>
        public bool DeleteNode(string xPath)
        {
            if (DocumentElement == null) throw new Exception("DocumentElemnt is null");
            return DeleteNode(DocumentElement.SelectSingleNode(xPath));
        }

        /// <summary>
        /// Delete node
        /// </summary>
        /// <param name="selectedNode">Node to delete from document</param>
        /// <returns></returns>
        public bool DeleteNode(XmlNode selectedNode)
        {
            if (selectedNode == null) return false;
            XmlNode selectedNode2 = selectedNode.ParentNode;
            if (selectedNode2 != null) selectedNode2.RemoveChild(selectedNode);
            return true;
        }

        /// <summary>
        /// Delete multiple nodes
        /// </summary>
        /// <param name="xPath">xPath to the nodes to be deleted</param>
        /// <returns></returns>
        public bool DeleteNodes(string xPath)
        {
            if (DocumentElement == null) throw new Exception("DocumentElemnt is null");
            return DeleteNodes(DocumentElement.SelectNodes(xPath));
        }

        /// <summary>
        /// Delete multiple nodes
        /// </summary>
        /// <param name="selectedNodes">Nodes to be deleted</param>
        /// <returns></returns>
        public bool DeleteNodes(XmlNodeList selectedNodes)
        {
            if (DocumentElement == null) throw new Exception("DocumentElemnt is null");
            foreach (XmlNode selectedNode in selectedNodes)
                DocumentElement.RemoveChild(selectedNode);
            return true;
        }




        #endregion
        #region attributes

        /// <summary>
        /// Addes an attribute and attribute value to a node that the xPath resolves to
        /// </summary>
        /// <param name="xPath">XPath of the node to add the attribute and attribute value to</param>
        /// <param name="attributeName">Name of the attribute to add</param>
        /// <param name="attributeValue">Value of the attribute being added</param>
        public void AddAttribute(string xPath, string attributeName, string attributeValue)
        {
            if (DocumentElement == null) throw new Exception("DocumentElemnt is null");
            AddAttribute(DocumentElement.SelectSingleNode(xPath), attributeName, attributeValue);
        }

        /// <summary>
        /// Addes an attribute and attribute value to a node passed in
        /// </summary>
        /// <param name="node">Node to add the attribute and attribute value to</param>
        /// <param name="attributeName">Name of the attribute to add</param>
        /// <param name="attributeValue">Value of the attribute being added</param>
        public void AddAttribute(XmlNode node, string attributeName, string attributeValue)
        {
            if (!IsNull(attributeName))
            {
                XmlAttribute attribute = CreateAttribute(attributeName);
                attribute.Value = attributeValue;
                if (node.Attributes != null) node.Attributes.SetNamedItem(attribute);
            }
        }


        /// <summary>
        /// Edit the value of an attribute
        /// </summary>
        /// <param name="xPath">XPath to the node with the attribute to edit</param>
        /// <param name="attributeName">Name of the attribute to edit</param>
        /// <param name="attributeValue">New value of the attribute</param>
        /// <returns></returns>
        public XmlNode EditAttribute(string xPath, string attributeName, string attributeValue)
        {
            if (DocumentElement == null) throw new Exception("DocumentElemnt is null");
            XmlNode node = DocumentElement.SelectSingleNode(xPath);
            if (node != null)
            {
                XmlAttributeCollection oAtt = node.Attributes;
                if (oAtt != null) oAtt[attributeName].Value = attributeValue;
            }
            return DocumentElement.SelectSingleNode(xPath);
        }

        /// <summary>
        /// Edit the value of an attribute
        /// </summary>
        /// <param name="xPath">XPath to the node with the attribute to edit</param>
        /// <param name="attributeName">Name of the attribute to edit</param>
        /// <returns></returns>
        public string GetAttribute(string xPath, string attributeName)
        {
            if (DocumentElement == null) throw new Exception("DocumentElemnt is null");
            string returnValue = "";
            XmlNode node = DocumentElement.SelectSingleNode(xPath);
            if (node != null && ((node.Attributes != null) && (node.Attributes[attributeName] != null)))
            {
                XmlNode singleNode = DocumentElement.SelectSingleNode(xPath);
                if (singleNode != null)
                    if (singleNode.Attributes != null) returnValue = singleNode.Attributes[attributeName].Value;
            }
            return returnValue;
        }

        #endregion
        #region misc functions

        public bool hasChildNodes2(XmlNode selectedNode)
        {
            return selectedNode.ChildNodes.Cast<XmlNode>().Any(selectedNode2 => selectedNode2.Name != "#text");
        }


        /// <summary>
        /// Converts the common xml date to normal date
        /// </summary>
        /// <param name="xmlDate"></param>
        /// <returns></returns>
        public string ConvertDateXML(string xmlDate)
        {
            if (xmlDate.IndexOf("T") == 0)
            {
                if (IsDate(xmlDate))
                    return xmlDate;
                return "";
            }
            if (xmlDate.IndexOf(".") > 0)
                xmlDate = xmlDate.Substring(1, xmlDate.IndexOf(".") - 1);
            return Convert.ToString(Convert.ToDateTime(xmlDate.Replace("T", " ")));
        }

        public static bool IsDate(string value)
        {
            DateTime t;
            return DateTime.TryParse(value, out t);
        }

        /// <summary>
        /// Return the value of a node, if the node does not exsist then string.Empty is returned
        /// </summary>
        /// <param name="xPath">XPath to the node you want the value of</param>
        /// <returns>Return the value of a node, if the node does not exsist then string.Empty is returned</returns>
        public string GetXmlnodeValue(string xPath)
        {
            if (DocumentElement == null) throw new Exception("DocumentElemnt is null");
            XmlNode node = this.DocumentElement.SelectSingleNode(xPath);
            if (node == null)
                return string.Empty;
            return node.InnerText;
        }

        #endregion
    }
}
