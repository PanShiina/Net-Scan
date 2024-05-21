using System;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Collections.Generic;

class NetworkScanner
{
    static async Task Main(string[] args)
    {
        if (args.Length != 2 || args[0] != "-a")
        {
            Console.WriteLine("Usage: program -a <network>");
            Console.WriteLine("Example: program -a 192.168.1.0/24");
            return;
        }

        string network = args[1];
        if (!IsValidNetwork(network, out string subnet, out int prefixLength))
        {
            Console.WriteLine("Invalid network format. Use CIDR notation, e.g., 192.168.1.0/24");
            return;
        }

        int hostCount = (int)Math.Pow(2, 32 - prefixLength) - 2;
        Task<PingResult>[] tasks = new Task<PingResult>[hostCount];

        string[] parts = subnet.Split('.');
        for (int i = 1; i <= hostCount; i++)
        {
            string ip = $"{parts[0]}.{parts[1]}.{parts[2]}.{i}";
            tasks[i - 1] = PingHostAsync(ip);
        }

        PingResult[] results = await Task.WhenAll(tasks);
        DisplayResults(results);
    }

    static bool IsValidNetwork(string network, out string subnet, out int prefixLength)
    {
        subnet = null;
        prefixLength = 0;

        string[] parts = network.Split('/');
        if (parts.Length != 2 || !int.TryParse(parts[1], out prefixLength))
        {
            return false;
        }

        subnet = parts[0];
        string[] subnetParts = subnet.Split('.');
        if (subnetParts.Length != 4)
        {
            return false;
        }

        foreach (string part in subnetParts)
        {
            if (!int.TryParse(part, out int bytePart) || bytePart < 0 || bytePart > 255)
            {
                return false;
            }
        }

        return prefixLength >= 0 && prefixLength <= 32;
    }

    static async Task<PingResult> PingHostAsync(string ipAddress)
    {
        using (Ping pinger = new Ping())
        {
            try
            {
                PingReply reply = await pinger.SendPingAsync(ipAddress, 1000); // 1000ms = 1 second timeout
                if (reply.Status == IPStatus.Success)
                {
                    return new PingResult { IPAddress = ipAddress, IsActive = true, RoundtripTime = reply.RoundtripTime };
                }
            }
            catch (PingException)
            {
                // Handle or log the exception as necessary
            }
        }
        return new PingResult { IPAddress = ipAddress, IsActive = false, RoundtripTime = -1 };
    }

    static void DisplayResults(PingResult[] results)
    {
        Console.WriteLine("IP Address\t\tStatus\t\tRoundtrip Time (ms)");
        Console.WriteLine("----------------------------------------------------");
        foreach (var result in results)
        {
            if (result.IsActive)
            {
                string status = result.IsActive ? "Active" : "Inactive";
                string roundtripTime = result.IsActive ? result.RoundtripTime.ToString() : "N/A";
                Console.WriteLine($"{result.IPAddress}\t\t{status}\t\t{roundtripTime}");
            }
        }
    }

    class PingResult
    {
        public string IPAddress { get; set; }
        public bool IsActive { get; set; }
        public long RoundtripTime { get; set; }
    }
}
