using System;
using System.IO;

namespace GZipTest
{
    public static class ValidationExtension
    {
        /// <summary>
        /// Проверка аргументов на правильность
        /// </summary>
        /// <param name="args">Аргументы</param>
        public static void ArgsValidation(this string[] args)
        {
            
            if (args.Length == 0 || args.Length > 3) throw new Exception("Use: compress/decompress [Source File] [Destination File]");

            if (args[0].ToLower() != "compress" && args[0].ToLower() != "decompress") throw new Exception("The first parameter passed to the command \"compress\" or \"decompress\".");
            
            if (args[1].Length == 0) throw new Exception("Do not specify the name of the source file.");

            if (!File.Exists(args[1])) throw new Exception("Source file not found.");

            FileInfo _fileIn = new FileInfo(args[1]);
            FileInfo _fileOut = new FileInfo(args[2]);

            if (!_fileIn.Exists) throw new FileNotFoundException($"File {_fileIn} not found!");

            if (_fileIn.Length <= 0) throw new Exception($"File size can not be equal to zero!");

            if (args[1] == args[2]) throw new Exception("The resulting file cannot be a source.");

            if (_fileIn.Extension == ".gz" && args[0] == "compress") throw new Exception("The source file is an archive GZip.");

            if (_fileOut.Extension != ".gz" && _fileOut.Exists) throw new Exception("The resulting file should have the extension GZip(.gz).");

            if (_fileIn.Extension != ".gz" && args[0] == "decompress") throw new Exception("The original file has an extension different from GZip(.gz).");

            if (args[2].Length == 0) throw new Exception("Not specified name of the output file.");
        }
    }
}
