using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TidsAnalysis
{
    public class SimpleRecord
    {
        public int Id;
        public string Date;
        public string DayOfWeek;
        public string Gdb;
        public string G1;
        public int[] Digits5; // [d5, d4, d3, d2, d1]
        public int De;
    }

    public class DayFeatures
    {
        public int ActualD3;
        public int[] Gans = new int[10];
        public int[] F30 = new int[10];
        public int[] F200Dow = new int[10];
        public int CDow;
        public int PrevDeDau;
        public int PrevDeDuoi;
        public int PrevD3;
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            var records = LoadData(@"data\lottery_master.csv");
            int n = records.Count;
            Console.WriteLine("Loaded {0} records for D3 Fast Analysis.", n);

            int evalPeriods = 500;
            int startK = n - evalPeriods;

            Console.WriteLine("Precomputing features for {0} evaluation periods...", evalPeriods);
            var features = new List<DayFeatures>();

            for (int k = startK; k < n; k++)
            {
                var df = new DayFeatures();
                df.ActualD3 = records[k].Digits5[2];
                string dow = records[k].DayOfWeek;
                var prevRec = records[k - 1];
                df.PrevDeDau = prevRec.Digits5[3];
                df.PrevDeDuoi = prevRec.Digits5[4];
                df.PrevD3 = prevRec.Digits5[2];

                for (int d = 0; d < 10; d++)
                {
                    for (int j = k - 1; j >= 0; j--)
                    {
                        if (records[j].Digits5[2] == d) break;
                        df.Gans[d]++;
                    }
                }

                for (int j = Math.Max(0, k - 30); j < k; j++)
                    df.F30[records[j].Digits5[2]]++;

                for (int j = Math.Max(0, k - 200); j < k; j++)
                {
                    if (records[j].DayOfWeek == dow)
                    {
                        df.F200Dow[records[j].Digits5[2]]++;
                        df.CDow++;
                    }
                }
                features.Add(df);
            }
            Console.WriteLine("Precomputing completed! Running fast grid search...");

            int[] goldMins = { 3, 5, 7 };
            int[] goldMaxs = { 12, 16, 20 };
            double[] wGans = { 1.0, 1.5, 2.0 };
            double[] wF30s = { 0.2, 0.4, 0.6 };
            double[] wDows = { 0.0, 0.3, 0.5 };
            double[] wDrops = { 0.0, 0.5, 0.8 };

            double bestRate = 0.0;
            double bestW_Gan = 0, bestW_F30 = 0, bestW_Dow = 0, bestW_Drop = 0;
            int bestOptimalGoldMin = 0, bestOptimalGoldMax = 0;

            var dScore = new double[10];
            var dIdx = new int[10] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 };

            foreach (var gMin in goldMins)
            {
                foreach (var gMax in goldMaxs)
                {
                    foreach (var wG in wGans)
                    {
                        foreach (var wF in wF30s)
                        {
                            foreach (var wDow in wDows)
                            {
                                foreach (var wDrop in wDrops)
                                {
                                    int hits = 0;
                                    for (int i = 0; i < evalPeriods; i++)
                                    {
                                        var df = features[i];
                                        double expDow = (df.CDow > 0) ? ((double)df.CDow / 10.0) : 0.0;

                                        for (int d = 0; d < 10; d++)
                                        {
                                            double r = 0.0;
                                            int g = df.Gans[d];
                                            if (g == 0) r = 1.0;
                                            else if (g >= gMin && g <= gMax) r = -wG;
                                            else if (g > 25) r = 0.5;

                                            double kF = -wF * (df.F30[d] - 3.0);
                                            double dowTerm = (df.CDow > 0 && wDow > 0) ? (-wDow * (df.F200Dow[d] - expDow)) : 0.0;
                                            double dropTerm = (wDrop > 0 && (d == df.PrevDeDau || d == df.PrevDeDuoi)) ? -wDrop : 0.0;

                                            dScore[d] = r + kF + dowTerm + dropTerm;
                                            dIdx[d] = d;
                                        }

                                        Array.Sort(dIdx, (a, b) => dScore[a].CompareTo(dScore[b]));
                                        int act = df.ActualD3;
                                        if (dIdx[0] == act || dIdx[1] == act || dIdx[2] == act) hits++;
                                    }

                                    double rate = (double)hits / evalPeriods * 100.0;
                                    if (rate > bestRate)
                                    {
                                        bestRate = rate;
                                        bestOptimalGoldMin = gMin;
                                        bestOptimalGoldMax = gMax;
                                        bestW_Gan = wG;
                                        bestW_F30 = wF;
                                        bestW_Dow = wDow;
                                        bestW_Drop = wDrop;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Console.WriteLine("=========================================================================");
            Console.WriteLine("            KẾT QUẢ TỐI ƯU HOÁ CÀNG 3D TRÊN 500 KỲ GẦN NHẤT             ");
            Console.WriteLine("=========================================================================");
            Console.WriteLine(" - Tỷ lệ trúng Top 3 Càng tối đa:   {0:F2}% (Kỳ vọng ngẫu nhiên: 30.0%)", bestRate);
            Console.WriteLine(" - Dải Gan Vàng tối ưu:             Gan {0} -> {1} kỳ", bestOptimalGoldMin, bestOptimalGoldMax);
            Console.WriteLine(" - Trọng số Gan:                    w_Gan = {0}", bestW_Gan);
            Console.WriteLine(" - Trọng số F30:                    w_F30 = {0}", bestW_F30);
            Console.WriteLine(" - Trọng số Chu kỳ Thứ:             w_Dow = {0}", bestW_Dow);
            Console.WriteLine(" - Trọng số Càng rơi từ Đề:         w_Drop = {0}", bestW_Drop);

            // Kiểm tra Top 4 Càng và Top 5 Càng
            int hitsTop4 = 0, hitsTop5 = 0;
            for (int i = 0; i < evalPeriods; i++)
            {
                var df = features[i];
                double expDow = (df.CDow > 0) ? ((double)df.CDow / 10.0) : 0.0;
                for (int d = 0; d < 10; d++)
                {
                    double r = 0.0;
                    int g = df.Gans[d];
                    if (g == 0) r = 1.0;
                    else if (g >= bestOptimalGoldMin && g <= bestOptimalGoldMax) r = -bestW_Gan;
                    else if (g > 25) r = 0.5;

                    double kF = -bestW_F30 * (df.F30[d] - 3.0);
                    double dowTerm = (df.CDow > 0 && bestW_Dow > 0) ? (-bestW_Dow * (df.F200Dow[d] - expDow)) : 0.0;
                    double dropTerm = (bestW_Drop > 0 && (d == df.PrevDeDau || d == df.PrevDeDuoi)) ? -bestW_Drop : 0.0;

                    dScore[d] = r + kF + dowTerm + dropTerm;
                    dIdx[d] = d;
                }
                Array.Sort(dIdx, (a, b) => dScore[a].CompareTo(dScore[b]));
                int act = df.ActualD3;
                if (dIdx[0] == act || dIdx[1] == act || dIdx[2] == act || dIdx[3] == act) hitsTop4++;
                if (dIdx[0] == act || dIdx[1] == act || dIdx[2] == act || dIdx[3] == act || dIdx[4] == act) hitsTop5++;
            }
            Console.WriteLine(" - Tỷ lệ trúng nếu lấy Top 4 Càng:  {0:F2}% (Kỳ vọng: 40.0%)", (double)hitsTop4 / evalPeriods * 100);
            Console.WriteLine(" - Tỷ lệ trúng nếu lấy Top 5 Càng:  {0:F2}% (Kỳ vọng: 50.0%)", (double)hitsTop5 / evalPeriods * 100);
            Console.WriteLine("=========================================================================");
        }

        private static List<SimpleRecord> LoadData(string path)
        {
            var list = new List<SimpleRecord>();
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = ParseCsvLine(line);
                if (parts.Count < 18) continue;
                try
                {
                    var rec = new SimpleRecord();
                    rec.Id = int.Parse(parts[0]);
                    rec.Date = parts[1];
                    rec.DayOfWeek = parts[2].Trim('\"');
                    rec.Gdb = parts[3].Trim('\"');
                    rec.G1 = parts[4].Trim('\"');

                    string gdbClean = rec.Gdb.PadLeft(5, '0');
                    if (gdbClean.Length > 5) gdbClean = gdbClean.Substring(gdbClean.Length - 5);
                    rec.Digits5 = new int[5];
                    for (int d = 0; d < 5; d++) rec.Digits5[d] = gdbClean[d] - '0';

                    rec.De = int.Parse(parts[11]);
                    list.Add(rec);
                }
                catch { }
            }
            return list;
        }

        private static List<SimpleRecord> LoadDataOld(string path) { return LoadData(path); }

        private static List<string> ParseCsvLine(string line)
        {
            var res = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    inQuotes = !inQuotes;
                    sb.Append(c);
                }
                else if (c == ',' && !inQuotes)
                {
                    res.Add(sb.ToString());
                    sb.Clear();
                }
                else sb.Append(c);
            }
            res.Add(sb.ToString());
            return res;
        }
    }
}
