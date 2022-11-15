using System.Diagnostics;
using System.IO;
using System.Text;
using IO.ThermoRawFileReader;
using MassSpectrometry;

namespace InjectionTimeGetter
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string[] files = Directory.GetFiles(args[0], "*.raw", SearchOption.AllDirectories);
            string[] outputFiles = CreateOutputFileNames(files, ".txt");
            int j = 1; 
            for(int i = 0; i < files.Length; i++)
            {
                List<MsDataScan> scansList = LoadFile(files[i]); 
                ProcessFile(scansList, out double?[] ms1Inj, out double[] ms1rt);
                string data = WriteDataToString(ms1Inj, ms1rt);
                WriteStringToTxtFile(outputFiles[i], data);
                Console.WriteLine("Completed file {0} of {1}", j, files.Length);
                j++; 
            }
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
        static void ProcessFile(List<MsDataScan> scansList, out double?[] ms1Inj, out double[] ms1rt)
        {
            ms1Inj = GetInjectionTimesMs1(scansList);
            ms1rt = GetMs1Rt(scansList);
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

        static void WriteStringToTxtFile(string filePath, string data)
        {
            File.WriteAllText(filePath, data);
        }

        static double?[] GetInjectionTimesMs1(List<MsDataScan> scan)
        {
            return scan.Where(i => i.MsnOrder == 1)
                .Select(i => i.InjectionTime).ToArray(); 
        }

        static double[] GetMs1Rt(List<MsDataScan> scan)
        {
            return scan.Where(i => i.MsnOrder == 1)
                .Select(i => i.RetentionTime).ToArray(); 
        }

    }
}