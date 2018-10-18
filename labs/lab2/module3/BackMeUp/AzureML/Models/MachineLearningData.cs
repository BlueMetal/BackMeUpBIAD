using Newtonsoft.Json;

namespace BackMeUp.AzureML.Models
{
    public class MachineLearningData
    {
        public string ID { get; set; }
        public string Diagnosis { get; set; }
        public string Spinal_Canal_Stenosis { get; set; }
        public string Neural_Foraminal_Narrowing { get; set; }
        public string Disc { get; set; }
        public string Annular_Tear { get; set; }
        public string Ligamentum_Flavum { get; set; }
        public string Facet_Arthropathy { get; set; }
        public string Synovial_Cyst_mm { get; set; }
        public string Perineurial_Cyst_mm { get; set; }
        public string Tarlov_Cyst_mm { get; set; }
        public string Sponylolysis { get; set; }
        public string Spondyolithesis { get; set; }
        public string Anterolithesis { get; set; }
        public string Retrolisthesis { get; set; }
        public string Compression { get; set; }
        public string Pain_Chronicity { get; set; }
        public string Pain_Severity { get; set; }
        public string Radiculopathy { get; set; }
        public string Cancer_History { get; set; }
        public string Spinal_Infect_Hist { get; set; }
        public string Cauda_Equina { get; set; }
        public string Neurologic_Deficits { get; set; }
        public string Osteoporosis { get; set; }
        public string Osteopenia { get; set; }
        public string Prev_PT { get; set; }
        public string Prev_CBT { get; set; }
        public string Prev_Exercise_Thera { get; set; }
        public string Prev_Pharma_Thera { get; set; }
        public string Prev_Surgical_Thera { get; set; }
        public string Unexplained_Wt_Loss { get; set; }
        public string Fever { get; set; }
        public string IVDU { get; set; }
        public string Recent_Infection { get; set; }
        public string Urinary_Retention { get; set; }
        public string Fecal_Incontinence { get; set; }
        public string Saddle_Anesthesia { get; set; }
        public string Use_Corticosterioids { get; set; }
        public string Management_Plan { get; set; }
        public string Opioid_Use { get; set; }
        public string Psychiatric_History { get; set; }
        public string PCP { get; set; }
        public string Self_Referral { get; set; }
        public string Gender { get; set; }
        public string Age { get; set; }
        public string Height_in { get; set; }
        public string Weight_lbs { get; set; }
        public string BMI { get; set; }
        public string Race { get; set; }
        public string National_Origin { get; set; }
        public string Marital_Status { get; set; }
        public string Income_Level { get; set; }
        public string Zip_Code { get; set; }
        public string Education { get; set; }
        public string Encounter_Date { get; set; }
        public string Prior_Surgery { get; set; }
        public string Acute_Chronic { get; set; }
        public string Overall_Health { get; set; }
        public string Activity_Level { get; set; }
        public string Sport { get; set; }
        public string Employment_Activity { get; set; }
        public string TV_Hours_Per_Day { get; set; }
        public string Treatment { get; set; }
        public string Outcome { get; set; }

        [JsonProperty(PropertyName = "Scored Probabilities for Class \"Day1_Repeat_Surgery\"")]
        [JsonConverter(typeof(DoubleConverter))]
        public double? Day1RepeatSurgery { get; set; }
        [JsonProperty(PropertyName = "Scored Probabilities for Class \"Day1_Success\"")]
        [JsonConverter(typeof(DoubleConverter))]
        public double? Day1Success { get; set; }
        [JsonProperty(PropertyName = "Scored Probabilities for Class \"Day1_Unsuccessful\"")]
        [JsonConverter(typeof(DoubleConverter))]
        public double? Day1Unsuccessful { get; set; }
        [JsonProperty(PropertyName = "Scored Probabilities for Class \"Long_Term_Pain_Mngmnt\"")]
        [JsonConverter(typeof(DoubleConverter))]
        public double? LongTermPainManagement { get; set; }
        [JsonProperty(PropertyName = "Scored Probabilities for Class \"Month3_Repeat_Surgery\"")]
        [JsonConverter(typeof(DoubleConverter))]
        public double? Month3RepeatSurgery { get; set; }
        [JsonProperty(PropertyName = "Scored Probabilities for Class \"Month3_Success\"")]
        [JsonConverter(typeof(DoubleConverter))]
        public double? Month3Success { get; set; }
        [JsonProperty(PropertyName = "Scored Probabilities for Class \"Month3_Unsuccessful\"")]
        [JsonConverter(typeof(DoubleConverter))]
        public double? Month3Unsuccessful { get; set; }
        [JsonProperty(PropertyName = "Scored Probabilities for Class \"Week1_Repeat_Surgery\"")]
        [JsonConverter(typeof(DoubleConverter))]
        public double? Week1RepeatSurgery { get; set; }
        [JsonProperty(PropertyName = "Scored Probabilities for Class \"Week1_Success\"")]
        [JsonConverter(typeof(DoubleConverter))]
        public double? Week1Success { get; set; }
        [JsonProperty(PropertyName = "Scored Probabilities for Class \"Week1_Unsuccessful\"")]
        [JsonConverter(typeof(DoubleConverter))]
        public double? Week1Unsuccessful { get; set; }
        [JsonProperty(PropertyName = "Scored Labels")]
        [JsonConverter(typeof(SortOrderConverter))]
        public int ResultStrength { get; set; }
    }
}