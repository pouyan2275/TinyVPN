using Microsoft.Extensions.Configuration;
using Pouyan.Network;
using Pouyan.SingBox;
using Pouyan.SingBox.Model;
using SingBoxLib.Configuration.Inbound.Abstract;
using SingBoxLib.Parsing;

namespace Pouyan
{
    public class Vpn
    {
        CancellationTokenSource? _ct;
        IEnumerable<TestResult> profilesResults;
        int index, counterProfile;
        string url;
        public List<TestResult> orderedProfiles;
        readonly string singboxPath = "./singbox/sing-box.exe";
        ProfileTester? profileTester;
        IConfigurationRoot? builder;
        readonly bool setSystemProxy;
        readonly string? urlTest;
        readonly int timeout;
        public InboundConfig inbounds { get; }
        Random random;

        public Vpn() {


            profileTester = new ProfileTester(singboxPath);
            builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json").Build();
            setSystemProxy = bool.Parse(builder["system_proxy_on_start"]!);

            urlTest = builder["url_test"] is "default" or "" or null ? null : builder["url_test"];

            timeout = int.Parse(builder["timeout"]!);
            counterProfile = int.Parse(builder?["count_profile"]!);
            url = builder?["subscribe_url"]!;

            inbounds = Inbound.CreateHttpInbound(
                listen: "127.0.0.1",
                listenPort: 3080,

                setSystemProxy
                );
            random = new();

        }

        public async Task Start(CancellationTokenSource? ct = default)
        {
            _ct = ct;


            Console.WriteLine($"Http: {inbounds.Listen}:{inbounds.ListenPort}");
            Console.Title = "Connecting";

            var singbox = new Tunnel(singboxPath, [inbounds]);
            
            List<ProfileItem> profiles = TakeProfiles(url).OrderBy(x => random.Next()).ToList();

            counterProfile = counterProfile > profiles.Count ? profiles.Count : counterProfile;
            index = 0;
            Console.WriteLine($"Start Test Profiles...");
            do
            {
                Console.WriteLine($"{index + counterProfile}/{profiles.Count}");

                profilesResults = await profileTester!.UrlTestAsync(profiles.GetRange(index, counterProfile), urlTest: urlTest,timeout:timeout);
                orderedProfiles = profilesResults.Where(p => p.Result!.Delay > 0).OrderBy(p => p.Result!.Delay).ToList();

                index += counterProfile;
                counterProfile = profiles.Count < (index + counterProfile) ? profiles.Count - index : counterProfile;

            } while (orderedProfiles.Count == 0 && index != profiles.Count);

            if (orderedProfiles.Count == 0)
            {
                Console.WriteLine("0 profiles work well with your internet connection");
                Console.ReadLine();
                return;
            }
            Console.WriteLine("Profiles Work Well:");
            orderedProfiles.ForEach(p => { Console.WriteLine($"Name: {p.Profile.Name} Delay: {p.Result!.Delay}"); });

            Console.WriteLine($"Connecting To {orderedProfiles[0].Profile.Name}");
            string titleMessage = $"Connected - {orderedProfiles[0].Profile.Name} - ";
            titleMessage = setSystemProxy ? titleMessage + "Enable" : titleMessage + "Disable";
            Console.Title = titleMessage;
            if (setSystemProxy)
            {
                Pouyan.Network.Proxy.EnableProxy("127.0.0.1", 3080);
            }


            Console.WriteLine($"Connected - {orderedProfiles[0].Profile.Name}" +
                $"\n{orderedProfiles[0].Profile.Address}" +
                $"\n{orderedProfiles[0].Profile.Port}" +
                $"\n{orderedProfiles[0].Profile.Type?.ToString()}");
            Console.CancelKeyPress += new ConsoleCancelEventHandler((e, s) => OnProcessExit(_ct));
            AppDomain.CurrentDomain.ProcessExit += new EventHandler((s, e) => OnProcessExit(_ct));

            Task singBox = singbox.StartAsync(orderedProfiles[0].Profile, _ct);
        }
        public void WriteTestResult(IEnumerable<TestResult> profilesResults)
        {
            Console.WriteLine("Test Result:");
            foreach (TestResult result in profilesResults) {
                Console.WriteLine($"{result.Profile.Name} - Delay:{result.Result!.Delay}");
            }
        }

        public List<ProfileItem> TakeProfiles(string url)
        {
            var profiles = SingBox.GetProfile.GetProfilesFromSubscribe(url);

            Console.WriteLine($"{profiles.Count} Profiles Founded ");
            return profiles;
        }

        public static void OnProcessExit(CancellationTokenSource? cts = default)
        {
            Tunnel.CloseTunnel();
            cts?.Cancel();
            Proxy.DisableProxy();
        }

    }
}
