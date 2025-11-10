// Цель: .NET Framework 4.7.2/4.8
using System;
using System.Windows.Forms;
using System.Xml;

namespace Oktell.MinPlugin
{
    public class PluginEntry
    {
        // === Обязательный делегат/событие ===
        public delegate string PluginQueryInvoker(string xml);
        public event PluginQueryInvoker OnQuery;

        // === GUID'ы (можно оставить или заменить своими) ===
        private static readonly Guid PluginId  = new Guid("b3e6f6a7-4c2a-4a7f-9f6d-8a1c2b0e9e55");
        private static readonly Guid FormId    = new Guid("e0a2a2f5-0b54-4c9f-8a24-64d2f8c8a7aa");

        // === Обязательные методы интерфейса ===
        public Guid   GetId()                                => PluginId;
        public int    GetInterfaceVersion(int v)             => v;
        public string GetModuleVersion()                     => "1.0.0";
        public string GetModuleName()                        => "Scenario Tools (Minimal)";
        public string GetDBUpdate()                          => string.Empty;

        // 1) Объявляем одну серверную форму (scriptcomponent/none)
        public string GetForms() =>
@"<?xml version=""1.0"" encoding=""utf-16""?>
<oktellxmlmapper version=""80710"">
  <data name=""availableforms"" count=""1"">
    <property_set>
      <property_simple key=""id"" value=""" + FormId + @""" />
      <property_cdata  key=""name""><![CDATA[Нормализация телефона]]></property_cdata>
      <property_cdata  key=""description""><![CDATA[Сценарный компонент: нормализует телефон]]></property_cdata>
      <property_simple key=""controltype"" value=""2"" name=""none"" />
      <property_simple key=""module"" value=""2"" name=""scriptcomponent"" />
    </property_set>
  </data>
</oktellxmlmapper>";

        // 2) По idform возвращаем схему ВХОДОВ (редактор свойств)
        public string GetInputParams(string xml)
        {
            var idform = GetIdForm(xml);
            if (!IsOurForm(idform)) return EmptyInputParams();
            return
@"<?xml version=""1.0"" encoding=""utf-16""?>
<oktellxmlmapper version=""80710"">
  <data name=""inputparams"" count=""1"">
    <property_set name=""forminput"">
      <property_collection name=""items"" count=""2"">
        <property_set>
          <property_simple key=""id"" value=""phone""/>
          <property_simple key=""type"" value=""string""/>
          <property_cdata  key=""name""><![CDATA[Телефон]]></property_cdata>
          <property_cdata  key=""default""><![CDATA[]]></property_cdata>
        </property_set>
        <property_set>
          <property_simple key=""id"" value=""country""/>
          <property_simple key=""type"" value=""string""/>
          <property_cdata  key=""name""><![CDATA[Код страны]]></property_cdata>
          <property_cdata  key=""default""><![CDATA[7]]></property_cdata>
        </property_set>
      </property_collection>
    </property_set>
  </data>
</oktellxmlmapper>";
        }

        // 3) По idform возвращаем схему ВЫХОДОВ (редактор свойств)
        public string GetOutputParams(string xml)
        {
            var idform = GetIdForm(xml);
            if (!IsOurForm(idform)) return EmptyOutputParams();
            return
@"<?xml version=""1.0"" encoding=""utf-16""?>
<oktellxmlmapper version=""80710"">
  <data name=""outputparams"" count=""1"">
    <property_set name=""formoutput"">
      <property_collection name=""items"" count=""2"">
        <property_set>
          <property_simple key=""id"" value=""normalized""/>
          <property_simple key=""type"" value=""string""/>
          <property_cdata  key=""name""><![CDATA[Нормализованный телефон]]></property_cdata>
        </property_set>
        <property_set>
          <property_simple key=""id"" value=""result""/>
          <property_simple key=""type"" value=""string""/>
          <property_cdata  key=""name""><![CDATA[Результат]]></property_cdata>
        </property_set>
      </property_collection>
    </property_set>
  </data>
</oktellxmlmapper>";
        }

