using System.Numerics;
using System.Text;

namespace Community.PowerToys.Run.Plugin.HexInspector;

public class ConvertResult(string raw, string formated)
{
    public string Raw { get; set; } = raw;
    public string Formated { get; set; } = formated;
}

public class Convert(SettingsHelper settingHelper)
{
    private readonly SettingsHelper settings = settingHelper;
    public bool is_upper;

    private static string ToBigEndian(string value, int chunkSize)
    {
        if (value.Length < chunkSize)
        {
            return value; // No need to reverse
        }
        if (value.Length % chunkSize != 0)
        {
            value = value.PadLeft(value.Length + (chunkSize - value.Length % chunkSize), '0');
        }
        string[] splited = new string[value.Length / chunkSize];
        for (int i = 0; i < splited.Length; i++)
        {
            splited[i] = value.Substring(i * chunkSize, chunkSize);
        }
        Array.Reverse(splited);
        return string.Join("", splited);
    }

    private static string HexToBigEndian(string hex) => ToBigEndian(hex, 2);
    private static string BinToBigEndian(string bin) => ToBigEndian(bin, 8);

    private static string HexToLittleEndian(string hex) => HexToBigEndian(hex);
    private static string BinToLittleEndian(string bin) => BinToBigEndian(bin);
    private static string SplitBinary(string bin)
    {
        if (bin.Length % 4 != 0)
        {
            bin = bin.PadLeft(bin.Length + (4 - bin.Length % 4), '0');
        }
        string[] splited = new string[bin.Length / 4];
        for (int i = 0; i < bin.Length / 4; i++)
        {
            splited[i] = bin.Substring(i * 4, 4);
        }
        return string.Join(" ", splited);
    }

    public ConvertResult HexFormat(string hex, bool upper)
    {
        // hex should be in little endian
        if (settings.OutputEndian == Endian.BigEndian)
        {
            hex = HexToBigEndian(hex);
        }

        if (upper)
        {
            return new ConvertResult(hex.ToUpper(), hex.ToUpper());
        }
        else
        {
            return new ConvertResult(hex.ToLower(), hex.ToLower());
        }
    }

    public ConvertResult BinFormat(string bin)
    {
        // bin should be in little endian
        if (settings.OutputEndian == Endian.BigEndian)
        {
            bin = BinToBigEndian(bin);
        }

        if (settings.SplitBinary)
        {
            return new ConvertResult(bin, SplitBinary(bin));
        }
        return new ConvertResult(bin, bin);
    }

    public static ConvertResult OctFormat(string oct)
    {
        // No need to change octal format
        return new ConvertResult(oct, oct);
    }

    public static ConvertResult DecFormat(string dec)
    {
        return new ConvertResult(dec, dec);
    }

    public ConvertResult AsciiFormat(string ascii)
    {
        // ascii should be in little endian
        if (settings.OutputEndian == Endian.BigEndian)
        {
            ascii = new string(ascii.Reverse().ToArray());
        }

        StringBuilder strb = new(ascii);
        for (int i = 0; i < strb.Length; i++)
        {
            if (char.IsControl(strb[i]))
            {
                strb[i] = ' ';
            }
        }
        ascii = strb.ToString();

        return new ConvertResult(ascii, ascii);
    }

    // Ascii to integer(Decimal)
    public static string AsciiToInt(string ascii)
    {
        return System.Convert.ToInt64(
            System.Convert.ToHexString([.. Encoding.ASCII.GetBytes(ascii) // ASCII to hex string, e.g. "AB" -> "42-41"
                .Reverse()]),
            (int)Base.Hex
        ).ToString();
    }

    // integer(Decimal) to Ascii
    public static string IntToAscii(string dec)
    {
        return new(
            [.. Encoding.ASCII.GetString(
                BitConverter.GetBytes(System.Convert.ToInt64(dec, 10)))
                .TrimEnd('\0') // Remove null character
                .Reverse()]
            );
    }

    // Convert string to BigInteger(Decimal)
    public static BigInteger BigIntegerConvert(string input, Base fromBase)
    {
        return fromBase switch
        {
            Base.Bin => BigInteger.Parse("0" + input, System.Globalization.NumberStyles.BinaryNumber),
            Base.Oct => new Func<BigInteger>(
                () =>
                {
                    input = input.Replace(" ", ""); // Remove space
                    BigInteger result = 0;
                    for (int i = 0; i < input.Length; i++)
                    {
                        result += (input[i] - '0') * BigInteger.Pow(8, input.Length - i - 1);
                    }
                    return result;
                }
            )(),
            Base.Dec => BigInteger.Parse(input),
            Base.Hex => BigInteger.Parse("0" + input, System.Globalization.NumberStyles.HexNumber),
            Base.Ascii => new Func<BigInteger>(
                () =>
                {
                    byte[] bytes = Encoding.ASCII.GetBytes(input).Reverse().ToArray();
                    return new BigInteger(bytes);
                }
            )(),
            Base.Fra16 => new Func<BigInteger>(
                () =>
                {
                    // Convert float16 to bytes, then to BigInteger
                    Half half = Half.Parse(input);
                    byte[] bytes = BitConverter.GetBytes(half);
                    return new BigInteger(bytes);
                }
            )(),
            Base.Fra32 => new Func<BigInteger>(
                () =>
                {
                    // Convert float32 to bytes, then to BigInteger
                    float float32 = float.Parse(input);
                    byte[] bytes = BitConverter.GetBytes(float32);
                    return new BigInteger(bytes);
                }
            )(),
            Base.Fra64 => new Func<BigInteger>(
                () =>
                {
                    // Convert float64 to bytes, then to BigInteger
                    double float64 = double.Parse(input);
                    byte[] bytes = BitConverter.GetBytes(float64);
                    return new BigInteger(bytes);
                }
            )(),
            _ => throw new ArgumentException("Invalid base", nameof(fromBase))
        };
    }

