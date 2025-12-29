// Program.cs
// .NET 8+
// Usage:
//   dotnet run -- <folder1> <folder2> <folder3> --out report.csv --threads 8 --alg sha256 --mode hash
//
// Modes:
//   exist  - only existence check
//   size   - existence + size
//   hash   - existence + size + hash (default)
//
// Notes:
// - Designed for very large trees (2M+ files): streaming enumeration, no preloading lists.
// - Works with link/junction roots: does NOT skip ReparsePoint.
// - Writes ONLY problems to CSV to keep report small.
// - Includes progress output every 2 seconds.
// - Includes optional recursion depth limit (default 512) to reduce risk of link cycles.

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

internal static class Program
{
    private enum HashAlg { Sha256, Md5 }
    private enum Mode { Exist, Size, Hash }

    private sealed record Options(
        string Folder1,
        string Folder2,
        string Folder3,
        string OutPath,
        int Threads,
        HashAlg Alg,
        Mode Mode,
        int MaxDepth
    );

    private static long _scanned;
    private static long _mismatches;
    private static long _hashCompared;
    private static long _errors;

    public static int Main(string[] args)
    {
        try
        {
            var opt = ParseArgs(args);

            var f1 = EnsureDir(opt.Folder1);
            var f2 = EnsureDir(opt.Folder2);
            var f3 = EnsureDir(opt.Folder3);

            var outPath = Path.GetFullPath(opt.OutPath);
            Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);

            Console.WriteLine("Folders:");
            Console.WriteLine($"  1: {f1}");
            Console.WriteLine($"  2: {f2}");
            Console.WriteLine($"  3: {f3}");
            Console.WriteLine($"Output:   {outPath}");
            Console.WriteLine($"Threads:  {opt.Threads}");
            Console.WriteLine($"Mode:     {opt.Mode}");
            Console.WriteLine($"Alg:      {opt.Alg}");
            Console.WriteLine($"MaxDepth: {opt.MaxDepth}");
            Console.WriteLine();

            using var writer = new StreamWriter(outPath, append: false, encoding: new UTF8Encoding(false));
            writer.WriteLine("Type,RelativePath,Details");

            // Thread-safe buffered writes to CSV without locking on each line
            var logQueue = new ConcurrentQueue<string>();
            var logStop = new CancellationTokenSource();

            var logThread = new Thread(() =>
            {
                var sw = Stopwatch.StartNew();
                while (!logStop.IsCancellationRequested || !logQueue.IsEmpty)
                {
                    while (logQueue.TryDequeue(out var line))
                    {
                        writer.WriteLine(line);
                    }

                    // Flush periodically
                    if (sw.ElapsedMilliseconds >= 1000)
                    {
                        writer.Flush();
                        sw.Restart();
                    }

                    Thread.Sleep(50);
                }

                // final drain
                while (logQueue.TryDequeue(out var line))
                    writer.WriteLine(line);

                writer.Flush();
            })
            { IsBackground = true };
            logThread.Start();

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Cancellation requested...");
            };

            var progressThread = new Thread(() =>
            {
                var sw = Stopwatch.StartNew();
                while (!cts.IsCancellationRequested)
                {
                    Thread.Sleep(2000);
                    var scanned = Interlocked.Read(ref _scanned);
                    var mism = Interlocked.Read(ref _mismatches);
                    var hc = Interlocked.Read(ref _hashCompared);
                    var err = Interlocked.Read(ref _errors);

                    Console.WriteLine(
                        $"[{DateTime.Now:HH:mm:ss}] scanned={scanned:n0}, hashCompared={hc:n0}, mismatches={mism:n0}, errors={err:n0}, elapsed={sw.Elapsed:hh\\:mm\\:ss}");
                }
            })
            { IsBackground = true };
            progressThread.Start();

