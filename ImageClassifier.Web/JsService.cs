using Microsoft.JSInterop;

namespace ImageClassifier.Web;

public class JsService
{
    public delegate void SelectImage(ImageData imageData); // delegate

    public static event SelectImage? ImageAreaSelected; // event 
    public static event SelectImage? SourceImageSelected; // event 

    [JSInvokable]
    public static void SelectArea(int width, int height, byte[] data)
    {
        ImageAreaSelected?.Invoke(new ImageData() { Width = width, Height = height, Data = data });
    }
    
    
    [JSInvokable]
    public static void AddSourceImage(int width, int height, byte[] data)
    {
        SourceImageSelected?.Invoke(new ImageData() { Width = width, Height = height, Data = data });
    }
}