using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using BackMeUp.AzureML.Models;
using BackMeUp.Dialogs.BackPain;
using Newtonsoft.Json;

namespace BackMeUp.AzureML
{
    public class HealthOutcomeService
    {
        private readonly string _apiKey;
        private readonly Uri _azureMachineLearningUri;

        public HealthOutcomeService(Uri azureMachineLearningUri, string apiKey)
        {
            _azureMachineLearningUri = azureMachineLearningUri;
            _apiKey = apiKey;
        }

        public async Task<List<MachineLearningData>> GetOutcomesForTreatmentsAsync(
            BackPainDemographics demographics,
            Dictionary<string, string> treatments,
            CancellationToken cancellationToken)
        {
            // Runs Azure Machine Learning calls for the treatments in parallel to get results faster
            var awaitedResults = await Task.WhenAll(treatments
                .Keys
                .AsParallel()
                .Select(t => GetOutcomeForTreatmentAsync(demographics, t, cancellationToken)));

            // Orders the results by the result strength
            var results = awaitedResults
                .Where(y => y.ResultStrength < int.MaxValue)
                .OrderBy(y => y.ResultStrength)
                .ToList();
            return results;
        }

        private async Task<MachineLearningData> GetOutcomeForTreatmentAsync(
            BackPainDemographics demographics,
            string treatmentCode,
            CancellationToken cancellationToken)
        {
            using (var client = new HttpClient())
            {
                // Azure Machine Learning API uses strange data structures
                var scoreRequest = new
                {
                    Inputs = new Dictionary<string, List<Dictionary<string, string>>>
                    {
                        {
                            "input1",
                            new List<Dictionary<string, string>>
                            {
                                new Dictionary<string, string>
                                {
                                    { "ID", "1" },
                                    { "Diagnosis", string.Empty },
                                    { "Spinal_Canal_Stenosis", string.Empty },
                                    { "Neural_Foraminal_Narrowing", string.Empty },
                                    { "Disc", string.Empty },
                                    { "Annular_Tear", string.Empty },
                                    { "Ligamentum_Flavum", string.Empty },
                                    { "Facet_Arthropathy", string.Empty },
                                    { "Synovial_Cyst_mm", "1" },
                                    { "Perineurial_Cyst_mm", "1" },
                                    { "Tarlov_Cyst_mm", "1" },
                                    { "Sponylolysis", string.Empty },
                                    { "Spondyolithesis", string.Empty },
                                    { "Anterolithesis", string.Empty },
                                    { "Retrolisthesis", string.Empty },
                                    { "Compression", string.Empty },
                                    { "Pain_Chronicity", "1" },
                                    { "Pain_Severity", demographics.LevelOfPain },
                                    { "Radiculopathy", string.Empty },
                                    { "Cancer_History", demographics.CancerHistory },
                                    { "Spinal_Infect_Hist", string.Empty },
                                    { "Cauda_Equina", string.Empty },
                                    { "Neurologic_Deficits", string.Empty },
                                    { "Osteoporosis", string.Empty },
                                    { "Osteopenia", string.Empty },
                                    { "Prev_PT", demographics.HadPhysicalTherapy },
                                    { "Prev_CBT", demographics.CognitiveBehavioralTherapy },
                                    { "Prev_Exercise_Thera", string.Empty },
                                    { "Prev_Pharma_Thera", string.Empty },
                                    { "Prev_Surgical_Thera", string.Empty },
                                    { "Unexplained_Wt_Loss", string.Empty },
                                    { "Fever", demographics.Fever },
                                    { "IVDU", string.Empty },
                                    { "Recent_Infection", string.Empty },
                                    { "Urinary_Retention", string.Empty },
                                    { "Fecal_Incontinence", string.Empty },
                                    { "Saddle_Anesthesia", string.Empty },
                                    { "Use_Corticosterioids", string.Empty },
                                    { "Management_Plan", string.Empty },
                                    { "Opioid_Use", demographics.OpioidUse },
                                    { "Psychiatric_History", demographics.PsychiatricCare },
                                    { "PCP", string.Empty },
                                    { "Self_Referral", string.Empty },
                                    { "Gender", demographics.BiologicalSex },
                                    { "Age", demographics.Age },
                                    { "Height_in", "1" },
                                    { "Weight_lbs", "1" },
                                    { "BMI", "1" },
                                    { "Race", demographics.Race },
                                    { "National_Origin", string.Empty },
                                    { "Marital_Status", string.Empty },
                                    { "Income_Level", "1" },
                                    { "Zip_Code", "1" },
                                    { "Education", string.Empty },
                                    { "Encounter_Date", string.Empty },
                                    { "Prior_Surgery", demographics.PreviousBackSurgery },
                                    { "Acute_Chronic", string.Empty },
                                    { "Overall_Health", string.Empty },
                                    { "Activity_Level", "1" },
                                    { "Sport", string.Empty },
                                    { "Employment_Activity", string.Empty },
                                    { "TV_Hours_Per_Day", "1" },
                                    { "Treatment", treatmentCode },
                                    { "Outcome", string.Empty },
                                },
                            }
                        },
                    },
                    GlobalParameters = new Dictionary<string, string>(),
                };

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                client.BaseAddress = _azureMachineLearningUri;

                // WARNING: The 'await' statement below can result in a deadlock
                // if you are calling this code from the UI thread of an ASP.Net application.
                // One way to address this would be to call ConfigureAwait(false)
                // so that the execution does not attempt to resume on the original context.
                // For instance, replace code such as:
                //      result = await DoSomeTask()
                // with the following:
                //      result = await DoSomeTask().ConfigureAwait(false)
                var response = await client.PostAsJsonAsync(string.Empty, scoreRequest, cancellationToken).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var output = JsonConvert.DeserializeObject<RootObject>(result);
                    return output.Results.output1.FirstOrDefault();
                }

                Console.WriteLine("The request failed with status code: {0}", response.StatusCode);

                // Print the headers - they include the request ID and the timestamp,
                // which are useful for debugging the failure
                Console.WriteLine(response.Headers.ToString());

                return new MachineLearningData
                {
                    Treatment = treatmentCode,
                    ResultStrength = int.MaxValue,
                };
            }
        }
    }
}