    // Convert BigInteger(Decimal) to string
    public static string ConvertBigInteger(BigInteger input, Base toBase) => toBase switch
    {
        Base.Bin => input.ToString("B"),
        Base.Oct => new Func<string>(
            () => {
                if (input == 0)
                {
                    return "0";
                }

                string result = "";
                while (input > 0)
                {
                    result = (input % 8).ToString() + result;
                    input /= 8;
                }
                return result;
            }
        )(),
        Base.Dec => input.ToString(),
        Base.Hex => input.ToString("X"),
        Base.Ascii => new Func<string>(
            () => {
                byte[] bytes = input.ToByteArray().Reverse().ToArray();
                return Encoding.ASCII.GetString(bytes);
            }
        )(),
        Base.Fra16 => new Func<string>(
            () => {
                // Convert BigInteger to bytes, then to float16
                byte[] bytes = input.ToByteArray();
                if (bytes.Length < 2)
                {
                    // Pad with zeros if the byte array is too short
                    bytes = bytes.Concat(new byte[2 - bytes.Length]).ToArray();
                }
                else if (bytes.Length > 2)
                {
                    // Truncate to 2 bytes if the byte array is too long
                    bytes = bytes.Take(2).ToArray();
                }
                Half half = BitConverter.ToHalf(bytes, 0);
                return half.ToString();
            }
        )(),
        Base.Fra32 => new Func<string>(
            () => {
                // Convert BigInteger to bytes, then to float32
                byte[] bytes = input.ToByteArray();
                if (bytes.Length < 4)
                {
                    // Pad with zeros if the byte array is too short
                    bytes = bytes.Concat(new byte[4 - bytes.Length]).ToArray();
                }
                else if (bytes.Length > 4)
                {
                    // Truncate to 4 bytes if the byte array is too long
                    bytes = bytes.Take(4).ToArray();
                }
                float float32 = BitConverter.ToSingle(bytes, 0);
                return float32.ToString();
            }
        )(),
        Base.Fra64 => new Func<string>(
            () => {
                // Convert BigInteger to bytes, then to float64
                byte[] bytes = input.ToByteArray();
                if (bytes.Length < 8)
                {
                    // Pad with zeros if the byte array is too short
                    bytes = bytes.Concat(new byte[8 - bytes.Length]).ToArray();
                }
                else if (bytes.Length > 8)
                {
                    // Truncate to 8 bytes if the byte array is too long
                    bytes = bytes.Take(8).ToArray();
                }
                double float64 = BitConverter.ToDouble(bytes, 0);
                return float64.ToString();
            }
        )(),
        _ => throw new ArgumentException("Invalid base", nameof(toBase))
    };

    public ConvertResult UniversalConvert(string input, Base fromBase, Base toBase)
    {
        // Make sure the input is in the little endian before converting
        if (settings.InputEndian == Endian.BigEndian)
        {
            input = fromBase switch
            {
                Base.Bin => BinToLittleEndian(input),
                Base.Hex => HexToLittleEndian(input),
                Base.Ascii => new string(input.Reverse().ToArray()),
                _ => input
            };
        }

        try
        {
            string dec = BigIntegerConvert(input, fromBase).ToString();

            string raw = ConvertBigInteger(BigInteger.Parse(dec), toBase);

            string formated = toBase switch
            {
                Base.Bin => BinFormat(raw).Formated,
                Base.Oct => OctFormat(raw).Formated,
                Base.Dec => DecFormat(raw).Formated,
                Base.Hex => HexFormat(raw, is_upper).Formated,
                Base.Ascii => AsciiFormat(raw).Formated,
                Base.Fra16 => raw, // No additional formatting for float16
                Base.Fra32 => raw, // No additional formatting for float32
                Base.Fra64 => raw, // No additional formatting for float64
                _ => raw
            };

            return new ConvertResult(raw, formated);
        }
        catch (Exception e)
        when (e is FormatException || e is InvalidCastException || e is OverflowException || e is ArgumentNullException)
        {
            return e switch 
            {
                FormatException         => new ConvertResult("Invalid format", "Invalid format"),
                InvalidCastException    => new ConvertResult("Invalid cast", "Invalid cast"),
                OverflowException       => new ConvertResult("Overflow", "Overflow"),
                ArgumentNullException   => new ConvertResult("Null argument", "Null argument"),
                _                       => new ConvertResult("Unknown error", "Unknown error")
            };
        }
    }
}