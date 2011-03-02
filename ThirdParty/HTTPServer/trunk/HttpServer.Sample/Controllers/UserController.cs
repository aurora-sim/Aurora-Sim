using System;
using System.IO;
using HttpServer.Exceptions;
using HttpServer.MVC.Controllers;
using HttpServer.MVC.Rendering;

namespace HttpServer.Sample.Controllers
{
    public class UserController : RequestController
    {
        private readonly TemplateManager _templateMgr;

        public UserController(TemplateManager mgr)
        {
            _templateMgr = mgr;
            DefaultMethod = "World";
        }

        public UserController(UserController controller) : base(controller)
        {
            _templateMgr = controller._templateMgr;
        }

        public string Hello()
        {
            return Render("text", "Hello World!");
        }

        private string Render(params object[] args)
        {
            try
            {
                // Converts the incoming object arguments to proper TemplateArguments
                TemplateArguments arguments = new TemplateArguments(args);
                string pageTemplate = _templateMgr.Render("views\\user\\" + MethodName + ".haml", arguments);
                arguments.Clear();
                arguments.Add("text", pageTemplate);
                return _templateMgr.Render("views\\layouts\\application.haml", arguments);
            }
                catch(FileNotFoundException err)
                {
                    throw new NotFoundException("Failed to find template. Details: " + err.Message, err);
                }
                catch(InvalidOperationException err)
                {
                    throw new InternalServerException("Failed to render template. Details: " + err.Message, err);
                }
                catch (TemplateException err)
                {
                    throw new InternalServerException("Failed to compile template. Details: " + err.Message, err);
                }
            catch (ArgumentException err)
            {
                throw new InternalServerException("Failed to render templates", err);
            }
        }

        public override object Clone()
        {
            return new UserController(this);
        }

        public string World()
        {
            return "Mothafucka";
        }
    }
}
