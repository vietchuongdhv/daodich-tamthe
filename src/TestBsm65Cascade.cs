using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TidsBsmCascade
{
    public class LotteryRecord
    {
        public int Id;
        public string Date;
        public string DayOfWeek;
        public int De;
        public List<int> UniqueLo;
    }

    public class Marginal1DResult
    {
        public double[] TensPotentials = new double[10];
        public double[] UnitsPotentials = new double[10];
        public double[] SumPotentials = new double[10];
        public double[] ParityPotentials = new double[4];
        public double[] SizePotentials = new double[2];
    }

    public class StateEnergy
    {
        public int State;
        public double Potential;
        public int Gan;
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            string csvPath = @"data\lottery_master.csv";
            var records = LoadData(csvPath);
            int n = records.Count;

            // Kiểm thử trên 300 kỳ OOS gần nhất
            int testPeriods = 300;
            int startK = n - testPeriods;

            Console.WriteLine("=========================================================================================");
            Console.WriteLine("   KHẢO SÁT CHUYÊN SÂU NGUYÊN TẮC RÚT GỌN ĐỆ QUY 65% THEO BSM FACTOR-NEUTRAL");
            Console.WriteLine("   Mẫu thử nghiệm OOS: {0} kỳ gần nhất (từ {1} đến {2})", testPeriods, records[startK].Date, records[n - 1].Date);
            Console.WriteLine("=========================================================================================\n");

            // Chuỗi kích thước dàn theo lũy thừa tỷ lệ 65%:
            int[] sizes = new int[] { 65, 42, 27, 18, 12, 8, 5, 3, 2, 1 };
            int numLevels = sizes.Length;

            int[] hitsDirect = new int[numLevels];
            int[] hitsRecursive = new int[numLevels];

            // Ma trận đếm chuyển tiếp giữa các tầng để đo tỷ lệ bảo toàn
            int[,] retainMatrix = new int[numLevels, numLevels];

            for (int k = startK; k < n; k++)
            {
                var past = records.Take(k).ToList();
                var actual = records[k];
                int actDe = actual.De;

                var marginal = ComputeMarginal1D(past, actual.DayOfWeek);
                var bsmEnergies = ComputeBsmBasket(past, marginal);

                // -------------------------------------------------------------
                // 1. CẮT LỚP TĨNH (DIRECT BSM FACTOR-NEUTRAL)
                // -------------------------------------------------------------
                for (int lvl = 0; lvl < numLevels; lvl++)
                {
                    int sz = sizes[lvl];
                    var setDirect = SelectFactorNeutralDeSet(bsmEnergies, sz);
                    if (setDirect.Contains(actDe)) hitsDirect[lvl]++;
                }

                // -------------------------------------------------------------
                // 2. TÁI CHUẨN HÓA ĐỆ QUY 65% (RECURSIVE SUBSPACE DECIMATION)
                // -------------------------------------------------------------
                // Tầng 1: Chọn 65 số từ 100 số
                var currentEnergies = new List<StateEnergy>(bsmEnergies);
                var currentDan = SelectFactorNeutralDeSet(currentEnergies, 65);

                bool prevHit = currentDan.Contains(actDe);
                if (prevHit) hitsRecursive[0]++;

                for (int lvl = 1; lvl < numLevels; lvl++)
                {
                    int targetSize = sizes[lvl];

                    // Lọc tập energies chỉ còn các số trong currentDan
                    var subEnergies = currentEnergies.Where(e => currentDan.Contains(e.State)).ToList();

                    // TÁI TÍNH TOÁN LẠI NĂNG LƯỢNG TRONG KHÔNG GIAN CON:
                    // Trong không gian con, mật độ các đầu thay đổi.
                    // Áp dụng tái cân bằng đầu trục Factor-Neutral trên không gian con
                    var nextDan = SelectFactorNeutralDeSet(subEnergies, targetSize);

                    bool curHit = nextDan.Contains(actDe);
                    if (curHit) hitsRecursive[lvl]++;

                    // Cập nhật currentDan cho tầng tiếp theo
                    currentDan = nextDan;
                }
            }

            Console.WriteLine("===================================================================================================================");
            Console.WriteLine("    BẢNG KẾT QUẢ ĐỐI SOÁT CÁC TẦNG RÚT GỌN 65% (TỪ DÀN 65 SỐ VỀ BẠCH THỦ TRÊN {0} KỲ)", testPeriods);
            Console.WriteLine("===================================================================================================================");
            Console.WriteLine("Tầng | Quy mô | Ngẫu nhiên | CẮT LỚP TRỰC TIẾP (Direct)       | RÚT GỌN ĐỆ QUY (Recursive)         | So sánh");
            Console.WriteLine("     | dàn    | xác suất   | Số kỳ trúng | Tỷ lệ %   | ROI %  | Số kỳ trúng | Tỷ lệ %   | ROI %    | Hiệu quả");
            Console.WriteLine("-------------------------------------------------------------------------------------------------------------------");

            long reward = 99000;
            for (int lvl = 0; lvl < numLevels; lvl++)
            {
                int sz = sizes[lvl];
                double randProb = (double)sz / 100.0 * 100.0;

                int hDir = hitsDirect[lvl];
                double rateDir = (double)hDir / testPeriods * 100.0;
                long costDir = (long)sz * 1000 * testPeriods;
                long winDir = (long)hDir * reward;
                double roiDir = ((double)(winDir - costDir) / costDir) * 100.0;

                int hRec = hitsRecursive[lvl];
                double rateRec = (double)hRec / testPeriods * 100.0;
                long costRec = (long)sz * 1000 * testPeriods;
                long winRec = (long)hRec * reward;
                double roiRec = ((double)(winRec - costRec) / costRec) * 100.0;

                string roiDirStr = roiDir >= 0 ? string.Format("+{0:F1}%", roiDir) : string.Format("{0:F1}%", roiDir);
                string roiRecStr = roiRec >= 0 ? string.Format("+{0:F1}%", roiRec) : string.Format("{0:F1}%", roiRec);

                string verdict = hRec > hDir ? string.Format("🟢 Đệ quy hơn (+{0})", hRec - hDir) : 
                                (hRec == hDir ? "⚪ Ngang nhau" : string.Format("🔴 Trực tiếp hơn (+{0})", hDir - hRec));

                Console.WriteLine("Lvl {0,2} | {1,2} số  | {2,6:F2}%   | {3,4} / {4,-3}   | {5,6:F2}% | {6,7} | {7,4} / {8,-3}   | {9,6:F2}% | {10,8} | {11}",
                    lvl + 1, sz, randProb,
                    hDir, testPeriods, rateDir, roiDirStr,
                    hRec, testPeriods, rateRec, roiRecStr,
                    verdict);
            }
            Console.WriteLine("===================================================================================================================\n");

            Console.WriteLine("--- TỶ LỆ SỐNG SÓT CỦA CON SỐ TRÚNG KHI BƯỚC QUA TỪNG TẦNG CẮT 35% ---");
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            for (int lvl = 0; lvl < numLevels - 1; lvl++)
            {
                double retain = (double)hitsRecursive[lvl + 1] / hitsRecursive[lvl] * 100.0;
                double expectedRetain = (double)sizes[lvl + 1] / sizes[lvl] * 100.0;
                Console.WriteLine("Từ Lvl {0,2} ({1,2} số) -> Lvl {2,2} ({3,2} số): Tỷ lệ giữ chân = {4,5:F1}% (Lý thuyết ngẫu nhiên: {5,4:F1}%) -> Vượt trội {6,5:F1}%",
                    lvl + 1, sizes[lvl], lvl + 2, sizes[lvl + 1], retain, expectedRetain, retain - expectedRetain);
            }
            Console.WriteLine("=========================================================================================");
        }

        private static List<int> SelectFactorNeutralDeSet(List<StateEnergy> energies, int count)
        {
            var selected = new List<int>();
            var headCounts = new Dictionary<int, int>();
            int maxPerHead = (count / 10) + 1;

            foreach (var e in energies)
            {
                int head = e.State / 10;
                int hCount = 0;
                headCounts.TryGetValue(head, out hCount);
                if (hCount < maxPerHead)
                {
                    selected.Add(e.State);
                    headCounts[head] = hCount + 1;
                    if (selected.Count == count) break;
                }
            }

            if (selected.Count < count)
            {
                foreach (var e in energies)
                {
                    if (!selected.Contains(e.State))
                    {
                        selected.Add(e.State);
                        if (selected.Count == count) break;
                    }
                }
            }
            return selected;
        }

        private static List<StateEnergy> ComputeBsmBasket(List<LotteryRecord> records, Marginal1DResult marginal1D)
        {
            int n = records.Count;
            int lastDe = records[n - 1].De;
            var lastUniqueLo = records[n - 1].UniqueLo;

            int[] gan = new int[100];
            for (int s = 0; s < 100; s++)
            {
                for (int i = n - 1; i >= 0; i--)
                {
                    if (records[i].De == s) break;
                    gan[s]++;
                }
            }

            int[] f100De = new int[100];
            for (int i = Math.Max(0, n - 100); i < n; i++) f100De[records[i].De]++;

            int[] f30Lo = new int[100];
            for (int i = Math.Max(0, n - 30); i < n; i++)
            {
                foreach (int u in records[i].UniqueLo) f30Lo[u]++;
            }

            var list = new List<StateEnergy>();
            for (int s = 0; s < 100; s++)
            {
                double vRenewal = 0.0;
                if (gan[s] == 0) vRenewal = 2.5;
                else if (gan[s] >= 1 && gan[s] <= 79) vRenewal = -0.008 * gan[s];
                else if (gan[s] >= 80 && gan[s] <= 220) vRenewal = -0.64 - 0.003 * (gan[s] - 80);
                else if (gan[s] > 300) vRenewal = 1.50;

                double vKinetic = -0.15 * (f100De[s] - 1.00) - 0.05 * (f30Lo[s] - 8.10);
                double vCascade = lastUniqueLo.Contains(s) ? -1.0 : 0.0;

                int head = s / 10;
                int unit = s % 10;
                int sum = (head + unit) % 10;
                int p = (head % 2) * 2 + (unit % 2);
                int sz = (s >= 50) ? 1 : 0;

                double v1D = (marginal1D.TensPotentials[head] + marginal1D.UnitsPotentials[unit] + 0.8 * marginal1D.SumPotentials[sum]) / 2.8;

                double vUnified = vRenewal + vKinetic + vCascade + 0.12 * v1D + 0.08 * marginal1D.ParityPotentials[p] + 0.04 * marginal1D.SizePotentials[sz];
                list.Add(new StateEnergy { State = s, Potential = vUnified, Gan = gan[s] });
            }

            return list.OrderBy(x => x.Potential).ToList();
        }

        private static Marginal1DResult ComputeMarginal1D(List<LotteryRecord> records, string currentDow)
        {
            int n = records.Count;
            var res = new Marginal1DResult();

            int[] ganTens = new int[10];
            int[] ganUnits = new int[10];
            for (int d = 0; d < 10; d++)
            {
                for (int i = n - 1; i >= 0; i--) { if (records[i].De / 10 == d) break; ganTens[d]++; }
                for (int i = n - 1; i >= 0; i--) { if (records[i].De % 10 == d) break; ganUnits[d]++; }
            }

            for (int d = 0; d < 10; d++)
            {
                res.TensPotentials[d] = (ganTens[d] == 0) ? 2.0 : ((ganTens[d] >= 5 && ganTens[d] <= 15) ? -1.5 : 0.0);
                res.UnitsPotentials[d] = (ganUnits[d] == 0) ? 2.0 : ((ganUnits[d] >= 5 && ganUnits[d] <= 15) ? -1.5 : 0.0);
                res.SumPotentials[d] = 0.0;
            }

            return res;
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
