using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using System.Xml;

namespace OktellScenarioPlugin
{
    public class PluginScenario
    {
        // Событие для клиентских сервисных запросов — на сервере не используется
        public delegate string PluginQueryInvoker(string xml);
        public event PluginQueryInvoker OnQuery;

        // ==== ИДЕНТИФИКАТОРЫ ====
        private static readonly Guid PluginId = new Guid("11111111-2222-3333-4444-555555555555");
        private static readonly Guid FormId_Script = new Guid("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"); // компонент для сценариев

        // ==== ОБЯЗАТЕЛЬНЫЕ МЕТОДЫ ====
        public Guid GetId() => PluginId;
        public int GetInterfaceVersion(int lastknownversion) => lastknownversion; // отдать поддерживаемую версию
        public string GetModuleVersion() => "1.0.0";
        public string GetModuleName() => "Scenario Tools";
        
        // Автосоздание таблицы чёрного списка (опционально)
        public string GetDBUpdate()
        {
            // Можно вернуть tSQL строкой без xml, сервер выполнит батчи через GO.  [oai_citation:4‡Oktell](https://wiki.oktell.ru/%D0%9E%D0%BF%D0%B8%D1%81%D0%B0%D0%BD%D0%B8%D0%B5_%D0%B1%D0%B0%D0%B7%D0%BE%D0%B2%D1%8B%D1%85_%D1%8D%D0%BB%D0%B5%D0%BC%D0%B5%D0%BD%D1%82%D0%BE%D0%B2_%D0%B8%D0%BD%D1%82%D0%B5%D1%80%D1%84%D0%B5%D0%B9%D1%81%D0%B0)
            return @"
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[T_Blacklist]') AND type in (N'U'))
BEGIN
  CREATE TABLE dbo.T_Blacklist(
    id INT IDENTITY(1,1) PRIMARY KEY,
    phone NVARCHAR(32) NOT NULL UNIQUE
  )
END
GO";
        }

        // Объявляем доступные «формы». Для сценариев: controltype=none, module=scriptcomponent.  [oai_citation:5‡Oktell](https://wiki.oktell.ru/%D0%9E%D0%BF%D0%B8%D1%81%D0%B0%D0%BD%D0%B8%D0%B5_%D0%B1%D0%B0%D0%B7%D0%BE%D0%B2%D1%8B%D1%85_%D1%8D%D0%BB%D0%B5%D0%BC%D0%B5%D0%BD%D1%82%D0%BE%D0%B2_%D0%B8%D0%BD%D1%82%D0%B5%D1%80%D1%84%D0%B5%D0%B9%D1%81%D0%B0)
        public string GetForms() =>
            @"<?xml version=""1.0"" encoding=""utf-16""?>
              <oktellxmlmapper version=""80710"">
                <data name=""availableforms"" count=""1"">
                  <property_set>
                    <property_simple key=""id"" value=""" + FormId_Script + @""" />
                    <property_cdata key=""name""><![CDATA[Нормализация и проверка номера]]></property_cdata>
                    <property_cdata key=""description""><![CDATA[Компонент для сценариев: нормализует телефон и проверяет в БД]]></property_cdata>
                    <property_simple key=""controltype"" value=""2"" name=""none"" />
                    <property_simple key=""module"" value=""2"" name=""scriptcomponent"" />
                  </property_set>
                </data>
              </oktellxmlmapper>";

        // Описываем входные параметры для FormId_Script. Будут видны в инспекторе компонента «Плагин».  [oai_citation:6‡Oktell](https://wiki.oktell.ru/%D0%9E%D0%BF%D0%B8%D1%81%D0%B0%D0%BD%D0%B8%D0%B5_%D0%B1%D0%B0%D0%B7%D0%BE%D0%B2%D1%8B%D1%85_%D1%8D%D0%BB%D0%B5%D0%BC%D0%B5%D0%BD%D1%82%D0%BE%D0%B2_%D0%B8%D0%BD%D1%82%D0%B5%D1%80%D1%84%D0%B5%D0%B9%D1%81%D0%B0)
        public string GetInputParams(string xml)
        {
            // Ожидаем: phone (string), country (string, опционально), conn (строка подключения к БД)
            return @"<?xml version=""1.0"" encoding=""utf-16""?>
                <oktellxmlmapper version=""80710"">
                 <data name=""inputparams"" count=""1"">
                  <property_set name=""forminput"">
                    <property_collection name=""items"" count=""3"">
                      <property_set>
                        <property_simple key=""id"" value=""phone""/>
                        <property_simple key=""type"" value=""string""/>
                        <property_cdata key=""name""><![CDATA[Телефон]]></property_cdata>
                        <property_cdata key=""default""><![CDATA[]]></property_cdata>
                      </property_set>
                      <property_set>
                        <property_simple key=""id"" value=""country""/>
                        <property_simple key=""type"" value=""string""/>
                        <property_cdata key=""name""><![CDATA[Код страны (если нет +)]]></property_cdata>
                        <property_cdata key=""default""><![CDATA[7]]></property_cdata>
                      </property_set>
                      <property_set>
                        <property_simple key=""id"" value=""conn""/>
                        <property_simple key=""type"" value=""string""/>
                        <property_cdata key=""name""><![CDATA[Строка подключения к БД]]></property_cdata>
                        <property_cdata key=""default""><![CDATA[]]></property_cdata>
                      </property_set>
                    </property_collection>
                  </property_set>
                 </data>
                </oktellxmlmapper>";
        }

