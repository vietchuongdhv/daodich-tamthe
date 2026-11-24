using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TidsDirectSpace
{
    public class SimpleRecord
    {
        public int Id;
        public string Date;
        public string DayOfWeek;
        public string Gdb;
        public int[] Digits5; // [d5, d4, d3, d2, d1]
        public int State3D;   // d3*100 + d2*10 + d1 (0 -> 999)
        public int State4D;   // d4*1000 + d3*100 + d2*10 + d1 (0 -> 9999)
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            string csvPath = @"data\lottery_master.csv";
            var records = LoadData(csvPath);
            int n = records.Count;
            Console.WriteLine("Loaded {0} records for Direct 3D/4D Space Analysis.", n);

            // Kiểm thử Walk-Forward OOS trên 200 kỳ gần nhất
            int testPeriods = 200;
            int startK = n - testPeriods;

            Console.WriteLine("=========================================================================");
            Console.WriteLine("   MÔ HÌNH TOÁN HỌC TRƯỜNG THẾ NĂNG TRỰC TIẾP TRÊN 1.000 SỐ 3D & 10.000 SỐ 4D");
            Console.WriteLine("   (TIDS QUANTUM DIRECT TENSOR MANIFOLD - HOÀN TOÀN ĐỘC LẬP 2D)        ");
            Console.WriteLine("   Số kỳ kiểm thử OOS: {0} kỳ gần nhất (từ {1} đến {2})", testPeriods, records[startK].Date, records[n-1].Date);
            Console.WriteLine("=========================================================================");

            // Bộ đếm trúng cho không gian 1.000 số 3D
            int hit3D_Top20 = 0;   // Dàn 20 số (2% không gian)
            int hit3D_Top50 = 0;   // Dàn 50 số (5% không gian)
            int hit3D_Top100 = 0;  // Dàn 100 số (10% không gian)
            int hit3D_Top150 = 0;  // Dàn 150 số (15% không gian)
            int hit3D_Top250 = 0;  // Dàn 250 số (25% không gian)

            // Bộ đếm trúng cho không gian 10.000 số 4D
            int hit4D_Top20 = 0;   // Dàn 20 số (0.2% không gian)
            int hit4D_Top50 = 0;   // Dàn 50 số (0.5% không gian)
            int hit4D_Top100 = 0;  // Dàn 100 số (1.0% không gian)
            int hit4D_Top200 = 0;  // Dàn 200 số (2.0% không gian)

            // Tiền thưởng giả định
            long winReward3D = 960000;  // 1:960 cho cược 1k
            long winReward4D = 8880000; // 1:8880 cho cược 1k

            for (int k = startK; k < n; k++)
            {
                var past = records.Take(k).ToList();
                var actual = records[k];
                int act3D = actual.State3D;
                int act4D = actual.State4D;

                // =====================================================================
                // PHẦN 1: MÔ HÌNH TRƯỜNG THẾ NĂNG TRỰC TIẾP TRÊN 1.000 SỐ 3D
                // =====================================================================
                // 1.1 Thế năng biên 1D độc lập cho 3 trục: d3, d2, d1
                double[] vMargD3 = ComputeMarginal1D(past, 2); // Trục hàng trăm
                double[] vMargD2 = ComputeMarginal1D(past, 3); // Trục hàng chục
                double[] vMargD1 = ComputeMarginal1D(past, 4); // Trục hàng đơn vị

                // 1.2 Ma trận ghép cặp tương tác lân cận (2-point coupling): W32 và W21
                double[,] w32 = ComputeCoupling2D(past, 2, 3);
                double[,] w21 = ComputeCoupling2D(past, 3, 4);

                // 1.3 Phân cực vĩ mô 8 góc phần tám (3-Bit Parity Cube: CCC, CCL, CLC...)
                double[] vParity8 = ComputeParity8Potential(past);

                // 1.4 Thế năng Tổng 3 số (Sum10: 0 -> 9)
                double[] vSum3D = ComputeSum10Potential(past);

                // 1.5 Gan trực tiếp của 1.000 trạng thái 3D
                int[] gan3D = new int[1000];
                for (int s = 0; s < 1000; s++)
                {
                    for (int j = k - 1; j >= 0; j--)
                    {
                        if (past[j].State3D == s) break;
                        gan3D[s]++;
                    }
                }

                // 1.6 Tần suất 300 kỳ của 1.000 trạng thái 3D
                int[] f300_3D = new int[1000];
                for (int j = Math.Max(0, k - 300); j < k; j++) f300_3D[past[j].State3D]++;

                // Tổng hợp trường thế năng hiệu dụng V_eff trên từng trạng thái trong 1.000 số:
                var energies3D = new List<Tuple<int, double>>(1000);
                for (int s = 0; s < 1000; s++)
                {
                    int d3 = s / 100;
                    int d2 = (s / 10) % 10;
                    int d1 = s % 10;

                    // Biên 1D
                    double v1D = vMargD3[d3] + vMargD2[d2] + vMargD1[d1];

                    // Tương tác 2 hạt
                    double vCoupling = w32[d3, d2] + w21[d2, d1];

                    // Parity 8 góc
                    int pIdx = ((d3 % 2) << 2) | ((d2 % 2) << 1) | (d1 % 2);
                    double vP8 = vParity8[pIdx];

                    // Tổng 3 số
                    int sum3 = (d3 + d2 + d1) % 10;
                    double vS = vSum3D[sum3];

                    // Gan trực tiếp 3D
                    double rTerm3D = 0.0;
                    if (gan3D[s] == 0) rTerm3D = 2.0; // cản lặp
                    else if (gan3D[s] >= 150 && gan3D[s] <= 450) rTerm3D = -1.5; // Vùng hồi quy 3D
                    else if (gan3D[s] > 1200) rTerm3D = 0.8; // Cực gan 3D

                    double kTerm3D = -0.4 * (f300_3D[s] - 0.30);

                    double totalV = 0.5 * v1D + 0.6 * vCoupling + 0.8 * vP8 + 0.7 * vS + rTerm3D + kTerm3D;
                    energies3D.Add(new Tuple<int, double>(s, totalV));
                }

                // Sắp xếp tìm đáy thế năng 1.000 số
                var sorted3D = energies3D.OrderBy(x => x.Item2).Select(x => x.Item1).ToArray();

                var set20 = new HashSet<int>(sorted3D.Take(20));
                var set50 = new HashSet<int>(sorted3D.Take(50));
                var set100 = new HashSet<int>(sorted3D.Take(100));
                var set150 = new HashSet<int>(sorted3D.Take(150));
                var set250 = new HashSet<int>(sorted3D.Take(250));

                if (set20.Contains(act3D)) hit3D_Top20++;
                if (set50.Contains(act3D)) hit3D_Top50++;
                if (set100.Contains(act3D)) hit3D_Top100++;
                if (set150.Contains(act3D)) hit3D_Top150++;
                if (set250.Contains(act3D)) hit3D_Top250++;

                // =====================================================================
                // PHẦN 2: MÔ HÌNH TRƯỜNG THẾ NĂNG TRỰC TIẾP TRÊN 10.000 SỐ 4D
                // =====================================================================
                // Phân rã Tensor 4 chiều: (d4, d3, d2, d1)
                // d4 (nghìn), d3 (trăm), d2 (chục), d1 (đơn vị)
                double[] vMargD4 = ComputeMarginal1D(past, 1);
                double[,] w43 = ComputeCoupling2D(past, 1, 2);

                // Lọc Top 20 Cặp đầu (d4, d3) và Top 50 Cặp đầu
                var head4DPotentials = new List<Tuple<int, double>>(100);
                for (int h = 0; h < 100; h++)
                {
                    int d4 = h / 10;
                    int d3 = h % 10;
                    double vHead = vMargD4[d4] + vMargD3[d3] + w43[d4, d3];
                    head4DPotentials.Add(new Tuple<int, double>(h, vHead));
                }
                var topHeads = head4DPotentials.OrderBy(x => x.Item2).Select(x => x.Item1).ToArray();

                // Lọc Top Đuôi Đề (d2, d1) từ trường thế năng 2D độc lập
                var tail2DPotentials = new List<Tuple<int, double>>(100);
                for (int t = 0; t < 100; t++)
                {
                    int d2 = t / 10;
                    int d1 = t % 10;
                    double vTail = vMargD2[d2] + vMargD1[d1] + w21[d2, d1];
                    tail2DPotentials.Add(new Tuple<int, double>(t, vTail));
                }
                var topTails = tail2DPotentials.OrderBy(x => x.Item2).Select(x => x.Item1).ToArray();

                // Dàn 4D trực tiếp theo cắt lớp thế năng đa chiều:
                // Dàn 20 số 4D: 4 đầu x 5 đuôi
                var dan4D_20 = new HashSet<int>();
                for (int h = 0; h < 4; h++)
                    for (int t = 0; t < 5; t++)
                        dan4D_20.Add(topHeads[h] * 100 + topTails[t]);

                // Dàn 50 số 4D: 5 đầu x 10 đuôi
                var dan4D_50 = new HashSet<int>();
                for (int h = 0; h < 5; h++)
                    for (int t = 0; t < 10; t++)
                        dan4D_50.Add(topHeads[h] * 100 + topTails[t]);

                // Dàn 100 số 4D: 10 đầu x 10 đuôi (hoặc 5 đầu x 20 đuôi)
                var dan4D_100 = new HashSet<int>();
                for (int h = 0; h < 10; h++)
                    for (int t = 0; t < 10; t++)
                        dan4D_100.Add(topHeads[h] * 100 + topTails[t]);

                // Dàn 200 số 4D: 10 đầu x 20 đuôi
                var dan4D_200 = new HashSet<int>();
                for (int h = 0; h < 10; h++)
                    for (int t = 0; t < 20; t++)
                        dan4D_200.Add(topHeads[h] * 100 + topTails[t]);

                if (dan4D_20.Contains(act4D)) hit4D_Top20++;
                if (dan4D_50.Contains(act4D)) hit4D_Top50++;
                if (dan4D_100.Contains(act4D)) hit4D_Top100++;
                if (dan4D_200.Contains(act4D)) hit4D_Top200++;
            }

            Console.WriteLine("\n=========================================================================");
            Console.WriteLine("    KẾT QUẢ ĐỐI SOÁT OOS TRÊN 1.000 SỐ 3D (ĐỘC LẬP TRỰC TIẾP TIDS)       ");
            Console.WriteLine("=========================================================================");
            Console.WriteLine("Quy mô Dàn 3D         | Số Kỳ Trúng / 200 | Tỷ Lệ Thực Tế | Ngẫu Nhiên | Hiệu Suất / ROI Dự Kiến");
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            PrintRow3D("Dàn 3D 20 số (2%)", hit3D_Top20, testPeriods, 0.02, 20, winReward3D);
            PrintRow3D("Dàn 3D 50 số (5%)", hit3D_Top50, testPeriods, 0.05, 50, winReward3D);
            PrintRow3D("Dàn 3D 100 số (10%)", hit3D_Top100, testPeriods, 0.10, 100, winReward3D);
            PrintRow3D("Dàn 3D 150 số (15%)", hit3D_Top150, testPeriods, 0.15, 150, winReward3D);
            PrintRow3D("Dàn 3D 250 số (25%)", hit3D_Top250, testPeriods, 0.25, 250, winReward3D);
            Console.WriteLine("=========================================================================================");

            Console.WriteLine("\n=========================================================================");
            Console.WriteLine("   KẾT QUẢ ĐỐI SOÁT OOS TRÊN 10.000 SỐ 4D (ĐỘC LẬP TRỰC TIẾP TIDS)      ");
            Console.WriteLine("=========================================================================");
            Console.WriteLine("Quy mô Dàn 4D         | Số Kỳ Trúng / 200 | Tỷ Lệ Thực Tế | Ngẫu Nhiên | Doanh Thu / Chi Phí");
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            PrintRow4D("Dàn 4D 20 số (0.2%)", hit4D_Top20, testPeriods, 0.002, 20, winReward4D);
            PrintRow4D("Dàn 4D 50 số (0.5%)", hit4D_Top50, testPeriods, 0.005, 50, winReward4D);
            PrintRow4D("Dàn 4D 100 số (1.0%)", hit4D_Top100, testPeriods, 0.010, 100, winReward4D);
            PrintRow4D("Dàn 4D 200 số (2.0%)", hit4D_Top200, testPeriods, 0.020, 200, winReward4D);
            Console.WriteLine("=========================================================================================");
        }

        private static void PrintRow3D(string name, int hits, int total, double randProb, int costPerDay, long reward)
        {
            double actRate = (double)hits / total * 100.0;
            double randRate = randProb * 100.0;
            long totalCost = (long)costPerDay * 1000 * total;
            long totalReward = (long)hits * reward;
            long pnl = totalReward - totalCost;
            double roi = totalCost > 0 ? ((double)pnl / totalCost * 100.0) : 0.0;
            string roiStr = pnl >= 0 ? string.Format("+{0:F1}% (LÃI +{1:N0}k)", roi, pnl/1000) : string.Format("{0:F1}% ({1:N0}k)", roi, pnl/1000);
            Console.WriteLine("{0,-21} | {1,3} kỳ / {2,-3}     | {3,6:F2}%      | {4,5:F2}%     | {5}",
                name, hits, total, actRate, randRate, roiStr);
        }

        private static void PrintRow4D(string name, int hits, int total, double randProb, int costPerDay, long reward)
        {
            double actRate = (double)hits / total * 100.0;
            double randRate = randProb * 100.0;
            long totalCost = (long)costPerDay * 1000 * total;
            long totalReward = (long)hits * reward;
            long pnl = totalReward - totalCost;
            string pnlStr = pnl >= 0 ? string.Format("LÃI +{0:N0}k (Ăn {1} lần)", pnl/1000, hits) : string.Format("Chi {0:N0}k / Thu {1:N0}k", totalCost/1000, totalReward/1000);
            Console.WriteLine("{0,-21} | {1,3} kỳ / {2,-3}     | {3,6:F2}%      | {4,5:F2}%     | {5}",
                name, hits, total, actRate, randRate, pnlStr);
        }

        // Tính thế năng biên 1D
        private static double[] ComputeMarginal1D(List<SimpleRecord> past, int digitIdx)
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

            double[] v = new double[10];
            for (int d = 0; d < 10; d++)
            {
                double r = 0.0;
                if (gan[d] == 0) r = 1.0;
                else if (gan[d] >= 6 && gan[d] <= 18) r = -1.5;
                else if (gan[d] > 25) r = 0.5;

                double k = -0.3 * (f30[d] - 3.0);
                v[d] = r + k;
            }
            return v;
        }

        // Tính ma trận ghép cặp tương tác lân cận 2 hạt W_ij
        private static double[,] ComputeCoupling2D(List<SimpleRecord> past, int idx1, int idx2)
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
                    double exp = 1.0; // kỳ vọng 1.0 trong 100 ngày
                    w[i, j] = -0.4 * (f100[i, j] - exp);
                }
            }
            return w;
        }

        // Tính thế năng 8 góc phần tám Parity 3D
        private static double[] ComputeParity8Potential(List<SimpleRecord> past)
        {
            int n = past.Count;
            int[] f50 = new int[8];
            int start = Math.Max(0, n - 50);
            for (int i = start; i < n; i++)
            {
                int p = ((past[i].Digits5[2] % 2) << 2) | ((past[i].Digits5[3] % 2) << 1) | (past[i].Digits5[4] % 2);
                f50[p]++;
            }

            int[] gan = new int[8];
            for (int p = 0; p < 8; p++)
            {
                for (int i = n - 1; i >= 0; i--)
                {
                    int pI = ((past[i].Digits5[2] % 2) << 2) | ((past[i].Digits5[3] % 2) << 1) | (past[i].Digits5[4] % 2);
                    if (pI == p) break;
                    gan[p]++;
                }
            }

            double[] v = new double[8];
            for (int p = 0; p < 8; p++)
            {
                double r = (gan[p] >= 8 && gan[p] <= 20) ? -1.8 : (gan[p] == 0 ? 1.2 : 0.0);
                double k = -0.3 * (f50[p] - (50.0 / 8.0));
                v[p] = r + k;
            }
            return v;
        }

        // Tính thế năng Tổng 3D
        private static double[] ComputeSum10Potential(List<SimpleRecord> past)
        {
            int n = past.Count;
            int[] f50 = new int[10];
            int start = Math.Max(0, n - 50);
            for (int i = start; i < n; i++)
            {
                int s = (past[i].Digits5[2] + past[i].Digits5[3] + past[i].Digits5[4]) % 10;
                f50[s]++;
            }

            int[] gan = new int[10];
            for (int s = 0; s < 10; s++)
            {
                for (int i = n - 1; i >= 0; i--)
                {
                    int sI = (past[i].Digits5[2] + past[i].Digits5[3] + past[i].Digits5[4]) % 10;
                    if (sI == s) break;
                    gan[s]++;
                }
            }

            double[] v = new double[10];
            for (int s = 0; s < 10; s++)
            {
                double r = (gan[s] >= 7 && gan[s] <= 20) ? -1.5 : (gan[s] == 0 ? 1.0 : 0.0);
                double k = -0.3 * (f50[s] - 5.0);
                v[s] = r + k;
            }
            return v;
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

                    string gdbClean = rec.Gdb.PadLeft(5, '0');
                    if (gdbClean.Length > 5) gdbClean = gdbClean.Substring(gdbClean.Length - 5);
                    rec.Digits5 = new int[5];
                    for (int d = 0; d < 5; d++) rec.Digits5[d] = gdbClean[d] - '0';

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
