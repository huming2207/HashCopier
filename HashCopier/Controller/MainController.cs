using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using HashCopier.Model;

namespace HashCopier.Controller
{
    public class MainController
    {
        public async Task<Dictionary<string, List<string>>> GetFileList(string path, int bufferedSize = 1048576)
        {
            // Declare file list
            var fileList = new Dictionary<string, List<string>>();
            var shaHasher = new SHA256Managed();


            foreach (var filePath in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                await Task.Run(() =>
                {
                    try
                    {
                        var bufferedStream = new BufferedStream(new FileStream(filePath, FileMode.Open), bufferedSize);

                        // Add to file list, remove "-" so that it helps me easier to debug.
                        var hashString = BitConverter.ToString(shaHasher.ComputeHash(bufferedStream)).Replace("-", "");

                        // Detect if this file exists, if not, create a path list and add it
                        List<string> filePathList;

                        if (fileList.TryGetValue(hashString, out filePathList))
                        {
                            filePathList.Add(filePath);
                        }
                        else
                        {
                            filePathList = new List<string> {filePath};
                            fileList.Add(hashString, filePathList);
                        }

                        bufferedStream.Dispose();
                    }
                    catch (UnauthorizedAccessException uacEexcption)
                    {
                        Debug.WriteLine("[ERROR] Permission denied @ {0}\n", filePath);
                        Debug.WriteLine(uacEexcption.StackTrace);
                    }
                    catch (IOException ioException)
                    {
                        MessageBox.Show($"File occupied by another program @ {filePath}", "ERROR", MessageBoxButton.OK,
                            MessageBoxImage.Error);
                        Debug.WriteLine("[ERROR] File occupied by another process @ {0}\n", filePath);
                        Debug.WriteLine(ioException.StackTrace);
                        return;
                    }
                });
            }
            

            return fileList;
        }

        public static async Task GetFileListModel(Dictionary<string, List<string>> srcHashList, Dictionary<string, List<string>> destHashList,
            string destDir, IProgress<double> progress, bool moveFile = false)
        {
            var modelList = new List<FileListModel>();
            var fileListIndex = 0d;

            /*
                Get a relative root directory

                    e.g. If there are some files:
                        C:\Users\Jackson\test1.txt
                        C:\Users\Jackson\archive.zip
                        C:\Users\Jackson\private.torrent
                        C:\Users\Jackson\doge.jpg
                        C:\Users\Jackson\baidu.exe
                        C:\Users\Jackson\schoolwork\lecture.pdf
                        C:\Users\Jackson\Thumbs.db

                    Then the relative root directory is:
                        C:\Users\Jackson


                Ref: https://stackoverflow.com/questions/8578110/how-to-extract-common-file-path-from-list-of-file-paths-in-c-sharp
             
             */

            // Convert a List<List<string>> to List<string> by using LINQ
            var filePathList = srcHashList.Values.ToList().SelectMany(list => list).ToList();

            // Select the base path (see reference URL)
            var matchingChars = 
                from len in Enumerable.Range(0, filePathList.Min(s => s.Length)).Reverse()
                let possibleMatch = filePathList.First().Substring(0, len)
                where filePathList.All(f => f.StartsWith(possibleMatch))
                select possibleMatch;
            var rootDir = Path.GetDirectoryName(matchingChars.First());

            // Invoke the GUI, bind the list to GUI control
            MainWindow.MainWindowToInvoke.FileListLoader = modelList;
 
            // Iterate the file from the list
            foreach (var dictionaryItem in srcHashList)
            {
                foreach (var path in dictionaryItem.Value)
                {
                    if (!destHashList.ContainsKey(dictionaryItem.Key))
                    {
                        modelList.Add(new FileListModel
                        {
                            Name = path,
                            Status = "Copied",
                            StatusColor = new SolidColorBrush(Colors.Green)
                        });

                        // Force refresh UI from the binding (otherwise InvalidOperationException will throw)
                        // Ref: https://stackoverflow.com/questions/32254676/invalidoperationexception-an-itemscontrol-is-inconsistent-with-its-items-source
                        MainWindow.MainWindowToInvoke.ForceRefresh();

                        // Recursively create a directory first, before do any copying tasks.
                        var relativeDir = Path.GetDirectoryName(path).Replace(rootDir, "");
                        if (!relativeDir.StartsWith(@"\")) { relativeDir = @"\" + relativeDir; }
                        var destPath = destDir + relativeDir;
                        Directory.CreateDirectory(destPath);

                        // Do copying task
                        await AsyncCopier.Copy(path,
                            destPath + Path.GetFileName(path));

                        // If this method runs in move file mode, then delete the file after copying it.
                        if (moveFile) { File.Delete(path); }
                    }
                    else
                    {
                        modelList.Add(new FileListModel
                        {
                            Name = path,
                            Status = "Duplicated",
                            StatusColor = new SolidColorBrush(Colors.DarkOrange)
                        });

                        // Force refresh UI from the binding (otherwise InvalidOperationException will throw)
                        // Ref: https://stackoverflow.com/questions/32254676/invalidoperationexception-an-itemscontrol-is-inconsistent-with-its-items-source
                        MainWindow.MainWindowToInvoke.ForceRefresh();
                    }

                    progress.Report(((++fileListIndex) / srcHashList.Count) * 100);
                }
            }
        }
    }
}