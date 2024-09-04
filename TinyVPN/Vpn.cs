using Pouyan.SingBox.Model;
using SingBoxLib.Parsing;

namespace Pouyan
{
    public class Vpn
    { 
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
    }
}