            var enumOptions = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                ReturnSpecialDirectories = false,
                AttributesToSkip = 0, // IMPORTANT: do not skip ReparsePoint (your roots are links)
                MaxRecursionDepth = opt.MaxDepth
            };

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = opt.Threads,
                CancellationToken = cts.Token
            };

            var runSw = Stopwatch.StartNew();

            // STREAMING enumeration + parallel processing; no preloading lists.
            Parallel.ForEach(
                Directory.EnumerateFiles(f1, "*", enumOptions),
                parallelOptions,
                file1 =>
                {
                    parallelOptions.CancellationToken.ThrowIfCancellationRequested();
                    Interlocked.Increment(ref _scanned);

                    string rel;
                    try
                    {
                        rel = Path.GetRelativePath(f1, file1);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref _errors);
                        logQueue.Enqueue($"Error,{EscapeCsv(file1)},{EscapeCsv("relpath: " + ex.Message)}");
                        return;
                    }

                    var p2 = Path.Combine(f2, rel);
                    var p3 = Path.Combine(f3, rel);

                    // Existence check
                    var exists2 = File.Exists(p2);
                    var exists3 = File.Exists(p3);

                    if (!exists2 || !exists3)
                    {
                        Interlocked.Increment(ref _mismatches);
                        var details = $"missingIn={(exists2 ? "" : "2")}{(!exists2 && !exists3 ? "&" : "")}{(exists3 ? "" : "3")}";
                        logQueue.Enqueue($"Missing,{EscapeCsv(rel)},{EscapeCsv(details)}");
                        return;
                    }

                    if (opt.Mode == Mode.Exist)
                        return;

                    // Size check
                    long len1, len2, len3;
                    try
                    {
                        len1 = new FileInfo(file1).Length;
                        len2 = new FileInfo(p2).Length;
                        len3 = new FileInfo(p3).Length;
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref _errors);
                        logQueue.Enqueue($"Error,{EscapeCsv(rel)},{EscapeCsv("stat: " + ex.Message)}");
                        return;
                    }

                    if (len1 != len2 || len1 != len3)
                    {
                        Interlocked.Increment(ref _mismatches);
                        var details = $"size1={len1}, size2={len2}, size3={len3}";
                        logQueue.Enqueue($"SizeMismatch,{EscapeCsv(rel)},{EscapeCsv(details)}");
                        return;
                    }

                    if (opt.Mode == Mode.Size)
                        return;

                    // Hash check (streaming read)
                    try
                    {
                        var h1 = ComputeHashHex(file1, opt.Alg);
                        var h2 = ComputeHashHex(p2, opt.Alg);
                        var h3 = ComputeHashHex(p3, opt.Alg);

                        Interlocked.Increment(ref _hashCompared);

                        if (!h1.Equals(h2, StringComparison.OrdinalIgnoreCase) ||
                            !h1.Equals(h3, StringComparison.OrdinalIgnoreCase))
                        {
                            Interlocked.Increment(ref _mismatches);
                            var details = $"hash1={h1} hash2={h2} hash3={h3}";
                            logQueue.Enqueue($"HashMismatch,{EscapeCsv(rel)},{EscapeCsv(details)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref _errors);
                        logQueue.Enqueue($"Error,{EscapeCsv(rel)},{EscapeCsv("hash: " + ex.Message)}");
                    }
                });

            runSw.Stop();

            // stop logger + finalize
            logStop.Cancel();
            logThread.Join();

            cts.Cancel(); // stop progress thread

            Console.WriteLine();
            Console.WriteLine("Done.");
            Console.WriteLine($"Scanned:      {Interlocked.Read(ref _scanned):n0}");
            Console.WriteLine($"HashCompared: {Interlocked.Read(ref _hashCompared):n0}");
            Console.WriteLine($"Mismatches:   {Interlocked.Read(ref _mismatches):n0}");
            Console.WriteLine($"Errors:       {Interlocked.Read(ref _errors):n0}");
            Console.WriteLine($"Elapsed:      {runSw.Elapsed:hh\\:mm\\:ss}");
            Console.WriteLine($"Report:       {outPath}");

            return 0;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Canceled.");
            return 3;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            return 2;
        }
    }

    private static Options ParseArgs(string[] args)
    {
        if (args.Length < 3)
            throw new ArgumentException("Usage: <folder1> <folder2> <folder3> [--out report.csv] [--threads N] [--alg sha256|md5] [--mode exist|size|hash] [--maxdepth N]");

        string f1 = args[0];
        string f2 = args[1];
        string f3 = args[2];

        string outPath = "report.csv";
        int threads = Math.Max(1, Environment.ProcessorCount / 2);
        HashAlg alg = HashAlg.Sha256;
        Mode mode = Mode.Hash;
        int maxDepth = 512;

        for (int i = 3; i < args.Length; i++)
        {
            var a = args[i];

            if (a.Equals("--out", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                outPath = args[++i];
            }
            else if (a.Equals("--threads", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                if (!int.TryParse(args[++i], out var t) || t < 1) throw new ArgumentException("Invalid --threads");
                threads = t;
            }
            else if (a.Equals("--alg", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                var v = args[++i].ToLowerInvariant();
                alg = v switch
                {
                    "sha256" => HashAlg.Sha256,
                    "md5" => HashAlg.Md5,
                    _ => throw new ArgumentException("Invalid --alg. Use sha256 or md5.")
                };
            }
            else if (a.Equals("--mode", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                var v = args[++i].ToLowerInvariant();
                mode = v switch
                {
                    "exist" => Mode.Exist,
                    "size" => Mode.Size,
                    "hash" => Mode.Hash,
                    _ => throw new ArgumentException("Invalid --mode. Use exist, size, or hash.")
                };
            }
            else if (a.Equals("--maxdepth", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
            {
                if (!int.TryParse(args[++i], out var d) || d < 1) throw new ArgumentException("Invalid --maxdepth");
                maxDepth = d;
            }
            else
            {
                throw new ArgumentException($"Unknown argument: {a}");
            }
        }

        return new Options(f1, f2, f3, outPath, threads, alg, mode, maxDepth);
    }

    private static string EnsureDir(string path)
    {
        var full = Path.GetFullPath(path);
        if (!Directory.Exists(full))
            throw new DirectoryNotFoundException(full);

        // normalize to have trailing separator to keep Path.GetRelativePath stable
        return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
    }

    private static string ComputeHashHex(string path, HashAlg alg)
    {
        // Large buffer + SequentialScan for throughput
        byte[] buffer = ArrayPool<byte>.Shared.Rent(1024 * 1024); // 1MB
        try
        {
            using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 1024 * 1024,
                options: FileOptions.SequentialScan);

            byte[] hash = alg switch
            {
                HashAlg.Sha256 => SHA256.HashData(stream),
                HashAlg.Md5 => MD5.HashData(stream),
                _ => throw new ArgumentOutOfRangeException(nameof(alg))
            };

            return Convert.ToHexString(hash);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static string EscapeCsv(string s)
    {
        if (s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0)
            return "\"" + s.Replace("\"", "\"\"") + "\"";
        return s;
    }
}