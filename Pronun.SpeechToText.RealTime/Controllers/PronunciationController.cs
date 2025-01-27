//using Microsoft.AspNetCore.Cors.Infrastructure;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.CognitiveServices.Speech;
//using Microsoft.CognitiveServices.Speech.Audio;

//using Microsoft.CognitiveServices.Speech.PronunciationAssessment;

//namespace Pronun.SpeechToText.RealTime.Controllers
//{
//    public class PronunciationController : Controller
//    {

//        [HttpGet]
//        public IActionResult Index()
//        {
//            return View();
//        }

//        [HttpPost]
//        public async Task<IActionResult> AssessPronunciation()
//        {
//            // Call pronunciation assessment using microphone
//            var result = await PronunciationAssessmentHelper.PronunciationAssessmentContinuousWithFile();

//            // Return the result as JSON
//            //return Json(result);
//            // Return the result as JSON
//            return Json(new { result });
//        }
      
//    }

//    public static class PronunciationAssessmentHelper
//    {
//        public static async Task<string> PronunciationAssessmentContinuousWithFile(string filePath = null)
//        {
//            var config = SpeechConfig.FromSubscription("eab6399a341a456aa6849fcd01f9a83e", "eastus");

//            // If a file is provided, use it
//            AudioConfig audioConfig;
//            if (!string.IsNullOrEmpty(filePath))
//            {
//                audioConfig = AudioConfig.FromWavFileInput(filePath);
//            }
//            else
//            {
//                // Otherwise, use microphone input
//                audioConfig = AudioConfig.FromDefaultMicrophoneInput();
//            }

//            var referenceText = "Crow was Thirsty.";

//            try
//            {
//                var recognizer = new SpeechRecognizer(config, audioConfig);

//                // Configure pronunciation assessment
//                var pronConfig = new PronunciationAssessmentConfig(referenceText, GradingSystem.HundredMark, Granularity.Phoneme);
//                pronConfig.ApplyTo(recognizer);

//                // Create a flag to track the recognition status
//                bool recognitionCompleted = false;
//                string resultMessage = string.Empty;

//                // Event handler for when speech is recognized
//                recognizer.Recognized += (s, e) =>
//                {
//                    if (e.Result.Reason == ResultReason.RecognizedSpeech)
//                    {
//                        // Perform pronunciation assessment
//                        var pronResult = PronunciationAssessmentResult.FromResult(e.Result);

//                        if (pronResult != null)
//                        {
//                            resultMessage = $"Pronunciation Score: {pronResult.PronunciationScore}, Accuracy: {pronResult.AccuracyScore}";
//                            Console.WriteLine(resultMessage);
                     

//                        }
//                        else
//                        {
//                            resultMessage = "Pronunciation assessment result is null.";
//                        }
//                        recognitionCompleted = true; // Mark as completed
//                    }
//                };

//                // Event handler for when recognition ends or is canceled
//                recognizer.Canceled += (s, e) =>
//                {
//                    if (e.Reason == CancellationReason.Error)
//                    {
//                        resultMessage = $"Recognition failed. Error: {e.ErrorDetails}";
//                    }
//                    recognitionCompleted = true;
//                };

//                // Start continuous recognition
//                await recognizer.StartContinuousRecognitionAsync();

//                // Wait until recognition is completed or canceled
//                while (!recognitionCompleted)
//                {
//                    await Task.Delay(100); // Wait briefly to avoid busy-waiting
                    
//                }

//                // Stop continuous recognition
//                await recognizer.StopContinuousRecognitionAsync();

//                return resultMessage;
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine(ex);
//                return ex.Message;
//            }

//        }
        
//    }

//    // Helper models
//    public class PronunciationAssessmentResultModel
//    {
//        public double PronunciationScore { get; set; }
//        public double AccuracyScore { get; set; }
//        public double CompletenessScore { get; set; }
//        public double FluencyScore { get; set; }
//        public double ProsodyScore { get; set; }
//        public List<WordResult> Words { get; set; }
//    }

//    public class WordResult
//    {
//        public string WordText { get; set; }
//        public double AccuracyScore { get; set; }
//        public string ErrorType { get; set; }
//    }


//}

