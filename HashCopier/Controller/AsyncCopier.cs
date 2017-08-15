using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HashCopier.Controller
{
    public class AsyncCopier
    {
        public static async Task Copy(string srcPath, string destPath, int bufferSize = 16777216)
        {
            var srcDir = new DirectoryInfo(srcPath);
            var destDir = new DirectoryInfo(destPath);

            foreach (var file in srcDir.EnumerateDirectories())
            {
                using (var inStream = File.Open(srcPath, FileMode.Open))
                {
                    var relativePath = srcDir.FullName.Replace(srcDir.FullName, destDir.FullName);

                    using (var outStream = File.Create(relativePath))
                    {
                        // Run async copy
                        await inStream.CopyToAsync(outStream);
                    }
                }
            }
        }
    }
}
