namespace SqlAnalyzer.Api.Tests.E2E
{
    public static class TestConfiguration
    {
        // Port range for E2E tests: 15500-15600
        public const int ApiPortRangeStart = 15500;
        public const int ApiPortRangeEnd = 15520;
        public const int DatabasePortRangeStart = 15521;
        public const int DatabasePortRangeEnd = 15540;
        public const int MockServicePortRangeStart = 15541;
        public const int MockServicePortRangeEnd = 15560;
        public const int WebUIPortRangeStart = 15561;
        public const int WebUIPortRangeEnd = 15580;
        public const int RedisPortRangeStart = 15581;
        public const int RedisPortRangeEnd = 15600;

        private static readonly Random _random = new();
        private static readonly HashSet<int> _usedPorts = new();
        private static readonly object _lock = new();

        public static int GetNextAvailablePort(int rangeStart, int rangeEnd)
        {
            lock (_lock)
            {
                for (int i = 0; i < 100; i++) // Try up to 100 times
                {
                    var port = _random.Next(rangeStart, rangeEnd + 1);
                    if (!_usedPorts.Contains(port) && IsPortAvailable(port))
                    {
                        _usedPorts.Add(port);
                        return port;
                    }
                }
                throw new InvalidOperationException($"Could not find available port in range {rangeStart}-{rangeEnd}");
            }
        }

        public static void ReleasePort(int port)
        {
            lock (_lock)
            {
                _usedPorts.Remove(port);
            }
        }

        private static bool IsPortAvailable(int port)
        {
            try
            {
                using var socket = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Any, port);
                socket.Start();
                socket.Stop();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}