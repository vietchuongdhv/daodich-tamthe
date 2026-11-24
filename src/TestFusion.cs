using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

class Program
{
    class Rec
    {
        public int Id;
        public string Date;
        public int De;
        public int Dau;
        public int Duoi;
        public HashSet<int> Lo;
    }

    static void Main()
    {
        string path = Path.Combine("data", "lottery_master.csv");
        var lines = File.ReadAllLines(path, Encoding.UTF8);
        var recs = new List<Rec>();
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var parts = ParseCsvLine(lines[i]);
            if (parts.Count < 19) continue;
            var r = new Rec {
                Id = int.Parse(parts[0]),
                Date = parts[1],
                De = int.Parse(parts[11]),
                Dau = int.Parse(parts[12]),
                Duoi = int.Parse(parts[13]),
                Lo = new HashSet<int>()
            };
            if (!string.IsNullOrEmpty(parts[16]))
            {
                foreach (var s in parts[16].Split(','))
                {
                    int val;
                    if (int.TryParse(s.Trim(), out val)) r.Lo.Add(val);
                }
            }
            recs.Add(r);
        }

        Console.WriteLine("Loaded " + recs.Count + " records.");

        // Walk-forward backtest on last 60 days
        int testDays = 60;
        int startIdx = recs.Count - testDays;

        double[] weights = new double[] { 0.0, 0.3, 0.5, 0.7, 1.0 };

