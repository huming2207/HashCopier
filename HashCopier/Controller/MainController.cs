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

                    // Add to file list
                    fileList.Add(BitConverter.ToString(shaHasher.ComputeHash(bufferedStream)), filePath);
                    bufferedStream.Dispose();
                }
            });

            return fileList;
        }

        public async Task GetFileListModel(Dictionary<string, string> srcHashList, Dictionary<string, string> destHashList,
            string destDir, IProgress<double> progress)
        {
            var modelList = new List<FileListModel>();
            var fileListIndex = 0d;
            var asyncCopier = new AsyncCopier();

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

            // Invoke the GUI
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

                    // Recursively create a directory first, before do any copying tasks.
                    var relativeDir = Path.GetDirectoryName(dictionaryItem.Key).Replace(rootDir, "");
                    if (!relativeDir.StartsWith(@"\")) { relativeDir = @"\" + relativeDir; }
                    var destPath = destDir + relativeDir;
                    Directory.CreateDirectory(destPath);

                    // Do copying task
                    await AsyncCopier.Copy(dictionaryItem.Key, 
                        destPath + @"\" +  Path.GetFileName(dictionaryItem.Key));

                    // Report index
                }
                else
                {
                    modelList.Add(new FileListModel
                    {
                        Name = dictionaryItem.Value,
                        Status = "Duplicated",
                        StatusColor = new SolidColorBrush(Colors.DarkOrange)
                    });
                }

                fileListIndex++;
            }
        }
    }
}