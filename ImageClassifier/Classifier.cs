using SixLabors.Fonts;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;

namespace ImageClassifier;

public static class Classifier
{
    private static String RESULT_NAME_TEMPLATE = "";
    private static string RESULT_DATE_FORMAT = "ddMMyy_HHmm";

    public static List<FramePrediction> Launch(String imageName, List<String> classesImagesNames, List<Color> colors,
        int areaSize)
    {
        /* Отримуємо значення кольорів пікселів для зображень кожного класу */
        var classesValues = GetClassesValues(classesImagesNames);

        /* Знаходимо оптимальне значення дельти для СКД */
        var delta = getOptimalDelta(classesValues);

        /* Переводимо значення у бінарний вигляд та знаходимо еталонні вектори кожного класу */
        var classBinaryMatrices = new List<int[][]>();
        var classVectors = new int[classesValues.Count()][];

        var limitVector = getLimitVector(classesValues[0]);
        for (var i = 0; i < classesValues.Count(); i++)
        {
            int[][] classBinaryMatrix = getBinaryMatrix(classesValues[i], limitVector, delta);
            classBinaryMatrices.Add(classBinaryMatrix);
            classVectors[i] = getVectorFromBinaryMatrix(classBinaryMatrix);
        }

        /* Знаходимо радіуси контейнера кожного класу */
        var radii = getRadii(classVectors, classBinaryMatrices);
        Console.WriteLine("Optimal radii: " + radii);

        /* Класифікуємо зображення (екзамен) */
        return ClassifyImage(imageName, colors, areaSize, delta, limitVector, classVectors, radii);
    }

    /**
     * Метод для перетворення значень кольорів зображення у бінарний вигляд відносно СКД
     *
     * @param values      значення кольорів зображення, яке необхідно перетворити у бінарну матрицю
     * @param limitVector вектор, який задає СКД
     * @param delta       значення дельти для СКД
     * @return бінарну матрицю зображення
     */
    private static int[][] getBinaryMatrix(int[][] values, List<Double> limitVector, int delta)
    {
        var binaryMatrix = new int[values.Length][];
        for (var i = 0; i < values.Length; i++)
        {
            binaryMatrix[i] = new int[values[0].Length];
            for (var j = 0; j < values[0].Length; j++)
            {
                if (values[i][j] >= limitVector[j] - delta && values[i][j] <= limitVector[j] + delta)
                {
                    binaryMatrix[i][j] = 1;
                }
                else
                {
                    binaryMatrix[i][j] = 0;
                }
            }
        }

        return binaryMatrix;
    }


    /**
     * Метод для отримання вектора, який задає СКД
     *
     * @param values значення кольорів базового класу
     * @return вектор, який задає СКД
     */
    private static List<Double> getLimitVector(int[][] values)
    {
        var limitVector = new List<double>();
        for (var i = 0; i < values[0].Length; i++)
        {
            double sum = 0;
            foreach (var row in values)
            {
                sum += row[i];
            }

            limitVector.Add(sum / values.Length);
        }

        return limitVector;
    }

