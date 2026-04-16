using PSI.Sox;
using PSI.Sox.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace ShipExecNavigator.BusinessLogic.Tools
{
    public class CompanyExtractor
    {
        // Lazily-built list of every PSI.Sox enum-typed XML element name found in the
        // Company object graph. Computed once on first use and shared across all calls.
        private static readonly Lazy<IReadOnlyList<(string ElementName, Type EnumType)>> _soxEnumElements =
            new Lazy<IReadOnlyList<(string ElementName, Type EnumType)>>(DiscoverSoxEnumElements);

        // Lazily-built set of every Nullable<Guid> XML element name found in the
        // Company object graph. Used to normalise empty elements before deserialisation.
        private static readonly Lazy<IReadOnlyList<string>> _nullableGuidElements =
            new Lazy<IReadOnlyList<string>>(DiscoverNullableGuidElements);

        public static Company GetCompany(string xml)
        {
            // XmlSerializer only recognises xsi:nil="true" as a null indicator for
            // Nullable value types (e.g. ProfileId, EnterpriseId : int?).
            // A bare nil="true" without the xsi: prefix is silently ignored, so the
            // deserializer tries to call int.Parse("") on the empty element content,
            // throwing FormatException / InvalidOperationException.
            // Normalise any such attribute before handing the XML to the serializer.
            xml = xml.Replace(" nil=\"true\"", " xsi:nil=\"true\"");

            // XmlSerializer expects enum member names (e.g. "Ready"), not integer
            // values. Older serializers and WCF/DataContract pipelines emit the
            // underlying integer (e.g. "1"). For [Flags] enums the value may be a
            // composite bitmask (e.g. "73" for AdapterType). Normalise ALL PSI.Sox
            // enum fields discovered via reflection before handing the XML to the serializer.
            foreach (var (elementName, enumType) in _soxEnumElements.Value)
                xml = NormalizeEnumValues(xml, elementName, enumType);

            // Nullable<Guid> properties (e.g. Shipper.SiteId) throw
            // "Unrecognized Guid format" when the element is present but empty
            // (<SiteId></SiteId> or <SiteId />). Normalise these to xsi:nil="true"
            // so XmlSerializer treats them as null instead of trying to parse "".
            foreach (var elementName in _nullableGuidElements.Value)
                xml = NormalizeEmptyGuidElements(xml, elementName);

            // Ensure the xsi namespace is declared on the root element whenever
            // any of the normalisations above introduced xsi: prefixed attributes.
            if (xml.Contains("xsi:nil") && !xml.Contains("xmlns:xsi"))
            {
                // Insert the declaration on the first element (root) only.
                var nsMatch = Regex.Match(xml, @"(<\w+)([\s>])");
                if (nsMatch.Success)
                {
                    xml = string.Concat(
                        xml.AsSpan(0, nsMatch.Groups[1].Index + nsMatch.Groups[1].Length),
                        " xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"",
                        xml.AsSpan(nsMatch.Groups[1].Index + nsMatch.Groups[1].Length));
                }
            }

            if (GetRootElementName(xml) == "CompanyConfiguration")
                return CompanyFromConfiguration(xml);

            XmlSerializer serializer = new XmlSerializer(typeof(Company));
            using (TextReader reader = new StringReader(xml))
            {
                return (Company)serializer.Deserialize(reader);
            }
        }

        private static string NormalizeEnumValues(string xml, string elementName, Type enumType)
        {
            bool isFlags = enumType.IsDefined(typeof(FlagsAttribute), false);

            // Build a lookup: C# member name → XML serialisation name, honouring any
            // [XmlEnum] attribute. XmlSerializer uses [XmlEnum].Name (not the C# identifier)
            // when reading/writing enum values, so we must emit that name, not Enum.GetName().
            var memberToXmlName = enumType
                .GetFields(BindingFlags.Public | BindingFlags.Static)
                .ToDictionary(
                    f => f.Name,
                    f => f.GetCustomAttribute<XmlEnumAttribute>()?.Name ?? f.Name);

            // Step 1 – Replace legacy integer / bitmask values with the name XmlSerializer expects.
            xml = Regex.Replace(
                xml,
                $@"<{elementName}>(\d+)</{elementName}>",
                match =>
                {
                    if (!int.TryParse(match.Groups[1].Value, out int intVal))
                        return match.Value;

                    if (isFlags)
                    {
                        // Decompose the composite bitmask into space-separated [XmlEnum] names,
                        // which is the format XmlSerializer expects for [Flags] enums.
                        string combined = string.Join(" ",
                            Enum.GetValues(enumType)
                                .Cast<object>()
                                .Select(v => Convert.ToInt32(v))
                                .Where(v => v > 0 && (intVal & v) == v)
                                .Select(v =>
                                {
                                    string name = Enum.GetName(enumType, v);
                                    return name != null && memberToXmlName.TryGetValue(name, out string xmlName)
                                        ? xmlName : name;
                                })
                                .Where(n => n != null));

                        return !string.IsNullOrEmpty(combined)
                            ? $"<{elementName}>{combined}</{elementName}>"
                            : match.Value;
                    }

                    if (!Enum.IsDefined(enumType, intVal))
                        return match.Value;

                    string memberName = Enum.GetName(enumType, intVal);
                    return memberName != null && memberToXmlName.TryGetValue(memberName, out string xmlSerialName)
                        ? $"<{elementName}>{xmlSerialName}</{elementName}>"
                        : match.Value;
                });

            // Step 2 – Replace C# member names that XmlSerializer won't recognise because
            //          [XmlEnum] maps them to a different string (e.g. "Textbox" → "1").
            //          This handles XML produced by serializers that emit the C# identifier
            //          rather than the [XmlEnum] name.
            foreach (var kvp in memberToXmlName.Where(kvp => kvp.Key != kvp.Value))
            {
                xml = xml.Replace(
                    $"<{elementName}>{kvp.Key}</{elementName}>",
                    $"<{elementName}>{kvp.Value}</{elementName}>");
            }

            return xml;
        }

        /// <summary>
        /// Replaces empty &lt;ElementName&gt;&lt;/ElementName&gt; and self-closing
        /// &lt;ElementName /&gt; elements with xsi:nil="true" so XmlSerializer
        /// returns null instead of attempting to parse an empty string as a Guid.
        /// </summary>
        private static string NormalizeEmptyGuidElements(string xml, string elementName)
        {
            // <SiteId></SiteId>  or  <SiteId>   </SiteId>
            xml = Regex.Replace(xml, $@"<{elementName}>\s*</{elementName}>",
                $"<{elementName} xsi:nil=\"true\" />");
            // <SiteId/>  or  <SiteId />
            xml = Regex.Replace(xml, $@"<{elementName}\s*/>",
                $"<{elementName} xsi:nil=\"true\" />");
            return xml;
        }

        // ── Reflection-based discovery ─────────────────────────────────────────

        // Reflects over the Company object graph and returns one entry per distinct
        // PSI.Sox-assembly enum property, keyed by its XML element name.
        private static IReadOnlyList<(string ElementName, Type EnumType)> DiscoverSoxEnumElements()
        {
            var soxAssembly = typeof(AdapterType).Assembly;
            var results     = new List<(string ElementName, Type EnumType)>();
            var visited     = new HashSet<Type>();
            CollectEnumProperties(typeof(Company), soxAssembly, visited, results);
            return results;
        }

        // Recursively walks the public instance properties of a type, collecting every
        // PSI.Sox-backed enum property (including those nested inside List<T> and arrays).
        private static void CollectEnumProperties(
            Type type,
            Assembly soxAssembly,
            HashSet<Type> visited,
            List<(string ElementName, Type EnumType)> results)
        {
            if (!visited.Add(type))
                return;

            foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                try
                {
                    Type propType   = prop.PropertyType;
                    Type underlying = Nullable.GetUnderlyingType(propType) ?? propType;

                    // Unwrap List<T> → T, or T[] → T
                    if (underlying.IsGenericType &&
                        underlying.GetGenericTypeDefinition() == typeof(List<>))
                        underlying = underlying.GetGenericArguments()[0];
                    else if (underlying.IsArray)
                        underlying = underlying.GetElementType() ?? underlying;

                    if (underlying.IsEnum && underlying.Assembly == soxAssembly)
                    {
                        // Respect any [XmlElement(ElementName = "...")] override; fall back to the property name.
                        var    xmlElem = prop.GetCustomAttribute<XmlElementAttribute>();
                        string eltName = xmlElem?.ElementName ?? prop.Name;

                        // Register only the first enum type seen for a given element name to avoid
                        // double-processing when multiple PSI.Sox types share the same property name.
                        if (!results.Any(r => r.ElementName == eltName))
                            results.Add((eltName, underlying));
                    }
                    else if (!underlying.IsPrimitive        &&
                             underlying != typeof(string)   &&
                             underlying.Assembly == soxAssembly)
                    {
                        CollectEnumProperties(underlying, soxAssembly, visited, results);
                    }
                }
                catch (FileNotFoundException) { /* assembly for this property type not present — skip */ }
            }
        }

        // Reflects over the Company object graph and returns every distinct
        // Nullable<Guid> XML element name.
        private static IReadOnlyList<string> DiscoverNullableGuidElements()
        {
            var soxAssembly = typeof(AdapterType).Assembly;
            var results     = new HashSet<string>();
            var visited     = new HashSet<Type>();
            CollectNullableGuidProperties(typeof(Company), soxAssembly, visited, results);
            return results.ToList();
        }

        private static void CollectNullableGuidProperties(
            Type type,
            Assembly soxAssembly,
            HashSet<Type> visited,
            HashSet<string> results)
        {
            if (!visited.Add(type))
                return;

            foreach (PropertyInfo prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                try
                {
                    Type propType   = prop.PropertyType;
                    Type underlying = Nullable.GetUnderlyingType(propType) ?? propType;

                    // Unwrap List<T> → T, or T[] → T
                    Type elementType = underlying;
                    if (underlying.IsGenericType &&
                        underlying.GetGenericTypeDefinition() == typeof(List<>))
                        elementType = underlying.GetGenericArguments()[0];
                    else if (underlying.IsArray)
                        elementType = underlying.GetElementType() ?? underlying;

                    if (Nullable.GetUnderlyingType(propType) == typeof(Guid))
                    {
                        var    xmlElem = prop.GetCustomAttribute<XmlElementAttribute>();
                        string eltName = !string.IsNullOrEmpty(xmlElem?.ElementName)
                            ? xmlElem.ElementName
                            : prop.Name;
                        results.Add(eltName);
                    }

                    if (!elementType.IsPrimitive     &&
                        elementType != typeof(string) &&
                        elementType != typeof(Guid)   &&
                        !elementType.IsEnum           &&
                        elementType.Assembly == soxAssembly)
                    {
                        CollectNullableGuidProperties(elementType, soxAssembly, visited, results);
                    }
                }
                catch (FileNotFoundException) { /* assembly for this property type not present — skip */ }
            }
        }

        public static Company GetFile_TestOutput(string filePath)
            => GetCompany(File.ReadAllText(filePath));

        public static Company GetFile_ModifiedCompany(string filePath)
            => GetCompany(File.ReadAllText(filePath));

        // ── Helpers ────────────────────────────────────────────────────────────

        private static string GetRootElementName(string xml)
        {
            try
            {
                using (var sr = new StringReader(xml))
                using (var xr = XmlReader.Create(sr))
                {
                    xr.MoveToContent();
                    return xr.LocalName;
                }
            }
            catch { return string.Empty; }
        }

        private static Company CompanyFromConfiguration(string xml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(CompanyConfiguration));
            CompanyConfiguration cc;
            using (TextReader reader = new StringReader(xml))
                cc = (CompanyConfiguration)serializer.Deserialize(reader);

            System.Guid companyId;
            System.Guid.TryParse(cc.Guid, out companyId);

            return new Company
            {
                Id              = companyId,
                Name            = cc.Name,
                Symbol          = cc.Symbol,
                Enabled         = cc.Enabled,
                LicenseId       = cc.LicenseId,
                RegistrationKey = cc.RegistrationKey,

                Shippers = cc.Shippers != null
                    ? cc.Shippers.Select(s => new Shipper
                    {
                        Name          = s.Name,
                        Symbol        = s.Symbol,
                        Code          = s.Code,
                        Address1      = s.Address1,
                        Address2      = s.Address2,
                        Address3      = s.Address3,
                        City          = s.City,
                        Company       = s.Company,
                        Contact       = s.Contact,
                        Country       = s.Country,
                        Fax           = s.Fax,
                        Phone         = s.Phone,
                        Email         = s.Email,
                        PoBox         = s.PoBox,
                        PostalCode    = s.PostalCode,
                        Residential   = s.Residential,
                        StateProvince = s.StateProvince,
                        Sms           = s.Sms,
                    }).ToList()
                    : new List<Shipper>(),

                Clients = cc.Clients != null
                    ? cc.Clients.Select(c => new Client
                    {
                        Name      = c.Name,
                        AccessKey = c.AccessKey,
                    }).ToList()
                    : new List<Client>(),

                // Collections whose Configuration counterparts have incompatible
                // structures are left empty — CarrierRoutes (ShippingRouteConfiguration
                // has no Carrier/AdapterRegistration), Sites (deeply recursive), etc.
                AdapterRegistrations      = new List<AdapterRegistration>(),
                CarrierRoutes             = new List<CarrierRoute>(),
                Sites                     = new List<Site>(),
                Machines                  = new List<Machine>(),
                Profiles                  = new List<Profile>(),
                PrinterDefinitions        = new List<PrinterDefinition>(),
                PrinterConfigurations     = new List<PrinterConfiguration>(),
                DocumentConfigurations    = new List<DocumentConfiguration>(),
                ScaleDefinitions          = new List<ScaleDefinition>(),
                ScaleConfigurations       = new List<ScaleConfiguration>(),
                SourceConfigurations      = new List<SourceConfiguration>(),
                DataConfigurationMappings = new List<DataConfigurationMapping>(),
                Schedules                 = new List<Schedule>(),
            };
        }
    }
}
