using Microsoft.JSInterop;

namespace ImageClassifier.Web;

public class JsService
{
    public delegate void SelectImage(ImageData imageData); // delegate

    public static event SelectImage? ImageSelected; // event 

    [JSInvokable]
    public static void AddRecognitionClass(int width, int height, byte[] data)
    {
        ImageSelected?.Invoke(new ImageData() { Width = width, Height = height, Data = data });
    }
}