namespace ImageClassifier;

/// <summary>
/// Клас розпізнавання
/// </summary>
public class RecognitionClass
{
    public byte[] Data { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public string? DataUrl { get; set; }

    public RecognitionClass(ImageData imageData, string name = null)
    {
        Name = name;
        Data = imageData.Data;
        Width = imageData.Width;
        Height = imageData.Height;
    }

    /// <summary>
    /// Ім'я класу розпізнавання
    /// </summary>

    public string Name { get; set; }
}