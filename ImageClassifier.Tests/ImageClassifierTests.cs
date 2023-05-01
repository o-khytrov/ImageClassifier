using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace ImageClassifier.Tests;

public class ImageClassifierTests
{
    private static readonly List<string> Colors = new()
    {
        "#38e819", "#1a4ca5", "#47a7e1", "#859b61", "#9d0f52", "#b8dc63", "#5ff930", "#21c424", "#c0a9eb"
    };

    private const string ResultDateFormat = "ddMMyy_HHmm";

    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void ClassifyImageTest()
    {
        //Arrange
        const int areaSize = 51;
        const string imageName = "Phantom_article_cloudy_photo_nadir_cropped.jpg";
        var classesImagesNames = new List<string> { "forest.jpg", "road.jpg", "field.jpg", "field2.jpg" };
        var colors = new List<Color>
        {
            Color.Blue, Color.Orange, Color.Red, Color.Black
        };
        var classesImages = classesImagesNames.Select(Image.Load<Rgba32>).ToList();
        var classifier = new Classifier();
        var trainingResults = new List<TrainingEventArgs>();
        classifier.TrainingIterationCompleted += (sender, args) => { trainingResults.Add(args); };

        classifier.Train(classesImages, areaSize);

        //Act
        var image = Image.Load(imageName);
        var predictions = classifier.ClassifyImage(image);

        AddPredictionsToImage(predictions, image, colors, areaSize, imageName);

        BuildCharts(trainingResults);

        //Assert
        var expectedPredictions =
            JsonConvert.DeserializeObject<List<AreaPrediction>>(File.ReadAllText("ExpectedPredictions.json"));
        Assert.AreEqual(expectedPredictions.Count, predictions.Count);

        for (var i = 0; i < predictions.Count; i++)
        {
            Assert.AreEqual(expectedPredictions[i].X, predictions[i].X);
            Assert.AreEqual(expectedPredictions[i].Y, predictions[i].Y);
            Assert.AreEqual(expectedPredictions[i].Class, predictions[i].Class);
        }
    }

    private void BuildCharts(List<TrainingEventArgs> trainingResults)
    {
        File.WriteAllText("training.json", JsonConvert.SerializeObject(trainingResults));
    }

    private void AddPredictionsToImage(List<AreaPrediction> predictions, Image image, List<Color> colors, int areaSize,
        string imageName)
    {
        var brushes = Colors.Select(x => Brushes.Solid(Color.ParseHex(x).WithAlpha(0.35f))).ToList();

        foreach (var prediction in predictions)
        {
            if (prediction.Class > -1)
            {
                // image.Mutate(x => x.DrawText(prediction.Class.ToString(), _font, colors[prediction.Class],
                //   new PointF(prediction.X + areaSize / 2, prediction.Y + areaSize / 2)));

                var yourPolygon = new Rectangle(prediction.X, prediction.Y, areaSize, areaSize);
                image.Mutate(x => x.Fill(brushes[prediction.Class], yourPolygon));
            }
        }

        image.Save(File.OpenWrite($"result_{imageName}_{DateTime.Now.ToString(ResultDateFormat)}.png"), new PngEncoder());
    }
}