using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TidsCounterfactual
{
    public class LotteryRecord
    {
        public int Id;
        public string Date;
        public string DayOfWeek;
        public int De;
        public List<int> UniqueLo;
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            string csvPath = @"data\lottery_master.csv";
            var records = LoadData(csvPath);
            int n = records.Count;

            int testPeriods = 300;
            int startK = n - testPeriods;

            Console.WriteLine("=========================================================================================");
            Console.WriteLine("   KHẢO SÁT CHUYÊN BIỆT: TÁI TÍNH TOÁN TOÀN BỘ LỊCH SỬ TRÊN KHÔNG GIAN CON 65 SỐ");
            Console.WriteLine("   So sánh Cách 1 (Lọc thế năng trực tiếp) vs Cách 2 (Tái lập toàn bộ mô hình lịch sử)");
            Console.WriteLine("   Mẫu thử nghiệm OOS: {0} kỳ gần nhất (từ {1} đến {2})", testPeriods, records[startK].Date, records[n - 1].Date);
            Console.WriteLine("=========================================================================================\n");

            int hitLvl1_65 = 0;

            // Cách 1: Chỉ lọc 42 số từ rổ thế năng hiện hành của Dàn 65 số
            int hitLvl2_ApproachA = 0;

            // Cách 2: LOẠI BỎ 35 SỐ KHỎI LỊCH SỬ VÀ TÍNH TOÁN LẠI TOÀN BỘ CSDL LỊCH SỬ TRÊN 65 SỐ
            // - Tái tính toán Gan trong không gian 65 số
            // - Tái tính toán Tần suất F100, F30 chuẩn hóa trên 65 số
            // - Tái tính toán Chuỗi Markov chuyển dịch 65 x 65
            // - Tái tính toán Phân phối biên 1D và BSM Options trong không gian 65 số
            int hitLvl2_ApproachB = 0;

            long reward = 99000;

            for (int k = startK; k < n; k++)
            {
                var past = records.Take(k).ToList();
                var actual = records[k];
                int actDe = actual.De;

                // BƯỚC 1: XÁC ĐỊNH DÀN 65 SỐ TỪ KHÔNG GIAN 100 SỐ BAN ĐẦU
                var baseEnergies100 = ComputeFullBsmEnergies(past, Enumerable.Range(0, 100).ToList());
                var dan65 = SelectFactorNeutral(baseEnergies100, 65);

                bool hit65 = dan65.Contains(actDe);
                if (hit65) hitLvl1_65++;

                // =====================================================================
                // CÁCH 1: LỌC TRỰC TIẾP (SUB-FILTERING)
                // Lấy 42 số có thế năng tốt nhất trong 65 số hiện hành mà không tính lại lịch sử
                // =====================================================================
                var subEnergiesA = baseEnergies100.Where(e => dan65.Contains(e.Key)).OrderBy(e => e.Value).ToList();
                var dan42_A = SelectFactorNeutral(subEnergiesA, 42);
                if (dan42_A.Contains(actDe)) hitLvl2_ApproachA++;

                // =====================================================================
                // CÁCH 2: TÁI LẬP TOÀN BỘ LÝ THUYẾT LỊCH SỬ TRÊN KHÔNG GIAN CON 65 SỐ (CONDITIONAL RE-ESTIMATION)
                // Xem như không gian mẫu chỉ gồm 65 số này.
                // Loại bỏ 35 số kia, tính lại Gan, Tần suất, Markov, Biên 1D trong nội bộ 65 số!
                // =====================================================================
                var recomputedEnergies65 = ComputeFullBsmEnergies(past, dan65);
                var dan42_B = SelectFactorNeutral(recomputedEnergies65, 42);
                if (dan42_B.Contains(actDe)) hitLvl2_ApproachB++;
            }

            Console.WriteLine("=========================================================================================");
            Console.WriteLine("   KẾT QUẢ ĐỐI SOÁT OOS TRÊN {0} KỲ CHO DÀN 42 SỐ (RÚT TỪ 65 SỐ)", testPeriods);
            Console.WriteLine("=========================================================================================");
            Console.WriteLine("Phương pháp chọn 42 số từ 65 số      | Số kỳ trúng | Tỷ lệ trúng | Ngẫu nhiên | Hiệu suất ROI %");
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            PrintRow("Dàn 65 số gốc (Tầng 1)              ", 65, hitLvl1_65, testPeriods, 0.65, reward);
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            PrintRow("Cách 1: Lọc thế năng trực tiếp      ", 42, hitLvl2_ApproachA, testPeriods, 0.42, reward);
            PrintRow("Cách 2: TÍNH LẠI TOÀN BỘ LỊCH SỬ 65s", 42, hitLvl2_ApproachB, testPeriods, 0.42, reward);
            Console.WriteLine("=========================================================================================\n");

            // So sánh chi tiết
            int diff = hitLvl2_ApproachB - hitLvl2_ApproachA;
            Console.WriteLine("--- ĐÁNH GIÁ CHÊNH LỆCH KHOA HỌC GIỮA CÁCH 1 VÀ CÁCH 2 ---");
            Console.WriteLine("1. Số kỳ trúng lệch nhau: {0} kỳ", diff >= 0 ? "+" + diff : diff.ToString());
            Console.WriteLine("2. Tỷ lệ giữ chân số trúng từ Dàn 65 số:");
            Console.WriteLine("   - Cách 1 (Lọc thế năng trực tiếp) : {0,5:F1}% ({1}/{2} kỳ trúng của Dàn 65)", 
                (double)hitLvl2_ApproachA / hitLvl1_65 * 100, hitLvl2_ApproachA, hitLvl1_65);
            Console.WriteLine("   - Cách 2 (Tái tính toàn bộ lịch sử) : {0,5:F1}% ({1}/{2} kỳ trúng của Dàn 65)", 
                (double)hitLvl2_ApproachB / hitLvl1_65 * 100, hitLvl2_ApproachB, hitLvl1_65);
            Console.WriteLine("   [Lý thuyết ngẫu nhiên rút 42 từ 65 :  64,6%]");
            Console.WriteLine("=========================================================================================");
        }

        private static void PrintRow(string name, int size, int hits, int total, double randProb, long reward)
        {
            double actRate = (double)hits / total * 100.0;
            double randRate = randProb * 100.0;
            long cost = (long)size * 1000 * total;
            long win = (long)hits * reward;
            long pnl = win - cost;
            double roi = total > 0 && cost > 0 ? ((double)pnl / cost * 100.0) : 0.0;
            string roiStr = pnl >= 0 ? string.Format("+{0:F1}% (LÃI +{1:N0}k)", roi, pnl / 1000) : string.Format("{0:F1}% ({1:N0}k)", roi, pnl / 1000);

            Console.WriteLine("{0} | {1,3} / {2,-3} | {3,6:F2}%   | {4,5:F2}%    | {5}",
                name, hits, total, actRate, randRate, roiStr);
        }

        private static List<int> SelectFactorNeutral(List<KeyValuePair<int, double>> sortedEnergies, int count)
        {
            var selected = new List<int>();
            var headCounts = new Dictionary<int, int>();
            int maxPerHead = (count / 10) + 1;

            foreach (var kv in sortedEnergies)
            {
                int head = kv.Key / 10;
                int hCount = 0;
                headCounts.TryGetValue(head, out hCount);
                if (hCount < maxPerHead)
                {
                    selected.Add(kv.Key);
                    headCounts[head] = hCount + 1;
                    if (selected.Count == count) break;
                }
            }

            if (selected.Count < count)
            {
                foreach (var kv in sortedEnergies)
                {
                    if (!selected.Contains(kv.Key))
                    {
                        selected.Add(kv.Key);
                        if (selected.Count == count) break;
                    }
                }
            }
            return selected;
        }

        // TÍNH TOÁN TOÀN BỘ MÔ HÌNH THẾ NĂNG BSM TRÊN MỘT TẬP SỐ MẪU BẤT KỲ (100 số hoặc 65 số)
        private static List<KeyValuePair<int, double>> ComputeFullBsmEnergies(List<LotteryRecord> past, List<int> activeUniverse)
        {
            int n = past.Count;
            var activeSet = new HashSet<int>(activeUniverse);
            int universeSize = activeUniverse.Count;

            // 1. TÍNH LẠI GAN TRONG LỊCH SỬ CHỈ CHO CÁC SỐ THUỘC ACTIVE UNIVERSE
            var gan = new Dictionary<int, int>();
            foreach (int s in activeUniverse)
            {
                int g = 0;
                for (int i = n - 1; i >= 0; i--)
                {
                    if (past[i].De == s) break;
                    g++;
                }
                gan[s] = g;
            }

            // 2. TÍNH LẠI TẦN SUẤT 100 KỲ VÀ CHUẨN HÓA KỲ VỌNG TRÊN KHÔNG GIAN NÀY
            var f100 = new Dictionary<int, int>();
            int totalF100InActive = 0;
            foreach (int s in activeUniverse) f100[s] = 0;

            for (int i = Math.Max(0, n - 100); i < n; i++)
            {
                if (activeSet.Contains(past[i].De))
                {
                    f100[past[i].De]++;
                    totalF100InActive++;
                }
            }

            // Kỳ vọng tần suất của 1 số trong không gian này:
            double expectedF = totalF100InActive > 0 ? (double)totalF100InActive / universeSize : 1.0;

            // 3. TÍNH LẠI MARKOV TRONG KHÔNG GIAN NÀY
            // Xét chuỗi con (Embedded Sub-chain): Chỉ khi số đề thuộc activeUniverse
            int lastDe = past[n - 1].De;
            var markovCount = new Dictionary<int, int>();
            foreach (int s in activeUniverse) markovCount[s] = 0;
            int totalMarkovTransitions = 0;

            for (int i = 0; i < n - 1; i++)
            {
                if (past[i].De == lastDe && activeSet.Contains(past[i + 1].De))
                {
                    markovCount[past[i + 1].De]++;
                    totalMarkovTransitions++;
                }
            }

            // 4. LÔ RƠI HÔM QUA
            var lastUniqueLo = past[n - 1].UniqueLo;

            // 5. TÍNH THẾ NĂNG TỔNG HỢP CHO TỪNG SỐ TRONG ACTIVE UNIVERSE
            var result = new List<KeyValuePair<int, double>>();

            foreach (int s in activeUniverse)
            {
                // a) Thế năng Gan (với rào cản cản lặp)
                double vRenewal = 0.0;
                int g = gan[s];
                if (g == 0) vRenewal = 3.5; // Cản lặp tuyệt đối
                else if (g >= 1 && g <= 25) vRenewal = -0.05 * g;
                else if (g > 50) vRenewal = 1.0;

                // b) Thế năng Tần suất F100 chuẩn hóa
                double vKinetic = -0.20 * (f100[s] - expectedF);

                // c) Thế năng Markov có điều kiện
                double pMarkov = totalMarkovTransitions > 0 
                    ? ((double)markovCount[s] + 0.1) / (totalMarkovTransitions + 1.0)
                    : 1.0 / universeSize;
                double vMarkov = -0.25 * Math.Log(pMarkov);

                // d) Lô rơi
                double vCascade = (lastUniqueLo != null && lastUniqueLo.Contains(s)) ? -1.0 : 0.0;

                // e) Cân bằng phân phối đầu trục (Head Density Tension)
                int head = s / 10;
                int headCountInUniverse = activeUniverse.Count(x => x / 10 == head);
                double headExpected = (double)universeSize / 10.0;
                double vHeadBalance = -0.15 * (headCountInUniverse - headExpected);

                double totalV = vRenewal + vKinetic + vMarkov + vCascade + vHeadBalance;
                result.Add(new KeyValuePair<int, double>(s, totalV));
            }

            return result.OrderBy(x => x.Value).ToList();
        }

        private static List<LotteryRecord> LoadData(string path)
        {
            var list = new List<LotteryRecord>();
            var lines = File.ReadAllLines(path, Encoding.UTF8);
            for (int i = 1; i < lines.Length; i++)
            {
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;
                var parts = ParseCsvLine(line);
                if (parts.Count < 18) continue;
                try
                {
                    var rec = new LotteryRecord();
                    rec.Id = int.Parse(parts[0]);
                    rec.Date = parts[1];
                    rec.DayOfWeek = parts[2].Trim('\"');
                    string gdb = parts[3].Trim('\"').PadLeft(5, '0');
                    if (gdb.Length > 5) gdb = gdb.Substring(gdb.Length - 5);
                    rec.De = (gdb[3] - '0') * 10 + (gdb[4] - '0');

                    rec.UniqueLo = new List<int>();
                    string loStr = parts[parts.Count - 1].Trim('\"');
                    var loNums = loStr.Split(new char[] { ' ', ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var lo in loNums)
                    {
                        int val;
                        if (int.TryParse(lo.Trim(), out val))
                        {
                            if (!rec.UniqueLo.Contains(val)) rec.UniqueLo.Add(val);
                        }
                    }

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
