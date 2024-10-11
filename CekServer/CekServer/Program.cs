using GodSharp.Opc.Da;
using GodSharp.Opc.Da.Options;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GodSharpOpcDaSample
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("==============================");
                Console.WriteLine("======CONSOLE APP OPC DA======");
                Console.WriteLine("==============================");
                // Meminta input dari pengguna untuk Host dan ProgId
                Console.Write("Masukkan Host (misalnya, 127.0.0.1): ");
                string host = Console.ReadLine();

                Console.Write("Masukkan ProgId (misalnya, Kepware.KEPServerEX.V6): ");
                string progId = Console.ReadLine();

                // Meminta input dari pengguna untuk tag
                Console.Write("Masukkan tag yang ingin dibaca (pisahkan dengan koma, misalnya: SIMULATOR.KEPWARE.Group.Value10, SIMULATOR.KEPWARE.Group.Value20): ");
                string inputTags = Console.ReadLine();
                var itemNames = inputTags.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                          .Select(tag => tag.Trim())
                                          .ToList();

                // Membuat grup dengan tag yang ingin dibaca
                var groups = new List<GroupData>
                {
                    new GroupData
                    {
                        Name = "default", UpdateRate = 100, ClientHandle = 010, IsSubscribed = true,
                        Tags = itemNames.Select((itemName, index) => new Tag(itemName, index + 1)).ToList()
                    }
                };

                var server = new ServerData
                {
                    Host = host,
                    ProgId = progId,
                    Groups = groups
                };

                var client = DaClientFactory.Instance.CreateOpcNetApiClient(new DaClientOptions(
                    server,
                    OnDataChangedHandler,
                    OnShoutdownHandler,
                    OnAsyncReadCompletedHandler));

                Console.WriteLine("Connecting to server ...");
                client.Connect();
                Console.WriteLine("==============================");
                Console.WriteLine($"Connected to server {client.Server.ProgId}: {client.Connected}");

                Console.WriteLine($"Waiting for synchronous reads ...");
                Console.ReadLine();

                foreach (var group in client.Groups.Values)
                {
                    if (group.Tags.Count == 0) continue;
                    var results = group.Reads(group.Tags.Values.Select(x => x.ItemName)?.ToArray());

                    foreach (var item in results)
                    {
                        Console.WriteLine(
                            $"Sync Read {item.Result.ItemName}: {item.Result.Value}, {item.Result.Quality} / {item.Result.Timestamp} / {item.Ok}|{item.Code}");
                    }
                }

                Console.WriteLine($"Waiting to disconnect ...");
                client.Disconnect();
                client.Dispose();
                Console.WriteLine($"Disposed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Terjadi kesalahan: {ex.Message}");
            }

            // Menunggu input dari pengguna sebelum keluar
            Console.WriteLine("Tekan Enter untuk keluar...");
            Console.ReadLine();
        }

        public static void OnDataChangedHandler(DataChangedOutput output)
        {
            Console.WriteLine($"{output.Data.ItemName}: {output.Data.Value}, {output.Data.Quality} / {output.Data.Timestamp}");
        }

        public static void OnAsyncReadCompletedHandler(AsyncReadCompletedOutput output)
        {
            Console.WriteLine(
                $"Async Read {output.Data.Result.ItemName}: {output.Data.Result.Value}, {output.Data.Result.Quality} / {output.Data.Result.Timestamp} / {output.Data.Code}");
        }

        public static void OnShoutdownHandler(Server server, string reason)
        {
            Console.WriteLine($"Server shutdown: {reason}");
        }
    }
}
