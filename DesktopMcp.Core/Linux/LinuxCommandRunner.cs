using System.Diagnostics;

namespace DesktopMcp.Core.Linux;

internal static class LinuxCommandRunner
{
    public static bool HasCommand(string name)
    {
        try
        {
            using var process = CreateProcess("which", Escape(name));
            process.Start();
            process.WaitForExit(3000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    public static string Run(string command, string args, int timeoutMs = 10000)
    {
        using var process = CreateProcess(command, args);
        process.Start();
        if (!process.WaitForExit(timeoutMs))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
            }

            throw new TimeoutException($"Command '{command} {args}' timed out.");
        }

        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Command '{command} {args}' failed with code {process.ExitCode}. {stderr.Trim()}");
        }

        return stdout;
    }

    public static byte[] RunForBytes(string command, string args, int timeoutMs = 20000)
    {
        using var process = CreateProcess(command, args, redirectStandardOutput: true);
        process.Start();

        using var ms = new MemoryStream();
        process.StandardOutput.BaseStream.CopyTo(ms);

        if (!process.WaitForExit(timeoutMs))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch
            {
            }

            throw new TimeoutException($"Command '{command} {args}' timed out.");
        }

        var stderr = process.StandardError.ReadToEnd();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Command '{command} {args}' failed with code {process.ExitCode}. {stderr.Trim()}");
        }

        return ms.ToArray();
    }

    private static Process CreateProcess(string command, string args, bool redirectStandardOutput = true)
    {
        return new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = redirectStandardOutput,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
    }

    private static string Escape(string value)
    {
        return $"\"{value.Replace("\"", "\\\"")}\"";
    }
}
