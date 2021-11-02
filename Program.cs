using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;

namespace EditSumoQuery
{
    class Program
    {
        const string MS_PATH = @"D:\git\ops-sumo-query\d9_ops\micro_services";
        const string MISSING_NO_ERROR_COUNT_LOG_FILE_PATH = @"Logs\No Error Count services.txt";
        const string CHANGED_LOG_FILE_PATH = @"Logs\Changed Files.txt";
        const string VERNDOR = "aws";
        static void Main()
        {
            // Go over all aws micro services -> the files that contaiin "error_count" -> add new filter in specific place.
            List<string> directoriesWithNoErrorCount = new List<string>();

            if (!Directory.Exists(MISSING_NO_ERROR_COUNT_LOG_FILE_PATH))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(MISSING_NO_ERROR_COUNT_LOG_FILE_PATH));
            }

            foreach (var directory in Directory.GetDirectories(MS_PATH))
            {
                if (!directory.ToLower().Contains(VERNDOR)) continue;
                var files = Directory.GetFiles(Path.Combine(MS_PATH, directory)).Where(x => x.Contains("error_count")).ToList();
                if (files.Count == 0) directoriesWithNoErrorCount.Add(directory);
                else
                {
                    foreach (string file in files)
                    {
                        AddFilterInFile(file);
                    }
                }
            }
            directoriesWithNoErrorCount.ForEach(x => File.AppendAllText(MISSING_NO_ERROR_COUNT_LOG_FILE_PATH, x + Environment.NewLine));
        }

        static void AddFilterInFile(string path)
        {
            string file = File.ReadAllText(path);
            const string QUERY_PREFIX = "\"queryText\": ";
            const string NEW_FILTER = " \\nAND !RegionDisabledException\\n";
            if(file.Contains(NEW_FILTER))
            {
                File.AppendAllText(CHANGED_LOG_FILE_PATH, path +
                Environment.NewLine + NEW_FILTER+": was already exist." +
                Environment.NewLine + "*******************************************************************************" + Environment.NewLine);
                return;
            }
            // Three options for the index to insert our new filter: 
            // 1. After a prefered line.
            // 2. Before comments of other filters.
            // 3. Before the pipe signal.
            const string PREFERRED_LINE_TO_GO_AFTER = "Unable to read data from the transport connection\\\"";
            const string LINE_TO_GO_BEFORE1 = "/*";
            const string LINE_TO_GO_BEFORE2 = "|";

            int insertIndex;
            if (file.Contains(PREFERRED_LINE_TO_GO_AFTER))
            {
                insertIndex = file.IndexOf(PREFERRED_LINE_TO_GO_AFTER) + PREFERRED_LINE_TO_GO_AFTER.Length;

            }
            else if (file.IndexOf(LINE_TO_GO_BEFORE1) != -1 && file.IndexOf(LINE_TO_GO_BEFORE1) < file.IndexOf(LINE_TO_GO_BEFORE2))
            {
                insertIndex = file.IndexOf(LINE_TO_GO_BEFORE1) - Environment.NewLine.Length;
            }
            else
            {
                insertIndex = file.IndexOf(LINE_TO_GO_BEFORE2) - Environment.NewLine.Length;
            }

            // Insert the new filter
            string newFile = file.Insert(insertIndex, NEW_FILTER);
            File.WriteAllText(path, newFile);
            // Print into the log the new query (take from beginning until a little bit after our new filter)
            int startIndexOfQery = newFile.IndexOf(QUERY_PREFIX) + QUERY_PREFIX.Length;
            int endIndexOfQery = newFile.IndexOf(NEW_FILTER) + NEW_FILTER.Length + 20;
            string query = newFile.Substring(startIndexOfQery, endIndexOfQery);
            query = query.Replace("\\n", Environment.NewLine);
            File.AppendAllText(CHANGED_LOG_FILE_PATH, path +
                Environment.NewLine + query +
                Environment.NewLine + "*******************************************************************************" +
                Environment.NewLine);

            return;
        }

    }
}
