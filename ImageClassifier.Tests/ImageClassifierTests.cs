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
    private Font _font;
    private const string ResultDateFormat = "ddMMyy_HHmm";

    public ImageClassifierTests()
    {
        FontCollection collection = new();
        var family = collection.Add("/home/ok/OpenSans-Regular.ttf");
        _font = family.CreateFont(12, FontStyle.Italic);
    }

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

        Classifier.Setup(classesImages, areaSize);

        //Act
        var image = Image.Load(imageName);
        var predictions = Classifier.ClassifyImage(image);

        AddPredictionsToImage(predictions, image, colors, areaSize, imageName);

        //Assert
        var expectedPredictions =
            JsonConvert.DeserializeObject<List<FramePrediction>>(File.ReadAllText("ExpectedPredictions.json"));
        Assert.AreEqual(expectedPredictions.Count, predictions.Count);

        for (var i = 0; i < predictions.Count; i++)
        {
            Assert.AreEqual(expectedPredictions[i].X, predictions[i].X);
            Assert.AreEqual(expectedPredictions[i].Y, predictions[i].Y);
            Assert.AreEqual(expectedPredictions[i].Class, predictions[i].Class);
        }
    }

    private void AddPredictionsToImage(List<FramePrediction> predictions, Image image, List<Color> colors, int areaSize,
        string imageName)
    {
        var brushes = new List<IBrush>
        {
            Brushes.Solid(Color.Blue.WithAlpha(0.1f)),
            Brushes.Solid(Color.Orange.WithAlpha(0.1f)),
            Brushes.Solid(Color.Red.WithAlpha(0.1f)),
            Brushes.Solid(Color.Black.WithAlpha(0.1f)),
        };
        
        foreach (var prediction in predictions)
        {
            if (prediction.Class > -1)
            {
                image.Mutate(x => x.DrawText(prediction.Class.ToString(), _font, colors[prediction.Class],
                    new PointF(prediction.X + areaSize / 2, prediction.Y + areaSize / 2)));
                
                //var yourPolygon = new Rectangle(prediction.X, prediction.Y, areaSize, areaSize);
                //image.Mutate(x => x.Fill(brushes[prediction.Class], yourPolygon));  
            }
        }

        image.Save(File.OpenWrite($"result_{imageName}_{DateTime.Now.ToString(ResultDateFormat)}.png"), new PngEncoder());
    }
}