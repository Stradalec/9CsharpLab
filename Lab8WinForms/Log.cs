using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Lab8WinForms
{

    public class XmlLogger
    {
        private string logPath;

        public XmlLogger(string logPath)
        {
            this.logPath = logPath;
        }

        public void CreateLog(string logMessage)
        {
            if (!File.Exists(logPath))
            {
                // Если файла нет, создаём его
                using (StreamWriter writer = File.CreateText(logPath))
                {
                    writer.WriteLine("=== Начало лога ===");
                }
            }

            // Добавляем сообщение в лог
            using (StreamWriter writer = File.AppendText(logPath))
            {
                writer.WriteLine($"{DateTime.Now}: {logMessage}");
            }
        }
        public string GetLastString()
        {
            string last = File.ReadAllLines(logPath).Last();
            return last;
        }
    }

    public class JsonLogger
    {
        private string logPath;

        public JsonLogger(string logPath)
        {
            this.logPath = logPath;
        }

        public void CreateLog(string logMessage)
        {
            if (!File.Exists(logPath))
            {
                var logEntry = new { timestamp = DateTime.Now, logMessage = "=== Начало лога ===" };
                var json = JsonConvert.SerializeObject(logEntry);
                json += "\n";
                File.AppendAllText(logPath, json);
            }
            else
            {
                var logEntry = new { timestamp = DateTime.Now, logMessage = logMessage };
                var json = JsonConvert.SerializeObject(logEntry);
                json += "\n";
                File.AppendAllText(logPath, json);
            }
        }
    }
}