        // Описываем выходные значения. Администратор привяжет их к переменным сценария.  [oai_citation:7‡Oktell](https://wiki.oktell.ru/%D0%9E%D0%BF%D0%B8%D1%81%D0%B0%D0%BD%D0%B8%D0%B5_%D0%B1%D0%B0%D0%B7%D0%BE%D0%B2%D1%8B%D1%85_%D1%8D%D0%BB%D0%B5%D0%BC%D0%B5%D0%BD%D1%82%D0%BE%D0%B2_%D0%B8%D0%BD%D1%82%D0%B5%D1%80%D1%84%D0%B5%D0%B9%D1%81%D0%B0)
        public string GetOutputParams(string xml)
        {
            return @"<?xml version=""1.0"" encoding=""utf-16""?>
                <oktellxmlmapper version=""80710"">
                 <data name=""outputparams"" count=""1"">
                  <property_set name=""formoutput"">
                    <property_collection name=""items"" count=""4"">
                      <property_set>
                        <property_simple key=""id"" value=""normalized""/>
                        <property_simple key=""type"" value=""string""/>
                        <property_cdata key=""name""><![CDATA[Нормализованный телефон]]></property_cdata>
                      </property_set>
                      <property_set>
                        <property_simple key=""id"" value=""isblack""/>
                        <property_simple key=""type"" value=""bool""/>
                        <property_cdata key=""name""><![CDATA[В чёрном списке]]></property_cdata>
                      </property_set>
                      <property_set>
                        <property_simple key=""id"" value=""code""/>
                        <property_simple key=""type"" value=""int""/>
                        <property_cdata key=""name""><![CDATA[Код результата]]></property_cdata>
                      </property_set>
                      <property_set>
                        <property_simple key=""id"" value=""message""/>
                        <property_simple key=""type"" value=""string""/>
                        <property_cdata key=""name""><![CDATA[Сообщение]]></property_cdata>
                      </property_set>
                    </property_collection>
                  </property_set>
                 </data>
                </oktellxmlmapper>";
        }

        // Диалоговые методы — не используются, оставляем пустыми
        public string ShowDesign(string xml) => string.Empty;
        public string GetCurrentFillInfo(string xml) => string.Empty;
        public string StopShow(string xml) => string.Empty;
        public string GetControlResult(string xml) => string.Empty;
        public System.Windows.Forms.Control CreateControl(string xml) => null;

        // === ГЛАВНАЯ ЛОГИКА ДЛЯ СЕРВЕРА ===
        // Сервер сценариев вызывает именно PrepareShow: вход → логика → выход.  [oai_citation:8‡Oktell](https://wiki.oktell.ru/%D0%9E%D0%BF%D0%B8%D1%81%D0%B0%D0%BD%D0%B8%D0%B5_%D0%B1%D0%B0%D0%B7%D0%BE%D0%B2%D1%8B%D1%85_%D1%8D%D0%BB%D0%B5%D0%BC%D0%B5%D0%BD%D1%82%D0%BE%D0%B2_%D0%B8%D0%BD%D1%82%D0%B5%D1%80%D1%84%D0%B5%D0%B9%D1%81%D0%B0)
        public string PrepareShow(string xml)
        {
            try
            {
                var ctx = InputReader.ParsePrepareShow(xml); // phone, country, conn
                string normalized = NormalizePhone(ctx.Phone, ctx.CountryCode);
                bool isBlack = !string.IsNullOrEmpty(ctx.ConnectionString)
                               && CheckBlacklist(ctx.ConnectionString, normalized);

                return OutputWriter.Build(
                    normalized: normalized,
                    isBlack: isBlack,
                    code: 0,
                    message: "OK");
            }
            catch (Exception ex)
            {
                return OutputWriter.Build(
                    normalized: "",
                    isBlack: false,
                    code: -1,
                    message: ex.Message);
            }
        }

        // Сервисные запросы — не требуются в server-only сценарии
        public string DoQuery(string xml)
        {
            // можем вернуть OK на pluginloaded и пр. — для полноты интерфейса.  [oai_citation:9‡Oktell](https://wiki.oktell.ru/%D0%A1%D1%82%D1%80%D1%83%D0%BA%D1%82%D1%83%D1%80%D0%B0_%D0%B8%D0%BD%D1%82%D0%B5%D1%80%D1%84%D0%B5%D0%B9%D1%81%D0%B0)
            return @"<?xml version=""1.0"" encoding=""utf-16""?><oktellxmlmapper version=""80710""><data name=""ok"" count=""0""/></oktellxmlmapper>";
        }

