using ArpLookup;
using HalotMageProDashboard.Entities;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.CompilerServices;

namespace HalotMageProDashboard.Models {
    /// <summary>
    /// 3Dプリンターの管理を行うクラス
    /// </summary>
    public class PrinterManager(ApplicationDbContext DbContext, IServiceProvider ServiceProvider) : IDisposable {

        /// <summary>
        /// SQLiteに保存されたプリンターの一覧を取得する
        /// </summary>
        /// <returns>一覧</returns>
        public Task<IEnumerable<SavedPrinter>> GetSavedPrintersAsync() {
            return Task.FromResult(DbContext.SavedPrinters.AsNoTracking().AsEnumerable());
        }

        /// <summary>
        /// 利用可能なプリンターの一覧を取得する
        /// </summary>
        /// <returns>一覧</returns>
        public async Task<IEnumerable<Printer>> GetAvailablePrintersAsync() {

            var savedPrinters = await GetSavedPrintersAsync().ConfigureAwait(false);

            var printers = new List<Printer>();

            foreach (var savedPrinter in savedPrinters) {
                printers.Add(new Printer(ActivatorUtilities.GetServiceOrCreateInstance<ApplicationDbContext>(ServiceProvider), savedPrinter));
            }

            return printers;
        }

        /// <summary>
        /// 検知したプリンターをDBに保存する
        /// </summary>
        /// <param name="printer">追加したいプリンター</param>
        public void Add(DetectedPrinter printer) {

            DbContext.SavedPrinters.Add(new SavedPrinter {
                Host = printer.Host,
                Name = printer.Host,
                MacAddress = printer.MacAddress?.ToString(),
                Password = "1234",
            });
            DbContext.SaveChanges();
            DbContext.ChangeTracker.Clear();
        }

        /// <summary>
        /// 指定したプリンターをDBから削除する
        /// </summary>
        /// <param name="printer">削除したいプリンター</param>
        public void Remove(Printer printer) {

            var savedPrinter = printer.GetSavedPrinter();
            if (savedPrinter == null) {
                return;
            }
            printer.Dispose();
            DbContext.SavedPrinters.Remove(savedPrinter);
            DbContext.SaveChanges();
        }

        public void Refresh() {

        }

        public void Dispose() {
            DbContext.Dispose();
        }

        private UnicastIPAddressInformation? GetPrimaryLocalIpAddressInformation(NetworkInterface networkInterface) {

            // ネットワークが利用できない場合はnullを返す
            if (!NetworkInterface.GetIsNetworkAvailable()) {
                return null;
            }

            var ipProperties = networkInterface.GetIPProperties();
            return ipProperties.UnicastAddresses
                .Where(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork)
                .FirstOrDefault();
        }

        public List<NetworkInterface> GetNetworkInterfaces() {
            return NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up)
                .OrderByDescending(o => o.GetIPProperties().GatewayAddresses.Any())
                .ToList();
        }

        private List<IPAddress> ListAllHostAddresses(IPAddress hostAddress, byte prefixLength) {

            var ipNetwork = new IPNetwork2(hostAddress, prefixLength);
            return [.. ipNetwork.ListIPAddress(FilterEnum.Usable)];
        }

        /// <summary>
        /// LAN内のプリンターを探索する
        /// </summary>
        public async IAsyncEnumerable<DetectedPrinter>? SearchPrinters(NetworkInterface networkInterface, [EnumeratorCancellation] CancellationToken cancellationToken = default) {

            var localIpAddressInformation = GetPrimaryLocalIpAddressInformation(networkInterface);
            if (localIpAddressInformation == null) {
                yield break;
            }

            var hostAddresses = ListAllHostAddresses(localIpAddressInformation.Address, (byte)localIpAddressInformation.PrefixLength);

            // 探索タスクを先に作成しておく
            var tasks = new List<Task<IPAddress?>>();
            foreach (var hostAddress in hostAddresses) {
                // 自分自身のIPアドレスはスキップ
                if (hostAddress == localIpAddressInformation.Address) {
                    continue;
                }
                tasks.Add(Task.Run(async () => {
                    using var client = new TcpClient();
                    try {
                        await client.ConnectAsync(hostAddress, 18188, cancellationToken);
                        return hostAddress;
                    } catch {
                        return null;
                    }
                }));
            }

            while (true) {
                if (cancellationToken.IsCancellationRequested) {
                    break;
                }
                var task = await Task.WhenAny(tasks);
                tasks.Remove(task);
                if (await task != null) {

                    var arp = await Arp.LookupAsync(task.Result!);
                    yield return new DetectedPrinter {
                        Host = task.Result!.ToString(),
                        MacAddress = arp,
                        AlreadyRegistered = DbContext.SavedPrinters.Any(sp => sp.MacAddress != null && sp.MacAddress == arp!.ToString())
                    };
                }
                if (tasks.Count == 0) {
                    break;
                }
            }
            yield break;
        }
    }

    public class DetectedPrinter {
        /// <summary>
        /// 既に登録済みかどうか
        /// </summary>
        public bool AlreadyRegistered { get; set; }

        /// <summary>
        /// IPアドレス
        /// </summary>
        public required string Host { get; set; }

        /// <summary>
        /// Macアドレス（取得可能な場合）
        /// </summary>
        public PhysicalAddress? MacAddress { get; set; }
    }
}
