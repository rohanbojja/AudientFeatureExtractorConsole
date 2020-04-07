using NWaves.Audio;
using NWaves.FeatureExtractors;
using NWaves.FeatureExtractors.Multi;
using NWaves.FeatureExtractors.Options;
using NWaves.FeatureExtractors.Serializers;
using NWaves.Features;
using NWaves.Signals;
using NWaves.Transforms;
using NWaves.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace audient_feature_extractor
{
    class Program
    {
        private static IList<float[]> mfccVectors;
        private static IList<float[]> tdVectors;

        static void Main(string[] args)
        {
            DiscreteSignal signal;

            // load
            var samplingRate = 16000;
            var mfccOptions = new MfccOptions
            {
                SamplingRate = 16000,
                FeatureCount = 13,
                FrameDuration = 0.025/*sec*/,
                HopDuration = 0.010/*sec*/,
                PreEmphasis = 0.97,
                Window = WindowTypes.Hamming
            };

            var opts = new MultiFeatureOptions
            {
                SamplingRate = 16000,
                FrameDuration = 0.025,
                HopDuration = 0.010
            };

            

            var tdExtractor = new TimeDomainFeaturesExtractor(opts);

            var mfccExtractor = new MfccExtractor(mfccOptions);

            var folders = Directory.GetDirectories(Path.Combine(Environment.CurrentDirectory, "Dataset"));
            Console.WriteLine($"Started!");
            using (var writer = File.CreateText(Path.Combine(Environment.CurrentDirectory, "Data.csv")))
            {
                //Write header
                var main_header = "genre,";
                main_header += String.Join(",", mfccExtractor.FeatureDescriptions);
                main_header += ",";
                main_header += String.Join(",", tdExtractor.FeatureDescriptions);
                main_header += ",centroid,spread,flatness,noiseness,roloff,crest,decrease,entropy";
                writer.WriteLine(main_header);
                string feature_string = String.Empty;
                foreach (var folder in folders)
                {
                    var f_name = new DirectoryInfo(folder).Name;
                    var files = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "Dataset", folder));
                    //Write the genre label here
                    Console.WriteLine($"{f_name}");
                    foreach (var filename in files)
                    {
                        feature_string = String.Empty;
                        feature_string = $"{f_name},";
                        //MFCC
                        var avg_vec_mfcc = new List<float>(14);
                        //TD Features
                        var avg_vec_td = new List<float>(4);
                        //Spectral features
                        var avg_vec_spect = new List<float>(10);

                        for (var i = 0; i < 13; i++)
                        {
                            avg_vec_mfcc.Add(0f);
                        }
                        for (var i = 0; i < 4; i++)
                        {
                            avg_vec_td.Add(0f);
                        }

                        for (var i = 0; i < 10; i++)
                        {
                            avg_vec_spect.Add(0f);
                        }

                        string specFeatures = String.Empty;
                        using (var stream = new FileStream(Path.Combine(Environment.CurrentDirectory, "Dataset", filename), FileMode.Open))
                        {
                            var waveFile = new WaveFile(stream);
                            signal = waveFile[Channels.Average];
                            //Compute MFCC
                            tdVectors = tdExtractor.ComputeFrom(signal);
                            mfccVectors = mfccExtractor.ComputeFrom(signal);
                            var fft = new Fft(1024);
                            var fftSize = 1024;
                            var resolution = (float)samplingRate / fftSize;

                            var frequencies = Enumerable.Range(0, fftSize / 2 + 1)
                                                        .Select(f => f * resolution)
                                                        .ToArray();

                            var spectrum = new Fft(fftSize).MagnitudeSpectrum(signal).Samples;

                            var centroid = Spectral.Centroid(spectrum, frequencies);
                            var spread = Spectral.Spread(spectrum, frequencies);
                            var flatness = Spectral.Flatness(spectrum, 0);
                            var noiseness = Spectral.Noiseness(spectrum, frequencies, 3000);
                            var rolloff = Spectral.Rolloff(spectrum, frequencies, 0.85f);
                            var crest = Spectral.Crest(spectrum);
                            var decrease = Spectral.Decrease(spectrum);
                            var entropy = Spectral.Entropy(spectrum);
                            specFeatures = $"{centroid},{spread},{flatness},{noiseness},{rolloff},{crest},{decrease},{entropy}";
                        }

                        //Write label here TODO

                        foreach (var inst in mfccVectors)
                        {
                            for (var i = 0; i < 13; i++)
                            {
                                avg_vec_mfcc[i] += inst[i];
                            }
                        }

                        foreach (var inst in tdVectors)
                        {
                            for (var i = 0; i < 4; i++)
                            {
                                avg_vec_td[i] += inst[i];
                            }
                        }
                        
                        for (var i = 0; i < 13; i++)
                        {
                            avg_vec_mfcc[i] /= mfccVectors.Count;
                        }

                        for (var i = 0; i < 4; i++)
                        {
                            avg_vec_td[i] /= tdVectors.Count;
                        }


                        // Write MFCCs
                        feature_string += String.Join(",", avg_vec_mfcc);
                        feature_string += ",";
                        feature_string += String.Join(",", avg_vec_td);
                        //Write Spectral features as well
                        feature_string += ",";
                        feature_string += specFeatures;
                        writer.WriteLine(feature_string); 
                        var file_name = new DirectoryInfo(filename).Name;
                        Console.WriteLine($"{file_name}");
                    }
                }
            }
            Console.WriteLine($"DONE");
            Console.ReadLine();
        }
    }
}
