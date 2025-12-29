using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Channels;

internal static class Program
{
    private enum HashAlg { Sha256, Md5 }

    private sealed record Options(
        string Folder1,
        string Folder2,
        string Folder3,
        string OutPath,
        int Threads,
        HashAlg Alg
    );

    private sealed record FileJob(string FullPath1, string RelativePath);

    private static long _scanned;
    private static long _mismatches;
    private static long _hashCompared;
    private static long _errors;

    public static async Task<int> Main(string[] args)
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
            Console.WriteLine($"Output: {outPath}");
            Console.WriteLine($"Threads: {opt.Threads}");
            Console.WriteLine($"Alg: {opt.Alg}");
            Console.WriteLine();

            var channel = Channel.CreateBounded<FileJob>(new BoundedChannelOptions(50_000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = true,
                SingleReader = false
            });

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Cancellation requested...");
            };

            var writeLock = new object();

            // Открываем один StreamWriter на весь прогон; пишем только несоответствия/ошибки.
            using var writer = new StreamWriter(outPath, append: false, encoding: new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            writer.WriteLine("Type,RelativePath,Details");

            var sw = Stopwatch.StartNew();

            // Задача прогресса
            var progressTask = Task.Run(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), cts.Token).ConfigureAwait(false);
                    var scanned = Interlocked.Read(ref _scanned);
                    var mism = Interlocked.Read(ref _mismatches);
                    var hc = Interlocked.Read(ref _hashCompared);
                    var err = Interlocked.Read(ref _errors);

                    Console.WriteLine(
                        $"[{DateTime.Now:HH:mm:ss}] scanned={scanned:n0}, hashCompared={hc:n0}, mismatches={mism:n0}, errors={err:n0}, elapsed={sw.Elapsed:hh\\:mm\\:ss}");
                }
            }, cts.Token);

            // Producer: перечисляем файлы в Folder1
            var producer = Task.Run(async () =>
            {
                try
                {
                    var enumerationOptions = new EnumerationOptions
                    {
                        RecurseSubdirectories = true,
                        IgnoreInaccessible = true,
                        ReturnSpecialDirectories = false,
                        AttributesToSkip = FileAttributes.ReparsePoint // часто полезно, чтобы не ходить по junction/symlink
                    };

                    foreach (var file1 in Directory.EnumerateFiles(f1, "*", enumerationOptions))
                    {
                        cts.Token.ThrowIfCancellationRequested();

                        var rel = GetRelativePathFast(f1, file1);
                        await channel.Writer.WriteAsync(new FileJob(file1, rel), cts.Token).ConfigureAwait(false);
                    }
                }
                finally
                {
                    channel.Writer.TryComplete();
                }
            }, cts.Token);

            // Consumers
            var consumers = new Task[opt.Threads];
            for (int i = 0; i < consumers.Length; i++)
            {
                consumers[i] = Task.Run(async () =>
                {
                    await foreach (var job in channel.Reader.ReadAllAsync(cts.Token).ConfigureAwait(false))
                    {
                        try
                        {
                            Interlocked.Increment(ref _scanned);

                            var p2 = Path.Combine(f2, job.RelativePath);
                            var p3 = Path.Combine(f3, job.RelativePath);

                            // Существование
                            var exists2 = File.Exists(p2);
                            var exists3 = File.Exists(p3);

                            if (!exists2 || !exists3)
                            {
                                Interlocked.Increment(ref _mismatches);
                                var details = $"missingIn={(exists2 ? "" : "2")}{(!exists2 && !exists3 ? "&" : "")}{(exists3 ? "" : "3")}";
                                lock (writeLock)
                                {
                                    writer.WriteLine($"Missing,{EscapeCsv(job.RelativePath)},{EscapeCsv(details)}");
                                }
                                continue;
                            }

                            // Быстрая проверка размера перед хэшом
                            long len1, len2, len3;
                            try
                            {
                                len1 = new FileInfo(job.FullPath1).Length;
                                len2 = new FileInfo(p2).Length;
                                len3 = new FileInfo(p3).Length;
                            }
                            catch (Exception ex)
                            {
                                Interlocked.Increment(ref _errors);
                                lock (writeLock)
                                {
                                    writer.WriteLine($"Error,{EscapeCsv(job.RelativePath)},{EscapeCsv("stat: " + ex.Message)}");
                                }
                                continue;
                            }

                            if (len1 != len2 || len1 != len3)
                            {
                                Interlocked.Increment(ref _mismatches);
                                var details = $"size1={len1}, size2={len2}, size3={len3}";
                                lock (writeLock)
                                {
                                    writer.WriteLine($"SizeMismatch,{EscapeCsv(job.RelativePath)},{EscapeCsv(details)}");
                                }
                                continue;
                            }

                            // Хэш сравнение
                            var h1 = ComputeHashHex(job.FullPath1, opt.Alg);
                            var h2 = ComputeHashHex(p2, opt.Alg);
                            var h3 = ComputeHashHex(p3, opt.Alg);

                            Interlocked.Increment(ref _hashCompared);

                            if (!h1.Equals(h2, StringComparison.OrdinalIgnoreCase) ||
                                !h1.Equals(h3, StringComparison.OrdinalIgnoreCase))
                            {
                                Interlocked.Increment(ref _mismatches);
                                var details = $"hash1={h1} hash2={h2} hash3={h3}";
                                lock (writeLock)
                                {
                                    writer.WriteLine($"HashMismatch,{EscapeCsv(job.RelativePath)},{EscapeCsv(details)}");
                                }
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            // останов
                            break;
                        }
                        catch (Exception ex)
                        {
                            Interlocked.Increment(ref _errors);
                            lock (writeLock)
                            {
                                writer.WriteLine($"Error,{EscapeCsv(job.RelativePath)},{EscapeCsv(ex.Message)}");
                            }
                        }
                    }
                }, cts.Token);
            }

            await producer.ConfigureAwait(false);
            await Task.WhenAll(consumers).ConfigureAwait(false);

            cts.Cancel(); // останавливаем прогресс
            try { await progressTask.ConfigureAwait(false); } catch { /* ignore */ }

            sw.Stop();
            lock (writeLock) writer.Flush();

            Console.WriteLine();
            Console.WriteLine("Done.");
            Console.WriteLine($"Scanned:      {Interlocked.Read(ref _scanned):n0}");
            Console.WriteLine($"HashCompared: {Interlocked.Read(ref _hashCompared):n0}");
            Console.WriteLine($"Mismatches:   {Interlocked.Read(ref _mismatches):n0}");
            Console.WriteLine($"Errors:       {Interlocked.Read(ref _errors):n0}");
            Console.WriteLine($"Elapsed:      {sw.Elapsed:hh\\:mm\\:ss}");
            Console.WriteLine($"Report:       {outPath}");

            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.ToString());
            return 2;
        }
    }

    private static string EnsureDir(string path)
    {
        var full = Path.GetFullPath(path);
        if (!Directory.Exists(full))
            throw new DirectoryNotFoundException(full);
        return full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
    }

    private static Options ParseArgs(string[] args)
    {
        if (args.Length < 3)
            throw new ArgumentException("Usage: <folder1> <folder2> <folder3> [--out report.csv] [--threads N] [--alg sha256|md5]");

        string f1 = args[0];
        string f2 = args[1];
        string f3 = args[2];

        string outPath = "report.csv";
        int threads = Math.Max(1, Environment.ProcessorCount / 2);
        HashAlg alg = HashAlg.Sha256;

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
            else
            {
                throw new ArgumentException($"Unknown argument: {a}");
            }
        }

        return new Options(f1, f2, f3, outPath, threads, alg);
    }

    // Быстрее, чем Path.GetRelativePath на миллионах файлов (избавляемся от лишней нормализации).
    private static string GetRelativePathFast(string rootWithSep, string fullPath)
    {
        // rootWithSep гарантированно заканчивается на separator
        if (fullPath.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase))
            return fullPath.Substring(rootWithSep.Length);

        // fallback
        return Path.GetRelativePath(rootWithSep, fullPath);
    }

    private static string ComputeHashHex(string path, HashAlg alg)
    {
        // Большой буфер даёт нормальный throughput на sequential read
        byte[] buffer = ArrayPool<byte>.Shared.Rent(1024 * 1024); // 1 MB
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
