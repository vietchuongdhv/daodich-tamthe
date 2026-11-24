using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace TidsTheory
{
    public class TidsWebServer
    {
        private static HttpListener listener;
        private static string csvFilePath;
        private static List<LotteryRecord> records;
        private static bool isRunning = true;
        private static int serverPort = 5050;
        private static object dataLock = new object();

        public static void Start(string[] args, string path)
        {
            csvFilePath = path;
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("=========================================================================================");
            Console.WriteLine("     ĐẠO DỊCH HUYỀN CƠ PHÁP - WEB APP SERVER ĐỊNH LƯỢNG TẬP TRUNG (TIDS QUANTUM V2.0)    ");
            Console.WriteLine("=========================================================================================");

            if (!File.Exists(csvFilePath))
            {
                Console.WriteLine("[LỖI] Không tìm thấy file dữ liệu: " + csvFilePath);
                return;
            }

            lock (dataLock)
            {
                records = Program.LoadData(csvFilePath);
            }

            // Tự động kiểm tra và cập nhật CSDL mới nhất từ GitHub khi khởi động (chống mất dữ liệu khi Render ngủ đông)
            TrySyncFromGitHubOnStartup();

            Console.WriteLine(string.Format(">> Đã nạp thành công {0} bản ghi lịch sử (2002 -> nay).", records.Count));

            string envPort = Environment.GetEnvironmentVariable("PORT");
            if (!string.IsNullOrEmpty(envPort))
            {
                int ep;
                if (int.TryParse(envPort, out ep)) serverPort = ep;
            }
            else if (args.Length > 1)
            {
                int p;
                if (int.TryParse(args[1], out p)) serverPort = p;
            }

            try
            {
                listener = new HttpListener();
                // Trên Linux / Docker / Cloud: lắng nghe trên tất cả interface
                listener.Prefixes.Add("http://*:" + serverPort + "/");
                listener.Start();
            }
            catch
            {
                try
                {
                    listener = new HttpListener();
                    listener.Prefixes.Add("http://localhost:" + serverPort + "/");
                    listener.Prefixes.Add("http://127.0.0.1:" + serverPort + "/");
                    listener.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[CẢNH BÁO] Không thể mở cổng " + serverPort + ": " + ex.Message);
                    serverPort = 5055;
                    listener = new HttpListener();
                    listener.Prefixes.Add("http://localhost:" + serverPort + "/");
                    listener.Prefixes.Add("http://127.0.0.1:" + serverPort + "/");
                    listener.Start();
                }
            }

            string appUrl = "http://localhost:" + serverPort;
            Console.WriteLine(">> WEB APP ĐANG CHẠY TẠI: " + appUrl);
            Console.WriteLine(">> Bạn có thể mở trình duyệt máy tính hoặc điện thoại cùng mạng để sử dụng.");
            Console.WriteLine(">> Nhấn phím Ctrl + C để dừng máy chủ web.");
            Console.WriteLine("=========================================================================================");

            // Tự động mở trình duyệt nếu trên Windows
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = appUrl,
                    UseShellExecute = true
                });
            }
            catch { }

            while (isRunning)
            {
                try
                {
                    var context = listener.GetContext();
                    ThreadPool.QueueUserWorkItem(new WaitCallback(ProcessRequest), context);
                }
                catch (Exception)
                {
                    if (!isRunning) break;
                }
            }
        }

        private static void ProcessRequest(object state)
        {
            var context = (HttpListenerContext)state;
            var req = context.Request;
            var res = context.Response;

            // Thiết lập Headers CORS, UTF-8 và Chống Cache triệt để cho mọi thiết bị di động
            res.Headers.Add("Access-Control-Allow-Origin", "*");
            res.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            res.Headers.Add("Access-Control-Allow-Headers", "Content-Type");
            res.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate, max-age=0");
            res.Headers.Add("Pragma", "no-cache");
            res.Headers.Add("Expires", "0");
            res.ContentEncoding = Encoding.UTF8;

            if (req.HttpMethod == "OPTIONS")
            {
                res.StatusCode = 200;
                res.Close();
                return;
            }

            string rawUrl = req.RawUrl;
            string path = req.Url.AbsolutePath.ToLower();

            try
            {
                if (path == "/" || path == "/index.html")
                {
                    ServeStaticFile(res, "web/index.html", "text/html; charset=utf-8");
                }
                else if (path == "/manifest.json")
                {
                    ServeStaticFile(res, "web/manifest.json", "application/json; charset=utf-8");
                }
                else if (path == "/sw.js")
                {
                    ServeStaticFile(res, "web/sw.js", "application/javascript; charset=utf-8");
                }
                else if (path == "/api/status")
                {
                    HandleApiStatus(res);
                }
                else if (path == "/api/predict")
                {
                    long budget = 500000;
                    string bStr = req.QueryString["budget"];
                    if (!string.IsNullOrEmpty(bStr))
                    {
                        bStr = bStr.ToLower().Replace("k", "000").Replace("m", "000000").Replace("tr", "000000").Trim();
                        long b;
                        if (long.TryParse(bStr, out b))
                        {
                            if (b < 1000 && b > 0) b *= 1000;
                            budget = b;
                        }
                    }
                    HandleApiPredict(res, budget);
                }
                else if (path == "/api/history")
                {
                    int days = 15;
                    string dStr = req.QueryString["days"];
                    if (!string.IsNullOrEmpty(dStr)) int.TryParse(dStr, out days);
                    HandleApiHistory(res, days);
                }
                else if (path == "/api/parse-result" && req.HttpMethod == "POST")
                {
                    HandleApiParseResult(req, res);
                }
                else if (path == "/api/save-result" && req.HttpMethod == "POST")
                {
                    HandleApiSaveResult(req, res);
                }
                else
                {
                    // Thử tìm file tĩnh trong web/
                    string relPath = req.Url.AbsolutePath.TrimStart('/');
                    string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web", relPath);
                    if (!File.Exists(filePath))
                    {
                        filePath = Path.Combine(Directory.GetCurrentDirectory(), "web", relPath);
                    }
                    if (File.Exists(filePath))
                    {
                        string mime = "text/plain";
                        if (filePath.EndsWith(".html")) mime = "text/html";
                        else if (filePath.EndsWith(".js")) mime = "application/javascript";
                        else if (filePath.EndsWith(".css")) mime = "text/css";
                        else if (filePath.EndsWith(".json")) mime = "application/json";
                        else if (filePath.EndsWith(".png")) mime = "image/png";
                        ServeStaticFile(res, filePath, mime);
                    }
                    else
                    {
                        res.StatusCode = 404;
                        byte[] buf = Encoding.UTF8.GetBytes("{\"error\": \"Not Found\"}");
                        res.ContentType = "application/json";
                        res.OutputStream.Write(buf, 0, buf.Length);
                        res.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                try
                {
                    res.StatusCode = 500;
                    byte[] buf = Encoding.UTF8.GetBytes("{\"error\": \"" + EscapeJson(ex.Message) + "\"}");
                    res.ContentType = "application/json";
                    res.OutputStream.Write(buf, 0, buf.Length);
                    res.Close();
                }
                catch { }
            }
        }

        private static void ServeStaticFile(HttpListenerResponse res, string filePath, string contentType)
        {
            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), filePath);
            if (!File.Exists(fullPath))
            {
                fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filePath);
            }
            if (!File.Exists(fullPath))
            {
                res.StatusCode = 404;
                byte[] buf = Encoding.UTF8.GetBytes("File not found: " + filePath);
                res.OutputStream.Write(buf, 0, buf.Length);
                res.Close();
                return;
            }

            byte[] bytes = File.ReadAllBytes(fullPath);
            res.ContentType = contentType;
            res.ContentLength64 = bytes.Length;
            res.OutputStream.Write(bytes, 0, bytes.Length);
            res.Close();
        }

        private static void HandleApiStatus(HttpListenerResponse res)
        {
            List<LotteryRecord> localRecs;
            lock (dataLock)
            {
                localRecs = records;
            }

            var last = localRecs[localRecs.Count - 1];
            var marginal1D = Program.Compute1DMarginals(localRecs);
            var deEnergies = Program.ComputeUnifiedPotentials2D(localRecs, marginal1D);
            var lotto1D = Program.Compute1DLottoMarginals(localRecs);
            double[,] W;
            var lottoEnergies = Program.ComputeUnifiedLottoPotentials(localRecs, lotto1D, out W);
            var qci = Program.ComputeQCI(localRecs, lottoEnergies, deEnergies, lotto1D, marginal1D);
            var regime = Program.DetectMarketRegime(localRecs, deEnergies, W, marginal1D);

            var sb = new StringBuilder();
            sb.Append("{");
            sb.AppendFormat("\"totalRecords\": {0},", localRecs.Count);
            sb.AppendFormat("\"latestDate\": \"{0}\",", last.Date);
            sb.AppendFormat("\"latestDayOfWeek\": \"{0}\",", EscapeJson(last.DayOfWeek));
            sb.AppendFormat("\"latestGdb\": \"{0}\",", last.Gdb);
            sb.AppendFormat("\"latestDe\": {0},", last.De);
            sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "\"qciScore\": {0:F1},", qci.Score);
            sb.AppendFormat("\"qciZone\": \"{0}\",", EscapeJson(qci.Zone));
            sb.AppendFormat("\"actionRecommendation\": \"{0}\",", EscapeJson(qci.ActionRecommendation));
            sb.AppendFormat("\"regimeName\": \"{0}\",", EscapeJson(regime.RegimeName));
            sb.AppendFormat("\"regimeSymbol\": \"{0}\",", EscapeJson(regime.RegimeSymbol));
            sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "\"regimeEntropy\": {0:F3},", regime.EntropyH);
            sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "\"regimeGamma\": {0:F3},", regime.SpectralGapGamma);
            sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "\"regimeZ\": {0:F2},", regime.ZMacro);
            sb.AppendFormat("\"regimeCompressedFactor\": \"{0}\",", EscapeJson(regime.CompressedFactor));
            sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "\"regimeMomentum\": {0:F1},", regime.MomentumMRoll * 100);
            sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "\"kellyMultiplier\": {0:F2}", regime.KellyMultiplier);
            sb.Append("}");

            byte[] buf = Encoding.UTF8.GetBytes(sb.ToString());
            res.ContentType = "application/json; charset=utf-8";
            res.OutputStream.Write(buf, 0, buf.Length);
            res.Close();
        }

        private static void HandleApiPredict(HttpListenerResponse res, long budget)
        {
            List<LotteryRecord> localRecs;
            lock (dataLock)
            {
                localRecs = records;
            }

            int n = localRecs.Count;
            var last = localRecs[n - 1];
            var marginal1D = Program.Compute1DMarginals(localRecs);
            var deEnergies = Program.ComputeUnifiedPotentials2D(localRecs, marginal1D);
            var lotto1D = Program.Compute1DLottoMarginals(localRecs);
            double[,] W;
            var lottoEnergies = Program.ComputeUnifiedLottoPotentials(localRecs, lotto1D, out W);
            var qci = Program.ComputeQCI(localRecs, lottoEnergies, deEnergies, lotto1D, marginal1D);
            var regime = Program.DetectMarketRegime(localRecs, deEnergies, W, marginal1D);

            var top4Lo = Program.SelectTop4LoAdaptive(localRecs, lottoEnergies);
            int btLo = top4Lo[0];
            int stLo = top4Lo[1];
            int tuThu3 = top4Lo[2];
            int tuThu4 = top4Lo[3];

            // Cấu trúc Đề theo Thang 65% (TIDS 65% Decimation Pyramid) - Tất cả đều sắp xếp tăng dần
            int e1 = deEnergies[0].State;
            var e2 = deEnergies.Take(2).Select(x => x.State).OrderBy(x => x).ToList();
            var e4 = deEnergies.Take(4).Select(x => x.State).OrderBy(x => x).ToList();
            var e65 = Program.SelectFactorNeutralDeSet(deEnergies, 65).OrderBy(x => x).ToList();
            var sub42 = deEnergies.Where(e => e65.Contains(e.State)).ToList();
            var e42 = Program.SelectFactorNeutralDeSet(sub42, 42).OrderBy(x => x).ToList();
            var sub27 = deEnergies.Where(e => e42.Contains(e.State)).ToList();
            var e27 = Program.SelectFactorNeutralDeSet(sub27, 27).OrderBy(x => x).ToList();
            var sub18 = deEnergies.Where(e => e27.Contains(e.State)).ToList();
            var e18 = Program.SelectFactorNeutralDeSet(sub18, 18).OrderBy(x => x).ToList();
            var bsmCoiling = Program.ExtractBsmCoilingCluster(deEnergies, regime, 10).OrderBy(x => x).ToList();

            // Tương thích ngược cho các phiên bản cũ
            var e50 = e42;
            var e25 = e27;
            var e9 = e18.Take(9).OrderBy(x => x).ToList();

            // 3D Cascade đồng bộ
            var cascade3D = Program.ComputeDan3DCascade(localRecs, e65, deEnergies);
            string bt3D = cascade3D.BachThu3D;
            var dan3D_12 = cascade3D.Dan12.OrderBy(x => x).ToList();
            var dan3D_27 = cascade3D.Dan27.OrderBy(x => x).ToList();

            // 1. TRỤ CỘT ĐÒN BẨY BẮT BUỘC HÀNG NGÀY: 27s 3D & 81s 4D
            long cost27_3D = 27 * 1000;
            long cost81_4D = (regime.Regime == MarketRegimeType.Chaos) ? 0 : 81 * 1000;
            long costDonBay = cost27_3D + cost81_4D;

            // 2. CẮT BỎ HOÀN TOÀN CÁC DÀN TIÊU HAO VỐN: XIÊN, E1, E4
            long costXien = 0;
            long costE1 = 0;
            long costE4 = 0;
            long costE18 = 0;

            // 3. TAM THỦ LÔ (100% SỐ NGUYÊN ĐIỂM)
            long costPerPoint = 27000;
            int budgetMult = (int)Math.Max(1, Math.Round((double)budget / 500000.0));
            int stPts1 = 3 * budgetMult;
            int stPts2 = 3 * budgetMult;
            int lotPts = 1 * budgetMult;
            if (regime.Regime == MarketRegimeType.Peak) { stPts1 = 3 * budgetMult; stPts2 = 3 * budgetMult; lotPts = 1 * budgetMult; }
            else if (regime.Regime == MarketRegimeType.Convergent) { stPts1 = 3 * budgetMult; stPts2 = 2 * budgetMult; lotPts = 1 * budgetMult; }
            else if (regime.Regime == MarketRegimeType.Coiling) { stPts1 = 2 * budgetMult; stPts2 = 2 * budgetMult; lotPts = 1 * budgetMult; }
            else if (regime.Regime == MarketRegimeType.Chaos) { stPts1 = 1 * budgetMult; stPts2 = 1 * budgetMult; lotPts = 1 * budgetMult; }

            int stPts = Math.Max(stPts1, stPts2);
            long costStLo = (stPts1 + stPts2) * costPerPoint;
            long costLotLo = lotPts * costPerPoint;
            long costLotto = costStLo + costLotLo;

            // 4. ĐỀ 2D (KHÓA CỨNG TRẦN E65 - 100% SỐ NGUYÊN NGHÌN ĐỒNG)
            long c42 = 3000 * budgetMult;
            long costE42 = 42 * c42;
            long c65 = 1000 * budgetMult;
            long costE65 = 23 * c65;
            long c27 = 0;
            long costE27 = 0;
            long costDeTotal = 0;

            if (regime.Regime == MarketRegimeType.Peak)
            {
                c27 = 1000 * budgetMult;
                costE27 = 12 * c27;
                costDeTotal = costE42 + costE65 + costE27; // 161k
            }
            else if (regime.Regime == MarketRegimeType.Convergent || regime.Regime == MarketRegimeType.Coiling)
            {
                c42 = 3000 * budgetMult;
                c65 = 3000 * budgetMult;
                costDeTotal = 65 * c65; // 195k phẳng
                costE42 = 42 * c42;
                costE65 = 23 * c65;
            }
            else // Chaos
            {
                c42 = 2000 * budgetMult;
                c65 = 2000 * budgetMult;
                costDeTotal = 65 * c65; // 130k phẳng
                costE42 = 42 * c42;
                costE65 = 23 * c65;
            }

            // Gán tương thích cho các client cũ (E50, E25, E9)
            long c50 = c42;
            long costE50 = costE42;
            long c25 = c27;
            long costE25 = costE27;
            long c18 = 0;
            long c9 = 0;
            long costE9 = 0;
            long c4 = 0;
            long c1 = 0;

            // 5. DÀN BỔ SUNG 3D/4D (CHỈ KÍCH HOẠT KHI PEAK/CONVERGENT)
            long cost3D_12 = 0;
            long cost4D_small = 0;
            if (regime.Regime == MarketRegimeType.Peak)
            {
                cost3D_12 = 12 * 2000 * budgetMult; // 24k
                cost4D_small = 18 * 1000 * budgetMult; // 18k
            }
            else if (regime.Regime == MarketRegimeType.Convergent)
            {
                cost3D_12 = 12 * 2000 * budgetMult; // 24k
                cost4D_small = 11000 * budgetMult;  // 11k
            }

            long c3D_base = (cost3D_12 > 0 ? (cost3D_12 / 12) : 0);
            long c3D_bt = c3D_base;
            long cost3D = cost27_3D + cost3D_12;

            long actualTotal = costLotto + costDeTotal + costDonBay + cost3D_12 + cost4D_small;

            var tamThuLoList = new List<int> { btLo, stLo, tuThu3 }.OrderBy(x => x).ToList();
            var tuThuLoList = new List<int> { btLo, stLo, tuThu3, tuThu4 }.OrderBy(x => x).ToList();

            // 3 Cặp Xiên 2 Tam Thủ (Sắp xếp tăng dần)
            var xien2List = new List<string> {
                string.Format("{0}-{1}", tamThuLoList[0].ToString("D2"), tamThuLoList[1].ToString("D2")),
                string.Format("{0}-{1}", tamThuLoList[0].ToString("D2"), tamThuLoList[2].ToString("D2")),
                string.Format("{0}-{1}", tamThuLoList[1].ToString("D2"), tamThuLoList[2].ToString("D2"))
            };

            // 1 Bộ Xiên 3 Tam Thủ
            var xien3List = new List<string> {
                string.Format("{0}-{1}-{2}", tamThuLoList[0].ToString("D2"), tamThuLoList[1].ToString("D2"), tamThuLoList[2].ToString("D2"))
            };

            string xien4 = string.Join("-", tuThuLoList.Select(x => x.ToString("D2")));

            // 4 CÀNG (4D): Quantum TIDS (5 Quy luật 2 Cột Tổng + 7 Thế Năng + Lưới Lọc Đại Số Mod 2, 3, 5 - Sắp xếp tăng dần)
            string bt4D;
            List<string> dan4D_4;
            List<string> dan4D_16;
            List<string> dan4D_36;
            List<string> dan4D_81;
            Program.Compute4D_QuantumOptimal(localRecs, out bt4D, out dan4D_4, out dan4D_16, out dan4D_36, out dan4D_81);
            dan4D_4 = dan4D_4.OrderBy(x => x).ToList();
            dan4D_16 = dan4D_16.OrderBy(x => x).ToList();
            dan4D_36 = dan4D_36.OrderBy(x => x).ToList();
            dan4D_81 = dan4D_81.OrderBy(x => x).ToList();

            // XÂY DỰNG DANH SÁCH 24 HẠNG MỤC CHUẨN LỆNH SHEET (SẮP XẾP TĂNG DẦN TRIỆT ĐỂ)
            var sheetRows = new List<string>();
            Action<int, string, string, int, string, string, long, long, string> addRow = (stt, axis, cat, qty, nums, unit, rate, total, note) =>
            {
                var rowSb = new StringBuilder();
                rowSb.Append("{");
                rowSb.AppendFormat("\"stt\": {0},", stt);
                rowSb.AppendFormat("\"axis\": \"{0}\",", EscapeJson(axis));
                rowSb.AppendFormat("\"category\": \"{0}\",", EscapeJson(cat));
                rowSb.AppendFormat("\"quantity\": {0},", qty);
                rowSb.AppendFormat("\"numbers\": \"{0}\",", EscapeJson(nums));
                rowSb.AppendFormat("\"unit\": \"{0}\",", EscapeJson(unit));
                rowSb.AppendFormat("\"betPerUnit\": {0},", rate);
                rowSb.AppendFormat("\"totalBet\": {0},", total);
                rowSb.AppendFormat("\"note\": \"{0}\"", EscapeJson(note));
                rowSb.Append("}");
                sheetRows.Add(rowSb.ToString());
            };

            addRow(1, "Trục 1: Địa Lợi (+EV)", "Song Thủ Lô (STL)", 2, string.Join(", ", new List<int> { btLo, stLo }.OrderBy(x => x).Select(x => x.ToString("D2"))), "Điểm/số", stPts1, costStLo, string.Format("Động cơ +EV: Con 1 ({0}đ), Con 2 ({1}đ)", stPts1, stPts2));
            addRow(2, "Trục 1: Địa Lợi (+EV)", "Bạch Thủ Lô (BTL)", 1, btLo.ToString("D2"), "Điểm/số", 0, 0, "Đã nằm trong cặp Song Thủ Lô (Vốn = 0 đ)");
            addRow(3, "Trục 2: Nhân Hòa (Hộ thân)", "Lô Lót Tam Thủ", 1, tuThu3.ToString("D2"), "Điểm/số", lotPts, costLotLo, string.Format("Lót bảo hiểm danh mục ({0} điểm)", lotPts));
            addRow(4, "Trục 2: Nhân Hòa (Hộ thân)", "Trọn Bộ Tam Thủ Lô", 3, string.Join(", ", tamThuLoList.Select(x => x.ToString("D2"))), "Điểm/số", 0, 0, "Hệ 3 số bảo vệ (Đã phân bổ ở STL + Lót)");
            addRow(5, "Trục 1: Địa Lợi (+EV)", "Cặp Xiên 2 Song Thủ", 1, string.Format("({0}-{1})", Math.Min(btLo, stLo).ToString("D2"), Math.Max(btLo, stLo).ToString("D2")), "VNĐ/vé", 0, 0, "Đã cắt bỏ để dồn vốn cho 3D/4D (Vốn = 0 đ)");
            addRow(6, "Trục 1: Địa Lợi (+EV)", "Bộ 3 Cặp Xiên 2 Tam Thủ", 3, string.Join(", ", xien2List.Select(x => "(" + x + ")")), "VNĐ/vé", 0, 0, "Xiên quay dự phòng (Vốn = 0 đ)");
            addRow(7, "Trục 3: Thiên Thời (Đột biến)", "Bộ 1 Xiên 3 Tam Thủ", 1, string.Join(", ", xien3List.Select(x => "(" + x + ")")), "VNĐ/vé", 0, 0, "Xiên 3 Tam Thủ (Vốn = 0 đ)");
            addRow(8, "Trục 2: Nhân Hòa (Hộ thân)", "Tứ Thủ Lô Dự Phòng (4s)", 4, string.Join(", ", tuThuLoList.Select(x => x.ToString("D2"))), "Điểm/số", 0, 0, "Mở rộng số lót thứ 2 (Vốn = 0 đ)");
            addRow(9, "Trục 2: Nhân Hòa (Hộ thân)", "Dàn Chân Kiềng (E42)", 42, string.Join(", ", e42.Select(x => x.ToString("D2"))), "VNĐ/số", c42, costE42, string.Format("Lõi 42 số thang 65% ({0:N0} đ/số)", c42));
            addRow(10, "Trục 2: Nhân Hòa (Hộ thân)", "Huyền Thiên Giáp (E65)", 65, string.Join(", ", e65.Select(x => x.ToString("D2"))), "VNĐ/số", c65, costE65, string.Format("Vỏ bọc 23 số biên E65 ({0:N0} đ/số)", c65));
            addRow(11, "Trục 2: Nhân Hòa (Hộ thân)", string.Format("Quy Nguyên Trận [{0}]", regime.CompressedFactor), bsmCoiling.Count, string.Join(", ", bsmCoiling.Select(x => x.ToString("D2"))), "VNĐ/số", 0, 0, "Dự phòng săn điểm nổ vĩ mô (Vốn = 0 đ)");
            addRow(12, "Trục 2: Nhân Hòa (Hộ thân)", "Dàn Tấn Công (E27)", 27, string.Join(", ", e27.Select(x => x.ToString("D2"))), "VNĐ/số", c27, costE27, (costE27 > 0 ? string.Format("Cược thêm 12 số đột phá ({0:N0} đ/số)", c27) : "Dự phòng (Vốn = 0 đ)"));
            addRow(13, "Trục 2: Nhân Hòa (Hộ thân)", "Dàn Gối Đầu (E18)", 18, string.Join(", ", e18.Select(x => x.ToString("D2"))), "VNĐ/số", 0, 0, "Đã cắt bỏ theo nguyên tắc tinh gọn (Vốn = 0 đ)");
            addRow(14, "Trục 2: Nhân Hòa (Hộ thân)", "Tứ Tượng Hội Tụ (E4)", 4, string.Join(", ", e4.Select(x => x.ToString("D2"))), "VNĐ/số", 0, 0, "Đã cắt bỏ theo nguyên tắc tinh gọn (Vốn = 0 đ)");
            addRow(15, "Trục 2: Nhân Hòa (Hộ thân)", "Thái Cực Nhất Điểm (E1)", 1, e1.ToString("D2"), "VNĐ/số", 0, 0, "Đã cắt bỏ theo nguyên tắc tinh gọn (Vốn = 0 đ)");
            addRow(16, "Trục 2: Nhân Hòa (Hộ thân)", "Lưỡng Nghi Đồng Quy (E2)", 2, string.Join(", ", e2.Select(x => x.ToString("D2"))), "VNĐ/số", 0, 0, "Đã nằm trong Tứ thủ E4 (Vốn = 0 đ)");
            addRow(17, "Trục 3: Thiên Thời (Đột biến)", "Dàn 3 Càng Tinh Tuyển 12 số (3D)", 12, string.Join(", ", dan3D_12), "VNĐ/vé", c3D_base, cost3D_12, (cost3D_12 > 0 ? "Bổ sung tấn công theo thời khí Peak/Convergent" : "Tắt dàn nhỏ ở Coiling/Chaos (Vốn = 0 đ)"));
            addRow(18, "Trục 3: Thiên Thời (Đột biến)", "Bạch Thủ 3 Càng (3D)", 1, bt3D, "VNĐ/vé", 0, 0, "Đã gộp vốn vào Dàn 27 số (Vốn = 0 đ)");
            addRow(19, "Trục 3: Thiên Thời (Đột biến)", "Dàn 3 Càng Phổ 27 số (3D)", 27, string.Join(", ", dan3D_27), "VNĐ/vé", 1000, cost27_3D, "Trụ cột đòn bẩy thường trực ăn 1:960 (27k/ngày)");
            addRow(20, "Trục 3: Thiên Thời (Đột biến)", "Bạch Thủ 4 Càng (4D)", 1, bt4D, "VNĐ/vé", 0, 0, "Dự phòng siêu đòn bẩy (Vốn = 0 đ)");
            addRow(21, "Trục 3: Thiên Thời (Đột biến)", "Tứ Thủ 4 Càng (4D)", 4, string.Join(", ", dan4D_4), "VNĐ/vé", 0, 0, "Dự phòng siêu đòn bẩy (Vốn = 0 đ)");
            addRow(22, "Trục 3: Thiên Thời (Đột biến)", "Dàn 4 Càng Tinh Tuyển 16 số (4D)", 16, string.Join(", ", dan4D_16), "VNĐ/vé", 0, 0, "Dự phòng (Vốn = 0 đ)");
            addRow(23, "Trục 3: Thiên Thời (Đột biến)", "Dàn 4 Càng Tối Ưu ROI 36 số (4D)", 36, string.Join(", ", dan4D_36), "VNĐ/vé", 0, 0, "Dự phòng (Vốn = 0 đ)");
            addRow(24, "Trục 3: Thiên Thời (Đột biến)", "Dàn 4 Càng Trục Chính 81 số (4D)", 81, string.Join(", ", dan4D_81), "VNĐ/vé", (cost81_4D > 0 ? 1000 : 0), cost81_4D, (cost81_4D > 0 ? "Trụ cột siêu đòn bẩy thường trực ăn 1:8,880 (81k/ngày)" : "Tạm dừng ở pha Chaos để bảo toàn vốn (Vốn = 0 đ)"));

            // Xây dựng chuỗi Copy Zalo / Tin nhắn (Tất cả dàn đều sắp xếp tăng dần)
            var copyZaloSb = new StringBuilder();
            copyZaloSb.AppendLine(string.Format("--- ĐẠO DỊCH HUYỀN CƠ PHÁP (VỐN {0:N0} Đ) ---", budget));
            copyZaloSb.AppendLine(string.Format("🟢 Thiên Cơ Độ: {0:F1}% | Khí: {1}", qci.Score, regime.RegimeName));
            copyZaloSb.AppendLine(string.Format("1. Tam Thủ Lô: {0:D2} ({1}đ), {2:D2} ({3}đ) | Lót {4:D2} ({5}đ)", btLo, stPts1, stLo, stPts2, tuThu3, lotPts));
            copyZaloSb.AppendLine(string.Format("2. Đề 2D (Trần E65): E42 lõi x {0:N0}k, 23s vỏ x {1:N0}k", c42 / 1000, c65 / 1000));
            copyZaloSb.AppendLine(string.Format("3. Trụ Cột Đòn Bẩy Thường Trực:"));
            copyZaloSb.AppendLine(string.Format("   • 27s 3D x 1k (Ăn 1:960, Thu 960k)"));
            if (cost81_4D > 0)
                copyZaloSb.AppendLine(string.Format("   • 81s 4D x 1k (Ăn 1:8.880, Thu 8.88 triệu)"));
            else
                copyZaloSb.AppendLine(string.Format("   • 81s 4D: [Tạm dừng ở pha Chaos để bảo toàn vốn]"));
            if (cost3D_12 > 0)
                copyZaloSb.AppendLine(string.Format("4. Dàn 3D 12s Tinh Tuyển x thêm {0:N0}k", cost3D_12 / (12 * 1000)));
            copyZaloSb.AppendLine("[Bảo toàn vốn: Xác suất hòa vốn hoặc lãi >= 90% | Bỏ Xiên, E1, E4]");
            copyZaloSb.AppendLine("--------------------------------------------------");

            // Xây dựng JSON
            var sb = new StringBuilder();
            sb.Append("{");
            sb.AppendFormat("\"budget\": {0},", budget);
            sb.AppendFormat("\"actualTotal\": {0},", actualTotal);
            sb.AppendFormat("\"btLo\": {0},", btLo);
            sb.AppendFormat("\"stLo\": {0},", stLo);
            sb.AppendFormat("\"tamThuLo\": [{0}, {1}, {2}],", tamThuLoList[0], tamThuLoList[1], tamThuLoList[2]);
            sb.AppendFormat("\"tuThuLo\": [{0}, {1}, {2}, {3}],", tuThuLoList[0], tuThuLoList[1], tuThuLoList[2], tuThuLoList[3]);
            sb.AppendFormat("\"stPts\": {0},", stPts);
            sb.AppendFormat("\"stPts1\": {0},", stPts1);
            sb.AppendFormat("\"stPts2\": {0},", stPts2);
            sb.AppendFormat("\"lotPts\": {0},", lotPts);
            sb.AppendFormat("\"costStLo\": {0},", costStLo);
            sb.AppendFormat("\"costLotLo\": {0},", costLotLo);
            sb.AppendFormat("\"costXien\": {0},", costXien);
            sb.AppendFormat("\"cost27_3D\": {0},", cost27_3D);
            sb.AppendFormat("\"cost81_4D\": {0},", cost81_4D);
            sb.AppendFormat("\"cost3D_12\": {0},", cost3D_12);
            sb.AppendFormat("\"cost4D_small\": {0},", cost4D_small);
            sb.AppendFormat("\"dan3D_27\": [{0}],", string.Join(",", dan3D_27.Select(x => "\"" + x + "\"")));

            sb.AppendFormat("\"e1\": {0},", e1);
            sb.AppendFormat("\"e2\": [{0}],", string.Join(",", e2));
            sb.AppendFormat("\"e4\": [{0}],", string.Join(",", e4));
            sb.AppendFormat("\"e18\": [{0}],", string.Join(",", e18));
            sb.AppendFormat("\"e27\": [{0}],", string.Join(",", e27));
            sb.AppendFormat("\"e42\": [{0}],", string.Join(",", e42));
            sb.AppendFormat("\"e65\": [{0}],", string.Join(",", e65));
            sb.AppendFormat("\"bsmCoiling\": [{0}],", string.Join(",", bsmCoiling));

            // Tương thích ngược
            sb.AppendFormat("\"e9\": [{0}],", string.Join(",", e9));
            sb.AppendFormat("\"e25\": [{0}],", string.Join(",", e25));
            sb.AppendFormat("\"e50\": [{0}],", string.Join(",", e50));

            sb.AppendFormat("\"c65\": {0},", c65);
            sb.AppendFormat("\"c42\": {0},", c42);
            sb.AppendFormat("\"c27\": {0},", c27);
            sb.AppendFormat("\"c18\": {0},", c18);
            sb.AppendFormat("\"c4\": {0},", c4);
            sb.AppendFormat("\"c1\": {0},", c1);

            // Tương thích ngược
            sb.AppendFormat("\"c50\": {0},", c50);
            sb.AppendFormat("\"c25\": {0},", c25);
            sb.AppendFormat("\"c9\": {0},", c9);

            sb.AppendFormat("\"costE65\": {0},", costE65);
            sb.AppendFormat("\"costE42\": {0},", costE42);
            sb.AppendFormat("\"costE27\": {0},", costE27);
            sb.AppendFormat("\"costE18\": {0},", costE18);
            sb.AppendFormat("\"costE50\": {0},", costE50);
            sb.AppendFormat("\"costE9\": {0},", costE9);
            sb.AppendFormat("\"costE4\": {0},", costE4);
            sb.AppendFormat("\"costE1\": {0},", costE1);

            sb.AppendFormat("\"regimeCompressedFactor\": \"{0}\",", EscapeJson(regime.CompressedFactor));
            sb.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "\"regimeZ\": {0:F2},", regime.ZMacro);
            sb.AppendFormat("\"regimeName\": \"{0}\",", EscapeJson(regime.RegimeName));

            sb.AppendFormat("\"bt3D\": \"{0}\",", bt3D);
            sb.AppendFormat("\"dan3D_12\": [{0}],", string.Join(",", dan3D_12.Select(x => "\"" + x + "\"")));
            sb.AppendFormat("\"cost3D\": {0},", cost3D);
            sb.AppendFormat("\"c3D_bt\": {0},", c3D_bt);
            sb.AppendFormat("\"c3D_base\": {0},", c3D_base);

            sb.AppendFormat("\"bt4D\": \"{0}\",", bt4D);
            sb.AppendFormat("\"dan4D_4\": [{0}],", string.Join(",", dan4D_4.Select(x => "\"" + x + "\"")));
            sb.AppendFormat("\"dan4D_16\": [{0}],", string.Join(",", dan4D_16.Select(x => "\"" + x + "\"")));
            sb.AppendFormat("\"dan4D_36\": [{0}],", string.Join(",", dan4D_36.Select(x => "\"" + x + "\"")));
            sb.AppendFormat("\"dan4D_81\": [{0}],", string.Join(",", dan4D_81.Select(x => "\"" + x + "\"")));

            sb.AppendFormat("\"copyZaloText\": \"{0}\",", EscapeJson(copyZaloSb.ToString()));
            sb.AppendFormat("\"sheetItems\": [{0}]", string.Join(",", sheetRows));
            sb.Append("}");

            byte[] buf = Encoding.UTF8.GetBytes(sb.ToString());
            res.ContentType = "application/json; charset=utf-8";
            res.OutputStream.Write(buf, 0, buf.Length);
            res.Close();
        }

        private static void HandleApiHistory(HttpListenerResponse res, int days)
        {
            List<LotteryRecord> localRecs;
            lock (dataLock)
            {
                localRecs = records;
            }

            int n = localRecs.Count;
            int start = Math.Max(2, n - days);
            var list = new List<string>();

            for (int k = start; k < n; k++)
            {
                var past = localRecs.Take(k).ToList();
                var actual = localRecs[k];

                var lotto1D = Program.Compute1DLottoMarginals(past);
                double[,] W;
                var lottoEnergies = Program.ComputeUnifiedLottoPotentials(past, lotto1D, out W);
                var top4 = Program.SelectTop4LoAdaptive(past, lottoEnergies);

                var marginal1D = Program.Compute1DMarginals(past);
                var deEnergies = Program.ComputeUnifiedPotentials2D(past, marginal1D);
                var e65 = Program.SelectFactorNeutralDeSet(deEnergies, 65);
                var e42 = Program.SelectFactorNeutralDeSet(deEnergies, 42);
                var e27 = Program.SelectFactorNeutralDeSet(deEnergies, 27);
                var e18 = Program.SelectFactorNeutralDeSet(deEnergies, 18);
                var e50 = Program.SelectFactorNeutralDeSet(deEnergies, 50);
                var e25 = Program.SelectFactorNeutralDeSet(deEnergies, 25);
                var e9 = Program.SelectFactorNeutralDeSet(deEnergies, 9);
                var e4 = Program.SelectFactorNeutralDeSet(deEnergies, 4);
                int e1 = deEnergies[0].State;

                // Lô
                bool hitStl = actual.UniqueLo.Contains(top4[0]) || actual.UniqueLo.Contains(top4[1]);
                int stlHits = 0;
                foreach (var lo in actual.LoList)
                {
                    if (lo == top4[0] || lo == top4[1]) stlHits++;
                }

                bool hitXien2 = actual.UniqueLo.Contains(top4[0]) && actual.UniqueLo.Contains(top4[1]);

                int tuThuHits = 0;
                foreach (var lo in actual.LoList)
                {
                    if (lo == top4[0] || lo == top4[1] || lo == top4[2] || lo == top4[3]) tuThuHits++;
                }
                bool hitTuThu = tuThuHits > 0;

                // Đề
                bool hitE65 = e65.Contains(actual.De);
                bool hitE42 = e42.Contains(actual.De);
                bool hitE27 = e27.Contains(actual.De);
                bool hitE18 = e18.Contains(actual.De);
                bool hitE50 = e50.Contains(actual.De);
                bool hitE25 = e25.Contains(actual.De);
                bool hitE9 = e9.Contains(actual.De);
                bool hitE4 = e4.Contains(actual.De);
                bool hitE1 = (e1 == actual.De);

                // 3D
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
                var d3List = new List<Tuple<int, double>>();
                for (int d = 0; d < 10; d++)
                {
                    double rTerm = (ganD3[d] >= 8 && ganD3[d] <= 20) ? -1.5 : (ganD3[d] == 0 ? 1.5 : 0.0);
                    double kTerm = -0.3 * (f30D3[d] - 3.0);
                    d3List.Add(new Tuple<int, double>(d, rTerm + kTerm));
                }
                var topD3 = d3List.OrderBy(x => x.Item2).Take(3).Select(x => x.Item1).ToArray();
                string bt3D = topD3[0].ToString() + e1.ToString("D2");
                var dan3D_12 = Program.ComputeDan3D_12_Recursive(past, past[k - 1].DayOfWeek, bt3D, e4);

                var dan3D_27 = new List<string>();
                foreach (int h in topD3)
                    foreach (int t in e9)
                        dan3D_27.Add(h.ToString() + t.ToString("D2"));

                string actual3D = (actual.Gdb != null && actual.Gdb.Length >= 3) ? actual.Gdb.Substring(actual.Gdb.Length - 3) : "";
                bool hit3D = !string.IsNullOrEmpty(actual3D) && dan3D_12.Contains(actual3D);
                bool hit3D_27 = !string.IsNullOrEmpty(actual3D) && dan3D_27.Contains(actual3D);

                // 4D Quantum Optimal
                string bt4D;
                List<string> dan4D_4, dan4D_16, dan4D_36, dan4D_81;
                Program.Compute4D_QuantumOptimal(past, out bt4D, out dan4D_4, out dan4D_16, out dan4D_36, out dan4D_81);

                string actual4D = (actual.Gdb != null && actual.Gdb.Length >= 4) ? actual.Gdb.Substring(actual.Gdb.Length - 4) : "";
                bool hit4D_4  = !string.IsNullOrEmpty(actual4D) && dan4D_4.Contains(actual4D);
                bool hit4D_16 = !string.IsNullOrEmpty(actual4D) && dan4D_16.Contains(actual4D);
                bool hit4D_36 = !string.IsNullOrEmpty(actual4D) && dan4D_36.Contains(actual4D);
                bool hit4D_81 = !string.IsNullOrEmpty(actual4D) && dan4D_81.Contains(actual4D);

                var sbRow = new StringBuilder();
                sbRow.Append("{");
                sbRow.AppendFormat("\"date\": \"{0}\",", actual.Date);
                sbRow.AppendFormat("\"dayOfWeek\": \"{0}\",", EscapeJson(actual.DayOfWeek));
                sbRow.AppendFormat("\"gdb\": \"{0}\",", actual.Gdb);
                sbRow.AppendFormat("\"de\": {0},", actual.De);
                sbRow.AppendFormat("\"actual3D\": \"{0}\",", actual3D);
                sbRow.AppendFormat("\"actual4D\": \"{0}\",", actual4D);
                sbRow.AppendFormat("\"btLo\": {0},", top4[0]);
                sbRow.AppendFormat("\"stLo\": {0},", top4[1]);
                sbRow.AppendFormat("\"tuThu3\": {0},", top4[2]);
                sbRow.AppendFormat("\"tuThu4\": {0},", top4[3]);
                sbRow.AppendFormat("\"hitStl\": {0},", hitStl.ToString().ToLower());
                sbRow.AppendFormat("\"stlHits\": {0},", stlHits);
                sbRow.AppendFormat("\"hitTuThu\": {0},", hitTuThu.ToString().ToLower());
                sbRow.AppendFormat("\"tuThuHits\": {0},", tuThuHits);
                sbRow.AppendFormat("\"hitXien2\": {0},", hitXien2.ToString().ToLower());
                sbRow.AppendFormat("\"hitE65\": {0},", hitE65.ToString().ToLower());
                sbRow.AppendFormat("\"hitE42\": {0},", hitE42.ToString().ToLower());
                sbRow.AppendFormat("\"hitE27\": {0},", hitE27.ToString().ToLower());
                sbRow.AppendFormat("\"hitE18\": {0},", hitE18.ToString().ToLower());
                sbRow.AppendFormat("\"hitE50\": {0},", hitE50.ToString().ToLower());
                sbRow.AppendFormat("\"hitE25\": {0},", hitE25.ToString().ToLower());
                sbRow.AppendFormat("\"hitE9\": {0},", hitE9.ToString().ToLower());
                sbRow.AppendFormat("\"hitE4\": {0},", hitE4.ToString().ToLower());
                sbRow.AppendFormat("\"hitE1\": {0},", hitE1.ToString().ToLower());
                sbRow.AppendFormat("\"e1\": {0},", e1);
                sbRow.AppendFormat("\"hit3D\": {0},", hit3D.ToString().ToLower());
                sbRow.AppendFormat("\"hit3D_27\": {0},", hit3D_27.ToString().ToLower());
                sbRow.AppendFormat("\"hit4D_4\": {0},", hit4D_4.ToString().ToLower());
                sbRow.AppendFormat("\"hit4D_16\": {0},", hit4D_16.ToString().ToLower());
                sbRow.AppendFormat("\"hit4D_36\": {0},", hit4D_36.ToString().ToLower());
                sbRow.AppendFormat("\"hit4D_81\": {0}", hit4D_81.ToString().ToLower());
                sbRow.Append("}");
                list.Add(sbRow.ToString());
            }

            string json = "[" + string.Join(",", list) + "]";
            byte[] buf = Encoding.UTF8.GetBytes(json);
            res.ContentType = "application/json; charset=utf-8";
            res.OutputStream.Write(buf, 0, buf.Length);
            res.Close();
        }

        private static void HandleApiParseResult(HttpListenerRequest req, HttpListenerResponse res)
        {
            string body;
            using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
            {
                body = reader.ReadToEnd();
            }

            var parsed = ParseDailyLotteryText(body);
            byte[] buf = Encoding.UTF8.GetBytes(parsed);
            res.ContentType = "application/json; charset=utf-8";
            res.OutputStream.Write(buf, 0, buf.Length);
            res.Close();
        }

        private static void HandleApiSaveResult(HttpListenerRequest req, HttpListenerResponse res)
        {
            string body;
            using (var reader = new StreamReader(req.InputStream, req.ContentEncoding))
            {
                body = reader.ReadToEnd();
            }

            string resultJson = SaveDailyLotteryRecord(body);
            byte[] buf = Encoding.UTF8.GetBytes(resultJson);
            res.ContentType = "application/json; charset=utf-8";
            res.OutputStream.Write(buf, 0, buf.Length);
            res.Close();
        }

        // Bóc tách thông minh văn bản xổ số được dán vào
        public static string ParseDailyLotteryText(string rawText)
        {
            if (string.IsNullOrWhiteSpace(rawText))
            {
                return "{\"valid\": false, \"error\": \"Văn bản trống\"}";
            }

            // Tìm ngày nếu có và loại bỏ ngày ra khỏi văn bản để không bị nhận nhầm số
            string dateStr = DateTime.Now.ToString("yyyy-MM-dd");
            string textWithoutDate = rawText;
            var dateMatch = Regex.Match(rawText, @"(\d{1,2})[\/\-\.](\d{1,2})[\/\-\.](\d{4})");
            if (dateMatch.Success)
            {
                int day = int.Parse(dateMatch.Groups[1].Value);
                int month = int.Parse(dateMatch.Groups[2].Value);
                int year = int.Parse(dateMatch.Groups[3].Value);
                dateStr = string.Format("{0:D4}-{1:D2}-{2:D2}", year, month, day);
                textWithoutDate = rawText.Remove(dateMatch.Index, dateMatch.Length);
            }
            else
            {
                var isoMatch = Regex.Match(rawText, @"(\d{4})[\/\-\.](\d{1,2})[\/\-\.](\d{1,2})");
                if (isoMatch.Success)
                {
                    int year = int.Parse(isoMatch.Groups[1].Value);
                    int month = int.Parse(isoMatch.Groups[2].Value);
                    int day = int.Parse(isoMatch.Groups[3].Value);
                    dateStr = string.Format("{0:D4}-{1:D2}-{2:D2}", year, month, day);
                    textWithoutDate = rawText.Remove(isoMatch.Index, isoMatch.Length);
                }
                else
                {
                    // Lấy ngày tiếp theo của bản ghi cuối
                    lock (dataLock)
                    {
                        if (records != null && records.Count > 0)
                        {
                            DateTime lastD;
                            if (DateTime.TryParse(records[records.Count - 1].Date, out lastD))
                            {
                                dateStr = lastD.AddDays(1).ToString("yyyy-MM-dd");
                            }
                        }
                    }
                }
            }

            DateTime dt;
            DateTime.TryParse(dateStr, out dt);
            string dayOfWeek = GetVietnameseDayOfWeek(dt);

            // Bóc tách số:
            // Phương án 1: Thử trích xuất theo nhãn GĐB, G1, G2, G3, G4, G5, G6, G7
            string gdb = "";
            string g1 = "";
            var g2 = new List<string>();
            var g3 = new List<string>();
            var g4 = new List<string>();
            var g5 = new List<string>();
            var g6 = new List<string>();
            var g7 = new List<string>();

            // Trích xuất tất cả các số nguyên trong văn bản (đã loại bỏ chuỗi ngày tháng)
            var matches = Regex.Matches(textWithoutDate, @"\b\d{2,5}\b");
            var cleanNumbers = new List<string>();
            foreach (Match m in matches)
            {
                string val = m.Value;
                // Bỏ qua năm 2024, 2025, 2026 nếu đứng lẻ
                if (val == "2024" || val == "2025" || val == "2026") continue;
                cleanNumbers.Add(val);
            }

            // Nếu đủ 27 số hoặc gần đủ
            if (cleanNumbers.Count >= 27)
            {
                // Ưu tiên 27 số đầu tiên hoặc 27 số khớp độ dài [5x10, 4x10, 3x3, 2x4]
                gdb = cleanNumbers[0].PadLeft(5, '0');
                g1 = cleanNumbers[1].PadLeft(5, '0');
                g2.Add(cleanNumbers[2].PadLeft(5, '0'));
                g2.Add(cleanNumbers[3].PadLeft(5, '0'));
                for (int i = 4; i < 10; i++) g3.Add(cleanNumbers[i].PadLeft(5, '0'));
                for (int i = 10; i < 14; i++) g4.Add(cleanNumbers[i].PadLeft(4, '0'));
                for (int i = 14; i < 20; i++) g5.Add(cleanNumbers[i].PadLeft(4, '0'));
                for (int i = 20; i < 23; i++) g6.Add(cleanNumbers[i].PadLeft(3, '0'));
                for (int i = 23; i < 27; i++) g7.Add(cleanNumbers[i].PadLeft(2, '0'));
            }
            else
            {
                // Tìm theo từ khóa
                var lines = rawText.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    string l = line.ToLower();
                    var numMatches = Regex.Matches(line, @"\b\d{2,5}\b");
                    var lineNums = new List<string>();
                    foreach (Match nm in numMatches)
                    {
                        if (nm.Value != "2025" && nm.Value != "2026") lineNums.Add(nm.Value);
                    }

                    if ((l.Contains("đặc biệt") || l.Contains("đb") || l.Contains("g.đb")) && lineNums.Count > 0 && string.IsNullOrEmpty(gdb))
                    {
                        gdb = lineNums[0].PadLeft(5, '0');
                    }
                    else if ((l.Contains("nhất") || l.Contains("g1") || l.Contains("g.1")) && lineNums.Count > 0 && string.IsNullOrEmpty(g1))
                    {
                        g1 = lineNums[0].PadLeft(5, '0');
                    }
                    else if ((l.Contains("nhì") || l.Contains("g2") || l.Contains("g.2")) && lineNums.Count > 0 && g2.Count < 2)
                    {
                        foreach (var nStr in lineNums) if (g2.Count < 2) g2.Add(nStr.PadLeft(5, '0'));
                    }
                    else if ((l.Contains("ba") || l.Contains("g3") || l.Contains("g.3")) && lineNums.Count > 0 && g3.Count < 6)
                    {
                        foreach (var nStr in lineNums) if (g3.Count < 6) g3.Add(nStr.PadLeft(5, '0'));
                    }
                    else if ((l.Contains("tư") || l.Contains("g4") || l.Contains("g.4")) && lineNums.Count > 0 && g4.Count < 4)
                    {
                        foreach (var nStr in lineNums) if (g4.Count < 4) g4.Add(nStr.PadLeft(4, '0'));
                    }
                    else if ((l.Contains("năm") || l.Contains("g5") || l.Contains("g.5")) && lineNums.Count > 0 && g5.Count < 6)
                    {
                        foreach (var nStr in lineNums) if (g5.Count < 6) g5.Add(nStr.PadLeft(4, '0'));
                    }
                    else if ((l.Contains("sáu") || l.Contains("g6") || l.Contains("g.6")) && lineNums.Count > 0 && g6.Count < 3)
                    {
                        foreach (var nStr in lineNums) if (g6.Count < 3) g6.Add(nStr.PadLeft(3, '0'));
                    }
                    else if ((l.Contains("bảy") || l.Contains("g7") || l.Contains("g.7")) && lineNums.Count > 0 && g7.Count < 4)
                    {
                        foreach (var nStr in lineNums) if (g7.Count < 4) g7.Add(nStr.PadLeft(2, '0'));
                    }
                }
            }

            // Tính 27 con Lô và Đề
            int de = -1;
            int dau = -1;
            int duoi = -1;
            int tong = -1;
            var loList = new List<int>();

            if (gdb.Length >= 2)
            {
                int.TryParse(gdb.Substring(gdb.Length - 2), out de);
                dau = de / 10;
                duoi = de % 10;
                tong = (dau + duoi) % 10;
            }

            Action<string> addLo = delegate(string p)
            {
                if (!string.IsNullOrEmpty(p) && p.Length >= 2)
                {
                    int v;
                    if (int.TryParse(p.Substring(p.Length - 2), out v)) loList.Add(v);
                }
            };

            addLo(gdb);
            addLo(g1);
            foreach (var p in g2) addLo(p);
            foreach (var p in g3) addLo(p);
            foreach (var p in g4) addLo(p);
            foreach (var p in g5) addLo(p);
            foreach (var p in g6) addLo(p);
            foreach (var p in g7) addLo(p);

            var sb = new StringBuilder();
            sb.Append("{");
            sb.AppendFormat("\"valid\": {0},", (loList.Count == 27 && de >= 0).ToString().ToLower());
            sb.AppendFormat("\"date\": \"{0}\",", dateStr);
            sb.AppendFormat("\"dayOfWeek\": \"{0}\",", EscapeJson(dayOfWeek));
            sb.AppendFormat("\"gdb\": \"{0}\",", gdb);
            sb.AppendFormat("\"g1\": \"{0}\",", g1);
            sb.AppendFormat("\"g2\": [{0}],", string.Join(",", g2.Select(x => "\"" + x + "\"")));
            sb.AppendFormat("\"g3\": [{0}],", string.Join(",", g3.Select(x => "\"" + x + "\"")));
            sb.AppendFormat("\"g4\": [{0}],", string.Join(",", g4.Select(x => "\"" + x + "\"")));
            sb.AppendFormat("\"g5\": [{0}],", string.Join(",", g5.Select(x => "\"" + x + "\"")));
            sb.AppendFormat("\"g6\": [{0}],", string.Join(",", g6.Select(x => "\"" + x + "\"")));
            sb.AppendFormat("\"g7\": [{0}],", string.Join(",", g7.Select(x => "\"" + x + "\"")));
            sb.AppendFormat("\"de\": {0},", de);
            sb.AppendFormat("\"dau\": {0},", dau);
            sb.AppendFormat("\"duoi\": {0},", duoi);
            sb.AppendFormat("\"tong\": {0},", tong);
            sb.AppendFormat("\"loCount\": {0},", loList.Count);
            sb.AppendFormat("\"loList\": [{0}]", string.Join(",", loList));
            sb.Append("}");

            return sb.ToString();
        }

        public static string SaveDailyLotteryRecord(string jsonPayload)
        {
            // Trích xuất các trường từ payload
            string dateStr = ExtractJsonString(jsonPayload, "date");
            string dayOfWeek = ExtractJsonString(jsonPayload, "dayOfWeek");
            string gdb = ExtractJsonString(jsonPayload, "gdb").PadLeft(5, '0');
            string g1 = ExtractJsonString(jsonPayload, "g1").PadLeft(5, '0');

            var g2 = ExtractJsonArray(jsonPayload, "g2");
            var g3 = ExtractJsonArray(jsonPayload, "g3");
            var g4 = ExtractJsonArray(jsonPayload, "g4");
            var g5 = ExtractJsonArray(jsonPayload, "g5");
            var g6 = ExtractJsonArray(jsonPayload, "g6");
            var g7 = ExtractJsonArray(jsonPayload, "g7");

            if (string.IsNullOrEmpty(dateStr) || string.IsNullOrEmpty(gdb) || gdb.Length < 5)
            {
                return "{\"success\": false, \"message\": \"Dữ liệu không hợp lệ. Vui lòng kiểm tra lại GĐB và Ngày.\"}";
            }

            int de = int.Parse(gdb.Substring(gdb.Length - 2));
            int dau = de / 10;
            int duoi = de % 10;
            int tong = (dau + duoi) % 10;

            var all27Lo = new List<int>();
            Action<string> addLo = delegate(string p)
            {
                if (!string.IsNullOrEmpty(p) && p.Length >= 2)
                {
                    int v;
                    if (int.TryParse(p.Substring(p.Length - 2), out v)) all27Lo.Add(v);
                }
            };

            addLo(gdb);
            addLo(g1);
            foreach (var p in g2) addLo(p);
            foreach (var p in g3) addLo(p);
            foreach (var p in g4) addLo(p);
            foreach (var p in g5) addLo(p);
            foreach (var p in g6) addLo(p);
            foreach (var p in g7) addLo(p);

            if (all27Lo.Count != 27)
            {
                return string.Format("{{\"success\": false, \"message\": \"Số lượng lô giải mã không đủ 27 giải (hiện có {0} giải).\"}}", all27Lo.Count);
            }

            var uniqueLo = new HashSet<int>(all27Lo);
            var sortedUnique = uniqueLo.OrderBy(x => x).Select(x => x.ToString("D2")).ToList();
            string sortedUniqueLoStr = string.Join(",", sortedUnique);
            string all27LoStr = string.Join(",", all27Lo.Select(x => x.ToString("D2")));
            string combinedDigits = gdb + g1 + string.Join("", g2) + string.Join("", g3) + string.Join("", g4) + string.Join("", g5) + string.Join("", g6) + string.Join("", g7);

            lock (dataLock)
            {
                // Kiểm tra xem ngày này đã tồn tại chưa
                if (records.Any(r => r.Date == dateStr))
                {
                    return string.Format("{{\"success\": false, \"message\": \"Kết quả kỳ ngày {0} đã tồn tại trong CSDL!\"}}", dateStr);
                }

                int newId = records.Count > 0 ? records[records.Count - 1].Id + 1 : 1;

                string csvLine = string.Format("{0},{1},\"{2}\",{3},{4},\"{5}\",\"{6}\",\"{7}\",\"{8}\",\"{9}\",\"{10}\",{11},{12},{13},{14},{15},\"{16}\",\"{17}\",\"{18}\"",
                    newId, dateStr, dayOfWeek, gdb, g1,
                    string.Join(",", g2), string.Join(",", g3), string.Join(",", g4), string.Join(",", g5), string.Join(",", g6), string.Join(",", g7),
                    de, dau, duoi, tong, uniqueLo.Count, sortedUniqueLoStr, all27LoStr, combinedDigits);

                File.AppendAllText(csvFilePath, "\r\n" + csvLine, Encoding.UTF8);

                // Tải lại toàn bộ dữ liệu vào bộ nhớ
                records = Program.LoadData(csvFilePath);

                // Tự động đồng bộ lên GitHub (nếu có cấu hình GITHUB_TOKEN & GITHUB_REPO)
                PushToGitHubAsync();
            }

            return string.Format("{{\"success\": true, \"message\": \"Đã nạp thành công kết quả ngày {0}! Tổng số kỳ lịch sử hiện tại: {1} kỳ.\", \"totalRecords\": {1}}}", dateStr, records.Count);
        }

        private static void TrySyncFromGitHubOnStartup()
        {
            try
            {
                string ghRepo = Environment.GetEnvironmentVariable("GITHUB_REPO");
                string ghToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
                string ghBranch = Environment.GetEnvironmentVariable("GITHUB_BRANCH");
                if (string.IsNullOrEmpty(ghBranch)) ghBranch = "main";

                if (string.IsNullOrEmpty(ghRepo)) return;

                ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // Tls12
                Console.WriteLine(">> [GITHUB SYNC] Đang kiểm tra CSDL mới nhất từ GitHub: " + ghRepo);

                string url = string.Format("https://raw.githubusercontent.com/{0}/{1}/data/lottery_master.csv", ghRepo, ghBranch);
                using (var wc = new WebClient())
                {
                    wc.Encoding = Encoding.UTF8;
                    wc.Headers.Add("User-Agent", "TidsEngine-Web");
                    if (!string.IsNullOrEmpty(ghToken))
                    {
                        wc.Headers.Add("Authorization", "token " + ghToken);
                    }
                    string remoteCsv = wc.DownloadString(url);
                    if (!string.IsNullOrEmpty(remoteCsv))
                    {
                        var remoteLines = remoteCsv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                        var localLines = File.Exists(csvFilePath) ? File.ReadAllLines(csvFilePath) : new string[0];
                        if (remoteLines.Length > localLines.Length)
                        {
                            File.WriteAllText(csvFilePath, remoteCsv, Encoding.UTF8);
                            lock (dataLock)
                            {
                                records = Program.LoadData(csvFilePath);
                            }
                            Console.WriteLine(string.Format(">> [GITHUB SYNC] Tải thành công CSDL mới từ GitHub ({0} bản ghi).", records.Count));
                        }
                        else
                        {
                            Console.WriteLine(">> [GITHUB SYNC] CSDL cục bộ đã đồng nhất với GitHub.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[GITHUB SYNC CẢNH BÁO] Không thể nạp dữ liệu từ GitHub: " + ex.Message);
            }
        }

        public static void PushToGitHubAsync()
        {
            ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    string ghRepo = Environment.GetEnvironmentVariable("GITHUB_REPO");
                    string ghToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
                    string ghBranch = Environment.GetEnvironmentVariable("GITHUB_BRANCH");
                    if (string.IsNullOrEmpty(ghBranch)) ghBranch = "main";

                    if (string.IsNullOrEmpty(ghRepo) || string.IsNullOrEmpty(ghToken)) return;

                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; // Tls12
                    Console.WriteLine(">> [GITHUB SYNC] Đang tự động lưu CSDL lên GitHub: " + ghRepo);

                    // 1. Lấy SHA của file hiện tại trên GitHub
                    string apiUrl = string.Format("https://api.github.com/repos/{0}/contents/data/lottery_master.csv?ref={1}", ghRepo, ghBranch);
                    string sha = "";
                    try
                    {
                        using (var wc = new WebClient())
                        {
                            wc.Encoding = Encoding.UTF8;
                            wc.Headers.Add("User-Agent", "TidsEngine-Web");
                            wc.Headers.Add("Authorization", "Bearer " + ghToken);
                            wc.Headers.Add("Accept", "application/vnd.github.v3+json");
                            string json = wc.DownloadString(apiUrl);
                            sha = ExtractJsonString(json, "sha");
                        }
                    }
                    catch { }

                    // 2. Đọc file local và Base64
                    byte[] fileBytes;
                    lock (dataLock)
                    {
                        fileBytes = File.ReadAllBytes(csvFilePath);
                    }
                    string base64Content = Convert.ToBase64String(fileBytes);

                    // 3. Gửi PUT commit lên GitHub API
                    using (var wc = new WebClient())
                    {
                        wc.Encoding = Encoding.UTF8;
                        wc.Headers.Add("User-Agent", "TidsEngine-Web");
                        wc.Headers.Add("Authorization", "Bearer " + ghToken);
                        wc.Headers.Add("Accept", "application/vnd.github.v3+json");
                        wc.Headers.Add("Content-Type", "application/json");

                        string payload = string.Format("{{\"message\": \"Auto update lottery result {0}\", \"content\": \"{1}\", \"sha\": \"{2}\", \"branch\": \"{3}\"}}",
                            DateTime.Now.ToString("yyyy-MM-dd HH:mm"), base64Content, sha, ghBranch);

                        wc.UploadString(string.Format("https://api.github.com/repos/{0}/contents/data/lottery_master.csv", ghRepo), "PUT", payload);
                        Console.WriteLine(">> [GITHUB SYNC] ĐÃ ĐỒNG BỘ THÀNH CÔNG LÊN GITHUB REPO VĨNH VIỄN!");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[GITHUB SYNC LỖI ĐẨY DỮ LIỆU]: " + ex.Message);
                }
            });
        }

        private static string GetVietnameseDayOfWeek(DateTime dt)
        {
            switch (dt.DayOfWeek)
            {
                case DayOfWeek.Monday: return "Thứ hai";
                case DayOfWeek.Tuesday: return "Thứ ba";
                case DayOfWeek.Wednesday: return "Thứ tư";
                case DayOfWeek.Thursday: return "Thứ năm";
                case DayOfWeek.Friday: return "Thứ sáu";
                case DayOfWeek.Saturday: return "Thứ bảy";
                case DayOfWeek.Sunday: return "Chủ nhật";
                default: return "Thứ hai";
            }
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        }

        private static string ExtractJsonString(string json, string key)
        {
            var m = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\"([^\"]*)\"");
            return m.Success ? m.Groups[1].Value : "";
        }

        private static List<string> ExtractJsonArray(string json, string key)
        {
            var res = new List<string>();
            var m = Regex.Match(json, "\"" + key + "\"\\s*:\\s*\\[([^\\]]*)\\]");
            if (m.Success)
            {
                var inner = m.Groups[1].Value;
                var items = inner.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in items)
                {
                    string clean = item.Trim().Trim('\"', '\'');
                    if (!string.IsNullOrEmpty(clean)) res.Add(clean);
                }
            }
            return res;
        }
    }
}
