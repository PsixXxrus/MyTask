кusing System;
using System.Collections.Generic;
using System.Windows.Forms;
using OCTell.Plugins; // пространство имён Oktell SDK

namespace MyMinimalPlugin
{
    // Главный класс плагина — ОБЯЗАТЕЛЬНО должен реализовывать IOktellPluginForm
    public class PluginMain : IOktellPluginForm
    {
        public string PluginID => "MyMinimalPlugin"; // уникальный идентификатор
        public string PluginName => "Пример плагина Oktell";

        public PluginMain()
        {
            // Конструктор
        }

        // Возвращает входные параметры формы
        public Dictionary<string, string> GetInputParams(Guid idPlugin, Guid idForm)
        {
            var input = new Dictionary<string, string>
            {
                { "phone", "Номер телефона" }
            };
            return input;
        }

        // Возвращает выходные параметры формы
        public Dictionary<string, string> GetOutputParams(Guid idPlugin, Guid idForm)
        {
            var output = new Dictionary<string, string>
            {
                { "result", "Результат работы плагина" }
            };
            return output;
        }

        // Основной метод запуска
        public Dictionary<string, string> Run(Dictionary<string, string> inputParams, 
                                              out bool success, 
                                              out string message)
        {
            success = true;
            message = "Плагин успешно выполнен";

            var output = new Dictionary<string, string>
            {
                { "result", $"Обработан номер: {inputParams["phone"]}" }
            };

            return output;
        }

        // Метод возвращает WinForm-контрол (если форма требуется)
        public Control GetControl(Guid idPlugin, Guid idForm)
        {
            return new Label
            {
                Text = "Минимальный плагин Oktell работает!",
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
        }
    }
}


using System;
using System.Windows.Forms;

namespace OktellPlugin
{
    /// <summary>
    /// Минимальный родительский контрол плагина без зависимостей от Control01/02/03/04.
    /// Достаточно для корректной работы Base.cs (OnQuery, Mode, GetById).
    /// </summary>
    public class ControlParent : UserControl
    {
        // То, что ожидает Base.cs
        public event Base.PluginQueryInvoker OnQuery;

        /// <summary>
        /// Режим работы (Base.cs пишет сюда 0/1/2/3).
        /// </summary>
        public int Mode { get; set; }

        public ControlParent()
        {
            Dock = DockStyle.Fill;

            // Простейший UI, чтобы было видно, что плагин отрисовался
            var lbl = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                AutoSize = false,
                Text = "Минимальный плагин Oktell: ControlParent без Control01/02/03/04"
            };
            Controls.Add(lbl);
        }

        /// <summary>
        /// Base.cs вызывает cp.GetById(i, true) — делаем безопасный no-op.
        /// </summary>
        public void GetById(int id, bool open)
        {
            // Ничего не делаем — минимальная заглушка
            // По желанию можно поднимать событие, логировать и т.п.
        }

        /// <summary>
        /// Вспомогательный метод: если где-то понадобится дернуть запрос из UI.
        /// </summary>
        public void RaiseQuery(string xml)
        {
            // Если кто-то подписан (Base.cs подписывается), отдадим xml наверх
            var handler = OnQuery;
            if (handler != null)
                handler(xml);
        }
    }
}