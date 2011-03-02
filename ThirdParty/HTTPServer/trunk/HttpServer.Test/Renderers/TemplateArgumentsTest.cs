using System;
using HttpServer.MVC.Rendering;
using Xunit;

namespace HttpServer.Test.Renderers
{
    /// <summary>
    /// Contains tests for the TemplateArgument class and subclass ArgumentContainer.
    /// Most functions tests the ArgumentContainer class automagically because those are the ones actually throwing the
    /// exceptions.
    /// </summary>
    
    public class TemplateArgumentsTest
    {
        private TemplateArguments _arguments;

        /// <summary>
        /// Test to check so that a user must pass a valid object 
        /// </summary>
        [Fact]
        public void TestNoTypeSubmittedTo()
        {
            Assert.Throws(typeof (ArgumentNullException), delegate { _arguments = new TemplateArguments("User", null); });
        }

        /// <summary>
        /// Test to check so that a user must pass a valid name for an object 
        /// </summary>
        [Fact]
        public void TestNullStringSubmitted()
        {
            Assert.Throws(typeof(ArgumentNullException), delegate { _arguments = new TemplateArguments(null, 1); });
        }

        /// <summary>
        /// Test to make sure types must correspond
        /// </summary>
        [Fact]
        public void TestWrongTypeSubmitted()
        {

            _arguments = new TemplateArguments();
            Assert.Throws(typeof (ArgumentException), delegate { _arguments.Add("TestString", 4, typeof (float)); });
        }

        /// <summary>
        /// Test to make sure duplicates are noticed
        /// </summary>
        [Fact]
        public void TestDuplicate()
        {
            _arguments = new TemplateArguments("Test", 1);
            Assert.Throws(typeof (ArgumentException), delegate
                                                          {
                                                              _arguments.Add("Test", 2);
                                                          });
        }

        /// <summary>
        /// Test to make sure null objects cannot be passed without type information
        /// </summary>
        [Fact]
        public void TestNullObject()
        {
            _arguments = new TemplateArguments();
            Assert.Throws(typeof(ArgumentNullException), delegate { _arguments.Add("Test", null); });
        }

        /// <summary>
        /// Tests to make sure no nonexisting value can be updated
        /// </summary>
        [Fact]
        public void TestNonExisting()
        {
            _arguments = new TemplateArguments();
            Assert.Throws(typeof(ArgumentException), delegate { _arguments.Update("Test", 2); });
        }
    }
}