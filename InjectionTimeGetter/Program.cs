using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using IO.ThermoRawFileReader;
using MassSpectrometry;
using System.Data; 

namespace InjectionTimeGetter
{
    internal class Program
    {
        /// <summary>
        /// Args[0] is the option for program to run. Args[1] is the parent . 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            int optionNumber = Convert.ToInt32(args[0]);

            if(optionNumber == 3)
            {
                PeakOutputConverter.PeakOutputConverter_Main(args[1..]);
                return; 
            }

            string[] files = Directory.GetFiles(args[1], "*.raw", SearchOption.AllDirectories);
            string[] fileNamesLite = files.Select(Path.GetFileNameWithoutExtension).ToArray(); 
            List<double[]> injectionTimes = new();
            List<IEnumerable<double>> injectionTimesBag = new(); 
            int msOrder = Convert.ToInt32(args[1]);
            
            int fileNumber = 1; 
            Stopwatch sw = new Stopwatch();
            foreach (string file in files)
            {
                Console.WriteLine("Processing file {0} of {1}", fileNumber, files.Length);
                sw.Restart(); 
                List<MsDataScan> scansList = LoadFile(file);
                ProcessFile(scansList, msOrder, out IEnumerable<double> ms1Inj);
                injectionTimesBag.Add(ms1Inj);
                sw.Stop(); 
                Console.WriteLine("Processed file in {0} ms", sw.ElapsedMilliseconds);
                fileNumber++; 
            }
            string output = CreateOutput(injectionTimesBag.ToList(), fileNamesLite);
            File.WriteAllText(Path.Combine(args[0], "InjectionTimes.txt"), output);
        }

        static string CreateOutput(List<IEnumerable<double>> dataList, string[] files)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Join("\t", files)); 
            int maxLength = dataList.Select(k => k.Count()).Max();

            for (int i = 0; i < maxLength - 1; i++)
            {
                StringBuilder sbTemp = new StringBuilder();
                for (int j = 0; j < dataList.Count; j++)
                {
                    sbTemp.Append(dataList[j].ElementAtOrDefault(i) + "\t"); 
                }

                sb.AppendLine(sbTemp.ToString()); 
            }
            return sb.ToString(); 
        }

        static string[] CreateOutputFileNames(string[] files, string finalExtension)
        {
            string[] finalPaths = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                string baseFile = Path.GetFileNameWithoutExtension(files[i]);
                finalPaths[i] = string.Join("", baseFile, finalExtension);
            }
            return finalPaths; 
        }
        static List<MsDataScan> LoadFile(string filePath)
        {
            return ThermoRawFileReader.LoadAllStaticData(filePath,null,8)
                .GetAllScansList();
        }
        static void ProcessFile(List<MsDataScan> scansList, int msOrder, out IEnumerable<double> ms1Inj)
        {
            ms1Inj = GetInjectionTimesMs1(scansList, msOrder);
        }
        static IEnumerable<double> ProcessFile(List<MsDataScan> scansList, int msOrder)
        {
            return GetInjectionTimesMs1(scansList, msOrder);
        }

        static string WriteDataToString(double?[] ms1Inj, double[] ms1rt)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Ms1 Retention Time" + "\t" + "Ms1 Injection Time"); 
            for (int i = 0; i < ms1Inj.Length; i++)
            {
                sb.AppendLine(string.Join("\t", ms1rt[i], ms1Inj[i])); 
            }
            return sb.ToString(); 
        }
        static IEnumerable<double> GetInjectionTimesMs1(List<MsDataScan> scan, int msOrder)
        {
            return scan.AsParallel()
                .Where(i => i.MsnOrder == msOrder)
                .Select(i => i.InjectionTime.Value); 
        }

        static double[] GetMs1Rt(List<MsDataScan> scan)
        {
            return scan.Where(i => i.MsnOrder == 1)
                .Select(i => i.RetentionTime).ToArray(); 
        }

    }
}