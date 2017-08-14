using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using HashCopier.Model;

namespace HashCopier.Controller
{
    public class RecursiveLister
    {
        public async Task<Dictionary<string, string>> GetFileList(string path, Progress<double> progress,
            int bufferedSize = 1048576)
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

        public async Task<List<FileListModel>> GetFileListModel(Dictionary<string, string> srcHashList, Dictionary<string, string> destHashList)
        {
            var modelList = new List<FileListModel>();

            await Task.Run(() =>
            {
                foreach (var dictionaryItem in srcHashList)
                {
                    if (!destHashList.ContainsKey(dictionaryItem.Key))
                    {
                        modelList.Add(new FileListModel()
                        {
                            Name = dictionaryItem.Value,
                            Status = "Can copy",
                            StatusColor = new SolidColorBrush(Colors.Green)
                        });
                    }
                    else
                    {
                        modelList.Add(new FileListModel()
                        {
                            Name = dictionaryItem.Value,
                            Status = "Duplicated",
                            StatusColor = new SolidColorBrush(Colors.DarkOrange)
                        });
                    }
                }
            });

            return modelList;
        }
    }
}
