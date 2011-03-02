
using System.Reflection;

namespace HttpServer.Test
{
    class Program
    {


        static void Main(string[] args)
        {

            //HamlTest ytest = new HamlTest();
            //ytest.TestLayout();

            HttpServerLoadTests tests = new HttpServerLoadTests();
            tests.Test();
        }
    }
}
