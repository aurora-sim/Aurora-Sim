using System;
using HttpServer.Helpers;
using Xunit;

namespace HttpServer.Test.Helpers
{
	class XmlHelperTest
	{

		[Fact]
		private static void TestDeserialize()
		{
			TestUser user = new TestUser();
			user.Age = 19;
			user.UserName = "Momahob";
			string xml = XmlHelper.Serialize(user);
			TestUser user2 = XmlHelper.Deserialize<TestUser>(xml);
			Assert.Equal(19, user2.Age);
			Assert.Equal("Momahob", user2.UserName);
		}
	}

	/// <summary>
	/// 
	/// </summary>
	[Serializable]

	public class TestUser
	{
		private string _userName;
		private int _age;

		/// <summary>
		/// Gets or sets the name of the user.
		/// </summary>
		/// <value>The name of the user.</value>
		public string UserName
		{
			get { return _userName; }
			set { _userName = value; }
		}

		/// <summary>
		/// Gets or sets the age.
		/// </summary>
		/// <value>The age.</value>
		public int Age
		{
			get { return _age; }
			set { _age = value; }
		}
	}
}
