using System;

namespace Archive.Interface
{
    public readonly struct ArchiveEntry
    {
        public string Path { get; }
        public long Size { get; }
        public long CompressedSize { get; }
        public bool IsDirectory { get; }
        public DateTime LastModified { get; }

        public ArchiveEntry(string path, long size, long compressedSize, bool isDirectory, DateTime lastModified)
        {
            Path = path;
            Size = size;
            CompressedSize = compressedSize;
            IsDirectory = isDirectory;
            LastModified = lastModified;
        }
    }
}