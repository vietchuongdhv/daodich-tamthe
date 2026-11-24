using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TidsAsymmetricSearch
{
    public class Record
    {
        public int Id;
        public string Date;
        public string DayOfWeek;
        public int[] Digits5; // d5, d4, d3, d2, d1
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
            // Kiểm thử Walk-Forward OOS trên 1000 kỳ gần nhất
            int testPeriods = 1000;
            int startK = n - testPeriods;

            Console.WriteLine("=========================================================================================");
            Console.WriteLine("   KHẢO SÁT CẤU TRÚC TENSOR BẤT ĐỐI XỨNG TỐI ƯU CHO 3D & 4D (500 KỲ OOS)");
            Console.WriteLine("=========================================================================================\n");

            // 3D configs: (k3, k2, k1) -> size
            var configs3D = new List<Tuple<int, int, int, string>>()
            {
                new Tuple<int, int, int, string>(3, 3, 3, "Dàn 3D 27 số (3x3x3)"),
                new Tuple<int, int, int, string>(2, 4, 4, "Dàn 3D 32 số (2x4x4)"),
                new Tuple<int, int, int, string>(2, 5, 5, "Dàn 3D 50 số (2x5x5)"),
                new Tuple<int, int, int, string>(3, 4, 4, "Dàn 3D 48 số (3x4x4)"),
                new Tuple<int, int, int, string>(3, 4, 5, "Dàn 3D 60 số (3x4x5)"),
                new Tuple<int, int, int, string>(4, 4, 4, "Dàn 3D 64 số (4x4x4)"),
                new Tuple<int, int, int, string>(3, 5, 5, "Dàn 3D 75 số (3x5x5)"),
                new Tuple<int, int, int, string>(4, 5, 5, "Dàn 3D 100 số (4x5x5)"),
                new Tuple<int, int, int, string>(5, 5, 5, "Dàn 3D 125 số (5x5x5)")
            };

            // 4D configs: (k4, k3, k2, k1) -> size
            var configs4D = new List<Tuple<int, int, int, int, string>>()
            {
                new Tuple<int, int, int, int, string>(2, 2, 2, 2, "Dàn 4D 16 số (2x2x2x2)"),
                new Tuple<int, int, int, int, string>(2, 2, 3, 3, "Dàn 4D 36 số (2x2x3x3)"),
                new Tuple<int, int, int, int, string>(2, 2, 4, 4, "Dàn 4D 64 số (2x2x4x4)"),
                new Tuple<int, int, int, int, string>(3, 3, 3, 3, "Dàn 4D 81 số (3x3x3x3)"),
                new Tuple<int, int, int, int, string>(2, 2, 5, 5, "Dàn 4D 100 số (2x2x5x5)"),
                new Tuple<int, int, int, int, string>(3, 3, 4, 4, "Dàn 4D 144 số (3x3x4x4)"),
                new Tuple<int, int, int, int, string>(3, 3, 5, 5, "Dàn 4D 225 số (3x3x5x5)"),
                new Tuple<int, int, int, int, string>(4, 4, 4, 4, "Dàn 4D 256 số (4x4x4x4)")
            };

            int[] hits3D = new int[configs3D.Count];
            int[] hits4D = new int[configs4D.Count];

            long reward3D = 960000;
            long reward4D = 8880000;

            for (int k = startK; k < n; k++)
            {
                var past = records.Take(k).ToList();
                var actual = records[k];
                int actD4 = actual.Digits5[1];
                int actD3 = actual.Digits5[2];
                int actD2 = actual.Digits5[3];
                int actD1 = actual.Digits5[4];

                double[] vD4 = ComputeDigitPotential(past, 1, actual.DayOfWeek);
                double[] vD3 = ComputeDigitPotential(past, 2, actual.DayOfWeek);
                double[] vD2 = ComputeDigitPotential(past, 3, actual.DayOfWeek);
                double[] vD1 = ComputeDigitPotential(past, 4, actual.DayOfWeek);

                var sortD4 = Enumerable.Range(0, 10).OrderBy(d => vD4[d]).ToArray();
                var sortD3 = Enumerable.Range(0, 10).OrderBy(d => vD3[d]).ToArray();
                var sortD2 = Enumerable.Range(0, 10).OrderBy(d => vD2[d]).ToArray();
                var sortD1 = Enumerable.Range(0, 10).OrderBy(d => vD1[d]).ToArray();

                for (int i = 0; i < configs3D.Count; i++)
                {
                    var cfg = configs3D[i];
                    if (sortD3.Take(cfg.Item1).Contains(actD3) &&
                        sortD2.Take(cfg.Item2).Contains(actD2) &&
                        sortD1.Take(cfg.Item3).Contains(actD1))
                    {
                        hits3D[i]++;
                    }
                }

                for (int i = 0; i < configs4D.Count; i++)
                {
                    var cfg = configs4D[i];
                    if (sortD4.Take(cfg.Item1).Contains(actD4) &&
                        sortD3.Take(cfg.Item2).Contains(actD3) &&
                        sortD2.Take(cfg.Item3).Contains(actD2) &&
                        sortD1.Take(cfg.Item4).Contains(actD1))
                    {
                        hits4D[i]++;
                    }
                }
            }

            Console.WriteLine("=========================================================================================");
            Console.WriteLine("    BẢNG XẾP HẠNG CÁC DÀN 3D TỐI ƯU NHẤT TRÊN 500 KỲ (ĐỘC LẬP HOÀN TOÀN)");
            Console.WriteLine("=========================================================================================");
            Console.WriteLine("Tên Dàn 3D             | Quy Mô | Số Kỳ Trúng | Tỷ Lệ Thực | Ngẫu Nhiên | ROI / Hiệu Quất");
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            for (int i = 0; i < configs3D.Count; i++)
            {
                var cfg = configs3D[i];
                int size = cfg.Item1 * cfg.Item2 * cfg.Item3;
                PrintResultRow(cfg.Item4, size, hits3D[i], testPeriods, (double)size / 1000.0, reward3D);
            }

            Console.WriteLine("\n=========================================================================================");
            Console.WriteLine("    BẢNG XẾP HẠNG CÁC DÀN 4D TỐI ƯU NHẤT TRÊN 500 KỲ (ĐỘC LẬP HOÀN TOÀN)");
            Console.WriteLine("=========================================================================================");
            Console.WriteLine("Tên Dàn 4D             | Quy Mô | Số Kỳ Trúng | Tỷ Lệ Thực | Ngẫu Nhiên | ROI / Hiệu Suất");
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            for (int i = 0; i < configs4D.Count; i++)
            {
                var cfg = configs4D[i];
                int size = cfg.Item1 * cfg.Item2 * cfg.Item3 * cfg.Item4;
                PrintResultRow(cfg.Item5, size, hits4D[i], testPeriods, (double)size / 10000.0, reward4D);
            }
            Console.WriteLine("=========================================================================================");
        }

        private static void PrintResultRow(string name, int size, int hits, int total, double randProb, long reward)
        {
            double actRate = (double)hits / total * 100.0;
            double randRate = randProb * 100.0;
            long totalCost = (long)size * 1000 * total;
            long totalReward = (long)hits * reward;
            long pnl = totalReward - totalCost;
            double roi = totalCost > 0 ? ((double)pnl / totalCost * 100.0) : 0.0;
            string roiStr = pnl >= 0 
                ? string.Format("+{0:F1}% (LÃI +{1:N0}k)", roi, pnl / 1000) 
                : string.Format("{0:F1}% ({1:N0}k)", roi, pnl / 1000);
            
            Console.WriteLine("{0,-22} | {1,5} số | {2,4} / {3,-3}  | {4,6:F2}%   | {5,5:F2}%    | {6}",
                name, size, hits, total, actRate, randRate, roiStr);
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
                if (gan[d] == 0) r = 1.8;
                else if (gan[d] >= 5 && gan[d] <= 14) r = -2.0;
                else if (gan[d] > 25) r = 0.8;

                double k = -0.3 * (f30[d] - 3.0);
                double dowExp = dowTotal > 0 ? (double)dowTotal / 10.0 : 1.0;
                double dTerm = -0.4 * (dowCount[d] - dowExp);
                double dropTerm = (d == lastVal) ? -0.5 : 0.0;

                v[d] = r + k + dTerm + dropTerm;
            }
            return v;
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
