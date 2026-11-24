using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

class Program
{
    static void Main()
    {
        string csvPath = Path.Combine("data", "lottery_master.csv");
        var lines = File.ReadAllLines(csvPath, Encoding.UTF8).ToList();
        
        int newId = 8604;
        string date = "2026-11-15";
        string dayOfWeek = "\"Chủ nhật\"";
        string gdb = "61225";
        string g1 = "83438";
        string g2 = "\"28193,64772\"";
        string g3 = "\"16283,41264,95726,61780,63680,38697\"";
        string g4 = "\"4403,8645,2183,5423\"";
        string g5 = "\"9123,8986,5739,7536,5309,2485\"";
        string g6 = "\"440,582,769\"";
        string g7 = "\"44,12,25,65\"";

        int de = 25;
        int dau = 2;
        int duoi = 5;
        int tong = (dau + duoi) % 10; // 7

        // 27 Lô in order of draw:
        string[] loRaw = new string[] {
            "25", // GDB
            "38", // G1
            "93", "72", // G2
            "83", "64", "26", "80", "80", "97", // G3
            "03", "45", "83", "23", // G4
            "23", "86", "39", "36", "09", "85", // G5
            "40", "82", "69", // G6
            "44", "12", "25", "65" // G7
        };
        
        var uniqueLo = loRaw.Distinct().OrderBy(x => x).ToList();
        int uniqueCount = uniqueLo.Count;
        string uniqueLoStr = "\"" + string.Join(",", uniqueLo) + "\"";
        string allLoStr = "\"" + string.Join(",", loRaw) + "\"";

        string rawDigits = "\"" + gdb + g1 
            + g2.Replace("\"", "").Replace(",", "") 
            + g3.Replace("\"", "").Replace(",", "") 
            + g4.Replace("\"", "").Replace(",", "") 
            + g5.Replace("\"", "").Replace(",", "") 
            + g6.Replace("\"", "").Replace(",", "") 
            + g7.Replace("\"", "").Replace(",", "") + "\"";

        string newLine = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},{13},{14},{15},{16},{17},{18}",
            newId, date, dayOfWeek, gdb, g1, g2, g3, g4, g5, g6, g7, de, dau, duoi, tong, uniqueCount, uniqueLoStr, allLoStr, rawDigits);

        lines.Add(newLine);
        File.WriteAllLines(csvPath, lines, Encoding.UTF8);
        Console.WriteLine("Added record " + newId + " successfully to " + csvPath);

        // Update metadata.json
        string metaPath = Path.Combine("data", "metadata.json");
        if (File.Exists(metaPath))
        {
            string meta = File.ReadAllText(metaPath, Encoding.UTF8);
            meta = meta.Replace("\"total_records\": 8603", "\"total_records\": 8604");
            meta = meta.Replace("\"end_date\": \"2026-11-14\"", "\"end_date\": \"2026-11-15\"");
            File.WriteAllText(metaPath, meta, Encoding.UTF8);
            Console.WriteLine("Updated metadata.json to 8604 records, end_date = 2026-11-15");
        }
    }
}
