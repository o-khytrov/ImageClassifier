using System;
using System.Collections.Generic;
using NUnit.Framework;
using SixLabors.ImageSharp;

namespace ImageClassifier.Tests;

public class Tests
{
    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        var classesImagesNames = new List<string> { "forest.jpg", "road.jpg", "field.jpg", "field2.jpg" };
        var colors = new List<Color>
        {
            Color.Blue, Color.Orange, Color.Red, Color.Black
        };


        Classifier.launch("Phantom_article_cloudy_photo_nadir_cropped.jpg", classesImagesNames, colors, 51);
        Assert.True(true);
    }
}