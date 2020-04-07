using NWaves.Audio;
using NWaves.FeatureExtractors;
using NWaves.FeatureExtractors.Multi;
using NWaves.FeatureExtractors.Options;
using NWaves.FeatureExtractors.Serializers;
using NWaves.Signals;
using NWaves.Windows;
using System;
using System.Collections.Generic;
using System.IO;

namespace audient_feature_extractor
{
    class Program
    {
        private static IList<float[]> mfccVectors;
        private static IList<float[]> tdVectors;

        static async System.Threading.Tasks.Task Main(string[] args)
        {
            DiscreteSignal signal;

            // load
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

            var folders = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "Dataset"));
            using (var writer = File.CreateText(Path.Combine(Environment.CurrentDirectory, "Data.csv")))
            {
                //Write header
                var main_header = "genre,";
                main_header += String.Join(",", mfccExtractor.FeatureDescriptions);
                
                string feature_string = String.Empty;
                foreach (var folder in folders)
                {
                    feature_string = $"{folder},";
                    var files = Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "Dataset", folder));
                    //Write the genre label here

                    writer.WriteLine(main_header);
                    foreach (var filename in files)
                    {
                        //MFCC
                        var avg_vec_mfcc = new List<float>(14);
                        //TD Features
                        var avg_vec_td = new List<float>(4);
                        //Spectral features



                        for (var i = 0; i < 13; i++)
                        {
                            avg_vec_mfcc.Add(0f);
                        }
                        for (var i = 0; i < 4; i++)
                        {
                            avg_vec_td.Add(0f);
                        }

                        using (var stream = new FileStream(Path.Combine(Environment.CurrentDirectory, "Dataset", filename), FileMode.Open))
                        {
                            var waveFile = new WaveFile(stream);
                            signal = waveFile[Channels.Average];
                            //Compute MFCC
                            tdVectors = tdExtractor.ComputeFrom(signal);
                            mfccVectors = mfccExtractor.ComputeFrom(signal);

                        }

                        Console.WriteLine(tdVectors.Count);

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

                        // Write MFCCs
                        for (var i = 0; i < 13; i++)
                        {
                            avg_vec_mfcc[i] /= mfccVectors.Count;
                        }

                        for (var i = 0; i < 13; i++)
                        {
                            avg_vec_td[i] /= tdVectors.Count;
                        }

                        feature_string = String.Join(",", avg_vec_mfcc);
                        feature_string += String.Join(",", avg_vec_td);
                        //Write Spectral features as well


                        writer.WriteLine(feature_string);
                        Console.WriteLine($"{filename}");
                    }
                }
            }
            Console.WriteLine($"DONE");
            Console.ReadLine();
        }
    }
}
