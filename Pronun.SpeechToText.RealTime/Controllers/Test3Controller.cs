
using Microsoft.AspNetCore.Mvc;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.PronunciationAssessment;
using Microsoft.CognitiveServices.Speech;
using System.Text.RegularExpressions;
using System.Linq.Expressions;

namespace Pronun.SpeechToText.RealTime.Controllers
{
    public class Test3Controller : Controller
    {
        private readonly ILogger<Test3Controller> _logger;
        public Test3Controller(ILogger<Test3Controller> logger)
        {
            _logger = logger;
        }

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
        [RequestSizeLimit(20 * 1024 * 1024)]
        public async Task<IActionResult> ProcessAudioStream()
        {
            var result = new AssessmentResult();
            var tempPath = Path.GetTempFileName();

            try
            {
                var audioFile = Request.Form.Files.FirstOrDefault();
                if (audioFile == null)
                {
                    _logger.LogError("No audio file received");
                    return BadRequest("No audio file received");
                }

                _logger.LogInformation($"Received audio file. Size: {audioFile.Length} bytes, Type: {audioFile.ContentType}");

                // Save the received audio file
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await audioFile.CopyToAsync(stream);
                }

                _logger.LogInformation($"Audio file saved to temp path: {tempPath}");

                // Configure speech service
                var config = SpeechConfig.FromSubscription("Key", "eastus");
                using var audioConfig = AudioConfig.FromWavFileInput(tempPath);

                using (var recognizer = new SpeechRecognizer(config, "en-US", audioConfig))
                {
                    var referenceText = "Today was a beautiful day. " +
                        "We had a great time taking a long walk outside in the morning." +
                        " The countryside was in full bloom, yet the air was crisp and cold." +
                        " Towards the end of the day, " +
                        "clouds came in, forecasting much needed rain.";

                    // Add cancellation handler
                    recognizer.Canceled += (s, e) =>
                    {
                        _logger.LogError($"Recognition canceled. Error code: {e.ErrorCode}, Error details: {e.ErrorDetails}");
                    };

                    // Configure pronunciation assessment
                    var pronConfig = new PronunciationAssessmentConfig(
                        referenceText,
                        GradingSystem.HundredMark,
                        Granularity.Phoneme,
                        enableMiscue: true);

                    pronConfig.EnableProsodyAssessment();
                    pronConfig.ApplyTo(recognizer);

                    _logger.LogInformation("Starting speech recognition...");

                    // Perform recognition with timeout
                    var recognitionTask = recognizer.RecognizeOnceAsync();
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30)); // 30 second timeout

                    var completedTask = await Task.WhenAny(recognitionTask, timeoutTask);
                    if (completedTask == timeoutTask)
                    {
                        _logger.LogError("Recognition timed out after 30 seconds");
                        return StatusCode(500, "Recognition timed out");
                    }

                    var recognitionResult = await recognitionTask;
                    _logger.LogInformation($"Recognition completed. Result reason: {recognitionResult.Reason}");

                    if (recognitionResult.Reason == ResultReason.RecognizedSpeech)
                    {
                        _logger.LogInformation($"Recognized text: {recognitionResult.Text}");
                        var pronResult = PronunciationAssessmentResult.FromResult(recognitionResult);

                        if (pronResult != null)
                        {
                            result.AccuracyScore = pronResult.AccuracyScore;
                            result.PronunciationScore = pronResult.PronunciationScore;
                            result.CompletenessScore = pronResult.CompletenessScore;
                            result.FluencyScore = pronResult.FluencyScore;
                            result.ProsodyScore = pronResult.ProsodyScore;

                            foreach (var word in pronResult.Words)
                            {
                                result.Words.Add(new Word(
                                    word.Word,
                                    word.ErrorType,
                                    word.AccuracyScore));
                            }

                            _logger.LogInformation($"Assessment completed. Pronunciation score: {result.PronunciationScore}");
                        }
                        else
                        {
                            _logger.LogError("Pronunciation assessment result was null");
                            return StatusCode(500, "Failed to assess pronunciation");
                        }
                    }
                    else if (recognitionResult.Reason == ResultReason.NoMatch)
                    {
                        var noMatchDetails = NoMatchDetails.FromResult(recognitionResult);
                        _logger.LogError($"NoMatch reason: {noMatchDetails.Reason}");
                        return StatusCode(500, $"Speech not recognized. NoMatch reason: {noMatchDetails.Reason}");
                    }
                    else if (recognitionResult.Reason == ResultReason.Canceled)
                    {
                        var cancellation = CancellationDetails.FromResult(recognitionResult);
                        _logger.LogError($"Recognition canceled. Reason: {cancellation.Reason}, Error code: {cancellation.ErrorCode}, Error details: {cancellation.ErrorDetails}");
                        return StatusCode(500, $"Recognition canceled. Reason: {cancellation.Reason}, Error code: {cancellation.ErrorCode}");
                    }
                }

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing audio");
                return StatusCode(500, $"Error processing audio: {ex.Message}");
            }
            finally
            {
                if (System.IO.File.Exists(tempPath))
                {
                    System.IO.File.Delete(tempPath);
                    _logger.LogInformation("Temporary audio file deleted");
                }
            }
        }
    }
}