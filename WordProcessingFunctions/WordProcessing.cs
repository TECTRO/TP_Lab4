using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms.DataVisualization.Charting;
using PorterStemmer;
using PorterStemmer.Stemmers;
using Standard.Data.StringMetrics;

namespace WordProcessingFunctions
{
    public static class WordProcessing
    {
        public static bool IsEqual(this IEnumerable<string> val1, IEnumerable<string> val2)
        {
            var value1 = val1.ToList();
            var value2 = val2.ToList();
            if (value1.Count == value2.Count)
                if (!value1.Where((val, ind) => !val.Equals(value2.ElementAt(ind))).Any())
                    return true;
            return false;
        }
        public static string PreProcess(this string document, string wrongSymbols = "./,<>@`-=+\"\\()[]{}«»:;–_")
        {
            return string
                .Join(" ", document
                    .Split(wrongSymbols.ToCharArray())
                    .Select(word => word.Trim().ToLower())
                    .Where(word => word.Any()));
        }

        private static readonly EnglishStemmer Stemmer = new EnglishStemmer();
        public static string StemIt(this string document, string splitSymbols = " ./,<>@`-=+\"\\()[]{}«»:;–_")
        {
            return string
                .Join(" ", document
                    .Split(splitSymbols.ToCharArray())
                    .Where(word=>word.Trim().Any())
                    .Select(word =>
                    {
                        try
                        {
                            return word.GetStem();
                        }
                        catch (Exception)
                        {
                            try
                            {
                                return Stemmer.GetStem(word);
                            }
                            catch (Exception)
                            {
                                return word;
                            }
                        }
                    })
                );
        }

        public static IEnumerable<IEnumerable<string>> GetNGrams(this IEnumerable<string> sentence, int n)
        {
            List<List<string>> nGramCollection = new List<List<string>>();
            if (n > 0)
            {
                var sentenceAsList = sentence.ToList();
                for (int i = 0; i < sentenceAsList.Count - (n - 1); i++)
                {
                    List<string> nGram = new List<string>();
                    for (int j = 0; j < n; j++)
                        nGram.Add(sentenceAsList.ElementAt(i + j));
                    nGramCollection.Add(nGram);
                }
            }
            return nGramCollection;
        }

        public static IEnumerable<IEnumerable<string>> GetNGrams(this string sentence, int n, string splitSymbols = " ./,<>@`-=+\"\\()[]{}«»:;–_")
        {
            return GetNGrams(sentence.Split(splitSymbols.ToCharArray(),StringSplitOptions.RemoveEmptyEntries), n);
        }
        public static IEnumerable<(IEnumerable<string>, float)> GetAnalysisFrequency(this IEnumerable<IEnumerable<string>> nGrams)
        {
            return nGrams.GroupBy(nGram => string.Join(" ", nGram)).Select(nGram => (nGram.First(), (float)nGram.Count())).ToList();
        }

        public static IEnumerable<(IEnumerable<string>, float)> GetTopRelevant(this IEnumerable<(IEnumerable<string>, float)> nGrams, int count)
        {
            return nGrams.OrderByDescending(biGram => biGram.Item2).Take(count);
        }

        public static void BuildCloud(this IEnumerable<(IEnumerable<string>, float)> nGramsWithFrequency, string name = "cloud")
        {
            var gramsWithFrequency = nGramsWithFrequency.ToList();
            new WordCloud.WordCloud(800, 600).Draw(gramsWithFrequency.Select(t => string.Join(" ", t.Item1)).ToList(), gramsWithFrequency.Select(t => (int)t.Item2).ToList()).Save($"{name}.jpg");
        }

        public static void BuildCloud(this IEnumerable<(IEnumerable<string>, float)> nGramsWithFrequency, string name,int top )
        {
            BuildCloud(nGramsWithFrequency.GetTopRelevant(top), name);
        }

