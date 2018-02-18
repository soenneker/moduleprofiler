using System;
using System.IO;
using System.Text;

namespace ModuleProfiler.Module
{
    /// <summary>
    /// Allows for hooking into the request's stream via a filter to analyze and modify it.
    /// </summary>
    public class StreamCapture : Stream
    {
        private readonly Stream _base;
        private readonly MemoryStream _memoryStream = new MemoryStream();

        public StreamCapture(Stream stream)
        {
            _base = stream;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _base.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _memoryStream.Write(buffer, offset, count);
            _base.Write(buffer, offset, count);
        }

        public override long Length => _memoryStream.Length;

        public override string ToString()
        {
            return Encoding.UTF8.GetString(_memoryStream.ToArray());
        }

        public override void Flush()
        {
            _base.Flush();
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override long Position
        {
            get => throw new NotImplementedException();
            set => throw new NotImplementedException();
        }
    }
}