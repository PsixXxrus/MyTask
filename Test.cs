// Target: .NET Framework 4.7.2/4.8
using System;
using System.Windows.Forms;

namespace Oktell.EtalonPlugin
{
    // ===== Трассировка в файл ProgramData =====
    internal static class Tr
    {
        private static readonly string LogPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "OktellPluginEtalon", "trace.log");

        public static void Log(string msg)
        {
            try
            {
                var dir = System.IO.Path.GetDirectoryName(LogPath);
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                System.IO.File.AppendAllText(LogPath,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff ") + msg + Environment.NewLine);
            }
            catch { /* ignore */ }
        }
    }

    // ===== Эталонный плагин =====
    public class PluginEntry
    {
        // === Делегат и событие (обязательны) ===
        public delegate string PluginQueryInvoker(string xml);
        public event PluginQueryInvoker OnQuery;

        // === GUID'ы (ЗАМЕНИ на свои реальные!) ===
        private static readonly Guid PluginId     = new Guid("11111111-2222-3333-4444-555555555555"); // ID плагина
        private static readonly Guid FormIdScript = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"); // ID сценарной формы

        // === Конструктор без параметров обязателен ===
        public PluginEntry()
        {
            Tr.Log("PluginEntry.ctor()");
        }

        // === Обязательные методы интерфейса ===
        public Guid GetId()
        {
            Tr.Log("GetId()");
            return PluginId;
        }

        public int GetInterfaceVersion(int lastknownversion)
        {
            Tr.Log($"GetInterfaceVersion(lastknownversion={lastknownversion})");
            return lastknownversion; // безопасно отдать поддерживаемую версию
        }

        public string GetModuleVersion()
        {
            Tr.Log("GetModuleVersion()");
            return "1.0.0";
        }

        public string GetModuleName()
        {
            Tr.Log("GetModuleName()");
            return "Scenario Tools (Etalon)";
        }

        public string GetDBUpdate()
        {
            Tr.Log("GetDBUpdate()");
            return string.Empty; // без автообновления БД
        }

