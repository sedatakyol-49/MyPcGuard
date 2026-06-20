using System.Net.NetworkInformation;
using MyPcGuard.Models;
using MyPcGuard.Services.Abstractions;

namespace MyPcGuard.Services.Windows;

public sealed class WindowsNetworkScanner : INetworkScanner
{
    public Task<IReadOnlyList<NetworkConnectionItem>> ScanAsync(CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var properties = IPGlobalProperties.GetIPGlobalProperties();
                return (IReadOnlyList<NetworkConnectionItem>)properties.GetActiveTcpConnections()
                    .Select(connection => new NetworkConnectionItem
                    {
                        Protocol = "TCP",
                        LocalEndpoint = connection.LocalEndPoint.ToString(),
                        RemoteEndpoint = connection.RemoteEndPoint.ToString(),
                        State = connection.State.ToString()
                    })
                    .Concat(properties.GetActiveUdpListeners().Select(endpoint => new NetworkConnectionItem
                    {
                        Protocol = "UDP",
                        LocalEndpoint = endpoint.ToString(),
                        RemoteEndpoint = "-",
                        State = "Listening"
                    }))
                    .OrderBy(item => item.Protocol)
                    .ThenBy(item => item.LocalEndpoint)
                    .ToList();
            }
            catch
            {
                return [];
            }
        }, cancellationToken);
    }
}