        public static IEnumerable<(IEnumerable<string>, float)> ConvertToTfIdf(this IEnumerable<(IEnumerable<string>, float)> nGramsWithFrequency, IEnumerable<string> corp)
        {
            bool IsNGramContains(IEnumerable<string> nGram, IEnumerable<string> doc)
            {
                var documentAsList = doc.ToList();
                var nGramAsList = nGram.ToList();

                if (documentAsList.Count >= nGramAsList.Count)
                {
                    int insets = 0;
                    for (int i = 0; i < documentAsList.Count - (nGramAsList.Count - 1); i++)
                    {
                        bool inset = true;
                        for (int j = 0; j < nGramAsList.Count; j++)
                        {
                            if (documentAsList.ElementAt(i + j) != nGramAsList.ElementAt(j))
                                inset = false;
                        }
                        if (inset) insets++;
                    }
                    return insets > 0;
                }
                return false;
            }

            var corpus = corp.ToList();
            var splitterCorp = corpus.Select(doc => doc.Split(' ').Select(word => word.Trim()));

            var gramsWithFrequency = nGramsWithFrequency.ToList();
            return gramsWithFrequency
                .Select(nGram =>
                {
                    var tf = nGram.Item2 / gramsWithFrequency.Count;

                    var insets = splitterCorp.Count(doc => IsNGramContains(nGram.Item1, doc));
                    var idf = Math.Log(corpus.Count / (float)insets);

                    return (
                                nGram.Item1,
                                (float)(tf * idf)
                            );
                })
                .ToList();
        }

        public static void BuildGraphic(this IEnumerable<(IEnumerable<string>, float)> nGramsWithIfIdf,string name = "chart")
        {
            Chart chart = new Chart {Width = 2400, Height = 1200};
            chart.ChartAreas.Add(new ChartArea("Math functions"));
            chart.ChartAreas["Math functions"].AxisX.Interval = 1;

            Series mySeriesOfPoint = new Series("TF-IDF") {ChartType = SeriesChartType.Column};
            var index = 0;
            foreach (var nGram in nGramsWithIfIdf)
            {
                mySeriesOfPoint.Points.AddXY(string.Join(" ", nGram.Item1), nGram.Item2);
                mySeriesOfPoint.Points[index].Label = $"{nGram.Item2,0:f3}";
                index++;
            }

            chart.Series.Add(mySeriesOfPoint);
            chart.SaveImage($"{name}.jpg", ImageFormat.Jpeg);
        }

        public static void BuildGraphic(this IEnumerable<(IEnumerable<string>, float)> nGramsWithIfIdf, string name, int top)
        {
            BuildGraphic(nGramsWithIfIdf.GetTopRelevant(top),name);
        }

        private const int ColumnPadding = 12;
        public static IEnumerable<IEnumerable<double>> GetSimilarityCrossMatrix(this IEnumerable<string> corpus)
        {
            var similarity = new CosineSimilarity();
            var corpusAsList = corpus.ToList();

            return corpusAsList
                .Select(t => corpusAsList
                    .Select(tt => similarity
                        .GetSimilarity(tt, t))
                    );
        }

        public static string CrossMatrixAsString(this IEnumerable<IEnumerable<double>> matrix, string label = "Документ")
        {
            var matrixAsList = matrix.ToList();
            return string.Join("\n", matrixAsList
                .Select((row, ind) =>
                    $"{$"{label} {ind + 1}",ColumnPadding}" + string.Join("", row
                        .Select(item =>
                            $"{item,ColumnPadding:f3}")))

                .Prepend(string.Join("", matrixAsList
                    .Select((val, ind) =>
                        $"{$"{label} {ind + 1}",ColumnPadding}")
                    .Prepend($"{"",ColumnPadding}"))));
        }

        public static void RunTwice(Action runFirstFunc, Action runSecondFunc, Action<int> runAfterFunc)
        {
            for (int i = 0; i < 2; i++)
            {
                if (Convert.ToBoolean(i))
                    runSecondFunc();
                else
                    runFirstFunc();

                runAfterFunc(i);
            }
        }

        //AsString()/////////////////////////////////

        public static string AsString(this IEnumerable<IEnumerable<string>> nGrams)
        {
            return string.Join("\n",nGrams.Select(nGram =>string.Join(" ", nGram)));
        }

        public static string AsString(this IEnumerable<(IEnumerable<string>, float)> nGramsWith)
        {
            return string.Join("\n", nGramsWith.Select(nGram=>$"{string.Join(" ", nGram.Item1)}: {nGram.Item2}"));
        }
    }
}
