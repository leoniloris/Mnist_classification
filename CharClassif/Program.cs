using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using ConvNetSharp;
using ConvNetSharp.Layers;
using ConvNetSharp.Training;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml.Serialization;
using System.Xml;
using ConvNetSharp.Serialization;
using System.Runtime.Serialization.Json;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;

namespace CharClassif
{

    internal class Program
    {
        
        private const int BatchSize = 3000;
        private readonly Random random = new Random();
        private readonly CircularBuffer<double> trainAccWindow = new CircularBuffer<double>(100);
        private readonly CircularBuffer<double> valAccWindow = new CircularBuffer<double>(100);
        private readonly CircularBuffer<double> wLossWindow = new CircularBuffer<double>(100);
        private readonly CircularBuffer<double> xLossWindow = new CircularBuffer<double>(100);
        private Net net;
        private int stepCount;
        private List<MnistEntry> testing;
        private AdadeltaTrainer trainer;
        private List<MnistEntry> training;
        private int trainingCount = BatchSize;
        private const string folder = @"..\..\..\char\"; // para que saia da pasta 'debug' encontre os dados de treino
        private void MnistTrain()
        {
            // carrega os chars
            Console.WriteLine("carregando datasets...");
            if (File.Exists("dataset_training.dat") || File.Exists("dataset_testing.dat"))
            {   // se já tiver os datasets tratados, carrega eles
                training= useful.ReadObject<List<MnistEntry>>(File.ReadAllBytes("dataset_training.dat"));
                testing = useful.ReadObject<List<MnistEntry>>(File.ReadAllBytes("dataset_testing.dat"));
            }
            else
            {   // caso contrário, carrega a partir das imagens
                training = MnistReader.Load(folder, true);
                testing = MnistReader.Load(folder, false);
                File.WriteAllBytes("dataset_training.dat", useful.WriteObject<List<MnistEntry> >(training)  );
                File.WriteAllBytes("dataset_testing.dat", useful.WriteObject<List<MnistEntry>>(testing));
            }
            Random rnd = new Random();
            training = training.OrderBy(x => rnd.Next()).ToList();
            testing = testing.OrderBy(x => rnd.Next()).ToList();

            if (training.Count == 0 || testing.Count == 0)
            {
                Console.WriteLine("ajuste o diretório dos arquivos de treino/teste.");
                Console.ReadKey();
                return;
            }
            // cria uma CNN simples
            net = new Net();
            net.AddLayer(new InputLayer(24, 24, 1)); //tamanho que eu escalei as imagens
            net.AddLayer(new ConvLayer(5, 5, 20) { Stride = 1, Pad = 2, Activation = Activation.Relu });
            net.AddLayer(new PoolLayer(2, 2) { Stride = 2 });
            net.AddLayer(new ConvLayer(5, 5, 35) { Stride = 1, Pad = 2, Activation = Activation.Relu });
            net.AddLayer(new PoolLayer(3, 3) { Stride = 3 });
            net.AddLayer(new FullyConnLayer(50));
            net.AddLayer(new SoftmaxLayer(33));

            // este é o meu otimizador
            // ver depois http://int8.io/comparison-of-optimization-techniques-stochastic-gradient-descent-momentum-adagrad-and-adadelta/#AdaDelta
            trainer = new AdadeltaTrainer(net)
            {
                BatchSize = 20,
                L2Decay = 0.001,
            };
            Console.WriteLine("CNN treinando... aperte uma tecla para parar");
            // o passo  
            do
            {
                var sample = SampleTrainingInstance();

                // // só pra ver no matlab a imagem (no matlab eu uso o script imag.m)
                //if (File.Exists("oi.txt")) { File.Delete("oi.txt"); }
                //for (int i = 0; i < sample.Volume.Weights.Length; i++)
                //    using (StreamWriter file = new StreamWriter(@"oi.txt", true))
                //        file.WriteLine(((sample.Volume.Weights[i] * 255.0)).ToString());
                Step(sample);
            } while (!Console.KeyAvailable);

        }
        private void Step(Item sample)
        {
            var x = sample.Volume; 
            var y = sample.Label;  // pego o rótulo e a imagem
            if (sample.IsValidation) // vejo se é de validação ou treino
            {
                // a estimação do erro é feita com os dados de entrada 'x' e a previsão boa ou ruim é 'valAcc'
                net.Forward(x);
                var yhat = net.GetPrediction();
                var valAcc = yhat == y ? 1.0 : 0.0;
                valAccWindow.Add(valAcc);
                return;
            }

            trainer.Train(x, y);
            var lossx = trainer.CostLoss;
            var lossw = trainer.L2DecayLoss;
            //  erro e a função perda
            var prediction = net.GetPrediction();
            var trainAcc = prediction == y ? 1.0 : 0.0;
            xLossWindow.Add(lossx);
            wLossWindow.Add(lossw);
            trainAccWindow.Add(trainAcc);

            if (stepCount % 200 == 0)
            {
                if (xLossWindow.Count == xLossWindow.Capacity)
                {
                    var xa = xLossWindow.Items.Average();
                    var xw = wLossWindow.Items.Average();
                    var loss = xa + xw;
                    Console.WriteLine("Perda: {0} precisão no treiono: {1}% precisão do teste: {2}%", loss,Math.Round(trainAccWindow.Items.Average() * 100.0, 2), Math.Round(valAccWindow.Items.Average() * 100.0, 2));
                    Console.WriteLine("{0} exemplos vistos. tempo pro fwd: {1}ms tempo pro bckw: {2}ms", stepCount,Math.Round(trainer.ForwardTime.TotalMilliseconds, 2),Math.Round(trainer.BackwardTime.TotalMilliseconds, 2));
                    // AQUI, salvo A REDE ATUAl, POSSO ESCOLHER MAIS TARDE qual usar
                    // a melhor rede atual está salva com o nome de 'best' na pasta debug
                    File.WriteAllText("rede"+ stepCount.ToString(), SerializationExtensions.ToJSON(net)); 
                }
            }

            if (stepCount % 1000 == 0)
            {
                TestPredict();
            }
            stepCount++;
        }
        private void TestPredict()
        {
            for (var i = 0; i < 50; i++)
            {
                List<Item> sample = SampleTestingInstance();
                var y = sample[0].Label; // ground truth label

                // forward prop it through the network
                var average = new Volume(1, 1, 10, 0.0);
                var n = sample.Count;
                for (var j = 0; j < n; j++)
                {
                    var a = net.Forward(sample[j].Volume);
                    average.AddFrom(a);
                }

                var predictions = average.Weights.Select((w, k) => new { Label = k, Weight = w }).OrderBy(o => -o.Weight);
            }
        }
        private Item SampleTrainingInstance()
        {
            var n = random.Next(trainingCount);
            var entry = training[n];

            // load more batches over time
            if (stepCount % 5000 == 0 && stepCount > 0)
            {
                trainingCount = Math.Min(trainingCount + BatchSize, training.Count);
            }

            // Create volume from image data
            var x = new Volume(24, 24, 1, 0.0);

            for (var i = 0; i < 24; i++)
            {
                for (var j = 0; j < 24; j++)
                {
                    x.Weights[j + i *24] = entry.Image[j + i * 24] / 255.0;
                }
            }

            x = x.Augment(24);

            return new Item { Volume = x, Label = entry.Label, IsValidation = n % 10 == 0 };
        }
        private List<Item> SampleTestingInstance()
        {
            var result = new List<Item>();
            var n = random.Next(testing.Count);
            var entry = testing[n];

            // Create volume from image data
            var x = new Volume(24, 24, 1, 0.0);

            for (var i = 0; i < 24; i++)
            {
                for (var j = 0; j < 24; j++)
                {
                    x.Weights[j + i * 24] = entry.Image[j + i * 24] / 255.0;
                }
            }

            for (var i = 0; i < 4; i++)
            {
                result.Add(new Item { Volume = x.Augment(24), Label = entry.Label });
            }

            return result;
        }
        private static void Main(string[] args)
        {
            char charr='0';
            while ((charr != 't')&& (charr != 's') ) {
                Console.WriteLine("Treinar a rede ou apenas testar o último melhor treino? \n t - para treinar \n s - para testar a rede 'best' na pasta debug");
                charr=Console.ReadKey().KeyChar;
                if (charr == 't')
                {
                    var program = new Program();
                    program.MnistTrain();
                }
                else if (charr == 's')
                {
                    useful.teste();
                }


           }

            Console.WriteLine("\n\npressione enter para sair");
            Console.ReadKey();
        }

        private class Item
        {
            public Volume Volume { get; set; }
            public int Label { get; set; }
            public bool IsValidation { get; set; }
        }
    }
}