using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace AdvancedSafety.BundleVerifier.RestrictedProcessRunner
{
    public class MemoryMapWriterStream : Stream
    {
        private readonly MemoryMappedViewAccessor myView;
        private int myPosition;

        public MemoryMapWriterStream(MemoryMappedFile mapFile)
        {
            myView = mapFile.CreateViewAccessor();
        }

        public override void Flush() { }
        
        internal unsafe byte* GetPointer()
        {
            byte* ptr = null;
            myView.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
            return ptr;
        }

        internal void ReleasePointer() => myView.SafeMemoryMappedViewHandle.ReleasePointer();

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => myView.Write(0, (int) value);

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count + myPosition > myView.Capacity - 8)
                throw new IOException($"Stream is full (capacity {myView.Capacity}, requires {count + myPosition}");
            
            myView.WriteArray(myPosition + 8, buffer, offset, count);
            myPosition += count;
            Interlocked.MemoryBarrier();
            myView.Write(4, myPosition);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                myView.Dispose();
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => Position;
        public override long Position { get => myPosition; set => throw new NotSupportedException(); }
    }
}