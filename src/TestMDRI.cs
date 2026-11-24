using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TidsMDRITest
{
    public class Record
    {
        public int Id;
        public string Date;
        public string DayOfWeek;
        public int[] Digits5;
        public int State3D;
        public int State4D;
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            string csvPath = @"data\lottery_master.csv";
            var records = LoadData(csvPath);
            int n = records.Count;
            int testPeriods = 500;
            int startK = n - testPeriods;

            Console.WriteLine("=========================================================================================");
            Console.WriteLine("   KIỂM THỬ THỰC NGHIỆM CHỈ SỐ CỘNG HƯỞNG KÉP ĐA CHIỀU (MDRI) CHO 3D & 4D (500 KỲ OOS)");
            Console.WriteLine("=========================================================================================\n");

            int hit3D_Top1 = 0;  // Bạch thủ 3D (1/1.000 số = 0.1%)
            int hit3D_Top4 = 0;  // Tứ thủ 3D (4/1.000 số = 0.4%)
            int hit3D_Top10 = 0; // Thập thủ 3D (10/1.000 số = 1.0%)
            int hit3D_MDRI27 = 0; // Dàn 27 số MDRI (2.7%)

            int hit4D_Top1 = 0;  // Bạch thủ 4D (1/10.000 số = 0.01%)
            int hit4D_Top4 = 0;  // Tứ thủ 4D (4/10.000 số = 0.04%)
            int hit4D_Top10 = 0; // Thập thủ 4D (10/10.000 số = 0.1%)
            int hit4D_MDRI36 = 0; // Dàn 36 số MDRI (0.36%)
            int hit4D_MDRI81 = 0; // Dàn 81 số MDRI (0.81%)

            long reward3D = 960000;
            long reward4D = 8880000;

            for (int k = startK; k < n; k++)
            {
                var past = records.Take(k).ToList();
                var actual = records[k];
                string dow = actual.DayOfWeek;
                int act3D = actual.State3D;
                int act4D = actual.State4D;

                // 1. Phân phối biên 1D từng trục
                double[] vD4 = ComputeDigitPotential(past, 1, dow);
                double[] vD3 = ComputeDigitPotential(past, 2, dow);
                double[] vD2 = ComputeDigitPotential(past, 3, dow);
                double[] vD1 = ComputeDigitPotential(past, 4, dow);

                // 2. Tương tác cặp
                double[,] w43 = ComputeCoupling2D(past, 1, 2);
                double[,] w32 = ComputeCoupling2D(past, 2, 3);
                double[,] w21 = ComputeCoupling2D(past, 3, 4);

                // -------------------------------------------------------------
                // TÍNH CHỈ SỐ MDRI CHO 1.000 SỐ 3D
                // -------------------------------------------------------------
                var list3D = new List<Tuple<int, double, double>>(1000);
                for (int s = 0; s < 1000; s++)
                {
                    int d3 = s / 100;
                    int d2 = (s / 10) % 10;
                    int d1 = s % 10;

                    // Luồng 1: Thế năng Tensor 1D
                    double vTensor = vD3[d3] + vD2[d2] + vD1[d1];

                    // Luồng 2: Thế năng Liên kết Manifold 3D
                    double vManifold = vTensor + 0.5 * w32[d3, d2] + 0.5 * w21[d2, d1];
                    list3D.Add(new Tuple<int, double, double>(s, vTensor, vManifold));
                }

                // Xếp hạng Luồng 1 và Luồng 2
                var rankTensor3D = list3D.OrderBy(x => x.Item2).Select((x, idx) => new { State = x.Item1, Rank = idx }).ToDictionary(x => x.State, x => x.Rank);
                var rankManifold3D = list3D.OrderBy(x => x.Item3).Select((x, idx) => new { State = x.Item1, Rank = idx }).ToDictionary(x => x.State, x => x.Rank);

                // MDRI = RankTensor + RankManifold
                var mdri3D = list3D.Select(x => new Tuple<int, int>(x.Item1, rankTensor3D[x.Item1] + rankManifold3D[x.Item1]))
                                   .OrderBy(x => x.Item2)
                                   .Select(x => x.Item1)
                                   .ToArray();

                if (mdri3D[0] == act3D) hit3D_Top1++;
                if (mdri3D.Take(4).Contains(act3D)) hit3D_Top4++;
                if (mdri3D.Take(10).Contains(act3D)) hit3D_Top10++;
                if (mdri3D.Take(27).Contains(act3D)) hit3D_MDRI27++;

                // -------------------------------------------------------------
                // TÍNH CHỈ SỐ MDRI CHO 10.000 SỐ 4D
                // -------------------------------------------------------------
                var list4D = new List<Tuple<int, double, double>>(10000);
                for (int s = 0; s < 10000; s++)
                {
                    int d4 = s / 1000;
                    int d3 = (s / 100) % 10;
                    int d2 = (s / 10) % 10;
                    int d1 = s % 10;

                    double vTensor = vD4[d4] + vD3[d3] + vD2[d2] + vD1[d1];
                    double vManifold = vTensor + 0.5 * w43[d4, d3] + 0.5 * w32[d3, d2] + 0.5 * w21[d2, d1];
                    list4D.Add(new Tuple<int, double, double>(s, vTensor, vManifold));
                }

                var rankTensor4D = list4D.OrderBy(x => x.Item2).Select((x, idx) => new { State = x.Item1, Rank = idx }).ToDictionary(x => x.State, x => x.Rank);
                var rankManifold4D = list4D.OrderBy(x => x.Item3).Select((x, idx) => new { State = x.Item1, Rank = idx }).ToDictionary(x => x.State, x => x.Rank);

                var mdri4D = list4D.Select(x => new Tuple<int, int>(x.Item1, rankTensor4D[x.Item1] + rankManifold4D[x.Item1]))
                                   .OrderBy(x => x.Item2)
                                   .Select(x => x.Item1)
                                   .ToArray();

                if (mdri4D[0] == act4D) hit4D_Top1++;
                if (mdri4D.Take(4).Contains(act4D)) hit4D_Top4++;
                if (mdri4D.Take(10).Contains(act4D)) hit4D_Top10++;
                if (mdri4D.Take(36).Contains(act4D)) hit4D_MDRI36++;
                if (mdri4D.Take(81).Contains(act4D)) hit4D_MDRI81++;
            }

            Console.WriteLine("=========================================================================================");
            Console.WriteLine("   KẾT QUẢ ĐỐI SOÁT MDRI TRÊN 1.000 SỐ 3D (500 KỲ)");
            Console.WriteLine("=========================================================================================");
            PrintMetricRow("Bạch Thủ 3D (Top 1 số)", 1, hit3D_Top1, testPeriods, 0.001, reward3D);
            PrintMetricRow("Tứ Thủ 3D (Top 4 số)  ", 4, hit3D_Top4, testPeriods, 0.004, reward3D);
            PrintMetricRow("Thập Thủ 3D (Top 10 số)", 10, hit3D_Top10, testPeriods, 0.010, reward3D);
            PrintMetricRow("Dàn MDRI 3D (Top 27 số)", 27, hit3D_MDRI27, testPeriods, 0.027, reward3D);

            Console.WriteLine("\n=========================================================================================");
            Console.WriteLine("   KẾT QUẢ ĐỐI SOÁT MDRI TRÊN 10.000 SỐ 4D (500 KỲ)");
            Console.WriteLine("=========================================================================================");
            PrintMetricRow("Bạch Thủ 4D (Top 1 số)", 1, hit4D_Top1, testPeriods, 0.0001, reward4D);
            PrintMetricRow("Tứ Thủ 4D (Top 4 số)  ", 4, hit4D_Top4, testPeriods, 0.0004, reward4D);
            PrintMetricRow("Thập Thủ 4D (Top 10 số)", 10, hit4D_Top10, testPeriods, 0.0010, reward4D);
            PrintMetricRow("Dàn MDRI 4D (Top 36 số)", 36, hit4D_MDRI36, testPeriods, 0.0036, reward4D);
            PrintMetricRow("Dàn MDRI 4D (Top 81 số)", 81, hit4D_MDRI81, testPeriods, 0.0081, reward4D);
            Console.WriteLine("=========================================================================================");
        }

        private static void PrintMetricRow(string name, int size, int hits, int total, double randProb, long reward)
        {
            double actRate = (double)hits / total * 100.0;
            double randRate = randProb * 100.0;
            long cost = (long)size * 1000 * total;
            long win = (long)hits * reward;
            long pnl = win - cost;
            double roi = total > 0 && cost > 0 ? ((double)pnl / cost * 100.0) : 0.0;
            string roiStr = pnl >= 0 ? string.Format("+{0:F1}% (LÃI +{1:N0}k)", roi, pnl / 1000) : string.Format("{0:F1}% ({1:N0}k)", roi, pnl / 1000);

            Console.WriteLine("{0} | {1,2} kỳ / {2,-3} | Tỷ lệ: {3,5:F2}% (Ngẫu nhiên {4,5:F2}%) | ROI: {5}",
                name, hits, total, actRate, randRate, roiStr);
        }

        private static double[] ComputeDigitPotential(List<Record> past, int digitIdx, string currentDow)
        {
            int n = past.Count;
            int[] gan = new int[10];
            for (int d = 0; d < 10; d++)
            {
                for (int i = n - 1; i >= 0; i--)
                {
                    if (past[i].Digits5[digitIdx] == d) break;
                    gan[d]++;
                }
            }

            int[] f30 = new int[10];
            for (int i = Math.Max(0, n - 30); i < n; i++) f30[past[i].Digits5[digitIdx]]++;

            int[] dowCount = new int[10];
            int dowTotal = 0;
            for (int i = Math.Max(0, n - 150); i < n; i++)
            {
                if (past[i].DayOfWeek == currentDow)
                {
                    dowCount[past[i].Digits5[digitIdx]]++;
                    dowTotal++;
                }
            }

            int lastVal = past[n - 1].Digits5[digitIdx];

            double[] v = new double[10];
            for (int d = 0; d < 10; d++)
            {
                double r = 0.0;
                if (gan[d] == 0) r = 3.5;
                else if (gan[d] >= 5 && gan[d] <= 14) r = -2.2;
                else if (gan[d] > 25) r = 0.8;

                double k = -0.3 * (f30[d] - 3.0);
                double dowExp = dowTotal > 0 ? (double)dowTotal / 10.0 : 1.0;
                double dTerm = -0.4 * (dowCount[d] - dowExp);
                double repeatPenalty = (d == lastVal) ? 2.5 : 0.0;

                v[d] = r + k + dTerm + repeatPenalty;
            }
            return v;
        }

        private static double[,] ComputeCoupling2D(List<Record> past, int idx1, int idx2)
        {
            int n = past.Count;
            int[,] f100 = new int[10, 10];
            int start = Math.Max(0, n - 100);
            for (int i = start; i < n; i++)
            {
                int d1 = past[i].Digits5[idx1];
                int d2 = past[i].Digits5[idx2];
                f100[d1, d2]++;
            }

            double[,] w = new double[10, 10];
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    w[i, j] = -0.4 * (f100[i, j] - 1.0);
                }
            }
            return w;
        }

        private static List<Record> LoadData(string path)
        {
            var list = new List<Record>();
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = ParseCsvLine(line);
                if (parts.Count < 18) continue;
                try
                {
                    var rec = new Record();
                    rec.Id = int.Parse(parts[0]);
                    rec.Date = parts[1];
                    rec.DayOfWeek = parts[2].Trim('\"');
                    string gdb = parts[3].Trim('\"').PadLeft(5, '0');
                    if (gdb.Length > 5) gdb = gdb.Substring(gdb.Length - 5);
                    rec.Digits5 = new int[5];
                    for (int d = 0; d < 5; d++) rec.Digits5[d] = gdb[d] - '0';
                    rec.State3D = rec.Digits5[2] * 100 + rec.Digits5[3] * 10 + rec.Digits5[4];
                    rec.State4D = rec.Digits5[1] * 1000 + rec.Digits5[2] * 100 + rec.Digits5[3] * 10 + rec.Digits5[4];
                    list.Add(rec);
                }
                catch { }
            }
            return list;
        }

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
