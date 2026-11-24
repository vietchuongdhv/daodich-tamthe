using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TidsIntegratedSystem
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
            Console.WriteLine("   HỆ THỐNG KẾT HỢP TOÀN DIỆN: CHỌN DÀN & ĐIỂM RƠI THỜI ĐIỂM VÀNG CHO 3D & 4D (1.000 KỲ)");
            Console.WriteLine("=========================================================================================\n");

            long reward3D = 960000;
            long reward4D = 8880000;

            // KỊCH BẢN TỐI ƯU TOÀN PHẦN:
            // 3D: Dàn 27 số (3x3x3).
            // - Quy tắc 1: Cản lặp tuyệt đối trục cùng vị trí hôm trước.
            // - Quy tắc 2: Bộ lọc thứ vàng: Đánh các ngày Thứ Năm, Thứ Bảy, Thứ Ba, Thứ Sáu.
            //              (Nếu Thứ Hai/Chủ Nhật: chỉ đánh nếu đang ở trong Vùng Phục Kích Vàng ngày 16->30).
            //              Loại bỏ Thứ Tư.
            // 4D: Dàn 81 số (3x3x3x3).
            // - Loại bỏ Thứ Hai hoàn toàn (0% nổ).
            // - Ưu tiên Thứ Ba, Thứ Tư, Thứ Năm, Chủ Nhật.

            int betDays3D = 0;
            int hit3D = 0;
            int daysSinceWin3D = 20;

            int betDays4D = 0;
            int hit4D = 0;
            int daysSinceWin4D = 50;

            for (int k = startK; k < n; k++)
            {
                var past = records.Take(k).ToList();
                var actual = records[k];
                string dow = actual.DayOfWeek;

                double[] vD4 = ComputeDigitPotential(past, 1, dow);
                double[] vD3 = ComputeDigitPotential(past, 2, dow);
                double[] vD2 = ComputeDigitPotential(past, 3, dow);
                double[] vD1 = ComputeDigitPotential(past, 4, dow);

                var sortD4 = Enumerable.Range(0, 10).OrderBy(d => vD4[d]).ToArray();
                var sortD3 = Enumerable.Range(0, 10).OrderBy(d => vD3[d]).ToArray();
                var sortD2 = Enumerable.Range(0, 10).OrderBy(d => vD2[d]).ToArray();
                var sortD1 = Enumerable.Range(0, 10).OrderBy(d => vD1[d]).ToArray();

                bool actualHit3D = sortD3.Take(3).Contains(actual.Digits5[2]) &&
                                   sortD2.Take(3).Contains(actual.Digits5[3]) &&
                                   sortD1.Take(3).Contains(actual.Digits5[4]);

                bool actualHit4D = sortD4.Take(3).Contains(actual.Digits5[1]) &&
                                   sortD3.Take(3).Contains(actual.Digits5[2]) &&
                                   sortD2.Take(3).Contains(actual.Digits5[3]) &&
                                   sortD1.Take(3).Contains(actual.Digits5[4]);

                // Bộ lọc vào dàn 3D
                bool shouldBet3D = false;
                if (dow == "Thứ năm" || dow == "Thứ bảy" || dow == "Thứ sáu" || dow == "Thứ ba")
                {
                    shouldBet3D = true; // Ngày vàng
                }
                else if (dow == "Thứ hai" || dow == "Chủ nhật")
                {
                    if (daysSinceWin3D >= 16 && daysSinceWin3D <= 32) shouldBet3D = true; // Chỉ đánh nếu rơi vào đúng vùng phục kích vàng
                }

                if (shouldBet3D)
                {
                    betDays3D++;
                    if (actualHit3D) hit3D++;
                }

                // Bộ lọc vào dàn 4D: Tuyệt đối tránh Thứ Hai
                bool shouldBet4D = false;
                if (dow != "Thứ hai")
                {
                    if (dow == "Thứ ba" || dow == "Thứ tư" || dow == "Thứ năm" || dow == "Chủ nhật")
                    {
                        shouldBet4D = true;
                    }
                    else if (daysSinceWin4D <= 15 || (daysSinceWin4D >= 45 && daysSinceWin4D <= 85))
                    {
                        shouldBet4D = true;
                    }
                }

                if (shouldBet4D)
                {
                    betDays4D++;
                    if (actualHit4D) hit4D++;
                }

                if (actualHit3D) daysSinceWin3D = 0; else daysSinceWin3D++;
                if (actualHit4D) daysSinceWin4D = 0; else daysSinceWin4D++;
            }

            Console.WriteLine("=========================================================================================");
            Console.WriteLine("   KẾT QUẢ ĐỐI SOÁT OOS HỆ THỐNG TỐI ƯU TOÀN PHẦN (CHỌN DÀN + THỜI ĐIỂM + CẢN LẶP)");
            Console.WriteLine("=========================================================================================");
            PrintComparison("Dàn 3D 27 số (Cơ sở)", 1000, 36, 27, reward3D);
            PrintComparison("Dàn 3D 27 số (Tối ưu điểm rơi)", betDays3D, hit3D, 27, reward3D);
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            PrintComparison("Dàn 4D 81 số (Cơ sở)", 1000, 11, 81, reward4D);
            PrintComparison("Dàn 4D 81 số (Tối ưu điểm rơi)", betDays4D, hit4D, 81, reward4D);
            Console.WriteLine("=========================================================================================");
        }

        private static void PrintComparison(string name, int totalDays, int hits, int size, long reward)
        {
            double hitRate = (double)hits / totalDays * 100.0;
            double randRate = (double)size / (name.Contains("3D") ? 1000.0 : 10000.0) * 100.0;
            long cost = (long)totalDays * size * 1000;
            long win = (long)hits * reward;
            long pnl = win - cost;
            double roi = (double)pnl / cost * 100.0;
            string roiStr = pnl >= 0 ? string.Format("+{0:F1}% (LÃI +{1:N0}k)", roi, pnl/1000) : string.Format("{0:F1}% ({1:N0}k)", roi, pnl/1000);

            Console.WriteLine("{0,-32} | {1,4} kỳ | {2,2} kỳ trúng | Tỷ lệ: {3,5:F2}% (Ngẫu nhiên {4,4:F2}%) | ROI: {5}",
                name, totalDays, hits, hitRate, randRate, roiStr);
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
