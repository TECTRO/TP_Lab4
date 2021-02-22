using System;
using System.Linq;
using WordProcessingFunctions;

// ReSharper disable StringLiteralTypo

namespace Lab4
{
    class Program
    {
        static void Main()
        {
            var corpus = new[] {
                "мама мыла раму",
                "мама мыла раму",
                "мама мыла раму мылом",
                "мамы мыли рамы",
                "папы мыли рамы",
                "мыла мама раму",
                "мыла мама раму мылом, мелом и порошком"
            };
            WordProcessing.RunTwice(
                () => corpus = corpus.Select(doc => doc.PreProcess()).ToArray(),
                () => corpus = corpus.Select(doc => doc.StemIt()).ToArray(),
                index => 
                {
                    Console.WriteLine($"\n Проход {index + 1}\n");

                    var biGrams = corpus.SelectMany(doc => doc.Split(' ').GetNGrams(2)).ToList();
                    Console.WriteLine($"\nПолученные биграммы:\n{string.Join("\n", biGrams.Select(biGram => string.Join(" ", biGram)))}");

                    var biGramsWithFrequency = biGrams.GetAnalysisFrequency().ToList();
                    Console.WriteLine($"\nЧастотный анализ:\n{string.Join("\n", biGramsWithFrequency.Select(biGram => $"{string.Join(" ", biGram.Item1)}: {biGram.Item2}"))}");

                    biGramsWithFrequency.BuildCloud($"cloud{index}",100);
                    Console.WriteLine("\nОблако слов построено");

                    var biGramsWithTFxIdf = biGramsWithFrequency.ConvertToTfIdf(corpus).ToList();
                    Console.WriteLine($"\nTF-IDF:\n{string.Join("\n", biGramsWithTFxIdf.Select(biGram => $"{string.Join(" ", biGram.Item1)}: {biGram.Item2,0:f3}"))}");

                    biGramsWithTFxIdf.BuildGraphic($"TF_IDF({index})");
                    Console.WriteLine("\nГрафик построен");

                    Console.WriteLine($"\n10 наиболее часто встречающихся биграмм:\n{string.Join("\n", biGramsWithTFxIdf.GetTopRelevant(10).Select(words => string.Join(" ", words.Item1)))}");

                    Console.WriteLine(corpus.GetSimilarityCrossMatrix().CrossMatrixAsString());
                });

            Console.ReadKey();
        }
    }
}
