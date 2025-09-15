using System.Net.Sockets;
using System.Text;

namespace metin2freebsdapi.Helpers;

internal static class ServerHelpers
{
    internal static async Task<string> SendCommandAsync(string[] cmdLines)
    {
        using var cts = new CancellationTokenSource(3000);
        using var tcp = new TcpClient();

        var connectTask = tcp.ConnectAsync( EnvironmentVariables.AdminPageIp, Convert.ToInt32(EnvironmentVariables.AdminPagePort));
        var completed = await Task.WhenAny(connectTask, Task.Delay(3000, cts.Token));
    
        if (completed != connectTask)
        {
            throw new TimeoutException("connect timeout");
        }
    
        if (!tcp.Client.Connected) throw new Exception("failed to connect");

        await using var stream = tcp.GetStream();
        stream.ReadTimeout = 3000;
        stream.WriteTimeout = 3000;

        foreach (var line in cmdLines)
        {
            Console.WriteLine(line);
            var payload = line + "\n";
            var payloadBytes = Encoding.UTF8.GetBytes(payload);
            var buf = new byte[1 + payloadBytes.Length];
            buf[0] = 0x40;
            Array.Copy(payloadBytes, 0, buf, 1, payloadBytes.Length);
            await stream.WriteAsync(buf, cts.Token);
            await stream.FlushAsync(cts.Token);
        }

        var sb = new StringBuilder();
        var readBuf = new byte[1024];
        try
        {
            while (!cts.IsCancellationRequested)
            {
                var readTask = stream.ReadAsync(readBuf.AsMemory(0, readBuf.Length), cts.Token);
                var bytesRead = await readTask;
                if (bytesRead == 0)
                {
                    break;
                }
                var part = Encoding.UTF8.GetString(readBuf, 0, bytesRead);
                sb.Append(part);
                if (sb.ToString().EndsWith("\n")) break;
            }
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException("read timeout");
        }
        catch (Exception ex)
        {
            throw new Exception("read error: " + ex.Message, ex);
        }
        finally
        {
            try { tcp.Close(); } catch { /* ignore */ }
        }

        return sb.ToString().Trim();
    }
}