#region Copyright
//
// Nini Configuration Project.
// Copyright (C) 2006 Brent R. Matzelle.  All rights reserved.
//
// This software is published under the terms of the MIT X11 license, a copy of 
// which has been included with this distribution in the LICENSE.txt file.
// 
#endregion

using System;
using System.IO;
using NUnit.Framework;
using Nini.Util;

namespace Nini.Test.Util
{
	[TestFixture]
	public class OrderedListTests
	{
		[Test]
		public void BasicOrder ()
		{
			OrderedList list = new OrderedList ();
			
			list.Add ("One", 1);
			list.Add ("Two", 2);
			list.Add ("Three", 3);
			
			Assert.AreEqual (1, list[0]);
			Assert.AreEqual (2, list[1]);
			Assert.AreEqual (3, list[2]);
			
			Assert.AreEqual (1, list["One"]);
			Assert.AreEqual (2, list["Two"]);
			Assert.AreEqual (3, list["Three"]);			
		}
	}
}