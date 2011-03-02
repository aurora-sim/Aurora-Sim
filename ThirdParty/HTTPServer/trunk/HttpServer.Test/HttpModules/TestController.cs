
using HttpServer.MVC.Controllers;

namespace HttpServer.Test.HttpModules
{
    class TestController : RequestController
    {
        private static string _method;

        public string Method
        {
            get { return _method; }
            set { _method = value; }
        }

        public string MyTest()
        {
            _method = "MyTest";
            return "hello";
        }

        [RawHandler]
        public void MyRaw()
        {
            _method = "MyRaw";
        }

        public override object Clone()
        {
            return new TestController();
        }
    }
}
