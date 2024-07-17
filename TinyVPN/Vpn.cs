using Pouyan.SingBox.Model;
using SingBoxLib.Parsing;

namespace Pouyan
{
    public class Vpn
    {
        private static SingBox.ProfileTester? _testConfig;
        public Vpn(string singBoxExecutablePath)
        {
            _testConfig = new SingBox.ProfileTester(singBoxExecutablePath);
        }
        public async Task<IEnumerable<TestResult>> TestProfiles(int index, int count, List<ProfileItem> profiles)
        {
            Console.WriteLine($"Start Test Profiles...");
            return await _testConfig!.UrlTestAsync(profiles.GetRange(index, count));
        }
        
        public void WriteTestResult(List<TestResult> profilesResults)
        {
            Console.WriteLine("Test Result:");
            profilesResults.ForEach(p => Console.WriteLine($"{p.Profile.Name} - Delay:{p.Result!.Delay}"));
        }

        public List<ProfileItem> TakeProfiles(string url)
        {
            var profiles = SingBox.GetProfile.GetProfilesFromSubscribe(url);

            Console.WriteLine($"{profiles.Count} Profiles Founded ");
            return profiles;
        }
    }
}