    /**
     * Метод для класифікації областей зображення
     *
     * @param imageName    ім'я зображення для класифікації
     * @param colors       масив кольорів для класів
     * @param areaSize     розмір області для класифікації (повинна дорівнювати стороні квадрата зображення класу)
     * @param delta        значення дельти для СКД
     * @param limitVector  вектор, який задає СКД
     * @param classVectors еталонні вектори кожного класу
     * @param radii        радіуси контейнерів кожного класу
     * @throws IOException
     */
    private static List<FramePrediction> ClassifyImage(string imageName,
        List<Color> colors,
        int areaSize,
        int delta,
        List<Double> limitVector,
        int[][] classVectors,
        List<int> radii)
    {
        var result = new List<FramePrediction>();
        FontCollection collection = new();
        var family = collection.Add("/home/ok/OpenSans-Regular.ttf");
        var font = family.CreateFont(12, FontStyle.Italic);
        using var image = Image.Load(File.OpenRead(imageName));
        /* Проходимо по всім квадратним областям зі стороною areaSize у зображенні, яке потрібно класифікувати */
        for (var i = 0; i < image.Width; i += areaSize)
        {
            for (var j = 0; j < image.Height; j += areaSize)
            {
                try
                {
                    /* Вирізаємо квадратну область, перетворюємо її у бінарну матрицю та проводимо екзамен */
                    //BufferedImage crop = image.getSubimage(i, j, areaSize, areaSize);
                    var crop = image.Clone(x => x.Crop(new Rectangle(i, j, areaSize, areaSize))).CloneAs<Rgba32>();
                    int[][] cropValues = ImgToArray(crop);
                    int[][] cropBinaryMatrix = getBinaryMatrix(cropValues, limitVector, delta);
                    var classNumber = -1;
                    double classValue = 0;
                    /* Проводимо екзамен області відносно кожного класу */
                    for (var k = 0; k < classVectors.Length; k++)
                    {
                        var res = exam(classVectors[k], radii[k], cropBinaryMatrix);
                        /* Якщо значення після екзамену більше за поточне значення, то відносимо область до цього класу */
                        if (res > classValue)
                        {
                            classNumber = k;
                            classValue = res;
                        }
                    }

                    /* Якщо вдалося класифікувати область, то помічаємо її на вихідному зображенні відповідним номером та кольором */
                    if (classNumber != -1)
                    {
                        image.Mutate(x => x.DrawText(classNumber.ToString(), font, colors[classNumber],
                            new PointF(i + areaSize / 2, j + areaSize / 2)));
                    }

                    result.Add(new FramePrediction
                        { X = i, Y = j, Class = classNumber });
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }

        /* Зберігаємо зображення після класифікації */
        image.Save(File.OpenWrite($"result_{imageName}_{DateTime.Now.ToString(RESULT_DATE_FORMAT)}.png"),
            new PngEncoder()); //Replace Png encoder with the file format of choice
        return result;
    }

    private static int getOptimalDelta(List<int[][]> classesValues)
    {
        var optimalDelta = 0;
        double optimalDeltaCriterionValue = 0;
        /* Шукаємо оптимальне значення у інтервалі [1, 120] */
        Console.WriteLine("Calculation of the optimal delta");
        Console.WriteLine("Delta | criterion value | criterion value in working area");
        for (var delta = 1; delta <= 120; delta++)
        {
            /* Розраховуємо вектор, який задає СКД, бінарні матриці та еталонні вектори кожного класу */
            List<int[][]> classBinaryMatrices = new List<int[][]>();
            int[][] classVectors = new int[classesValues.Count][];
            var limitVector = getLimitVector(classesValues[0]);
            for (var i = 0; i < classesValues.Count; i++)
            {
                int[][] classBinaryMatrix = getBinaryMatrix(classesValues[i], limitVector, delta);
                classBinaryMatrices.Add(classBinaryMatrix);
                classVectors[i] = GetVectorFromBinaryMatrix(classBinaryMatrix);
            }

            /* Шукаємо сусідів класів */
            int[][] pairs = makePairs(classVectors);
            List<List<Tuple<Double, Boolean>>> criterionValues = new List<List<Tuple<Double, Boolean>>>();
            /* Для кожного класу знаходимо значення критеріїв */
            /*
            IntStream.range(0, classVectors.Length).forEach(
                classNumber->criterionValues.addAll(
                    getCriterionValuesForClassesAndRadii(classVectors, classBinaryMatrices, pairs))
            );
            */
            foreach (var t in classVectors)
            {
                criterionValues.AddRange(getCriterionValuesForClassesAndRadii(classVectors, classBinaryMatrices, pairs));
            }

            /* Обчислюємо середнє значення критерію та середнє значення критерію у робочій області */
            var sum = new List<double>();
            var sumWorkingArea = new List<double>();
            foreach (List<Tuple<double, bool>> criterionValue in criterionValues)
            {
                sum.Add(criterionValue.Select(x => x.Item1).Max());
                sumWorkingArea.Add(criterionValue.Select(pair => pair.Item2 ? pair.Item1 : -10).Max());
            }

            var currentValue = sumWorkingArea.Average();
            /* Якщо середнє значення критерію у робочій області на даному кроці більше за поточне оптимальне, то запам'ятовуємо його та значення дельти */
            if (currentValue > optimalDeltaCriterionValue)
            {
                optimalDelta = delta;
                optimalDeltaCriterionValue = currentValue;
            }

            Console.WriteLine(delta + " " + sum.Average()
                              + " " + (sumWorkingArea.Average() > 0
                                  ? sumWorkingArea.Average()
                                  : -1));
        }

        Console.WriteLine("Optimal delta: " + optimalDelta);

        return optimalDelta;
    }

    /**
     * Метод для обчислення значення критерію для класів та радіусів їх контейнера
     *
     * @param classVectors        еталонні вектори кожного класу
     * @param classBinaryMatrices бінарні матриці класів
     * @param pairs               сусідні класи
     * @return значення критерію для класів та радіусів їх контейнера
     */
    private static List<List<Tuple<Double, Boolean>>> getCriterionValuesForClassesAndRadii(int[][] classVectors,
        List<int[][]> classBinaryMatrices, int[][] pairs)
    {
        var criterionValues = new List<List<Tuple<Double, Boolean>>>();
        foreach (var classVector in classVectors)
        {
            criterionValues.Add(new List<Tuple<double, bool>>());
        }

        for (var classNumber = 0; classNumber < classVectors.Length; classNumber++)
        {
            /* Обчислюємо значення критерію для радіусів у інтервалі [0, 60] */
            for (var radius = 0; radius < 61; radius++)
            {
                /* Перша достовірність */
                var d1 = GetDistancesBetweenVectorAndBinaryMatrix(classVectors[classNumber],
                        classBinaryMatrices[classNumber])
                    .Select(i => i <= radius ? 1 : 0)
                    .Average();
                var alpha = 1 - d1;
                /* Помилка другого роду */
                var beta = GetDistancesBetweenVectorAndBinaryMatrix(classVectors[classNumber],
                        classBinaryMatrices[pairs[classNumber][0]])
                    .Select(i => i <= radius ? 1 : 0)
                    .Average();
                /* Обчислюємо значення критерію*/
                var criterionValue = calculateCriterion(alpha, beta);
                /* Якщо перша достовірність >= 0.5, а помилка другого роду < 0.5, то це значення знаходиться у робочій області */
                var isWorkingArea = (d1 >= 0.5 && beta < 0.5);
                criterionValues[classNumber].Add(new Tuple<double, bool>(criterionValue, isWorkingArea));
            }
        }


        return criterionValues;
    }


    /**
     * Метод для пошуку відстаней між вектором та рядками бінарної матриці
     *
     * @param vector       вектор
     * @param binaryMatrix бінарна матриця
     * @return відстані між вектором та рядками бінарної матриці
     */
    private static List<int> GetDistancesBetweenVectorAndBinaryMatrix(int[] vector, int[][] binaryMatrix)
    {
        var distances = new List<int>();
        foreach (var binaryMatrixVector in binaryMatrix)
        {
            distances.Add(getDistanceBetweenVectors(vector, binaryMatrixVector));
        }

        return distances;
    }

    private static List<int[][]> GetClassesValues(List<string> classesImagesNames)
    {
        var classesValues = new List<int[][]>();
        foreach (var classesImagesName in classesImagesNames)
        {
            classesValues.Add(ImgToArray(classesImagesName));
        }

        return classesValues;
    }

    private static int[][] ImgToArray(string classesImagesName)
    {
        var image = Image.Load<Rgba32>(classesImagesName);
        return ImgToArray(image);
    }

    private static int[][] ImgToArray(Image<Rgba32> image)
    {
        var imageWidth = image.Width;
        var imageHeight = image.Height;
        var values = new int[imageHeight][];
        for (var i = 0; i < imageHeight; i++)
        {
            values[i] = new int [imageWidth * 3];
            for (var j = 0; j < imageWidth; j++)
            {
                var pixel = image[j, i];
                values[i][j] = pixel.R;
                values[i][j + imageWidth] = pixel.G;
                values[i][j + imageWidth * 2] = pixel.B;
            }
        }

        return values;
    }

    private static int[] GetVectorFromBinaryMatrix(int[][] binaryMatrix)
    {
        var vector = new int[binaryMatrix[0].Length];
        for (var i = 0; i < binaryMatrix[0].Length; i++)
        {
            var sum = 0;
            foreach (var row in binaryMatrix)
            {
                sum += row[i];
            }

            vector[i] = (int) (Math.Round((double) sum / binaryMatrix.Length));
        }

        return vector;
    }

    /**
     * Метод для пошуку сусідів кожного класу
     *
     * @param classVectors еталонні вектори кожного класу
     * @return сусідів кожного класу
     */
    private static int[][] makePairs(int[][] classVectors)
    {
        //int[][] pairs = new int[classVectors.Length][2];
        var pairs = new int[classVectors.Length][];
        var valueToSet = classVectors[0].Length + 1;
        for (var i = 0; i < pairs.Length; i++)
        {
            pairs[i] = new int[2];
            Array.Fill(pairs[i], valueToSet);
        }

        for (var i = 0; i < classVectors.Length; i++)
        {
            for (var j = 0; j < classVectors.Length; j++)
            {
                if (i != j)
                {
                    var distance = getDistanceBetweenVectors(classVectors[i], classVectors[j]);
                    if (distance < pairs[i][1])
                    {
                        pairs[i][0] = j;
                        pairs[i][1] = distance;
                    }
                }
            }
        }

        return pairs;
    }

    /**
     * Метод для пошуку відстаней між двома векторами
     *
     * @param firstVector  перший вектор
     * @param secondVector другий вектор
     * @return відстань між двома векторами
     */
    private static int getDistanceBetweenVectors(int[] firstVector, int[] secondVector)
    {
        var distance = 0;
        for (var i = 0; i < firstVector.Length; i++)
        {
            if (firstVector[i] != secondVector[i])
            {
                distance++;
            }
        }

        return distance;
    }

    /**
     * Метод для отримання еталонного вектора із бінарної матриці класу
     *
     * @param binaryMatrix бінарна матриця класу, для якого необхідно знайти еталонний вектор
     * @return еталонний вектор класу
     */
    private static int[] getVectorFromBinaryMatrix(int[][] binaryMatrix)
    {
        var vector = new int[binaryMatrix[0].Length];
        for (var i = 0; i < binaryMatrix[0].Length; i++)
        {
            var sum = 0;
            foreach (var row in binaryMatrix)
            {
                sum += row[i];
            }

            vector[i] = (int) (Math.Round((double) sum / binaryMatrix.Length));
        }

        return vector;
    }

    /**
     * Метод для отримання оптимальні радіуси для кожного класу
     *
     * @param classVectors        еталонні вектори кожного класу
     * @param classBinaryMatrices бінарні матриці класів
     * @return список радіусів контейнерів усіх класів
     */
    private static List<int> getRadii(int[][] classVectors, List<int[][]> classBinaryMatrices)
    {
        /* Знаходимо сусідні класи */
        var pairs = makePairs(classVectors);
        /* Знаходимо значення критерію для можливих значень радіусів */
        var criterionValues = new List<List<Tuple<Double, Boolean>>>();
        criterionValues.AddRange(getCriterionValuesForClassesAndRadii(classVectors, classBinaryMatrices, pairs));
        /* Знаходимо оптимальні радіуси */
        var radii = new List<int>();
        Console.WriteLine("Calculation of radii for classes");
        for (var i = 0; i < criterionValues.Count; i++)
        {
            Console.WriteLine("Class number: " + i);
            Console.WriteLine("Is working area | radius | criterion value");
            var res = criterionValues[i];
            var index = -1;
            double value = -1;
            /* Проходимо по всім можливив значенням радіуса */
            for (var j = 0; j < res.Count; j++)
            {
                Console.WriteLine(res[j].Item2 + " " + j + " " + res[j].Item1);
                /* Якщо значення критерію у робочій області для даного радіуса більше за поточне оптимальне, то запам'ятовуємо його та значення радіуса */
                if (res[j].Item2 && res[j].Item1 >= value)
                {
                    value = res[j].Item1;
                    index = j;
                }
            }

            radii.Add(index);
        }

        return radii;
    }

    /**
     * Метод для проведення екзамену на належність зображення до певного класу
     *
     * @param classVector  еталонний вектор класу на належність до якого відбувається екзамен
     * @param radius       радіус контейнера класу на належність до якого відбувається екзамен
     * @param binaryMatrix бінарна матриця зображення, для якого відбувається пошук класу
     * @return результат екзамену
     */
    private static double exam(int[] classVector, int radius, int[][] binaryMatrix)
    {
        double sum = 0;
        foreach (var aBinaryMatrix in binaryMatrix)
        {
            sum += 1 - (double) getDistanceBetweenVectors(classVector, aBinaryMatrix) / radius;
        }

        return sum / binaryMatrix.Length;
    }

    /**
     * Метод для розрахунку критерію
     *
     * @param alpha помилка першого роду
     * @param beta  помилка другого роду
     * @return значення критерію
     */
    private static double calculateCriterion(double alpha, double beta)
    {
        return calculateKullback(alpha, beta) / calculateKullback(0, 0);
    }

    /**
     * Метод для розрахунку критерію Кульбака
     *
     * @param alpha помилка першого роду
     * @param beta  помилка другого роду
     * @return значення критерію Кульбака
     */
    private static double calculateKullback(double alpha, double beta)
    {
        return (Math.Log((2 - (alpha + beta) + 0.1) / (alpha + beta + 0.1)) / Math.Log(2)) * (1 - (alpha + beta));
    }
}