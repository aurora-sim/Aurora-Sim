/*
 * Copyright (c) Contributors, http://aurora-sim.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the Aurora-Sim Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

//Uses the following website for the creation of the encrpyted hashes
//http://www.obviex.com/samples/Encryption.aspx

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace Aurora.Framework
{
    public static class Utilities
    {
        private static string EncryptorType = "SHA1";
        private static int EncryptIterations = 2;
        private static int KeySize = 256;
        private static string CachedExternalIP = "";

        /// <summary>
        ///   Get the URL to the release notes for the current version of Aurora
        /// </summary>
        /// <returns></returns>
        public static string GetServerReleaseNotesURL()
        {
            return (MainServer.Instance.Secure ? "https://" : "http://") + GetExternalIp() +
                   ":" + MainServer.Instance.Port.ToString() + "/AuroraServerRelease" + AuroraServerVersion() + ".html";
        }

        /// <summary>
        ///   Get the URL to our sim
        /// </summary>
        /// <returns></returns>
        public static string GetAddress()
        {
            return (MainServer.Instance.Secure ? "https://" : "http://") + GetExternalIp() + ":" +
                   MainServer.Instance.Port.ToString();
        }

        /// <summary>
        ///   What is our version?
        /// </summary>
        /// <returns></returns>
        public static string AuroraServerVersion()
        {
            return "1.0";
        }

        public static void SetEncryptorType(string type)
        {
            if (type == "SHA1" || type == "MD5")
            {
                EncryptorType = type;
            }
        }

        /// <summary>
        ///   This is for encryption, it sets the number of times to iterate through the encryption
        /// </summary>
        /// <param name = "iterations"></param>
        public static void SetEncryptIterations(int iterations)
        {
            EncryptIterations = iterations;
        }

        /// <summary>
        ///   This is for encryption, it sets the size of the key
        /// </summary>
        /// <param name = "size"></param>
        public static void SetKeySize(int size)
        {
            KeySize = size;
        }

        /// <summary>
        ///   Encrypts specified plaintext using Rijndael symmetric key algorithm
        ///   and returns a base64-encoded result.
        /// </summary>
        /// <param name = "plainText">
        ///   Plaintext value to be encrypted.
        /// </param>
        /// <param name = "passPhrase">
        ///   Passphrase from which a pseudo-random password will be derived. The
        ///   derived password will be used to generate the encryption key.
        ///   Passphrase can be any string. In this example we assume that this
        ///   passphrase is an ASCII string.
        /// </param>
        /// <param name = "saltValue">
        ///   Salt value used along with passphrase to generate password. Salt can
        ///   be any string. In this example we assume that salt is an ASCII string.
        /// </param>
        /// <param name = "hashAlgorithm">
        ///   Hash algorithm used to generate password. Allowed values are: "MD5" and
        ///   "SHA1". SHA1 hashes are a bit slower, but more secure than MD5 hashes.
        /// </param>
        /// <param name = "passwordIterations">
        ///   Number of iterations used to generate password. One or two iterations
        ///   should be enough.
        /// </param>
        /// <param name = "initVector">
        ///   Initialization vector (or IV). This value is required to encrypt the
        ///   first block of plaintext data. For RijndaelManaged class IV must be 
        ///   exactly 16 ASCII characters long.
        /// </param>
        /// <param name = "keySize">
        ///   Size of encryption key in bits. Allowed values are: 128, 192, and 256. 
        ///   Longer keys are more secure than shorter keys.
        /// </param>
        /// <returns>
        ///   Encrypted value formatted as a base64-encoded string.
        /// </returns>
        public static string Encrypt(string plainText,
                                     string passPhrase,
                                     string saltValue)
        {
            // Convert strings into byte arrays.
            // Let us assume that strings only contain ASCII codes.
            // If strings include Unicode characters, use Unicode, UTF7, or UTF8 
            // encoding.
            byte[] initVectorBytes = Encoding.ASCII.GetBytes("@IBAg3D4e5E6g7H5");
            byte[] saltValueBytes = Encoding.ASCII.GetBytes(saltValue);

            // Convert our plaintext into a byte array.
            // Let us assume that plaintext contains UTF8-encoded characters.
            byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);

            // First, we must create a password, from which the key will be derived.
            // This password will be generated from the specified passphrase and 
            // salt value. The password will be created using the specified hash 
            // algorithm. Password creation can be done in several iterations.
            PasswordDeriveBytes password = new PasswordDeriveBytes(
                passPhrase,
                saltValueBytes,
                EncryptorType,
                EncryptIterations);

            // Use the password to generate pseudo-random bytes for the encryption
            // key. Specify the size of the key in bytes (instead of bits).
            byte[] keyBytes = password.GetBytes(KeySize/8);

            // Create uninitialized Rijndael encryption object.
            RijndaelManaged symmetricKey = new RijndaelManaged {Mode = CipherMode.CBC};

            // It is reasonable to set encryption mode to Cipher Block Chaining
            // (CBC). Use default options for other symmetric key parameters.

            // Generate encryptor from the existing key bytes and initialization 
            // vector. Key size will be defined based on the number of the key 
            // bytes.
            ICryptoTransform encryptor = symmetricKey.CreateEncryptor(
                keyBytes,
                initVectorBytes);

            // Define memory stream which will be used to hold encrypted data.
            MemoryStream memoryStream = new MemoryStream();

            // Define cryptographic stream (always use Write mode for encryption).
            CryptoStream cryptoStream = new CryptoStream(memoryStream,
                                                         encryptor,
                                                         CryptoStreamMode.Write);
            // Start encrypting.
            cryptoStream.Write(plainTextBytes, 0, plainTextBytes.Length);

            // Finish encrypting.
            cryptoStream.FlushFinalBlock();

            // Convert our encrypted data from a memory stream into a byte array.
            byte[] cipherTextBytes = memoryStream.ToArray();

            // Close both streams.
            memoryStream.Close();
            cryptoStream.Close();

            // Convert encrypted data into a base64-encoded string.
            string cipherText = Convert.ToBase64String(cipherTextBytes);

            // Return encrypted string.
            return cipherText;
        }

        /// <summary>
        ///   Decrypts specified ciphertext using Rijndael symmetric key algorithm.
        /// </summary>
        /// <param name = "cipherText">
        ///   Base64-formatted ciphertext value.
        /// </param>
        /// <param name = "passPhrase">
        ///   Passphrase from which a pseudo-random password will be derived. The
        ///   derived password will be used to generate the encryption key.
        ///   Passphrase can be any string. In this example we assume that this
        ///   passphrase is an ASCII string.
        /// </param>
        /// <param name = "saltValue">
        ///   Salt value used along with passphrase to generate password. Salt can
        ///   be any string. In this example we assume that salt is an ASCII string.
        /// </param>
        /// <param name = "hashAlgorithm">
        ///   Hash algorithm used to generate password. Allowed values are: "MD5" and
        ///   "SHA1". SHA1 hashes are a bit slower, but more secure than MD5 hashes.
        /// </param>
        /// <param name = "passwordIterations">
        ///   Number of iterations used to generate password. One or two iterations
        ///   should be enough.
        /// </param>
        /// <param name = "initVector">
        ///   Initialization vector (or IV). This value is required to encrypt the
        ///   first block of plaintext data. For RijndaelManaged class IV must be
        ///   exactly 16 ASCII characters long.
        /// </param>
        /// <param name = "keySize">
        ///   Size of encryption key in bits. Allowed values are: 128, 192, and 256.
        ///   Longer keys are more secure than shorter keys.
        /// </param>
        /// <returns>
        ///   Decrypted string value.
        /// </returns>
        /// <remarks>
        ///   Most of the logic in this function is similar to the Encrypt
        ///   logic. In order for decryption to work, all parameters of this function
        ///   - except cipherText value - must match the corresponding parameters of
        ///   the Encrypt function which was called to generate the
        ///   ciphertext.
        /// </remarks>
        public static string Decrypt(string cipherText,
                                     string passPhrase,
                                     string saltValue)
        {
            // Convert strings defining encryption key characteristics into byte
            // arrays. Let us assume that strings only contain ASCII codes.
            // If strings include Unicode characters, use Unicode, UTF7, or UTF8
            // encoding.
            byte[] initVectorBytes = Encoding.ASCII.GetBytes("@IBAg3D4e5E6g7H5");
            byte[] saltValueBytes = Encoding.ASCII.GetBytes(saltValue);

            // Convert our ciphertext into a byte array.
            byte[] cipherTextBytes = Convert.FromBase64String(cipherText);

            // First, we must create a password, from which the key will be 
            // derived. This password will be generated from the specified 
            // passphrase and salt value. The password will be created using
            // the specified hash algorithm. Password creation can be done in
            // several iterations.
            PasswordDeriveBytes password = new PasswordDeriveBytes(
                passPhrase,
                saltValueBytes,
                EncryptorType,
                EncryptIterations);

            // Use the password to generate pseudo-random bytes for the encryption
            // key. Specify the size of the key in bytes (instead of bits).
            byte[] keyBytes = password.GetBytes(KeySize/8);

            // Create uninitialized Rijndael encryption object.
            RijndaelManaged symmetricKey = new RijndaelManaged {Mode = CipherMode.CBC};

            // It is reasonable to set encryption mode to Cipher Block Chaining
            // (CBC). Use default options for other symmetric key parameters.

            // Generate decryptor from the existing key bytes and initialization 
            // vector. Key size will be defined based on the number of the key 
            // bytes.
            ICryptoTransform decryptor = symmetricKey.CreateDecryptor(
                keyBytes,
                initVectorBytes);

            // Define memory stream which will be used to hold encrypted data.
            MemoryStream memoryStream = new MemoryStream(cipherTextBytes);

            // Define cryptographic stream (always use Read mode for encryption).
            CryptoStream cryptoStream = new CryptoStream(memoryStream,
                                                         decryptor,
                                                         CryptoStreamMode.Read);

            // Since at this point we don't know what the size of decrypted data
            // will be, allocate the buffer long enough to hold ciphertext;
            // plaintext is never longer than ciphertext.
            byte[] plainTextBytes = new byte[cipherTextBytes.Length];

            // Start decrypting.
            int decryptedByteCount = 0;
            try
            {
                decryptedByteCount = cryptoStream.Read(plainTextBytes,
                                                       0,
                                                       plainTextBytes.Length);
            }
            catch (Exception)
            {
                return "";
            }

            // Close both streams.
            memoryStream.Close();
            cryptoStream.Close();

            // Convert decrypted data into a string. 
            // Let us assume that the original plaintext string was UTF8-encoded.
            string plainText = Encoding.UTF8.GetString(plainTextBytes,
                                                       0,
                                                       decryptedByteCount);

            // Return decrypted string.   
            return plainText;
        }

        /// <summary>
        ///   Get OUR external IP
        /// </summary>
        /// <returns></returns>
        public static string GetExternalIp()
        {
            if (CachedExternalIP == "")
            {
                // External IP Address (get your external IP locally)
                String externalIp = "";
                UTF8Encoding utf8 = new UTF8Encoding();

                WebClient webClient = new WebClient();
                try
                {
                    //Ask what is my ip for it
                    externalIp = utf8.GetString(webClient.DownloadData("http://checkip.dyndns.org/"));
                    //Remove the HTML stuff
                    externalIp =
                        externalIp.Remove(0, 76).Split(new string[1] {"</body>"}, StringSplitOptions.RemoveEmptyEntries)
                            [0];
                    NetworkUtils.InternetSuccess();
                }
                catch (Exception)
                {
                    try
                    {
                        externalIp =
                            utf8.GetString(webClient.DownloadData("http://automation.whatismyip.com/n09230945.asp"));
                        NetworkUtils.InternetSuccess();
                    }
                    catch (Exception iex)
                    {
                        NetworkUtils.InternetFailure();
                        MainConsole.Instance.Error("[Utilities]: Failed to get external IP, " + iex +
                                    ", please check your internet connection (if this applies), setting to internal...");
                        externalIp = "127.0.0.1";
                    }
                }
                CachedExternalIP = externalIp;
                return externalIp;
            }
            else
                return CachedExternalIP;
        }

        /// <summary>
        ///   Read a website into a string
        /// </summary>
        /// <param name = "URL">URL to change into a string</param>
        /// <returns></returns>
        public static string ReadExternalWebsite(string URL)
        {
            String website = "";
            UTF8Encoding utf8 = new UTF8Encoding();

            WebClient webClient = new WebClient();
            if (NetworkUtils.CheckInternetConnection())
            {
                try
                {
                    byte[] bytes = webClient.DownloadData(URL);
                    website =
                        utf8.GetString(webClient.ResponseHeaders["Content-Encoding"] == "gzip"
                                           ? UnGzip(bytes, 0)
                                           : bytes);
                    NetworkUtils.InternetSuccess();
                }
                catch (Exception)
                {
                    NetworkUtils.InternetFailure();
                }
            }
            return website;
        }

        private static byte[] UnGzip(byte[] data, int start)
        {
            int size = BitConverter.ToInt32(data, data.Length - 4);
            byte[] uncompressedData = new byte[size];
            MemoryStream memStream = new MemoryStream(data, start, (data.Length - start)) {Position = 0};
            GZipStream gzStream = new GZipStream(memStream, CompressionMode.Decompress);

            try
            {
                gzStream.Read(uncompressedData, 0, size);
            }
            catch (Exception)
            {
                throw;
            }

            gzStream.Close();
            return uncompressedData;
        }


        /// <summary>
        ///   Download the file from downloadLink and save it to Aurora + Version +
        /// </summary>
        /// <param name = "downloadLink">Link to the download</param>
        /// <param name = "filename">Name to put the download in</param>
        public static void DownloadFile(string downloadLink, string filename)
        {
            WebClient webClient = new WebClient();
            try
            {
                MainConsole.Instance.Warn("Downloading new file from " + downloadLink + " now into file " + filename + ".");
                webClient.DownloadFile(downloadLink, filename);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Downloads a file async
        /// </summary>
        /// <param name="downloadLink"></param>
        /// <param name="filename"></param>
        /// <param name="onProgress">can be null</param>
        /// <param name="onComplete">can be null</param>
        public static void DownloadFileAsync(string downloadLink, string filename, DownloadProgressChangedEventHandler onProgress, AsyncCompletedEventHandler onComplete)
        {
            WebClient webClient = new WebClient();
            try
            {
                MainConsole.Instance.Warn("Downloading new file from " + downloadLink + " now into file " + filename + ".");
                if (onProgress != null)
                    webClient.DownloadProgressChanged += onProgress;
                if (onComplete != null)
                    webClient.DownloadFileCompleted += onComplete;
                webClient.DownloadFileAsync(new Uri(downloadLink), filename);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        ///   Bring up a popup with a text box to ask the user for some input
        /// </summary>
        /// <param name = "title"></param>
        /// <param name = "promptText"></param>
        /// <param name = "value"></param>
        /// <returns></returns>
        public static DialogResult InputBox(string title, string promptText, ref string value)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] {label, textBox, buttonOk, buttonCancel});
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            return dialogResult;
        }

        /// <summary>
        ///   Bring up a popup with a text box to ask the user for some input
        /// </summary>
        /// <param name = "title"></param>
        /// <param name = "promptText"></param>
        /// <returns></returns>
        public static DialogResult InputBox(string title, string promptText)
        {
            Form form = new Form();
            Label label = new Label();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] {label, buttonOk, buttonCancel});
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            return dialogResult;
        }
    }
}