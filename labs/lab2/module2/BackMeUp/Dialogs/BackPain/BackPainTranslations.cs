using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Builder.Dialogs.Choices;

namespace BackMeUp.Dialogs.BackPain
{
    public static class BackPainTranslations
    {
        public static string YesNo(bool value)
        {
            return value ? "yes" : "no";
        }

        public static readonly Dictionary<string, string> Treatments = new Dictionary<string, string>
            {
                {"22554", "Anterior Interbody Fusion"},
                {"22524", "Percutaneous Vertebral Augmentation"},
                {"97111", "Physical Therapy"},
                {"97110", "OTC Rx"},
                {"76910", "Epidural"},
                {"76911", "Physiatry"},
                {"63030", "Posterior Lamina Removal with Decomp"},
            };

        public static readonly List<Choice> PainLevelOptions = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }.Select(x => new Choice(x.ToString())).ToList();

        public static IDictionary<string, (string code, Choice choice)> BiologicalSexes => new Dictionary<string, (string code, Choice choice)>
        {
            {
                "Unspecified", ("unspecified", new Choice("Unspecified"))
            },
            {
                "Intersex", ("intersex", new Choice("Intersex"))
            },
            {
                "Female", ("female", new Choice("Female")
                {
                    Synonyms = new List<string> { "Woman", "Girl" },
                })
            },
            {
                "Male", ("male", new Choice("Male")
                {
                    Synonyms = new List<string> { "Man", "Boy" },
                })
            },
        };

        public static IDictionary<string, (string code, Choice choice)> Races => new Dictionary<string, (string code, Choice choice)>
        {
            {
                "African American", ("B", new Choice("African American")
                {
                    Synonyms = new List<string> { "Black" },
                })
            },
            {
                "Caucasian", ("W", new Choice("Caucasian")
                {
                    Synonyms = new List<string> { "White" },
                })
            },
            {
                "Asian", ("A", new Choice("Asian"))
            },
            {
                "Hispanic", ("H", new Choice("Hispanic")
                {
                    Synonyms = new List<string> { "Latino", "Latina", "Mexican", "Mexican American" },
                })
            },
            {
                "Other", ("U", new Choice("Other"))
            },
        };
    }
}