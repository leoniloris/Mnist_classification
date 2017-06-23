# Mnist_classification
Short extract of c# code in which we train a CNN for MNIST classification

##### Everything is uploaded (included the already processed MNIST datasets, they're in 'CharClassif\bin\Debug' with the name of  dataset_testing.dat and dataset_training.dat)

That’s a Visual Studio 2017 project, so, to open it, you should have visual studio and the latest version of .net installed.
Just run the application and the console will guide you.

here, you can 
* Train the network
* Test the network (using the network 'best', in 'CharClassif\bin\Debug', which I trained)

In 'Program.cs' you'll find a conventional definition for a CNN network:
```
net = new Net();
net.AddLayer(new InputLayer(24, 24, 1)); //tamanho que eu escalei as imagens
net.AddLayer(new ConvLayer(5, 5, 20) { Stride = 1, Pad = 2, Activation = Activation.Relu });
net.AddLayer(new PoolLayer(2, 2) { Stride = 2 });
net.AddLayer(new ConvLayer(5, 5, 35) { Stride = 1, Pad = 2, Activation = Activation.Relu });
net.AddLayer(new PoolLayer(3, 3) { Stride = 3 });
net.AddLayer(new FullyConnLayer(50));
net.AddLayer(new SoftmaxLayer(33));
```
Pretty straightforward.
## Details

For details about a CNN, each layer, its computations and other results, head to one of my repositories [parallel_computing_2](https://github.com/leoniloris/parallel_computing_2). You'll find a Python implementation of a CNN for a [Kaggle](https://www.kaggle.com) competition, as well as a [tecnical article](https://github.com/leoniloris/parallel_computing_2/blob/master/Relatorio_Trabalho02.pdf) named 'Relatorio_Trabalho02.pdf' which details several aspects of a CNN. 

### Framework
Credits for doing the hardwork of implementing each computation goes to ** ConvNetSharp ** 

