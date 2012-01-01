using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurora.Framework
{
    public interface ISceneObject : ISceneEntity
    {
        /// <summary>
        /// Returns an XML based document that represents this object
        /// </summary>
        /// <returns></returns>
        string ToXml2();

        /// <summary>
        /// Returns an XML based document that represents this object
        /// </summary>
        /// <returns></returns>
        byte[] ToBinaryXml2();

        /// <summary>
        /// Adds the FromInventoryItemID to the xml
        /// </summary>
        /// <returns></returns>
        string ExtraToXmlString();
        void ExtraFromXmlString(string xmlstr);
    }
}