        foreach (var w in weights)
        {
            int hitTop1 = 0;
            int hitTop4 = 0;
            int hitTop9 = 0;
            int hitTop25 = 0;
            int hitTop50 = 0;

            for (int t = startIdx; t < recs.Count; t++)
            {
                var train = recs.Take(t).ToList();
                int actual = recs[t].De;

                var ranking = ComputeUnifiedRanking(train, w);

                if (ranking[0] == actual) hitTop1++;
                if (ranking.Take(4).Contains(actual)) hitTop4++;
                if (ranking.Take(9).Contains(actual)) hitTop9++;
                if (ranking.Take(25).Contains(actual)) hitTop25++;
                if (ranking.Take(50).Contains(actual)) hitTop50++;
            }

            Console.WriteLine(string.Format("Weight 1D = {0:F1}: Top1={1}/{2} ({3:P1}), Top4={4}/{2} ({5:P1}), Top9={6}/{2} ({7:P1}), Top25={8}/{2} ({9:P1}), Top50={10}/{2} ({11:P1})",
                w, hitTop1, testDays, (double)hitTop1/testDays,
                hitTop4, (double)hitTop4/testDays,
                hitTop9, (double)hitTop9/testDays,
                hitTop25, (double)hitTop25/testDays,
                hitTop50, (double)hitTop50/testDays));
        }
    }

    static List<int> ComputeUnifiedRanking(List<Rec> train, double w1D)
    {
        int n = train.Count;
        int lastDe = train[n - 1].De;
        var lastUniqueLo = train[n - 1].Lo;

        int[] gan = new int[100];
        for (int s = 0; s < 100; s++)
        {
            for (int i = n - 1; i >= 0; i--)
            {
                if (train[i].De == s) break;
                gan[s]++;
            }
        }

        int[] f30 = new int[100];
        for (int i = Math.Max(0, n - 30); i < n; i++) f30[train[i].De]++;

        int[] markovCount = new int[100];
        int totalFromLast = 0;
        for (int i = 0; i < n - 1; i++)
        {
            if (train[i].De == lastDe)
            {
                markovCount[train[i + 1].De]++;
                totalFromLast++;
            }
        }

        int[] ganHeads = new int[10];
        for (int d = 0; d < 10; d++)
        {
            for (int i = n - 1; i >= 0; i--)
            {
                if (train[i].Dau == d) break;
                ganHeads[d]++;
            }
        }
        int[] f30Heads = new int[10];
        for (int i = Math.Max(0, n - 30); i < n; i++) f30Heads[train[i].Dau]++;

        int[] ganUnits = new int[10];
        for (int d = 0; d < 10; d++)
        {
            for (int i = n - 1; i >= 0; i--)
            {
                if (train[i].Duoi == d) break;
                ganUnits[d]++;
            }
        }
        int[] f30Units = new int[10];
        for (int i = Math.Max(0, n - 30); i < n; i++) f30Units[train[i].Duoi]++;

        // 1D Potentials
        double[] vTens = new double[10];
        for (int d = 0; d < 10; d++)
        {
            double rTerm = (ganHeads[d] >= 8 && ganHeads[d] <= 18) ? -1.6 + (Math.Abs(ganHeads[d] - 12) / 10.0) : (ganHeads[d] == 0 ? 1.5 : (ganHeads[d] > 25 ? 1.0 : -0.3));
            double kTerm = -0.3 * (f30Heads[d] - 3.0);
            double antiMomentum = 0.0;
            if (ganHeads[d] >= 5) antiMomentum = 0.35 * (ganHeads[d] - 4);
            else if (ganHeads[d] >= 1 && ganHeads[d] <= 4) antiMomentum = -0.45;
            vTens[d] = rTerm + kTerm + antiMomentum;
        }

        double[] vUnits = new double[10];
        for (int d = 0; d < 10; d++)
        {
            double rTerm = (ganUnits[d] >= 8 && ganUnits[d] <= 18) ? -1.6 + (Math.Abs(ganUnits[d] - 12) / 10.0) : (ganUnits[d] == 0 ? 1.5 : (ganUnits[d] > 25 ? 1.0 : -0.3));
            double kTerm = -0.3 * (f30Units[d] - 3.0);
            vUnits[d] = rTerm + kTerm;
        }

        var list = new List<Tuple<int, double>>();
        for (int s = 0; s < 100; s++)
        {
            double renewalTerm = 0.0;
            if (gan[s] == 0) renewalTerm = 2.5;
            else if (gan[s] >= 8 && gan[s] <= 22) renewalTerm = -1.8 + (Math.Abs(gan[s] - 13) / 10.0);
            else if (gan[s] >= 3 && gan[s] <= 7) renewalTerm = -0.5;
            else if (gan[s] > 30) renewalTerm = 1.2 + 0.05 * (gan[s] - 30);
            else renewalTerm = 0.2;

            double kineticTerm = -0.6 * (f30[s] - 0.30);
            double pMarkov = totalFromLast > 0 ? ((double)markovCount[s] + 0.05) / (totalFromLast + 5.0) : 0.01;
            double markovTerm = -0.3 * Math.Log(pMarkov);

            double cascadeTerm = 0.0;
            if (lastUniqueLo.Contains(s))
            {
                cascadeTerm = -0.35;
                if (gan[s] >= 1 && gan[s] <= 6) cascadeTerm -= 0.30;
            }

            int head = s / 10;
            int unit = s % 10;
            double antiMomentumTerm = 0.0;
            if (ganHeads[head] >= 5) antiMomentumTerm = 0.25 * (ganHeads[head] - 4);
            else if (ganHeads[head] >= 1 && ganHeads[head] <= 4) antiMomentumTerm = -0.35;

            double v2D = renewalTerm + kineticTerm + markovTerm + cascadeTerm + antiMomentumTerm;
            double v1D = (vTens[head] + vUnits[unit]) * 0.5;

            double vTotal = v2D + w1D * v1D;

            list.Add(new Tuple<int, double>(s, vTotal));
        }

        return list.OrderBy(x => x.Item2).Select(x => x.Item1).ToList();
    }

    static List<string> ParseCsvLine(string line)
    {
        var result = new List<string>();
        bool inQuotes = false;
        var current = new StringBuilder();
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\"') inQuotes = !inQuotes;
            else if (c == ',' && !inQuotes)
            {
                result.Add(current.ToString().Trim('\"'));
                current.Clear();
            }
            else current.Append(c);
        }
        result.Add(current.ToString().Trim('\"'));
        return result;
    }
}
