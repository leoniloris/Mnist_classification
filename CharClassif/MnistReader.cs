using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading;

namespace CharClassif
{
    public static class MnistReader
    {
        public static List<MnistEntry> Load(string folder, bool train)
        {
            List<int> label = LoadLabels(folder, train);
            List<byte[]> images = LoadImages(folder, train);

            if (label.Count == 0 || images.Count == 0)
            {
                return new List<MnistEntry>();
            }

            return label.Select((t, i) => new MnistEntry {Label = t, Image = images[i]}).ToList();
        }
        private static int ReverseBytes(int v)
        {
            byte[] intAsBytes = BitConverter.GetBytes(v);
            Array.Reverse(intAsBytes);
            return BitConverter.ToInt32(intAsBytes, 0);
        }
        private static List<int> LoadLabels(string folder, bool train)
        {
            int train_count = 750, test_count=250;
            var result = new List<int>();
            DirectoryInfo root = new DirectoryInfo(folder);
            var ae = root.GetDirectories();
            foreach (DirectoryInfo dr in root.GetDirectories())
                for (int i = 0; i < (train ? train_count : test_count); i++)
                    result.Add(int.Parse(dr.Name));
            
            return result;
        }
        public static List<byte[]> LoadImages(string folder, bool train)
        {
            
            var result = new List<byte[]>();
            int train_count = 750, test_count = 250;
            DirectoryInfo root = new DirectoryInfo(folder);
            foreach (DirectoryInfo dr in root.GetDirectories())
            {
                var files = dr.GetFiles();
                for (int i = (train ? 0 : train_count); i < (train ? train_count : test_count + train_count); i++)
                {
                    byte[] imgbytes = new byte[24 * 24];
                    var newImage = MnistReader.ScaleImage(new Bitmap(files[i].FullName), 24, 24);

                    for (var ai = 0; ai < newImage.Width; ai++)
                        for (var j = 0; j < newImage.Width; j++)
                            imgbytes[j + ai * newImage.Width] = (byte)(((int)newImage.GetPixel(j, ai).B + (int)newImage.GetPixel(j, ai).R + (int)newImage.GetPixel(j, ai).G) / 3);


                    //var tasks = new Task[Environment.ProcessorCount];
                    //int oi = -1;
                    //for (var ai = 0; ai < newImage.Width; ai++)
                    //{ 
                    //    for (int taskNumber = 0; taskNumber < Environment.ProcessorCount; taskNumber++)
                    //    tasks[taskNumber] = Task.Factory.StartNew(() =>
                    //    {
                    //        int j = Interlocked.Increment(ref oi);
                    //        while (j < newImage.Width)
                    //        {
                    //            imgbytes[j + ai * newImage.Width]= (byte)(((int)newImage.GetPixel(j, ai).B + (int)newImage.GetPixel(j, ai).R + (int)newImage.GetPixel(j, ai).G)/ 3);
                    //            j = Interlocked.Increment(ref oi);
                    //        }
                    //    });
                    //    Task.WaitAll(tasks);
                    //}




                    //if (File.Exists("oi.txt")) { File.Delete("oi.txt"); }
                    //for (int asd = 0; asd < imgbytes.Length; asd++)
                    //    using (StreamWriter file = new StreamWriter(@"oi.txt", true))
                    //        file.WriteLine(imgbytes[asd].ToString());

                    result.Add(imgbytes);
                    
                }
            }

            return result;
        }
        public static Bitmap ScaleImage(Bitmap image, int maxWidth, int maxHeight)
        {
            var newWidth = (int)(maxWidth);
            var newHeight = (int)(maxHeight);

            var newImage = new Bitmap(newWidth, newHeight);

            using (var graphics = Graphics.FromImage(newImage))
                graphics.DrawImage(image, 0, 0, newWidth, newHeight);

            return newImage;
        }

    }
}