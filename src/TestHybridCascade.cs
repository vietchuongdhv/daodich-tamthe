using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TidsHybridCascade
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
            int testPeriods = 300;
            int startK = n - testPeriods;

            Console.WriteLine("=========================================================================================");
            Console.WriteLine("   KHẢO SÁT MÔ HÌNH LAI: RÚT GỌN ĐỆ QUY 65% KẾT HỢP GIAO THOA CỘNG HƯỞNG KÉP (DRI)");
            Console.WriteLine("   Mẫu kiểm thử: {0} kỳ gần nhất (từ {1} đến {2})", testPeriods, records[startK].Date, records[n - 1].Date);
            Console.WriteLine("=========================================================================================\n");

            int hitLvl1_65 = 0; // 65 số
            int hitLvl2_42 = 0; // 42 số
            int hitLvl3_27 = 0; // 27 số
            int hitLvl4_18 = 0; // 18 số
            int hitLvl5_12 = 0; // 12 số
            int hitTuThu_4 = 0; // Tứ thủ (Giao thoa DRI từ 18 số)
            int hitSongThu_2 = 0; // Song thủ (Giao thoa DRI)
            int hitBachThu_1 = 0; // Bạch thủ (Giao thoa DRI)

            long reward = 99000;

            for (int k = startK; k < n; k++)
            {
                var past = records.Take(k).ToList();
                var actual = records[k];
                int actDe = actual.De;

                var marginal = ComputeMarginal1D(past, actual.DayOfWeek);
                var bsmEnergies = ComputeBsmBasket(past, marginal);

                // Tầng 1: 65 số
                var dan65 = SelectFactorNeutralDeSet(bsmEnergies, 65);
                if (dan65.Contains(actDe)) hitLvl1_65++;

                // Tầng 2: 42 số (65% của 65)
                var sub42 = bsmEnergies.Where(e => dan65.Contains(e.State)).ToList();
                var dan42 = SelectFactorNeutralDeSet(sub42, 42);
                if (dan42.Contains(actDe)) hitLvl2_42++;

                // Tầng 3: 27 số (65% của 42)
                var sub27 = bsmEnergies.Where(e => dan42.Contains(e.State)).ToList();
                var dan27 = SelectFactorNeutralDeSet(sub27, 27);
                if (dan27.Contains(actDe)) hitLvl3_27++;

                // Tầng 4: 18 số (65% của 27)
                var sub18 = bsmEnergies.Where(e => dan27.Contains(e.State)).ToList();
                var dan18 = SelectFactorNeutralDeSet(sub18, 18);
                if (dan18.Contains(actDe)) hitLvl4_18++;

                // Tầng 5: 12 số (65% của 18)
                var sub12 = bsmEnergies.Where(e => dan18.Contains(e.State)).ToList();
                var dan12 = SelectFactorNeutralDeSet(sub12, 12);
                if (dan12.Contains(actDe)) hitLvl5_12++;

                // RÚT GỌN VỀ TINH TUYỂN BẰNG GIAO THOA CỘNG HƯỞNG KÉP (DRI):
                // Tính DRI cho các số nằm trong Dàn 18 số
                var driScores = new List<Tuple<int, double>>();
                var rank18 = dan18.Select((s, idx) => new { State = s, Rank = idx }).ToDictionary(x => x.State, x => x.Rank);

                foreach (var s in dan18)
                {
                    int h = s / 10;
                    int u = s % 10;
                    double v1D = marginal.TensPotentials[h] + marginal.UnitsPotentials[u];
                    double driScore = rank18[s] * 1.0 + v1D * 3.0;
                    driScores.Add(new Tuple<int, double>(s, driScore));
                }

                var sortedDRI = driScores.OrderBy(x => x.Item2).Select(x => x.Item1).ToArray();

                if (sortedDRI.Take(4).Contains(actDe)) hitTuThu_4++;
                if (sortedDRI.Take(2).Contains(actDe)) hitSongThu_2++;
                if (sortedDRI[0] == actDe) hitBachThu_1++;
            }

            Console.WriteLine("=========================================================================================");
            Console.WriteLine("   BẢNG TỔNG HỢP HIỆU SUẤT MÔ HÌNH RÚT GỌN 65% + DRI CỘNG HƯỞNG (300 KỲ OOS)");
            Console.WriteLine("=========================================================================================");
            Console.WriteLine("Tầng Rút Gọn           | Quy Mô | Số Kỳ Trúng | Tỷ Lệ Thực | Ngẫu Nhiên | Hiệu Suất ROI %");
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            PrintRow("Tầng 1: Huyền Thiên Giáp", 65, hitLvl1_65, testPeriods, 0.65, reward);
            PrintRow("Tầng 2: Cắt lọc Cấp 2  ", 42, hitLvl2_42, testPeriods, 0.42, reward);
            PrintRow("Tầng 3: Dàn Hà Đồ 27s   ", 27, hitLvl3_27, testPeriods, 0.27, reward);
            PrintRow("Tầng 4: Dàn Xung Kích 18s", 18, hitLvl4_18, testPeriods, 0.18, reward);
            PrintRow("Tầng 5: Dàn Cốt Lõi 12s  ", 12, hitLvl5_12, testPeriods, 0.12, reward);
            Console.WriteLine("-----------------------------------------------------------------------------------------");
            PrintRow("Tứ Thủ Đề (DRI Top 4)   ", 4, hitTuThu_4, testPeriods, 0.04, reward);
            PrintRow("Song Thủ Đề (DRI Top 2) ", 2, hitSongThu_2, testPeriods, 0.02, reward);
            PrintRow("Bạch Thủ Đề (DRI Top 1) ", 1, hitBachThu_1, testPeriods, 0.01, reward);
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

            Console.WriteLine("{0,-24} | {1,2} số | {2,3} / {3,-3} | {4,6:F2}%   | {5,5:F2}%    | {6}",
                name, size, hits, total, actRate, randRate, roiStr);
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
