using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace HttpServer.Test
{
	
	public class HttpInputItemTest
	{
		public void Test()
		{
			HttpInputItem item = new HttpInputItem("Name", "Value");
			Assert.Equal("Value", item.Value);

			item.Add("value2");
			Assert.Equal(item.Values.Count, 2);
			Assert.Equal("value2", item.Values[1]);

			item.Add("subName", "subValue");
			Assert.Equal("subValue", item["subName"].Value);
		}
	}
}
