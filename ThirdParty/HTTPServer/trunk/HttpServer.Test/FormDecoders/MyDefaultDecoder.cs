using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using HttpServer.FormDecoders;
using HttpServer;

namespace HttpServer.Test.FormDecoders
{
    class MyDefaultDecoder : IFormDecoder
    {
        private bool _called;

        public bool Called
        {
            get { return _called; }
            set { _called = value; }
        }

        #region IFormDecoder Members

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream">Stream containing the content</param>
        /// <param name="contentType">Content type (with any additional info like boundry). Content type is always supplied in lower case</param>
        /// <param name="encoding">Stream enconding</param>
        /// <returns>A http form, or null if content could not be parsed.</returns>
        /// <exception cref="InvalidDataException">If contents in the stream is not valid input data.</exception>
        public HttpForm Decode(Stream stream, string contentType, Encoding encoding)
        {
            _called = true;
			return HttpForm.EmptyForm;
        }

        /// <summary>
        /// Checks if the decoder can handle the mime type
        /// </summary>
        /// <param name="contentType">Content type (with any additional info like boundry). Content type is always supplied in lower case.</param>
        /// <returns>True if the decoder can parse the specified content type</returns>
        public bool CanParse(string contentType)
        {
            return string.IsNullOrEmpty(contentType);
        }

        #endregion
    }
}
