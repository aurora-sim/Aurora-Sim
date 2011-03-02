using System.Text;
using HttpServer.MVC.Controllers;

namespace HttpServer.Test.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    class MyController : RequestController
    {
        public string HelloWorld()
        {
            return "HelloWorld";
        }

        [RawHandler]
        public void Raw()
        {
            Response.SendHeaders();

            byte[] mybytes = Encoding.ASCII.GetBytes("Hello World");
            Response.SendBody(mybytes, 0, mybytes.Length);
        }

        public override object Clone()
        {
            return new MyController();
        }
    }
}
