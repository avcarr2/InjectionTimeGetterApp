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
        static void Main(string[] args)
        {
            string[] files = Directory.GetFiles(args[0], "*.raw", SearchOption.AllDirectories);
            string[] fileNamesLite = files.Select(Path.GetFileNameWithoutExtension).ToArray(); 
            List<double[]> injectionTimes = new(); 

            for (int i = 0; i < files.Length; i++)
            {
                List<MsDataScan> scansList = LoadFile(files[i]);
                ProcessFile(scansList, out double[] ms1Inj);
                injectionTimes.Add(ms1Inj);
            }

            string output = CreateOutput(injectionTimes, fileNamesLite);
            File.WriteAllText(Path.Combine(args[0], "InjectionTimes.txt"), output);
        }

        static string CreateOutput(List<double[]> dataList, string[] files)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(string.Join("\t", files)); 
            int maxLength = dataList.Select(k => k.Length).Max();

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
            return ThermoRawFileReader.LoadAllStaticData(filePath)
                .GetAllScansList();
        }
        static void ProcessFile(List<MsDataScan> scansList, out double[] ms1Inj)
        {
            ms1Inj = GetInjectionTimesMs1(scansList);
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
        static double[] GetInjectionTimesMs1(List<MsDataScan> scan)
        {
            return scan.Where(i => i.MsnOrder == 1)
                .Select(i => i.InjectionTime.Value).ToArray(); 
        }

        static double[] GetMs1Rt(List<MsDataScan> scan)
        {
            return scan.Where(i => i.MsnOrder == 1)
                .Select(i => i.RetentionTime).ToArray(); 
        }

    }
}