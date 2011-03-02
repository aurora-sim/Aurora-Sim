using System;
using System.Collections.Generic;
using System.Text;
using HttpServer.Helpers;
using HttpServer.Helpers.Implementations;
using Xunit;

namespace HttpServer.Test.Helpers
{
    
    public class PrototypeTest
    {
        PrototypeImp _imp = new PrototypeImp();

        [Fact]
        public void TestOnSubmit()
        {
            string res = _imp.AjaxFormOnSubmit("onsuccess", "alert('Hello world!');");
            Assert.Equal("new Ajax.Request(this.action, { parameters: Form.serialize(this), method: 'post', asynchronous: true, evalScripts: true });", res);
            res = _imp.AjaxFormOnSubmit("onsuccess:", "alert('Hello world!');");
            Assert.Equal("new Ajax.Request(this.action, { parameters: Form.serialize(this), onsuccess: alert(\'Hello world!\');, method: 'post', asynchronous: true, evalScripts: true });", res);

            string ajax = JSHelper.AjaxUpdater("/test", "theField");
            res = _imp.AjaxFormOnSubmit("onsuccess:", ajax);
            Assert.Equal("new Ajax.Request(this.action, { parameters: Form.serialize(this), onsuccess: " + ajax + ", method: 'post', asynchronous: true, evalScripts: true });", res);
        }
    }
}
