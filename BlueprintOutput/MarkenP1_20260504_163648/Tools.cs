using System;
using PSI.Sox.Interfaces;

public class Tools
{
    public Tools(object logger)
    {
    }

    public string GetStringValueFromBusinessRuleSettings(string key, object settings)
    {
        return string.Empty;
    }

    public static string RemoveSpecialCharacters(string value)
    {
        return value;
    }

    public static bool ConvertToBool(string value)
    {
        return false;
    }

    public static Weight ConvertToWeightObj(string value)
    {
        return new Weight();
    }

    public static Money ConvertToMoneyObj(string value)
    {
        return new Money();
    }

    public static long ConvertStringToLong(string value)
    {
        return 0;
    }

    public static double ConvertStringToDouble(string value)
    {
        return 0;
    }

    public static bool IsEmailFormatValid(string value)
    {
        return true;
    }

    public static Service TranslateServiceType(string value)
    {
        return new Service();
    }

    public static string TranslateTerms(string terms, string country)
    {
        return terms;
    }

    public ShipmentResponse SuppressPrintingCommericalInvoice(ShipmentRequest shipmentRequest, ShipmentResponse shipmentResponse)
    {
        return shipmentResponse;
    }
}