        // 4) Логика выполнения в сценарии: читаем ЗНАЧЕНИЯ входов, возвращаем выходы
        public string PrepareShow(string xml)
        {
            var idform = GetIdForm(xml);
            if (!IsOurForm(idform)) return BuildOutput(("result", "UNKNOWN_FORM"));

            var phone   = GetInputValue(xml, "phone");
            var country = GetInputValue(xml, "country");
            var normalized = NormalizePhone(phone, country);

            return BuildOutput(
                ("normalized", normalized),
                ("result", "OK")
            );
        }

        // Остальные обязательные методы-заглушки
        public string ShowDesign(string xml)         => string.Empty;
        public string GetCurrentFillInfo(string xml) => string.Empty;
        public string StopShow(string xml)           => string.Empty;
        public string GetControlResult(string xml)   => string.Empty;

        // Сигнатура должна быть точной; UI не нужен — вернём null
        public Control CreateControl(string xml)     => null;

        public string DoQuery(string xml) =>
@"<?xml version=""1.0"" encoding=""utf-16""?>
<oktellxmlmapper version=""80710""><data name=""ok"" count=""0""/></oktellxmlmapper>";

        // ===== ВСПОМОГАТЕЛЬНОЕ =====

        private static bool IsOurForm(string idform) =>
            Guid.TryParse(idform, out var f) && f == FormId;

        private static string GetIdForm(string xml)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            // Полная форма
            var el = doc.SelectSingleNode("//property_simple[@key='idform']") as XmlElement;
            if (el?.HasAttribute("value") == true) return el.GetAttribute("value");

            // Короткая форма (prs/prc)
            el = doc.SelectSingleNode("//prs[@k='idform']") as XmlElement;
            if (el?.HasAttribute("v") == true) return el.GetAttribute("v");

            var cdata = doc.SelectSingleNode("//property_cdata[@key='idform']")?.InnerText
                     ?? doc.SelectSingleNode("//prc[@k='idform']")?.InnerText;

            return cdata ?? string.Empty;
        }

        private static string GetInputValue(string xml, string id)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            // property_set, где id=<id>
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
            var digits = new System.Text.StringBuilder();
            foreach (var ch in raw) if (char.IsDigit(ch)) digits.Append(ch);

            var s = digits.ToString();
            var hasPlus = raw.Trim().StartsWith("+");

            if (s.StartsWith("00")) s = s.Substring(2); // международный префикс 00
            if (hasPlus) return s;
            if (!string.IsNullOrEmpty(defaultCountry))
                return defaultCountry + s.TrimStart('8', '0'); // простая нормализация
            return s;
        }

        private static string BuildOutput(params (string id, string value)[] pairs)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(@"<?xml version=""1.0"" encoding=""utf-16""?>
<oktellxmlmapper version=""80710"">
  <data name=""output"" count=""1"">
    <property_set name=""formoutput"">
      <property_collection name=""items"" count=""").Append(pairs.Length).Append(@"">");
            foreach (var p in pairs)
            {
                sb.Append(@"
        <property_set>
          <property_simple key=""id"" value=""").Append(p.id).Append(@""" />
          <property_cdata  key=""value""><![CDATA[").Append(p.value ?? "").Append(@"]]></property_cdata>
        </property_set>");
            }
            sb.Append(@"
      </property_collection>
    </property_set>
  </data>
</oktellxmlmapper>");
            return sb.ToString();
        }

        private static string EmptyInputParams() =>
@"<?xml version=""1.0"" encoding=""utf-16""?>
<oktellxmlmapper version=""80710""><data name=""inputparams"" count=""0""></data></oktellxmlmapper>";

        private static string EmptyOutputParams() =>
@"<?xml version=""1.0"" encoding=""utf-16""?>
<oktellxmlmapper version=""80710""><data name=""outputparams"" count=""0""></data></oktellxmlmapper>";
    }
}