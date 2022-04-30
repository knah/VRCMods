using System;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;
using AdvancedSafety.BundleVerifier.RestrictedProcessRunner;
using MelonLoader;

namespace AdvancedSafety.BundleVerifier
{
    internal class BundleDlContext : IDisposable
    {
        internal readonly IntPtr OriginalBundleDownload;
        
        private MemoryMappedFile myMemoryMap;
        private MemoryMapWriterStream myWriterStream;
        private BundleVerifierProcessHandle myVerifierProcess;
        internal readonly string Url;
        internal readonly bool IsBadUrl;
        
        public BundleDlContext(IntPtr originalBundleDownload, string url)
        {
            OriginalBundleDownload = originalBundleDownload;
            Url = url;
            IsBadUrl = BundleVerifierMod.BadBundleCache?.Contains(url) == true;
        }

        internal bool PreProcessBytes()
        {
            if (myMemoryMap != null) return true;

            var declaredSize = BundleDlInterceptor.GetTotalSize(OriginalBundleDownload);
            if (declaredSize <= 0) return false;
            
            try
            {
                var memName = "BundleVerifier-" + Guid.NewGuid();
                myMemoryMap = MemoryMappedFile.CreateNew(memName, declaredSize + 8,
                    MemoryMappedFileAccess.ReadWrite, MemoryMappedFileOptions.None, HandleInheritability.None);
                myWriterStream = new MemoryMapWriterStream(myMemoryMap);
                myWriterStream.SetLength(declaredSize);


                myVerifierProcess = new BundleVerifierProcessHandle(BundleVerifierMod.BundleVerifierPath, memName,
                    TimeSpan.FromSeconds(BundleVerifierMod.TimeLimit.Value),
                    (ulong)BundleVerifierMod.MemoryLimit.Value * 1024L * 1024L, 20,
                    BundleVerifierMod.ComponentLimit.Value);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Error while initializing verifier internals: {ex}");
                return false;
            }

            return true;
        }

        internal int ProcessBytes(byte[] bytes, int offset, int length)
        {
            try
            {
                myWriterStream.Write(bytes, offset, length);
            }
            catch (IOException ex)
            {
                MelonLogger.Error($"Received more bytes than declared for bundle URL {Url} (declared: {BundleDlInterceptor.GetTotalSize(OriginalBundleDownload)})");
                MelonLogger.Error(ex.ToString());
                DoBackSpew();
                unsafe
                {
                    fixed (byte* bytesPtr = bytes)
                        BundleDownloadMethods.OriginalReceiveBytes(OriginalBundleDownload, (IntPtr)bytesPtr, length);
                }

                BundleDlInterceptor.CancelIntercept(this);
            }

            return length;
        }

        internal long GetDownloadedSize()
        {
            return myWriterStream.Position;
        }

        internal void CompleteDownload()
        {
            if (myMemoryMap == null)
            {
                MelonDebug.Msg($"Did not succ any bytes for ptr {OriginalBundleDownload}, just completing");
                BundleDownloadMethods.OriginalCompleteDownload(OriginalBundleDownload);
                return;
            }
            
            MelonDebug.Msg($"Succed {myWriterStream.Position} bytes out of declared {BundleDlInterceptor.GetTotalSize(OriginalBundleDownload)}; waiting for victim process");

            var stopwatch = Stopwatch.StartNew();
            var exitCode = myVerifierProcess.WaitForExit(TimeSpan.FromSeconds(BundleVerifierMod.TimeLimit.Value));
            MelonDebug.Msg($"Process wait done after {stopwatch.ElapsedMilliseconds}ms extra wait");
            if (exitCode != 0)
            {
                var cleanedUrl = BundleVerifierMod.SanitizeUrl(Url);
                MelonLogger.Msg($"Verifier process failed with exit code {exitCode} for bundle uid={cleanedUrl.Item1}+{cleanedUrl.Item2}");
                BundleVerifierMod.BadBundleCache.Add(Url);
                MelonDebug.Msg("Reporting completion without data");
                // feed some garbage into it, otherwise it dies
                unsafe
                {
                    *(long*)(OriginalBundleDownload + 0x40) = 0;
                    var stub = "UnityFS\0";
                    var bytes = Encoding.UTF8.GetBytes(stub);
                    fixed (byte* bytesPtr = bytes)
                        BundleDownloadMethods.OriginalReceiveBytes(OriginalBundleDownload, (IntPtr) bytesPtr, bytes.Length);
                }

                BundleDownloadMethods.OriginalCompleteDownload(OriginalBundleDownload);
                return;
            }

            MelonDebug.Msg("Bundle looks clean, spewing back...");
            DoBackSpew();
            MelonDebug.Msg("Back-spew done, completing");
            BundleDownloadMethods.OriginalCompleteDownload(OriginalBundleDownload);
            MelonDebug.Msg("Completed!");
        }

        private unsafe void DoBackSpew()
        {
            var returnSizeStep = 65536;
            // reset it back to zero
            *(long*)(OriginalBundleDownload + 0x40) = 0;

            var rawPointer = myWriterStream.GetPointer() + 8;
            var currentPosition = 0;
            var totalLength = (int)myWriterStream.Length;

            while (currentPosition < totalLength)
            {
                var currentRead = Math.Min(returnSizeStep, totalLength - currentPosition);
                var bytesConsumed = BundleDownloadMethods.OriginalReceiveBytes(OriginalBundleDownload,
                    (IntPtr)(rawPointer + currentPosition), currentRead);
                currentPosition += currentRead;

                if (bytesConsumed != currentRead)
                {
                    // The thing refused to eat our data?
                    break;
                }
            }

            myWriterStream.ReleasePointer();
        }


        public void Dispose()
        {
            myVerifierProcess?.Dispose();
            myWriterStream?.Dispose();
            myMemoryMap?.Dispose();
        }
    }
}