// File: PluginEntry.cs
// Target framework: .NET Framework 4.7.2/4.8
// Reference: System.Windows.Forms (обязательно для точной сигнатуры CreateControl)

using System;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace Oktell.Etalon
{
    public class PluginEntry
    {
        // === Обязательный делегат/событие интерфейса ===
        public delegate string PluginQueryInvoker(string xml);
        public event PluginQueryInvoker OnQuery;

        // === Идентификаторы (замените на свои постоянные GUID'ы) ===
        private static readonly Guid PluginId  = new Guid("11111111-2222-3333-4444-555555555555");
        private static readonly Guid FormId    = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        // === Обязательные методы интерфейса (см. вики) ===
        public Guid   GetId()                                => PluginId;                                              //  [oai_citation:1‡wiki.oktell.ru](https://wiki.oktell.ru/%D0%A1%D1%82%D1%80%D1%83%D0%BA%D1%82%D1%83%D1%80%D0%B0_%D0%B8%D0%BD%D1%82%D0%B5%D1%80%D1%84%D0%B5%D0%B9%D1%81%D0%B0)
        public int    GetInterfaceVersion(int lastKnown)     => lastKnown;                                             //  [oai_citation:2‡wiki.oktell.ru](https://wiki.oktell.ru/%D0%A1%D1%82%D1%80%D1%83%D0%BA%D1%82%D1%83%D1%80%D0%B0_%D0%B8%D0%BD%D1%82%D0%B5%D1%80%D1%84%D0%B5%D0%B9%D1%81%D0%B0)
        public string GetModuleVersion()                     => "1.0.0";
        public string GetModuleName()                        => "Scenario Tools (Etalon)";
        public string GetDBUpdate()                          => string.Empty;

        // 1) Объявляем одну форму: серверный компонент сценариев (module=scriptcomponent, controltype=none)
        public string GetForms()                                                                                       //  [oai_citation:3‡wiki.oktell.ru](https://wiki.oktell.ru/%D0%9E%D0%BF%D0%B8%D1%81%D0%B0%D0%BD%D0%B8%D0%B5_%D0%B1%D0%B0%D0%B7%D0%BE%D0%B2%D1%8B%D1%85_%D1%8D%D0%BB%D0%B5%D0%BC%D0%B5%D0%BD%D1%82%D0%BE%D0%B2_%D0%B8%D0%BD%D1%82%D0%B5%D1%80%D1%84%D0%B5%D0%B9%D1%81%D0%B0)
        {
            return XmlOk($@"
<oktellxmlmapper version=""80710"">
  <data name=""availableforms"" count=""1"">
    <property_set>
      <property_simple key=""id"" value=""{FormId}"" />
      <property_cdata  key=""name""><![CDATA[Нормализация телефона]]></property_cdata>
      <property_cdata  key=""description""><![CDATA[Серверный компонент для сценариев]]></property_cdata>
      <property_simple key=""controltype"" value=""2"" name=""none"" />
      <property_simple key=""module"" value=""2""  name=""scriptcomponent"" />
    </property_set>
  </data>
</oktellxmlmapper>");
        }

        // 2) Редактор свойств запрашивает схему ВХОДОВ по idform (во входном XML только idplugin/idform)        //  [oai_citation:4‡wiki.oktell.ru](https://wiki.oktell.ru/%D0%9E%D0%BF%D0%B8%D1%81%D0%B0%D0%BD%D0%B8%D0%B5_%D0%B1%D0%B0%D0%B7%D0%BE%D0%B2%D1%8B%D1%85_%D1%8D%D0%BB%D0%B5%D0%BC%D0%B5%D0%BD%D1%82%D0%BE%D0%B2_%D0%B8%D0%BD%D1%82%D0%B5%D1%80%D1%84%D0%B5%D0%B9%D1%81%D0%B0)
        public string GetInputParams(string xml)                                                                       //  [oai_citation:5‡wiki.oktell.ru](https://wiki.oktell.ru/%D0%9E%D0%BF%D0%B8%D1%81%D0%B0%D0%BD%D0%B8%D0%B5_%D0%B1%D0%B0%D0%B7%D0%BE%D0%B2%D1%8B%D1%85_%D1%8D%D0%BB%D0%B5%D0%BC%D0%B5%D0%BD%D1%82%D0%BE%D0%B2_%D0%B8%D0%BD%D1%82%D0%B5%D1%80%D1%84%D0%B5%D0%B9%D1%81%D0%B0)
        {
            var idform = ReadIdForm(xml);
            if (!IsOurForm(idform)) return EmptyInputParams();

            return XmlOk(@"
<oktellxmlmapper version=""80710"">
  <data name=""inputparams"" count=""1"">
    <property_set name=""forminput"">
      <property_collection name=""items"" count=""2"">
        <property_set>
          <property_simple key=""id""   value=""phone""/>
          <property_simple key=""type"" value=""string""/>
          <property_cdata  key=""name""><![CDATA[Телефон]]></property_cdata>
          <property_cdata  key=""default""><![CDATA[]]></property_cdata>
        </property_set>
        <property_set>
          <property_simple key=""id""   value=""country""/>
          <property_simple key=""type"" value=""string""/>
          <property_cdata  key=""name""><![CDATA[Код страны (если нет +)]]></property_cdata>
          <property_cdata  key=""default""><![CDATA[7]]></property_cdata>
        </property_set>
      </property_collection>
    </property_set>
  </data>
</oktellxmlmapper>");
        }

        // 3) Редактор свойств запрашивает схему ВЫХОДОВ по idform                                                    //  [oai_citation:6‡wiki.oktell.ru](https://wiki.oktell.ru/%D0%9E%D0%BF%D0%B8%D1%81%D0%B0%D0%BD%D0%B8%D0%B5_%D0%B1%D0%B0%D0%B7%D0%BE%D0%B2%D1%8B%D1%85_%D1%8D%D0%BB%D0%B5%D0%BC%D0%B5%D0%BD%D1%82%D0%BE%D0%B2_%D0%B8%D0%BD%D1%82%D0%B5%D1%80%D1%84%D0%B5%D0%B9%D1%81%D0%B0)
        public string GetOutputParams(string xml)                                                                      //  [oai_citation:7‡wiki.oktell.ru](https://wiki.oktell.ru/%D0%9E%D0%BF%D0%B8%D1%81%D0%B0%D0%BD%D0%B8%D0%B5_%D0%B1%D0%B0%D0%B7%D0%BE%D0%B2%D1%8B%D1%85_%D1%8D%D0%BB%D0%B5%D0%BC%D0%B5%D0%BD%D1%82%D0%BE%D0%B2_%D0%B8%D0%BD%D1%82%D0%B5%D1%80%D1%84%D0%B5%D0%B9%D1%81%D0%B0)
        {
            var idform = ReadIdForm(xml);
            if (!IsOurForm(idform)) return EmptyOutputParams();

            return XmlOk(@"
<oktellxmlmapper version=""80710"">
  <data name=""outputparams"" count=""1"">
    <property_set name=""formoutput"">
      <property_collection name=""items"" count=""2"">
        <property_set>
          <property_simple key=""id""   value=""normalized""/>
          <property_simple key=""type"" value=""string""/>
          <property_cdata  key=""name""><![CDATA[Нормализованный телефон]]></property_cdata>
        </property_set>
        <property_set>
          <property_simple key=""id""   value=""result""/>
          <property_simple key=""type"" value=""string""/>
          <property_cdata  key=""name""><![CDATA[Результат]]></property_cdata>
        </property_set>
      </property_collection>
    </property_set>
  </data>
</oktellxmlmapper>");
        }

        // 4) Исполнение в сценарии: сюда приходят ЗНАЧЕНИЯ входов; возвращаем значения выходов                    //  [oai_citation:8‡wiki.oktell.ru](https://wiki.oktell.ru/%D0%9E%D0%BF%D0%B8%D1%81%D0%B0%D0%BD%D0%B8%D0%B5_%D0%B1%D0%B0%D0%B7%D0%BE%D0%B2%D1%8B%D1%85_%D1%8D%D0%BB%D0%B5%D0%BC%D0%B5%D0%BD%D1%82%D0%BE%D0%B2_%D0%B8%D0%BD%D1%82%D0%B5%D1%80%D1%84%D0%B5%D0%B9%D1%81%D0%B0)
        public string PrepareShow(string xml)                                                                          // формат коллекций см. вики  [oai_citation:9‡wiki.oktell.ru](https://wiki.oktell.ru/%D0%A4%D0%BE%D1%80%D0%BC%D0%B0%D1%82_%D0%BF%D0%B0%D1%80%D0%B0%D0%BC%D0%B5%D1%82%D1%80%D0%BE%D0%B2_%D0%B8_%D0%B2%D1%8B%D1%85%D0%BE%D0%B4%D0%BD%D1%8B%D1%85_%D0%B7%D0%BD%D0%B0%D1%87%D0%B5%D0%BD%D0%B8%D0%B9)
        {
            var idform = ReadIdForm(xml);
            if (!IsOurForm(idform)) return BuildOutput(("result", "UNKNOWN_FORM"));

            var phone   = ReadInputValue(xml, "phone");
            var country = ReadInputValue(xml, "country");
            var normalized = NormalizePhone(phone, country);

            return BuildOutput(
                ("normalized", normalized),
                ("result", "OK")
            );
        }

        // Остальные обязательные методы-заглушки (должны существовать для регистрации)                              //  [oai_citation:10‡wiki.oktell.ru](https://wiki.oktell.ru/%D0%A1%D1%82%D1%80%D1%83%D0%BA%D1%82%D1%83%D1%80%D0%B0_%D0%B8%D0%BD%D1%82%D0%B5%D1%80%D1%84%D0%B5%D0%B9%D1%81%D0%B0)
        public string ShowDesign(string xml)         => string.Empty;
        public string GetCurrentFillInfo(string xml) => string.Empty;
        public string StopShow(string xml)           => string.Empty;
        public string GetControlResult(string xml)   => string.Empty;

        // Обязательная точная сигнатура; UI не нужен — просто вернуть null                                          //  [oai_citation:11‡wiki.oktell.ru](https://wiki.oktell.ru/%D0%A1%D1%82%D1%80%D1%83%D0%BA%D1%82%D1%83%D1%80%D0%B0_%D0%B8%D0%BD%D1%82%D0%B5%D1%80%D1%84%D0%B5%D0%B9%D1%81%D0%B0)
        public Control CreateControl(string xml)     => null;

        // Служебные вызовы (в т.ч. pluginloaded=20401)                                                              //  [oai_citation:12‡wiki.oktell.ru](https://wiki.oktell.ru/%D0%A1%D1%82%D1%80%D1%83%D0%BA%D1%82%D1%83%D1%80%D0%B0_%D0%B8%D0%BD%D1%82%D0%B5%D1%80%D1%84%D0%B5%D0%B9%D1%81%D0%B0)
        public string DoQuery(string xml) =>
            XmlOk(@"<oktellxmlmapper version=""80710""><data name=""ok"" count=""0""/></oktellxmlmapper>");

        // ========== УТИЛИТЫ (минимум) ==========

        private static bool IsOurForm(string idform)
            => Guid.TryParse(idform, out var g) && g == FormId;

        private static string ReadIdForm(string xml)
        {
            var doc = new XmlDocument(); doc.LoadXml(xml);

            // Полные теги
            if (doc.SelectSingleNode("//property_simple[@key='idform']") is XmlElement el && el.HasAttribute("value"))
                return el.GetAttribute("value");
            var c = doc.SelectSingleNode("//property_cdata[@key='idform']")?.InnerText;
            if (!string.IsNullOrEmpty(c)) return c;

            // Сокращённые теги (prs/prc) — на всякий случай
            if (doc.SelectSingleNode("//prs[@k='idform']") is XmlElement el2 && el2.HasAttribute("v"))
                return el2.GetAttribute("v");
            return doc.SelectSingleNode("//prc[@k='idform']")?.InnerText ?? string.Empty;
        }

        private static string ReadInputValue(string xml, string id)
        {
            var doc = new XmlDocument(); doc.LoadXml(xml);

            // Ищем набор свойств, где id=<id> (поддерживаем полные и короткие теги)
            var set = doc.SelectSingleNode($"//property_set[property_simple[@key='id' and @value='{id}']]")
                   ?? doc.SelectSingleNode($"//s[prs[@k='id' and @v='{id}']]");

            var valSimple = (set?.SelectSingleNode(".//property_simple[@key='value']") as XmlElement)?.GetAttribute("value")
                         ?? (set?.SelectSingleNode(".//prs[@k='value']") as XmlElement)?.GetAttribute("v");

            var valCdata  = set?.SelectSingleNode(".//property_cdata[@key='value']")?.InnerText
                         ?? set?.SelectSingleNode(".//prc[@k='value']")?.InnerText;

            return string.IsNullOrEmpty(valCdata) ? (valSimple ?? "") : valCdata;
        }

        private static string NormalizePhone(string raw, string defaultCountry)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            var sb = new StringBuilder();
            foreach (var ch in raw) if (char.IsDigit(ch)) sb.Append(ch);

            var digits = sb.ToString();
            var hasPlus = raw.Trim().StartsWith("+");
            if (digits.StartsWith("00")) digits = digits.Substring(2);  // международный префикс 00
            if (hasPlus) return digits;
            if (!string.IsNullOrEmpty(defaultCountry))
                return defaultCountry + digits.TrimStart('8', '0');
            return digits;
        }

        private static string BuildOutput(params (string id, string value)[] pairs)
        {
            var sb = new StringBuilder();
            sb.Append(@"<?xml version=""1.0"" encoding=""utf-16""?>");
            sb.Append(@"<oktellxmlmapper version=""80710""><data name=""output"" count=""1""><property_set name=""formoutput""><property_collection name=""items"" count=""");
            sb.Append(pairs.Length);
            sb.Append(@""">");
            foreach (var p in pairs)
            {
                sb.Append(@"<property_set><property_simple key=""id"" value=""")
                  .Append(p.id)
                  .Append(@"""/><property_cdata key=""value""><![CDATA[")
                  .Append(p.value ?? "")
                  .Append(@"]]></property_cdata></property_set>");
            }
            sb.Append(@"</property_collection></property_set></data></oktellxmlmapper>");
            return sb.ToString();
        }

        private static string XmlOk(string innerWithoutHeader)
            => $@"<?xml version=""1.0"" encoding=""utf-16""?>{innerWithoutHeader}";

        private static string EmptyInputParams()  => XmlOk(@"<oktellxmlmapper version=""80710""><data name=""inputparams""  count=""0""/></oktellxmlmapper>");
        private static string EmptyOutputParams() => XmlOk(@"<oktellxmlmapper version=""80710""><data name=""outputparams"" count=""0""/></oktellxmlmapper>");
    }
}