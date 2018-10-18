using System.Collections.Generic;
using System.Linq;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Fact = AdaptiveCards.Fact;

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

        public static readonly List<Choice> ConfirmCompleteChoices = new List<Choice>
        {
            new Choice("Continue")
            {
                Synonyms = new List<string> { "yes", "confirm", "affirmative" },
            },
            new Choice("Start Over")
            {
                Synonyms = new List<string> { "retry" },
            },
            new Choice("Cancel")
            {
                Synonyms = new List<string> { "quit", "exit", "bye" },
            },
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

        public static List<Fact> ToFactList(this BackPainDemographics source)
        {
            var race = Races.Single(r => r.Value.code == source.Race).Key;

            return new List<Fact>
            {
                new Fact("Age", source.Age, $"<s>age</s> {source.Age}"),
                new Fact("Biological Sex", source.BiologicalSex, $"<s>biological sex</s> {source.BiologicalSex}"),
                new Fact("History of Cancer", source.CancerHistory, $"<s>cancer history</s> {source.CancerHistory}"),
                new Fact("Psychiatric Care", source.PsychiatricCare, $"<s>psychiatric care</s> {source.PsychiatricCare}"),
                new Fact("Physical Therapy", source.HadPhysicalTherapy, $"<s>physical therapy</s> {source.HadPhysicalTherapy}"),
                new Fact("Cognitive Behavioral Therapy", source.CognitiveBehavioralTherapy, $"<s>cognitive behavioral therapy</s> {source.CognitiveBehavioralTherapy}"),
                new Fact("Back Surgery", source.PreviousBackSurgery, $"<s>back surgery</s> {source.PreviousBackSurgery}"),
                new Fact("Fever", source.Fever, $"<s>fever</s> {source.Fever}"),
                new Fact("Fecal Incontinence", source.FecalIncontinence, $"<s>fecal incontinence</s> {source.FecalIncontinence}"),
                new Fact("Opioid Use", source.OpioidUse, $"<s>opioid use</s> {source.OpioidUse}"),
                new Fact("Pain Level", source.LevelOfPain, $"<s>pain level</s> {source.LevelOfPain}"),
                new Fact("Race", race, $"<s>race</s> {race}"),
            };
        }
    }
}