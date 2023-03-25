using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using NUnit.Framework;
using SixLabors.ImageSharp;

namespace ImageClassifier.Tests;

public class ImageClassifierTests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void ClassifyImageTest()
    {
        //Arrange
        var classesImagesNames = new List<string> { "forest.jpg", "road.jpg", "field.jpg", "field2.jpg" };
        var colors = new List<Color>
        {
            Color.Blue, Color.Orange, Color.Red, Color.Black
        };


        //Act
        var predictions = Classifier.Launch("Phantom_article_cloudy_photo_nadir_cropped.jpg", classesImagesNames, colors, 51);


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
}