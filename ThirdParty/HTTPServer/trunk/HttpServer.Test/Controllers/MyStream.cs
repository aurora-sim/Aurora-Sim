using System;
using System.IO;
using System.Threading;

namespace HttpServer.Test.Controllers
{
    public class MyStream : Stream
    {
        private byte[] _buffer = new byte[8192];
        private bool _canRead = true;
        private bool _canSeek = true;
        private bool _canWrite = true;
        private ManualResetEvent _event = new ManualResetEvent(false);
        private long _offset;
        private long _size;

        ///<summary>
        ///When overridden in a derived class, gets a value indicating whether the current stream supports reading.
        ///</summary>
        ///
        ///<returns>
        ///true if the stream supports reading; otherwise, false.
        ///</returns>
        ///<filterpriority>1</filterpriority>
        public override bool CanRead
        {
            get { return _canRead; }
        }

        ///<summary>
        ///When overridden in a derived class, gets a value indicating whether the current stream supports seeking.
        ///</summary>
        ///
        ///<returns>
        ///true if the stream supports seeking; otherwise, false.
        ///</returns>
        ///<filterpriority>1</filterpriority>
        public override bool CanSeek
        {
            get { return _canSeek; }
        }

        ///<summary>
        ///When overridden in a derived class, gets a value indicating whether the current stream supports writing.
        ///</summary>
        ///
        ///<returns>
        ///true if the stream supports writing; otherwise, false.
        ///</returns>
        ///<filterpriority>1</filterpriority>
        public override bool CanWrite
        {
            get { return _canWrite; }
        }

        ///<summary>
        ///When overridden in a derived class, gets the length in bytes of the stream.
        ///</summary>
        ///
        ///<returns>
        ///A long value representing the length of the stream in bytes.
        ///</returns>
        ///
        ///<exception cref="T:System.NotSupportedException">A class derived from Stream does not support seeking. </exception>
        ///<exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception><filterpriority>1</filterpriority>
        public override long Length
        {
            get { return _size; }
        }

        ///<summary>
        ///When overridden in a derived class, gets or sets the position within the current stream.
        ///</summary>
        ///
        ///<returns>
        ///The current position within the stream.
        ///</returns>
        ///
        ///<exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        ///<exception cref="T:System.NotSupportedException">The stream does not support seeking. </exception>
        ///<exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception><filterpriority>1</filterpriority>
        public override long Position
        {
            get { return _offset; }
            set { _offset = value; }
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback,
                                               object state)
        {
            _event.Reset();
            AsyncRes res = new AsyncRes(buffer, offset, count);
            callback.BeginInvoke(res, null, state);
            return res;
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            _event.WaitOne();
            AsyncRes res = (AsyncRes) asyncResult;
            return Read(res.Buffer, res.Offset, res.Count);
        }

        ///<summary>
        ///When overridden in a derived class, clears all buffers for this stream and causes any buffered data to be written to the underlying device.
        ///</summary>
        ///
        ///<exception cref="T:System.IO.IOException">An I/O error occurs. </exception><filterpriority>2</filterpriority>
        public override void Flush()
        {
            _offset = 0;
            _size = 0;
        }

        ///<summary>
        ///When overridden in a derived class, reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
        ///</summary>
        ///
        ///<returns>
        ///The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.
        ///</returns>
        ///
        ///<param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream. </param>
        ///<param name="count">The maximum number of bytes to be read from the current stream. </param>
        ///<param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source. </param>
        ///<exception cref="T:System.ArgumentException">The sum of offset and count is larger than the buffer length. </exception>
        ///<exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
        ///<exception cref="T:System.NotSupportedException">The stream does not support reading. </exception>
        ///<exception cref="T:System.ArgumentNullException">buffer is null. </exception>
        ///<exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        ///<exception cref="T:System.ArgumentOutOfRangeException">offset or count is negative. </exception><filterpriority>1</filterpriority>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count > _size)
                count = (int) _size;

            _size -= count;
            lock (_buffer)
				Buffer.BlockCopy(_buffer, 0, buffer, offset, count);
            return count;
        }

