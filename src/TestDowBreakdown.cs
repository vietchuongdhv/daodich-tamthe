using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TidsDowBreakdown
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
            int testPeriods = 1000;
            int startK = n - testPeriods;

            Console.WriteLine("=========================================================================================");
            Console.WriteLine("   CHI TIẾT HIỆU SUẤT TRÚNG 3D (27 SỐ) & 4D (81 SỐ) THEO TỪNG THỨ TRONG TUẦN (1.000 KỲ)");
            Console.WriteLine("=========================================================================================\n");

            var dows = new string[] { "Thứ hai", "Thứ ba", "Thứ tư", "Thứ năm", "Thứ sáu", "Thứ bảy", "Chủ nhật" };
            var daysByDow = new Dictionary<string, int>();
            var hits3DByDow = new Dictionary<string, int>();
            var hits4DByDow = new Dictionary<string, int>();

            foreach (var d in dows)
            {
                daysByDow[d] = 0;
                hits3DByDow[d] = 0;
                hits4DByDow[d] = 0;
            }

            long reward3D = 960000;
            long reward4D = 8880000;

            for (int k = startK; k < n; k++)
            {
                var past = records.Take(k).ToList();
                var actual = records[k];
                string dow = actual.DayOfWeek;
                daysByDow[dow]++;

                double[] vD4 = ComputeDigitPotential(past, 1, dow);
                double[] vD3 = ComputeDigitPotential(past, 2, dow);
                double[] vD2 = ComputeDigitPotential(past, 3, dow);
                double[] vD1 = ComputeDigitPotential(past, 4, dow);

                var sortD4 = Enumerable.Range(0, 10).OrderBy(d => vD4[d]).ToArray();
                var sortD3 = Enumerable.Range(0, 10).OrderBy(d => vD3[d]).ToArray();
                var sortD2 = Enumerable.Range(0, 10).OrderBy(d => vD2[d]).ToArray();
                var sortD1 = Enumerable.Range(0, 10).OrderBy(d => vD1[d]).ToArray();

                if (sortD3.Take(3).Contains(actual.Digits5[2]) &&
                    sortD2.Take(3).Contains(actual.Digits5[3]) &&
                    sortD1.Take(3).Contains(actual.Digits5[4]))
                {
                    hits3DByDow[dow]++;
                }

                if (sortD4.Take(3).Contains(actual.Digits5[1]) &&
                    sortD3.Take(3).Contains(actual.Digits5[2]) &&
                    sortD2.Take(3).Contains(actual.Digits5[3]) &&
                    sortD1.Take(3).Contains(actual.Digits5[4]))
                {
                    hits4DByDow[dow]++;
                }
            }

            Console.WriteLine("--- BẢNG HIỆU SUẤT DÀN 3D 27 SỐ THEO THỨ ---");
            Console.WriteLine("Thứ        | Số kỳ | Số kỳ trúng | Tỷ lệ trúng | Ngẫu nhiên | Chi phí    | Tiền thưởng | Lãi ròng / ROI");
            Console.WriteLine("----------------------------------------------------------------------------------------------------");
            foreach (var d in dows)
            {
                int tot = daysByDow[d];
                int hit = hits3DByDow[d];
                double rate = (double)hit / tot * 100.0;
                long cost = (long)tot * 27 * 1000;
                long win = (long)hit * reward3D;
                long pnl = win - cost;
                double roi = (double)pnl / cost * 100.0;
                string pnlStr = pnl >= 0 ? string.Format("+{0:F1}% (LÃI +{1:N0}k)", roi, pnl/1000) : string.Format("{0:F1}% ({1:N0}k)", roi, pnl/1000);
                Console.WriteLine("{0,-10} | {1,5} | {2,11} | {3,10:F2}% | {4,9:F2}% | {5,7:N0}k | {6,8:N0}k | {7}",
                    d, tot, hit, rate, 2.70, cost/1000, win/1000, pnlStr);
            }

            Console.WriteLine("\n--- BẢNG HIỆU SUẤT DÀN 4D 81 SỐ THEO THỨ ---");
            Console.WriteLine("Thứ        | Số kỳ | Số kỳ trúng | Tỷ lệ trúng | Ngẫu nhiên | Chi phí    | Tiền thưởng | Lãi ròng / ROI");
            Console.WriteLine("----------------------------------------------------------------------------------------------------");
            foreach (var d in dows)
            {
                int tot = daysByDow[d];
                int hit = hits4DByDow[d];
                double rate = (double)hit / tot * 100.0;
                long cost = (long)tot * 81 * 1000;
                long win = (long)hit * reward4D;
                long pnl = win - cost;
                double roi = (double)pnl / cost * 100.0;
                string pnlStr = pnl >= 0 ? string.Format("+{0:F1}% (LÃI +{1:N0}k)", roi, pnl/1000) : string.Format("{0:F1}% ({1:N0}k)", roi, pnl/1000);
                Console.WriteLine("{0,-10} | {1,5} | {2,11} | {3,10:F2}% | {4,9:F2}% | {5,7:N0}k | {6,8:N0}k | {7}",
                    d, tot, hit, rate, 0.81, cost/1000, win/1000, pnlStr);
            }
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
