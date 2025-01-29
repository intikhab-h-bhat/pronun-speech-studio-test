using Microsoft.AspNetCore.Mvc;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using Microsoft.CognitiveServices.Speech;
using System.Text.RegularExpressions;
using System;

namespace Pronun.SpeechToText.RealTime.Controllers
{
    public class Test2Controller : Controller
    {
        public class AssessmentResult
        {
            public List<Word> Words { get; set; }
            public double PronunciationScore { get; set; }
            public double AccuracyScore { get; set; }
            public double CompletenessScore { get; set; }
            public double FluencyScore { get; set; }
            public double ProsodyScore { get; set; }

            public AssessmentResult()
            {
                Words = new List<Word>();
            }
        }

        public class Word
        {
            public string WordText { get; set; }
            public string ErrorType { get; set; }
            public double AccuracyScore { get; set; }

            public Word(string wordText, string errorType, double accuracyScore = 0)
            {
                WordText = wordText;
                ErrorType = errorType;
                AccuracyScore = accuracyScore;
            }
        }

        public IActionResult Index()
        {
            return View(new AssessmentResult());
        }

        [HttpPost]
        public async Task<IActionResult> AssessPronunciation()
        {
            var result = await PronunciationAssessmentContinuousWithMicrophone();
            return View("Index", result);
        }

        public static async Task<AssessmentResult> PronunciationAssessmentContinuousWithMicrophone()
        {
            var result = new AssessmentResult();

            // Replace with your own subscription key and service region (e.g., "westus").
            var config = SpeechConfig.FromSubscription("eab6399a341a456aa6849fcd01f9a83e", "eastus");

            try
            {
                // Creates a speech recognizer using the default microphone as audio input.
                using (var audioInput = AudioConfig.FromDefaultMicrophoneInput())
            {
                var language = "en-US";

                using (var recognizer = new SpeechRecognizer(config, language, audioInput))
                {
                    var referenceText = "Today was a beautiful day. We had a great time taking a long walk outside in the morning. The countryside was in full bloom, yet the air was crisp and cold. Towards the end of the day, clouds came in, forecasting much needed rain.";

                    // Create pronunciation assessment config.
                    var pronConfig = new PronunciationAssessmentConfig(referenceText, GradingSystem.HundredMark, Granularity.Phoneme, enableMiscue: true);
                    pronConfig.EnableProsodyAssessment();
                    pronConfig.ApplyTo(recognizer);

                    var recognizedWords = new List<string>();
                    var pronWords = new List<Word>();
                    var fluency_scores = new List<double>();
                    var prosody_scores = new List<double>();
                    var durations = new List<int>();
                    var done = false;

                    recognizer.SessionStopped += (s, e) =>
                    {
                        Console.WriteLine("Session stopped.");
                        done = true;
                    };

                    recognizer.Canceled += (s, e) =>
                    {
                        Console.WriteLine($"Recognition canceled: Reason={e.Reason}");
                        done = true;
                    };

                    recognizer.Recognized += (s, e) =>
                    {
                        Console.WriteLine($"Recognized: Text={e.Result.Text}");

                        if (e.Result.Reason == ResultReason.RecognizedSpeech)
                        {
                            var pronResult = PronunciationAssessmentResult.FromResult(e.Result);
                            if (pronResult != null)
                            {
                                Console.WriteLine($"Accuracy score: {pronResult.AccuracyScore}, pronunciation score: {pronResult.PronunciationScore}, completeness score: {pronResult.CompletenessScore}, fluency score: {pronResult.FluencyScore}, prosody score: {pronResult.ProsodyScore}");

                                fluency_scores.Add(pronResult.FluencyScore);
                                prosody_scores.Add(pronResult.ProsodyScore);

                                foreach (var word in pronResult.Words)
                                {
                                    var newWord = new Word(word.Word, word.ErrorType, word.AccuracyScore);
                                    pronWords.Add(newWord);
                                }
                            }
                            else
                            {
                                Console.WriteLine("Pronunciation assessment result is null.");
                            }
                            done = true;
                        }
                    };

                    // Starts continuous recognition.
                    await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                    Console.WriteLine("Speak into your microphone...");
                    while (!done)
                    {
                        await Task.Delay(1000);
                    }

                    // Stops recognition.
                    await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);

                    // Post-processing to calculate scores.
                    string[] referenceWords = referenceText.ToLower().Split(' ');
                    for (int j = 0; j < referenceWords.Length; j++)
                    {
                        referenceWords[j] = Regex.Replace(referenceWords[j], "^[\\p{P}\\s]+|[\\p{P}\\s]+$", "");
                    }

                    result.Words = pronWords;

                    // Calculate overall scores.
                    var filteredWords = pronWords.Where(item => item.ErrorType != "Insertion");
                    result.AccuracyScore = filteredWords.Sum(item => item.AccuracyScore) / filteredWords.Count();
                    result.ProsodyScore = prosody_scores.Sum() / prosody_scores.Count();
                    result.FluencyScore = fluency_scores.Sum();
                    result.CompletenessScore = (double)pronWords.Count(item => item.ErrorType == "None") / referenceWords.Length * 100;
                    result.CompletenessScore = result.CompletenessScore <= 100 ? result.CompletenessScore : 100;

                    result.PronunciationScore = result.AccuracyScore * 0.4 + result.ProsodyScore * 0.2 +
                                              result.FluencyScore * 0.2 + result.CompletenessScore * 0.2;

                    Console.WriteLine($"Paragraph pronunciation score: {result.PronunciationScore}, " +
                                    $"accuracy score: {result.AccuracyScore}, " +
                                    $"completeness score: {result.CompletenessScore}, " +
                                    $"fluency score: {result.FluencyScore}, " +
                                    $"prosody score: {result.ProsodyScore}");

                    for (int idx = 0; idx < result.Words.Count(); idx++)
                    {
                        Word word = result.Words[idx];
                        Console.WriteLine($"{idx + 1}: word: {word.WordText}\taccuracy score: {word.AccuracyScore}\terror type: {word.ErrorType}");
                    }
                }
            }

            
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                // Log the exception details for further investigation
            }

            return result;
        }

    }
}
