using Xunit;

namespace HttpServer.Test
{
    
    public class HttpInputTest
    {
        private readonly HttpInputItem _item;

        public HttpInputTest()
        {
            _item = new HttpInputItem("base", "esab");
        }


		[Fact]
		private static void TestToString()
		{
			string queryString = "title=hello&firstname=jonas&status=super";
			HttpInput input = HttpHelper.ParseQueryString(queryString);
			Assert.Equal(queryString, input.ToString(true));

			queryString = "title[jonas]=hello&title[arne]=ohno&firstname=jonas&status=super";
			input = HttpHelper.ParseQueryString(queryString);
			Assert.Equal(queryString, input.ToString(true));

		}

        [Fact]
        public void Test()
        {
            HttpInput input = new HttpInput("teest");
            input.Add("ivrMenu", string.Empty);
            input.Add("ivrMenu[Digits][1][Digit]", "1");
            input.Add("ivrMenu[Digits][1][Action]", "2");
            input.Add("ivrMenu[Digits][1][Value]", string.Empty);
            Assert.Equal("1", input["ivrMenu"]["Digits"]["1"]["Digit"].Value);
            Assert.Equal("2", input["ivrMenu"]["Digits"]["1"]["Action"].Value);
            Assert.Equal(string.Empty, input["ivrMenu"]["Digits"]["1"]["Value"].Value);
        }

        [Fact]
        public void TestSimple()
        {
            HttpInput input = new HttpInput("teest");
            input.Add("ivrMenu", "myName");
            Assert.Equal("myName", input["ivrMenu"].Value);
        }

        [Fact]
        public void TestProperties()
        {
            Assert.Equal("base", _item.Name);
            Assert.Equal("esab", _item.Value);
        }

        [Fact]
        public void TestSubItems()
        {
            _item.Add("user[name]", "jonas");
            HttpInputItem item = _item["user"];
            Assert.Equal(null, item.Value);

            HttpInputItem item2 = item["name"];
            Assert.Equal("jonas", item2.Value);
            Assert.Equal("jonas", item["name"].Value);

            Assert.Equal(item["name"].Name, "name");
        }

        [Fact]
        public void TestMultipleValues()
        {
            _item.Add("names", "Jonas");
            _item.Add("names", "Stefan");
            _item.Add("names", "Adam");
            Assert.Equal(3, _item["names"].Count);
            Assert.Equal("Jonas", _item["names"].Values[0]);
            Assert.Equal("Stefan", _item["names"].Values[1]);
            Assert.Equal("Adam", _item["names"].Values[2]);
        }

        [Fact]
        public void TestAgain()
        {
            HttpInput parent = new HttpInput("test");
            parent.Add("interception[code]", "2");
            Assert.Equal("2", parent["interception"]["code"].Value);
            parent.Add("theid", "2");
            Assert.Equal("2", parent["theid"].Value);
            parent.Add("interception[totime]", "1200");
            Assert.Equal("1200", parent["interception"]["totime"].Value);
        }

        [Fact]
        public void TestMultipleLevels()
        {
            HttpInput parent = new HttpInput("test");
            parent.Add("user[extension][id]", "1");
            Assert.Equal("1", parent["user"]["extension"]["id"].Value);
        }
    }
}