        public string GetForms()
        {
            Tr.Log("GetForms()");
            return Safe(() =>
@"<?xml version=""1.0"" encoding=""utf-16""?>
<oktellxmlmapper version=""80710"">
  <data name=""availableforms"" count=""1"">
    <property_set>
      <property_simple key=""id"" value=""" + FormIdScript + @""" />
      <property_cdata  key=""name""><![CDATA[Серверный компонент (эталон)]]></property_cdata>
      <property_cdata  key=""description""><![CDATA[Минимальный сценарный компонент]]></property_cdata>
      <property_simple key=""controltype"" value=""2"" name=""none"" />
      <property_simple key=""module"" value=""2"" name=""scriptcomponent"" />
    </property_set>
  </data>
</oktellxmlmapper>");
        }

        public string GetInputParams(string xml)
        {
            Tr.Log("GetInputParams()");
            return Safe(() =>
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
</oktellxmlmapper>");
        }

        public string GetOutputParams(string xml)
        {
            Tr.Log("GetOutputParams()");
            return Safe(() =>
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
</oktellxmlmapper>");
        }

        public string ShowDesign(string xml)
        {
            Tr.Log("ShowDesign()");
            return string.Empty;
        }

        // === Основная логика сценарного компонента ===
        public string PrepareShow(string xml)
        {
            Tr.Log("PrepareShow() IN");
            try
            {
                // Мини: вернуть OK + нормализованный телефон (уберём всё, кроме цифр; если нет '+', добавим country)
                var (phone, country) = ParseInputs(xml);
                var normalized = NormalizePhone(phone, country);

                var response = 
@"<?xml version=""1.0"" encoding=""utf-16""?>
<oktellxmlmapper version=""80710"">
  <data name=""output"" count=""1"">
    <property_set name=""formoutput"">
      <property_collection name=""items"" count=""2"">
        <property_set>
          <property_simple key=""id"" value=""normalized""/>
          <property_cdata  key=""value""><![CDATA[" + normalized + @"]]></property_cdata>
        </property_set>
        <property_set>
          <property_simple key=""id"" value=""result""/>
          <property_cdata  key=""value""><![CDATA[OK]]></property_cdata>
        </property_set>
      </property_collection>
    </property_set>
  </data>
</oktellxmlmapper>";
                Tr.Log("PrepareShow() OUT OK");
                return response;
            }
            catch (Exception ex)
            {
                Tr.Log("PrepareShow() EX: " + ex.Message);
                return Safe(() =>
@"<?xml version=""1.0"" encoding=""utf-16""?>
<oktellxmlmapper version=""80710"">
  <data name=""output"" count=""1"">
    <property_set name=""formoutput"">
      <property_collection name=""items"" count=""2"">
        <property_set>
          <property_simple key=""id"" value=""normalized""/>
          <property_cdata  key=""value""><![CDATA[]]></property_cdata>
        </property_set>
        <property_set>
          <property_simple key=""id"" value=""result""/>
          <property_cdata  key=""value""><![CDATA[ERROR: " + EscapeCdata(ex.Message) + @"]]></property_cdata>
        </property_set>
      </property_collection>
    </property_set>
  </data>
</oktellxmlmapper>");
            }
        }

        public string GetCurrentFillInfo(string xml)
        {
            Tr.Log("GetCurrentFillInfo()");
            return string.Empty;
        }

        public string StopShow(string xml)
        {
            Tr.Log("StopShow()");
            return string.Empty;
        }

        public string GetControlResult(string xml)
        {
            Tr.Log("GetControlResult()");
            return string.Empty;
        }

        // Точная сигнатура! Можно вернуть null, но тип должен быть Control.
        public Control CreateControl(string xml)
        {
            Tr.Log("CreateControl()");
            return null;
        }

        public string DoQuery(string xml)
        {
            Tr.Log("DoQuery()");
            return
@"<?xml version=""1.0"" encoding=""utf-16""?>
<oktellxmlmapper version=""80710""><data name=""ok"" count=""0""/></oktellxmlmapper>";
        }

        // ==== Вспомогательные методы ====

        private static string Safe(Func<string> buildXml)
        {
            // Валидируем XML до возврата, чтобы инспектор не падал на пустоте
            var s = buildXml();
            var doc = new System.Xml.XmlDocument();
            doc.LoadXml(s);
            return s;
        }

        private static (string phone, string country) ParseInputs(string xml)
        {
            // Простая выборка значений по id=phone / id=country
            string Get(string id)
            {
                var doc = new System.Xml.XmlDocument();
                doc.LoadXml(xml);
                // ищем property_set, содержащий id=<id>
                var node = doc.SelectSingleNode(
                    $"//property_set[child::property_simple[@key='id' and @value='{id}']]");
                if (node == null) return "";
                var valCdata  = node.SelectSingleNode(".//property_cdata[@key='value']")?.InnerText;
                var valSimple = (node.SelectSingleNode(".//property_simple[@key='value']") as System.Xml.XmlElement)?.GetAttribute("value");
                return string.IsNullOrEmpty(valCdata) ? (valSimple ?? "") : valCdata;
            }

            var phone = Get("phone");
            var country = Get("country");
            return (phone ?? "", country ?? "");
        }

        private static string NormalizePhone(string raw, string defaultCountry)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            var digitsOnly = new System.Text.StringBuilder();
            foreach (var ch in raw)
                if (char.IsDigit(ch)) digitsOnly.Append(ch);

            var s = digitsOnly.ToString();
            // если исходная строка начиналась с '+', считаем, что уже международный формат
            var hasPlus = raw.Trim().StartsWith("+");
            if (s.StartsWith("00")) s = s.Substring(2);      // 00 → международный префикс
            if (hasPlus) return s;
            if (!string.IsNullOrEmpty(defaultCountry))
                return defaultCountry + s.TrimStart('8', '0'); // простая нормализация
            return s;
        }

        private static string EscapeCdata(string text)
        {
            return text?.Replace("]]>", "]]]]><![CDATA[>") ?? "";
        }
    }
}