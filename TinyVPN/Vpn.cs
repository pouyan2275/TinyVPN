using Pouyan.SingBox.Model;
using SingBoxLib.Parsing;

namespace Pouyan
{
    public class Vpn
    {
        private static SingBox.Test? _testConfig;
        public Vpn(string singBoxExecutablePath)
        {
            _testConfig = new SingBox.Test(singBoxExecutablePath);
        }
        public List<Task<TestResult>> TestProfiles(int index, int count, SingBox.Client singbox, List<ProfileItem> profiles)
        {
            var testResults = new List<Task<TestResult>>();
            Console.WriteLine($"Start Test Profiles...");
            var freePorts = Network.Port.GetFreePorts(count + 1);
            int i = 0;
            profiles.GetRange(index, count).ForEach(p =>
            {
                i++;
                testResults.Add(_testConfig!.UrlTestAsync(p, freePorts[i],1000));
            });
            return testResults;
        }
        
        public TestResult[] CheckProfiles(List<Task<TestResult>> profilesResults)
        {
            var testedProfiles = Task.WhenAll(profilesResults).Result;
            Console.WriteLine("Test Result:");
            testedProfiles.ToList().ForEach(p => Console.WriteLine($"{p.Profile.Name} - Delay:{p.Result!.Delay}"));
            return testedProfiles;
        }

        public List<ProfileItem> TakeProfiles(string url)
        {
            var profiles = SingBox.GetProfile.GetProfilesFromSubscribe(url);

            Console.WriteLine($"{profiles.Count} Profiles Founded ");
            return profiles;
        }
    }
}
