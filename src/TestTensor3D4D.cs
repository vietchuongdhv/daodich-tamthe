using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TidsTesting
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
        public int Head4D;
        public List<int> LoList;
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            string csvPath = @"data\lottery_master.csv";
            if (!File.Exists(csvPath))
            {
                Console.WriteLine("Khong tim thay file " + csvPath);
                return;
            }

            var records = LoadData(csvPath);
            int n = records.Count;
            Console.WriteLine("Loaded {0} records.", n);

            // Chạy Walk-Forward Backtest trên 200 kỳ gần nhất
            int testPeriods = 200;
            int start = n - testPeriods;

            Console.WriteLine("=========================================================================");
            Console.WriteLine("   BẮT ĐẦU KIỂM THỬ ĐỘ ĐỘT PHÁ THUẬT TOÁN 3D & 4D (WALK-FORWARD OOS)     ");
            Console.WriteLine("   Số kỳ kiểm thử: {0} kỳ gần nhất (từ {1} đến {2})", testPeriods, records[start].Date, records[n-1].Date);
            Console.WriteLine("=========================================================================");

            // Biến đếm kết quả Mô hình Hiện tại (Baseline)
            int baseHitD3 = 0;       // Top 3 Càng đúng
            int baseHit3D_12 = 0;    // Dàn 3D 12 số trúng
            int baseHit3D_27 = 0;    // Dàn 3D 27 số trúng
            int baseHit4D_16 = 0;    // Dàn 4D 16 số trúng
            int baseHit4D_81 = 0;    // Dàn 4D 81 số trúng

            // Biến đếm kết quả Mô hình Mới (Tensor Entangled & Fallback)
            int newHitD3 = 0;
            int newHit3D_12 = 0;
            int newHit3D_27 = 0;
            int newHit4D_16 = 0;
            int newHit4D_81 = 0;

            for (int k = start; k < n; k++)
            {
                var past = records.Take(k).ToList();
                var actual = records[k];
                int actualD3 = actual.Digits5[2];
                string actual3D = actual.Gdb.Length >= 3 ? actual.Gdb.Substring(actual.Gdb.Length - 3) : "";
                string actual4D = actual.Gdb.Length >= 4 ? actual.Gdb.Substring(actual.Gdb.Length - 4) : "";

                // ----------------------------------------------------
                // 1. MÔ HÌNH HIỆN TẠI (BASELINE)
                // ----------------------------------------------------
                // 1.1 Càng hiện tại: Gan 8-20 và F30
                int[] ganD3 = new int[10];
                for (int d = 0; d < 10; d++)
                {
                    for (int i = k - 1; i >= 0; i--)
                    {
                        if (past[i].Digits5[2] == d) break;
                        ganD3[d]++;
                    }
                }
                int[] f30D3 = new int[10];
                for (int i = Math.Max(0, k - 30); i < k; i++) f30D3[past[i].Digits5[2]]++;
                var baseD3Scores = new List<Tuple<int, double>>();
                for (int d = 0; d < 10; d++)
                {
                    double rTerm = (ganD3[d] >= 8 && ganD3[d] <= 20) ? -1.5 : (ganD3[d] == 0 ? 1.5 : 0.0);
                    double kTerm = -0.3 * (f30D3[d] - 3.0);
                    baseD3Scores.Add(new Tuple<int, double>(d, rTerm + kTerm));
                }
                var baseTopD3 = baseD3Scores.OrderBy(x => x.Item2).Take(3).Select(x => x.Item1).ToArray();

                if (baseTopD3.Contains(actualD3)) baseHitD3++;

                // Lấy 4 đề và 9 đề của past theo thế năng vi mô
                var de4 = ComputeTopDe(past, 4);
                var de9 = ComputeTopDe(past, 9);

                var baseDan12 = new List<string>();
                foreach (int h in baseTopD3)
                    foreach (int t in de4)
                        baseDan12.Add(h.ToString() + t.ToString("D2"));
                if (baseDan12.Contains(actual3D)) baseHit3D_12++;

                var baseDan27 = new List<string>();
                foreach (int h in baseTopD3)
                    foreach (int t in de9)
                        baseDan27.Add(h.ToString() + t.ToString("D2"));
                if (baseDan27.Contains(actual3D)) baseHit3D_27++;

                // 4D baseline
                int[] ganHead = new int[100];
                for (int s = 0; s < 100; s++)
                {
                    for (int i = k - 1; i >= 0; i--)
                    {
                        if (past[i].Head4D == s) break;
                        ganHead[s]++;
                    }
                }
                int[] f30Head = new int[100];
                for (int i = Math.Max(0, k - 30); i < k; i++) f30Head[past[i].Head4D]++;
                var baseHeadScores = new List<Tuple<int, double>>();
                for (int s = 0; s < 100; s++)
                {
                    double rTerm = 0.0;
                    if (ganHead[s] == 0) rTerm = 2.0;
                    else if (ganHead[s] >= 8 && ganHead[s] <= 20) rTerm = -1.6 + (Math.Abs(ganHead[s] - 13) / 10.0);
                    else if (ganHead[s] > 30) rTerm = 1.0;
                    double kTerm = -0.5 * (f30Head[s] - 0.30);
                    baseHeadScores.Add(new Tuple<int, double>(s, rTerm + kTerm));
                }
                var baseTopHeads = baseHeadScores.OrderBy(x => x.Item2).Take(4).Select(x => x.Item1).ToArray();
                var baseTop9Heads = baseHeadScores.OrderBy(x => x.Item2).Take(9).Select(x => x.Item1).ToArray();

                var baseDan4D_16 = new List<string>();
                foreach (int hd in baseTopHeads)
                    foreach (int tl in de4)
                        baseDan4D_16.Add(hd.ToString("D2") + tl.ToString("D2"));
                if (baseDan4D_16.Contains(actual4D)) baseHit4D_16++;

                var baseDan4D_81 = new List<string>();
                foreach (int hd in baseTop9Heads)
                    foreach (int tl in de9)
                        baseDan4D_81.Add(hd.ToString("D2") + tl.ToString("D2"));
                if (baseDan4D_81.Contains(actual4D)) baseHit4D_81++;

                // ----------------------------------------------------
                // 2. MÔ HÌNH MỚI (TENSOR MULTI-FACTOR & CÀNG RƠI)
                // ----------------------------------------------------
                var lastPast = past[k - 1];
                int lastD3 = lastPast.Digits5[2];
                int lastDeDau = lastPast.Digits5[3];
                int lastDeDuoi = lastPast.Digits5[4];
                int lastG1D3 = (lastPast.G1 != null && lastPast.G1.Length >= 3) ? (lastPast.G1[2] - '0') : -1;
                int lastG1Tail = (lastPast.G1 != null && lastPast.G1.Length >= 5) ? (lastPast.G1[4] - '0') : -1;

                // Tần suất theo ngày trong tuần (Day of week) của d3
                string curDow = actual.DayOfWeek;
                int[] fDowD3 = new int[10];
                int dowCount = 0;
                for (int i = Math.Max(0, k - 300); i < k; i++)
                {
                    if (past[i].DayOfWeek == curDow)
                    {
                        fDowD3[past[i].Digits5[2]]++;
                        dowCount++;
                    }
                }

                var newD3Scores = new List<Tuple<int, double>>();
                for (int d = 0; d < 10; d++)
                {
                    // 1. Gan Term chuẩn hóa
                    double rTerm = 0.0;
                    if (ganD3[d] == 0) rTerm = 1.2; // cản lặp
                    else if (ganD3[d] >= 7 && ganD3[d] <= 24) rTerm = -1.8 + Math.Abs(ganD3[d] - 14) * 0.05;
                    else if (ganD3[d] > 35) rTerm = 0.8;

                    // 2. Tần suất 30 ngày
                    double kTerm = -0.35 * (f30D3[d] - 3.0);

                    // 3. Hiệu ứng Càng Rơi (Ball-Drop Transition)
                    double dropTerm = 0.0;
                    if (d == lastG1D3) dropTerm -= 1.1; // Rơi từ tâm giải Nhất
                    if (d == lastG1Tail) dropTerm -= 0.8; // Rơi từ đuôi giải Nhất
                    if (d == lastDeDau || d == lastDeDuoi) dropTerm -= 0.6; // Rơi từ Đề hôm qua
                    if (d == lastD3) dropTerm += 0.5; // Cản Càng bệt 2 ngày

                    // 4. Chu kỳ Thứ
                    double dowTerm = 0.0;
                    if (dowCount > 0)
                    {
                        double expDow = (double)dowCount / 10.0;
                        dowTerm = -0.4 * (fDowD3[d] - expDow);
                    }

                    double score = rTerm + kTerm + dropTerm + dowTerm;
                    newD3Scores.Add(new Tuple<int, double>(d, score));
                }
                var newTopD3 = newD3Scores.OrderBy(x => x.Item2).Take(3).Select(x => x.Item1).ToArray();

                if (newTopD3.Contains(actualD3)) newHitD3++;

                // MA TRẬN VƯỚNG VÍU 3D: LỌC TỔ HỢP THEO TỔNG 3 SỐ
                // Tính gan tổng 3 số
                int[] ganSum3D = new int[10];
                for (int s = 0; s < 10; s++)
                {
                    for (int i = k - 1; i >= 0; i--)
                    {
                        int sum = (past[i].Digits5[2] + past[i].Digits5[3] + past[i].Digits5[4]) % 10;
                        if (sum == s) break;
                        ganSum3D[s]++;
                    }
                }

                // Dàn 3D 12 số mới: Ưu tiên ghép cặp có Tổng 3 số ở vùng thế năng hút
                var newPairs12 = new List<Tuple<string, double>>();
                foreach (int h in newTopD3)
                {
                    foreach (int t in de4)
                    {
                        int sum3 = (h + (t / 10) + (t % 10)) % 10;
                        double penalty = 0.0;
                        if (ganSum3D[sum3] == 0) penalty = 0.8; // vừa ra tổng này hôm qua
                        else if (ganSum3D[sum3] >= 6 && ganSum3D[sum3] <= 20) penalty = -1.2; // tổng nén
                        newPairs12.Add(new Tuple<string, double>(h.ToString() + t.ToString("D2"), penalty));
                    }
                }
                var newDan12 = newPairs12.OrderBy(x => x.Item2).Take(12).Select(x => x.Item1).ToList();
                if (newDan12.Contains(actual3D)) newHit3D_12++;

                // Dàn 3D 27 số mới: Ghép với Top 9 đề và lọc tổng
                var newPairs27 = new List<Tuple<string, double>>();
                foreach (int h in newTopD3)
                {
                    foreach (int t in de9)
                    {
                        int sum3 = (h + (t / 10) + (t % 10)) % 10;
                        double penalty = 0.0;
                        if (ganSum3D[sum3] == 0) penalty = 0.8;
                        else if (ganSum3D[sum3] >= 6 && ganSum3D[sum3] <= 20) penalty = -1.2;
                        newPairs27.Add(new Tuple<string, double>(h.ToString() + t.ToString("D2"), penalty));
                    }
                }
                var newDan27 = newPairs27.OrderBy(x => x.Item2).Take(27).Select(x => x.Item1).ToList();
                if (newDan27.Contains(actual3D)) newHit3D_27++;

                // 4D MỚI: PHÂN RÃ MARKOV P(d5) x P(d4|d5)
                // Hàng chục nghìn d5 (10 số)
                int[] ganD5 = new int[10];
                for (int d = 0; d < 10; d++)
                {
                    for (int i = k - 1; i >= 0; i--)
                    {
                        if (past[i].Digits5[0] == d) break;
                        ganD5[d]++;
                    }
                }
                int[] f30D5 = new int[10];
                for (int i = Math.Max(0, k - 30); i < k; i++) f30D5[past[i].Digits5[0]]++;
                var d5Scores = new List<Tuple<int, double>>();
                for (int d = 0; d < 10; d++)
                {
                    double r = (ganD5[d] >= 7 && ganD5[d] <= 25) ? -1.5 : (ganD5[d] == 0 ? 1.0 : 0.0);
                    double kF = -0.3 * (f30D5[d] - 3.0);
                    d5Scores.Add(new Tuple<int, double>(d, r + kF));
                }
                var top2_D5 = d5Scores.OrderBy(x => x.Item2).Take(2).Select(x => x.Item1).ToArray();
                var top3_D5 = d5Scores.OrderBy(x => x.Item2).Take(3).Select(x => x.Item1).ToArray();

                // Hàng nghìn d4 (10 số)
                int[] ganD4 = new int[10];
                for (int d = 0; d < 10; d++)
                {
                    for (int i = k - 1; i >= 0; i--)
                    {
                        if (past[i].Digits5[1] == d) break;
                        ganD4[d]++;
                    }
                }
                int[] f30D4 = new int[10];
                for (int i = Math.Max(0, k - 30); i < k; i++) f30D4[past[i].Digits5[1]]++;
                var d4Scores = new List<Tuple<int, double>>();
                for (int d = 0; d < 10; d++)
                {
                    double r = (ganD4[d] >= 7 && ganD4[d] <= 25) ? -1.5 : (ganD4[d] == 0 ? 1.0 : 0.0);
                    double kF = -0.3 * (f30D4[d] - 3.0);
                    d4Scores.Add(new Tuple<int, double>(d, r + kF));
                }
                var top2_D4 = d4Scores.OrderBy(x => x.Item2).Take(2).Select(x => x.Item1).ToArray();
                var top3_D4 = d4Scores.OrderBy(x => x.Item2).Take(3).Select(x => x.Item1).ToArray();

                // 4 Cặp đầu tốt nhất tạo từ 2 x 2
                var newTop4Heads = new List<int>();
                foreach (int d5 in top2_D5)
                    foreach (int d4 in top2_D4)
                        newTop4Heads.Add(d5 * 10 + d4);

                // 9 Cặp đầu tốt nhất tạo từ 3 x 3
                var newTop9Heads = new List<int>();
                foreach (int d5 in top3_D5)
                    foreach (int d4 in top3_D4)
                        newTop9Heads.Add(d5 * 10 + d4);

                var newDan4D_16 = new List<string>();
                foreach (int hd in newTop4Heads)
                    foreach (int tl in de4)
                        newDan4D_16.Add(hd.ToString("D2") + tl.ToString("D2"));
                if (newDan4D_16.Contains(actual4D)) newHit4D_16++;

                var newDan4D_81 = new List<string>();
                foreach (int hd in newTop9Heads)
                    foreach (int tl in de9)
                        newDan4D_81.Add(hd.ToString("D2") + tl.ToString("D2"));
                if (newDan4D_81.Contains(actual4D)) newHit4D_81++;
            }

            Console.WriteLine("\n=========================================================================");
            Console.WriteLine("                KẾT QUẢ SO SÁNH ĐỐI SOÁT OOS ({0} KỲ)                   ", testPeriods);
            Console.WriteLine("=========================================================================");
            Console.WriteLine("Chỉ số kiểm thử                       | Mô hình Cũ        | MÔ HÌNH MỚI (TENSOR)");
            Console.WriteLine("-------------------------------------------------------------------------");
            Console.WriteLine("1. Bắt trúng Càng 3D (Top 3)          | {0,3} kỳ ({1:F1}%)  | {2,3} kỳ ({3:F1}%) ★",
                baseHitD3, (double)baseHitD3 / testPeriods * 100, newHitD3, (double)newHitD3 / testPeriods * 100);
            Console.WriteLine("2. Dàn 3 Càng Tinh Tuyển 12 số (3D)   | {0,3} kỳ ({1:F1}%)  | {2,3} kỳ ({3:F1}%) ★",
                baseHit3D_12, (double)baseHit3D_12 / testPeriods * 100, newHit3D_12, (double)newHit3D_12 / testPeriods * 100);
            Console.WriteLine("3. Dàn 3 Càng Phổ 27 số (3D)          | {0,3} kỳ ({1:F1}%)  | {2,3} kỳ ({3:F1}%) ★",
                baseHit3D_27, (double)baseHit3D_27 / testPeriods * 100, newHit3D_27, (double)newHit3D_27 / testPeriods * 100);
            Console.WriteLine("4. Dàn 4 Càng Tinh Chọn 16 số (4D)    | {0,3} kỳ ({1:F1}%)  | {2,3} kỳ ({3:F1}%) ★",
                baseHit4D_16, (double)baseHit4D_16 / testPeriods * 100, newHit4D_16, (double)newHit4D_16 / testPeriods * 100);
            Console.WriteLine("5. Dàn 4 Càng Mở Rộng 81 số (4D)      | {0,3} kỳ ({1:F1}%)  | {2,3} kỳ ({3:F1}%) ★",
                baseHit4D_81, (double)baseHit4D_81 / testPeriods * 100, newHit4D_81, (double)newHit4D_81 / testPeriods * 100);
            Console.WriteLine("=========================================================================");
        }

        private static List<int> ComputeTopDe(List<SimpleRecord> past, int count)
        {
            int n = past.Count;
            int[] gan = new int[100];
            for (int s = 0; s < 100; s++)
            {
                for (int i = n - 1; i >= 0; i--)
                {
                    if (past[i].De == s) break;
                    gan[s]++;
                }
            }
            int[] f100 = new int[100];
            for (int i = Math.Max(0, n - 100); i < n; i++) f100[past[i].De]++;

            var scores = new List<Tuple<int, double>>();
            for (int s = 0; s < 100; s++)
            {
                double r = (gan[s] >= 30 && gan[s] <= 90) ? -1.8 : (gan[s] == 0 ? 1.5 : 0.0);
                double k = -0.3 * (f100[s] - 1.0);
                scores.Add(new Tuple<int, double>(s, r + k));
            }
            return scores.OrderBy(x => x.Item2).Take(count).Select(x => x.Item1).ToList();
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
                    if (rec.De < 0 || rec.De > 99) continue;
                    rec.Head4D = rec.Digits5[1] * 10 + rec.Digits5[2];
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
