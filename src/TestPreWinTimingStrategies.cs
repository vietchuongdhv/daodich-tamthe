using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TidsTimingStrategy
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
            int testPeriods = 1000;
            int startK = n - testPeriods;

            Console.WriteLine("=========================================================================================");
            Console.WriteLine("   KIỂM THỬ CHIẾN LƯỢC ĐIỂM RƠI VÀ BỘ LỌC THỜI ĐIỂM VÀO DÀN 3D & 4D (1.000 KỲ OOS)");
            Console.WriteLine("=========================================================================================\n");

            // GIÁ TRỊ TRẢ THƯỞNG (Cược 1k)
            long reward3D = 960000;
            long reward4D = 8880000;

            // ---------------------------------------------------------------------------------
            // CHIẾN LƯỢC 1: ĐÁNH MỖI NGÀY (BASELINE - KHÔNG BỘ LỌC THỜI ĐIỂM)
            // ---------------------------------------------------------------------------------
            RunBaseline(records, startK, n, reward3D, reward4D);

            // ---------------------------------------------------------------------------------
            // CHIẾN LƯỢC 2: BỘ LỌC KHÁNG LẶP TUYỆT ĐỐI (ANTI-REPEAT ON SAME AXIS)
            // Loại bỏ hoàn toàn chữ số giải ĐB hôm qua ở từng trục, dồn xác suất vào các chữ số đang tích tụ
            // ---------------------------------------------------------------------------------
            RunAntiRepeatStrategy(records, startK, n, reward3D, reward4D);

            // ---------------------------------------------------------------------------------
            // CHIẾN LƯỢC 3: BỘ LỌC THỜI ĐIỂM VÀNG (TIMING REGIME FILTER / AMBUSH WINDOW)
            // Quy luật rút ra:
            // - Sau khi trúng, có "cửa sổ sóng ngắn" 1 -> 12 ngày (23% nổ tiếp)
            // - Nếu qua 12 ngày chưa nổ, nghỉ cược chờ đến ngày thứ 16.
            // - Từ ngày thứ 16 -> 35: Vào cược mạnh vì 57.1% các lần nổ tập trung ở đây!
            // ---------------------------------------------------------------------------------
            RunTimingWindowStrategy(records, startK, n, reward3D, reward4D);

            // ---------------------------------------------------------------------------------
            // CHIẾN LƯỢC 4: BỘ LỌC CỘNG HƯỞNG THỨ TRONG TUẦN (DOW RESONANCE FILTER)
            // Dồn vốn hoặc ưu tiên vào các ngày thứ Năm, thứ Bảy, thứ Ba, thứ Sáu
            // ---------------------------------------------------------------------------------
            RunDowResonanceStrategy(records, startK, n, reward3D, reward4D);
        }

        private static void RunBaseline(List<Record> records, int startK, int n, long reward3D, long reward4D)
        {
            int totalDays = n - startK;
            int hit3D_27 = 0;
            int hit3D_64 = 0;
            int hit4D_81 = 0;
            int hit4D_36 = 0;

            for (int k = startK; k < n; k++)
            {
                var past = records.Take(k).ToList();
                var actual = records[k];
                int actD4 = actual.Digits5[1];
                int actD3 = actual.Digits5[2];
                int actD2 = actual.Digits5[3];
                int actD1 = actual.Digits5[4];

                double[] vD4 = ComputeDigitPotential(past, 1, actual.DayOfWeek, false);
                double[] vD3 = ComputeDigitPotential(past, 2, actual.DayOfWeek, false);
                double[] vD2 = ComputeDigitPotential(past, 3, actual.DayOfWeek, false);
                double[] vD1 = ComputeDigitPotential(past, 4, actual.DayOfWeek, false);

                var sortD4 = Enumerable.Range(0, 10).OrderBy(d => vD4[d]).ToArray();
                var sortD3 = Enumerable.Range(0, 10).OrderBy(d => vD3[d]).ToArray();
                var sortD2 = Enumerable.Range(0, 10).OrderBy(d => vD2[d]).ToArray();
                var sortD1 = Enumerable.Range(0, 10).OrderBy(d => vD1[d]).ToArray();

                if (sortD3.Take(3).Contains(actD3) && sortD2.Take(3).Contains(actD2) && sortD1.Take(3).Contains(actD1)) hit3D_27++;
                if (sortD3.Take(4).Contains(actD3) && sortD2.Take(4).Contains(actD2) && sortD1.Take(4).Contains(actD1)) hit3D_64++;
                if (sortD4.Take(3).Contains(actD4) && sortD3.Take(3).Contains(actD3) && sortD2.Take(3).Contains(actD2) && sortD1.Take(3).Contains(actD1)) hit4D_81++;
                if (sortD4.Take(2).Contains(actD4) && sortD3.Take(2).Contains(actD3) && sortD2.Take(3).Contains(actD2) && sortD1.Take(3).Contains(actD1)) hit4D_36++;
            }

            Console.WriteLine("=========================================================================================");
            Console.WriteLine("1. CHIẾN LƯỢC GỐC: VÀO DÀN ĐỀU MỖI NGÀY (1.000 KỲ LIÊN TỤC)");
            Console.WriteLine("=========================================================================================");
            PrintMetrics("Dàn 3D 27 số", 27, hit3D_27, totalDays, reward3D);
            PrintMetrics("Dàn 3D 64 số", 64, hit3D_64, totalDays, reward3D);
            PrintMetrics("Dàn 4D 81 số", 81, hit4D_81, totalDays, reward4D);
            PrintMetrics("Dàn 4D 36 số", 36, hit4D_36, totalDays, reward4D);
            Console.WriteLine();
        }

        private static void RunAntiRepeatStrategy(List<Record> records, int startK, int n, long reward3D, long reward4D)
        {
            int totalDays = n - startK;
            int hit3D_27 = 0;
            int hit3D_64 = 0;
            int hit4D_81 = 0;
            int hit4D_36 = 0;

            for (int k = startK; k < n; k++)
            {
                var past = records.Take(k).ToList();
                var actual = records[k];
                int actD4 = actual.Digits5[1];
                int actD3 = actual.Digits5[2];
                int actD2 = actual.Digits5[3];
                int actD1 = actual.Digits5[4];

                // Cản lặp mạnh với chữ số hôm qua trên cùng trục
                double[] vD4 = ComputeDigitPotential(past, 1, actual.DayOfWeek, true);
                double[] vD3 = ComputeDigitPotential(past, 2, actual.DayOfWeek, true);
                double[] vD2 = ComputeDigitPotential(past, 3, actual.DayOfWeek, true);
                double[] vD1 = ComputeDigitPotential(past, 4, actual.DayOfWeek, true);

                var sortD4 = Enumerable.Range(0, 10).OrderBy(d => vD4[d]).ToArray();
                var sortD3 = Enumerable.Range(0, 10).OrderBy(d => vD3[d]).ToArray();
                var sortD2 = Enumerable.Range(0, 10).OrderBy(d => vD2[d]).ToArray();
                var sortD1 = Enumerable.Range(0, 10).OrderBy(d => vD1[d]).ToArray();

                if (sortD3.Take(3).Contains(actD3) && sortD2.Take(3).Contains(actD2) && sortD1.Take(3).Contains(actD1)) hit3D_27++;
                if (sortD3.Take(4).Contains(actD3) && sortD2.Take(4).Contains(actD2) && sortD1.Take(4).Contains(actD1)) hit3D_64++;
                if (sortD4.Take(3).Contains(actD4) && sortD3.Take(3).Contains(actD3) && sortD2.Take(3).Contains(actD2) && sortD1.Take(3).Contains(actD1)) hit4D_81++;
                if (sortD4.Take(2).Contains(actD4) && sortD3.Take(2).Contains(actD3) && sortD2.Take(3).Contains(actD2) && sortD1.Take(3).Contains(actD1)) hit4D_36++;
            }

            Console.WriteLine("=========================================================================================");
            Console.WriteLine("2. CHIẾN LƯỢC BẢN NÂNG CẤP: BỘ LỌC KHÁNG LẶP TUYỆT ĐỐI (ANTI-REPEAT)");
            Console.WriteLine("=========================================================================================");
            PrintMetrics("Dàn 3D 27 số", 27, hit3D_27, totalDays, reward3D);
            PrintMetrics("Dàn 3D 64 số", 64, hit3D_64, totalDays, reward3D);
            PrintMetrics("Dàn 4D 81 số", 81, hit4D_81, totalDays, reward4D);
            PrintMetrics("Dàn 4D 36 số", 36, hit4D_36, totalDays, reward4D);
            Console.WriteLine();
        }

        private static void RunTimingWindowStrategy(List<Record> records, int startK, int n, long reward3D, long reward4D)
        {
            // BỘ LỌC THỜI ĐIỂM:
            // Chỉ vào cược 3D khi:
            // - Hoặc trong vòng 12 ngày sau khi vừa nổ (sóng cộng hưởng ngắn)
            // - Hoặc từ ngày thứ 16 đến ngày 38 sau lần nổ trước (vùng nổ tập trung 57.1%)
            // Nghỉ cược ở ngày 13, 14, 15 (vùng chết) và sau ngày 38 (nếu rơi vào nhịp gan dài)
            int betDays3D = 0;
            int hit3D_27 = 0;
            int daysSinceWin3D = 20; // khởi tạo

            // Tương tự cho 4D 81s:
            // Chu kỳ trung bình 69 ngày. Chỉ vào cược khi cách lần nổ trước <= 15 ngày (sóng kép) HOẶC từ ngày 40 -> 85
            int betDays4D = 0;
            int hit4D_81 = 0;
            int daysSinceWin4D = 50;

            for (int k = startK; k < n; k++)
            {
                var past = records.Take(k).ToList();
                var actual = records[k];
                int actD4 = actual.Digits5[1];
                int actD3 = actual.Digits5[2];
                int actD2 = actual.Digits5[3];
                int actD1 = actual.Digits5[4];

                double[] vD4 = ComputeDigitPotential(past, 1, actual.DayOfWeek, true);
                double[] vD3 = ComputeDigitPotential(past, 2, actual.DayOfWeek, true);
                double[] vD2 = ComputeDigitPotential(past, 3, actual.DayOfWeek, true);
                double[] vD1 = ComputeDigitPotential(past, 4, actual.DayOfWeek, true);

                var sortD4 = Enumerable.Range(0, 10).OrderBy(d => vD4[d]).ToArray();
                var sortD3 = Enumerable.Range(0, 10).OrderBy(d => vD3[d]).ToArray();
                var sortD2 = Enumerable.Range(0, 10).OrderBy(d => vD2[d]).ToArray();
                var sortD1 = Enumerable.Range(0, 10).OrderBy(d => vD1[d]).ToArray();

                // 3D Timing Filter
                bool shouldBet3D = (daysSinceWin3D <= 12) || (daysSinceWin3D >= 16 && daysSinceWin3D <= 38);
                bool actualHit3D = sortD3.Take(3).Contains(actD3) && sortD2.Take(3).Contains(actD2) && sortD1.Take(3).Contains(actD1);

                if (shouldBet3D)
                {
                    betDays3D++;
                    if (actualHit3D) hit3D_27++;
                }

                if (actualHit3D) daysSinceWin3D = 0;
                else daysSinceWin3D++;

                // 4D Timing Filter
                bool shouldBet4D = (daysSinceWin4D <= 15) || (daysSinceWin4D >= 45 && daysSinceWin4D <= 90);
                bool actualHit4D = sortD4.Take(3).Contains(actD4) && sortD3.Take(3).Contains(actD3) && sortD2.Take(3).Contains(actD2) && sortD1.Take(3).Contains(actD1);

                if (shouldBet4D)
                {
                    betDays4D++;
                    if (actualHit4D) hit4D_81++;
                }

                if (actualHit4D) daysSinceWin4D = 0;
                else daysSinceWin4D++;
            }

            Console.WriteLine("=========================================================================================");
            Console.WriteLine("3. CHIẾN LƯỢC BỘ LỌC THỜI ĐIỂM VÀNG (TIMING WINDOW - CHỈ VÀO CƯỢC KHI THỊ TRƯỜNG VÀO VÙNG NỔ)");
            Console.WriteLine("=========================================================================================");
            PrintMetrics("Dàn 3D 27 số (Có Timing)", 27, hit3D_27, betDays3D, reward3D);
            PrintMetrics("Dàn 4D 81 số (Có Timing)", 81, hit4D_81, betDays4D, reward4D);
            Console.WriteLine();
        }

        private static void RunDowResonanceStrategy(List<Record> records, int startK, int n, long reward3D, long reward4D)
        {
            // Chỉ đánh vào các ngày Thứ Năm, Thứ Bảy, Thứ Ba, Thứ Sáu (4 ngày có năng lượng hội tụ cao nhất)
            int betDays3D = 0;
            int hit3D_27 = 0;
            int hit4D_81 = 0;

            var targetDows = new HashSet<string>() { "Thứ năm", "Thứ bảy", "Thứ ba", "Thứ sáu" };

            for (int k = startK; k < n; k++)
            {
                var past = records.Take(k).ToList();
                var actual = records[k];
                int actD4 = actual.Digits5[1];
                int actD3 = actual.Digits5[2];
                int actD2 = actual.Digits5[3];
                int actD1 = actual.Digits5[4];

                if (!targetDows.Contains(actual.DayOfWeek)) continue;

                betDays3D++;

                double[] vD4 = ComputeDigitPotential(past, 1, actual.DayOfWeek, true);
                double[] vD3 = ComputeDigitPotential(past, 2, actual.DayOfWeek, true);
                double[] vD2 = ComputeDigitPotential(past, 3, actual.DayOfWeek, true);
                double[] vD1 = ComputeDigitPotential(past, 4, actual.DayOfWeek, true);

                var sortD4 = Enumerable.Range(0, 10).OrderBy(d => vD4[d]).ToArray();
                var sortD3 = Enumerable.Range(0, 10).OrderBy(d => vD3[d]).ToArray();
                var sortD2 = Enumerable.Range(0, 10).OrderBy(d => vD2[d]).ToArray();
                var sortD1 = Enumerable.Range(0, 10).OrderBy(d => vD1[d]).ToArray();

                if (sortD3.Take(3).Contains(actD3) && sortD2.Take(3).Contains(actD2) && sortD1.Take(3).Contains(actD1)) hit3D_27++;
                if (sortD4.Take(3).Contains(actD4) && sortD3.Take(3).Contains(actD3) && sortD2.Take(3).Contains(actD2) && sortD1.Take(3).Contains(actD1)) hit4D_81++;
            }

            Console.WriteLine("=========================================================================================");
            Console.WriteLine("4. CHIẾN LƯỢC CỘNG HƯỞNG THEO THỨ (DOW RESONANCE: CHỈ ĐÁNH THỨ 5, THỨ 7, THỨ 3, THỨ 6)");
            Console.WriteLine("=========================================================================================");
            PrintMetrics("Dàn 3D 27 số (Thứ Vàng)", 27, hit3D_27, betDays3D, reward3D);
            PrintMetrics("Dàn 4D 81 số (Thứ Vàng)", 81, hit4D_81, betDays3D, reward4D);
            Console.WriteLine();
        }

        private static void PrintMetrics(string name, int size, int hits, int totalBetDays, long reward)
        {
            double hitRate = totalBetDays > 0 ? (double)hits / totalBetDays * 100.0 : 0.0;
            double randRate = ((double)size / (name.Contains("3D") ? 1000.0 : 10000.0)) * 100.0;
            long totalCost = (long)size * 1000 * totalBetDays;
            long totalReward = (long)hits * reward;
            long pnl = totalReward - totalCost;
            double roi = totalCost > 0 ? ((double)pnl / totalCost * 100.0) : 0.0;
            string roiStr = pnl >= 0 ? string.Format("+{0:F1}% (LÃI +{1:N0}k)", roi, pnl / 1000) : string.Format("{0:F1}% ({1:N0}k)", roi, pnl / 1000);

            Console.WriteLine("{0,-24} | Đánh {1,4} ngày | Trúng {2,3} kỳ | Tỷ lệ: {3,5:F2}% (Ngẫu nhiên {4,4:F2}%) | ROI: {5}",
                name, totalBetDays, hits, hitRate, randRate, roiStr);
        }

        private static double[] ComputeDigitPotential(List<Record> past, int digitIdx, string currentDow, bool applyAntiRepeat)
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
                // Gan Vàng (5 -> 14 kỳ)
                double r = 0.0;
                if (gan[d] == 0) r = applyAntiRepeat ? 3.5 : 1.8; // Cản lặp mạnh nếu bật AntiRepeat
                else if (gan[d] >= 5 && gan[d] <= 14) r = -2.2;   // Vùng hồi quy vàng sâu hơn
                else if (gan[d] > 25) r = 0.8;

                double k = -0.3 * (f30[d] - 3.0);
                double dowExp = dowTotal > 0 ? (double)dowTotal / 10.0 : 1.0;
                double dTerm = -0.4 * (dowCount[d] - dowExp);

                // Nếu bật anti-repeat, số vừa ra hôm qua bị phạt thế năng dương rất lớn (+2.5)
                double repeatPenalty = (applyAntiRepeat && d == lastVal) ? 2.5 : 0.0;

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
