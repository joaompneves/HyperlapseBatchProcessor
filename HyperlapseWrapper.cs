using Microsoft.Research.Hyperlapse.Desktop;
using Microsoft.Research.VisionTools.Toolkit.Desktop.Native;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace HyperlapseBatchProcessor
{
    class HyperlapseWrapper
    {
        private static AutoResetEvent processingEvent = new AutoResetEvent(false);

        private static VideoReader videoReader = new VideoReader();
        private static CalibrationProvider calibrationProvider = new CalibrationProvider();
        private static CalibrationMatcher calibrationMatcher = new CalibrationMatcher(calibrationProvider);
        private static VideoBitrateEstimator videoBitrateEstimator = new VideoBitrateEstimator();

        public static void ProcessFiles(FileInfo[] files, int speedupFactor, Rational outputFramesPerSecond)
        {
            var i = 1;
            var filesCount = files.Count();
            var engine = new HyperlapseEngine();

            engine.ProcessingCancelled += OnEngineProcessingCancelled;
            engine.ProcessingFailed += OnEngineProcessingFailed;
            engine.ProcessingFinished += OnEngineProcessingFinished;
            engine.ProgressChanged += OnEngineProgressChanged;
            engine.TrialStatusChanged += OnEngineTrialStatusChanged;

            foreach (var file in files)
            {
                var processingMsg = "[Processing file " + i++ + " of " + filesCount + "] - " + file.Name;
                Console.Title = processingMsg;
                Console.WriteLine(processingMsg);

                var fileOutput = new FileInfo(Path.Combine(file.DirectoryName, "Output", file.Name));
                if (!fileOutput.Directory.Exists)
                {
                    fileOutput.Directory.Create(); // create output dir
                }

                Process(engine, file, fileOutput, speedupFactor, outputFramesPerSecond);
            }

            engine.Dispose();
        }

        private static void Process(HyperlapseEngine engine, FileInfo fileInput, FileInfo fileOutput, int speedupFactor, Rational outputFramesPerSecond)
        {
            var hyperlapseParameters = new HyperlapseParameters();

            var videoInfo = videoReader.ReadInfoFromFile(fileInput.FullName);
            var calibrationInfoForVideo = calibrationMatcher.FindCalibrationInfoForVideo(videoInfo);

            hyperlapseParameters.CalibrationFile = calibrationInfoForVideo.Calibration;

            hyperlapseParameters.StartFrame = 0;
            hyperlapseParameters.EndFrame = int.MaxValue; // all video
            hyperlapseParameters.VideoMode = calibrationInfoForVideo.VideoMode;
            hyperlapseParameters.FrameRate = videoInfo.FramesPerSecond;

            hyperlapseParameters.OutputHeight = videoInfo.Height;
            hyperlapseParameters.OutputRotation = videoInfo.Rotation;
            hyperlapseParameters.OutputWidth = videoInfo.Width;
            hyperlapseParameters.OutputBitrate = (int)videoBitrateEstimator.EstimateBitsPerSecond(videoInfo.BitsPerSecond,
                                                                                                   videoInfo.Width,
                                                                                                   videoInfo.Height,
                                                                                                   videoInfo.Width,
                                                                                                   videoInfo.Height,
                                                                                                   videoInfo.FramesPerSecond,
                                                                                                   outputFramesPerSecond ?? videoInfo.FramesPerSecond);

            hyperlapseParameters.SpeedupFactor = speedupFactor;

            hyperlapseParameters.VideoUri = new Uri(fileInput.FullName);
            hyperlapseParameters.VideoOutputFilePath = fileOutput.FullName;
            hyperlapseParameters.TempOutputDirectory = fileOutput.DirectoryName;

            hyperlapseParameters.CreditLength = 0;
            hyperlapseParameters.UseAdvancedSmoothing = false;
            hyperlapseParameters.ForceSoftwareRendering = false;
            hyperlapseParameters.UseGeometryShaders = false;
            hyperlapseParameters.UseHardwareVideoEncoder = false; // some videos fail when this is on

            engine.Start(hyperlapseParameters);
            processingEvent.WaitOne();
        }

        private static void OnEngineTrialStatusChanged(object sender, EventArgs e)
        {

        }

        private static void OnEngineProgressChanged(object sender, EventArgs e)
        {

        }

        private static void OnEngineProcessingFinished(object sender, ProcessingFinishedEventArgs e)
        {
            processingEvent.Set();
        }

        private static void OnEngineProcessingFailed(object sender, ProcessingFailedEventArgs e)
        {
            Console.WriteLine("Error: " + e.ErrorMessage);
            processingEvent.Set();
        }

        private static void OnEngineProcessingCancelled(object sender, EventArgs e)
        {
            Console.WriteLine("[Cancelled]");
            processingEvent.Set();
        }
    }
}
