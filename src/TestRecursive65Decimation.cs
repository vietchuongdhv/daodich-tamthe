using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TidsRecursive65
{
    public class Record
    {
        public int Id;
        public string Date;
        public string DayOfWeek;
        public int State2D; // 00 -> 99
        public int D2;      // Hàng chục
        public int D1;      // Hàng đơn vị
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
            int testPeriods = 500;
            int startK = n - testPeriods;

            Console.WriteLine("=========================================================================================");
            Console.WriteLine("   KHẢO SÁT Ý TƯỞNG: TÁI CHUẨN HÓA ĐỆ QUY 65% RÚT GỌN ĐA TẦNG TỪ DÀN 65 SỐ VỀ BẠCH THỦ");
            Console.WriteLine("   Mẫu kiểm thử thực nghiệm: {0} kỳ gần nhất ({1} -> {2})", testPeriods, records[startK].Date, records[n - 1].Date);
            Console.WriteLine("=========================================================================================\n");

            // Các tầng quy mô theo nguyên tắc giảm 65% liên tục:
            // Tầng 0: 100 số
            // Tầng 1: 100 * 65% = 65 số
            // Tầng 2: 65 * 65% = 42 số
            // Tầng 3: 42 * 65% = 27 số
            // Tầng 4: 27 * 65% = 18 số
            // Tầng 5: 18 * 65% = 12 số
            // Tầng 6: 12 * 65% = 8 số
            // Tầng 7: 8 * 65% = 5 số
            // Tầng 8: 5 * 65% = 3 số
            // Tầng 9: 3 * 65% = 2 số (Song thủ)
            // Tầng 10: 2 * 65% = 1 số (Bạch thủ)
            int[] cascadeSizes = new int[] { 65, 42, 27, 18, 12, 8, 5, 3, 2, 1 };
            int numLevels = cascadeSizes.Length;

            // Bộ đếm trúng cho 2 phương pháp:
            // Phương pháp A: Cắt lớp tĩnh (Top N trực tiếp từ bảng thế năng 100 số ban đầu)
            int[] hitsStatic = new int[numLevels];

            // Phương pháp B: Tái chuẩn hóa đệ quy không gian con (Subspace Renormalization)
            // Tại mỗi tầng, sau khi loại bỏ 35% số yếu nhất, ta tái tính toán lại tương quan ma trận cặp
            // và phân phối biên có điều kiện trên tập số còn lại, rồi mới chọn tiếp 65%!
            int[] hitsRecursive = new int[numLevels];

            // Tỷ lệ trả thưởng 2D chuẩn thị trường (cược 1k ăn 99k)
            long reward2D = 99000;

            for (int k = startK; k < n; k++)
            {
                var past = records.Take(k).ToList();
                var actual = records[k];
                int act2D = actual.State2D;

                // 1. TÍNH BẢNG THẾ NĂNG 100 SỐ BAN ĐẦU THEO CHUẨN TIDS 2D
                double[] initialPotentials = ComputeTids2DPotentials(past, actual.DayOfWeek);

                // =============================================================
                // PHƯƠNG PHÁP A: CẮT LỚP TĨNH (STATIC RANKING)
                // =============================================================
                var sortedStatic = Enumerable.Range(0, 100)
                                             .OrderBy(s => initialPotentials[s])
                                             .ToArray();

                for (int lvl = 0; lvl < numLevels; lvl++)
                {
                    int sz = cascadeSizes[lvl];
                    if (sortedStatic.Take(sz).Contains(act2D))
                    {
                        hitsStatic[lvl]++;
                    }
                }

                // =============================================================
                // PHƯƠNG PHÁP B: TÁI CHUẨN HÓA ĐỆ QUY KHÔNG GIAN CON (RECURSIVE SUBSPACE)
                // =============================================================
                List<int> currentPool = Enumerable.Range(0, 100).ToList();

                for (int lvl = 0; lvl < numLevels; lvl++)
                {
                    int targetSize = cascadeSizes[lvl];

                    // Tái tính toán lại năng lượng có điều kiện cho các số trong currentPool
                    var recomputedScores = RecalculateSubspacePotentials(past, currentPool, initialPotentials);

                    // Chọn Top targetSize số có năng lượng thấp nhất trong pool
                    var nextPool = currentPool.OrderBy(s => recomputedScores[s])
                                              .Take(targetSize)
                                              .ToList();

                    if (nextPool.Contains(act2D))
                    {
                        hitsRecursive[lvl]++;
                    }

                    currentPool = nextPool;
                }
            }

            // =========================================================================
            // BÁO CÁO ĐỐI SOÁT CHI TIẾT THEO CÁC TẦNG GIẢM 65%
            // =========================================================================
            Console.WriteLine("===================================================================================================================");
            Console.WriteLine("   BẢNG ĐỐI SOÁT CÁC TẦNG RÚT GỌN 65% (TỪ DÀN 65 SỐ VỀ BẠCH THỦ TRÊN 500 KỲ)");
            Console.WriteLine("===================================================================================================================");
            Console.WriteLine("Tầng | Quy mô | Ngẫu nhiên | CẮT LỚP TĨNH (Top 100 gốc)       | TÁI CHUẨN HÓA ĐỆ QUY (Recalculated) | Độ lệch (Delta)");
            Console.WriteLine("     | dàn    | xác suất   | Số kỳ trúng | Tỷ lệ %   | ROI %  | Số kỳ trúng | Tỷ lệ %   | ROI %   | Hiệu ứng đệ quy");
            Console.WriteLine("-------------------------------------------------------------------------------------------------------------------");

            for (int lvl = 0; lvl < numLevels; lvl++)
            {
                int sz = cascadeSizes[lvl];
                double randProb = (double)sz / 100.0 * 100.0;

                int hA = hitsStatic[lvl];
                double rateA = (double)hA / testPeriods * 100.0;
                long costA = (long)sz * 1000 * testPeriods;
                long winA = (long)hA * reward2D;
                double roiA = ((double)(winA - costA) / costA) * 100.0;

                int hB = hitsRecursive[lvl];
                double rateB = (double)hB / testPeriods * 100.0;
                long costB = (long)sz * 1000 * testPeriods;
                long winB = (long)hB * reward2D;
                double roiB = ((double)(winB - costB) / costB) * 100.0;

                int diffHits = hB - hA;
                string diffStr = diffHits >= 0 ? string.Format("+{0} kỳ", diffHits) : string.Format("{0} kỳ", diffHits);

                Console.WriteLine("Lvl {0,2} | {1,2} số  | {2,6:F2}%   | {3,4} / {4,-3}   | {5,6:F2}% | {6,6:F1}% | {7,4} / {8,-3}   | {9,6:F2}% | {10,6:F1}% | {11,10}",
                    lvl + 1, sz, randProb,
                    hA, testPeriods, rateA, roiA,
                    hB, testPeriods, rateB, roiB,
                    diffStr);
            }
            Console.WriteLine("===================================================================================================================\n");

            // Phân tích điều kiện sống sót (Conditional Survival Probability)
            Console.WriteLine("--- PHÂN TÍCH XÁC SUẤT BẢO TOÀN CON SỐ TRÚNG QUA TỪNG TẦNG GẠT 35% ---");
            Console.WriteLine("Nếu tầng trước TRÚNG, xác suất tầng kế tiếp vẫn GIỮ ĐƯỢC con số trúng là bao nhiêu?");
            Console.WriteLine("----------------------------------------------------------------------------------");
            for (int lvl = 0; lvl < numLevels - 1; lvl++)
            {
                double retainRate = (double)hitsRecursive[lvl + 1] / hitsRecursive[lvl] * 100.0;
                Console.WriteLine("Từ Lvl {0,2} ({1,2} số) -> Lvl {2,2} ({3,2} số): Tỷ lệ bảo toàn = {4,5:F1}% (Lý thuyết gạt ngẫu nhiên = {5,4:F1}%)",
                    lvl + 1, cascadeSizes[lvl], lvl + 2, cascadeSizes[lvl + 1], retainRate, (double)cascadeSizes[lvl + 1] / cascadeSizes[lvl] * 100.0);
            }
            Console.WriteLine("==================================================================================");
        }

        // Tái tính toán thế năng trong không gian con (Subspace Conditioning)
        private static Dictionary<int, double> RecalculateSubspacePotentials(List<Record> past, List<int> pool, double[] initialV)
        {
            var dict = new Dictionary<int, double>();
            int n = past.Count;

            // 1. Tính mật độ xuất hiện của các đầu (hàng chục) và đuôi (hàng đơn vị) trong pool còn lại
            int[] headCounts = new int[10];
            int[] tailCounts = new int[10];
            int[] parityCounts = new int[4]; // CC, CL, LC, LL
            int[] sumCounts = new int[10];

            foreach (var s in pool)
            {
                int h = s / 10;
                int t = s % 10;
                headCounts[h]++;
                tailCounts[t]++;
                int p = ((h % 2) << 1) | (t % 2);
                parityCounts[p]++;
                int sum = (h + t) % 10;
                sumCounts[sum]++;
            }

            // 2. Tính thế năng tái chuẩn hóa
            foreach (var s in pool)
            {
                int h = s / 10;
                int t = s % 10;
                int p = ((h % 2) << 1) | (t % 2);
                int sum = (h + t) % 10;

                // Năng lượng ban đầu
                double vBase = initialV[s];

                // Hiệu ứng nén không gian: Các đầu/đuôi có mật độ tập trung cao trong pool được thưởng năng lượng
                double headConcentration = (double)headCounts[h] / pool.Count;
                double tailConcentration = (double)tailCounts[t] / pool.Count;

                // Thưởng cho các số nằm trong vùng đối xứng đậm đặc của không gian con
                double subDensityBonus = -0.4 * (headConcentration + tailConcentration);

                // Phạt nếu số nằm lẻ loi đơn độc trong không gian con (Isolated outlier)
                if (headCounts[h] == 1) subDensityBonus += 0.5;
                if (tailCounts[t] == 1) subDensityBonus += 0.5;

                dict[s] = vBase + subDensityBonus;
            }

            return dict;
        }

        private static double[] ComputeTids2DPotentials(List<Record> past, string currentDow)
        {
            int n = past.Count;
            double[] v = new double[100];

            // 1. Gan của 100 số
            int[] gan = new int[100];
            for (int s = 0; s < 100; s++)
            {
                for (int i = n - 1; i >= 0; i--)
                {
                    if (past[i].State2D == s) break;
                    gan[s]++;
                }
            }

            // 2. Tần suất 30 kỳ F30
            int[] f30 = new int[100];
            for (int i = Math.Max(0, n - 30); i < n; i++) f30[past[i].State2D]++;

            // 3. Lô rơi hôm qua sang Đề
            var lastUniqueLo = past[n - 1].UniqueLo;

            for (int s = 0; s < 100; s++)
            {
                int h = s / 10;
                int t = s % 10;

                // Thế năng Gan (vùng hồi quy 6 -> 22)
                double vGan = 0.0;
                if (gan[s] == 0) vGan = 3.5; // Cản lặp
                else if (gan[s] >= 5 && gan[s] <= 20) vGan = -1.8;
                else if (gan[s] > 40) vGan = 1.0;

                // Thế năng F30
                double vF30 = -0.35 * (f30[s] - 0.30);

                // Thế năng Lô rơi sang Đề (cascade)
                double vCascade = (lastUniqueLo != null && lastUniqueLo.Contains(s)) ? -0.85 : 0.0;

                v[s] = vGan + vF30 + vCascade;
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
                    rec.D2 = gdb[3] - '0';
                    rec.D1 = gdb[4] - '0';
                    rec.State2D = rec.D2 * 10 + rec.D1;

                    // Lô 27 giải
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
