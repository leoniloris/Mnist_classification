using ConvNetSharp;
using ConvNetSharp.Serialization;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AForge.Imaging;
using AForge.Imaging.Filters;
using System.Threading;
using System.Drawing.Imaging;
using System.Runtime.Serialization;
using System.Xml;

namespace CharClassif
{
    /// <summary>
    /// classe com coisas úteis e teste aleatórios.
    /// </summary>
    
    class useful
    {

        public static void teste()
        {
            Console.WriteLine("\n\nA imagem deve estar na pasta debug, com o nome 'char.bmp' (já deve ter uma lá)");
            var net2 = SerializationExtensions.FromJSON(File.ReadAllText("best"));
            byte[] imgbytes = null;
            var newImage = MnistReader.ScaleImage(new Bitmap(@"char.bmp"), 24, 24);

            var x = new Volume(newImage.Width, newImage.Height, 1, 0.0);
            for (var i = 0; i < newImage.Width; i++)
                for (var j = 0; j < newImage.Width; j++)
                    x.Weights[j + i * newImage.Width] = (newImage.GetPixel(j, i).B + newImage.GetPixel(j, i).R + newImage.GetPixel(j, i).G) / 3;

            if (File.Exists("oi.txt")) { File.Delete("oi.txt"); }
            for (int i = 0; i < x.Weights.Length; i++)
                using (StreamWriter file = new StreamWriter(@"oi.txt", true))
                    file.WriteLine(x.Weights[i].ToString());

            net2.Forward(x);

            var yhat = net2.GetPrediction();
            Console.WriteLine("label prevista = {0}",yhat);

        }
        public static void segment_n_rec()
        {
            var image=new Bitmap(@"a.bmp");
            image=image.Clone(new Rectangle(0, 0, image.Width, image.Height), PixelFormat.Format8bppIndexed);
            byte[] imgbytes = new byte[image.Height* image.Width];
            #region parallel grey image conv
            var tasks = new Task[Environment.ProcessorCount];
            int oi = -1;
            for (var ai = 0; ai < image.Width; ai++)
            {
                for (int taskNumber = 0; taskNumber < Environment.ProcessorCount; taskNumber++)
                    tasks[taskNumber] = Task.Factory.StartNew(() =>
                    {
                        int j = Interlocked.Increment(ref oi);
                        while (j < image.Width)
                        {
                            imgbytes[j + ai * image.Width] = (byte)(((int)image.GetPixel(j, ai).B + (int)image.GetPixel(j, ai).R + (int)image.GetPixel(j, ai).G) / 3);
                            j = Interlocked.Increment(ref oi);
                        }
                    });
                Task.WaitAll(tasks);
            }
            #endregion
            new OtsuThreshold().ApplyInPlace(image);
            // check threshold value


            var net2 = SerializationExtensions.FromJSON(File.ReadAllText("best"));
            var newImage = MnistReader.ScaleImage(new Bitmap(@"C:\Users\leoni.win7-PC\Desktop\Nova pasta\a.bmp"), 24, 24);

            var x = new Volume(newImage.Width, newImage.Height, 1, 0.0);
            for (var i = 0; i < newImage.Width; i++)
                for (var j = 0; j < newImage.Width; j++)
                    x.Weights[j + i * newImage.Width] = (newImage.GetPixel(j, i).B + newImage.GetPixel(j, i).R + newImage.GetPixel(j, i).G) / 3;

            if (File.Exists("oi.txt")) { File.Delete("oi.txt"); }
            for (int i = 0; i < x.Weights.Length; i++)
                using (StreamWriter file = new StreamWriter(@"oi.txt", true))
                    file.WriteLine(x.Weights[i].ToString());

            net2.Forward(x);

            var yhat = net2.GetPrediction();
        }
        static void CustomParallelFalseSharing(double[] array, double factor)
        {
            var degreeOfParallelism = Environment.ProcessorCount;

            var tasks = new Task[degreeOfParallelism];

            int i = -1;

            for (int taskNumber = 0; taskNumber < degreeOfParallelism; taskNumber++)
            {
                tasks[taskNumber] = Task.Factory.StartNew(
                    () =>
                    {
                        int j = Interlocked.Increment(ref i);
                        while (j < array.Length)
                        {
                            array[j] = array[j] * factor;
                            j = Interlocked.Increment(ref i);
                        }
                    });
            }

            Task.WaitAll(tasks);
        }
        public static byte[] WriteObject<T>(T thingToSave)
        {
            Console.WriteLine("Serializing an instance of the object.");
            byte[] bytes;
            using (var stream = new MemoryStream())
            {
                var serializer = new DataContractSerializer(typeof(T));
                serializer.WriteObject(stream, thingToSave);
                bytes = new byte[stream.Length];
                stream.Position = 0;
                stream.Read(bytes, 0, (int)stream.Length);
            }
            return bytes;

        }
        public static T ReadObject<T>(byte[] data)
        {
            Console.WriteLine("Deserializing an instance of the object.");

            T deserializedThing = default(T);

            using (var stream = new MemoryStream(data))
            using (var reader = XmlDictionaryReader.CreateTextReader(stream, new XmlDictionaryReaderQuotas()))
            {
                var serializer = new DataContractSerializer(typeof(T));

                // Deserialize the data and read it from the instance.
                deserializedThing = (T)serializer.ReadObject(reader, true);
            }
            return deserializedThing;
        }


    }
}
