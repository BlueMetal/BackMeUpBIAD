using System.Collections.Generic;

namespace BackMeUp.AzureML.Models
{
    public class RootObject
    {
        public Results Results { get; set; }

        public Dictionary<string, string> GlobalParameters { get; set; }
            = new Dictionary<string, string>();
    }
}