        // ==== Бизнес-логика ====

        private static string NormalizePhone(string raw, string defaultCountry)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            var digits = new StringBuilder();
            foreach (char c in raw)
                if (char.IsDigit(c)) digits.Append(c);

            var s = digits.ToString();
            if (s.StartsWith("00")) s = s.Substring(2);     // 00 → международный
            if (raw.Trim().StartsWith("+")) return s;       // уже международный
            if (!string.IsNullOrEmpty(defaultCountry))
                return defaultCountry + s.TrimStart('8','0'); // простая нормализация
            return s;
        }

        private static bool CheckBlacklist(string conn, string phone)
        {
            if (string.IsNullOrEmpty(conn) || string.IsNullOrEmpty(phone))
                return false;

            using (var cn = new SqlConnection(conn))
            using (var cmd = new SqlCommand("SELECT 1 FROM dbo.T_Blacklist WHERE phone=@p", cn))
            {
                cmd.Parameters.AddWithValue("@p", phone);
                cn.Open();
                var x = cmd.ExecuteScalar();
                return x != null;
            }
        }
    }
}


using System.Xml;
using System.Text;

namespace OktellScenarioPlugin
{
    internal static class InputReader
    {
        internal sealed class Ctx
        {
            public string Phone { get; set; }
            public string CountryCode { get; set; }
            public string ConnectionString { get; set; }
        }

        public static Ctx ParsePrepareShow(string xml)
        {
            // Из параметра приходят вычисленные input-поля (см. GetInputParams).  [oai_citation:11‡Oktell](https://wiki.oktell.ru/%D0%9E%D0%BF%D0%B8%D1%81%D0%B0%D0%BD%D0%B8%D0%B5_%D0%B1%D0%B0%D0%B7%D0%BE%D0%B2%D1%8B%D1%85_%D1%8D%D0%BB%D0%B5%D0%BC%D0%B5%D0%BD%D1%82%D0%BE%D0%B2_%D0%B8%D0%BD%D1%82%D0%B5%D1%80%D1%84%D0%B5%D0%B9%D1%81%D0%B0)
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            string Get(string id)
            {
                var n = doc.SelectSingleNode($"//property_set[@name='item' or @name='items']/property_set[property_simple[@key='id' and @value='{id}']]");
                if (n == null) n = doc.SelectSingleNode($"//property_set[prs[@k='id' and @v='{id}']]");
                var valNode = n?.SelectSingleNode(".//property_cdata[@key='value']") ?? n?.SelectSingleNode(".//property_simple[@key='value']");
                return valNode?.InnerText ?? "";
            }

            return new Ctx
            {
                Phone = Get("phone"),
                CountryCode = Get("country"),
                ConnectionString = Get("conn"),
            };
        }
    }

    internal static class OutputWriter
    {
        public static string Build(string normalized, bool isBlack, int code, string message)
        {
            // Возврат значений компонента формуирует сценарий: сохранить в переменные, которые админ назначил.
            // Структура совместима с «Описание базовых элементов интерфейса».  [oai_citation:12‡Oktell](https://wiki.oktell.ru/%D0%9E%D0%BF%D0%B8%D1%81%D0%B0%D0%BD%D0%B8%D0%B5_%D0%B1%D0%B0%D0%B7%D0%BE%D0%B2%D1%8B%D1%85_%D1%8D%D0%BB%D0%B5%D0%BC%D0%B5%D0%BD%D1%82%D0%BE%D0%B2_%D0%B8%D0%BD%D1%82%D0%B5%D1%80%D1%84%D0%B5%D0%B9%D1%81%D0%B0)
            var sb = new StringBuilder();
            sb.Append(@"<?xml version=""1.0"" encoding=""utf-16""?>
<oktellxmlmapper version=""80710"">
 <data name=""output"" count=""1"">
  <property_set name=""formoutput"">
   <property_collection name=""items"" count=""4"">
    <property_set>
      <property_simple key=""id"" value=""normalized""/>
      <property_cdata key=""value""><![CDATA[").Append(normalized).Append(@"]]></property_cdata>
    </property_set>
    <property_set>
      <property_simple key=""id"" value=""isblack""/>
      <property_simple key=""value"" value=""").Append(isBlack ? "1" : "0").Append(@"""/>
    </property_set>
    <property_set>
      <property_simple key=""id"" value=""code""/>
      <property_simple key=""value"" value=""").Append(code).Append(@"""/>
    </property_set>
    <property_set>
      <property_simple key=""id"" value=""message""/>
      <property_cdata key=""value""><![CDATA[").Append(message).Append(@"]]></property_cdata>
    </property_set>
   </property_collection>
  </property_set>
 </data>
</oktellxmlmapper>");
            return sb.ToString();
        }
    }
}