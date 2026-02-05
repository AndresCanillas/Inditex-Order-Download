using Newtonsoft.Json;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Services.Core
{
    public sealed class RecordQueueReadResult<T> where T : class, new()
    {
        public int Count { get; set; }

        public List<T> Records { get; set; } = new List<T>();

        /// <summary>
        /// Absolute file offset of the next record to read; -1 if none available.
        /// </summary>
        public long NextOffset { get; set; }
    }

    // Some notes on how SQDataFile works:
    //  - File is meant to access data in sequential order from beggining to end. The class is safe for multi-threading (via exclusive lock), this means multiple
    //    threads can use the same object instance concurrently, but should be limited to a single writer/single reader.
    //  - Data is organized as follows:
    //      1) Header (first 20 bytes). Contains version number (8 bytes), head pointer (8 bytes), and row count (4 bytes).
    //      2) After the header comes the data (records).
    //      3) Each record includes a record marker (4 bytes) and its length (4 bytes), followed by the record payload (N bytes).
    //  - This structure allows to detect and fix errors in the file (unlikely, but possible if process crashes while writing to the file).
    //  - Usage pattern is as follows:
    //      1) One thread (producer) can write to the file (Records are always appended at the end of the file).
    //      2) Another thread (consumer) can read/discard entries from the file.
    //  - If entries are not discarded after being read, they will be read again the next time you call ReadEntries, this is meant to be able to retry as many times as
    //    necesary (for as long as you do not discard them).
    //  - As records are written, read and discarded, the file will be automatically shrinked once it hits a size threshold (50 MB by default).
    //  - Also, since we want to prevent the file from growing without control, we can also specify the maximum number of records that the file can hold (200,000 by default).
    //    Once we hit the maximum number of records, older records will start being deleted to make room for newer ones.
    public sealed class SQDataFile<T> where T : class, new()
    {
        private const string VERSION_TEXT = "VRECQ1/0";
        private const int VERSION_SIZE = 8;
        private const int HEADER_SIZE = 20;
        private const string RECORD_MARKER = "\x06rec";
        private const int RECORD_MARKER_SIZE = 4;
        private const int LENGTH_SIZE = 4;

        private readonly object syncObj = new object();
        private readonly string path;
        private readonly long compactThresholdBytes;
        private readonly int maxRows;

        public SQDataFile(string path, long compactThresholdBytes = 50 * 1024 * 1024, int maxRows = 10000)
        {
            this.path = path ??
                throw new ArgumentNullException(nameof(path));

            if(compactThresholdBytes < 10000)
                throw new InvalidOperationException("CompactThresholdBytes cannot be smaller than 10,000");

            if(maxRows < 1000)
                throw new InvalidOperationException("MaxRows cannot be smaller than 1,000");

            this.compactThresholdBytes = compactThresholdBytes;
            this.maxRows = maxRows;

            InternalCheckFile();
        }

        public int GetRowCount()
        {
            lock(syncObj)
            {
                using(var stream = Open(path, FileMode.Open, FileAccess.Read))
                {
                    ReadHeader(stream, out _, out var head, out var rowCount);
                    return rowCount;
                }
            }
        }

        /// <summary>
        /// Reads up to <paramref name="count"/> records starting from the current head.
        /// Does not modify the file. Returned result includes the offset of the next record to read (or -1 if we reached the end of the file).
        /// </summary>
        public RecordQueueReadResult<T> ReadEntries(int count)
        {
            lock(syncObj)
            {
                using(var stream = Open(path, FileMode.Open, FileAccess.Read))
                {
                    ReadHeader(stream, out _, out var head, out _);

                    if(head < 0 || head >= stream.Length)
                    {
                        return new RecordQueueReadResult<T>
                        {
                            Count = 0,
                            Records = new List<T>(),
                            NextOffset = -1
                        };
                    }

                    if(count <= 0)
                    {
                        return new RecordQueueReadResult<T>
                        {
                            Count = 0,
                            Records = new List<T>(),
                            NextOffset = head
                        };
                    }

                    stream.Position = head;
                    var (items, nextOffset) = InternalReadRecords(stream, count);
                    return new RecordQueueReadResult<T>
                    {
                        Count = items.Count,
                        Records = items,
                        NextOffset = nextOffset
                    };
                }
            }
        }

        /// <summary>
        /// After consuming a prior ReadEntries result, advances the head to its NextOffset (or -1).
        /// </summary>
        public void DiscardEntries(RecordQueueReadResult<T> result)
        {
            if(result is null)
                throw new ArgumentNullException(nameof(result));

            // If nothing was read, nothing to do.
            if(result.Count == 0)
                return;

            lock(syncObj)
            {
                using(var stream = Open(path, FileMode.Open, FileAccess.ReadWrite))
                {
                    ReadHeader(stream, out _, out var head, out var rowCount);

                    var newCount = rowCount - result.Count;
                    var newHead = result.NextOffset;

                    if(newHead == -1)
                    {
                        // We consumed all known records (or everything we could).
                        InternalResetFile(stream);
                    }
                    else
                    {
                        WriteHeader(stream, newHead, newCount);
                    }
                }
            }
        }

        /// <summary>
        /// Appends the given entries as JSON records at the tail.
        /// If the queue was empty, updates the head to point to the first newly written record.
        /// Automatically compacts if unused space exceeds the threshold.
        /// </summary>
        public void WriteEntries(IEnumerable<T> entries)
        {
            if(entries is null)
                throw new ArgumentNullException(nameof(entries));

            var entryList = entries as IList<T> ?? entries.ToList();
            if(entryList.Count == 0)
                return;

            var recMarkerBytes = Encoding.ASCII.GetBytes(RECORD_MARKER);
            if(recMarkerBytes.Length != RECORD_MARKER_SIZE)
                throw new InvalidOperationException($"RecordMarker must be {RECORD_MARKER_SIZE} bytes.");

            var lenBuffer = new byte[LENGTH_SIZE];

            lock(syncObj)
            {
                using(var stream = Open(path, FileMode.Open, FileAccess.ReadWrite))
                {
                    ReadHeader(stream, out _, out var head, out var rowCount);

                    if(rowCount >= maxRows)
                    {
                        // IMPORTANT: This prevents unlimited growth of the local file in an scenario where communication with the log service is down for a long period of time.
                        // In this case we simply start discarding the oldest entries to make room for new records. By default we keep up to 20K records before we start discarding
                        // entries.
                        var rowsToDiscard = rowCount / 3;
                        stream.Position = head;
                        var (discardedItems, offset) = InternalReadRecords(stream, rowsToDiscard);
                        if(offset == -1)
                        {
                            // Could not advance past discarded range (end-of-file or corruption), reset to a clean empty file.
                            head = -1;
                            rowCount = 0;
                            WriteHeader(stream, -1, 0);
                        }
                        else
                        {
                            head = offset;
                            rowCount = Math.Max(0, rowCount - discardedItems.Count);
                            WriteHeader(stream, head, rowCount);
                        }
                    }

                    stream.Position = stream.Length;
                    long firstNewRecordOffset = stream.Length;
                    int appendedRecords = 0;
                    foreach(var entry in entryList)
                    {
                        var json = JsonConvert.SerializeObject(entry);
                        var payload = Encoding.UTF8.GetBytes(json);
                        BinaryPrimitives.WriteUInt32LittleEndian(lenBuffer, (uint)payload.Length);

                        stream.Write(recMarkerBytes, 0, recMarkerBytes.Length);
                        stream.Write(lenBuffer, 0, lenBuffer.Length);
                        stream.Write(payload, 0, payload.Length);
                        appendedRecords++;
                    }
                    stream.Flush(true);

                    if(rowCount == 0)
                    {
                        // Queue was empty; first appended record becomes new head.
                        WriteHeader(stream, headOffset: firstNewRecordOffset, rowCount: appendedRecords);
                        head = firstNewRecordOffset;
                    }
                    else
                    {
                        // Add to header count
                        if(head == -1)
                            head = HEADER_SIZE;
                        WriteHeader(stream, headOffset: head, rowCount: rowCount + appendedRecords);
                    }

                    // Consider compaction if unused region is larger than compact threshold.
                    var unused = head - HEADER_SIZE;
                    if(unused >= compactThresholdBytes)
                    {
                        InternalCompact(stream);
                    }
                }
            }
        }


        public void Verify(CancellationToken cancellationToken = default)
        {
            lock(syncObj)
            {
                using(var stream = Open(path, FileMode.Open, FileAccess.ReadWrite))
                {
                    InternalVerifyAndRepairFile(stream, false, cancellationToken);
                }
            }
        }


        public void Repair(CancellationToken cancellationToken = default)
        {
            lock(syncObj)
            {
                using(var stream = Open(path, FileMode.Open, FileAccess.ReadWrite))
                {
                    InternalVerifyAndRepairFile(stream, true, cancellationToken);
                }
            }
        }


        public void Compact()
        {
            lock(syncObj)
            {
                using(var stream = Open(path, FileMode.Open, FileAccess.ReadWrite))
                {
                    InternalCompact(stream);
                }
            }
        }


        private void InternalCheckFile()
        {
            lock(syncObj)
            {
                if(!File.Exists(path))
                {
                    using(var fs = Open(path, FileMode.CreateNew, FileAccess.ReadWrite))
                    {
                        WriteHeader(fs, -1, 0);
                    }
                    return;
                }
            }
        }


        // This method scans the entire file, validating records, optionally repairing (which in most cases means removing invalid data).
        private void InternalVerifyAndRepairFile(FileStream stream, bool repairIfInvalid, CancellationToken cancellationToken = default)
        {
            if(stream.Length < HEADER_SIZE)
            {
                if(!repairIfInvalid)
                    throw new InvalidDataException("File header is corrupt");

                // File size is invalid, reset file.
                InternalResetFile(stream);
                return;
            }

            long head;
            try
            {
                ReadHeader(stream, out _, out head, out _);
            }
            catch(InvalidDataException)
            {
                if(!repairIfInvalid)
                    throw;

                // Header is invalid, no other option but reset entire file.
                InternalResetFile(stream);
                return;
            }

            if(head == -1)
            {
                // File should be empty, Reset and return
                InternalResetFile(stream);
                return;
            }

            if(head >= stream.Length)
            {
                if(!repairIfInvalid)
                    throw new InvalidDataException("Head is pointing to an invalid location");

                // Head is pointing to an invalid location, no other option but reset entire file.
                InternalResetFile(stream);
                return;
            }

            // Header is ok and points to a valid record, now check the sequence of records
            stream.Position = head;
            int rowCount = 0;
            do
            {
                var (valid, offset) = InternalPeekRecord(stream);

                if(!valid)
                {
                    if(!repairIfInvalid)
                        throw new InvalidDataException($"Found corrupt record at location {offset}");

                    if(head == offset)
                    {
                        // First record in the sequence (head) is corrupt, no other option but reset whole file
                        InternalResetFile(stream);
                        return;
                    }
                    else
                    {
                        // Found a corrupt entry, but is not the head, so we need to truncate the rest of the file
                        stream.SetLength(offset);
                        WriteHeader(stream, head, rowCount);
                        return;
                    }
                }
                else
                {
                    stream.Position = offset;
                    rowCount++;
                }
            } while(stream.Position < stream.Length && !cancellationToken.IsCancellationRequested);

            if(cancellationToken.IsCancellationRequested)
                return;

            WriteHeader(stream, head, rowCount);
        }

        private void InternalResetFile(FileStream stream)
        {
            stream.SetLength(0);
            WriteHeader(stream, -1, 0);
        }

        private static FileStream Open(string path, FileMode mode, FileAccess access) =>
            new FileStream(path, mode, access, FileShare.Read, 4096, FileOptions.RandomAccess);

        private static void ReadHeader(FileStream stream, out string version, out long headOffset, out int rowCount)
        {
            stream.Position = 0;

            byte[] ver = new byte[VERSION_SIZE];
            int readBytes = stream.Read(ver, 0, ver.Length);
            if(readBytes != VERSION_SIZE)
                throw new InvalidDataException("Invalid header (version).");

            version = Encoding.ASCII.GetString(ver);
            if(!string.Equals(version, VERSION_TEXT, StringComparison.Ordinal))
                throw new InvalidDataException($"Unsupported file version: '{version}'.");

            byte[] headBuf = new byte[8];
            if(stream.Read(headBuf, 0, headBuf.Length) != 8)
                throw new InvalidDataException("Invalid header (head offset).");

            headOffset = BinaryPrimitives.ReadInt64LittleEndian(headBuf);

            byte[] countBuf = new byte[4];
            if(stream.Read(countBuf, 0, countBuf.Length) != 4)
                throw new InvalidDataException("Invalid header (row count).");

            rowCount = BinaryPrimitives.ReadInt32LittleEndian(countBuf);
        }

        private static void WriteHeader(FileStream stream, long headOffset, int rowCount)
        {
            stream.Position = 0;
            var verBytes = Encoding.ASCII.GetBytes(VERSION_TEXT);
            if(verBytes.Length != VERSION_SIZE)
                throw new InvalidOperationException($"Version must be {VERSION_SIZE} bytes.");
            stream.Write(verBytes, 0, verBytes.Length);

            byte[] headBuf = new byte[8];
            BinaryPrimitives.WriteInt64LittleEndian(headBuf, headOffset);
            stream.Write(headBuf, 0, headBuf.Length);

            byte[] countBuf = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(countBuf, rowCount);
            stream.Write(countBuf, 0, countBuf.Length);

            stream.Flush(true);
        }


        private (bool valid, long nextOffset) InternalPeekRecord(FileStream stream)
        {
            byte[] markerBytes = new byte[RECORD_MARKER_SIZE];
            byte[] lengthBuffer = new byte[LENGTH_SIZE];

            var startOffset = stream.Position;
            int readBytes = stream.Read(markerBytes, 0, markerBytes.Length);
            if(readBytes != RECORD_MARKER_SIZE)
            {
                return (false, startOffset);
            }

            var recMarker = Encoding.ASCII.GetString(markerBytes);
            if(!string.Equals(recMarker, RECORD_MARKER, StringComparison.Ordinal))
            {
                return (false, startOffset);
            }

            if(stream.Read(lengthBuffer, 0, lengthBuffer.Length) != LENGTH_SIZE)
            {
                return (false, startOffset);
            }

            var recordLength = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer);
            if(recordLength <= 0)
            {
                return (false, startOffset);
            }

            var nextRecordOffset = startOffset + RECORD_MARKER_SIZE + LENGTH_SIZE + recordLength;

            if(nextRecordOffset > stream.Length)
            {
                return (false, startOffset);
            }

            byte[] payload = new byte[recordLength];
            readBytes = stream.Read(payload, 0, payload.Length);
            if(readBytes != payload.Length)
            {
                return (false, startOffset);
            }

            try
            {
                var json = Encoding.UTF8.GetString(payload);
                var obj = JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                return (false, startOffset);
            }

            return (true, nextRecordOffset);
        }

        private static (List<T> items, long nextOffset) InternalReadRecords(FileStream stream, int count)
        {
            byte[] markerBytes = new byte[RECORD_MARKER_SIZE];
            byte[] lenBuffer = new byte[LENGTH_SIZE];

            var items = new List<T>(Math.Min(count, 100));
            long next = -1;

            for(int i = 0; i < count; i++)
            {
                // NOTE: for all incomplete/corrupted trailing records: treat it as if there are no more records available.
                if(stream.Position + RECORD_MARKER_SIZE > stream.Length)
                {
                    next = -1;
                    break;
                }

                if(stream.Read(markerBytes, 0, markerBytes.Length) != RECORD_MARKER_SIZE)
                {
                    next = -1;
                    break;
                }

                var recMarker = Encoding.ASCII.GetString(markerBytes);
                if(!string.Equals(recMarker, RECORD_MARKER, StringComparison.Ordinal))
                {
                    next = -1;
                    break;
                }

                if(stream.Read(lenBuffer, 0, lenBuffer.Length) != LENGTH_SIZE)
                {
                    next = -1;
                    break;
                }

                uint len = BinaryPrimitives.ReadUInt32LittleEndian(lenBuffer);

                if(len == 0 || stream.Position + len > stream.Length)
                {
                    next = -1;
                    break;
                }

                byte[] payload = new byte[len];
                int readBytes = stream.Read(payload, 0, payload.Length);
                if(readBytes != payload.Length)
                {
                    next = -1;
                    break;
                }

                var json = Encoding.UTF8.GetString(payload);
                var obj = JsonConvert.DeserializeObject<T>(json);
                items.Add(obj);

                if(stream.Position < stream.Length)
                {
                    next = stream.Position; // next record starts here
                }
                else
                {
                    next = -1;
                    break;
                }
            }

            return (items, next);
        }

        /// <summary>
        /// Compacts the file by moving [head..end] bytes immediately after the header, trimming leading unused space.
        /// </summary>
        private void InternalCompact(FileStream stream)
        {
            if(compactThresholdBytes < 10000)
                throw new InvalidOperationException("CompactThresholdBytes cannot be smaller than 10,000");

            ReadHeader(stream, out _, out var head, out var rowCount);

            if(head < 0 || head > stream.Length)
            {
                InternalResetFile(stream);
                return;
            }

            if(head == HEADER_SIZE)
            {
                // Already compacted
                return;
            }

            if(head > HEADER_SIZE && head < stream.Length)
            {
                WriteHeader(stream, HEADER_SIZE, rowCount);

                int readBytes = 0;
                long writePosition = HEADER_SIZE;
                var buffer = new byte[8192];
                do
                {
                    stream.Position = head;
                    readBytes = stream.Read(buffer, 0, buffer.Length);
                    stream.Position = writePosition;
                    stream.Write(buffer, 0, readBytes);
                    head += readBytes;
                    writePosition += readBytes;
                } while(readBytes > 0);
                stream.Flush(true);
                stream.SetLength(writePosition);
            }
            else
            {
                InternalResetFile(stream);
            }
        }
    }
}
