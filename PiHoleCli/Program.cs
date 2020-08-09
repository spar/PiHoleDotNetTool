using Newtonsoft.Json;
using PiHoleCli.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace PiHoleCli
{
    class Program
    {
        static string SettingsFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        static Settings _settings = new Settings();
        static async Task Main(string[] args)
        {
            try
            {
                if (args.Length == 0)
                {
                    DisplayUsage();
                    return;
                }

                LoadSettings();

                if (args[0] == "setUrl")
                {
                    if (args.Length == 1)
                    {
                        Console.WriteLine("Please provide url value");
                        return;
                    }

                    SetUrl(args[1]);
                }

                if (args[0] == "setSecret")
                {
                    if (args.Length == 1)
                    {
                        Console.WriteLine("Please provide secret value");
                        return;
                    }

                    SetSecret(args[1]);
                }

                if (string.IsNullOrEmpty(_settings?.ApiUrl))
                {
                    Console.WriteLine("please use -setUrl and -setSecret to provide your pi-hole's api url and secret.");
                    DisplayUsage();
                    return;
                }

                if (string.IsNullOrEmpty(_settings.ApiSecret))
                {
                    Console.WriteLine("API secret has not been set, CLI can only run unauthenticated commands. Use -setSecret to set API's secret");
                }

                if (args[0] == "up")
                {
                    await UpAsync();
                }
                if (args[0] == "down")
                {
                    string seconds = "0";
                    if (args.Length > 1)
                    {
                        seconds = args[1];
                    }
                    await DownAsync(seconds);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }

        static void SetUrl(string url)
        {
            LoadSettings();
            _settings.ApiUrl = url;
            SetSettings();
            Console.WriteLine("Url has been set");
        }

        static void SetSecret(string secret)
        {
            LoadSettings();
            _settings.ApiSecret = secret;
            SetSettings();
            Console.WriteLine("Secret has been set");
        }

        static async Task UpAsync()
        {
            try
            {
                var client = new PiHoleApiClient.PiHoleApiClient(new HttpClient(), _settings.ApiUrl, _settings.ApiSecret);
                var status = await client.Enable();
                if (status.Status == "enabled")
                {
                    Console.WriteLine("Pi-Hole has been enabled.");
                }
                else
                {
                    Console.WriteLine("Opss! Something went wrong. Try again!");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"something went wrong while enabling your pi-hole : {e.Message}");
            }
        }

        static async Task DownAsync(string seconds)
        {
            try
            {
                long s;
                if (!long.TryParse(seconds, out s))
                    s = 0;
                var client = new PiHoleApiClient.PiHoleApiClient(new HttpClient(), _settings.ApiUrl, _settings.ApiSecret);
                var status = await client.Disable(s);
                if (status.Status == "disabled")
                {
                    Console.WriteLine("Pi-Hole has been disabled.");
                }
                else
                {
                    Console.WriteLine("Opss! Something went wrong. Try again!");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"something went wrong while disabled your pi-hole : {e.Message}");
            }
        }

        static void DisplayUsage()
        {
            var versionString = Assembly.GetEntryAssembly()
                                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                .InformationalVersion
                                .ToString();

            Console.WriteLine($"phc v{versionString}");
            Console.WriteLine("-------------");
            Console.WriteLine("\nUsage:");
            Console.WriteLine("  phc setUrl <apiUrl>");
            Console.WriteLine("  phc setSecret <apiSecret>");
            Console.WriteLine("  phc up");
            Console.WriteLine("  phc down <seconds>");
        }
        static void LoadSettings()
        {
            if (!File.Exists(SettingsFileName))
                return;

            var settingsJson = File.ReadAllText(SettingsFileName);
            _settings = JsonConvert.DeserializeObject<Settings>(settingsJson);
        }

        static void SetSettings()
        {
            File.WriteAllText(SettingsFileName, JsonConvert.SerializeObject(_settings));
        }
    }
}
