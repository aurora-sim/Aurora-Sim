using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Aurora.Framework
{
	public interface IAssetConnector
	{
		ObjectMediaURL GetObjectMediaInfo(string objectID, int side);

        void UpdateObjectMediaInfo(ObjectMediaURL media);
    }
}
