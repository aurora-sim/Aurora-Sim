using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using HttpServer.Exceptions;
using HttpServer.Sessions;
using System.Text;
using System.Diagnostics;

namespace HttpServer.HttpModules
{
    /// <summary>
    /// The purpose of this module is to serve files.
    /// </summary>
    public class AdvancedFileModule : HttpModule
    {
        private readonly string _baseUri;
        private readonly string _basePath;
        private readonly bool _useLastModifiedHeader;
        private readonly IDictionary<string, string> _mimeTypes = new Dictionary<string, string>();
        private static readonly string[] DefaultForbiddenChars = new[] { "\\", "..", ":" };
        private string[] _forbiddenChars;
        private static readonly string PathSeparator = Path.DirectorySeparatorChar.ToString();

        private readonly bool _allowDirectoryListing = true;
        private readonly IDictionary<string, string> _cgiApplications = new Dictionary<string, string>();
        private readonly IDictionary<string, string> _virtualDirectories = new Dictionary<string, string>();
        private readonly List<string> _defaultIndexFiles = new List<string>();

        private Index _index;
        private bool _serveUnknownTypes;


        public static void CreateHTTPServer(string filePath, string httpServerPath,
            string PHPCGIPath, uint port, bool allowDirectoryListing)
        {
            HttpServer server = new HttpServer();
            AdvancedFileModule afm = new AdvancedFileModule(httpServerPath, filePath, false, allowDirectoryListing);
            afm.ServeUnknownTypes(true, "php");
            afm.AddCgiApplication("php", PHPCGIPath);
            server.Add(afm);
            server.Start(IPAddress.Any, (int)port);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileModule"/> class.
        /// </summary>
        /// <param name="baseUri">Uri to serve, for instance "/files/"</param>
        /// <param name="basePath">Path on hard drive where we should start looking for files</param>
        /// <param name="useLastModifiedHeader">If true a Last-Modifed header will be sent upon requests urging webbrowser to cache files</param>
        /// <param name="allowDirectoryListing">If true a request to a directory will list its content</param>
        public AdvancedFileModule(string baseUri, string basePath, bool useLastModifiedHeader, bool allowDirectoryListing)
        {
            Check.Require(baseUri, "baseUri");
            Check.Require(basePath, "basePath");

            _useLastModifiedHeader = useLastModifiedHeader;
            _baseUri = baseUri;
            _basePath = basePath;
            if (!_basePath.EndsWith(PathSeparator))
                _basePath += PathSeparator;
            ForbiddenChars = DefaultForbiddenChars;

            _allowDirectoryListing = allowDirectoryListing;

            _serveUnknownTypes = false;
            _mimeTypes.Add("default", "application/octet-stream");
            AddDefaultMimeTypes();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileModule"/> class.
        /// </summary>
        /// <param name="baseUri">Uri to serve, for instance "/files/"</param>
        /// <param name="basePath">Path on hard drive where we should start looking for files</param>
        /// <param name="useLastModifiedHeader">If true a Last-Modifed header will be sent upon requests urging webbrowser to cache files</param>
        public AdvancedFileModule(string baseUri, string basePath, bool useLastModifiedHeader)
            : this(baseUri, basePath, useLastModifiedHeader, false)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileModule"/> class.
        /// </summary>
        /// <param name="baseUri">Uri to serve, for instance "/files/"</param>
        /// <param name="basePath">Path on hard drive where we should start looking for files</param>
        public AdvancedFileModule(string baseUri, string basePath)
            : this(baseUri, basePath, false, false)
        { }

        /// <summary>
        /// List with all mime-type that are allowed. 
        /// </summary>
        /// <remarks>All other mime types will result in a Forbidden http status code.</remarks>
        public IDictionary<string, string> MimeTypes
        {
            get { return _mimeTypes; }
        }

        /// <summary>
        /// characters that may not  exist in a path.
        /// </summary>
        /// <example>
        /// fileMod.ForbiddenChars = new string[]{ "\\", "..", ":" };
        /// </example>
        public string[] ForbiddenChars
        {
            get { return _forbiddenChars; }
            set { _forbiddenChars = value; }
        }

        /// <summary>
        /// Adds a virtual directory.
        /// </summary>
        public void AddVirtualDirectory(string virtualPath, string physicalPath)
        {
            if (!Directory.Exists(physicalPath))
                return;
            //throw new Exception("path not found.");

            if (_virtualDirectories.ContainsKey(virtualPath))
                _virtualDirectories[virtualPath] = physicalPath;
            else
                _virtualDirectories.Add(virtualPath, physicalPath);
        }

        /// <summary>
        /// Adds a cgi application to handle a specific filetype.
        /// </summary>
        public void AddCgiApplication(string extension, string applicationPath)
        {
            if (!File.Exists(applicationPath))
                return;
            //throw new Exception("CGI app not found.");

            if (_cgiApplications.ContainsKey(extension))
                _cgiApplications[extension] = applicationPath;
            else
                _cgiApplications.Add(extension, applicationPath);

            // Update mimetypes
            if (!_mimeTypes.ContainsKey(extension))
                _mimeTypes.Add(extension, "wwwserver/stdcgi");
        }

        /// <summary>
        /// Adds a default index.
        /// </summary>
        /// <param name="index"></param>
        public void AddDefaultIndex(string index)
        {
            _defaultIndexFiles.Add(index);
        }

        /// <summary>
        /// Serve unknown file types.
        /// </summary>
        /// <param name="serve">True to serve unknown files.</param>
        /// <param name="type">Mimetype (i.e. 'application/octet-stream')</param>
        public void ServeUnknownTypes(bool serve, string type)
        {
            _serveUnknownTypes = serve;
            _mimeTypes["default"] = type;
        }

        /// <summary>
        /// Adds a cgi application to handle a specific filetype.
        /// </summary>
        public void AddMimeType(string extension, string mimetype)
        {
            if (_mimeTypes.ContainsKey(extension))
                _mimeTypes[extension] = mimetype;
            else
                _mimeTypes.Add(extension, mimetype);
        }

        /// <summary>
        /// Mimtypes that this class can handle per default
        /// </summary>
        public void AddDefaultMimeTypes()
        {
            MimeTypes.Add("txt", "text/plain");
            MimeTypes.Add("html", "text/html");
            MimeTypes.Add("htm", "text/html");
            MimeTypes.Add("jpg", "image/jpg");
            MimeTypes.Add("jpeg", "image/jpg");
            MimeTypes.Add("bmp", "image/bmp");
            MimeTypes.Add("gif", "image/gif");
            MimeTypes.Add("png", "image/png");

            MimeTypes.Add("ico", "image/vnd.microsoft.icon");
            MimeTypes.Add("css", "text/css");
            MimeTypes.Add("gzip", "application/x-gzip");
            MimeTypes.Add("zip", "multipart/x-zip");
            MimeTypes.Add("tar", "application/x-tar");
            MimeTypes.Add("pdf", "application/pdf");
            MimeTypes.Add("rtf", "application/rtf");
            MimeTypes.Add("xls", "application/vnd.ms-excel");
            MimeTypes.Add("ppt", "application/vnd.ms-powerpoint");
            MimeTypes.Add("doc", "application/application/msword");
            MimeTypes.Add("js", "application/javascript");
            MimeTypes.Add("mp3", "audio/mpeg");
            MimeTypes.Add("mid", "audio/midi");
            MimeTypes.Add("wav", "audio/x-wav");
            MimeTypes.Add("avi", "video/avi");
            MimeTypes.Add("swf", "application/x-shockwave-flash");
            MimeTypes.Add("xaml", "application/xaml+xml");
            MimeTypes.Add("xap", "application/x-silverlight-app");
            MimeTypes.Add("xbap", "application/x-ms-xbap");
        }

        /// <summary>
        /// Determines if the request should be handled by this module.
        /// Invoked by the HttpServer
        /// </summary>
        /// <param name="uri"></param>
        /// <returns>true if this module should handle it.</returns>
        public bool CanHandle(Uri uri)
        {
            if (Contains(uri.AbsolutePath, _forbiddenChars))
                return false;

            return uri.AbsolutePath.StartsWith(_baseUri);
        }

        /// <summary>
        /// Gets the physical path of a requested file. Handles virtual directories.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns>Physical path.</returns>
        private string GetPath(Uri uri)
        {
            if (Contains(uri.AbsolutePath, _forbiddenChars))
                throw new BadRequestException("Illegal path");

            string query = uri.LocalPath.Substring(_baseUri.Length);

            foreach (string vitrualPath in _virtualDirectories.Keys)
            {
                if (query.StartsWith(vitrualPath))
                {
                    return _virtualDirectories[vitrualPath] + query.Substring(vitrualPath.Length).Replace('/', Path.DirectorySeparatorChar);
                }
            }

            if (query == "")
            {
                query = File.Exists(_basePath + "index.php") ? "index.php" : File.Exists(_basePath + "index.html") ? "index.html" : "";
            }

            string path = _basePath + query;
            return path.Replace('/', Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// check if source contains any of the chars.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="chars"></param>
        /// <returns></returns>
        private static bool Contains(string source, IEnumerable<string> chars)
        {
            foreach (string s in chars)
            {
                if (source.Contains(s))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Method that process the url
        /// </summary>
        /// <param name="request">Information sent by the browser about the request</param>
        /// <param name="response">Information that is being sent back to the client.</param>
        /// <param name="session">Session used to </param>
        public override bool Process(IHttpRequest request, IHttpResponse response, IHttpSession session)
        {
            if (!CanHandle(request.Uri))
                return false;

            try
            {
                string path = GetPath(request.Uri);
                string extension = GetFileExtension(path);

                // Directory names can be "C:\MyDirectory.ext", file names can be "C:\MyFileWithoutExt",
                // so the safest way to see if one of it exists, is by asking the windows file system.
                bool directory_exists = Directory.Exists(path);
                bool file_exists = File.Exists(path);

                if (!directory_exists && !file_exists)
                {
                    if (!path.EndsWith("favicon.ico"))
                        Aurora.Framework.MainConsole.Instance.Output("Failed to find " + path);
                    return false;
                    throw new NotFoundException("Failed to proccess request: " + path);
                }

                if (directory_exists)
                {
                    bool indexFound = false;

                    // Look for default index files
                    if (_defaultIndexFiles.Count > 0)
                    {
                        foreach (string index in _defaultIndexFiles)
                        {
                            // TODO: Does path always end with '/'?
                            if (File.Exists(path + index))
                            {
                                path = path + index;
                                extension = GetFileExtension(path);
                                indexFound = true;
                                break;
                            }
                        }
                    }

                    if (!indexFound)
                    {
                        // List directory
                        if (!_allowDirectoryListing)
                        {
                            throw new ForbiddenException("Directory listing not allowed");
                        }

                        string output = GetDirectoryListing(path, request.Uri);

                        response.ContentType = "text/html";
                        response.ContentLength = output.Length;
                        response.SendHeaders();

                        response.SendBody(System.Text.Encoding.Default.GetBytes(output));

                        return true;
                    }
                }

                if (extension == null && file_exists) extension = "default";

                if (!MimeTypes.ContainsKey(extension))
                {
                    if (_serveUnknownTypes)
                    {
                        extension = "default";
                    }
                    else throw new ForbiddenException("Forbidden file type: " + extension);
                }

                // Cgi file
                if (MimeTypes[extension].Equals("wwwserver/stdcgi"))
                {
                    if (!_cgiApplications.ContainsKey(extension))
                        throw new ForbiddenException("Unknown cgi file type: " + extension);

                    if (!File.Exists(_cgiApplications[extension]))
                        throw new InternalServerException("Cgi executable not found: " + _cgiApplications[extension]);

                    string output = CGI.Execute(_cgiApplications[extension], path, request);

                    response.ContentType = "text/html";

                    GetCgiHeaders(ref output, response);

                    response.ContentLength = output.Length;
                    response.SendHeaders();
                    response.SendBody(Encoding.Default.GetBytes(output));
                }
                else // other files
                {
                    response.ContentType = MimeTypes[extension];

                    using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        if (!string.IsNullOrEmpty(request.Headers["if-Modified-Since"]))
                        {
                            DateTime lastRequest = DateTime.Parse(request.Headers["if-Modified-Since"]);
                            if (lastRequest.CompareTo(File.GetLastWriteTime(path)) <= 0)
                                response.Status = HttpStatusCode.NotModified;
                        }

                        if (_useLastModifiedHeader)
                            response.AddHeader("Last-modified", File.GetLastWriteTime(path).ToString("r"));
                        response.ContentLength = stream.Length;
                        response.SendHeaders();

                        if (request.Method != "Headers" && response.Status != HttpStatusCode.NotModified)
                        {
                            byte[] buffer = new byte[8192];
                            int bytesRead = stream.Read(buffer, 0, 8192);
                            while (bytesRead > 0)
                            {
                                response.SendBody(buffer, 0, bytesRead);
                                bytesRead = stream.Read(buffer, 0, 8192);
                            }
                        }
                    }
                }
            }
            catch (InternalServerException err)
            {
                throw err;
            }
            catch (ForbiddenException err)
            {
                throw err;
            }
            catch (NotFoundException err)
            {
                throw err;
            }
            catch (FileNotFoundException err)
            {
                throw new NotFoundException("Failed to proccess file: " + request.Uri.LocalPath, err);
            }
            catch (Exception err)
            {
                throw new InternalServerException("Internal server error [FileModule]", err);
            }

            return true;
        }

        /// <summary>
        /// return a file extension from an absolute uri path (or plain filename)
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public static string GetFileExtension(string uri)
        {
            int pos = uri.LastIndexOf('.');
            return pos == -1 ? null : uri.Substring(pos + 1).ToLower();
        }

        /// <summary>
        /// return a file extension from an absolute uri path (or plain filename)
        /// </summary>
        /// <param name="path"></param>
        /// <param name="uri"></param>
        /// <returns></returns>
        public string GetDirectoryListing(string path, Uri uri)
        {
            if (_index == null)
            {
                if (_virtualDirectories.ContainsKey("resources/"))
                {
                    _index = new Index(_virtualDirectories["resources/"]);
                }
                else _index = new Index();
            }

            string uriPathCurrent = uri.LocalPath.EndsWith("/") ? uri.LocalPath : uri.LocalPath + "/";
            string uriPathParent = uriPathCurrent.Substring(0, uriPathCurrent.Length - 1); ;

            int pos = uriPathParent.LastIndexOf('/') + 1;

            if (pos > 0)
            {
                uriPathParent = uriPathParent.Substring(0, pos);
            }

            string index = _index.GetTemplate(uri.LocalPath, uriPathParent, "HTTPServer", DateTime.Now.ToString());

            StringBuilder sb = new StringBuilder();

            DirectoryInfo directory = new DirectoryInfo(path);

            foreach (DirectoryInfo di in directory.GetDirectories())
            {
                sb.Append(_index.GetEntry(uriPathCurrent + di.Name, di.Name, "Folder", "", "folder"));
            }

            System.Globalization.NumberFormatInfo nfi = new System.Globalization.CultureInfo("en-US", false).NumberFormat;
            double size;

            string className, extension;

            foreach (FileInfo fi in directory.GetFiles())
            {
                className = "unknown";
                extension = fi.Extension.ToLower();
                if (extension.Length > 0)
                {
                    if (extension[0] == '.') extension = extension.Substring(1);

                    if (_mimeTypes.ContainsKey(extension))
                    {
                        if (_mimeTypes[extension].StartsWith("text")) className = "text";
                        else if (_mimeTypes[extension].StartsWith("image")) className = "image";
                    }
                }

                size = (double)fi.Length / 1024;

                sb.Append(_index.GetEntry(uriPathCurrent + fi.Name, fi.Name, size.ToString("0.00", nfi) + " KB", fi.LastWriteTime.ToString(), className));
            }

            return index.Replace("[Entry]", sb.ToString());
        }

        /// <summary>
        /// Gets the headers sent by the cgi program
        /// </summary>
        private void GetCgiHeaders(ref string cgiOutput, IHttpResponse response)
        {
            // TODO: Make this more robust (are we really stripping headers???)
            int index = cgiOutput.IndexOf("\r\n\r\n");

            if (index != -1)
            {
                string header = cgiOutput.Substring(0, index + 2);
                cgiOutput = cgiOutput.Substring(index + 2);

                int end = header.IndexOf("\r\n");

                while (end != -1)
                {
                    string line = header.Substring(0, end);
                    header = header.Substring(end + 2);

                    int colonIndex = line.IndexOf(":");
                    if (colonIndex <= 1)
                        break;

                    string val = line.Substring(colonIndex + 1).Trim();
                    string name = line.Substring(0, colonIndex).Trim();

                    response.AddHeader(name, val);

                    end = header.IndexOf("\r\n");
                }
            }

            // Remove unwanted linebreaks
            cgiOutput = cgiOutput.Trim();
        }
    }


    /// <summary>
    /// Manage CGI process.
    /// </summary>
    internal static class CGI
    {
        public static string Execute(string application, string file, IHttpRequest request)
        {
            string output = "";

            // Create the child process.
            Process cgi = new Process();

            cgi.StartInfo.UseShellExecute = false;
            cgi.StartInfo.CreateNoWindow = true;
            cgi.StartInfo.RedirectStandardOutput = true;
            cgi.StartInfo.RedirectStandardError = true;
            cgi.StartInfo.FileName = application;
            // cgi.StartInfo.Arguments = "\"" + file + "\"";

            // Be careful here:
            // For some reason the cgi variable names are turned into lowercase by the windows 
            // environment (i.e. REMOTE_ADDR gets remote_addr). So in PHP, for example, it is 
            // safe to use get_env("REMOTE_ADDR"), but $_SERVER["REMOTE_ADDR"] will be empty!
            //
            // See http://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=326163
            SetCgiEnvironmentVariables(cgi.StartInfo, request, file);

            if (request.Method == "POST")
            {
                string post = GetPostData(request.Form);

                cgi.StartInfo.RedirectStandardInput = true;
                cgi.Start();

                cgi.StandardInput.Write(post);
                cgi.StandardInput.Close();
            }
            else
            {
                cgi.Start();
            }

            output = cgi.StandardOutput.ReadToEnd();

            if (output.Trim() == "")
            {
                output = cgi.StandardError.ReadToEnd();
            }

            cgi.WaitForExit(30000);

            cgi.Close();

            return output;
        }

        /// <summary>
        /// Sets the cgi environment variables.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="request"></param>
        private static void SetCgiEnvironmentVariables(ProcessStartInfo info, IHttpRequest request, string path)
        {
            // not request-specific

            info.EnvironmentVariables["REQUEST_URI"] = request.QueryString["path"].Value;

            info.EnvironmentVariables["HTTP_ACCEPT_ENCODING"] = (request.Headers["Accept-Encoding"]) == null ? "" : request.Headers["Accept-Encoding"];

            // The name and version of the information server software answering the request 
            // (and running the gateway). Format: name/version 
            info.EnvironmentVariables["SERVER_SOFTWARE"] = "HTTPServer/1.1";

            // The server's hostname, DNS alias, or IP address as it would appear in self-referencing URLs.
            info.EnvironmentVariables["SERVER_NAME"] = "localhost";

            // The revision of the CGI specification to which this server complies. Format: CGI/revision
            info.EnvironmentVariables["GATEWAY_INTERFACE"] = "CGI/1.1";

            // Set to any value will prevent cgi (i.e. php) to show force-cgi-redirect security alert and quit.
            info.EnvironmentVariables["REDIRECT_STATUS"] = "OK";

            // variables specific to the request

            // The name and revision of the information protcol this request came in with. 
            // Format: protocol/revision
            info.EnvironmentVariables["SERVER_PROTOCOL"] = "HTTP/1.1";

            // The port number to which the request was sent.
            info.EnvironmentVariables["SERVER_PORT"] = "80"; //request.Server.Port.ToString();

            // The method with which the request was made. For HTTP, this is "GET", "HEAD", "POST", etc.
            info.EnvironmentVariables["REQUEST_METHOD"] = request.Method;

            // The extra path information, as given by the client. In other words, scripts can be accessed 
            // by their virtual pathname, followed by extra information at the end of this path. The extra 
            // information is sent as PATH_INFO. This information should be decoded by the server if it comes 
            // from a URL before it is passed to the CGI script.
            info.EnvironmentVariables["PATH_INFO"] = path;

            // The server provides a translated version of PATH_INFO, which takes the path and does any 
            // virtual-to-physical mapping to it.
            info.EnvironmentVariables["PATH_TRANSLATED"] = path;

            // A virtual path to the script being executed, used for self-referencing URLs.
            info.EnvironmentVariables["SCRIPT_NAME"] = Path.GetFileName(path);

            // A virtual path to the script being executed, used for self-referencing URLs.
            info.EnvironmentVariables["SCRIPT_FILENAME"] = path;

            // The information which follows the ? in the URL which referenced this script. This is the 
            // query information. It should not be decoded in any fashion. This variable should always be 
            // set when there is query information, regardless of command line decoding.
            string query = request.Uri.Query;
            if (query.StartsWith("?")) query = query.Substring(1);
            info.EnvironmentVariables["QUERY_STRING"] = query;

            // If the server supports user authentication, and the script is protects, this is the 
            // protocol-specific authentication method used to validate the user.
            info.EnvironmentVariables["AUTH_TYPE"] = "";

            // The hostname making the request. If the server does not have this information, it should set 
            // REMOTE_ADDR and leave this unset.
            info.EnvironmentVariables["REMOTE_HOST"] = "";

            // The IP address of the remote host making the request.
            info.EnvironmentVariables["REMOTE_ADDR"] = (request.Headers["remote_addr"]) == null ? "" : request.Headers["remote_addr"];

            // If the server supports user authentication, and the script is protected, this is the username 
            // they have authenticated as.
            info.EnvironmentVariables["REMOTE_USER"] = "";

            // If the HTTP server supports RFC 931 identification, then this variable will be set to the remote 
            // user name retrieved from the server. Usage of this variable should be limited to logging only.
            info.EnvironmentVariables["REMOTE_IDENT"] = "";

            // Remote port.
            info.EnvironmentVariables["REMOTE_PORT"] = (request.Headers["remote_port"]) == null ? "" : request.Headers["remote_port"];

            // For queries which have attached information, such as HTTP POST and PUT, this is the content 
            // type of the data.
            info.EnvironmentVariables["CONTENT_TYPE"] = (request.Headers["Content-Type"]) == null ? "" : request.Headers["Content-Type"];

            // The length of the said content as given by the client.
            if (request.Method == "POST")
            {
                string post = GetPostData(request.Form);

                info.EnvironmentVariables["CONTENT_LENGTH"] = post.Length.ToString();
            }
            else info.EnvironmentVariables["CONTENT_LENGTH"] = "0";

            // Other: REQUEST_METHOD, REQUEST_URI

            // header lines received from the client
            // TODO: Get all accepted types.
            info.EnvironmentVariables["HTTP_ACCEPT"] = request.AcceptTypes.Length > 0 ? request.AcceptTypes[0] : "text/html";
            info.EnvironmentVariables["HTTP_COOKIE"] = GetCookieString(request.Cookies);
            info.EnvironmentVariables["HTTP_USER_AGENT"] = (request.Headers["User-Agent"]) == null ? "" : request.Headers["User-Agent"];
            info.EnvironmentVariables["HTTP_REFERER"] = (request.Headers["Referer"]) == null ? "" : request.Headers["Referer"];
        }

        private static string GetPostData(HttpForm form)
        {
            StringBuilder post = new StringBuilder();

            foreach (HttpInputItem item in form)
            {
                post.AppendFormat("{0}={1}&", item.Name, item.Value);
            }

            string result = post.ToString();

            if (result.EndsWith("&"))
            {
                result = result.Substring(0, result.Length - 1);
            }

            return result;
        }


        /// <summary>
        /// Prepares a string with cookie data for HTTP_COOKIE cgi variable.
        /// </summary>
        /// <param name="cookies"></param>
        /// <returns></returns>
        private static string GetCookieString(RequestCookies cookies)
        {
            StringBuilder cookie = new StringBuilder();

            foreach (RequestCookie c in cookies)
            {
                cookie.AppendFormat("{0}={1};", c.Name, c.Value);
            }

            string result = cookie.ToString();

            if (result.EndsWith(";"))
            {
                result = result.Substring(0, result.Length - 1);
            }

            return result;
        }
    }


    /// <summary>
    /// The purpose of this module is to serve files.
    /// </summary>
    internal class Index
    {
        string _template;
        string _entry;

        public Index()
        {
            SimpleTemplate();
        }

        public Index(string templatePath)
        {
            if (File.Exists(templatePath + "template-index.html"))
            {
                _template = File.ReadAllText(templatePath + "template-index.html", Encoding.Default);

                int start = _template.IndexOf("[EntryStart]") + 12;
                int length = _template.IndexOf("[EntryEnd]") - start;

                if (start >= 0 && length > 0) _entry = _template.Substring(start, length);

                _template = _template.Remove(start - 12, length + 12 + 10);
                _template = _template.Insert(start - 12, "[Entry]");
            }
            else SimpleTemplate();
        }

        public string GetTemplate(string uri, string path, string server, string time)
        {
            string template = _template.Replace("{Uri}", uri);

            template = template.Replace("{PathUp}", path);
            template = template.Replace("{Visible}", (path.Length > 0) ? "" : " style=\"display:none;\"");
            template = template.Replace("{ServerName}", server);

            return template.Replace("{ServerTime}", time);
        }

        public string GetEntry(string path, string name, string size, string date, string className)
        {
            string entry = _entry.Replace("{Path}", path);

            entry = entry.Replace("{Name}", name);
            entry = entry.Replace("{Size}", size);
            entry = entry.Replace("{Date}", date);

            return entry.Replace("{Class}", className);
        }

        private void SimpleTemplate()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<html>");
            sb.AppendLine("<head>");
            sb.AppendLine("    <title>Index of {Uri}</title>");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");
            sb.AppendLine("    <p>Index of {Uri}</p>");
            sb.AppendLine("    <table>");
            sb.AppendLine("        <thead>");
            sb.AppendLine("            <tr>");
            sb.AppendLine("                <th style=\"width:60%\">Name</th>");
            sb.AppendLine("                <th style=\"width:20%\">Size</th>");
            sb.AppendLine("                <th style=\"width:20%\">Last modified</th>");
            sb.AppendLine("            </tr>");
            sb.AppendLine("        </thead>");
            sb.AppendLine("        <tbody>");
            sb.AppendLine("[Entry]");
            sb.AppendLine("        </tbody>");
            sb.AppendLine("    </table>");
            sb.AppendLine("    <p>{ServerName}, {ServerTime}</p>    </div>");
            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            _template = sb.ToString();


            sb = new StringBuilder();

            sb.AppendLine("            <tr>");
            sb.AppendLine("                <td><a href=\"{Path}\">{Name}</a></td>");
            sb.AppendLine("                <td>{Size}</td>");
            sb.AppendLine("                <td>{Date}</td>");
            sb.AppendLine("            </tr>");

            _entry = sb.ToString();
        }
    }
}