        ///<summary>
        ///When overridden in a derived class, sets the position within the current stream.
        ///</summary>
        ///
        ///<returns>
        ///The new position within the current stream.
        ///</returns>
        ///
        ///<param name="offset">A byte offset relative to the origin parameter. </param>
        ///<param name="origin">A value of type <see cref="T:System.IO.SeekOrigin"></see> indicating the reference point used to obtain the new position. </param>
        ///<exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        ///<exception cref="T:System.NotSupportedException">The stream does not support seeking, such as if the stream is constructed from a pipe or console output. </exception>
        ///<exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception><filterpriority>1</filterpriority>
        public override long Seek(long offset, SeekOrigin origin)
        {
            if (origin == SeekOrigin.Begin)
                _offset = offset;
            else if (origin == SeekOrigin.Current)
                _offset += offset;
            else
                _offset = _size - offset;

            return _offset;
        }

        ///<summary>
        ///When overridden in a derived class, sets the length of the current stream.
        ///</summary>
        ///
        ///<param name="value">The desired length of the current stream in bytes. </param>
        ///<exception cref="T:System.NotSupportedException">The stream does not support both writing and seeking, such as if the stream is constructed from a pipe or console output. </exception>
        ///<exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        ///<exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception><filterpriority>2</filterpriority>
        public override void SetLength(long value)
        {
            lock (_buffer)
                if (_buffer.Length < value)
                {
                    byte[] newbuffer = new byte[value];
					Buffer.BlockCopy(_buffer, 0, newbuffer, 0, (int)_size);
                    _buffer = newbuffer;
                }

            _size = value;
        }

        public void Signal()
        {
            _event.Set();
        }

        ///<summary>
        ///When overridden in a derived class, writes a sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
        ///</summary>
        ///
        ///<param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream. </param>
        ///<param name="count">The number of bytes to be written to the current stream. </param>
        ///<param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream. </param>
        ///<exception cref="T:System.IO.IOException">An I/O error occurs. </exception>
        ///<exception cref="T:System.NotSupportedException">The stream does not support writing. </exception>
        ///<exception cref="T:System.ObjectDisposedException">Methods were called after the stream was closed. </exception>
        ///<exception cref="T:System.ArgumentNullException">buffer is null. </exception>
        ///<exception cref="T:System.ArgumentException">The sum of offset and count is greater than the buffer length. </exception>
        ///<exception cref="T:System.ArgumentOutOfRangeException">offset or count is negative. </exception><filterpriority>1</filterpriority>
        public override void Write(byte[] buffer, int offset, int count)
        {
            _size += count;
            lock (_buffer)
				Buffer.BlockCopy(buffer, 0, _buffer, offset, count);
        }

        #region Nested type: AsyncRes

        public class AsyncRes : IAsyncResult
        {
            private readonly WaitHandle _asyncWaitHandle = new ManualResetEvent(false);
            private readonly bool _completedSynchronously = true;
            private readonly bool _isCompleted = true;
            private object _asyncState;
            private byte[] _buffer;
            private int _count;
            private int _offset;

            public AsyncRes(byte[] buffer, int offset, int cound)
            {
                _count = cound;
                _offset = offset;
                _buffer = buffer;
            }

            public byte[] Buffer
            {
                get { return _buffer; }
            }

            public int Count
            {
                get { return _count; }
                set { _count = value; }
            }

            public int Offset
            {
                get { return _offset; }
                set { _offset = value; }
            }

            #region IAsyncResult Members

            ///<summary>
            ///Gets an indication whether the asynchronous operation has completed.
            ///</summary>
            ///
            ///<returns>
            ///true if the operation is complete; otherwise, false.
            ///</returns>
            ///<filterpriority>2</filterpriority>
            public bool IsCompleted
            {
                get { return _isCompleted; }
            }

            ///<summary>
            ///Gets a <see cref="T:System.Threading.WaitHandle"></see> that is used to wait for an asynchronous operation to complete.
            ///</summary>
            ///
            ///<returns>
            ///A <see cref="T:System.Threading.WaitHandle"></see> that is used to wait for an asynchronous operation to complete.
            ///</returns>
            ///<filterpriority>2</filterpriority>
            public WaitHandle AsyncWaitHandle
            {
                get { return _asyncWaitHandle; }
            }


            ///<summary>
            ///Gets an indication of whether the asynchronous operation completed synchronously.
            ///</summary>
            ///
            ///<returns>
            ///true if the asynchronous operation completed synchronously; otherwise, false.
            ///</returns>
            ///<filterpriority>2</filterpriority>
            public bool CompletedSynchronously
            {
                get { return _completedSynchronously; }
            }

            public object AsyncState
            {
                get { return _asyncState; }
                set { _asyncState = value; }
            }

            #endregion
        }

        #endregion
    }
}