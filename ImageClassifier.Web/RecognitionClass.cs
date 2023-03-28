using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ImageClassifier.Web;

/// <summary>
/// Клас розпізнавання
/// </summary>
public class RecognitionClass
{
    public Image<Rgba32> Image { get; }


    public string Color { get; set; }

    public string? DataUrl { get; set; }

    public RecognitionClass(Image<Rgba32> image, string name = null)
    {
        Name = name;
        Image = image;
    }

    /// <summary>
    /// Ім'я класу розпізнавання
    /// </summary>

    public string Name { get; set; }
}