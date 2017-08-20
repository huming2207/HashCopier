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
            using (var inStream = File.Open(srcPath, FileMode.Open))
            {
                using (var outStream = File.Create(destPath))
                {
                    // Run async copy
                    await inStream.CopyToAsync(outStream);
                }
            }
        }
    }
}
