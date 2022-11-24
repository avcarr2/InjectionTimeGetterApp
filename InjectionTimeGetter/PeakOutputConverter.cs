using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Easy.Common;
using static System.Formats.Asn1.AsnWriter;
using ThermoFisher.CommonCore.Data.Business;
using MathNet.Numerics.LinearAlgebra;

namespace InjectionTimeGetter
{
    public class PeakOutputConverter
    {
        public static DataTable CompiledPeptidesInfo = new DataTable();

        private static string[] DataColumnNames = {
            "File Name",
            "Base Sequence",
            "Full Sequence",
            "Peptide Monoisotopic Mass",
            "Scan Retention Time",
            "PrecursorCharge",
            "Protein Accession"
        };
        private static string[] PeaksColumnNames =
        {
            // peptide is the full sequence with post-translational modifications
            "Peptde",
            "-10lgP", 
            // use this for monoisotopic mass 
            "Mass",
            "Length",
            "ppm",
            "m/z", 
            // charge
            "Z", 
            // use this for the scan retention time
            "RT",
            "Area",
            "Fraction",
            "Id",
            "Scan",
            "from Chimera",
            "Source File", 
            // use for accession
            "Accession",
            "PTM",
            "AScore",
            "Found By"
        }; 
        // need to convert .csv to .tsv 

        // To get the final .csv output name:
        // 1. need to go through the list of subfolders and get the peptides.csv file, 
        // 2. then concatentate the subfolder name with the peptides.csv file name 
        public static string[] GetSubdirectoryFolderNames(string parentDirectoryPath)
        {
            DirectoryInfo directoryInfo = new DirectoryInfo(parentDirectoryPath);
            FileSystemInfo[] fsi = directoryInfo.GetFileSystemInfos();

            return fsi.Select(i => i.Name).ToArray(); 
        }
        public static string[] GetFinalFileNames(string[] folderNames)
        {
            string[] finalStrings = new string[folderNames.Length];
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < folderNames.Length; i++)
            { 
                finalStrings[i] = string.Join("_", finalStrings[i], "peptides.csv"); 
            }
            return finalStrings; 
        }
      
        public static DataTable ImportCsvData(string pathToPeptidesCsv)
        {
            // import data
            DataTable dt = new(); 
            using (StreamReader sr = new StreamReader(pathToPeptidesCsv))
            {
                string[] headers = sr.ReadLine().Split(","); 
                foreach(string header in headers)
                {
                    dt.Columns.Add(header); 
                }
                while (!sr.EndOfStream)
                {
                    string[] rows = sr.ReadLine().Split(",");
                    DataRow dr = dt.NewRow(); 
                    for(int i = 0; i < headers.Length; i++)
                    {
                        dr[i] = rows[i]; 

                    }
                    dt.Rows.Add(dr); 
                }
            }
            return dt; 
        }
        public static void CreateCompiledPeptidesInfo()
        {
            foreach(string colName in DataColumnNames)
            {
                CompiledPeptidesInfo.Columns.Add(colName);
            }
        }
        public static void AddImportedDataToMaster(DataTable idData, string sourceFile)
        {
            // for each row, get the data from the relevant columns 
            idData.AsEnumerable()
                .AsParallel()
                .ForAll(i => ConvertPeaksFileAndAddRow(sourceFile, i)); 
        }
        private static void ConvertPeaksFileAndAddRow(string sourceFile, DataRow peaksDataRow)
        {

            string fullSequence = peaksDataRow.Field<string>("Peptide");
            string baseSequence = GetBaseSequence(fullSequence);

            int mass = peaksDataRow.Field<int>("Mass");
            int charge = peaksDataRow.Field<int>("Z");
            double rt = peaksDataRow.Field<double>("RT");
            string accession = peaksDataRow.Field<string>("Accession");
            // source file is passed. 

            DataRow dr = CompiledPeptidesInfo.NewRow();
            dr[0] = sourceFile;
            dr[1] = baseSequence;
            dr[2] = fullSequence;
            dr[3] = mass;
            dr[4] = rt;
            dr[5] = charge;
            dr[6] = accession;

            CompiledPeptidesInfo.Rows.Add(dr);
        }

        private static string GetBaseSequence(string sequence)
        {
            return new string(sequence.Where(char.IsLetter).ToArray()); 
        }

        public static void PeakOutputConverter_Main(string[] args)
        {
            CreateCompiledPeptidesInfo();

            // args[0] = paths of the Peaks output .csv files 
            // args[1] = the path to the original .raw files
            // search for all paths 
            string parentFolder = args[0]; 
            string[] identificationfiles = Directory.GetFiles(args[0], ".*db search.csv", SearchOption.AllDirectories);
            // get the name of the folder containing the file. Match the directory name to the raw file name
            
            string[] rawFiles = Directory.GetFiles(args[1], "*.raw", SearchOption.AllDirectories);

            // match the output file to the raw file. 
            List<DataTable> dataTableList = new();
            Dictionary<string, DataTable> idFileToDataTableDict = new(); 
            Parallel.ForEach(identificationfiles, i =>
            {
                idFileToDataTableDict.Add(i, ImportCsvData(i));
            });

            var idFileToRawDict = CreateIdentificationToRawDictionary(parentFolder, identificationfiles, rawFiles);
            foreach(KeyValuePair<string, DataTable> kvp in idFileToDataTableDict)
            {
                AddImportedDataToMaster(kvp.Value, idFileToRawDict[kvp.Key]); 
            }
            WriteMasterDtToTsv(Path.Combine(parentFolder, "combinedResults.tsv")); 
        }
        public static void WriteMasterDtToTsv(string finalPath)
        {
            StringBuilder sb = new();

            // write column headers
            List<string> colNames = new(); 
            foreach(DataColumn col in CompiledPeptidesInfo.Columns)
            {
                colNames.Add(col.ColumnName); 
            }
            sb.AppendLine(string.Join("\t", colNames)); 

            // write the rest of the data 
            foreach(DataRow rows in CompiledPeptidesInfo.Rows)
            {
                foreach (DataColumn col in CompiledPeptidesInfo.Columns)
                {
                    sb.AppendFormat("{0}", rows[col]); 
                }
                sb.AppendLine(); 
            }
             File.WriteAllText(finalPath, sb.ToString()); 
        }
        public static Dictionary<string, string> CreateIdentificationToRawDictionary(string parentDirectory, string[] idFiles, string[] rawFiles)
        {
            var resultsDictionary = new Dictionary<string, string>();
            // get the containing folder name alone 
            string[] folderNames = idFiles.Select(i => Path.GetRelativePath(parentDirectory, i)).ToArray();
            for(int i = 0; i < idFiles.Length; i++)
            {
                string rawFile = rawFiles.Where(j => j.Contains(folderNames[i])).First();
                resultsDictionary[idFiles[i]] = rawFile; 

            }
            return resultsDictionary; 
        }
    }
}
