using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Media;
using HashCopier.Model;

namespace HashCopier.Controller
{
    public class MainController
    {
        public async Task<Dictionary<string, string>> GetFileList(string path, int bufferedSize = 1048576)
        {
            // Declare file list
            var fileList = new Dictionary<string, string>();
            var shaHasher = new SHA256Managed();

            await Task.Run(() =>
            {
                foreach (var filePath in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    var bufferedStream = new BufferedStream(new FileStream(filePath, FileMode.Open), bufferedSize);

                    // Add to file list, remove "-" so that it helps me easier to debug.
                    fileList.Add(BitConverter.ToString(shaHasher.ComputeHash(bufferedStream)).Replace("-", ""), filePath);
                    bufferedStream.Dispose();
                }
            });

            return fileList;
        }

        public async Task GetFileListModel(Dictionary<string, string> srcHashList, Dictionary<string, string> destHashList,
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
            var filePathList = srcHashList.Values.ToList();
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
                if (!destHashList.ContainsKey(dictionaryItem.Key))
                {
                    modelList.Add(new FileListModel
                    {
                        Name = dictionaryItem.Value,
                        Status = "Copied",
                        StatusColor = new SolidColorBrush(Colors.Green)
                    });

                    // Force refresh UI from the binding (otherwise InvalidOperationException will throw)
                    // Ref: https://stackoverflow.com/questions/32254676/invalidoperationexception-an-itemscontrol-is-inconsistent-with-its-items-source
                    MainWindow.MainWindowToInvoke.ForceRefresh();

                    // Recursively create a directory first, before do any copying tasks.
                    var relativeDir = Path.GetDirectoryName(dictionaryItem.Value).Replace(rootDir, "");
                    if (!relativeDir.StartsWith(@"\")) { relativeDir = @"\" + relativeDir; }
                    var destPath = destDir + relativeDir;
                    Directory.CreateDirectory(destPath);

                    // Do copying task
                    await AsyncCopier.Copy(dictionaryItem.Value, 
                        destPath +  Path.GetFileName(dictionaryItem.Value));

                    // If this method runs in move file mode, then delete the file after copying it.
                    if(moveFile) { File.Delete(dictionaryItem.Value); }
                }
                else
                {
                    modelList.Add(new FileListModel
                    {
                        Name = dictionaryItem.Value,
                        Status = "Duplicated",
                        StatusColor = new SolidColorBrush(Colors.DarkOrange)
                    });

                    // Force refresh UI from the binding (otherwise InvalidOperationException will throw)
                    // Ref: https://stackoverflow.com/questions/32254676/invalidoperationexception-an-itemscontrol-is-inconsistent-with-its-items-source
                    MainWindow.MainWindowToInvoke.ForceRefresh();
                }

                progress.Report(((++fileListIndex)/srcHashList.Count)*100);
            }
        }
    }
}