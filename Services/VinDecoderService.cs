using System.Text.RegularExpressions;

namespace RS.Fahrzeugsystem.Api.Services;

public sealed class VinDecodeResult
{
	public string Vin { get; set; } = default!;
	public bool IsValidLength { get; set; }
	public bool HasValidCharacters { get; set; }
	public bool CheckDigitValid { get; set; }
	public bool IsValid => IsValidLength && HasValidCharacters && CheckDigitValid;

	public string? Wmi { get; set; }
	public string? Manufacturer { get; set; }
	public string? Country { get; set; }
	public string? ModelYearCode { get; set; }
	public int? ModelYear { get; set; }
	public string? PlantCode { get; set; }
	public string? SerialNumber { get; set; }
}

public interface IVinDecoderService
{
	VinDecodeResult Decode(string vin);
}

public sealed class VinDecoderService : IVinDecoderService
{
	private static readonly Dictionary<char, int> Transliteration = new()
	{
		['A'] = 1,
		['B'] = 2,
		['C'] = 3,
		['D'] = 4,
		['E'] = 5,
		['F'] = 6,
		['G'] = 7,
		['H'] = 8,
		['J'] = 1,
		['K'] = 2,
		['L'] = 3,
		['M'] = 4,
		['N'] = 5,
		['P'] = 7,
		['R'] = 9,
		['S'] = 2,
		['T'] = 3,
		['U'] = 4,
		['V'] = 5,
		['W'] = 6,
		['X'] = 7,
		['Y'] = 8,
		['Z'] = 9,
		['0'] = 0,
		['1'] = 1,
		['2'] = 2,
		['3'] = 3,
		['4'] = 4,
		['5'] = 5,
		['6'] = 6,
		['7'] = 7,
		['8'] = 8,
		['9'] = 9
	};

	private static readonly int[] Weights = { 8, 7, 6, 5, 4, 3, 2, 10, 0, 9, 8, 7, 6, 5, 4, 3, 2 };

	private static readonly Regex ValidVinRegex = new("^[A-HJ-NPR-Z0-9]{17}$", RegexOptions.Compiled);

	public VinDecodeResult Decode(string vin)
	{
		var normalized = (vin ?? string.Empty).Trim().ToUpperInvariant();

		var result = new VinDecodeResult
		{
			Vin = normalized,
			IsValidLength = normalized.Length == 17,
			HasValidCharacters = ValidVinRegex.IsMatch(normalized)
		};

		if (!result.IsValidLength || !result.HasValidCharacters)
			return result;

		result.Wmi = normalized[..3];
		result.ModelYearCode = normalized[9].ToString();
		result.PlantCode = normalized[10].ToString();
		result.SerialNumber = normalized[11..17];
		result.CheckDigitValid = ValidateCheckDigit(normalized);

		MapManufacturer(result, result.Wmi);
		result.ModelYear = DecodeModelYear(normalized[9]);

		return result;
	}

	private static bool ValidateCheckDigit(string vin)
	{
		var sum = 0;

		for (var i = 0; i < vin.Length; i++)
		{
			if (!Transliteration.TryGetValue(vin[i], out var value))
				return false;

			sum += value * Weights[i];
		}

		var remainder = sum % 11;
		var expected = remainder == 10 ? 'X' : remainder.ToString()[0];

		return vin[8] == expected;
	}

	private static int? DecodeModelYear(char code)
	{
		// Für moderne Fahrzeuge reicht erstmal 2001–2030 Mapping.
		return code switch
		{
			'1' => 2001,
			'2' => 2002,
			'3' => 2003,
			'4' => 2004,
			'5' => 2005,
			'6' => 2006,
			'7' => 2007,
			'8' => 2008,
			'9' => 2009,
			'A' => 2010,
			'B' => 2011,
			'C' => 2012,
			'D' => 2013,
			'E' => 2014,
			'F' => 2015,
			'G' => 2016,
			'H' => 2017,
			'J' => 2018,
			'K' => 2019,
			'L' => 2020,
			'M' => 2021,
			'N' => 2022,
			'P' => 2023,
			'R' => 2024,
			'S' => 2025,
			'T' => 2026,
			'V' => 2027,
			'W' => 2028,
			'X' => 2029,
			'Y' => 2030,
			_ => null
		};
	}

	private static void MapManufacturer(VinDecodeResult result, string? wmi)
	{
		switch (wmi)
		{
			case "WVW":
				result.Manufacturer = "Volkswagen";
				result.Country = "Deutschland";
				break;
			case "WVG":
				result.Manufacturer = "Volkswagen";
				result.Country = "Deutschland";
				break;
			case "WAU":
				result.Manufacturer = "Audi";
				result.Country = "Deutschland";
				break;
			case "WBA":
			case "WBS":
				result.Manufacturer = "BMW";
				result.Country = "Deutschland";
				break;
			case "TRU":
				result.Manufacturer = "Audi Hungaria";
				result.Country = "Ungarn";
				break;
			default:
				result.Manufacturer = "Unbekannt";
				result.Country = "Unbekannt";
				break;
		}
	}
}