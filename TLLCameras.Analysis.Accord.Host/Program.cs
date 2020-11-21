using System;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Accord.Imaging;
using Accord.MachineLearning;
using Accord.MachineLearning.VectorMachines;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Math;
using Accord.Math.Optimization.Losses;
using Accord.Statistics.Kernels;
using Bitmap = System.Drawing.Bitmap;
using ITransform = Accord.MachineLearning.ITransform;

namespace TLLCameras.Analysis.Accord.Host
{
    class Program
    {
        private static Dictionary<string, Bitmap> trainingImages = new Dictionary<string, Bitmap>();
        private static Dictionary<string, Bitmap> testingImages = new Dictionary<string, Bitmap>();

        private static Dictionary<string, double[]> trainingFeatures = new Dictionary<string, double[]>();
        private static Dictionary<string, double[]> testingFeatures = new Dictionary<string, double[]>();

        private static MulticlassSupportVectorMachine<IKernel> ksvm;

        static void Main(string[] args)
        {
            Console.WriteLine("Creating bag of words...");
            CreateBoW();
            Console.WriteLine("Bag of words created!");

            Console.WriteLine("Creating vector machines...");
            CreateVectorMachines();
            Console.WriteLine("Vector machines created!");

            Console.WriteLine("Classifying test images...");
            Classify();
            Console.WriteLine("Classification done!");

            Console.ReadKey();
        }

        private static void CreateBoW() { 
            var numberOfWords = 36;

            foreach (var file in Directory.EnumerateFiles(@"C:\Temp\TLLCamerasTestData\37_Training", "*.jpg"))
            {
                var trainingImage = (Bitmap)Bitmap.FromFile(file);

                trainingImages.Add(file, trainingImage);
            }

            foreach (var file in Directory.EnumerateFiles(@"C:\Temp\TLLCamerasTestData\37_Testing", "*.jpg"))
            {
                var testImage = (Bitmap) Bitmap.FromFile(file);

                testingImages.Add(file, testImage);
            }



            // We will use SURF, so we can use a standard clustering
            // algorithm that is based on Euclidean distances. A good
            // algorithm for clustering codewords is the Binary Split
            // variant of the K-Means algorithm.

            // Create a Binary-Split clustering algorithm
            BinarySplit binarySplit = new BinarySplit(numberOfWords);

            // Create bag-of-words (BoW) with the given algorithm
            BagOfVisualWords surfBow = new BagOfVisualWords(binarySplit);

            // Compute the BoW codebook using training images only
            IBagOfWords<Bitmap> bow = surfBow.Learn(trainingImages.Values.ToArray());

            // now that we've created the bow we need to use it to create a representation of each training and test image

            foreach (var trainingImage in trainingImages.Keys)
            {
                var asBitmap = trainingImages[trainingImage] as Bitmap;

                var featureVector = (bow as ITransform<Bitmap, double[]>).Transform(asBitmap);

                var featureString = featureVector.ToString(DefaultArrayFormatProvider.InvariantCulture);

                trainingFeatures.Add(trainingImage, featureVector);
            }

            foreach (var testingImage in testingImages.Keys)
            {
                var asBitmap = testingImages[testingImage] as Bitmap;

                var featureVector = (bow as ITransform<Bitmap, double[]>).Transform(asBitmap);

                var featureString = featureVector.ToString(DefaultArrayFormatProvider.InvariantCulture);

                testingFeatures.Add(testingImage, featureVector);
            }

        }

        private static void CreateVectorMachines()
        {
            var kernel = new ChiSquare();

            // Extract training parameters from the interface
            double complexity = (double) 1;
            double tolerance = (double) 0.01;
            int cacheSize = (int) 500;
            SelectionStrategy strategy = SelectionStrategy.Sequential;

            // Create the support vector machine learning algorithm
            var teacher = new MulticlassSupportVectorLearning<IKernel>()
            {
                Kernel = kernel,
                Learner = (param) =>
                {
                    return new SequentialMinimalOptimization<IKernel>()
                    {
                        Kernel = kernel,
                        Complexity = complexity,
                        Tolerance = tolerance,
                        CacheSize = cacheSize,
                        Strategy = strategy,
                    };
                }
            };

            // Get the input and output data
            double[][] inputs;
            int[] outputs;

            var inputsList = new List<double[]>();
            var outputsList = new List<int>();
            var i = 0;
            foreach (var trainingImage in trainingImages.Keys)
            {
                var trainingFeature = trainingFeatures[trainingImage];

                inputsList.Add(trainingFeature);
                outputsList.Add(i);

                i++;
            }

            inputs = inputsList.ToArray();
            outputs = outputsList.ToArray();

            ksvm = teacher.Learn(inputs, outputs);

            double error = new ZeroOneLoss(outputs).Loss(ksvm.Decide(inputs));

            Console.WriteLine("Error was {0}", error);
        }

        private static void Classify()
        {
            var hits = 0;
            var misses = 0;

            var i = 0;
            foreach (var testImage in testingImages.Keys)
            {
                var input = testingFeatures[testImage];
                var expected = i;

                var actual = ksvm.Decide(input);

                if (expected == actual)
                {
                    Console.WriteLine("{0}\tMatched!", Path.GetFileName(testImage));
                    hits++;
                }
                else
                {
                    Console.WriteLine("{0}\tDid not match\t{1} != {2}", Path.GetFileName(testImage), actual, expected);
                    misses++;
                }

                i++;
            }

            Console.WriteLine("Hits: {0}, Misses: {1}", hits, misses);
        }
    }
}
